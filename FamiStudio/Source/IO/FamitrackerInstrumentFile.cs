using System;
using System.Linq;
using System.Text;

namespace FamiStudio
{
    class FamitrackerInstrumentFile
    {
        enum InstrumentType
        {
            INST_NONE = 0,
            INST_2A03 = 1,
            INST_VRC6 = 2,
            INST_VRC7 = 3,
            INST_FDS  = 4,
            INST_N163 = 5,
            INST_S5B  = 6
        };
        
        enum SequenceType
        {
            SEQ_VOLUME,
            SEQ_ARPEGGIO,
            SEQ_PITCH,
            SEQ_HIPITCH,
            SEQ_DUTYCYCLE,
            SEQ_COUNT
        };

        public static Instrument CreateFromFile(Project project, string filename)
        {
            var bytes = System.IO.File.ReadAllBytes(filename);

            if (!bytes.Skip(0).Take(3).SequenceEqual(Encoding.ASCII.GetBytes("FTI")))
                return null;
            if (!bytes.Skip(3).Take(3).SequenceEqual(Encoding.ASCII.GetBytes("2.4")))
                return null;

            var instType = (InstrumentType)bytes[6];

            // Needs to match the current expansion audio. Our enum happens to match (-1) for now.
            if (instType != InstrumentType.INST_2A03 && (int)(instType - 1) != project.ExpansionAudio)
                return null;

            var offset = 7;
            var nameLen = BitConverter.ToInt32(bytes, offset); offset += 4;
            var name = Encoding.ASCII.GetString(bytes, offset, nameLen); offset += nameLen;

            if (bytes[offset++] != (int)SequenceType.SEQ_COUNT)
                return null;

            if (project.GetInstrument(name) != null)
                return null;

            var instrument = project.CreateInstrument((int)instType - 1, name);

            // Envelopes
            for (int i = 0; i < (int)SequenceType.SEQ_COUNT; i++)
            {
                if (bytes[offset++] == 1)
                {
                    var itemCount    = BitConverter.ToInt32(bytes, offset); offset += 4;
                    var loopPoint    = BitConverter.ToInt32(bytes, offset); offset += 4;
                    var releasePoint = BitConverter.ToInt32(bytes, offset); offset += 4;
                    var setting      = BitConverter.ToInt32(bytes, offset); offset += 4;
                    var seq          = new sbyte[itemCount];
                    var scale        = i == (int)SequenceType.SEQ_PITCH ? -1 : 1;

                    for (int j = 0; j < itemCount; j++)
                        seq[j] = (sbyte)(bytes[offset++] * scale);

                    if (releasePoint >= 0 && i != (int)SequenceType.SEQ_VOLUME)
                        releasePoint = -1;

                    // Our loop/release logic is slightly different.
                    if (i == (int)SequenceType.SEQ_VOLUME && loopPoint == -1 && releasePoint >= 0)
                    {
                        loopPoint = releasePoint;
                        releasePoint++;
                    }

                    Envelope env = null;
                    switch ((SequenceType)i)
                    {
                        case SequenceType.SEQ_VOLUME:    env = instrument.Envelopes[Envelope.Volume]; break;
                        case SequenceType.SEQ_PITCH:     env = instrument.Envelopes[Envelope.Pitch]; env.Relative = true; break;
                        case SequenceType.SEQ_ARPEGGIO:  env = instrument.Envelopes[Envelope.Arpeggio]; break;
                        case SequenceType.SEQ_DUTYCYCLE: instrument.DutyCycle = seq[0]; break;
                    }

                    if (env != null)
                    {
                        env.Length = itemCount;
                        env.Loop = loopPoint;
                        env.Release = releasePoint;
                        Array.Copy(seq, 0, env.Values, 0, itemCount);
                    }
                }
            }

            // Samples
            if (instType == InstrumentType.INST_2A03)
            {
                // Skip over the sample mappings for now, we will load them after the actual sample data.
                var assignedCount = BitConverter.ToInt32(bytes, offset); offset += 4;
                var mappingOffset = offset; 
                offset += assignedCount * 4;

                var sampleCount = BitConverter.ToInt32(bytes, offset); offset += 4;
                var sampleMap = new DPCMSample[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    var sampleIdx     = BitConverter.ToInt32(bytes, offset); offset += 4;
                    var sampleNameLen = BitConverter.ToInt32(bytes, offset); offset += 4;
                    var sampleName    = Encoding.ASCII.GetString(bytes, offset, sampleNameLen); offset += sampleNameLen;
                    var sampleSize    = BitConverter.ToInt32(bytes, offset); offset += 4;
                    var sampleData    = new byte[sampleSize];

                    Array.Copy(bytes, offset, sampleData, 0, sampleSize); offset += sampleSize;

                    sampleMap[sampleIdx] = project.CreateDPCMSample(sampleName, sampleData);
                }

                for (int i = 0; i < assignedCount; i++)
                {
                    byte idx    = bytes[mappingOffset++];
                    byte sample = bytes[mappingOffset++];
                    byte pitch  = bytes[mappingOffset++];
                    byte delta  = bytes[mappingOffset++];

                    if (project.NoteSupportsDPCM(idx + 1))
                        project.MapDPCMSample(idx + 1, sampleMap[sample - 1], pitch);
                }
            }

            return instrument;
        }
    }
}
