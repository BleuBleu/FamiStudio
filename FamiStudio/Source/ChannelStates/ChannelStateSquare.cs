using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateSquare : ChannelState
    {
        int regOffset = 0;
        int prevPeriodHi = 1000;

        public ChannelStateSquare(int apuIdx, int channelType, bool pal) : base(apuIdx, channelType, pal)
        {
            regOffset = channelType * 4;
        }

        public override void UpdateAPU()
        {
            var duty = GetDuty();

            if (note.IsStop)
            {
                WriteRegister(NesApu.APU_PL1_VOL + regOffset, (duty << 6) | (0x30) | 0);
            }
            else if (note.IsMusical)
            {
                Debug.WriteLine(note.FriendlyName);

                var period = GetPeriod();
                var volume = GetVolume();

                var periodHi = (period >> 8) & 0x07;
                var periodLo = (period >> 0) & 0xff;
                int deltaHi  = periodHi - prevPeriodHi;

                if (deltaHi != 0) // Avoid resetting the sequence.
                {
                    if (Settings.SquareSmoothVibrato && Math.Abs(deltaHi) == 1 && !IsSeeking)
                    {
                        // Blaarg's smooth vibrato technique using the sweep to avoid resetting the phase. Cool stuff.
                        // http://forums.nesdev.com/viewtopic.php?t=231

                        WriteRegister(NesApu.APU_FRAME_CNT, 0x40); // reset frame counter in case it was about to clock
                        WriteRegister(NesApu.APU_PL1_LO + regOffset, deltaHi < 0 ? 0x00 : 0xff); // be sure low 8 bits of timer period are $FF ($00 when negative)
                        WriteRegister(NesApu.APU_PL1_SWEEP + regOffset, deltaHi < 0 ? 0x8f : 0x87); // sweep enabled, shift = 7, set negative flag.
                        WriteRegister(NesApu.APU_FRAME_CNT, 0xc0); // clock sweep immediately
                        WriteRegister(NesApu.APU_PL1_SWEEP + regOffset, 0x08); // disable sweep
                    }
                    else
                    {
                        WriteRegister(NesApu.APU_PL1_HI + regOffset, periodHi);
                    }

                    prevPeriodHi = periodHi;
                }

                WriteRegister(NesApu.APU_PL1_LO + regOffset, periodLo);
                WriteRegister(NesApu.APU_PL1_VOL + regOffset, (duty << 6) | (0x30) | volume);
            }

            base.UpdateAPU();
        }
    };
}
