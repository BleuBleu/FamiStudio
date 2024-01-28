using System;

namespace FamiStudio
{
    public class ChannelStateVrc6Square : ChannelState
    {
        int regOffset = 0;
        int prevPeriodHi = 0;

        public ChannelStateVrc6Square(IPlayerInterface player, int apuIdx, int channelType) : base(player, apuIdx, channelType, false)
        {
            regOffset = (channelType - ChannelType.Vrc6Square1) * 0x1000;
        }

        public override void UpdateAPU()
        {
            var duty = GetDuty();

            if (note.IsStop)
            {
                WriteRegister(NesApu.VRC6_PL1_VOL + regOffset, (duty << 4));
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod();
                var volume = GetVolume();
                
                var periodHi = ((period >> 8) & 0x0f);
                prevPeriodHi = periodHi;

                WriteRegister(NesApu.VRC6_PL1_LO  + regOffset, period & 0xff);
                WriteRegister(NesApu.VRC6_PL1_HI  + regOffset, periodHi | 0x80);
                WriteRegister(NesApu.VRC6_PL1_VOL + regOffset, (duty << 4) | volume);
            }

            base.UpdateAPU();
        }

        protected override void ResetPhase()
        {
            // Clear and set the hi-bit of 9002/A002 to reset phase.
            SkipCycles(6); // tax/lda
            WriteRegister(NesApu.VRC6_PL1_HI + regOffset, prevPeriodHi);
            SkipCycles(2); // ora
            WriteRegister(NesApu.VRC6_PL1_HI + regOffset, prevPeriodHi | 0x80);
            SkipCycles(2); // txa
        }
    }
}
