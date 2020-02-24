using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateVrc7 : ChannelState
    {
        int channelIdx = 0;
        byte vrc7Instrument = 0;

        public ChannelStateVrc7(int apuIdx, int channelType) : base(apuIdx, channelType, false)
        {
            channelIdx = channelType - Channel.Vrc7Fm1;
            //maximumPeriod = NesApu.MaximumPeriod12Bit;
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

        bool first = true;

        public override void UpdateAPU()
        {
            if (note.IsValid)
            {
                var volume = 15 - GetVolume();

                WriteRegister(NesApu.VRC7_REG_SEL,   0x10);
                WriteRegister(NesApu.VRC7_REG_WRITE, 0x80);

                WriteRegister(NesApu.VRC7_REG_SEL,   0x30);
                WriteRegister(NesApu.VRC7_REG_WRITE, vrc7Instrument | volume);

                WriteRegister(NesApu.VRC7_REG_SEL,   0x20);
                WriteRegister(NesApu.VRC7_REG_WRITE, 0x15);

                // To prevent from re-triggering.
                note.IsValid = false;
            }
        }
    };
}
