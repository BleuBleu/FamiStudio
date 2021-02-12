﻿using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateVrc7 : ChannelState
    {
        int channelIdx = 0;
        byte vrc7Instrument = 0;
        byte prevPeriodHi;

        public ChannelStateVrc7(int apuIdx, int channelType) : base(apuIdx, channelType, false)
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
                        for (byte i = 0; i < 8; i++)
                            WriteVrc7Register(i, instrument.Vrc7PatchRegs[i]);
                    }

                    vrc7Instrument = (byte)(instrument.Vrc7Patch << 4);
                }
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
