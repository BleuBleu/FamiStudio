using System.Collections.Generic;

namespace FamiStudio
{
    class WavPlayer : BasePlayer
    {
        List<short> samples;

        public WavPlayer(int sampleRate) : base(NesApu.APU_WAV_EXPORT, sampleRate)
        {
            loopMode = LoopMode.LoopPoint;
        }

        public short[] GetSongSamples(Song song, bool pal)
        {
            samples = new List<short>();

            if (BeginPlaySong(song, pal, 0))
            {
                while (PlaySongFrame());
            }

            return samples.ToArray();
        }

        protected override short[] EndFrame()
        {
            samples.AddRange(base.EndFrame());
            return null;
        }
    }
}
