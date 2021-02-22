using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateVrc7 : ChannelState
    {
        protected int  channelIdx = 0;
        protected byte vrc7Instrument = 0;
        protected byte prevPeriodHi;

        public ChannelStateVrc7(IPlayerInterface player, int apuIdx, int channelType) : base(player, apuIdx, channelType, false)
        {
            channelIdx = channelType - ChannelType.Vrc7Fm1;
            customRelease = true;
        }

        private void WriteVrc7Register(int reg, int data)
        {
            WriteRegister(NesApu.VRC7_REG_SEL,   reg);
            WriteRegister(NesApu.VRC7_REG_WRITE, data);
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
            {
                Debug.Assert(instrument.ExpansionType == ExpansionType.Vrc7);

                if (instrument.ExpansionType == ExpansionType.Vrc7)
                {
                    if (instrument.Vrc7Patch == 0)
                    {
                        // Tell other channels using custom patches that they will need 
                        // to reload their instruments.
                        player.RequestInstrumentReload(
                            (1 << ChannelType.Vrc7Fm1) |
                            (1 << ChannelType.Vrc7Fm2) |
                            (1 << ChannelType.Vrc7Fm3) |
                            (1 << ChannelType.Vrc7Fm4) |
                            (1 << ChannelType.Vrc7Fm5) |
                            (1 << ChannelType.Vrc7Fm6));

                        for (byte i = 0; i < 8; i++)
                            WriteVrc7Register(i, instrument.Vrc7PatchRegs[i]);
                    }

                    vrc7Instrument = (byte)(instrument.Vrc7Patch << 4);
                }
            }
        }

        public override void InstrumentReloadRequested()
        {
            // This will be called when another channel loads a custom patch.
            if (note.Instrument != null && 
                note.Instrument.Vrc7Patch == 0)
            {
                forceInstrumentReload = true;
            }
        }

        private int GetOctave(ref int period)
        {
            var octave = 0;
            while (period > 0x100)
            {
                period >>= 1;
                octave++;
            }
            return octave;
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                prevPeriodHi = (byte)(prevPeriodHi & ~(0x30));
                WriteVrc7Register(NesApu.VRC7_REG_HI_1 + channelIdx, prevPeriodHi);
            }
            else if (note.IsRelease)
            {
                prevPeriodHi = (byte)(prevPeriodHi & ~(0x10));
                WriteVrc7Register(NesApu.VRC7_REG_HI_1 + channelIdx, prevPeriodHi);
            }
            else if (note.IsMusical)
            {
                var period  = GetPeriod();
                var octave  = GetOctave(ref period);
                var volume  = 15 - GetVolume();

                var periodLo = (byte)(period & 0xff);
                var periodHi = (byte)(0x30 | ((octave & 0x7) << 1) | ((period >> 8) & 1));

                if (noteTriggered && (prevPeriodHi & 0x10) != 0)
                    WriteVrc7Register(NesApu.VRC7_REG_HI_1 + channelIdx, prevPeriodHi & ~(0x10));

                WriteVrc7Register(NesApu.VRC7_REG_LO_1  + channelIdx, periodLo);
                WriteVrc7Register(NesApu.VRC7_REG_HI_1  + channelIdx, periodHi);
                WriteVrc7Register(NesApu.VRC7_REG_VOL_1 + channelIdx, vrc7Instrument | volume);

                prevPeriodHi = periodHi;
            }

            base.UpdateAPU();
        }
    };
}
