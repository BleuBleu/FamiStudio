using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    static class WavMp3ExportUtils
    {
        public unsafe static void Save(Song song, string filename, int sampleRate, int loopCount, int duration, int channelMask, bool separateFiles, bool separateIntro, Action<short[], string> function)
        {
            var project = song.Project;
            var introDuration = separateIntro ? GetIntroDuration(song, sampleRate) : 0;

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

                            function(samples, channelIntroFileName);
                            function(loopSamples, channelLoopFileName);
                        }
                        else
                        {
                            var channelFileName = Utils.AddFileSuffix(filename, "_" + song.Channels[channelIdx].ShortName);
                            function(samples, channelFileName);
                        }
                    }
                }
            }
            else
            {
                var player = new WavPlayer(sampleRate, loopCount, channelMask);
                var samples = player.GetSongSamples(song, project.PalMode, duration);

                if (introDuration > 0)
                {
                    var loopSamples = new short[samples.Length - introDuration];
                    Array.Copy(samples, introDuration, loopSamples, 0, loopSamples.Length);
                    Array.Resize(ref samples, introDuration);

                    var introFileName = Utils.AddFileSuffix(filename, "_Intro");
                    var loopFileName = filename;

                    function(samples, introFileName);
                    function(loopSamples, loopFileName);
                }
                else
                {
                    function(samples, filename);
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
}
