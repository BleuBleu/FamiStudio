using System;

namespace FamiStudio
{
    public class ChannelStateNamco : ChannelState
    {
        int channelIdx = 0;

        public ChannelStateNamco(int apuIdx, int channelType, bool pal) : base(apuIdx, channelType, pal)
        {
            channelIdx = channelType - Channel.NamcoWave1;
            //maximumPeriod = NesApu.MaximumPeriod12Bit;
        }

        public override void UpdateAPU()
        {
        }
    };
}
