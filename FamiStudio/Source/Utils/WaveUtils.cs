using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    static class WaveUtils
    {
        //public interface IWave
        //{
        //    void SerializeState(ProjectBuffer buffer);
        //    bool Trim(float timeStart, float timeEnd);
        //    bool TrimSilence();
        //    int NumSamples { get; }
        //    float SampleRate;
        //};

        //// Mono, 16-bit PCM wave
        //public class PCMWave
        //{
        //    float 

        //    public PCMWave(short samples[], float rate)
        //    {

        //    }

        //    public DPCMWave ToDPCM()
        //    {
        //        return null;
        //    }
        //};

        //// Mono, 1-bit DPCM wave
        //public class DPCMWave
        //{
        //    public PCMWave ToPCM()
        //    {
        //        return null;
        //    }
        //};

        static public void Resample(short[] source, short[] dest)
        {
            var ratio = source.Length / (float)dest.Length;

            // Linear filtering, will suffer from aliasing when downsampling by less than 1/2...
            for (int i = 0; i < dest.Length; i++)
            {
                var srcPos = i * ratio;

                var idx0 = Math.Min((int)Math.Floor  (srcPos), source.Length - 1);
                var idx1 = Math.Min((int)Math.Ceiling(srcPos), source.Length - 1);

                var s0 = source[idx0];
                var s1 = source[idx1];
                var alpha = Utils.Frac(srcPos);

                dest[i] = (short)Utils.Lerp(s0, s1, alpha);
            }
        }

        static public int DpcmCounterToWaveSample(int counter)
        {
            Debug.Assert(counter >= 0 && counter < 64);
            return counter * (65536 / 64) - 32768;
        }

        static public void WaveToDpcm(short[] wave, float waveSampleRate, float dpcmSampleRate, int dpcmCounterStart, out byte[] dpcm)
        {
            var dpcmNumSamples = (int)Math.Round(wave.Length * (dpcmSampleRate / (float)waveSampleRate));

            // Resample to the correct rate 
            var resampledWave = new short[dpcmNumSamples];
            Resample(wave, resampledWave);

            var dpcmSize = (dpcmNumSamples + 7) / 8; // Round up to byte. 
            var dpcmSizePadded = (dpcmSize + 15) & ~15; // Technically we should make the size 16x + 1. DPCMTODO

            dpcm = new byte[dpcmSizePadded];

            // We might not (fully) write the last few bytes, so pre-fill.
            for (int i = dpcmSize - 1; i < dpcmSizePadded; i++)
                dpcm[i] = 0x55; 

            // DPCM conversion.
            var dpcmCounter = dpcmCounterStart;

            for (int i = 0; i < resampledWave.Length; i++)
            {
                var waveSample = resampledWave[i];
                var dpcmSample = DpcmCounterToWaveSample(dpcmCounter);

                var index = i / 8;
                var mask  = (1 << (i & 7));

                if (dpcmSample < waveSample)
                {
                    dpcm[index] |= (byte)mask;
                    dpcmCounter = Math.Min(dpcmCounter + 1, 63);
                }
                else
                {
                    dpcm[index] &= (byte)~mask;
                    dpcmCounter = Math.Max(dpcmCounter - 1, 0);
                }
            }
        }

        static public void DpcmToWave(byte[] dpcm, int dpcmCounterStart, out short[] wave)
        {
            wave = new short[dpcm.Length * 8];
            var dpcmCounter = dpcmCounterStart;

            // DPCM -> WAV conversion
            for (int i = 0; i < dpcm.Length; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    // DPCMTODO : Do we increment before or after?
                    wave[i * 8 + j] = (short)DpcmCounterToWaveSample(dpcmCounter);

                    var mask = 1 << j;

                    // TODO: Validate if hardware clamps or wraps.
                    if ((dpcm[i] & (byte)mask) != 0)
                        dpcmCounter = Math.Min(dpcmCounter + 1, 63);
                    else
                        dpcmCounter = Math.Max(dpcmCounter - 1, 0);
                }
            }
        }

        static public bool TrimWave(ref short[] wave, int sampleRate, float timeStart, float timeEnd)
        {
            var trimSampleMin = Utils.Clamp((int)Math.Ceiling(timeStart * sampleRate), 0, wave.Length - 1);
            var trimSampleMax = Utils.Clamp((int)Math.Ceiling(timeEnd   * sampleRate), 0, wave.Length - 1);

            if (trimSampleMin < trimSampleMax)
            {
                var newLength = trimSampleMin + (wave.Length - trimSampleMax);
                var newWavData = new short[newLength];

                if (trimSampleMin > 0)
                    Array.Copy(wave, newWavData, trimSampleMin);
                if (trimSampleMax < wave.Length)
                    Array.Copy(wave, trimSampleMax, newWavData, trimSampleMin, wave.Length - trimSampleMax);

                wave = newWavData;

                return true;
            }
            else
            {
                return false;
            }
        }

        static public bool TrimSilence(ref short[] wave, int rate)
        {
            const int threshold = 512;

            int nonZeroMin = 0;
            int nonZeroMax = wave.Length - 1;

            for (int i = 0; i < wave.Length; i++)
            {
                if (Math.Abs(wave[i]) > threshold)
                {
                    nonZeroMin = i;
                    break;
                }
            }

            for (int i = wave.Length - 1; i >= 0; i--)
            {
                if (Math.Abs(wave[i]) > threshold)
                {
                    nonZeroMax = i;
                    break;
                }
            }

            if (nonZeroMin != 0 || nonZeroMax != wave.Length - 1)
            {
                var newWavData = new short[nonZeroMax - nonZeroMin + 1];
                Array.Copy(wave, nonZeroMin, newWavData, 0, newWavData.Length);

                wave = newWavData;

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
