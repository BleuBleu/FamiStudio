using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public enum WaveToDpcmRoundingMode
    {
        None,
        RoundTo16Bytes,
        RoundTo16BytesPlusOne
    };

    public static class WaveUtils
    {
        static public void Resample(short[] source, int minSample, int maxSample, short[] dest)
        {
            var numSourceSamples = maxSample - minSample;
            var ratio = numSourceSamples / (float)dest.Length;

            // Linear filtering, will suffer from aliasing when downsampling by less than 1/2...
            for (int i = 0; i < dest.Length; i++)
            {
                var srcPos = minSample + i * ratio;

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

        static public void WaveToDpcm(short[] wave, int minSample, int maxSample, float waveSampleRate, float dpcmSampleRate, int dpcmCounterStart, WaveToDpcmRoundingMode roundMode, out byte[] dpcm)
        {
            var waveNumSamples = maxSample - minSample;
            var dpcmNumSamplesFloat = waveNumSamples * (dpcmSampleRate / (float)waveSampleRate);
            var dpcmNumSamples = 0;

            switch (roundMode)
            {
                case WaveToDpcmRoundingMode.RoundTo16Bytes:
                    dpcmNumSamples = (int)Math.Round(dpcmNumSamplesFloat / 128.0f) * 128;
                    break;
                case WaveToDpcmRoundingMode.RoundTo16BytesPlusOne:
                    dpcmNumSamples = (int)Math.Round(dpcmNumSamplesFloat / 128.0f) * 128 + 8;
                    break;
                default:
                    dpcmNumSamples = (int)Math.Round(dpcmNumSamplesFloat);
                    break;
            }

            // Resample to the correct rate 
            var resampledWave = new short[dpcmNumSamples];
            Resample(wave, minSample, maxSample, resampledWave);

            var dpcmSize = (dpcmNumSamples + 7) / 8; // Round up to byte. 
            dpcm = new byte[dpcmSize];

            // We might not (fully) write the last few bytes, so pre-fill.
            // DPCMTODO: Decide on 0xaa or 0x55 depending on odd/even number of raw samples.
            for (int i = dpcmSize - 1, j = 0; i >= 0 && j < 32; i--, j++)
                dpcm[i] = 0x55;

            // DPCM conversion.
            var dpcmCounter = dpcmCounterStart;

            for (int i = 0; i < resampledWave.Length; i++)
            {
                var waveSample = resampledWave[i];
                var dpcmSample = DpcmCounterToWaveSample(dpcmCounter);

                var index = i / 8;
                var mask  = (1 << (i & 7));

                // When samples are equal, look at the next one. This is helpful when re-converting back to DMC.
                if (dpcmSample < waveSample || (dpcmSample == waveSample && i != resampledWave.Length - 1 && resampledWave[i + 1] > waveSample))
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

        static public void GetDmcNonZeroVolumeRange(byte[] dmc, out int nonZeroMinByte, out int nonZeroMaxByte)
        {
            nonZeroMinByte = 0;
            nonZeroMaxByte = dmc.Length;

            // Very coarse, only remove entire byte alternative 0/1s.
            for (int i = 0; i < dmc.Length; i++)
            {
                if (dmc[i] != 0x55 && dmc[i] != 0xaa)
                {
                    nonZeroMinByte = i;
                    break;
                }
            }

            for (int i = dmc.Length - 1; i >= 0; i--)
            {
                if (dmc[i] != 0x55 && dmc[i] != 0xaa)
                {
                    nonZeroMaxByte = i + 1;
                    break;
                }
            }
        }

        static public void GetWaveNonZeroVolumeRange(short[] wave, int threshold, out int nonZeroMin, out int nonZeroMax)
        {
            nonZeroMin = 0;
            nonZeroMax = wave.Length - 1;

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
                    nonZeroMax = i + 1;
                    break;
                }
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

        static public void AdjustVolume(short[] wave, float volume)
        {
            for (int i = 0; i < wave.Length; i++)
            {
                wave[i] = (short)Utils.Clamp((int)Math.Round(wave[i] * volume), short.MinValue, short.MaxValue);
            }
        }
    }
}
