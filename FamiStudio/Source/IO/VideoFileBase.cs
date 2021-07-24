using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if FAMISTUDIO_WINDOWS
    using RenderBitmap   = SharpDX.Direct2D1.Bitmap;
    using RenderGraphics = FamiStudio.Direct2DOffscreenGraphics;
#else
    using RenderBitmap   = FamiStudio.GLBitmap;
    using RenderGraphics = FamiStudio.GLOffscreenGraphics;
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

            // We simplified the rendering.
            Debug.Assert(oscLen == windowSize);

            for (int i = 0; i < oscLen; ++i)
            {
                var idx = Utils.Clamp(position - windowSize / 2 + i, 0, wav.Length - 1);
                var sample = Utils.Clamp((int)(wav[idx] * scaleY), short.MinValue, short.MaxValue);

                var x = Utils.Lerp(minX, maxX, i / (float)(oscLen - 1));
                var y = Utils.Lerp(minY, maxY, (sample - short.MinValue) / (float)(ushort.MaxValue));

                oscilloscope[i, 0] = x;
                oscilloscope[i, 1] = y;

                lastIdx = idx;
            }
        }

        protected Process LaunchFFmpeg(string ffmpegExecutable, string commandLine, bool redirectStdIn, bool redirectStdOut)
        {
            var psi = new ProcessStartInfo(ffmpegExecutable, commandLine);

            psi.UseShellExecute = false;
            psi.WorkingDirectory = Path.GetDirectoryName(ffmpegExecutable);
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;

            if (redirectStdIn)
            {
                psi.RedirectStandardInput = true;
            }

            if (redirectStdOut)
            {
                psi.RedirectStandardOutput = true;
            }

            var process = Process.Start(psi);
            process.PriorityClass = ProcessPriorityClass.BelowNormal;
            return process;
        }

        protected bool DetectFFmpeg(string ffmpegExecutable)
        {
            try
            {
                var process = LaunchFFmpeg(ffmpegExecutable, $"-version", false, true);
                var output = process.StandardOutput.ReadToEnd();

                var ret = true;
                if (!output.Contains("--enable-libx264"))
                {
                    Log.LogMessage(LogSeverity.Error, "ffmpeg does not seem to be compiled with x264 support. Make sure you have the GPL version.");
                    ret = false;
                }

                process.WaitForExit();
                process.Dispose();

                return ret;
            }
            catch
            {
                Log.LogMessage(LogSeverity.Error, "Error launching ffmpeg. Make sure the path is correct.");
                return false;
            }
        }

        protected bool Initialize(string ffmpegExecutable, int channelMask, int loopCount)
        {
            if (channelMask == 0 || loopCount < 1)
                return false;

            Log.LogMessage(LogSeverity.Info, "Detecting FFmpeg...");

            if (!DetectFFmpeg(ffmpegExecutable))
                return false;

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

    }

    class VideoChannelState
    {
        public int videoChannelIndex;
        public int songChannelIndex;
        public string channelText;
        public Channel channel;
        public RenderBitmap bmpIcon;
        public RenderGraphics graphics;
        public RenderBitmap bitmap;
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

        public VideoMetadataPlayer(int sampleRate, int maxLoop) : base(NesApu.APU_WAV_EXPORT, sampleRate)
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
            numSamples += base.EndFrame().Length;
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

#if FAMISTUDIO_LINUX || FAMISTUDIO_MACOS
    class DummyGLControl : GLControl
    {
    };
#endif
}
