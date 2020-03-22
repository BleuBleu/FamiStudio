using System;
using System.Linq;
using System.Text;

namespace FamiStudio
{
    class FamitrackerInstrumentFile
    {
        readonly static int[] InstrumentTypeLookup =
        {
            Project.ExpansionCount, // INST_NONE: Should never happen.
            Project.ExpansionNone,  // INST_2A03
            Project.ExpansionVrc6,  // INST_VRC6
            Project.ExpansionVrc7,  // INST_VRC7
            Project.ExpansionFds,   // INST_FDS
            Project.ExpansionN163,  // INST_N163
            Project.ExpansionS5B    // INST_S5B
        };

        readonly static int[] EnvelopeTypeLookup =
        {
            Envelope.Volume,   // SEQ_VOLUME
            Envelope.Arpeggio, // SEQ_ARPEGGIO
            Envelope.Pitch,    // SEQ_PITCH
            Envelope.Count,    // SEQ_HIPITCH
            Envelope.DutyCycle // SEQ_DUTYCYCLE
        };

        const int SEQ_COUNT = 5;

        private void ReadEnvelope(byte[] bytes, ref int offset, Instrument instrument, int envType)
        {
            var itemCount    = BitConverter.ToInt32(bytes, offset); offset += 4;
            var loopPoint    = BitConverter.ToInt32(bytes, offset); offset += 4;
            var releasePoint = BitConverter.ToInt32(bytes, offset); offset += 4;
            var setting      = BitConverter.ToInt32(bytes, offset); offset += 4;

            var seq = new sbyte[itemCount];
            for (int j = 0; j < itemCount; j++)
                seq[j] = (sbyte)bytes[offset++];

            // Skip unsupported types.
            if (envType == Envelope.Count)
                return;

            Envelope env = instrument.Envelopes[envType];

            if (env == null)
                return;

            if (releasePoint >= 0 && !env.CanRelease)
                releasePoint = -1;

            // FamiTracker allows envelope with release with no loop. We dont allow that.
            if (env.CanRelease && releasePoint != -1)
            {
                if (loopPoint == -1)
                    loopPoint = releasePoint;
                if (releasePoint != -1)
                    releasePoint++;
            }

            if (envType == Envelope.Pitch)
                env.Relative = true;

            if (env != null)
            {
                env.Length  = itemCount;
                env.Loop    = loopPoint;
                env.Release = releasePoint;
                Array.Copy(seq, 0, env.Values, 0, itemCount);
            }
        }

        public Instrument CreateFromFile(Project project, string filename)
        {
            var bytes = System.IO.File.ReadAllBytes(filename);

            if (!bytes.Skip(0).Take(3).SequenceEqual(Encoding.ASCII.GetBytes("FTI")))
                return null;
            if (!bytes.Skip(3).Take(3).SequenceEqual(Encoding.ASCII.GetBytes("2.4")))
                return null;

            var instType = InstrumentTypeLookup[bytes[6]];

            // Needs to match the current expansion audio. Our enum happens to match (-1) for now.
            if (instType != Project.ExpansionNone && instType != project.ExpansionAudio)
                return null;

            var offset = 7;
            var nameLen = BitConverter.ToInt32(bytes, offset); offset += 4;
            var name = Encoding.ASCII.GetString(bytes, offset, nameLen); offset += nameLen;

            if (project.GetInstrument(name) != null)
                return null;

            var instrument = project.CreateInstrument(instType, name);

            if (instType == Project.ExpansionFds)
            {
                var wavEnv = instrument.Envelopes[Envelope.FdsWaveform];
                for (int i = 0; i < wavEnv.Length; i++)
                    wavEnv.Values[i] = (sbyte)bytes[offset++];

                var modEnv = instrument.Envelopes[Envelope.FdsModulation];
                for (int i = 0; i < modEnv.Length; i++)
                    modEnv.Values[i] = (sbyte)bytes[offset++];

                instrument.FdsWavePreset = Envelope.WavePresetCustom;
                instrument.FdsModPreset = Envelope.WavePresetCustom;

                modEnv.ConvertFdsModulationToAbsolute();

                // Skip mod speed/depth/delay.
                offset += sizeof(int) * 3;

                ReadEnvelope(bytes, ref offset, instrument, Envelope.Volume);
                ReadEnvelope(bytes, ref offset, instrument, Envelope.Arpeggio);
                ReadEnvelope(bytes, ref offset, instrument, Envelope.Pitch);
            }
            else if (instType == Project.ExpansionNone ||
                     instType == Project.ExpansionN163)
            {
                if (bytes[offset++] != SEQ_COUNT)
                    return null;

                // Envelopes
                for (int i = 0; i < SEQ_COUNT; i++)
                {
                    if (bytes[offset++] == 1)
                        ReadEnvelope(bytes, ref offset, instrument, EnvelopeTypeLookup[i]);
                }
            }
            else if (instType == Project.ExpansionVrc7)
            {
                instrument.Vrc7Patch = (byte)BitConverter.ToInt32(bytes, offset); offset += 4;

                if (instrument.Vrc7Patch == 0)
                {
                    for (int i = 0; i < 8; ++i)
                        instrument.Vrc7PatchRegs[i] = bytes[offset++];
                }
            }

            if (instType == Project.ExpansionN163)
            {
                int waveSize  = BitConverter.ToInt32(bytes, offset); offset += 4;
                int wavePos   = BitConverter.ToInt32(bytes, offset); offset += 4;
                int waveCount = BitConverter.ToInt32(bytes, offset); offset += 4;

                instrument.N163WavePreset = Envelope.WavePresetCustom;
                instrument.N163WaveSize   = (byte)waveSize;
                instrument.N163WavePos    = (byte)wavePos;

                var wavEnv = instrument.Envelopes[Envelope.N163Waveform];

                // Only read the first wave for now.
                for (int j = 0; j < waveSize; j++)
                    wavEnv.Values[j] = (sbyte)bytes[offset++];
            }

            // Samples
            if (instType == Project.ExpansionNone)
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

                    var sampleData = new byte[sampleSize];
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
