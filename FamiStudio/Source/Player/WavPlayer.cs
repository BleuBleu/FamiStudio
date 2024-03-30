using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    class WavPlayer : BasePlayer
    {
        List<short> samples;

        public WavPlayer(int sampleRate, bool pal, bool stereo, int maxLoop, long mask, int threadIndex = 0, int tnd = NesApu.TND_MODE_SINGLE) : base(NesApu.APU_WAV_EXPORT + threadIndex, pal, stereo, sampleRate)
        {
            Debug.Assert(threadIndex < NesApu.NUM_WAV_EXPORT_APU);

            maxLoopCount = maxLoop;
            channelMask = mask;
            tndMode = tnd;
        }

        public short[] GetSongSamples(Song song, int duration, bool log = false, bool allowAbort = false)
        {
            int maxSample = int.MaxValue;

            if (duration > 0)
                maxSample = duration * sampleRate * (stereo ? 2 : 1);

            var loopPoint = Math.Max(0, song.LoopPoint);
            var totalNumPatterns = loopPoint + (song.Length - loopPoint) * maxLoopCount;
                
            samples = new List<short>();

            BeginPlaySong(song);

            while (PlaySongFrame() && samples.Count < maxSample)
            {
                if (log)
                {
                    if (duration > 0)
                        Log.ReportProgress(samples.Count / (float)maxSample);
                    else
                        Log.ReportProgress(numPlayedPatterns / (float)totalNumPatterns);
                }

                if (allowAbort && Log.ShouldAbortOperation)
                { 
                    return new short[0];
                }
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
