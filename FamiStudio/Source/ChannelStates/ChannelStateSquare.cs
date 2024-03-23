using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateSquare : ChannelState
    {
        int regOffset = 0;
        int prevPeriodHi = 0x80;

        public ChannelStateSquare(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
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
                var period = GetPeriod();
                var volume = GetVolume();

                var periodHi = (period >> 8) & 0x07;
                var periodLo = (period >> 0) & 0xff;
                int deltaHi  = periodHi - prevPeriodHi;

                // Write low first, this matches the NSF driver.
                WriteRegister(NesApu.APU_PL1_LO + regOffset, periodLo);

                if (deltaHi != 0) // Avoid resetting the sequence.
                {
                    // TODO : We should not reference settings from here.
                    if (Settings.SquareSmoothVibrato && Math.Abs(deltaHi) == 1 && !IsSeeking)
                    {
                        // Blaarg's smooth vibrato technique using the sweep to avoid resetting the phase. Cool stuff.
                        // http://forums.nesdev.com/viewtopic.php?t=231

                        WriteRegister(NesApu.APU_FRAME_CNT, 0x40); // reset frame counter in case it was about to clock
                        WriteRegister(NesApu.APU_PL1_LO + regOffset, deltaHi < 0 ? 0x00 : 0xff); // be sure low 8 bits of timer period are $FF ($00 when negative)
                        WriteRegister(NesApu.APU_PL1_SWEEP + regOffset, deltaHi < 0 ? 0x8f : 0x87); // sweep enabled, shift = 7, set negative flag.
                        WriteRegister(NesApu.APU_FRAME_CNT, 0xc0); // clock sweep immediately
                        WriteRegister(NesApu.APU_PL1_SWEEP + regOffset, 0x08); // disable sweep
                        WriteRegister(NesApu.APU_PL1_LO + regOffset, periodLo); // Restore correct low.
                    }
                    else
                    {
                        WriteRegister(NesApu.APU_PL1_HI + regOffset, periodHi);
                    }

                    prevPeriodHi = periodHi;
                }

                WriteRegister(NesApu.APU_PL1_VOL + regOffset, (duty << 6) | (0x30) | volume);
            }
            
            base.UpdateAPU();
        }

        protected override void ResetPhase()
        {
            SkipCycles(4); // ldy
            WriteRegister(NesApu.APU_PL1_HI + regOffset, prevPeriodHi);
        }
    }
}
