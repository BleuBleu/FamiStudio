using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    static class AudioExportUtils
    {
        public unsafe static void Save(Song song, string filename, int sampleRate, int loopCount, int duration, int channelMask, bool separateFiles, bool separateIntro, bool stereo, float[] pan, Action<short[], int, string> function)
        {
            var project = song.Project;
            var introDuration = separateIntro ? GetIntroDuration(song, sampleRate) : 0;

            if (channelMask == 0)
                return;

            if (separateFiles)
            {
                for (int channelIdx = 0; channelIdx < song.Channels.Length; channelIdx++)
                {
                    var channelBit = 1 << channelIdx;
                    if ((channelBit & channelMask) != 0)
                    {
                        var player = new WavPlayer(sampleRate, loopCount, channelBit);
                        var samples = player.GetSongSamples(song, project.PalMode, duration);

                        if (introDuration > 0)
                        {
                            var loopSamples = new short[samples.Length - introDuration];
                            Array.Copy(samples, introDuration, loopSamples, 0, loopSamples.Length);
                            Array.Resize(ref samples, introDuration);

                            var channelIntroFileName = Utils.AddFileSuffix(filename, "_" + song.Channels[channelIdx].ShortName + "_Intro");
                            var channelLoopFileName = Utils.AddFileSuffix(filename, "_" + song.Channels[channelIdx].ShortName);

                            function(samples, 1, channelIntroFileName);
                            function(loopSamples, 1, channelLoopFileName);
                        }
                        else
                        {
                            var channelFileName = Utils.AddFileSuffix(filename, "_" + song.Channels[channelIdx].ShortName);
                            function(samples, 1, channelFileName);
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
                    // Get all the samples for all channels.
                    var channelSamples = new short[song.Channels.Length][];
                    var numStereoSamples = 0;

                    for (int channelIdx = 0; channelIdx < song.Channels.Length; channelIdx++)
                    {
                        var channelBit = 1 << channelIdx;
                        if ((channelBit & channelMask) != 0)
                        {
                            var player = new WavPlayer(sampleRate, loopCount, channelBit);
                            channelSamples[channelIdx] = player.GetSongSamples(song, project.PalMode, duration);
                            numStereoSamples = Math.Max(numStereoSamples, channelSamples[channelIdx].Length);
                        }
                    }

                    // Mix and interleave samples.
                    samples = new short[numStereoSamples * 2];

                    for (int i = 0; i < numStereoSamples; i++)
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

                        samples[i * 2 + 0] = (short)Utils.Clamp((int)Math.Round(l), short.MinValue, short.MaxValue);
                        samples[i * 2 + 1] = (short)Utils.Clamp((int)Math.Round(r), short.MinValue, short.MaxValue);
                    }

                    numChannels = 2;
                }
                else
                {
                    var player = new WavPlayer(sampleRate, loopCount, channelMask);
                    samples = player.GetSongSamples(song, project.PalMode, duration);
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

        public static int GetIntroDuration(Song song, int sampleRate)
        {
            if (song.LoopPoint > 0)
            {
                // Create a shorter version of the song.
                var songIndex = song.Project.Songs.IndexOf(song);
                var clonedProject = song.Project.DeepClone();
                var clonedSong = clonedProject.Songs[songIndex];

                clonedSong.SetLength(song.LoopPoint);

                var player = new WavPlayer(sampleRate, 1, 0x7fffffff);
                var samples = player.GetSongSamples(clonedSong, song.Project.PalMode, -1);

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
