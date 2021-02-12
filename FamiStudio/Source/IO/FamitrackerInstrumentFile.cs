using System;
using System.Linq;
using System.Text;

namespace FamiStudio
{
    class FamitrackerInstrumentFile
    {
        readonly static int[] InstrumentTypeLookup =
        {
            ExpansionType.Count, // INST_NONE: Should never happen.
            ExpansionType.None,  // INST_2A03
            ExpansionType.Vrc6,  // INST_VRC6
            ExpansionType.Vrc7,  // INST_VRC7
            ExpansionType.Fds,   // INST_FDS
            ExpansionType.N163,  // INST_N163
            ExpansionType.S5B    // INST_S5B
        };

        readonly static int[] EnvelopeTypeLookup =
        {
            EnvelopeType.Volume,   // SEQ_VOLUME
            EnvelopeType.Arpeggio, // SEQ_ARPEGGIO
            EnvelopeType.Pitch,    // SEQ_PITCH
            EnvelopeType.Count,    // SEQ_HIPITCH
            EnvelopeType.DutyCycle // SEQ_DUTYCYCLE
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
            if (envType == EnvelopeType.Count)
            {
                Log.LogMessage(LogSeverity.Warning, $"Hi-pitch envelopes are unsupported, ignoring.");
                return;
            }

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

            if (envType == EnvelopeType.Pitch)
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
            {
                Log.LogMessage(LogSeverity.Error, "Incompatible file.");
                return null;
            }
            if (!bytes.Skip(3).Take(3).SequenceEqual(Encoding.ASCII.GetBytes("2.4")))
            {
                Log.LogMessage(LogSeverity.Error, "Incompatible FTI version.");
                return null;
            }

            var instType = InstrumentTypeLookup[bytes[6]];

            // Needs to match the current expansion audio. Our enum happens to match (-1) for now.
            if (instType != ExpansionType.None && instType != project.ExpansionAudio)
            {
                Log.LogMessage(LogSeverity.Error, "Audio expansion does not match the current project expansion.");
                return null;
            }

            var offset = 7;
            var nameLen = BitConverter.ToInt32(bytes, offset); offset += 4;
            var name = Encoding.ASCII.GetString(bytes, offset, nameLen); offset += nameLen;

            if (project.GetInstrument(name) != null)
            {
                Log.LogMessage(LogSeverity.Error, $"An instrument named '{name}' already exists.");
                return null;
            }

            var instrument = project.CreateInstrument(instType, name);

            if (instType == ExpansionType.Fds)
            {
                var wavEnv = instrument.Envelopes[EnvelopeType.FdsWaveform];
                for (int i = 0; i < wavEnv.Length; i++)
                    wavEnv.Values[i] = (sbyte)bytes[offset++];

                var modEnv = instrument.Envelopes[EnvelopeType.FdsModulation];
                for (int i = 0; i < modEnv.Length; i++)
                    modEnv.Values[i] = (sbyte)bytes[offset++];

                instrument.FdsWavePreset = WavePresetType.Custom;
                instrument.FdsModPreset = WavePresetType.Custom;

                modEnv.ConvertFdsModulationToAbsolute();

                // Skip mod speed/depth/delay.
                offset += sizeof(int) * 3;

                ReadEnvelope(bytes, ref offset, instrument, EnvelopeType.Volume);
                ReadEnvelope(bytes, ref offset, instrument, EnvelopeType.Arpeggio);
                ReadEnvelope(bytes, ref offset, instrument, EnvelopeType.Pitch);
            }
            else if (instType == ExpansionType.None ||
                     instType == ExpansionType.N163)
            {
                var seqCount = bytes[offset++];

                if (seqCount != SEQ_COUNT)
                {
                    Log.LogMessage(LogSeverity.Error, $"Unexpected number of envelopes ({seqCount}).");
                    return null;
                }

                // Envelopes
                for (int i = 0; i < SEQ_COUNT; i++)
                {
                    if (bytes[offset++] == 1)
                        ReadEnvelope(bytes, ref offset, instrument, EnvelopeTypeLookup[i]);
                }
            }
            else if (instType == ExpansionType.Vrc7)
            {
                instrument.Vrc7Patch = (byte)BitConverter.ToInt32(bytes, offset); offset += 4;

                if (instrument.Vrc7Patch == 0)
                {
                    for (int i = 0; i < 8; ++i)
                        instrument.Vrc7PatchRegs[i] = bytes[offset++];
                }
            }

            if (instType == ExpansionType.N163)
            {
                int waveSize  = BitConverter.ToInt32(bytes, offset); offset += 4;
                int wavePos   = BitConverter.ToInt32(bytes, offset); offset += 4;
                int waveCount = BitConverter.ToInt32(bytes, offset); offset += 4;

                instrument.N163WavePreset = WavePresetType.Custom;
                instrument.N163WaveSize   = (byte)waveSize;
                instrument.N163WavePos    = (byte)wavePos;

                var wavEnv = instrument.Envelopes[EnvelopeType.N163Waveform];

                // Only read the first wave for now.
                for (int j = 0; j < waveSize; j++)
                    wavEnv.Values[j] = (sbyte)bytes[offset++];

                if (waveCount > 1)
                {
                    Log.LogMessage(LogSeverity.Warning, $"Multiple N163 waveforms detected, only loading the first one.");
                }
            }

            // Samples
            if (instType == ExpansionType.None)
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

                    sampleMap[sampleIdx] = project.CreateDPCMSampleFromDmcData(sampleName, sampleData);
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

            project.SortInstruments();

            return instrument;
        }
    }
}
