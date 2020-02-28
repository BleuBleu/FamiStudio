using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateVrc7 : ChannelState
    {
        int channelIdx = 0;
        byte vrc7Instrument = 0;
        byte prevReg20;

        public ChannelStateVrc7(int apuIdx, int channelType) : base(apuIdx, channelType, false)
        {
            channelIdx = channelType - Channel.Vrc7Fm1;
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
            {
                Debug.Assert(instrument.ExpansionType == Project.ExpansionVrc7);

                if (instrument.Vrc7Patch == 0)
                {
                    for (byte i = 0; i < 8; i++)
                    {
                        WriteRegister(NesApu.VRC7_REG_SEL, i);
                        WriteRegister(NesApu.VRC7_REG_WRITE, instrument.Vrc7PatchRegs[i]);
                    }
                }

                vrc7Instrument = (byte)(instrument.Vrc7Patch << 4);
            }
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                prevReg20 = (byte)(prevReg20 & ~(0x10));
                WriteRegister(NesApu.VRC7_REG_SEL, 0x20 + channelIdx);
                WriteRegister(NesApu.VRC7_REG_WRITE, prevReg20);
            }
            else if (note.IsRelease)
            {
                prevReg20 = (byte)(prevReg20 & ~(0x10));
                WriteRegister(NesApu.VRC7_REG_SEL, 0x20 + channelIdx);
                WriteRegister(NesApu.VRC7_REG_WRITE, prevReg20 | 0x20);
            }
            else if (note.IsMusical)
            {
                var octave = (note.Value - 1) / 12;
                var period = NesApu.NoteTableVrc7[(note.Value - 1) % 12] >> 2;
                var volume = 15 - GetVolume();

                var reg10 = (byte)(period & 0xff);
                var reg20 = (byte)(0x10 | ((octave & 0x3) << 1) | ((period >> 8) & 1));
                var reg30 = (byte)(vrc7Instrument | volume);

                if ((prevReg20 & 0x10) != 0)
                {
                    prevReg20 = (byte)(prevReg20 & ~(0x10));
                    WriteRegister(NesApu.VRC7_REG_SEL, 0x20 + channelIdx);
                    WriteRegister(NesApu.VRC7_REG_WRITE, prevReg20);
                }

                WriteRegister(NesApu.VRC7_REG_SEL, 0x10 + channelIdx);
                WriteRegister(NesApu.VRC7_REG_WRITE, reg10);
                WriteRegister(NesApu.VRC7_REG_SEL, 0x20 + channelIdx);
                WriteRegister(NesApu.VRC7_REG_WRITE, reg20);
                WriteRegister(NesApu.VRC7_REG_SEL, 0x30 + channelIdx);
                WriteRegister(NesApu.VRC7_REG_WRITE, reg30);

                prevReg20 = reg20;
            }

            // To prevent from re-triggering.
            note.IsValid = false;
        }
    };
}
