using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

#if FAMISTUDIO_ANDROID
using VideoEncoder = FamiStudio.VideoEncoderAndroid;
#else
using VideoEncoder = FamiStudio.VideoEncoderFFmpeg;
#endif

namespace FamiStudio
{
    class VideoFileBase
    {
        protected const int   SampleRate = 44100;
        protected const int   ChannelIconTextSpacing = 8;

        protected int videoResX = 1920;
        protected int videoResY = 1080;

        protected bool   halfFrameRate;
        protected string tempAudioFile;

        protected int oscFrameWindowSize;
        protected int oscRenderWindowSize;

        protected Project project;
        protected Song song;
        protected OffscreenGraphics videoGraphics;
        protected VideoEncoder videoEncoder;
        protected VideoChannelState[] channelStates;
        protected VideoFrameMetadata[] metadata;
        protected FontRenderResources fontResources;
        protected Bitmap watermark;

        // TODO : This is is very similar to Oscilloscope.cs, unify eventually...
        protected float[,] UpdateOscilloscope(VideoChannelState state, int frameIndex)
        {
            var meta = metadata[frameIndex];
            var newTrigger = meta.channelTriggers[state.songChannelIndex];

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

            var vertices = new float[oscRenderWindowSize, 2];
            var startIdx = newTrigger >= 0 ? newTrigger : state.lastTrigger;

            for (int i = 0, j = startIdx - oscRenderWindowSize / 2; i < oscRenderWindowSize; i++, j++)
            {
                var samp = j < 0 || j >= state.wav.Length ? 0 : state.wav[j];

                vertices[i, 0] = i / (float)(oscRenderWindowSize - 1);
                vertices[i, 1] = Utils.Clamp(samp / 32768.0f * state.oscScale, -1.0f, 1.0f);
            }
        
            if (newTrigger >= 0)
                state.lastTrigger = newTrigger;

            return vertices;
        }

        protected bool InitializeEncoder(Project originalProject, int songId, int loopCount, string filename, int resX, int resY, bool halfRate, int window, long channelMask, int audioDelay, int audioBitRate, int videoBitRate, bool stereo, float[] pan)
        {
            if (channelMask == 0 || loopCount < 1)
                return false;

            Log.LogMessage(LogSeverity.Info, "Detecting FFmpeg...");

            videoEncoder = VideoEncoder.CreateInstance();
            if (videoEncoder == null)
                return false;

            videoResX = resX;
            videoResY = resY;
            halfFrameRate = halfRate;

            project = originalProject.DeepClone();
            song = project.GetSong(songId);

            ExtendSongForLooping(song, loopCount);

            // Save audio to temporary file.
            tempAudioFile = Path.Combine(Utils.GetTemporaryDiretory(), "temp.wav");
            AudioExportUtils.Save(song, tempAudioFile, SampleRate, 1, -1, channelMask, false, false, stereo, pan, audioDelay, true, (samples, samplesChannels, fn) => { WaveFile.Save(samples, fn, SampleRate, samplesChannels); });

            if (Log.ShouldAbortOperation)
                return false;

            // Start encoder, must be done before any GL calls on Android.
            GetFrameRateInfo(song.Project, halfFrameRate, out var frameRateNumer, out var frameRateDenom);

            if (!videoEncoder.BeginEncoding(videoResX, videoResY, frameRateNumer, frameRateDenom, videoBitRate, audioBitRate, stereo, tempAudioFile, filename))
            {
                Log.LogMessage(LogSeverity.Error, "Error starting video encoder, aborting.");
                return false;
            }

            // Create the channel states.
            channelStates = new VideoChannelState[Utils.NumberOfSetBits(channelMask)];

            for (int i = 0, channelIndex = 0; i < song.Channels.Length; i++)
            {
                if ((channelMask & (1L << i)) == 0)
                    continue;

                var channel = song.Channels[i];
                var pattern = channel.PatternInstances[0];
                var state = new VideoChannelState();

                state.videoChannelIndex = channelIndex;
                state.songChannelIndex = i;
                state.channel = song.Channels[i];
                state.channelText = state.channel.NameWithExpansion;

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
                state.wav = new WavPlayer(SampleRate, song.Project.OutputsStereoAudio, 1, 1 << state.songChannelIndex, threadIndex).GetSongSamples(song, song.Project.PalMode, -1, false, true);
                state.triggerFunction = new PeakSpeedTrigger(state.wav, false);

                if (Log.ShouldAbortOperation)
                    return false;

                if (song.Project.OutputsStereoAudio)
                    state.wav = WaveUtils.MixDown(state.wav);

                maxAbsSamples[stateIndex] = WaveUtils.GetMaxAbsValue(state.wav);

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
                channelStates[i].oscScale = maxAbsSamples[i] == 0 ? 1.0f : (float)Math.Sqrt(globalMaxAbsSample / (float)maxAbsSamples[i]) * (32768.0f / globalMaxAbsSample);

            // Create graphics resources.
            videoGraphics = OffscreenGraphics.Create(videoResX, videoResY, true);

            if (videoGraphics == null)
            {
                Log.LogMessage(LogSeverity.Error, "Error initializing off-screen graphics, aborting.");
                return false;
            }

            fontResources = new FontRenderResources(videoGraphics);
            watermark = videoGraphics.CreateBitmapFromResource("VideoWatermark");

            // Generate metadata
            metadata = new VideoMetadataPlayer(SampleRate, song.Project.OutputsStereoAudio, 1).GetVideoMetadata(song, song.Project.PalMode, -1);

            oscFrameWindowSize  = (int)(SampleRate / (song.Project.PalMode ? NesApu.FpsPAL : NesApu.FpsNTSC));
            oscRenderWindowSize = (int)(oscFrameWindowSize * window);

            return true;
        }

        protected bool LaunchEncoderLoop(Action<int> body, Action cleanup = null)
        {
            var videoImage = new byte[videoResY * videoResX * 4];
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

                    videoGraphics.BeginDrawFrame();
                    videoGraphics.BeginDrawControl(new Rectangle(0, 0, videoResX, videoResY), videoResY);

                    body(f);

                    // Watermark.
                    var cmd = videoGraphics.CreateCommandList();
                    cmd.DrawBitmap(watermark, videoResX - watermark.Size.Width, videoResY - watermark.Size.Height);
                    videoGraphics.DrawCommandList(cmd);

                    videoGraphics.EndDrawControl();
                    videoGraphics.EndDrawFrame();

                    // Readback
                    videoGraphics.GetBitmap(videoImage);

                    // Send to encoder.
                    videoEncoder.AddFrame(videoImage);
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
                fontResources.Dispose();
                watermark.Dispose();
                videoGraphics.Dispose();
                foreach (var c in channelStates)
                    c.icon?.Dispose();
                File.Delete(tempAudioFile);

                cleanup?.Invoke();
            }

            return success;
        }

        protected void LoadChannelIcons(bool large)
        {
            var suffix = large ? "@2x" : "";

            foreach (var s in channelStates)
                s.icon = videoGraphics.CreateBitmapFromResource(ChannelType.Icons[s.channel.Type] + suffix);
        }

        protected void ExtendSongForLooping(Song song, int loopCount)
        {
            // For looping, we simply extend the song by copying pattern instances.
            if (loopCount > 1 && song.LoopPoint >= 0 && song.LoopPoint < song.Length)
            {
                var originalLength = song.Length;
                var loopSectionLength = originalLength - song.LoopPoint;

                song.SetLength(Math.Min(Song.MaxLength, originalLength + loopSectionLength * (loopCount - 1)));

                var srcPatIdx = song.LoopPoint;

                for (var i = originalLength; i < song.Length; i++)
                {
                    foreach (var c in song.Channels)
                        c.PatternInstances[i] = c.PatternInstances[srcPatIdx];

                    if (song.PatternHasCustomSettings(srcPatIdx))
                    {
                        var customSettings = song.GetPatternCustomSettings(srcPatIdx);
                        song.SetPatternCustomSettings(i, customSettings.patternLength, customSettings.beatLength, customSettings.groove, customSettings.groovePaddingMode);
                    }

                    if (++srcPatIdx >= originalLength)
                        srcPatIdx = song.LoopPoint;
                }
            }
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
        public Bitmap icon;
        public Bitmap bitmap; // Offscreen bitmap for piano roll
        public OffscreenGraphics graphics;
        public short[] wav;
        public float oscScale;
        public int lastTrigger;
        public int holdFrameCount;
        public OscilloscopeTrigger triggerFunction;
    };

    class VideoFrameMetadata
    {
        public int playPattern;
        public float playNote;
        public int wavOffset;

        // MATTT : Create a class for this + rename metadata to something better.
        public Note[] channelNotes;
        public int[] channelVolumes;
        public int[] channelTriggers;
        public float[] channelScolls; // Used by piano roll.
        public Color[] channelColors; // Used by oscilloscope.
    };

    class VideoMetadataPlayer : BasePlayer
    {
        int numSamples = 0;
        int prevNumSamples = 0;
        List<VideoFrameMetadata> metadata;

        public VideoMetadataPlayer(int sampleRate, bool stereo, int maxLoop) : base(NesApu.APU_WAV_EXPORT, stereo, sampleRate)
        {
            maxLoopCount = maxLoop;
            metadata = new List<VideoFrameMetadata>();
        }

        private void WriteMetadata(List<VideoFrameMetadata> metadata)
        {
            var meta = new VideoFrameMetadata();

            meta.playPattern     = playLocation.PatternIndex;
            meta.playNote        = playLocation.NoteIndex;
            meta.wavOffset       = prevNumSamples;
            meta.channelNotes    = new Note[song.Channels.Length];
            meta.channelVolumes  = new int[song.Channels.Length];
            meta.channelTriggers = new int[song.Channels.Length];

            for (int i = 0; i < channelStates.Length; i++)
            {
                meta.channelNotes[i]    = channelStates[i].CurrentNote;
                meta.channelVolumes[i]  = channelStates[i].CurrentVolume;
                meta.channelTriggers[i] = GetOscilloscopeTrigger(channelStates[i].InnerChannelType);
            }

            metadata.Add(meta);

            prevNumSamples = numSamples;
        }

        public VideoFrameMetadata[] GetVideoMetadata(Song song, bool pal, int duration)
        {
            int maxSample = int.MaxValue;

            if (duration > 0)
                maxSample = duration * sampleRate;

            if (BeginPlaySong(song, pal, 0))
            {
                WriteMetadata(metadata);

                while (PlaySongFrame() && numSamples < maxSample)
                {
                    WriteMetadata(metadata);
                }
            }

            return metadata.ToArray();
        }

        protected override short[] EndFrame()
        {
            numSamples += base.EndFrame().Length / (stereo ? 2 : 1);
            return null;
        }
    }

    static class VideoResolution
    {
        public static readonly string[] Names =
        {
            "1080p",
            "720p",
            "576p",
            "480p"
        };

        public static readonly int[] ResolutionY =
        {
            1080,
            720,
            576,
            480
        };

        public static readonly int[] ResolutionX =
        {
            1920,
            1280,
            1024,
            854
        };

        public static int GetIndexForName(string str)
        {
            return Array.IndexOf(Names, str);
        }
    }
}
