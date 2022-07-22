using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

#if FAMISTUDIO_ANDROID
using VideoEncoder   = FamiStudio.VideoEncoderAndroid;
#else
using VideoEncoder   = FamiStudio.VideoEncoderFFmpeg;
#endif

namespace FamiStudio
{
    class VideoFileBase
    {
        protected const int SampleRate = 44100;
        protected const float OscilloscopeWindowSize = 0.075f; // in sec.
        protected const int ChannelIconTextSpacing = 8;

        protected int videoResX = 1920;
        protected int videoResY = 1080;

        protected string tempAudioFile;
        protected int maxAbsSample = 0;

        protected Project project;
        protected Song song;
        protected OffscreenGraphics videoGraphics;
        protected VideoEncoder videoEncoder;
        protected VideoChannelState[] channelStates;
        protected FontRenderResources fontResources;

        // Mostly from : https://github.com/kometbomb/oscilloscoper/blob/master/src/Oscilloscope.cpp
        protected void GenerateOscilloscope(short[] wav, int position, int windowSize, int maxLookback, float scaleY, float minX, float minY, float maxX, float maxY, float[,] oscilloscope)
        {
            // Find a point where the waveform crosses the axis, looks nicer.
            int lookback = 0;
            int orig = wav[position];

            // If sample is negative, go back until positive.
            if (orig < 0)
            {
                while (lookback < maxLookback)
                {
                    if (position == 0 || wav[--position] > 0)
                        break;

                    lookback++;
                }

                orig = wav[position];
            }

            // Then look for a zero crossing.
            if (orig > 0)
            {
                while (lookback < maxLookback)
                {
                    if (position == wav.Length - 1 || wav[++position] < 0)
                        break;

                    lookback++;
                }
            }

            int lastIdx = -1;
            int oscLen = oscilloscope.GetLength(0);

            for (int i = 0; i < oscLen; ++i)
            {
                var idx = Utils.Clamp(position - windowSize / 2 + i * windowSize / oscLen, 0, wav.Length - 1);
                var avg = (float)wav[idx];
                var cnt = 1;

                if (lastIdx >= 0)
                {
                    for (int j = lastIdx + 1; j < idx; j++, cnt++)
                        avg += wav[j];
                    avg /= cnt;
                }

                var sample = Utils.Clamp((int)(avg * scaleY), short.MinValue, short.MaxValue);

                var x = Utils.Lerp(minX, maxX, i / (float)(oscLen - 1));
                var y = Utils.Lerp(minY, maxY, (sample - short.MinValue) / (float)(ushort.MaxValue));

                oscilloscope[i, 0] = x;
                oscilloscope[i, 1] = y;

                lastIdx = idx;
            }
        }

        protected bool Initialize(Project originalProject, int songId, int loopCount, string filename, int resX, int resY, bool halfFrameRate, long channelMask, int audioBitRate, int videoBitRate, bool stereo, float[] pan)
        {
            if (channelMask == 0 || loopCount < 1)
                return false;

            Log.LogMessage(LogSeverity.Info, "Detecting FFmpeg...");

            videoEncoder = VideoEncoder.CreateInstance();
            if (videoEncoder == null)
                return false;

            videoResX = resX;
            videoResY = resY;

            project = originalProject.DeepClone();
            song = project.GetSong(songId);

            ExtendSongForLooping(song, loopCount);

            // Save audio to temporary file.
            tempAudioFile = Path.Combine(Utils.GetTemporaryDiretory(), "temp.wav");
            AudioExportUtils.Save(song, tempAudioFile, SampleRate, 1, -1, channelMask, false, false, stereo, pan, true, (samples, samplesChannels, fn) => { WaveFile.Save(samples, fn, SampleRate, samplesChannels); });

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

            Utils.NonBlockingParallelFor(channelStates.Length, NesApu.NUM_WAV_EXPORT_APU, counter, (stateIndex, threadIndex) =>
            {
                var state = channelStates[stateIndex];
                state.wav = new WavPlayer(SampleRate, song.Project.OutputsStereoAudio, 1, 1 << state.songChannelIndex, threadIndex).GetSongSamples(song, song.Project.PalMode, -1, false, true);

                if (Log.ShouldAbortOperation)
                    return false;

                if (song.Project.OutputsStereoAudio)
                    state.wav = WaveUtils.MixDown(state.wav);

                maxAbsSample = WaveUtils.GetMaxAbsValue(state.wav);
                return true;
            });

            while (counter.Value != channelStates.Length)
            {
                Log.ReportProgress(counter.Value / (float)channelStates.Length);
                Thread.Sleep(10);

                if (Log.ShouldAbortOperation)
                    return false;
            }

            // Create graphics resources.
            videoGraphics = OffscreenGraphics.Create(videoResX, videoResY, true);

            if (videoGraphics == null)
            {
                Log.LogMessage(LogSeverity.Error, "Error initializing off-screen graphics, aborting.");
                return false;
            }

            fontResources = new FontRenderResources(videoGraphics);

            return true;
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
        public Bitmap bmpIcon;
        public OffscreenGraphics graphics;
        public Bitmap bitmap;
        public short[] wav;
    };

    class VideoFrameMetadata
    {
        public int playPattern;
        public float playNote;
        public int wavOffset;
        public Note[] channelNotes;
        public int[] channelVolumes;
        public float[] scroll;
        public Color[] channelColors;
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

            meta.playPattern = playLocation.PatternIndex;
            meta.playNote = playLocation.NoteIndex;
            meta.wavOffset = prevNumSamples;
            meta.channelNotes = new Note[song.Channels.Length];
            meta.channelVolumes = new int[song.Channels.Length];

            for (int i = 0; i < channelStates.Length; i++)
            {
                meta.channelNotes[i] = channelStates[i].CurrentNote;
                meta.channelVolumes[i] = channelStates[i].CurrentVolume;
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
