using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

#if FAMISTUDIO_WINDOWS
using RenderFont     = SharpDX.DirectWrite.TextFormat;
    using RenderBitmap   = SharpDX.Direct2D1.Bitmap;
    using RenderBrush    = SharpDX.Direct2D1.Brush;
    using RenderGeometry = SharpDX.Direct2D1.PathGeometry;
    using RenderControl  = FamiStudio.Direct2DControl;
    using RenderGraphics = FamiStudio.Direct2DOffscreenGraphics;
    using RenderTheme    = FamiStudio.Direct2DTheme;
#else
    using RenderFont     = FamiStudio.GLFont;
    using RenderBitmap   = FamiStudio.GLBitmap;
    using RenderBrush    = FamiStudio.GLBrush;
    using RenderGeometry = FamiStudio.GLGeometry;
    using RenderControl  = FamiStudio.GLControl;
    using RenderGraphics = FamiStudio.GLOffscreenGraphics;
    using RenderTheme    = FamiStudio.GLTheme;
#endif

namespace FamiStudio
{
    class VideoFileOscilloscope : VideoFileBase
    {
        private void BuildChannelColors(List<VideoChannelState> channels, VideoFrameMetadata[] meta, int colorMode)
        {
            Color[,] colors = new Color[meta.Length, meta[0].channelNotes.Length];

            // Get the note colors.
            for (int i = 0; i < meta.Length; i++)
            {
                var m = meta[i];

                m.channelColors = new Color[m.channelNotes.Length];

                for (int j = 0; j < channels.Count; j++)
                {
                    var note = m.channelNotes[channels[j].songChannelIndex];

                    if (note != null && note.IsMusical)
                    {
                        var color = Color.Transparent;

                        if (channels[j].channel.Type == ChannelType.Dpcm)
                        {
                            if (colorMode == OscilloscopeColorType.InstrumentsAndSamples)
                            {
                                var mapping = channels[j].channel.Song.Project.GetDPCMMapping(note.Value);
                                if (mapping != null)
                                    color = mapping.Sample.Color;
                            }
                            else
                            {
                                color = ThemeBase.LightGreyFillColor1;
                            }
                        }
                        else
                        {
                            color = note.Instrument != null && colorMode != OscilloscopeColorType.None ? note.Instrument.Color : ThemeBase.LightGreyFillColor1;
                        }

                        colors[i, j] = color;
                    }
                }
            }

            // Extend any color until we hit another one.
            for (int i = 0; i < colors.GetLength(0) - 1; i++)
            {
                for (int j = 0; j < colors.GetLength(1); j++)
                {
                    if (colors[i, j].A != 0)
                    {
                        if (colors[i + 1, j].A == 0)
                            colors[i + 1, j] = colors[i, j];
                    }
                    else
                    {
                        colors[i, j] = ThemeBase.LightGreyFillColor1;
                    }
                }
            }

            const int ColorBlendTime = 5;

            // Blend the colors.
            for (int i = 0; i < meta.Length; i++)
            {
                var m = meta[i];

                for (int j = 0; j < m.channelColors.Length; j++)
                {
                    int avgR = 0;
                    int avgG = 0;
                    int avgB = 0;
                    int count = 0;

                    for (int k = i; k < i + ColorBlendTime && k < meta.Length; k++)
                    {
                        avgR += colors[k, j].R;
                        avgG += colors[k, j].G;
                        avgB += colors[k, j].B;
                        count++;
                    }

                    m.channelColors[j] = Color.FromArgb(avgR / count, avgG / count, avgB / count);
                }
            }
        }

        public unsafe bool Save(Project originalProject, int songId, int loopCount, int colorMode, int numColumns, int lineThickness, string ffmpegExecutable, string filename, int resX, int resY, bool halfFrameRate, int channelMask, int audioBitRate, int videoBitRate, bool stereo, float[] pan)
        {
            if (!Initialize(ffmpegExecutable, channelMask, loopCount))
                return false;

            videoResX = resX;
            videoResY = resY;

            var project = originalProject.DeepClone();
            var song = project.GetSong(songId);

            ExtendSongForLooping(song, loopCount);

            Log.LogMessage(LogSeverity.Info, "Initializing channels...");

            var frameRateNumerator = song.Project.PalMode ? 5000773 : 6009883;
            if (halfFrameRate)
                frameRateNumerator /= 2;
            var frameRate = frameRateNumerator.ToString() + "/100000";

            var numChannels = Utils.NumberOfSetBits(channelMask);
            var longestChannelName = 0.0f;
            var videoGraphics = RenderGraphics.Create(videoResX, videoResY, true);

            if (videoGraphics == null)
            {
                Log.LogMessage(LogSeverity.Error, "Error initializing off-screen graphics, aborting.");
                return false;
            }

            var theme = RenderTheme.CreateResourcesForGraphics(videoGraphics);
            var bmpWatermark = videoGraphics.CreateBitmapFromResource("VideoWatermark");

            // Generate WAV data for each individual channel for the oscilloscope.
            var channelStates = new List<VideoChannelState>();
            var maxAbsSample = 0;

            for (int i = 0, channelIndex = 0; i < song.Channels.Length; i++)
            {
                if ((channelMask & (1 << i)) == 0)
                    continue;

                var pattern = song.Channels[i].PatternInstances[0];
                var state = new VideoChannelState();

                state.videoChannelIndex = channelIndex;
                state.songChannelIndex = i;
                state.channel = song.Channels[i];
                state.channelText = state.channel.NameWithExpansion;
                state.wav = new WavPlayer(SampleRate, 1, 1 << i).GetSongSamples(song, song.Project.PalMode, -1);

                channelStates.Add(state);
                channelIndex++;

                // Find maximum absolute value to rescale the waveform.
                foreach (short s in state.wav)
                    maxAbsSample = Math.Max(maxAbsSample, Math.Abs(s));

                // Measure the longest text.
                longestChannelName = Math.Max(longestChannelName, videoGraphics.MeasureString(state.channelText, ThemeBase.FontBigUnscaled));
            }

            numColumns = Math.Min(numColumns, channelStates.Count);

            var numRows = (int)Math.Ceiling(channelStates.Count / (float)numColumns);

            var channelResXFloat = videoResX / (float)numColumns;
            var channelResYFloat = videoResY / (float)numRows;

            var channelResX = (int)channelResXFloat;
            var channelResY = (int)channelResYFloat;

            // Tweak some cosmetic stuff that depends on resolution.
            var smallChannelText = channelResY < 128;
            var bmpSuffix = smallChannelText ? "" : "@2x";
            var font = lineThickness > 1 ?
                (smallChannelText ? ThemeBase.FontMediumBoldUnscaled : ThemeBase.FontBigBoldUnscaled) : 
                (smallChannelText ? ThemeBase.FontMediumUnscaled     : ThemeBase.FontBigUnscaled);
            var textOffsetY = smallChannelText ? 1 : 4;
            var channelLineWidth = resY >= 720 ? 5 : 3;

            foreach (var s in channelStates)
                s.bmpIcon = videoGraphics.CreateBitmapFromResource(ChannelType.Icons[s.channel.Type] + bmpSuffix);

            var gradientSizeY = channelResY / 2;
            var gradientBrush = videoGraphics.CreateVerticalGradientBrush(0, gradientSizeY, Color.Black, Color.FromArgb(0, Color.Black));

            // Generate the metadata for the video so we know what's happening at every frame
            var metadata = new VideoMetadataPlayer(SampleRate, 1).GetVideoMetadata(song, song.Project.PalMode, -1);

            var oscScale = maxAbsSample != 0 ? short.MaxValue / (float)maxAbsSample : 1.0f;
            var oscLookback = (metadata[1].wavOffset - metadata[0].wavOffset) / 2;
            var oscWindowSize = (int)Math.Round(SampleRate * OscilloscopeWindowSize);

            BuildChannelColors(channelStates, metadata, colorMode);

#if FAMISTUDIO_LINUX || FAMISTUDIO_MACOS
            var dummyControl = new DummyGLControl();
            dummyControl.Move(0, 0, videoResX, videoResY);
#endif

            var videoImage = new byte[videoResY * videoResX * 4];
            var oscilloscope = new float[oscWindowSize, 2];

            // Start ffmpeg with pipe input.
            var tempFolder = Utils.GetTemporaryDiretory();
            var tempAudioFile = Path.Combine(tempFolder, "temp.wav");

#if !DEBUG
            try
#endif
            {
                Log.LogMessage(LogSeverity.Info, "Exporting audio...");

                // Save audio to temporary file.
                AudioExportUtils.Save(song, tempAudioFile, SampleRate, 1, -1, channelMask, false, false, stereo, pan, (samples, samplesChannels, fn) => { WaveFile.Save(samples, fn, SampleRate, samplesChannels); });

                var process = LaunchFFmpeg(ffmpegExecutable, $"-y -f rawvideo -pix_fmt argb -s {videoResX}x{videoResY} -r {frameRate} -i - -i \"{tempAudioFile}\" -c:v h264 -pix_fmt yuv420p -b:v {videoBitRate}K -c:a aac -b:a {audioBitRate}k \"{filename}\"", true, false);

#if FAMISTUDIO_WINDOWS
                // Cant raise the process priority without being admin on Linux/MacOS.
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
#endif

                // Generate each of the video frames.
                using (var stream = new BinaryWriter(process.StandardInput.BaseStream))
                {
                    for (int f = 0; f < metadata.Length; f++)
                    {
                        if (Log.ShouldAbortOperation)
                            break;

                        if ((f % 100) == 0)
                            Log.LogMessage(LogSeverity.Info, $"Rendering frame {f} / {metadata.Length}");

                        Log.ReportProgress(f / (float)(metadata.Length - 1));

                        if (halfFrameRate && (f & 1) != 0)
                            continue;

                        var frame = metadata[f];

#if FAMISTUDIO_LINUX || FAMISTUDIO_MACOS
                        videoGraphics.BeginDraw(dummyControl, videoResY);
#else
                        videoGraphics.BeginDraw();
#endif
                        videoGraphics.Clear(ThemeBase.DarkGreyLineColor2);

                        // Draw gradients.
                        for (int i = 0; i < numRows; i++)
                        {
                            videoGraphics.PushTranslation(0, i * channelResY);
                            videoGraphics.FillRectangle(0, 0, videoResX, channelResY, gradientBrush);
                            videoGraphics.PopTransform();
                        }

                        // Channel names + oscilloscope
                        for (int i = 0; i < channelStates.Count; i++)
                        {
                            var s = channelStates[i];

                            var channelX = i % numColumns;
                            var channelY = i / numColumns;

                            var channelPosX0 = (channelX + 0) * channelResX;
                            var channelPosX1 = (channelX + 1) * channelResX;
                            var channelPosY0 = (channelY + 0) * channelResY;
                            var channelPosY1 = (channelY + 1) * channelResY;

                            // Intentionally flipping min/max Y since D3D is upside down compared to how we display waves typically.
                            GenerateOscilloscope(s.wav, frame.wavOffset, oscWindowSize, oscLookback, oscScale, channelPosX0, channelPosY1, channelPosX1, channelPosY0, oscilloscope);

                            var geo = videoGraphics.CreateGeometry(oscilloscope, false);
                            var brush = videoGraphics.GetSolidBrush(frame.channelColors[i]);

                            videoGraphics.AntiAliasing = true;
                            videoGraphics.DrawGeometry(geo, brush, lineThickness);
                            videoGraphics.AntiAliasing = false;
                            geo.Dispose();

                            var channelIconPosX = channelPosX0 + s.bmpIcon.Size.Width  / 2;
                            var channelIconPosY = channelPosY0 + s.bmpIcon.Size.Height / 2;

                            videoGraphics.FillRectangle(channelIconPosX, channelIconPosY, channelIconPosX + s.bmpIcon.Size.Width, channelIconPosY + s.bmpIcon.Size.Height, theme.DarkGreyLineBrush2);
                            videoGraphics.DrawBitmap(s.bmpIcon, channelIconPosX, channelIconPosY);
                            videoGraphics.DrawText(s.channelText, font, channelIconPosX + s.bmpIcon.Size.Width + ChannelIconTextSpacing, channelIconPosY + textOffsetY, theme.LightGreyFillBrush1); 

                        }

                        // Grid lines
                        for (int i = 1; i < numRows; i++)
                            videoGraphics.DrawLine(0, i * channelResY, videoResX, i * channelResY, theme.BlackBrush, channelLineWidth); 
                        for (int i = 1; i < numColumns; i++)
                            videoGraphics.DrawLine(i * channelResX, 0, i * channelResX, videoResY, theme.BlackBrush, channelLineWidth);

                        // Watermark.
                        videoGraphics.DrawBitmap(bmpWatermark, videoResX - bmpWatermark.Size.Width, videoResY - bmpWatermark.Size.Height);
                        videoGraphics.EndDraw();

                        // Readback + send to ffmpeg.
                        videoGraphics.GetBitmap(videoImage);
                        stream.Write(videoImage);
                    }
                }

                process.WaitForExit();
                process.Dispose();
                process = null;

                File.Delete(tempAudioFile);
            }
#if !DEBUG
            catch (Exception e)
            {
                Log.LogMessage(LogSeverity.Error, "Error exporting video.");
                Log.LogMessage(LogSeverity.Error, e.Message);
            }
            finally
#endif
            {
#if FAMISTUDIO_WINDOWS
                // Cant raise the process priority without being admin on Linux/MacOS.
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
#endif

                foreach (var c in channelStates)
                    c.bmpIcon.Dispose();

                theme.Terminate();
                bmpWatermark.Dispose();
                gradientBrush.Dispose();
                videoGraphics.Dispose();
            }

            return true;
        }
    }

    static class OscilloscopeColorType
    {
        public const int None = 0;
        public const int Instruments = 1;
        public const int InstrumentsAndSamples = 2;

        public static readonly string[] Names =
        {
            "None",
            "Instruments",
            "Instruments and Samples"
        };

        public static int GetIndexForName(string str)
        {
            return Array.IndexOf(Names, str);
        }
    }
}
