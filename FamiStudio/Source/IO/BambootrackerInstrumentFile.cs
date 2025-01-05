using System;
using System.Linq;
using System.Text;

namespace FamiStudio
{
    class BambootrackerInstrumentFile
    {

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

        private void ReadEnvelope(byte[] bytes, ref int offset, Instrument instrument, int envType)
        {
            var itemCount = BitConverter.ToInt32(bytes, offset); offset += 4;
            var loopPoint = BitConverter.ToInt32(bytes, offset); offset += 4;
            var releasePoint = BitConverter.ToInt32(bytes, offset); offset += 4;
            var setting = BitConverter.ToInt32(bytes, offset); offset += 4;

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
                env.Length = itemCount;
                env.Loop = loopPoint;
                env.Release = releasePoint;
                Array.Copy(seq, 0, env.Values, 0, itemCount);
            }
        }

        public Instrument CreateFromFile(Project project, string filename)
        {
            var bytes = System.IO.File.ReadAllBytes(filename);

            if (!bytes.Skip(0).Take(16).SequenceEqual(Encoding.ASCII.GetBytes("BambooTrackerIst")))
            {
                Log.LogMessage(LogSeverity.Error, "Incompatible file.");
                return null;
            }
            if (!bytes.Skip(24).Take(8).SequenceEqual(Encoding.ASCII.GetBytes("INSTRMNT")))
            {
                Log.LogMessage(LogSeverity.Error, "Not an Instrument file.");
                return null;
            }

            var instType = ExpansionType.EPSM;

            //// Needs to match the current expansion audio. Our enum happens to match (-1) for now.
            ////if (instType != ExpansionType.None && instType != project.ExpansionAudio)
            /*if (ExpansionType.EPSM != project.ExpansionAudio)
             */
            if (!project.UsesExpansionAudio(instType))
            {
                Log.LogMessage(LogSeverity.Error, "Bambootracker import only supported by EPSM expansion");
                return null;
            }

            var offset = 36;
            var nameLen = BitConverter.ToInt32(bytes, offset); offset += 4;
            var name = Encoding.ASCII.GetString(bytes, offset, nameLen); offset += nameLen;

            if (project.GetInstrument(name) != null)
            {
                Log.LogMessage(LogSeverity.Error, $"An instrument named '{name}' already exists.");
                return null;
            }

            if (!(bytes[offset] == 0))
            {
                Log.LogMessage(LogSeverity.Error, "Only FM channels possible to import.");
                return null;
            }
            offset += 12;
            if (!bytes.Skip(offset).Take(8).SequenceEqual(Encoding.ASCII.GetBytes("INSTPROP")))
            {
                Log.LogMessage(LogSeverity.Error, "Instrument File error" + Encoding.ASCII.GetString(bytes, offset, 8));
                return null;
            }
            offset += 8;
            var instrument = project.CreateInstrument(instType, name);
            // 0xB0, 0xB4, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0x38, 0x48, 0x58, 0x68, 0x78, 0x88, 0x98, 0x34, 0x44, 0x54, 0x64, 0x74, 0x84, 0x94, 0x4c, 0x5c, 0x6c, 0x7c, 0x8c, 0x9c, 0x22
            //int[] registerOrder = { 0,30,};
            offset++; //envelope info offset
            offset++;
            offset += 4; //instrument end info offset


            for (int i = 0; i < 25; ++i)
            {
                //offset++;
                int value = bytes[offset++];
                if (i == 0)
                    instrument.EpsmPatchRegs[i] = (byte)(((value & 0xf0) >> 4) | ((value & 0x0f) << 3));
                if (i == 1)
                    instrument.EpsmPatchRegs[4] = (byte)((value & 0x1f));
                if (i == 2)
                {
                    instrument.EpsmPatchRegs[5] = (byte)((value & 0x1f));
                    instrument.EpsmPatchRegs[4] = (byte)(instrument.EpsmPatchRegs[4] | ((value & 0xe0) << 1));
                }
                if (i == 3)
                {
                    instrument.EpsmPatchRegs[6] = (byte)((value & 0x1f));
                    instrument.EpsmPatchRegs[2] = (byte)(instrument.EpsmPatchRegs[2] | ((value & 0xe0) >> 1));
                }
                if (i == 4)
                {
                    instrument.EpsmPatchRegs[7] = (byte)((value & 0xff));
                }
                if (i == 5)
                {
                    instrument.EpsmPatchRegs[3] = (byte)((value & 0xff));
                }
                if (i == 6)
                {
                    instrument.EpsmPatchRegs[6] = (byte)(((value & 0x70) >> 4) | ((~(value & 0x80) >> 4) & 0x8));
                    instrument.EpsmPatchRegs[2] = (byte)(instrument.EpsmPatchRegs[2] | ((value & 0x0f)));
                }
                if (i == 7)
                    instrument.EpsmPatchRegs[4 + 7] = (byte)((value & 0x1f));
                if (i == 8)
                {
                    instrument.EpsmPatchRegs[5 + 7] = (byte)((value & 0x1f));
                    instrument.EpsmPatchRegs[4 + 7] = (byte)(instrument.EpsmPatchRegs[4 + 7] | ((value & 0xe0) << 1));
                }
                if (i == 9)
                {
                    instrument.EpsmPatchRegs[6 + 7] = (byte)((value & 0x1f));
                    instrument.EpsmPatchRegs[2 + 7] = (byte)(instrument.EpsmPatchRegs[2 + 7] | ((value & 0xe0) >> 1));
                }
                if (i == 10)
                {
                    instrument.EpsmPatchRegs[7 + 7] = (byte)((value & 0xff));
                }
                if (i == 11)
                {
                    instrument.EpsmPatchRegs[3 + 7] = (byte)((value & 0xff));
                }
                if (i == 12)
                {
                    instrument.EpsmPatchRegs[6 + 7] = (byte)(((value & 0x70) >> 4) | ((~(value & 0x80) >> 4) & 0x8));
                    instrument.EpsmPatchRegs[2 + 7] = (byte)(instrument.EpsmPatchRegs[2 + 7] | ((value & 0x0f)));
                }
                if (i == 13)
                    instrument.EpsmPatchRegs[4 + 14] = (byte)((value & 0x1f));
                if (i == 14)
                {
                    instrument.EpsmPatchRegs[5 + 14] = (byte)((value & 0x1f));
                    instrument.EpsmPatchRegs[4 + 14] = (byte)(instrument.EpsmPatchRegs[4 + 14] | ((value & 0xe0) << 1));
                }
                if (i == 15)
                {
                    instrument.EpsmPatchRegs[6 + 14] = (byte)((value & 0x1f));
                    instrument.EpsmPatchRegs[2 + 14] = (byte)(instrument.EpsmPatchRegs[2 + 14] | ((value & 0xe0) >> 1));
                }
                if (i == 16)
                {
                    instrument.EpsmPatchRegs[7 + 14] = (byte)((value & 0xff));
                }
                if (i == 17)
                {
                    instrument.EpsmPatchRegs[3 + 14] = (byte)((value & 0xff));
                }
                if (i == 18)
                {
                    instrument.EpsmPatchRegs[6 + 14] = (byte)(((value & 0x70) >> 4) | ((~(value & 0x80) >> 4) & 0x8));
                    instrument.EpsmPatchRegs[2 + 14] = (byte)(instrument.EpsmPatchRegs[2 + 14] | ((value & 0x0f)));
                }
                if (i == 19)
                    instrument.EpsmPatchRegs[4 + 21] = (byte)((value & 0x1f));
                if (i == 20)
                {
                    instrument.EpsmPatchRegs[5 + 21] = (byte)((value & 0x1f));
                    instrument.EpsmPatchRegs[4 + 21] = (byte)(instrument.EpsmPatchRegs[4 + 21] | ((value & 0xe0) << 1));
                }
                if (i == 21)
                {
                    instrument.EpsmPatchRegs[6 + 21] = (byte)((value & 0x1f));
                    instrument.EpsmPatchRegs[2 + 21] = (byte)(instrument.EpsmPatchRegs[2 + 21] | ((value & 0xe0) >> 1));
                }
                if (i == 22)
                {
                    instrument.EpsmPatchRegs[7 + 21] = (byte)((value & 0xff));
                }
                if (i == 23)
                {
                    instrument.EpsmPatchRegs[3 + 21] = (byte)((value & 0xff));
                }
                if (i == 24)
                {
                    instrument.EpsmPatchRegs[6 + 21] = (byte)(((value & 0x70) >> 4) | ((~(value & 0x80) >> 4) & 0x8));
                    instrument.EpsmPatchRegs[2 + 21] = (byte)(instrument.EpsmPatchRegs[2 + 21] | ((value & 0x0f)));
                }
                if (i > 24)
                    instrument.EpsmPatchRegs[i] = (byte)value;
                //instrument.EpsmPatchRegs[i] = (byte)offset;
            }


            project.ConditionalSortInstruments();

            return instrument;
        }
    }
}
