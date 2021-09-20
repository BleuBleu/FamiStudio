using System;

namespace FamiStudio
{
    public class ChannelStateEPSMRythm : ChannelState
    {
        int channelIdx = 0;
        int lastNoteOn = 0;

        public ChannelStateEPSMRythm(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
            channelIdx = channelType - ChannelType.EPSMrythm1;
            customRelease = true;

        }

        public override void UpdateAPU()
        {
            if (note.IsRelease)
            {
                lastNoteOn = 0;
            }
            if (note.IsStop)
            {
                lastNoteOn = 0;
            }
            else if (note.IsMusical)
            {
                if (lastNoteOn == 0)
                {
                    var volume = GetVolume();
                    lastNoteOn = 1;
                    WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_RYTHM);
                    WriteRegister(NesApu.EPSM_DATA0, (1 << channelIdx));
                    WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_RYTHM_LEVEL + channelIdx);
                    WriteRegister(NesApu.EPSM_DATA0, 0xc0 | volume << 1);
                }
            }

            base.UpdateAPU();
        }
    };
}
