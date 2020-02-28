using System;

namespace FamiStudio
{
    public class ChannelStateSunsoft : ChannelState
    {
        int channelIdx = 0;

        public ChannelStateSunsoft(int apuIdx, int channelType, bool pal) : base(apuIdx, channelType, pal)
        {
            channelIdx = channelType - Channel.SunsoftSquare1;
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_VOL_A + channelIdx);
                WriteRegister(NesApu.S5B_DATA, 0);
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod();
                var volume = GetVolume();

                var periodHi = (period >> 8) & 0x0f;
                var periodLo = (period >> 0) & 0xff;

                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_LO_A + channelIdx * 2);
                WriteRegister(NesApu.S5B_DATA, periodLo);
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_HI_A + channelIdx * 2);
                WriteRegister(NesApu.S5B_DATA, periodHi);
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_VOL_A + channelIdx);
                WriteRegister(NesApu.S5B_DATA, volume);
            }
        }
    };
}
