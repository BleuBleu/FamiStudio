using System;

namespace FamiStudio
{
    public class ChannelStateVrc6Square : ChannelState
    {
        int regOffset = 0;

        public ChannelStateVrc6Square(int apuIdx, int channelType) : base(apuIdx, channelType, false)
        {
            regOffset = (channelType - Channel.Vrc6Square1) * 0x1000;
            maximumPeriod = NesApu.MaximumPeriod12Bit;
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

                WriteRegister(NesApu.VRC6_PL1_LO  + regOffset, period & 0xff);
                WriteRegister(NesApu.VRC6_PL1_HI  + regOffset, ((period >> 8) & 0x0f) | 0x80);
                WriteRegister(NesApu.VRC6_PL1_VOL + regOffset, (duty << 4) | volume);
            }
        }
    };
}
