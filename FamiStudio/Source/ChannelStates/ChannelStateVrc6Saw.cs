using System;

namespace FamiStudio
{
    public class ChannelStateVrc6Saw : ChannelState
    {
        public ChannelStateVrc6Saw(int apuIdx, int channelIdx) : base(apuIdx, channelIdx, false)
        {
            maximumPeriod = NesApu.MaximumPeriod12Bit;
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.VRC6_SAW_VOL, 0x00);
            }
            else if (note.IsValid)
            {
                var period = GetPeriod();
                var volume = GetVolume();
                var duty   = GetDuty();

                // Get hi-bit from duty, similar to FamiTracker, but taking volume into account.
                // FamiTracker looses ability to output low volume when duty is odd.
                if ((duty & 1) != 0 && volume != 0)
                    volume = (volume << 1) + 1;

                WriteRegister(NesApu.VRC6_SAW_VOL, (volume << 1)); 
                WriteRegister(NesApu.VRC6_SAW_LO, ((period >> 0) & 0xff));
                WriteRegister(NesApu.VRC6_SAW_HI, ((period >> 8) & 0x0f) | 0x80);
            }
        }
    };
}
