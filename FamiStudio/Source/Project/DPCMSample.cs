using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.IO;
using System.Globalization;

namespace FamiStudio
{
    public class DPCMSample
    {
        // General properties.
        private int id;
        private string name;
        private Color color;
        private string folderName;
        private Project project;
        private int bank = 0;

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
        private int   dmcInitialValueDiv2 = NesApu.DACDefaultValueDiv2;
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
        public string Name { get => name;  set => name  = value; }
        public Color Color { get => color; set => color = value; }
        public int Bank    { get => bank;  set => bank  = value; }
        public string FolderName { get => folderName; set => folderName = value; }
        public Folder Folder => string.IsNullOrEmpty(folderName) ? null : project.GetFolder(FolderType.Sample, folderName);
        public string NameWithFolder => (string.IsNullOrEmpty(folderName) ? "" : $"{folderName}\\") + name;

        public string SourceFilename => sourceFilename;
        public bool SourceDataIsWav { get => sourceData is DPCMSampleWavSourceData; }
        public IDPCMSampleSourceData  SourceData { get => sourceData; }
        public DPCMSampleWavSourceData SourceWavData { get => sourceData as DPCMSampleWavSourceData; }
        public DPCMSampleDmcSourceData SourceDmcData { get => sourceData as DPCMSampleDmcSourceData; }

        public float ProcessedStartTime  => processedDataStartTime;
        public float ProcessedEndTime    => processedDataStartTime + ProcessedDuration;
        public float ProcessedSampleRate => DPCMSampleRate.Frequencies[palProcessing ? 1 : 0, sampleRate];
        public float ProcessedDuration   => processedData.Length * 8 / ProcessedSampleRate;

        public byte[]   ProcessedData       { get => processedData;       set => processedData   = value; }
        public int      SampleRate          { get => sampleRate;          set => sampleRate      = value; }
        public int      PreviewRate         { get => previewRate;         set => previewRate     = value; }
        public bool     ReverseBits         { get => reverseBits;         set => reverseBits     = value; }
        public bool     TrimZeroVolume      { get => trimZeroVolume;      set => trimZeroVolume  = value; }
        public bool     PalProcessing       { get => palProcessing;       set => palProcessing   = value; }
        public int      VolumeAdjust        { get => volumeAdjust;        set => volumeAdjust    = value; }
        public int      DmcInitialValueDiv2 { get => dmcInitialValueDiv2; set => dmcInitialValueDiv2 = value; }
        public float    FinePitch           { get => finePitch;           set => finePitch       = value; }
        public int      PaddingMode         { get => paddingMode;         set => paddingMode     = value; }
        public SampleVolumePair[] VolumeEnvelope { get => volumeEnvelope; }

        public bool HasAnyProcessingOptions => SourceDataIsWav || sampleRate != 15 || volumeAdjust != 100 || !Utils.IsNearlyEqual(finePitch, 1.0f) || trimZeroVolume || reverseBits;

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

        public DPCMSample(Project project, int id, string name)
        {
            this.project = project;
            this.id = id;
            this.name = name;
            this.color = Theme.RandomCustomColor();
        }

        public float GetPlaybackSampleRate(bool palPlayback)
        {
            return DPCMSampleRate.Frequencies[palPlayback ? 1 : 0, previewRate];
        }

        public float GetPlaybackCents(bool palPlayback)
        {
            return DPCMSampleRate.GetSemitones(palPlayback, previewRate);
        }

        public float GetPlaybackDuration(bool palPlayback)
        {
            return processedData.Length * 8 / GetPlaybackSampleRate(palPlayback);
        }

        public int GetVolumeScaleDmcInitialValueDiv2()
        {
            var startVolume = volumeEnvelope[0].volume * (Math.Max(0, volumeAdjust) / 100.0f);
            return Utils.Clamp((int)Math.Round((dmcInitialValueDiv2 - NesApu.DACDefaultValueDiv2) * startVolume) + NesApu.DACDefaultValueDiv2, 0, 63);
        }

        public void SetDmcSourceData(byte[] data, string filename, bool resetParams)
        {
            sourceData = new DPCMSampleDmcSourceData(data);
            sourceFilename = filename;

            if (resetParams)
            {
                paddingMode = DPCMPaddingType.Unpadded;
                ResetVolumeEnvelope();
            }
            else
            {
                ClampVolumeEnvelope();
            }
        }

        public void SetWavSourceData(short[] data, int rate, string filename, bool resetParams)
        {
            sourceData = new DPCMSampleWavSourceData(data, rate);
            sourceFilename = filename;

            if (resetParams)
            {
                paddingMode = DPCMPaddingType.PadTo16Bytes;
                ResetVolumeEnvelope();
            }
            else
            {
                ClampVolumeEnvelope();
            }
        }

        public bool TrimSourceSourceData(int sampleStart, int sampleEnd)
        {
            bool trimmed = sourceData.Trim(sampleStart, sampleEnd);
            ClampVolumeEnvelope();
            return trimmed;
        }

        public void PermanentlyApplyAllProcessing()
        {
            SetDmcSourceData(processedData, null, true);
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

                        WaveUtils.DpcmToWave(dmcData, dmcInitialValueDiv2, out sourceWavData);
                    }

                    if (!Utils.IsNearlyEqual(finePitch, 1.0f))
                    {
                        var newLength = (int)Math.Round(sourceWavData.Length / finePitch);
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

                        envelope.Add(new SampleVolumePair(0, volumeEnvelope[0].volume * (volumeAdjust / 100.0f)));
                        envelope.Add(new SampleVolumePair(volumeEnvelope[1].sample, volumeEnvelope[1].volume * (volumeAdjust / 100.0f)));
                        envelope.Add(new SampleVolumePair(volumeEnvelope[2].sample, volumeEnvelope[2].volume * (volumeAdjust / 100.0f)));
                        envelope.Add(new SampleVolumePair(sourceWavData.Length - 1, volumeEnvelope[3].volume * (volumeAdjust / 100.0f)));

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

                    var volumeScaledInitialDmcValue = GetVolumeScaleDmcInitialValueDiv2();

                    WaveUtils.WaveToDpcm(sourceWavData, minProcessingSample, maxProcessingSample, sourceSampleRate, targetSampleRate, volumeScaledInitialDmcValue, roundMode, out processedData);
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
                    paddingMode == DPCMPaddingType.PadTo16BytesPlusOne ||
                    paddingMode == DPCMPaddingType.TrimTo16Bytes ||
                    paddingMode == DPCMPaddingType.TrimTo16BytesPlusOne
                    )
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
                        case DPCMPaddingType.TrimTo16Bytes:
                            newSize = Utils.RoundDown(processedData.Length, 16);
                            break;
                        case DPCMPaddingType.TrimTo16BytesPlusOne:
                            newSize = Utils.RoundDown(processedData.Length - 1, 16) + 1;
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

        public void SetProject(Project newProject)
        {
            project = newProject;
        }

        public void ValidateIntegrity(Project project, Dictionary<int, object> idMap)
        {
#if DEBUG
            project.ValidateId(id);

            Debug.Assert(project == this.project);

            if (idMap.TryGetValue(id, out var foundObj))
                Debug.Assert(foundObj == this);
            else
                idMap.Add(id, this);

            Debug.Assert(project.GetSample(id) == this);
            Debug.Assert(!string.IsNullOrEmpty(name.Trim()));
            Debug.Assert(bank >= 0 && bank < Project.MaxDPCMBanks);
            Debug.Assert(string.IsNullOrEmpty(folderName) || project.FolderExists(FolderType.Sample, folderName));
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

        public void ResetVolumeEnvelope()
        {
            volumeEnvelope[0] = new SampleVolumePair(0);
            volumeEnvelope[1] = new SampleVolumePair((int)Math.Round(SourceNumSamples * (1.0f / 3.0f)));
            volumeEnvelope[2] = new SampleVolumePair((int)Math.Round(SourceNumSamples * (2.0f / 3.0f)));
            volumeEnvelope[3] = new SampleVolumePair(SourceNumSamples - 1);
            Process();
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
            sourceData.Serialize(buffer);

            // At version 8 (FamiStudio 2.3.0) we added an explicit "reverse bit" flag.
            if (buffer.Version >= 8)
            {
                buffer.Serialize(ref reverseBits);
            }

            color = Theme.RandomCustomColor();
            paddingMode = DPCMPaddingType.Unpadded;

            ResetVolumeEnvelope();

            // Process to apply bit reverse, etc.
            Process();
        }

        public uint ComputeCRC(uint crc = 0)
        {
            var serializer = new ProjectCrcBuffer(crc);
            Serialize(serializer);
            return serializer.CRC;
        }

        public void Serialize(ProjectBuffer buffer)
        {
            if (buffer.IsReading)
                project = buffer.Project;

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

                sourceData.Serialize(buffer);
                buffer.Serialize(ref color);

                // At version 15 (FamiStudio 4.1.0) we added DPCM bankswitching.
                if (buffer.Version >= 15)
                {
                    buffer.Serialize(ref bank);
                }

                // At version 16 (FamiStudio 4.2.0) we added little folders in the project explorer.
                if (buffer.Version >= 16)
                {
                    buffer.Serialize(ref folderName);
                }

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
                    buffer.Serialize(ref finePitch);
                    buffer.Serialize(ref dmcInitialValueDiv2);
                }

                // The process data will not be stored in the file, it will 
                // be reprocessed every time we reopen the file.
                if (buffer.IsForUndoRedo)
                {
                    buffer.Serialize(ref processedData);
                    buffer.Serialize(ref processedDataStartTime);
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
            33143.945f,
            33252.14f
        };
    }

    public class DPCMSampleMapping
    {
        private DPCMSample sample;
        private bool loop = false;
        private bool overrideDmcInitialValue = false;
        private int pitch = 15;
        private int dmcInitialValueDiv2 = NesApu.DACDefaultValueDiv2;

        public DPCMSample Sample { get => sample; set => sample = value; }
        public bool Loop                    { get => loop;  set => loop  = value; }
        public int  Pitch                   { get => pitch; set => pitch = value; }
        public bool OverrideDmcInitialValue { get => overrideDmcInitialValue; set => overrideDmcInitialValue = value; }
        public int  DmcInitialValueDiv2     { get => dmcInitialValueDiv2;     set => dmcInitialValueDiv2 = value; }

        public void Serialize(ProjectBuffer buffer)
        {
            buffer.Serialize(ref sample);
            buffer.Serialize(ref loop);
            buffer.Serialize(ref pitch);

            if (buffer.Version >= 13)
            {
                buffer.Serialize(ref overrideDmcInitialValue);
                buffer.Serialize(ref dmcInitialValueDiv2);
            }
        }

        public void ValidateIntegrity(Project project, Dictionary<int, object> idMap)
        {
            if (sample != null)
            {
                Debug.Assert(project.GetSample(sample.Id) == sample);
                Debug.Assert(project.Samples.Contains(sample));
            }
        }

        public override int GetHashCode()
        {
            int hash = sample.Id;
            hash = Utils.HashCombine(hash, loop ? 1 : 0);
            hash = Utils.HashCombine(hash, pitch);
            hash = Utils.HashCombine(hash, overrideDmcInitialValue ? dmcInitialValueDiv2 : 0);
            return hash;
        }

        public override bool Equals(object obj)
        {
            DPCMSampleMapping other = obj as DPCMSampleMapping;
            return
                sample == other.sample &&
                loop == other.loop &&
                pitch == other.pitch &&
                overrideDmcInitialValue == other.overrideDmcInitialValue &&
                dmcInitialValueDiv2 == other.dmcInitialValueDiv2;
        }
    }

    public interface IDPCMSampleSourceData
    {
        void Serialize(ProjectBuffer buffer);
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

        public void Serialize(ProjectBuffer buffer)
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

        public void Serialize(ProjectBuffer buffer)
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
        public const int TrimTo16Bytes         = 5;
        public const int TrimTo16BytesPlusOne  = 6;

        public static readonly string[] Names =
        {
            "Unpadded",
            "Pad to 16",
            "Pad to 16+1",
            "Round to 16",
            "Round to 16+1",
            "Trim to 16",
            "Trim to 16+1"
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
                
        static LocalizedString KHzLabel;
        static LocalizedString SemitonesLabel;

        static DPCMSampleRate()
        {
            Localization.LocalizeStatic(typeof(DPCMSampleRate));
        }

        public static readonly float[,] Frequencies =
        {
            // NTSC
            {
                4181.7124f,
                4709.9287f,
                5264.038f,
                5593.0405f,
                6257.9478f,
                7046.3506f,
                7919.3496f,
                8363.425f,
                9419.857f,
                11186.081f,
                12604.035f,
                13982.602f,
                16884.65f,
                21306.822f,
                24857.959f,
                33143.945f
            },
            // PAL
            {
                4177.4043f,
                4696.63f,
                5261.4146f,
                5579.2183f,
                6023.9385f,
                7044.945f,
                7917.1763f,
                8397.005f,
                9446.631f,
                11233.831f,
                12595.508f,
                14089.89f,
                16965.377f,
                21315.475f,
                25191.016f,
                33252.14f
            }
        };

        public static float GetSemitones(bool pal, int idx)
        {
            var f = Frequencies[pal ? 1 : 0, idx];
            var b = Frequencies[pal ? 1 : 0, 15];

            return (float)Math.Log(f / b, 2.0) * 12;
        }

        public static string GetString(bool index, bool pal, bool freq, bool semitones, int idx)
        {
            var str = "";

            if (index)
            {
                str += $"[{idx}]";
            }

            if (freq)
            {
                if (str != "") str += " ";
                var f = Frequencies[pal ? 1 : 0, idx];
                str += (f / 1000).ToString("n1", CultureInfo.CurrentCulture) + " " + KHzLabel.ToString();
            }

            if (semitones)
            {
                if (str != "") str += " ";
                if (freq) str += "(";
                str += GetSemitones(pal, idx).ToString("n2", CultureInfo.CurrentCulture) + " " + SemitonesLabel.ToString();
                if (freq) str += ")";
            }

            return str;
        }

        public static string[] GetStringList(bool index, bool pal, bool freq, bool semitones)
        {
            var strings = new string[Frequencies.GetLength(1)];

            for (int i = 0; i < Frequencies.GetLength(1); i++)
                strings[i] = GetString(index, pal, freq, semitones, i);

            return strings;
        }
    };
}
