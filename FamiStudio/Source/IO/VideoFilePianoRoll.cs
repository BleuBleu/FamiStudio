using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

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
    class VideoFilePianoRoll : VideoFileBase
    {
        const int ChannelIconPosY = 26;
        const int SegmentTransitionNumFrames = 16;
        const int ThinNoteThreshold = 288;
        const int VeryThinNoteThreshold = 192;

        private void ComputeChannelsScroll(VideoFrameMetadata[] frames, int channelMask, int numVisibleNotes)
        {
            var numFrames = frames.Length;
            var numChannels = frames[0].channelNotes.Length;

            for (int c = 0; c < numChannels; c++)
            {
                if ((channelMask & (1 << c)) == 0)
                    continue;

                // Go through all the frames and split them in segments. 
                // A segment is a section of the song where all the notes fit in the view.
                var segments = new List<ScrollSegment>();
                var currentSegment = (ScrollSegment)null;
                var minOverallNote = int.MaxValue;
                var maxOverallNote = int.MinValue;

                for (int f = 0; f < numFrames; f++)
                {
                    var frame = frames[f];
                    var note  = frame.channelNotes[c];

                    if (frame.scroll == null)
                        frame.scroll = new float[numChannels];

                    if (note.IsMusical)
                    {
                        if (currentSegment == null)
                        {
                            currentSegment = new ScrollSegment();
                            segments.Add(currentSegment);
                        }

                        // If its the start of a new pattern and we've been not moving for ~10 sec, let's start a new segment.
                        bool forceNewSegment = frame.playNote == 0 && (f - currentSegment.startFrame) > 600;

                        var minNoteValue = note.Value - 1;
                        var maxNoteValue = note.Value + 1;

                        // Only consider slides if they arent too large.
                        if (note.IsSlideNote && Math.Abs(note.SlideNoteTarget - note.Value) < numVisibleNotes / 2)
                        {
                            minNoteValue = Math.Min(note.Value, note.SlideNoteTarget) - 1;
                            maxNoteValue = Math.Max(note.Value, note.SlideNoteTarget) + 1;
                        }

                        // Only consider arpeggios if they are not too big.
                        if (note.IsArpeggio && note.Arpeggio.GetChordMinMaxOffset(out var minArp, out var maxArp) && maxArp - minArp < numVisibleNotes / 2)
                        {
                            minNoteValue = note.Value + minArp;
                            maxNoteValue = note.Value + maxArp;
                        }

                        minOverallNote = Math.Min(minOverallNote, minNoteValue);
                        maxOverallNote = Math.Max(maxOverallNote, maxNoteValue);

                        var newMinNote = Math.Min(currentSegment.minNote, minNoteValue);
                        var newMaxNote = Math.Max(currentSegment.maxNote, maxNoteValue);

                        // If we cant fit the next note in the view, start a new segment.
                        if (forceNewSegment || newMaxNote - newMinNote + 1 > numVisibleNotes)
                        {
                            currentSegment.endFrame = f;
                            currentSegment = new ScrollSegment();
                            currentSegment.startFrame = f;
                            segments.Add(currentSegment);

                            currentSegment.minNote = minNoteValue;
                            currentSegment.maxNote = maxNoteValue;
                        }
                        else
                        {
                            currentSegment.minNote = newMinNote;
                            currentSegment.maxNote = newMaxNote;
                        }
                    }
                }

                // Not a single notes in this channel...
                if (currentSegment == null)
                {
                    currentSegment = new ScrollSegment();
                    currentSegment.minNote = Note.FromFriendlyName("C4");
                    currentSegment.maxNote = currentSegment.minNote;
                    segments.Add(currentSegment);
                }

                currentSegment.endFrame = numFrames;

                // Remove very small segments, these make the camera move too fast, looks bad.
                var shortestAllowedSegment = SegmentTransitionNumFrames * 2;

                bool removed = false;
                do
                {
                    var sortedSegment = new List<ScrollSegment>(segments);

                    sortedSegment.Sort((s1, s2) => s1.NumFrames.CompareTo(s2.NumFrames));

                    if (sortedSegment[0].NumFrames >= shortestAllowedSegment)
                        break;

                    for (int s = 0; s < sortedSegment.Count; s++)
                    {
                        var seg = sortedSegment[s];

                        if (seg.NumFrames >= shortestAllowedSegment)
                            break;

                        var thisSegmentIndex = segments.IndexOf(seg);

                        // Segment is too short, see if we can merge with previous/next one.
                        var mergeSegmentIndex  = -1;
                        var mergeSegmentLength = -1;
                        if (thisSegmentIndex > 0)
                        {
                            mergeSegmentIndex  = thisSegmentIndex - 1;
                            mergeSegmentLength = segments[thisSegmentIndex - 1].NumFrames;
                        }
                        if (thisSegmentIndex != segments.Count - 1 && segments[thisSegmentIndex + 1].NumFrames > mergeSegmentLength)
                        {
                            mergeSegmentIndex = thisSegmentIndex + 1;
                            mergeSegmentLength = segments[thisSegmentIndex + 1].NumFrames;
                        }
                        if (mergeSegmentIndex >= 0)
                        {
                            // Merge.
                            var mergeSeg = segments[mergeSegmentIndex];
                            mergeSeg.startFrame = Math.Min(mergeSeg.startFrame, seg.startFrame);
                            mergeSeg.endFrame   = Math.Max(mergeSeg.endFrame, seg.endFrame);
                            segments.RemoveAt(thisSegmentIndex);
                            removed = true;
                            break;
                        }
                    }
                }
                while (removed);

                // Build the actually scrolling data. 
                var minScroll = (float)Math.Ceiling(Note.MusicalNoteMin + numVisibleNotes * 0.5f);
                var maxScroll = (float)Math.Floor  (Note.MusicalNoteMax - numVisibleNotes * 0.5f);

                Debug.Assert(maxScroll >= minScroll);

                foreach (var segment in segments)
                {
                    segment.scroll = Utils.Clamp(segment.minNote + (segment.maxNote - segment.minNote) * 0.5f, minScroll, maxScroll);
                }

                for (var s = 0; s < segments.Count; s++)
                {
                    var segment0 = segments[s + 0];
                    var segment1 = s == segments.Count - 1 ? null : segments[s + 1];

                    for (int f = segment0.startFrame; f < segment0.endFrame - (segment1 == null ? 0 : SegmentTransitionNumFrames); f++)
                    {
                        frames[f].scroll[c] = segment0.scroll;
                    }

                    if (segment1 != null)
                    {
                        // Smooth transition to next segment.
                        for (int f = segment0.endFrame - SegmentTransitionNumFrames, a = 0; f < segment0.endFrame; f++, a++)
                        {
                            var lerp = a / (float)SegmentTransitionNumFrames;
                            frames[f].scroll[c] = Utils.Lerp(segment0.scroll, segment1.scroll, Utils.SmootherStep(lerp));
                        }
                    }
                }
            }
        }

        private void SmoothFamitrackerScrolling(VideoFrameMetadata[] frames)
        {
            var numFrames = frames.Length;

            for (int f = 0; f < numFrames; )
            {
                var thisFrame = frames[f];

                var currentPlayPattern = thisFrame.playPattern;
                var currentPlayNote    = thisFrame.playNote;

                // Keep moving forward until we see that we have advanced by 1 row.
                int nf = f + 1;
                for (; nf < numFrames; nf++)
                {
                    var nextFrame = frames[nf];
                    if (nextFrame.playPattern != thisFrame.playPattern ||
                        nextFrame.playNote    != thisFrame.playNote)
                    {
                        break;
                    }
                }

                var numFramesSameNote = nf - f;

                // Smooth out movement linearly.
                for (int i = 0; i < numFramesSameNote; i++)
                {
                    frames[f + i].playNote += i / (float)numFramesSameNote;
                }

                f = nf;
            }
        }

        private void SmoothFamiStudioScrolling(VideoFrameMetadata[] frames, Song song)
        {
            var patternIndices = new int[frames.Length];
            var absoluteNoteIndices = new float[frames.Length];

            // Keep copy of original pattern/notes.
            for (int i = 0; i < frames.Length; i++)
            {
                patternIndices[i] = frames[i].playPattern;
                absoluteNoteIndices[i] = song.GetPatternStartAbsoluteNoteIndex(frames[i].playPattern, (int)frames[i].playNote);
            }

            // Do moving average to smooth the movement.
            for (int i = 0; i < frames.Length; i++)
            {
                var averageSize = (Utils.Max(song.GetPatternGroove(patternIndices[i])) + 1) / 2;

                averageSize = Math.Min(averageSize, i);
                averageSize = Math.Min(averageSize, absoluteNoteIndices.Length - i - 1);

                var sum = 0.0f;
                var cnt = 0;
                for (int j = i - averageSize; j <= i + averageSize; j++)
                {
                    if (j >= 0 && j < absoluteNoteIndices.Length)
                    {
                        sum += absoluteNoteIndices[j];
                        cnt++;
                    }
                }
                sum /= cnt;

                frames[i].playPattern = song.PatternIndexFromAbsoluteNoteIndex((int)sum);
                frames[i].playNote = sum - song.GetPatternStartAbsoluteNoteIndex(frames[i].playPattern);
            }
        }

        // Not tested in a while, probably wrong channel order.
        private unsafe void DumpDebugImage(byte[] image, int sizeX, int sizeY, int frameIndex)
        {
#if FAMISTUDIO_LINUX || FAMISTUDIO_MACOS
            var pb = new Gdk.Pixbuf(image, true, 8, sizeX, sizeY, sizeX * 4);
            pb.Save($"/home/mat/Downloads/frame_{frameIndex:D4}.png", "png");
#else
            fixed (byte* vp = &image[0])
            {
                var b = new Bitmap(sizeX, sizeY, sizeX * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, new IntPtr(vp));
                b.Save($"d:\\dump\\pr\\frame_{frameIndex:D4}.png");
            }
#endif
        }

        public unsafe bool Save(Project originalProject, int songId, int loopCount, string ffmpegExecutable, string filename, int resX, int resY, bool halfFrameRate, int channelMask, int audioBitRate, int videoBitRate, int pianoRollZoom, bool stereo, float[] pan)
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
            var channelResXFloat = videoResX / (float)numChannels;
            var channelResX = videoResY;
            var channelResY = (int)channelResXFloat;
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
                state.channelText = state.channel.Name + (state.channel.IsExpansionChannel ? $" ({song.Project.ExpansionAudioShortName})" : "");
                state.wav = new WavPlayer(SampleRate, 1, 1 << i).GetSongSamples(song, song.Project.PalMode, -1);
                state.graphics = RenderGraphics.Create(channelResX, channelResY, false);
                state.bitmap = videoGraphics.CreateBitmapFromOffscreenGraphics(state.graphics);

                channelStates.Add(state);
                channelIndex++;

                // Find maximum absolute value to rescale the waveform.
                foreach (short s in state.wav)
                    maxAbsSample = Math.Max(maxAbsSample, Math.Abs(s));

                // Measure the longest text.
                longestChannelName = Math.Max(longestChannelName, state.graphics.MeasureString(state.channelText, ThemeBase.FontBigUnscaled));
            }

            // Tweak some cosmetic stuff that depends on resolution.
            var smallChannelText = longestChannelName + 32 + ChannelIconTextSpacing > channelResY * 0.8f;
            var bmpSuffix = smallChannelText ? "" : "@2x";
            var font = smallChannelText ? ThemeBase.FontMediumUnscaled : ThemeBase.FontBigUnscaled;
            var textOffsetY = smallChannelText ? 1 : 4;
            var pianoRollScaleX = Utils.Clamp(resY / 1080.0f, 0.6f, 0.9f);
            var pianoRollScaleY = channelResY < VeryThinNoteThreshold ? 0.5f : (channelResY < ThinNoteThreshold ? 0.667f : 1.0f);
            var channelLineWidth = channelResY < ThinNoteThreshold ? 3 : 5;
            var gradientSizeY = 256 * (videoResY / 1080.0f);
            var gradientBrush = videoGraphics.CreateVerticalGradientBrush(0, gradientSizeY, Color.Black, Color.FromArgb(0, Color.Black));

            foreach (var s in channelStates)
                s.bmpIcon = videoGraphics.CreateBitmapFromResource(ChannelType.Icons[s.channel.Type] + bmpSuffix);

            // Generate the metadata for the video so we know what's happening at every frame
            var metadata = new VideoMetadataPlayer(SampleRate, 1).GetVideoMetadata(song, song.Project.PalMode, -1);

            var oscScale = maxAbsSample != 0 ? short.MaxValue / (float)maxAbsSample : 1.0f;
            var oscLookback = (metadata[1].wavOffset - metadata[0].wavOffset) / 2;

#if FAMISTUDIO_LINUX || FAMISTUDIO_MACOS
            var dummyControl = new DummyGLControl();
            dummyControl.Move(0, 0, videoResX, videoResY);
#endif

            // Setup piano roll and images.
            var pianoRoll = new PianoRoll();
#if FAMISTUDIO_LINUX || FAMISTUDIO_MACOS
            pianoRoll.Move(0, 0, channelResX, channelResY);
#else
            pianoRoll.Width  = channelResX;
            pianoRoll.Height = channelResY;
#endif

            pianoRoll.StartVideoRecording(channelStates[0].graphics, song, pianoRollZoom, pianoRollScaleX, pianoRollScaleY, out var noteSizeY);

            // Build the scrolling data.
            var numVisibleNotes = (int)Math.Floor(channelResY / (float)noteSizeY);
            ComputeChannelsScroll(metadata, channelMask, numVisibleNotes);

            if (song.UsesFamiTrackerTempo)
                SmoothFamitrackerScrolling(metadata);
            else
                SmoothFamiStudioScrolling(metadata, song);

            var oscWindowSize = (int)Math.Round(SampleRate * OscilloscopeWindowSize);

            var videoImage   = new byte[videoResY * videoResX * 4];
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

                        // Render the piano rolls for each channels.
                        foreach (var s in channelStates)
                        {
                            var volume = frame.channelVolumes[s.songChannelIndex];
                            var note   = frame.channelNotes[s.songChannelIndex];

                            var color = Color.Transparent;

                            if (note.IsMusical)
                            {
                                if (s.channel.Type == ChannelType.Dpcm)
                                {
                                    var mapping = project.GetDPCMMapping(note.Value);
                                    if (mapping != null)
                                        color = mapping.Sample.Color;
                                }
                                else
                                {
                                    color = Color.FromArgb(128 + volume * 127 / 15, note.Instrument != null ? note.Instrument.Color : ThemeBase.LightGreyFillColor1);
                                }
                            }

#if FAMISTUDIO_LINUX || FAMISTUDIO_MACOS
                            s.graphics.BeginDraw(pianoRoll, channelResY);
#else
                            s.graphics.BeginDraw();
#endif
                            pianoRoll.RenderVideoFrame(s.graphics, Channel.ChannelTypeToIndex(s.channel.Type), frame.playPattern, frame.playNote, frame.scroll[s.songChannelIndex], note.Value, color);
                            s.graphics.EndDraw();
                        }

                        // Render the full screen overlay.
#if FAMISTUDIO_LINUX || FAMISTUDIO_MACOS
                        videoGraphics.BeginDraw(dummyControl, videoResY);
#else
                        videoGraphics.BeginDraw();
#endif
                        videoGraphics.Clear(Color.Black);

                        // Composite the channel renders.
                        foreach (var s in channelStates)
                        {
                            int channelPosX1 = (int)Math.Round((s.videoChannelIndex + 1) * channelResXFloat);
                            videoGraphics.DrawRotatedFlippedBitmap(s.bitmap, channelPosX1, videoResY, s.bitmap.Size.Width, s.bitmap.Size.Height);
                        }

                        // Gradient
                        videoGraphics.FillRectangle(0, 0, videoResX, gradientSizeY, gradientBrush);

                        // Channel names + oscilloscope
                        foreach (var s in channelStates)
                        {
                            int channelPosX0 = (int)Math.Round((s.videoChannelIndex + 0) * channelResXFloat);
                            int channelPosX1 = (int)Math.Round((s.videoChannelIndex + 1) * channelResXFloat);

                            var channelNameSizeX = videoGraphics.MeasureString(s.channelText, font);
                            var channelIconPosX = channelPosX0 + channelResY / 2 - (channelNameSizeX + s.bmpIcon.Size.Width + ChannelIconTextSpacing) / 2;

                            videoGraphics.FillRectangle(channelIconPosX, ChannelIconPosY, channelIconPosX + s.bmpIcon.Size.Width, ChannelIconPosY + s.bmpIcon.Size.Height, theme.DarkGreyLineBrush2);
                            videoGraphics.DrawBitmap(s.bmpIcon, channelIconPosX, ChannelIconPosY);
                            videoGraphics.DrawText(s.channelText, font, channelIconPosX + s.bmpIcon.Size.Width + ChannelIconTextSpacing, ChannelIconPosY + textOffsetY, theme.LightGreyFillBrush1);

                            if (s.videoChannelIndex > 0)
                                videoGraphics.DrawLine(channelPosX0, 0, channelPosX0, videoResY, theme.BlackBrush, channelLineWidth);

                            var oscMinY = (int)(ChannelIconPosY + s.bmpIcon.Size.Height + 10);
                            var oscMaxY = (int)(oscMinY + 100.0f * (resY / 1080.0f));

                            // Intentionally flipping min/max Y since D3D is upside down compared to how we display waves typically.
                            GenerateOscilloscope(s.wav, frame.wavOffset, oscWindowSize, oscLookback, oscScale, channelPosX0 + 10, oscMaxY, channelPosX1 - 10, oscMinY, oscilloscope);

                            var geo = videoGraphics.CreateGeometry(oscilloscope, false);

                            videoGraphics.AntiAliasing = true;
                            videoGraphics.DrawGeometry(geo, theme.LightGreyFillBrush1);
                            videoGraphics.AntiAliasing = false;
                            geo.Dispose();
                        }

                        // Watermark.
                        videoGraphics.DrawBitmap(bmpWatermark, videoResX - bmpWatermark.Size.Width, videoResY - bmpWatermark.Size.Height);
                        videoGraphics.EndDraw();

                        // Readback + send to ffmpeg.
                        videoGraphics.GetBitmap(videoImage);
                        stream.Write(videoImage);

                        // Dump debug images.
                        // DumpDebugImage(videoImage, videoResX, videoResY, f);
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

                pianoRoll.EndVideoRecording();
                foreach (var c in channelStates)
                {
                    c.bmpIcon.Dispose();
                    c.bitmap.Dispose();
                    c.graphics.Dispose();
                }
                theme.Terminate();
                bmpWatermark.Dispose();
                gradientBrush.Dispose();
                videoGraphics.Dispose();
            }

            return true;
        }
    }

    class ScrollSegment
    {
        public int startFrame;
        public int endFrame;
        public int minNote = int.MaxValue;
        public int maxNote = int.MinValue;
        public int NumFrames => endFrame - startFrame;
        public float scroll;
    }
}
