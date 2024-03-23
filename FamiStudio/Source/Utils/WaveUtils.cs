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

    public struct SampleVolumePair
    {
        public SampleVolumePair(int s, float v = 1.0f)
        {
            sample = s;
            volume = v;
        }

        public int   sample;
        public float volume;
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

        static public short[] ResampleStream(short[] prev, short[] curr, int inputRate, int outputRate, bool stereo, ref double sampleIndex)
        {
            if (inputRate == outputRate)
            {
                return curr;
            }

            var channelCount = stereo ? 2 : 1;
            var frameCount = curr.Length / channelCount;

            Debug.Assert(curr.Length % channelCount == 0);
            Debug.Assert(prev == null || prev.Length % channelCount == 0);

            var ratio = inputRate / (double)outputRate;
            var outputCount = (int)((frameCount - 1) / ratio);

            // TODO : There has to be a more clever formula...
            while (Math.Ceiling(sampleIndex + outputCount * ratio) < frameCount)
                outputCount++;

            var output = new short[outputCount * channelCount];

            for (var i = 0; i < outputCount; i++)
            {
                var alpha = (sampleIndex < 0.0 ? 1.0 : 0.0) + Utils.Frac(sampleIndex);

                {
                    var idx0 = (int)Math.Floor(sampleIndex)   * channelCount;
                    var idx1 = (int)Math.Ceiling(sampleIndex) * channelCount;
                    var s0 = idx0 < 0 ? prev[prev.Length + idx0] : curr[idx0];
                    var s1 = idx1 < 0 ? prev[prev.Length + idx1] : curr[idx1];

                    output[i * channelCount + 0] = (short)Utils.Lerp(s0, s1, alpha);
                }

                if (stereo)
                {
                    var idx0 = (int)Math.Floor(sampleIndex)   * channelCount + 1;
                    var idx1 = (int)Math.Ceiling(sampleIndex) * channelCount + 1;
                    var s0 = idx0 < 0 ? prev[prev.Length + idx0] : curr[idx0];
                    var s1 = idx1 < 0 ? prev[prev.Length + idx1] : curr[idx1];

                    output[i * channelCount + 1] = (short)Utils.Lerp(s0, s1, alpha);
                }

                sampleIndex += ratio;
            }

            sampleIndex -= frameCount;
            
            return output;
        }

        static public short[] ResampleBuffer(short[] source, int inputRate, int outputRate, bool stereo)
        {
            var ratio = inputRate / (double)outputRate;
            var sampleIndex = 0.0;
            var channelCount = stereo ? 2 : 1;
            var inputFrameCount = source.Length / channelCount;
            var outputFrameCount = (int)(inputFrameCount / ratio);
            var output = new short[outputFrameCount * channelCount];

            for (var i = 0; i < outputFrameCount * channelCount; i += channelCount)
            {
                var alpha = Math.Abs(Utils.Frac(sampleIndex));

                var idx0 = Math.Min((int)Math.Floor  (sampleIndex), inputFrameCount - 1) * channelCount;
                var idx1 = Math.Min((int)Math.Ceiling(sampleIndex), inputFrameCount - 1) * channelCount;

                {
                    var s0 = source[idx0];
                    var s1 = source[idx1];

                    output[i] = (short)Utils.Lerp(s0, s1, alpha);
                }

                if (stereo)
                {
                    var s0 = source[idx0 + 1];
                    var s1 = source[idx1 + 1];

                    output[i + 1] = (short)Utils.Lerp(s0, s1, alpha);
                }

                sampleIndex += ratio;
            }

            return output;
        }

        static public void Normalize(short[] wav)
        {
            var min = short.MaxValue;
            var max = short.MinValue;

            for (int i = 0; i < wav.Length; i++)
            {
                var val = wav[i];
                min = Math.Min(min, val);
                max = Math.Max(max, val);
            }

            var ratio = 32767.0f / Math.Max(-min, max);

            for (int i = 0; i < wav.Length; i++)
            {
                wav[i] = (short)Math.Round(wav[i] * ratio);
            }
        }

        static public int DpcmCounterToWaveSample(int counter)
        {
            Debug.Assert(counter >= 0 && counter < 64);
            return counter * (65536 / 64) - 32768;
        }

        static public byte[] CopyDpcm(byte[] data)
        {
            var newData = new byte[data.Length];
            Array.Copy(data, newData, newData.Length);
            return newData;
        }

        static public short[] CopyWave(short[] data)
        {
            var newData = new short[data.Length];
            Array.Copy(data, newData, newData.Length);
            return newData;
        }

        static public void ReverseDpcmBits(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = Utils.ReverseBits(data[i]);
        }

        static public void WaveToDpcm(short[] wave, int minSample, int maxSample, float waveSampleRate, float dpcmSampleRate, int dpcmCounterStart, WaveToDpcmRoundingMode roundMode, out byte[] dpcm)
        {
            if (wave.Length == 0)
            {
                dpcm = new byte[0];
                return;
            }

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

            // DPCM conversion.
            var dpcmCounter = dpcmCounterStart;
            var lastBit = 0;

            for (int i = 0; i < resampledWave.Length; i++)
            {
                var up = false;

                if (i != resampledWave.Length - 1)
                {
                    // Is it better to go up or down?
                    var distUp   = Math.Abs(DpcmCounterToWaveSample(Math.Min(dpcmCounter + 1, 63)) - resampledWave[i + 1]);
                    var distDown = Math.Abs(DpcmCounterToWaveSample(Math.Max(dpcmCounter - 1,  0)) - resampledWave[i + 1]);

                    up = distUp < distDown;
                }
                else
                {
                    up = DpcmCounterToWaveSample(dpcmCounter) < resampledWave[i];
                }

                if (up)
                {
                    var index = i / 8;
                    var mask  = (1 << (i & 7));

                    dpcm[index] |= (byte)mask;
                    dpcmCounter = Math.Min(dpcmCounter + 1, 63);
                    lastBit = 1;
                }
                else
                {
                    dpcmCounter = Math.Max(dpcmCounter - 1, 0);
                    lastBit = 0;
                }
            }

            // We might not (fully) write the last byte, so fill with 0x55 or 0xaa.
            for (int i = resampledWave.Length; i < Utils.RoundUp(resampledWave.Length, 8); i++)
            {
                if (lastBit == 0)
                {
                    var index = i / 8;
                    var mask = (1 << (i & 7));

                    dpcm[index] |= (byte)mask;
                }

                lastBit ^= 1;
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
                    wave[i * 8 + j] = (short)DpcmCounterToWaveSample(dpcmCounter);

                    var mask = 1 << j;

                    if ((dpcm[i] & (byte)mask) != 0)
                        dpcmCounter = Math.Min(dpcmCounter + 1, 63);
                    else
                        dpcmCounter = Math.Max(dpcmCounter - 1, 0);
                }
            }
        }

        // [min, max]
        static public bool TrimWave(ref short[] wave, int trimSampleMin, int trimSampleMax)
        {
            trimSampleMax++;
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

        // [min, max]
        static public bool TrimDmc(ref byte[] dmc, int trimSampleMin, int trimSampleMax)
        {
            trimSampleMax++;

            int trimByteMin = trimSampleMin / 8;
            int trimByteMax = trimSampleMax / 8;

            if (trimByteMin < trimByteMax)
            {
                var newLength = trimByteMin + (dmc.Length - trimByteMax);
                var newDmcData = new byte[newLength];

                if (trimByteMin > 0)
                    Array.Copy(dmc, newDmcData, trimByteMin);
                if (trimByteMax < dmc.Length)
                    Array.Copy(dmc, trimByteMax, newDmcData, trimByteMin, dmc.Length - trimByteMax);

                dmc = newDmcData;

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

            // Very coarse, only remove entire byte of alternating 0/1s.
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
            nonZeroMax = 0;

            for (int i = 0; i < wave.Length; i++)
            {
                if (Math.Abs((int)wave[i]) > threshold)
                {
                    nonZeroMin = i;
                    break;
                }
            }

            for (int i = wave.Length - 1; i >= 0; i--)
            {
                if (Math.Abs((int)wave[i]) > threshold)
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
                if (Math.Abs((int)wave[i]) > threshold)
                {
                    nonZeroMin = i;
                    break;
                }
            }

            for (int i = wave.Length - 1; i >= 0; i--)
            {
                if (Math.Abs((int)wave[i]) > threshold)
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

        // volumeEnvelope = series of time/volume pairs. 
        static public void AdjustVolume(short[] wave, List<SampleVolumePair> volumeEnvelope)
        {
            // Enforce the first/last times to cover the entire range.
            Debug.Assert(volumeEnvelope[0].sample == 0);
            Debug.Assert(volumeEnvelope[volumeEnvelope.Count - 1].sample == wave.Length - 1);

            // Simple smoothstep interpolation (which is equivalent to cosine interpolation).
            for (int i = 0; i < volumeEnvelope.Count - 1; i++)
            {
                var s0 = Utils.Clamp(volumeEnvelope[i + 0].sample, 0, wave.Length - 1);
                var s1 = Utils.Clamp(volumeEnvelope[i + 1].sample, 0, wave.Length - 1);
                var v0 = volumeEnvelope[i + 0].volume;
                var v1 = volumeEnvelope[i + 1].volume;

                for (int j = s0; j <= s1; j++)
                {
                    var ratio  = s0 == s1 ? 0.0f : (j - s0) / (float)(s1 - s0);
                    var volume = Utils.Lerp(v0, v1, Utils.SmoothStep(ratio));

                    wave[j] = (short)Utils.Clamp((int)Math.Round(wave[j] * volume), short.MinValue, short.MaxValue);
                }
            }
        }

        static private float Sinc(float x)
        {
            if (x == 0)
            {
                return 1.0f;
            }
            else
            {
                return (float)(Math.Sin(Math.PI * x) / (Math.PI * x));
            }
        }

        // Based off this great article : https://tomroelandts.com/articles/how-to-create-a-simple-low-pass-filter
        // cutoff and transition are expressed as fractions of the sample rate (ex: 1000 / 44100 = 0.22)

        // TODO : Convert all this to biquad filter.
        static public void LowPassFilter(ref short[] wave, float cutoff, float transition, int maxFilterSize = int.MaxValue)
        {
            var n = Math.Min(maxFilterSize, (int)Math.Ceiling(4.6f / transition));
            if ((n & 1) == 0) n++;
            var filter = new float[n];
            var sum = 0.0f;

            // Build filter (sinc + blackman window)
            for (int i = 0; i < n; i++)
            {
                float sinc = Sinc(2.0f * cutoff * (i - (n - 1) / 2));
                float window = (0.42f - 0.5f * (float)Math.Cos((2.0f * Math.PI * i) / (n - 1)) + 0.08f * (float)Math.Cos(4.0f * Math.PI * i / (n - 1)));
                float h = sinc * window;

                filter[i] = h;
                sum += h;
            }

            // Normalize.
            for (int i = 0; i < n; i++)
            {
                filter[i] /= sum;
            }

            // Convolution.
            short[] filteredWave = new short[wave.Length];

            int n2 = n / 2;
            for (int i = 0; i < wave.Length; i++)
            {
                var v = 0.0f;

                for (int j = 0; j < n; j++)
                {
                    var index = i + (j - n2);
                    if (index >= 0 && index < wave.Length)
                        v += wave[i + (j - n2)] * filter[j];
                }

                filteredWave[i] = (short)Math.Round(Utils.Clamp(v, short.MinValue, short.MaxValue));
            }

            wave = filteredWave;
        }

        public static void MixDown(short[] stereo, short[] mono)
        {
            for (int i = 0; i < stereo.Length; i += 2)
            {
                mono[i / 2] = (short)((stereo[i + 0] + stereo[i + 1]) / 2);
            }
        }

        public static short[] MixDown(short[] stereo)
        {
            var mono = new short[stereo.Length / 2];
            MixDown(stereo, mono);
            return mono;
        }

        public static int GetMaxAbsValue(short[] samples)
        {
            var maxAbsSample = 0;
            foreach (int s in samples)
                maxAbsSample = Math.Max(maxAbsSample, Math.Abs(s));
            return maxAbsSample;
        }
    }
}
