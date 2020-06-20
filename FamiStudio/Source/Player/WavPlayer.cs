using System;
using System.Collections.Generic;

namespace FamiStudio
{
    class WavPlayer : BasePlayer
    {
        List<short> samples;

        public WavPlayer(int sampleRate) : base(NesApu.APU_WAV_EXPORT, sampleRate)
        {
        }

        public short[] GetSongSamples(Song song, bool pal, int duration)
        {
            int maxSample = int.MaxValue;

            if (duration > 0)
                maxSample = duration * sampleRate;

            samples = new List<short>();

            if (BeginPlaySong(song, pal, 0))
            {
                while (PlaySongFrame() && samples.Count < maxSample);
            }

            if (samples.Count > maxSample)
                samples.RemoveRange(maxSample, samples.Count - maxSample);

            return samples.ToArray();
        }

        protected override short[] EndFrame()
        {
            samples.AddRange(base.EndFrame());
            return null;
        }
    }
}
