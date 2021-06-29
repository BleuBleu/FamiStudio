using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.IO;

namespace FamiStudio
{
    public class DPCMSample
    {
        // General properties.
        private int id;
        private string name;
        private Color color;

        // Source data
        private string sourceFilename = "";
        private IDPCMSampleSourceData sourceData;

        // Processed data 
        private byte[] processedData;
        private float  processedDataStartTime;

        // Processing parameters.
        private int   sampleRate = 15;
        private int   previewRate = 15;
        private int   volumeAdjust = 100;
        private float finePitch = 1.0f;
        private int   paddingMode = DPCMPaddingType.PadTo16Bytes;
        private bool  reverseBits;
        private bool  trimZeroVolume;
        private bool  palProcessing;
        private SampleVolumePair[] volumeEnvelope = new SampleVolumePair[4]
        {
            new SampleVolumePair(0, 1.0f),
            new SampleVolumePair(0, 1.0f),
            new SampleVolumePair(0, 1.0f),
            new SampleVolumePair(0, 1.0f)
        };

        // Properties.
        public int Id => id;
        public string Name { get => name; set => name = value; }
        public Color Color { get => color; set => color = value; }

        public string SourceFilename => sourceFilename;
        public bool SourceDataIsWav { get => sourceData is DPCMSampleWavSourceData; }
        public IDPCMSampleSourceData  SourceData { get => sourceData; }
        public DPCMSampleWavSourceData SourceWavData { get => sourceData as DPCMSampleWavSourceData; }
        public DPCMSampleDmcSourceData SourceDmcData { get => sourceData as DPCMSampleDmcSourceData; }

        public float ProcessedStartTime  => processedDataStartTime;
        public float ProcessedEndTime    => processedDataStartTime + ProcessedDuration;
        public float ProcessedSampleRate => DPCMSampleRate.Frequencies[palProcessing ? 1 : 0, sampleRate];
        public float ProcessedDuration   => processedData.Length * 8 / ProcessedSampleRate;

        public byte[]   ProcessedData  { get => processedData;  set => processedData  = value; }
        public int      SampleRate     { get => sampleRate;     set => sampleRate     = value; }
        public int      PreviewRate    { get => previewRate;    set => previewRate    = value; }
        public bool     ReverseBits    { get => reverseBits;    set => reverseBits    = value; }
        public bool     TrimZeroVolume { get => trimZeroVolume; set => trimZeroVolume = value; }
        public bool     PalProcessing  { get => palProcessing;  set => palProcessing  = value; }
        public int      VolumeAdjust   { get => volumeAdjust;   set => volumeAdjust   = value; }
        public float    FinePitch      { get => finePitch;      set => finePitch      = value; }
        public int      PaddingMode    { get => paddingMode;    set => paddingMode    = value; }
        public SampleVolumePair[] VolumeEnvelope { get => volumeEnvelope; }

        public bool HasAnyProcessingOptions => SourceDataIsWav || sampleRate != 15 || volumeAdjust != 100 || Utils.IsNearlyEqual(finePitch, 1.0f) || trimZeroVolume || reverseBits;

        public static object ProcessedDataLock = new object();
        public const int MaxSampleSize = (255 << 4) + 1;

        public int   SourceNumSamples => sourceData.NumSamples;
        public int   SourceDataSize   => sourceData.DataSize;
        public float SourceSampleRate => sourceData.GetSampleRate(palProcessing);
        public float SourceDuration   => sourceData.GetDuration(palProcessing);

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

        public float GetPlaybackSampleRate(bool palPlayback)
        {
            return DPCMSampleRate.Frequencies[palPlayback ? 1 : 0, previewRate];
        }

        public float GetPlaybackDuration(bool palPlayback)
        {
            return processedData.Length * 8 / GetPlaybackSampleRate(palPlayback);
        }

        public void SetDmcSourceData(byte[] data, string filename = null)
        {
            sourceData = new DPCMSampleDmcSourceData(data);
            sourceFilename = filename;
            paddingMode = DPCMPaddingType.Unpadded;
            ResetVolumeEnvelope();
        }

        public void SetWavSourceData(short[] data, int rate, string filename = null)
        {
            sourceData = new DPCMSampleWavSourceData(data, rate);
            sourceFilename = filename;
            paddingMode = DPCMPaddingType.PadTo16Bytes;
            ResetVolumeEnvelope();
        }

        public bool TrimSourceSourceData(int sampleStart, int sampleEnd)
        {
            bool trimmed = sourceData.Trim(sampleStart, sampleEnd);
            ClampVolumeEnvelope();
            return trimmed;
        }

        public void PermanentlyApplyAllProcessing()
        {
            SetDmcSourceData(processedData);
            sampleRate = 15; 
            previewRate = 15;
            volumeAdjust = 100;
            finePitch = 1.0f;
            trimZeroVolume = false;
            reverseBits = false;
            palProcessing = false;
            paddingMode = DPCMPaddingType.Unpadded;
            ResetVolumeEnvelope();
        }

        public bool UsesVolumeEnvelope
        {
            get
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Math.Abs(volumeEnvelope[i].volume - 1.0f) > 0.02f)
                        return true;
                }

                return false;
            }
        }

        public void Process()
        {
            lock (ProcessedDataLock)
            {
                processedDataStartTime = 0;

                var targetSampleRate = DPCMSampleRate.Frequencies[palProcessing ? 1 : 0, sampleRate];

                // Fast path for when there is (almost) nothing to do.
                if (!SourceDataIsWav && volumeAdjust == 100 && Utils.IsNearlyEqual(finePitch, 1.0f) && !UsesVolumeEnvelope && sampleRate == 15 && !trimZeroVolume)
                {
                    processedData = WaveUtils.CopyDpcm(SourceDmcData.Data);

                    // Bit reverse.
                    if (reverseBits)
                        WaveUtils.ReverseDpcmBits(processedData);
                }
                else
                {
                    short[] sourceWavData;
                    float sourceSampleRate = sourceData.GetSampleRate(palProcessing);

                    if (SourceDataIsWav)
                    {
                        sourceWavData = WaveUtils.CopyWave(SourceWavData.Samples);
                    }
                    else
                    {
                        var dmcData = SourceDmcData.Data;

                        // Bit reverse.
                        if (reverseBits)
                        {
                            dmcData = WaveUtils.CopyDpcm(dmcData);
                            WaveUtils.ReverseDpcmBits(dmcData);
                        }

                        WaveUtils.DpcmToWave(dmcData, NesApu.DACDefaultValueDiv2, out sourceWavData);
                    }

                    if (!Utils.IsNearlyEqual(finePitch, 1.0f))
                    {
                        var newLength = (int)Math.Round(sourceWavData.Length * finePitch);
                        if (newLength != sourceWavData.Length)
                        {
                            var resampledWavData = new short[newLength];
                            WaveUtils.Resample(sourceWavData, 0, sourceWavData.Length, resampledWavData);
                            sourceWavData = resampledWavData;
                        }
                    }

                    /*
                    if (lowPassFilter > 0)
                    {
                        var cutoff = Utils.Lerp(0.25f, 0.1f, lowPassFilter / 100.0f);
                        cutoff = (float)Math.Pow(cutoff * 2, 2.0f) / 2.0f;
                        WaveUtils.LowPassFilter(ref sourceWavData, cutoff, cutoff * 0.5f);
                    }
                    */

                    if (UsesVolumeEnvelope)
                    {
                        var envelope = new List<SampleVolumePair>();

                        volumeEnvelope[0].sample = 0;
                        volumeEnvelope[volumeEnvelope.Length - 1].sample = SourceNumSamples - 1;

                        envelope.Add(new SampleVolumePair(volumeEnvelope[0].sample, volumeEnvelope[0].volume * (volumeAdjust / 100.0f)));

                        for (int i = 1; i < 4; i++)
                        {
                            if (volumeEnvelope[i].sample != envelope[envelope.Count - 1].sample)
                            {
                                envelope.Add(new SampleVolumePair(volumeEnvelope[i].sample, volumeEnvelope[i].volume * (volumeAdjust / 100.0f)));
                            }
                        }

                        WaveUtils.AdjustVolume(sourceWavData, envelope);
                    }
                    else if (volumeAdjust != 100)
                    {
                        WaveUtils.AdjustVolume(sourceWavData, Math.Max(0, volumeAdjust) / 100.0f);
                    }

                    var minProcessingSample = 0;
                    var maxProcessingSample = sourceWavData.Length;

                    if (trimZeroVolume)
                    {
                        WaveUtils.GetWaveNonZeroVolumeRange(sourceWavData, SourceDataIsWav ? 512 : 1024, out minProcessingSample, out maxProcessingSample);
                        processedDataStartTime = minProcessingSample / sourceSampleRate;
                    }

                    var roundMode = WaveToDpcmRoundingMode.None;

                    switch (paddingMode)
                    {
                        case DPCMPaddingType.RoundTo16Bytes:
                            roundMode = WaveToDpcmRoundingMode.RoundTo16Bytes;
                            break;
                        case DPCMPaddingType.RoundTo16BytesPlusOne:
                            roundMode = WaveToDpcmRoundingMode.RoundTo16BytesPlusOne;
                            break;
                    }

                    WaveUtils.WaveToDpcm(sourceWavData, minProcessingSample, maxProcessingSample, sourceSampleRate, targetSampleRate, NesApu.DACDefaultValueDiv2, roundMode, out processedData);
                }

                // If trimming is enabled, remove any extra 0x55 / 0xaa from the beginning and end.
                // We cannot do this on rounded samples since 
                if (trimZeroVolume && paddingMode != DPCMPaddingType.RoundTo16Bytes && paddingMode != DPCMPaddingType.RoundTo16BytesPlusOne)
                {
                    WaveUtils.GetDmcNonZeroVolumeRange(processedData, out var minFinalNonZeroByte, out var maxFinalNonZeroByte);

                    processedDataStartTime += 8 * (minFinalNonZeroByte) / targetSampleRate;

                    var untrimmedProcessedData = processedData;
                    processedData = new byte[maxFinalNonZeroByte - minFinalNonZeroByte];
                    Array.Copy(untrimmedProcessedData, minFinalNonZeroByte, processedData, 0, processedData.Length);
                }

                // Optional padding.
                if (paddingMode == DPCMPaddingType.PadTo16Bytes ||
                    paddingMode == DPCMPaddingType.PadTo16BytesPlusOne)
                {
                    var newSize = 0;

                    switch (paddingMode)
                    {
                        case DPCMPaddingType.PadTo16Bytes:
                            newSize = Utils.RoundUp(processedData.Length, 16);
                            break;
                        case DPCMPaddingType.PadTo16BytesPlusOne:
                            newSize = Utils.RoundUp(processedData.Length - 1, 16) + 1;
                            break;
                    }

                    var oldSize = processedData.Length;
                    if (newSize != oldSize)
                    {
                        var lastByte = processedData.Length > 0 ? processedData[processedData.Length - 1] : 0;
                        Array.Resize(ref processedData, newSize);

                        var fillValue = (byte)((lastByte & 0x80) != 0 ? 0xaa : 0x55);
                        for (int i = oldSize; i < newSize; i++)
                            processedData[i] = fillValue;
                    }
                }

                // Clamp to max length.
                if (processedData.Length > MaxSampleSize)
                {
                    Array.Resize(ref processedData, DPCMSample.MaxSampleSize);
                }
            }
        }

        public void ChangeId(int newId)
        {
            id = newId;
        }

        public void ValidateIntegrity(Project project, Dictionary<int, object> idMap)
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

        public void SortVolumeEnvelope(ref int idx)
        {
            ClampVolumeEnvelope();

            if (volumeEnvelope[1].sample > volumeEnvelope[2].sample)
            {
                Utils.Swap(ref volumeEnvelope[1], ref volumeEnvelope[2]);
                if (idx == 1) idx = 2;
                else idx = 1;
            }
        }

        private void ResetVolumeEnvelope()
        {
            volumeEnvelope[0] = new SampleVolumePair(0);
            volumeEnvelope[1] = new SampleVolumePair((int)Math.Round(SourceNumSamples * (1.0f / 3.0f)));
            volumeEnvelope[2] = new SampleVolumePair((int)Math.Round(SourceNumSamples * (2.0f / 3.0f)));
            volumeEnvelope[3] = new SampleVolumePair(SourceNumSamples - 1);
        }

        private void ClampVolumeEnvelope()
        {
            volumeEnvelope[0].sample = 0;
            volumeEnvelope[3].sample = SourceNumSamples - 1;

            for (int i = 1; i <= 2; i++)
            {
                volumeEnvelope[i].sample = Math.Min(SourceNumSamples - 1, volumeEnvelope[i].sample);
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
            paddingMode = DPCMPaddingType.Unpadded;

            ResetVolumeEnvelope();

            // Process to apply bit reverse, etc.
            Process();
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref id, true);
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

                // Processing parameters.
                buffer.Serialize(ref sampleRate);
                buffer.Serialize(ref previewRate);
                buffer.Serialize(ref volumeAdjust);
                buffer.Serialize(ref paddingMode);
                buffer.Serialize(ref reverseBits);
                buffer.Serialize(ref trimZeroVolume);
                buffer.Serialize(ref palProcessing);

                for (int i = 0; i < volumeEnvelope.Length; i++)
                {
                    buffer.Serialize(ref volumeEnvelope[i].sample);
                    buffer.Serialize(ref volumeEnvelope[i].volume);
                }

                // At version 10 (FamiStudio 3.0.0) we added the filename of the source asset.
                if (buffer.Version >= 10)
                {
                    buffer.Serialize(ref sourceFilename);
                }

                if (buffer.Version >= 11)
                {
                    // MATTT : Serialize this once its finished.
                    //buffer.Serialize(ref finePitch);
                }

                // The process data will not be stored in the file, it will 
                // be reprocessed every time we reopen the file.
                if (buffer.IsForUndoRedo)
                {
                    buffer.Serialize(ref processedData);
                }
                else if (buffer.IsReading)
                {
                    Process();
                }
            }
        }

        // [0] = NTSC
        // [1] = PAL
        public static float[] DpcmSampleMaximumRate =
        {
            33143.9f,
            33252.1f
        };
    }

    public class DPCMSampleMapping
    {
        private DPCMSample sample;
        private bool loop = false;
        private int pitch = 15;

        public DPCMSample Sample { get => sample; set => sample = value; }
        public bool Loop { get => loop;  set => loop  = value; }
        public int Pitch { get => pitch; set => pitch = value; }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref sample);
            buffer.Serialize(ref loop);
            buffer.Serialize(ref pitch);
        }

        public void ValidateIntegrity(Project project, Dictionary<int, object> idMap)
        {
            if (sample != null)
            {
                Debug.Assert(project.GetSample(sample.Id) == sample);
                Debug.Assert(project.Samples.Contains(sample));
            }

        }
    }

    public interface IDPCMSampleSourceData
    {
        void SerializeState(ProjectBuffer buffer);
        bool Trim(int sampleStart, int sampleEnd);
        float GetSampleRate(bool pal);
        float GetDuration(bool pal);
        int DataSize { get; }
        int NumSamples { get; }
    }

    public class DPCMSampleWavSourceData : IDPCMSampleSourceData
    {
        private int     sampleRate;
        private short[] wavData;

        public int     DataSize   => wavData.Length * 2;
        public int     NumSamples => wavData.Length;
        public int     SampleRate => sampleRate;
        public short[] Samples    => wavData;

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

        public bool Trim(int sampleStart, int sampleEnd)
        {
            return WaveUtils.TrimWave(ref wavData, sampleStart, sampleEnd);
        }

        public float GetSampleRate(bool pal)
        {
            return sampleRate;
        }

        public float GetDuration(bool pal)
        {
            return (wavData.Length - 1) / (float)sampleRate;
        }
    }

    public class DPCMSampleDmcSourceData : IDPCMSampleSourceData
    {
        private byte[] dmcData;

        public byte[] Data       => dmcData;
        public int    DataSize   => dmcData.Length;
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

        public bool Trim(int sampleStart, int sampleEnd)
        {
            return WaveUtils.TrimDmc(ref dmcData, sampleStart, sampleEnd);
        }

        public float GetSampleRate(bool pal)
        {
            return DPCMSample.DpcmSampleMaximumRate[pal ? 1 : 0];
        }

        public float GetDuration(bool pal)
        {
            return dmcData.Length * 8 / GetSampleRate(pal);
        }
    }

    public static class DPCMPaddingType
    {
        public const int Unpadded              = 0;
        public const int PadTo16Bytes          = 1;
        public const int PadTo16BytesPlusOne   = 2;
        public const int RoundTo16Bytes        = 3;
        public const int RoundTo16BytesPlusOne = 4;

        public static readonly string[] Names =
        {
            "Unpadded",
            "Pad to 16",
            "Pad to 16+1",
            "Round to 16",
            "Round to 16+1"
        };

        public static int GetValueForName(string str)
        {
            return Array.IndexOf(Names, str);
        }
    };
    
    public static class DPCMSampleRate
    {
        // From NESDEV wiki.
        // [0,x] = NTSC
        // [1,x] = PAL
        public static readonly float[,] Frequencies =
        {
            // NTSC
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
            },
            // PAL
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
            }
        };

        // From NESDEV wiki.
        // [0,x] = NTSC
        // [1,x] = PAL
        public static readonly string[][] Strings =
        {
            // NTSC
            new [] {
                "0 (4.2 KHz)",
                "1 (4.7 KHz)",
                "2 (5.3 KHz)",
                "3 (5.6 KHz)",
                "4 (6.3 KHz)",
                "5 (7.0 KHz)",
                "6 (7.9 KHz)",
                "7 (8.3 KHz)",
                "8 (9.4 KHz)",
                "9 (11.1 KHz)",
                "10 (12.6 KHz)",
                "11 (13.9 KHz)",
                "12 (16.9 KHz)",
                "13 (21.3 KHz)",
                "14 (24.9 KHz)",
                "15 (33.1 KHz)"
            },
            // PAL
            new [] {
                "0 (4.2 KHz)",
                "1 (4.7 KHz)",
                "2 (5.3 KHz)",
                "3 (5.6 KHz)",
                "4 (6.0 KHz)",
                "5 (7.0 KHz)",
                "6 (7.9 KHz)",
                "7 (8.4 KHz)",
                "8 (9.4 KHz)",
                "9 (11.2 KHz)",
                "10 (12.6 KHz)",
                "11 (14.1 KHz)",
                "12 (17.0 KHz)",
                "13 (21.3 KHz)",
                "14 (25.2 KHz)",
                "15 (33.3 KHz)"
            }
        };

        public static int GetIndexForName(bool pal, string str)
        {
            return Array.IndexOf(Strings[pal ? 1 : 0], str);
        }
    };
}
