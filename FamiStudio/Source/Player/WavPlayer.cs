using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    class WavPlayer : BasePlayer
    {
        List<short> samples;

        public WavPlayer() : base(NesApu.APU_WAV_EXPORT)
        {
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
