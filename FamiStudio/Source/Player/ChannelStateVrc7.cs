using System;

namespace FamiStudio
{
    public class ChannelStateVrc7 : ChannelState
    {
        int channelIdx = 0;

        public ChannelStateVrc7(int apuIdx, int channelType) : base(apuIdx, channelType)
        {
            channelIdx = channelType - Channel.Vrc7Fm1;
            //maximumPeriod = NesApu.MaximumPeriod12Bit;
        }

        bool first = true;

        public override void UpdateAPU()
        {
            if (first && channelIdx == 0)
            {
                WriteRegister(NesApu.VRC7_REG_SEL,   0x30);
                WriteRegister(NesApu.VRC7_REG_WRITE, 0x30);
                WriteRegister(NesApu.VRC7_REG_SEL,   0x10);
                WriteRegister(NesApu.VRC7_REG_WRITE, 0x80);
                WriteRegister(NesApu.VRC7_REG_SEL,   0x20);
                WriteRegister(NesApu.VRC7_REG_WRITE, 0x15);
                first = false;
            }
        }
    };
}
