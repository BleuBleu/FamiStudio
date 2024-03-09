using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace FamiStudio
{
    class VideoFileBase
    {
        protected const int TextMargin = 4;
        protected const int SampleRate = 44100;
        protected const int ChannelIconTextSpacing = 8;

        protected int videoResX = 1920;
        protected int videoResY = 1080;

        protected bool halfFrameRate;
        protected bool showRegisters;
        protected string tempAudioFile;

        protected int registerPosY = 0;
        protected int oscFrameWindowSize;
        protected int oscRenderWindowSize;

        protected Project project;
        protected Song song;
        protected OffscreenGraphics videoGraphics;
        protected OffscreenGraphics downsampleGraphics;
        protected IVideoEncoder videoEncoder;
        protected VideoChannelState[] channelStates;
        protected VideoFrameMetadata[] metadata;
        protected Fonts fonts;
        protected Texture watermark;
        protected NesApu.NesRegisterValues registerValues;
        protected List<RegisterViewer> registerViewers;
        protected List<Texture> registerViewerIcons;
        protected Color[] registerColors = new Color[11];
        protected List<string> authorText = new List<string>();

        // TODO : This is is very similar to Oscilloscope.cs, unify eventually...
        protected float[] UpdateOscilloscope(VideoChannelState state, int frameIndex)
        {
            var meta = metadata[frameIndex];
            var newTrigger = meta.channelData[state.songChannelIndex].trigger;

            if (!state.useEmuTriggers)
                newTrigger = NesApu.TRIGGER_NONE;

            // TRIGGER_NONE (-2) means the emulation isnt able to provide a trigger, 
            // we must fallback on analysing the waveform to detect one.
            if (newTrigger == NesApu.TRIGGER_NONE)
            {
                newTrigger = state.triggerFunction.Detect(meta.wavOffset, oscFrameWindowSize);

                // Ugly fallback.
                if (newTrigger < 0)
                    newTrigger = meta.wavOffset;

                state.holdFrameCount = 0;
            }
            else if (newTrigger >= 0)
            {
                newTrigger = meta.wavOffset + newTrigger;
                state.holdFrameCount = 0;
            }
            else
            {
                // We can also get TRIGGER_HOLD (-1) here, which mean we do nothing and 
                // hope for a new trigger "soon". This will happen on very low freqency
                // notes where the period is longer than 1 frame.
                state.holdFrameCount++;
            }

            // If we hit this, it means that the emulation code told us a trigger
            // was eventually coming, but is evidently not. The longest periods we
            // have at the moment are very low EPSM notes with periods about 8 frames.
            Debug.Assert(state.holdFrameCount < 10);

            var vertices = new float[oscRenderWindowSize * 2];

            #if false
                // For debugging oscilloscope placement
                for (int i = 0; i < vertices.Length / 2; i++)
                {
                    vertices[i * 2 + 0] = i / (float)(oscRenderWindowSize - 1);
                    vertices[i * 2 + 1] = (i & 1) != 0 ? 1 : -1;
                }
            #else
                var startIdx = newTrigger >= 0 ? newTrigger : state.lastTrigger;

                for (int i = 0, j = startIdx - oscRenderWindowSize / 2; i < oscRenderWindowSize; i++, j++)
                {
                    var samp = j < 0 || j >= state.wav.Length ? 0 : state.wav[j];

                    vertices[i * 2 + 0] = i / (float)(oscRenderWindowSize - 1);
                    vertices[i * 2 + 1] = Utils.Clamp(samp / 32768.0f * state.oscScale, -1.0f, 1.0f);
                }
            #endif

            if (newTrigger >= 0)
                state.lastTrigger = newTrigger;

            return vertices;
        }

        private void BuildChannelColors(Song song, VideoChannelState[] channels, VideoFrameMetadata[] meta, int colorMode)
        {
            Color[,] colors = new Color[meta.Length, meta[0].channelData.Length];

            // Get the note colors.
            for (int i = 0; i < meta.Length; i++)
            {
                var m = meta[i];

                for (int j = 0; j < channels.Length; j++)
                {
                    var note = m.channelData[channels[j].songChannelIndex].note;

                    if (note != null && note.IsMusical)
                    {
                        var color = Theme.LightGreyColor1;

                        if (colorMode == OscilloscopeColorType.Channel)
                        {
                            var channel = song.Channels[channels[j].songChannelIndex];
                            for (int p = 0; p < channel.PatternInstances.Length; p++)
                            {
                                if (channel.PatternInstances[p] != null)
                                {
                                    color = channel.PatternInstances[p].Color;
                                    break;
                                }
                            }
                        }
                        else if (colorMode != OscilloscopeColorType.None)
                        {
                            if (note.Instrument != null && colorMode == OscilloscopeColorType.Instruments)
                            {
                                color = note.Instrument.Color;
                            }
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
                        colors[i, j] = Theme.LightGreyColor1;
                    }
                }
            }

            const int ColorBlendTime = 5;

            // Blend the colors.
            for (int i = 0; i < meta.Length; i++)
            {
                var m = meta[i];

                for (int j = 0; j < m.channelData.Length; j++)
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

                    m.channelData[j].color = Color.FromArgb(avgR / count, avgG / count, avgB / count);
                }
            }
        }

        protected bool InitializeEncoder(VideoExportSettings settings)
        {
            if (settings.ChannelMask == 0 || settings.LoopCount < 1)
                return false;

            Log.LogMessage(LogSeverity.Info, "Detecting FFmpeg...");

            videoEncoder = settings.Encoder;
            videoResX = settings.ResX;
            videoResY = settings.ResY;
            halfFrameRate = settings.HalfFrameRate;
            showRegisters = settings.ShowRegisters;
            project = settings.Project.DeepClone();
            song = project.GetSong(settings.SongId);
            song.ExtendForLooping(settings.LoopCount);

            var downsampleResX = videoResX / settings.Downsample;
            var downsampleResY = videoResY / settings.Downsample;
            var downsampled = settings.Downsample > 1;

            // Save audio to temporary file.
            tempAudioFile = Path.Combine(Utils.GetTemporaryDiretory(), "temp.wav");
            AudioExportUtils.Save(song, tempAudioFile, SampleRate, 1, -1, -1, false, false, settings.Stereo, settings.ChannelPan, settings.AudioDelay, true, (samples, samplesChannels, fn) => { WaveFile.Save(samples, fn, SampleRate, samplesChannels); });

            if (Log.ShouldAbortOperation)
                return false;

            // Start encoder, must be done before any GL calls on Android.
            GetFrameRateInfo(song.Project, halfFrameRate, out var frameRateNumer, out var frameRateDenom);

            if (!videoEncoder.BeginEncoding(downsampleResX, downsampleResY, frameRateNumer, frameRateDenom, settings.VideoBitRate, settings.AudioBitRate, settings.Stereo, tempAudioFile, settings.Filename))
            {
                Log.LogMessage(LogSeverity.Error, "Error starting video encoder, aborting.");
                return false;
            }

            // Create the channel states.
            channelStates = new VideoChannelState[Utils.NumberOfSetBits(settings.ChannelMask)];

            for (int i = 0, channelIndex = 0; i < song.Channels.Length; i++)
            {
                if ((settings.ChannelMask & (1L << i)) == 0)
                    continue;

                var channel = song.Channels[i];
                var pattern = channel.PatternInstances[0];
                var state = new VideoChannelState();

                state.videoChannelIndex = channelIndex;
                state.songChannelIndex = i;
                state.channel = song.Channels[i];
                state.channelText = state.channel.NameWithExpansion;
                state.useEmuTriggers = settings.EmuTriggers == null || settings.EmuTriggers[i];

                channelStates[channelIndex] = state;
                channelIndex++;
            }

            // Spawn threads to generate the WAV data for the oscilloscopes.
            Log.LogMessage(LogSeverity.Info, "Building channel oscilloscopes...");

            var counter = new ThreadSafeCounter();
            var maxAbsSamples = new int[channelStates.Length];

            Utils.NonBlockingParallelFor(channelStates.Length, NesApu.NUM_WAV_EXPORT_APU, counter, (stateIndex, threadIndex) =>
            {
                var state = channelStates[stateIndex];
                state.wav = new WavPlayer(SampleRate, song.Project.PalMode, song.Project.OutputsStereoAudio, 1, 1L << state.songChannelIndex, threadIndex).GetSongSamples(song, -1, false, true);
                state.triggerFunction = new PeakSpeedTrigger(state.wav, false);

                if (Log.ShouldAbortOperation)
                    return false;

                if (song.Project.OutputsStereoAudio)
                    state.wav = WaveUtils.MixDown(state.wav);

                maxAbsSamples[stateIndex] = WaveUtils.GetMaxAbsValue(state.wav);

                GC.Collect();

                return true;
            });

            while (counter.Value != channelStates.Length)
            {
                Log.ReportProgress(counter.Value / (float)channelStates.Length);
                Thread.Sleep(10);

                if (Log.ShouldAbortOperation)
                    return false;
            }

            var globalMaxAbsSample = maxAbsSamples.Max();

            // Apply a square root to keep other channels proportional, but still decent size.
            for (int i = 0; i < channelStates.Length; i++)
                channelStates[i].oscScale = maxAbsSamples[i] == 0 ? 1.0f : (float)MathF.Sqrt(globalMaxAbsSample / (float)maxAbsSamples[i]) * (32768.0f / globalMaxAbsSample);

            // HACK : The scaling is not longer tied to the graphics, so we need to temporarely override it.
            DpiScaling.ForceUnitScaling = true;
            Platform.AcquireGLContext();

            // Create graphics resources.
            videoGraphics = OffscreenGraphics.Create(videoResX, videoResY, !downsampled, settings.PreviewMode || downsampled);

            if (videoGraphics == null)
            {
                Log.LogMessage(LogSeverity.Error, "Error initializing off-screen graphics, aborting.");
                DpiScaling.ForceUnitScaling = false;
                return false;
            }

            if (settings.Downsample > 1)
            {
                downsampleGraphics = OffscreenGraphics.Create(downsampleResX, downsampleResY, true, false);

                if (downsampleGraphics == null)
                {
                    Log.LogMessage(LogSeverity.Error, "Error initializing off-screen graphics, aborting.");
                    DpiScaling.ForceUnitScaling = false;
                    return false;
                }
            }

            fonts = new Fonts(videoGraphics);
            watermark = videoGraphics.CreateTextureFromResource("FamiStudio.Resources.Misc.VideoWatermark");

            if (showRegisters)
            {
                registerValues = new NesApu.NesRegisterValues();
                registerViewers = new List<RegisterViewer>();
                registerViewerIcons = new List<Texture>();

                foreach (var exp in project.GetActiveExpansions())
                {
                    registerViewers.Add(RegisterViewer.CreateForExpansion(exp, registerValues));
                    registerViewerIcons.Add(videoGraphics.CreateTextureFromResource($"FamiStudio.Resources.Atlas.{ExpansionType.Icons[exp]}"));
                }

                var color0 = Theme.LightGreyColor2; // Grey
                var color1 = Theme.CustomColors[14, 5]; // Orange
                var color2 = Theme.CustomColors[0, 5]; // Red

                for (int i = 0; i < registerColors.Length; i++)
                {
                    var alpha = i / (float)(registerColors.Length - 1);
                    var color = Color.FromArgb(
                        (int)Utils.Lerp(color2.R, color0.R, alpha),
                        (int)Utils.Lerp(color2.G, color0.G, alpha),
                        (int)Utils.Lerp(color2.B, color0.B, alpha));
                    registerColors[i] = color;
                }
            }

            // Generate metadata
            Log.LogMessage(LogSeverity.Info, "Generating video metadata...");
            metadata = new VideoMetadataPlayer(SampleRate, song.Project.PalMode, song.Project.OutputsStereoAudio, showRegisters, 1).GetVideoMetadata(song, -1);

            oscFrameWindowSize  = (int)(SampleRate / (song.Project.PalMode ? NesApu.FpsPAL : NesApu.FpsNTSC));
            oscRenderWindowSize = (int)(oscFrameWindowSize * settings.OscWindow);

            BuildChannelColors(song, channelStates, metadata, settings.OscColorMode);

            if (!string.IsNullOrEmpty(project.Name))
            {
                if (!string.IsNullOrEmpty(song.Name))
                    authorText.Add($"{project.Name} - {song.Name}");
                else
                    authorText.Add(project.Name);
            }

            if (!string.IsNullOrEmpty(project.Author))
            {
                if (!string.IsNullOrEmpty(project.Copyright))
                    authorText.Add($"{project.Author} (c) {project.Copyright}");
                else
                    authorText.Add(project.Author);
            }

            return true;
        }

        protected void DrawRegisterValues(VideoFrameMetadata frame)
        {
            if (showRegisters)
            {
                frame.registerValues.CopyTo(registerValues);

                var c = videoGraphics.OverlayCommandList;
                var maxWidth = 0;
                var byteWidth = (int)c.Graphics.MeasureString("$00 ", fonts.FontSmall, true);

                for (int i = 0; i < registerViewers.Count; i++)
                {
                    var regViewer = registerViewers[i];

                    foreach (var row in regViewer.RegisterRows)
                    {
                        if (row.GetValue == null && row.CustomDraw == null)
                        {
                            var numBytes = (row.AddEnd - row.AddStart + 1) * (row.SubEnd - row.SubStart + 1);
                            var rowWidth = numBytes * byteWidth;
                            maxWidth = Math.Max(maxWidth, rowWidth);
                        }
                    }
                }

                var y = 0;
                var buttonTextNoIconPosX = 0;
                var registerLabelSizeX = 50;
                var contentSizeX = maxWidth + registerLabelSizeX;

                c.PushTranslation(videoResX - contentSizeX, registerPosY);

                for (int i = 0; i < registerViewers.Count; i++)
                {
                    var regViewer = registerViewers[i];
                    var icon = registerViewerIcons[i];

                    c.DrawTexture(icon, 0, y, 1, Theme.LightGreyColor2);
                    c.DrawText(ExpansionType.GetLocalizedName(regViewer.Expansion, ExpansionType.LocalizationMode.ChipName), fonts.FontSmallBold, icon.Size.Width + 2, y, Theme.LightGreyColor2, TextFlags.Middle, 0, icon.Size.Height);
                    y += icon.Size.Height;
                    c.DrawLine(0, y, contentSizeX - 4, y, Theme.LightGreyColor2);
                    y += 2;

                    foreach (var row in regViewer.RegisterRows)
                    {
                        var regSizeY = row.CustomHeight > 0 ? row.CustomHeight : 10;

                        c.PushTranslation(0, y);

                        if (row.CustomDraw != null)
                        {
                            var label = row.Label;
                            c.DrawText(label, fonts.FontSmall, buttonTextNoIconPosX, 0, Theme.LightGreyColor2, TextFlags.Middle | TextFlags.DropShadow, 0, regSizeY);

                            c.PushTranslation(registerLabelSizeX + 1, 0);
                            row.CustomDraw(c, fonts, new Rectangle(0, 0, contentSizeX - registerLabelSizeX - 1, regSizeY), true);
                            c.PopTransform();
                        }
                        else if (row.GetValue != null)
                        {
                            var label = row.Label;
                            var value = row.GetValue().ToString();
                            var flags = TextFlags.Middle | TextFlags.DropShadow | (row.Monospace ? TextFlags.Monospace : TextFlags.None);

                            c.DrawText(label, fonts.FontSmall, buttonTextNoIconPosX, 0, Theme.LightGreyColor2, TextFlags.Middle | TextFlags.Monospace, 0, regSizeY);
                            c.DrawText(value, fonts.FontSmall, buttonTextNoIconPosX + registerLabelSizeX, 0, Theme.LightGreyColor2, flags, 0, regSizeY);
                        }
                        else
                        {
                            c.DrawText(row.Label, fonts.FontSmall, buttonTextNoIconPosX, 0, Theme.LightGreyColor2, TextFlags.Middle | TextFlags.DropShadow | TextFlags.Monospace, 0, regSizeY);

                            var flags = TextFlags.Monospace | TextFlags.Middle | TextFlags.DropShadow;
                            var x = buttonTextNoIconPosX + registerLabelSizeX;

                            for (var r = row.AddStart; r <= row.AddEnd; r++)
                            {
                                for (var s = row.SubStart; s <= row.SubEnd; s++)
                                {
                                    var val = registerValues.GetRegisterValue(regViewer.Expansion, r, out var age, s);
                                    var str = $"${val:X2} ";
                                    var color = registerColors[Math.Min(age, registerColors.Length - 1)];

                                    c.DrawText(str, fonts.FontSmall, x, 0, color, flags, 0, regSizeY);
                                    x += (int)c.Graphics.MeasureString(str, fonts.FontSmall, true);
                                }
                            }
                        }

                        c.PopTransform();
                        y += regSizeY;
                    }

                    y += 4;
                }

                c.PopTransform();
            }
        }

        protected bool LaunchEncoderLoop(Action<int> body, Action cleanup = null)
        {
            var success = true;
            var lastTime = DateTime.Now;

#if !DEBUG
            try
#endif
            {
                // Generate each of the video frames.
                for (int f = 0; f < metadata.Length; f++)
                {
                    if (Log.ShouldAbortOperation)
                    {
                        success = false;
                        break;
                    }

                    if ((f % 100) == 0)
                        Log.LogMessage(LogSeverity.Info, $"Rendering frame {f} / {metadata.Length}{GetTimeLeftString(ref lastTime, f, metadata.Length, 100)}");

                    Log.ReportProgress(f / (float)(metadata.Length - 1));

                    if (halfFrameRate && (f & 1) != 0)
                        continue;

                    var frame = metadata[f];

                    // HACK : It was a terrible idea to make the DPI scaling a global thing. It should have remained on the 
                    // Graphics object like before. Need to keep switching to unit scaling back/forth.
                    DpiScaling.ForceUnitScaling = true;

                    body(f);

                    // Registers + watermark + artist.
                    DrawRegisterValues(frame);
                    videoGraphics.OverlayCommandList.DrawTexture(watermark, videoResX - watermark.Size.Width, videoResY - watermark.Size.Height);
                    
                    var textY = videoResY - authorText.Count * fonts.FontMediumBold.LineHeight - TextMargin;
                    for (var i = 0; i < authorText.Count; i++, textY += fonts.FontMediumBold.LineHeight)
                    {
                        // Ghetto drop shadow...
                        var shadowColor = new Color(0, 0, 0, 96);
                        videoGraphics.OverlayCommandList.DrawText(authorText[i], fonts.FontMediumBold, TextMargin - 1, textY - 1, shadowColor);
                        videoGraphics.OverlayCommandList.DrawText(authorText[i], fonts.FontMediumBold, TextMargin - 1, textY + 1, shadowColor);
                        videoGraphics.OverlayCommandList.DrawText(authorText[i], fonts.FontMediumBold, TextMargin + 1, textY - 1, shadowColor);
                        videoGraphics.OverlayCommandList.DrawText(authorText[i], fonts.FontMediumBold, TextMargin + 1, textY + 1, shadowColor);
                        videoGraphics.OverlayCommandList.DrawText(authorText[i], fonts.FontMediumBold, TextMargin, textY, Theme.LightGreyColor1);
                    }

                    videoGraphics.EndDrawFrame();

                    if (downsampleGraphics != null)
                    {
                        downsampleGraphics.BeginDrawFrame(new Rectangle(0, 0, downsampleGraphics.SizeX, downsampleGraphics.SizeY), true, Theme.BlackColor);
                        downsampleGraphics.DefaultCommandList.DrawTexture(videoGraphics.GetTexture(), 0, 0, downsampleGraphics.SizeX, downsampleGraphics.SizeY, 1.0f);
                        downsampleGraphics.EndDrawFrame(false);
                    }

                    DpiScaling.ForceUnitScaling = false;

                    // Send to encoder.
                    if (!videoEncoder.AddFrame(downsampleGraphics != null ? downsampleGraphics : videoGraphics))
                        break;
                }

                videoEncoder.EndEncoding(!success);
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
                Utils.DisposeAndNullify(ref fonts);
                Utils.DisposeAndNullify(ref watermark);
                Utils.DisposeAndNullify(ref videoGraphics);
                Utils.DisposeAndNullify(ref downsampleGraphics);
                Array.ForEach(channelStates, c => c.icon.Dispose());
                registerViewerIcons?.ForEach(i => i.Dispose());
                File.Delete(tempAudioFile);
                cleanup?.Invoke();
                channelStates = null;
                metadata = null;
                DpiScaling.ForceUnitScaling = false;
            }

            GC.Collect();

            return success;
        }

        protected void LoadChannelIcons(bool large)
        {
            var suffix = large ? "@2x" : "";

            foreach (var s in channelStates)
                s.icon = videoGraphics.CreateTextureFromResource($"FamiStudio.Resources.Atlas.{ChannelType.Icons[s.channel.Type]}{suffix}");
        }

        protected void GetFrameRateInfo(Project project, bool half, out int numer, out int denom)
        {
            numer = project.PalMode ? 5000773 : 6009883;
            if (half)
                numer /= 2;
            denom = 100000;
        }

        protected string GetTimeLeftString(ref DateTime lastTime, int numFramesRendered, int numFramesTotal, int batchCount)
        {
            var currTime = DateTime.Now;
            var str = "";

            if (numFramesRendered > 0)
            {
                var fps = batchCount / (currTime - lastTime).TotalSeconds;
                var timeLeft = (int)Math.Round((numFramesTotal - numFramesRendered) / fps);

                // We dont have the room to display FPS on Mobile.
                if (Platform.IsDesktop)
                    str = $" ({fps:0.0} FPS, {timeLeft} sec left)";
                else
                    str = $" ({timeLeft} sec left)";
            }

            lastTime = currTime;
            return str;
        }
    }

    class VideoChannelState
    {
        public int videoChannelIndex;
        public int songChannelIndex;
        public string channelText;
        public Channel channel;
        public Texture icon;
        public short[] wav;
        public float oscScale;
        public int lastTrigger;
        public int holdFrameCount;
        public bool useEmuTriggers;
        public OscilloscopeTrigger triggerFunction;
    };

    class VideoFrameMetadata
    {
        public class ChannelMetadata
        {
            public Note  note;
            public int   volume;
            public int   trigger;
            public float scroll; // Only used by piano roll.
            public Color color;
        };

        public int   playPattern;
        public float playNote;
        public int   wavOffset;
        public ChannelMetadata[] channelData;
        public NesApu.NesRegisterValues registerValues;
    };

    class VideoMetadataPlayer : BasePlayer
    {
        int numSamples = 0;
        int prevNumSamples = 0;
        bool readRegisters;
        List<VideoFrameMetadata> metadata;

        public VideoMetadataPlayer(int sampleRate, bool pal, bool stereo, bool registers, int maxLoop) : base(NesApu.APU_WAV_EXPORT, pal, stereo, sampleRate)
        {
            maxLoopCount = maxLoop;
            metadata = new List<VideoFrameMetadata>();
            readRegisters = registers;
        }

        private void WriteMetadata(List<VideoFrameMetadata> metadata)
        {
            var meta = new VideoFrameMetadata();

            meta.playPattern     = playLocation.PatternIndex;
            meta.playNote        = playLocation.NoteIndex;
            meta.wavOffset       = prevNumSamples;
            meta.channelData     = new VideoFrameMetadata.ChannelMetadata[song.Channels.Length];

            for (int i = 0; i < channelStates.Length; i++)
            {
                meta.channelData[i] = new VideoFrameMetadata.ChannelMetadata();
                meta.channelData[i].note    = channelStates[i].CurrentNote;
                meta.channelData[i].volume  = channelStates[i].CurrentVolume;
                meta.channelData[i].trigger = GetOscilloscopeTrigger(channelStates[i].InnerChannelType);
            }

            if (readRegisters)
            {
                meta.registerValues = new NesApu.NesRegisterValues();
                GetRegisterValues(meta.registerValues);
            }

            metadata.Add(meta);

            prevNumSamples = numSamples;
        }

        public VideoFrameMetadata[] GetVideoMetadata(Song song, int duration)
        {
            int maxSample = int.MaxValue;

            if (duration > 0)
                maxSample = duration * sampleRate;

            BeginPlaySong(song);

            while (PlaySongFrame() && numSamples < maxSample)
            {
                WriteMetadata(metadata);
                Log.ReportProgress(0.0f);
            }

            return metadata.ToArray();
        }

        protected override short[] EndFrame()
        {
            numSamples += base.EndFrame().Length / (stereo ? 2 : 1);
            return null;
        }
    }

    static class VideoMode
    {
        public const int Oscilloscope = 0;
        public const int PianoRollSeparateChannels = 1;
        public const int PianoRollUnified = 2;
        public const int Count = 3;

        public static LocalizedString[] LocalizedNames = new LocalizedString[3];

        static VideoMode()
        {
            Localization.LocalizeStatic(typeof(VideoMode));
        }

        public static int GetIndexForName(string str)
        {
            return Array.FindIndex(LocalizedNames, n => n.Value == str);
        }
    }

    static class VideoResolution
    {
        public static LocalizedString[] LocalizedNames = new LocalizedString[6];

        public static readonly int[] ResolutionY =
        {
            1080,
            720,
            480,
            1920,
            1280,
            854
        };

        public static readonly int[] ResolutionX =
        {
            1920,
            1280,
            854,
            1080,
            720,
            480,
        };

        static VideoResolution()
        {
            Localization.LocalizeStatic(typeof(VideoResolution));
        }

        public static int GetIndexForName(string str)
        {
            return Array.FindIndex(LocalizedNames, n => n.Value == str);
        }
    }

    class VideoExportSettings
    {
        public Project Project;
        public int SongId;
        public int VideoMode;
        public int LoopCount;
        public int OscWindow;
        public string Filename;
        public int ResX;
        public int ResY;
        public int Downsample;
        public bool HalfFrameRate;
        public long ChannelMask;
        public int AudioDelay;
        public int AudioBitRate;
        public int VideoBitRate;
        public int OscColorMode;
        public int OscNumColumns;
        public int OscLineThickness;
        public float PianoRollNoteWidth;
        public float PianoRollZoom;
        public float PianoRollPerspective;
        public int PianoRollNumRows;
        public bool ShowRegisters;
        public bool Stereo;
        public float[] ChannelPan;
        public int[] ChannelTranspose;
        public bool[] EmuTriggers;
        public bool PreviewMode;
        public IVideoEncoder Encoder;
    }
}
