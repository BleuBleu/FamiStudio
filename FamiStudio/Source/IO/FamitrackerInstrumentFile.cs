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
        const int MAX_DSAMPLES = 64;

        private void ReadEnvelope(byte[] bytes, ref int offset, Instrument instrument, Envelope env, int envType)
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

                if (env.Length < itemCount)
                    Log.LogMessage(LogSeverity.Warning, $"{EnvelopeType.LocalizedNames[envType]} envelope is longer ({itemCount}) than what FamiStudio supports ({env.Length}). Truncating.");

                Array.Copy(seq, 0, env.Values, 0, env.Length);
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
            if (instType != ExpansionType.None && !project.UsesExpansionAudio(instType))
            {
                Log.LogMessage(LogSeverity.Error, "Audio expansion is not enabled in the current project.");
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

                ReadEnvelope(bytes, ref offset, instrument, instrument.Envelopes[EnvelopeType.Volume],   EnvelopeType.Volume);
                ReadEnvelope(bytes, ref offset, instrument, instrument.Envelopes[EnvelopeType.Arpeggio], EnvelopeType.Arpeggio);
                ReadEnvelope(bytes, ref offset, instrument, instrument.Envelopes[EnvelopeType.Pitch],    EnvelopeType.Pitch);

                // Famitracker FDS envelopes uses the full volume range (0...32), we stick to 0...15.
                for (int i = 0; i < instrument.VolumeEnvelope.Length; i++)
                    instrument.VolumeEnvelope.Values[i] = (sbyte)Math.Min(15, instrument.VolumeEnvelope.Values[i] / 2);
            }
            else if (instType == ExpansionType.None)
            {
                // Envelopes
                var seqCount = bytes[offset++];
                for (int i = 0; i < seqCount; i++)
                {
                    var envType = EnvelopeTypeLookup[i];
                    if (bytes[offset++] == 1)
                        ReadEnvelope(bytes, ref offset, instrument, instrument.Envelopes[envType], envType);
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
            else if (instType == ExpansionType.N163)
            {
                var waveIndexEnvelope = (Envelope)null;

                // Envelopes
                var seqCount = bytes[offset++];
                for (int i = 0; i < seqCount; i++)
                {
                    if (bytes[offset++] == 1)
                    {
                        var envType = EnvelopeTypeLookup[i];
                        var env = instrument.Envelopes[envType];

                        if (i == 4 /* SEQ_DUTYCYCLE */)
                            env = waveIndexEnvelope = new Envelope(EnvelopeType.WaveformRepeat);

                        ReadEnvelope(bytes, ref offset, instrument, env, envType);
                    }
                }

                int waveSize  = BitConverter.ToInt32(bytes, offset); offset += 4;
                int wavePos   = BitConverter.ToInt32(bytes, offset); offset += 4;
                int waveCount = BitConverter.ToInt32(bytes, offset); offset += 4;

                // These checks are done in famitracker too..
                if (waveSize <= 0 || waveSize > 32)
                {
                    Log.LogMessage(LogSeverity.Error, "Invalid N163 wave size. Make sure the instrument came from the original FamiTracker 0.4.6.");
                    return null;
                }

                if (waveCount <= 0 || waveCount > 16)
                {
                    Log.LogMessage(LogSeverity.Error, "Invalid N163 wave count. Make sure the instrument came from the original FamiTracker 0.4.6.");
                    return null;
                }

                instrument.N163WavePreset = WavePresetType.Custom;
                instrument.N163WaveSize   = (byte)waveSize;
                instrument.N163WavePos    = (byte)wavePos;
                instrument.N163WaveCount  = (byte)waveCount;

                var wavEnv = instrument.Envelopes[EnvelopeType.N163Waveform];

                for (int i = 0; i < waveCount; i++)
                {
                    if (i < instrument.N163WaveCount)
                    {
                        for (int j = 0; j < waveSize; j++)
                            wavEnv.Values[i * instrument.N163WaveSize + j] = (sbyte)bytes[offset++];
                    }
                    else
                    {
                        // TODO : Give warning here.
                        offset += waveSize;
                    }
                }

                FamitrackerFileBase.ConvertN163WaveIndexToRepeatEnvelope(instrument, waveIndexEnvelope);
            }

            // Samples
            if (instType == ExpansionType.None)
            {
                // Skip over the sample mappings for now, we will load them after the actual sample data.
                var assignedCount = BitConverter.ToInt32(bytes, offset); offset += 4;
                var mappingOffset = offset;
                offset += assignedCount * 4;

                var sampleCount = BitConverter.ToInt32(bytes, offset); offset += 4;
                var sampleMap = new DPCMSample[MAX_DSAMPLES];

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

                    instrument.MapDPCMSample(idx + 1, sampleMap[sample - 1], pitch & 0x0f, (pitch & 0x80) != 0);
                }
            }

            project.ConditionalSortInstruments();

            return instrument;
        }
    }
}
