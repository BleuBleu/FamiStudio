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
            Console.WriteLine("Rythm" + channelIdx);
            if (note.IsStop)
            {
                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_VOL_A + channelIdx);
                WriteRegister(NesApu.EPSM_DATA0, 0);
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod();
                var volume = GetVolume();

                var periodHi = (period >> 8) & 0x0f;
                var periodLo = (period >> 0) & 0xff;

                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_LO_A + channelIdx * 2);
                WriteRegister(NesApu.EPSM_DATA0, periodLo);
                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_HI_A + channelIdx * 2);
                WriteRegister(NesApu.EPSM_DATA0, periodHi);
                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_VOL_A + channelIdx);
                WriteRegister(NesApu.EPSM_DATA0, volume);
            }

            base.UpdateAPU();
        }
    };
}
