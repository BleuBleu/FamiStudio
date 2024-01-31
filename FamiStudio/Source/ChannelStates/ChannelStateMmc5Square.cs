using System;

namespace FamiStudio
{
    public class ChannelStateMmc5Square : ChannelState
    {
        int regOffset = 0;
        int prevPeriodHi = 0x80;

        public ChannelStateMmc5Square(IPlayerInterface player, int apuIdx, int channelType) : base(player, apuIdx, channelType, false)
        {
            regOffset = (channelType - ChannelType.Mmc5Square1) * 4;
        }

        public override void UpdateAPU()
        {
            var duty = GetDuty();

            if (note.IsStop)
            {
                WriteRegister(NesApu.MMC5_PL1_VOL + regOffset, (duty << 6) | (0x30) | 0);
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod();
                var volume = GetVolume();

                var periodHi = (period >> 8) & 0x07;
                var periodLo = (period >> 0) & 0xff;

                if (periodHi != prevPeriodHi) // Avoid resetting the sequence.
                {
                    WriteRegister(NesApu.MMC5_PL1_HI + regOffset, periodHi);
                    prevPeriodHi = periodHi;
                }

                WriteRegister(NesApu.MMC5_PL1_LO  + regOffset, periodLo);
                WriteRegister(NesApu.MMC5_PL1_VOL + regOffset, (duty << 6) | (0x30) | volume);
            }

            base.UpdateAPU();
        }

        protected override void ResetPhase()
        {
            SkipCycles(4); // ldy
            WriteRegister(NesApu.MMC5_PL1_HI + regOffset, prevPeriodHi);
        }
    }
}
