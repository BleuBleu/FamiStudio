using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.IO;

namespace FamiStudio
{
    public class DPCMSample
    {
        private int id;
        private string name;
        private byte[] processedData;
        private Color color;

        private IDPCMSampleSourceData sourceData;

        // Processing parameters.
        private int sampleRate = 15;
        private int previewRate = 15;
        private bool reverseBits;
        private int volumeAdjust = 100;

        public int Id => id;
        public string Name { get => name; set => name = value; }
        public Color Color { get => color; set => color = value; }
        public byte[] ProcessedData { get => processedData; set => processedData = value; }

        public bool SourceDataIsWav { get => sourceData is DPCMSampleWavSourceData; }
        public IDPCMSampleSourceData SourceData { get => sourceData; }
        public DPCMSampleWavSourceData SourceWavData { get => sourceData as DPCMSampleWavSourceData; }
        public DPCMSampleDmcSourceData SourceDmcData { get => sourceData as DPCMSampleDmcSourceData; }

        public int SampleRate { get => sampleRate; set => sampleRate = value; }
        public int PreviewRate { get => previewRate; set => previewRate = value; }
        public bool ReverseBits { get => reverseBits; set => reverseBits = value; }
        public int VolumeAdjust { get => volumeAdjust; set => volumeAdjust = value; }

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
            sourceData = new DPCMSampleDmcSourceData(data);
        }

        public void SetWavSourceData(short[] data, int rate)
        {
            sourceData = new DPCMSampleWavSourceData(data, rate);
        }

        public void Process()
        {
            if (SourceDataIsWav)
            {
                WaveUtils.WaveToDpcm(SourceWavData.Samples, SourceWavData.SampleRate, 33144, 31, out processedData); // DPCMTODO : hardcoded 33144
            }
            else
            {
                // DPCMTODO : round to 64-bytes here!
                // DPCMTODO : Do bit reverse and everything else too!
                processedData = new byte[SourceDmcData.Data.Length];
                Array.Copy(SourceDmcData.Data, processedData, SourceDmcData.Data.Length);
            }

            if (reverseBits)
            {
                for (int i = 0; i < processedData.Length; i++)
                    processedData[i] = Utils.ReverseBits(processedData[i]);
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
        bool TrimSilence();
    }

    public class DPCMSampleWavSourceData : IDPCMSampleSourceData
    {
        int sampleRate;
        short[] wavData;

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

        public bool Trim(float timeStart, float timeEnd)
        {
            return WaveUtils.TrimWave(ref wavData, sampleRate, timeStart, timeEnd);
        }

        public bool TrimSilence()
        {
            return WaveUtils.TrimSilence(ref wavData, sampleRate);
        }
    }

    public class DPCMSampleDmcSourceData : IDPCMSampleSourceData
    {
        byte[] dmcData;

        public byte[] Data => dmcData;

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

        public bool TrimSilence()
        {
            // DPCMTODO : TODO!
            return false;
        }
    }
}
