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
        public unsafe static void Save(Song song, string filename, int sampleRate, int loopCount, int duration, long channelMask, bool separateFiles, bool separateIntro, bool stereo, float[] pan, int delay, bool log, bool allowAbort, Action<short[], int, string> function)
        {
            var project = song.Project;
            var introDuration = separateIntro ? GetIntroDuration(song, sampleRate, log, allowAbort) : 0;
            var outputsStereo = song.Project.OutputsStereoAudio;
            var delayInSamples = (int)(sampleRate * (delay / 1000.0f));

            if (Log.ShouldAbortOperation)
                return;

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
                    var channelBit = 1L << i;
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

                Debug.Assert(delay == 0);

                var numChannels = outputsStereo ? 2 : 1;
                var channelSamples = GetIndividualChannelSamples(song, outputsStereo, channelMask, sampleRate, loopCount, project.PalMode, duration, log, allowAbort);
                if (channelSamples == null)
                    return;

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
                    // we don't have any panning/delay to do, we can simply take 
                    // the raw result of the emulation.
                    if (delay == 0 && centerPan && outputsStereo)
                    {
                        var player = new WavPlayer(sampleRate, project.PalMode, outputsStereo, loopCount, channelMask, 0, NesApu.TND_MODE_SEPARATE);
                        samples = player.GetSongSamples(song, duration, log, allowAbort);
                        numChannels = 2;
                    }
                    else
                    {
                        Log.LogMessageConditional(log, LogSeverity.Info, $"Exporting channels individually due to custom panning or delay, this will take longer...");

                        var channelSamples = GetIndividualChannelSamples(song, outputsStereo, channelMask, sampleRate, loopCount, project.PalMode, duration, log, allowAbort);
                        if (channelSamples == null)
                            return;

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

                        var stepIn  = outputsStereo ? 2 : 1;
                        var stepOut = outputsStereo ? 1 : 2;

                        for (var i = 0; i < numGeneratedSamples; i += stepIn)
                        {
                            var l = 0.0f;
                            var r = 0.0f;

                            var i0 = i;
                            var i1 = i + (outputsStereo ? 1 : 0);

                            for (var j = 0; j < channelSamples.Length; j++)
                            {
                                if (channelSamples[j] != null)
                                {
                                    var p = pan[j];

                                    var sl = 1.0f - Utils.Clamp( 2.0f * (p - 0.5f), 0.0f, 1.0f);
                                    var sr = 1.0f - Utils.Clamp(-2.0f * (p - 0.5f), 0.0f, 1.0f);

                                    l += channelSamples[j][i0] * sl;
                                    r += channelSamples[j][i1] * sr;

                                    if (delayInSamples > 0)
                                    {
                                        var di0 = i0 - delayInSamples;
                                        var di1 = i1 - delayInSamples;

                                        // Apply opposite panning for delay.
                                        if (di0 >= 0)
                                        {
                                            l += channelSamples[j][di0] * sr;
                                            r += channelSamples[j][di1] * sl;
                                        }
                                    }
                                }
                            }

                            samples[i * stepOut + 0] = (short)Utils.Clamp((int)Math.Round(l), short.MinValue, short.MaxValue);
                            samples[i * stepOut + 1] = (short)Utils.Clamp((int)Math.Round(r), short.MinValue, short.MaxValue);
                        }

                        numChannels = 2;

                        if (!outputsStereo)
                            introDuration *= 2;
                    }
                }
                else
                {
                    var player = new WavPlayer(sampleRate, project.PalMode, outputsStereo, loopCount, channelMask);
                    var stereoSamples = player.GetSongSamples(song, duration, log, allowAbort);

                    samples = outputsStereo ? new short[stereoSamples.Length / 2] : stereoSamples;

                    if (outputsStereo)
                    {
                        for (int i = 0; i < samples.Length; i++)
                        {
                            if (i % 2 == 0)
                                samples[i] = (short)((stereoSamples[i * 2] + stereoSamples[i * 2 + 1]) / 2);
                        }
                    }

                    var samplesCopy = samples.Clone() as short[];

                    if (delay > 0)
                    {
                        for (int i = delayInSamples; i < samples.Length; i++)
                            samples[i] = (short)Utils.Clamp(samples[i] + samplesCopy[i - delayInSamples], short.MinValue, short.MaxValue);
                    }
                }

                if (introDuration > 0)
                {
                    // In case someone selects a shorter duration than the intro.
                    introDuration = Math.Min(samples.Length, introDuration);

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

            System.GC.Collect();
        }

        private static short[][] GetIndividualChannelSamples(Song song, bool outputsStereo, long channelMask, int sampleRate, int loopCount, bool pal, int duration, bool log, bool allowAbort)
        {
            // Get all the samples for all channels.
            var channelSamples = new short[song.Channels.Length][];
            var counter = new ThreadSafeCounter();

            Utils.NonBlockingParallelFor(song.Channels.Length, NesApu.NUM_WAV_EXPORT_APU, counter, (channelIdx, threadIndex) =>
            {
                var channelBit = 1L << channelIdx;
                if ((channelBit & channelMask) != 0)
                {
                    var player = new WavPlayer(sampleRate, pal, outputsStereo, loopCount, channelBit, threadIndex, NesApu.TND_MODE_SEPARATE);
                    channelSamples[channelIdx] = player.GetSongSamples(song, duration, log, allowAbort);

                    if (Log.ShouldAbortOperation)
                        return false;
                }
                
                System.GC.Collect();

                return true;
            });

            while (counter.Value != song.Channels.Length)
            {
                Log.ReportProgress(counter.Value / (float)song.Channels.Length);
                Thread.Sleep(10);
            }

            if (Log.ShouldAbortOperation)
                return null;

            return channelSamples;
        }

        private static int GetIntroDuration(Song song, int sampleRate, bool log, bool allowAbort)
        {
            if (song.LoopPoint > 0)
            {
                Log.LogMessageConditional(log, LogSeverity.Info, $"Calculating intro duration...");

                // Create a shorter version of the song.
                var songIndex = song.Project.Songs.IndexOf(song);
                var clonedProject = song.Project.DeepClone();
                var clonedSong = clonedProject.Songs[songIndex];

                clonedSong.SetLength(song.LoopPoint);

                var player = new WavPlayer(sampleRate, song.Project.PalMode, song.Project.OutputsStereoAudio, 1, -1);
                var samples = player.GetSongSamples(clonedSong, -1, log, allowAbort);

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
#if !FAMISTUDIO_MOBILE
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
