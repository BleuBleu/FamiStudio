using System;
using System.Collections.Generic;

namespace FamiStudio
{
    class WavPlayer : BasePlayer
    {
        List<short> samples;

        public WavPlayer(int sampleRate, bool stereo, int maxLoop, int mask, int tnd = NesApu.TND_MODE_SINGLE) : base(NesApu.APU_WAV_EXPORT, stereo, sampleRate)
        {
            maxLoopCount = maxLoop;
            channelMask = mask;
            tndMode = tnd;
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
