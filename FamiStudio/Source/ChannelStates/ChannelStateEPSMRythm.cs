using System;

namespace FamiStudio
{
    public class ChannelStateEPSMRythm : ChannelState
    {
        int channelIdx = 0;

        public ChannelStateEPSMRythm(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
            channelIdx = channelType - ChannelType.EPSMrythm1;
            
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_VOL_A + channelIdx);
                WriteRegister(NesApu.EPSM_DATA0, 0);
            }
            else if (note.IsMusical)
            {
                var volume = GetVolume();

                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_RYTHM);
                WriteRegister(NesApu.EPSM_DATA0, (1 << channelIdx));
                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_RYTHM_LEVEL + channelIdx);
                WriteRegister(NesApu.EPSM_DATA0, volume);
            }

            base.UpdateAPU();
        }
    };
}
