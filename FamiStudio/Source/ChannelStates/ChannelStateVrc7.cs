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
        }

        private void WriteVrc7Register(int reg, int data)
        {
            WriteRegister(NesApu.VRC7_REG_SEL, reg, 16);    // Roughly equivalent to what we do in sound engine (jsr + rts).
            WriteRegister(NesApu.VRC7_REG_WRITE, data, 84); // Roughly equivalent to what we do in sound engine (jsr + rts + 8 dummy loops).
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
            {
                Debug.Assert(instrument.IsVrc7);

                if (instrument.IsVrc7)
                {
                    if (instrument.Vrc7Patch == 0)
                    {
                        // Tell other channels using custom patches that they will need 
                        // to reload their instruments.
                        player.NotifyInstrumentLoaded(
                            instrument,
                            (1L << ChannelType.Vrc7Fm1) |
                            (1L << ChannelType.Vrc7Fm2) |
                            (1L << ChannelType.Vrc7Fm3) |
                            (1L << ChannelType.Vrc7Fm4) |
                            (1L << ChannelType.Vrc7Fm5) |
                            (1L << ChannelType.Vrc7Fm6));

                        for (byte i = 0; i < 8; i++)
                            WriteVrc7Register(i, instrument.Vrc7PatchRegs[i]);
                    }

                    vrc7Instrument = (byte)(instrument.Vrc7Patch << 4);
                }
            }
        }

        public override void IntrumentLoadedNotify(Instrument instrument)
        {
            Debug.Assert(instrument.IsVrc7 && instrument.Vrc7Patch == 0);

            var currentInstrument = note.Instrument;

            // This will be called when another channel loads a custom patch.
            if (currentInstrument != null && 
                currentInstrument != instrument &&
                currentInstrument.Vrc7Patch == 0)
            {
                forceInstrumentReload = true;
            }
        }

        private int GetOctave(ref int period)
        {
            var octave = 0;
            while (period >= 0x200)
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
            else if (note.IsMusical)
            {
                if (noteReleased)
                {
                    prevPeriodHi = (byte)(prevPeriodHi & ~(0x10));
                }
                else if (noteTriggered)
                {
                    if ((prevPeriodHi & 0x10) != 0)
                    {
                        WriteVrc7Register(NesApu.VRC7_REG_HI_1 + channelIdx, 0);
                    }
                    else
                    {
                        prevPeriodHi |= 0x10;
                    }
                }

                var period  = GetPeriod();
                var octave  = GetOctave(ref period);
                var volume  = 15 - GetVolume();

                // Period hi bit at 0x10 : goes from 0->1 = attack, goes from 1->0 release
                var periodLo = (byte)(period & 0xff);
                var periodHi = (byte)(0x20 | (prevPeriodHi & 0x10) | ((octave & 0x7) << 1) | ((period >> 8) & 1));

                WriteVrc7Register(NesApu.VRC7_REG_VOL_1 + channelIdx, vrc7Instrument | volume);
                WriteVrc7Register(NesApu.VRC7_REG_LO_1  + channelIdx, periodLo);
                WriteVrc7Register(NesApu.VRC7_REG_HI_1  + channelIdx, periodHi); // This is what seems to trigger the note, do last.

                prevPeriodHi = periodHi;
            }

            base.UpdateAPU();
        }
    };
}
