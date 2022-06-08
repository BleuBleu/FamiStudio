using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FamiStudio
{
    static class AudioExportUtils
    {
        public unsafe static void Save(Song song, string filename, int sampleRate, int loopCount, int duration, int channelMask, bool separateFiles, bool separateIntro, bool stereo, float[] pan, bool log, Action<short[], int, string> function)
        {
            var project = song.Project;
            var introDuration = separateIntro ? GetIntroDuration(song, sampleRate, log) : 0;
            var outputsStereo = song.Project.OutputsStereoAudio;

            // We will enforce stereo export if the chip outputs stereo.
            Debug.Assert(!outputsStereo || stereo);

            if (channelMask == 0)
                return;

            Log.ReportProgress(0.0f);
            Log.LogMessageConditional(log, LogSeverity.Info, "Rendering audio...");

            var centerPan = pan == null;

            if (pan != null)
            {
                centerPan = true;
                for (int i = 0; i < pan.Length; i++)
                {
                    var channelBit = 1 << i;
                    if ((channelBit & channelMask) != 0)
                    {
                        if (pan[i] != 0.5f)
                        {
                            centerPan = false;
                            break;
                        }
                    }
                }
            }

            if (separateFiles)
            {
                Log.LogMessageConditional(log, LogSeverity.Info, $"Rendering audio for individual channels...");

                var numChannels = outputsStereo ? 2 : 1;
                var channelSamples = GetIndividualChannelSamples(song, outputsStereo, channelMask, sampleRate, loopCount, project.PalMode, duration, log);

                for (int channelIdx = 0; channelIdx < song.Channels.Length; channelIdx++)
                {
                    var samples = channelSamples[channelIdx];
                    if (samples != null)
                    {
                        if (introDuration > 0)
                        {
                            var loopSamples = new short[samples.Length - introDuration];
                            Array.Copy(samples, introDuration, loopSamples, 0, loopSamples.Length);
                            Array.Resize(ref samples, introDuration);

                            var channelIntroFileName = Utils.AddFileSuffix(filename, "_" + song.Channels[channelIdx].ShortName + "_Intro");
                            var channelLoopFileName = Utils.AddFileSuffix(filename, "_" + song.Channels[channelIdx].ShortName);

                            function(samples, numChannels, channelIntroFileName);
                            function(loopSamples, numChannels, channelLoopFileName);
                        }
                        else
                        {
                            var channelFileName = Utils.AddFileSuffix(filename, "_" + song.Channels[channelIdx].ShortName);
                            function(samples, numChannels, channelFileName);
                        }
                    }
                }
            }
            else
            {
                var numChannels = 1;
                var samples = (short[])null;

                if (stereo)
                {
                    // Optimization : If the project already outputs stereo and
                    // we don't have any panning to do, we can simply take the
                    // raw result of the emulation.
                    if (centerPan && outputsStereo)
                    {
                        var player = new WavPlayer(sampleRate, outputsStereo, loopCount, channelMask, 0, NesApu.TND_MODE_SEPARATE);
                        samples = player.GetSongSamples(song, project.PalMode, duration, log);
                        numChannels = 2;
                    }
                    else
                    {
                        Log.LogMessageConditional(log, LogSeverity.Info, $"Exporting channels individually due to custom panning, this will take longer...");

                        var channelSamples = GetIndividualChannelSamples(song, outputsStereo, channelMask, sampleRate, loopCount, project.PalMode, duration, log);

                        var numGeneratedSamples = 0;
                        for (int i = 0; i < channelSamples.Length; i++)
                        {
                            if (channelSamples[i] != null)
                            {
                                numGeneratedSamples = channelSamples[i].Length;
                                break;
                            }
                        }

                        // Mix and interleave samples.
                        samples = outputsStereo ? new short[numGeneratedSamples] : new short[numGeneratedSamples * 2];

                        for (int i = 0; i < numGeneratedSamples; i++)
                        {
                            float l = 0;
                            float r = 0;

                            for (int j = 0; j < channelSamples.Length; j++)
                            {
                                if (channelSamples[j] != null)
                                {
                                    float sl = 1.0f - Utils.Clamp( 2.0f * (pan[j] - 0.5f), 0.0f, 1.0f);
                                    float sr = 1.0f - Utils.Clamp(-2.0f * (pan[j] - 0.5f), 0.0f, 1.0f);

                                    l += channelSamples[j][i] * sl;
                                    r += channelSamples[j][i] * sr;
                                }
                            }

                            if (outputsStereo)
                            {
                                if (i % 2 == 0)
                                    samples[i] = (short)Utils.Clamp((int)Math.Round(l), short.MinValue, short.MaxValue);
                                else
                                    samples[i] = (short)Utils.Clamp((int)Math.Round(r), short.MinValue, short.MaxValue);
                            }
                            else
                            {
                                samples[i * 2 + 0] = (short)Utils.Clamp((int)Math.Round(l), short.MinValue, short.MaxValue);
                                samples[i * 2 + 1] = (short)Utils.Clamp((int)Math.Round(r), short.MinValue, short.MaxValue);
                            }
                        }

                        numChannels = 2;

                        if (!outputsStereo)
                            introDuration *= 2;
                    }
                }
                else
                {
                    var player = new WavPlayer(sampleRate, outputsStereo, loopCount, channelMask);
                    var stereoSamples = player.GetSongSamples(song, project.PalMode, duration, log);

                    samples = outputsStereo ? new short[stereoSamples.Length / 2] : stereoSamples;

                    if (outputsStereo)
                    {
                        for (int i = 0; i < samples.Length; i++)
                        {
                            if (i % 2 == 0)
                                samples[i] = (short)((stereoSamples[i * 2] + stereoSamples[i * 2 + 1]) / 2);
                        }
                    }
                }

                if (introDuration > 0)
                {
                    var loopSamples = new short[samples.Length - introDuration];
                    Array.Copy(samples, introDuration, loopSamples, 0, loopSamples.Length);
                    Array.Resize(ref samples, introDuration);

                    var introFileName = Utils.AddFileSuffix(filename, "_Intro");
                    var loopFileName = filename;

                    function(samples, numChannels, introFileName);
                    function(loopSamples, numChannels, loopFileName);
                }
                else
                {
                    function(samples, numChannels, filename);
                }
            }
        }
        private static short[][] GetIndividualChannelSamples(Song song, bool outputsStereo, int channelMask, int sampleRate, int loopCount, bool pal, int duration, bool log)
        {
            // Get all the samples for all channels.
            var channelSamples = new short[song.Channels.Length][];
            var counter = new ThreadSafeCounter();

            Utils.NonBlockingParallelFor(song.Channels.Length, NesApu.NUM_WAV_EXPORT_APU, counter, (channelIdx, threadIndex) =>
            {
                var channelBit = 1 << channelIdx;
                if ((channelBit & channelMask) != 0)
                {
                    var player = new WavPlayer(sampleRate, outputsStereo, loopCount, channelBit, threadIndex, NesApu.TND_MODE_SEPARATE);
                    channelSamples[channelIdx] = player.GetSongSamples(song, pal, duration, false, log);

                    if (Log.ShouldAbortOperation)
                        return false;
                }

                return true;
            });

            while (counter.Value != song.Channels.Length)
            {
                Log.ReportProgress(counter.Value / (float)song.Channels.Length);
                Thread.Sleep(10);

                if (Log.ShouldAbortOperation)
                    break;
            }

            return channelSamples;
        }

        private static int GetIntroDuration(Song song, int sampleRate, bool log)
        {
            if (song.LoopPoint > 0)
            {
                Log.LogMessageConditional(log, LogSeverity.Info, $"Calculating intro duration...");

                // Create a shorter version of the song.
                var songIndex = song.Project.Songs.IndexOf(song);
                var clonedProject = song.Project.DeepClone();
                var clonedSong = clonedProject.Songs[songIndex];

                clonedSong.SetLength(song.LoopPoint);

                var player = new WavPlayer(sampleRate, song.Project.OutputsStereoAudio, 1, -1);
                var samples = player.GetSongSamples(clonedSong, song.Project.PalMode, -1, log);

                return samples.Length;
            }
            else
            {
                return 0;
            }
        }
    }

    public static class AudioFormatType
    {
        public const int Wav = 0;
        public const int Mp3 = 1;
        public const int Vorbis = 2;

        public static readonly string[] Names =
        {
            "WAV",
            "MP3",
#if !FAMISTUDIO_ANDROID
            "Ogg Vorbis"
#endif
        };

        public static readonly string[] Extensions =
        {
            "wav",
            "mp3",
            "ogg",
        };

        public static readonly string[] MimeTypes =
        {
            "audio/x-wav",
            "audio/mpeg",
            "audio/ogg",
        };

        public static int GetValueForName(string str)
        {
            return Array.IndexOf(Names, str);
        }
    }
}
