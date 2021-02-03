using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.IO;

namespace FamiStudio
{
    public enum DPCMPaddingMode
    {
        Unpadded,
        PadTo16Bytes,
        PadTo16BytesPlusOne,
        RoundTo16Bytes,
        RoundTo16BytesPlusOne
    };
    
    public class DPCMSample
    {
        // General properties.
        private int id;
        private string name;
        private Color color;

        // Source data
        private IDPCMSampleSourceData sourceData;

        // Processed data
        private byte[] processedData;
        private float minProcessingTime;
        private float maxProcessingTime;

        // Processing parameters.
        private int  sampleRate = 15;
        private int  previewRate = 15;
        private int  volumeAdjust = 100;
        private bool reverseBits;
        private bool trimZeroVolume;
        private DPCMPaddingMode paddingMode = DPCMPaddingMode.PadTo16Bytes;

        public int Id => id;
        public string Name { get => name; set => name = value; }
        public Color Color { get => color; set => color = value; }

        public bool SourceDataIsWav { get => sourceData is DPCMSampleWavSourceData; }
        public IDPCMSampleSourceData SourceData { get => sourceData; }
        public DPCMSampleWavSourceData SourceWavData { get => sourceData as DPCMSampleWavSourceData; }
        public DPCMSampleDmcSourceData SourceDmcData { get => sourceData as DPCMSampleDmcSourceData; }
        public float MinProcessingTime => minProcessingTime;
        public float MaxProcessingTime => maxProcessingTime;

        public byte[] ProcessedData  { get => processedData;  set => processedData  = value; }
        public int    SampleRate     { get => sampleRate;     set => sampleRate     = value; }
        public int    PreviewRate    { get => previewRate;    set => previewRate    = value; }
        public bool   ReverseBits    { get => reverseBits;    set => reverseBits    = value; }
        public bool   TrimZeroVolume { get => trimZeroVolume; set => trimZeroVolume = value; }
        public int    VolumeAdjust   { get => volumeAdjust;   set => volumeAdjust   = value; }
        public DPCMPaddingMode PaddingMode { get => paddingMode; set => paddingMode = value; }

        // DPCMTODO: Make those in the source data interface.

        // In seconds.
        public float SourceDuration
        {
            get
            {
                if (SourceDataIsWav)
                {
                    return SourceWavData.Samples.Length / (float)SourceWavData.SampleRate;
                }
                else
                {
                    return SourceDmcData.Data.Length * 8 / DpcmSampleRatesNtsc[DpcmSampleRatesNtsc.Length - 1]; // DPCMTODO: We assume the input sample rate here.
                }
            }
        }

        public float SourceSampleRate
        {
            get
            {
                if (SourceDataIsWav)
                {
                    return SourceWavData.SampleRate;
                }
                else
                {
                    return DpcmSampleRatesNtsc[DpcmSampleRatesNtsc.Length - 1]; // DPCMTODO: We assume the input sample rate here.
                }
            }
        }

        public float ProcessedDuration
        {
            get
            {
                return processedData.Length * 8 / DpcmSampleRatesNtsc[sampleRate];
            }
        }

        public DPCMSample()
        {
            // For serialization.
        }

        public DPCMSample(int id, string name)
        {
            this.id = id;
            this.name = name;
            this.color = ThemeBase.RandomCustomColor();
        }

        public void SetDmcSourceData(byte[] data)
        {
            sourceData  = new DPCMSampleDmcSourceData(data);
            paddingMode = DPCMPaddingMode.Unpadded;
        }

        public void SetWavSourceData(short[] data, int rate)
        {
            sourceData  = new DPCMSampleWavSourceData(data, rate);
            paddingMode = DPCMPaddingMode.PadTo16Bytes;
        }

        public void Process()
        {
            // DPCMTODO : Not right, user will be able to set the processing region maybe?
            minProcessingTime = 0;
            maxProcessingTime = SourceDuration;

            // DPCMTODO : What about PAL?
            var targetSampleRate = DpcmSampleRatesNtsc[sampleRate];

            // Fast path for when there is (almost) nothing to do.
            if (!SourceDataIsWav && volumeAdjust == 100 && !trimZeroVolume)
            {
                processedData = WaveUtils.CopyDpcm(SourceDmcData.Data);

                // Bit reverse.
                if (reverseBits)
                    WaveUtils.ReverseDpcmBits(processedData);
            }
            else
            {
                short[] sourceWavData;
                float sourceSampleRate;

                // DPCMTODO : Make this part of the source data interface.
                if (SourceDataIsWav)
                {
                    sourceWavData = WaveUtils.CopyWave(SourceWavData.Samples);
                    sourceSampleRate = SourceWavData.SampleRate;
                }
                else
                {
                    sourceSampleRate = DpcmSampleRatesNtsc[DpcmSampleRatesNtsc.Length - 1]; // DPCMTODO : Sample rate, is this right? What about PAL?

                    var dmcData = SourceDmcData.Data;

                    // Bit reverse.
                    if (reverseBits)
                    {
                        dmcData = WaveUtils.CopyDpcm(dmcData);
                        WaveUtils.ReverseDpcmBits(dmcData);
                    }

                    WaveUtils.DpcmToWave(dmcData, NesApu.DACDefaultValueDiv2, out sourceWavData);
                }

                if (volumeAdjust != 100)
                {
                    WaveUtils.AdjustVolume(sourceWavData, Math.Max(0, volumeAdjust) / 100.0f);
                }

                var minProcessingSample = 0;
                var maxProcessingSample = sourceWavData.Length;

                if (trimZeroVolume)
                {
                    WaveUtils.GetWaveNonZeroVolumeRange(sourceWavData, SourceDataIsWav ? 512 : 1024, out minProcessingSample, out maxProcessingSample);

                    minProcessingTime = minProcessingSample / sourceSampleRate;
                    maxProcessingTime = maxProcessingSample / sourceSampleRate;
                }

                var roundMode = WaveToDpcmRoundingMode.None;

                switch (paddingMode)
                {
                    case DPCMPaddingMode.RoundTo16Bytes:
                        roundMode = WaveToDpcmRoundingMode.RoundTo16Bytes;
                        break;
                    case DPCMPaddingMode.RoundTo16BytesPlusOne:
                        roundMode = WaveToDpcmRoundingMode.RoundTo16BytesPlusOne;
                        break;
                }

                WaveUtils.WaveToDpcm(sourceWavData, minProcessingSample, maxProcessingSample, sourceSampleRate, targetSampleRate, NesApu.DACDefaultValueDiv2, roundMode, out processedData); // DPCMTODO : hardcoded 33144
            }

            // If trimming is enabled, remove any extra 0x55 / 0xaa from the beginning and end.
            // We cannot do this on rounded samples since 
            if (trimZeroVolume && paddingMode != DPCMPaddingMode.RoundTo16Bytes && paddingMode != DPCMPaddingMode.RoundTo16BytesPlusOne)
            {
                WaveUtils.GetDmcNonZeroVolumeRange(processedData, out var minFinalNonZeroByte, out var maxFinalNonZeroByte);

                minProcessingTime += 8 * (minFinalNonZeroByte) / targetSampleRate;
                maxProcessingTime -= 8 * (processedData.Length - maxFinalNonZeroByte) / targetSampleRate;

                var untrimmedProcessedData = processedData;
                processedData = new byte[maxFinalNonZeroByte - minFinalNonZeroByte];
                Array.Copy(untrimmedProcessedData, minFinalNonZeroByte, processedData, 0, processedData.Length);
            }

            // Optional padding.
            if (paddingMode == DPCMPaddingMode.PadTo16Bytes ||
                paddingMode == DPCMPaddingMode.PadTo16BytesPlusOne)
            {
                var newSize = 0;

                switch (paddingMode)
                {
                    case DPCMPaddingMode.PadTo16Bytes:
                        newSize = Utils.RoundUp(processedData.Length, 16);
                        break;
                    case DPCMPaddingMode.PadTo16BytesPlusOne:
                        newSize = Utils.RoundUp(processedData.Length - 1, 16) + 1;
                        break;
                }

                var oldSize = processedData.Length;
                if (newSize != oldSize)
                {
                    Array.Resize(ref processedData, newSize);

                    // DPCMTODO: Look at last byte and decide if 0x55 or 0xaa is better.
                    for (int i = oldSize; i < newSize; i++)
                        processedData[i] = 0x55;
                }
            }
        }

        public void ChangeId(int newId)
        {
            id = newId;
        }

        public void Validate(Project project, Dictionary<int, object> idMap)
        {
#if DEBUG
            project.ValidateId(id);

            if (idMap.TryGetValue(id, out var foundObj))
                Debug.Assert(foundObj == this);
            else
                idMap.Add(id, this);

            Debug.Assert(project.GetSample(id) == this);
#endif
        }

        // DPCMTODO : Remove this!
        public byte[] GetDataWithReverse()
        {
            //if (reverseBits)
            //{
            //    var copy = processedData.Clone() as byte[];
            //    Utils.ReverseBits(copy);
            //    return copy;
            //}
            //else
            {
                return processedData;
            }
        }

        // At version 9 (FamiStudio 2.4.0) we added a proper DPCM sample editor
        // and refactored the DPCM samples a bit. 
        public void SerializeStatePreVer9(ProjectBuffer buffer)
        {
            sourceData = new DPCMSampleDmcSourceData();
            sourceData.SerializeState(buffer);

            // At version 8 (FamiStudio 2.3.0) we added an explicit "reverse bit" flag.
            if (buffer.Version >= 8)
            {
                buffer.Serialize(ref reverseBits);
            }

            color = ThemeBase.RandomCustomColor();
            paddingMode = DPCMPaddingMode.Unpadded;

            // Process to apply bit reverse, etc.
            Process();
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref id);
            buffer.Serialize(ref name);

            // At version 9 (FamiStudio 2.4.0) we added a proper DPCM sample editor.
            if (buffer.Version < 9)
            {
                SerializeStatePreVer9(buffer);
            }
            else
            {
                bool sourceDataIsWav = SourceDataIsWav;
                buffer.Serialize(ref sourceDataIsWav);

                if (buffer.IsReading)
                {
                    if (sourceDataIsWav)
                        sourceData = new DPCMSampleWavSourceData();
                    else
                        sourceData = new DPCMSampleDmcSourceData();
                }

                sourceData.SerializeState(buffer);
                buffer.Serialize(ref color);
                buffer.Serialize(ref processedData);
            }
        }

        // From NESDEV wiki.
        public static float[] DpcmSampleRatesNtsc =
        {
            4181.71f,
            4709.93f,
            5264.04f,
            5593.04f,
            6257.95f,
            7046.35f,
            7919.35f,
            8363.42f,
            9419.86f,
            11186.1f,
            12604.0f,
            13982.6f,
            16884.6f,
            21306.8f,
            24858.0f,
            33143.9f
        };

        public static float[] DpcmSampleRatesPal =
        {
            4177.40f,
            4696.63f,
            5261.41f,
            5579.22f,
            6023.94f,
            7044.94f,
            7917.18f,
            8397.01f,
            9446.63f,
            11233.8f,
            12595.5f,
            14089.9f,
            16965.4f,
            21315.5f,
            25191.0f,
            33252.1f
        };
    }

    public class DPCMSampleMapping
    {
        private DPCMSample sample;
        private bool loop = false;
        private int pitch = 15;

        public DPCMSample Sample { get => sample; set => sample = value; }
        public bool Loop { get => loop; set => loop = value; }
        public int Pitch { get => pitch; set => pitch = value; }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref sample);
            buffer.Serialize(ref loop);
            buffer.Serialize(ref pitch);
        }
    }

    public interface IDPCMSampleSourceData
    {
        void SerializeState(ProjectBuffer buffer);
        bool Trim(float timeStart, float timeEnd);
        int NumSamples { get; }
    }

    public class DPCMSampleWavSourceData : IDPCMSampleSourceData
    {
        int sampleRate;
        short[] wavData;

        public int     SampleRate => sampleRate;
        public short[] Samples    => wavData;
        public int     NumSamples => wavData.Length;

        public DPCMSampleWavSourceData()
        {
        }

        public DPCMSampleWavSourceData(short[] data, int rate)
        {
            wavData    = data;
            sampleRate = rate;
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref sampleRate);
            buffer.Serialize(ref wavData);
        }

        public bool Trim(float timeStart, float timeEnd)
        {
            return WaveUtils.TrimWave(ref wavData, sampleRate, timeStart, timeEnd);
        }
    }

    public class DPCMSampleDmcSourceData : IDPCMSampleSourceData
    {
        byte[] dmcData;

        public byte[] Data       => dmcData;
        public int    NumSamples => dmcData.Length * 8;

        public DPCMSampleDmcSourceData()
        {
        }

        public DPCMSampleDmcSourceData(byte[] data)
        {
            dmcData = data;
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref dmcData);
        }

        public bool Trim(float timeStart, float timeEnd)
        {
            // DPCMTODO : TODO!
            return false;
        }
    }
}
