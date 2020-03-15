using System;

namespace FamiStudio
{
    public class ChannelStateVrc6Saw : ChannelState
    {
        public ChannelStateVrc6Saw(int apuIdx, int channelType) : base(apuIdx, channelType, false)
        {
            maximumPeriod = NesApu.MaximumPeriod12Bit;
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.VRC6_SAW_VOL, 0x00);
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod();
                var volume = GetVolume();
                var duty   = GetDuty();

                WriteRegister(NesApu.VRC6_SAW_VOL, (volume << 2)); 
                WriteRegister(NesApu.VRC6_SAW_LO, ((period >> 0) & 0xff));
                WriteRegister(NesApu.VRC6_SAW_HI, ((period >> 8) & 0x0f) | 0x80);
            }

            base.UpdateAPU();
        }
    };
}
