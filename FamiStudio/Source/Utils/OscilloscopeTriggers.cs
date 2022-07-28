using System;
using System.Diagnostics;

namespace FamiStudio
{
    // Loosely based on SidWizPlus
    // https://github.com/maxim-zhao/SidWizPlus
    public abstract class OscilloscopeTrigger
    {
        private bool circular;
        private short[] wav;

        public OscilloscopeTrigger(short[] w, bool c = false)
        {
            wav = w;
            circular = c;
        }

        protected int IncrementIndex(int idx, int n = 1)
        {
            if (circular)
            {
                idx += n;

                while (idx >= wav.Length)
                    idx -= wav.Length;
                while (idx < 0)
                    idx += wav.Length;

                return idx;
            }
            else
            {
                return idx + n;
            }
        }

        protected int MeasureDistance(int idx0, int idx1)
        {
            if (circular && idx0 > idx1)
            {
                return wav.Length - idx0 + idx1;
            }
            else
            {
                return idx1 - idx0;
            }
        }

        protected short GetSample(int idx)
        {
            if (circular)
            {
                return wav[idx];
            }
            else
            {
                return idx < 0 || idx >= wav.Length ? (short)0 : wav[idx];
            }
        }

        public abstract int Detect(int idx, int count);
    }

    public class RisingEdgeTrigger : OscilloscopeTrigger
    {
        public RisingEdgeTrigger(short[] w, bool c) : base(w, c)
        {
        }

        public override int Detect(int idx, int count)
        { 
            var end = IncrementIndex(idx, count);

            while (GetSample(idx) >  0 && idx != end) idx = IncrementIndex(idx);
            while (GetSample(idx) <= 0 && idx != end) idx = IncrementIndex(idx);

            if (idx == end)
                return -1;

            return idx;
        }
    }

    public class PeakSpeedTrigger : OscilloscopeTrigger
    {
        public PeakSpeedTrigger(short[] w, bool c) : base(w, c)
        {
        }

        public override int Detect(int idx, int count)
        {
            var peak = short.MinValue;
            var minDist = int.MaxValue;
            var end = IncrementIndex(idx, count);
            var result = -1;

            while (idx != end)
            {
                // Find crossing
                while (GetSample(idx) >  0 && idx != end) idx = IncrementIndex(idx);
                while (GetSample(idx) <= 0 && idx != end) idx = IncrementIndex(idx);

                // Look for next peak.
                var crossing = idx;

                for (var sample = GetSample(idx); sample > 0 && idx != end; idx = IncrementIndex(idx), sample = GetSample(idx))
                {
                    var dist = MeasureDistance(crossing, idx);

                    if (sample > peak)
                    {
                        peak = sample;
                        result = crossing;
                        minDist = dist;
                    }
                    else if (sample == peak && dist < minDist)
                    {
                        result = crossing;
                        minDist = dist;
                    }
                }
            }

            return result;
        }
    }
}
