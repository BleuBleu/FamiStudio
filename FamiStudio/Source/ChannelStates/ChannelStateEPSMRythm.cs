using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateEPSMRythm : ChannelStateEPSMBase
    {
        int channelIdx = 0;
        int stereoFlags = 0;

        public ChannelStateEPSMRythm(IPlayerInterface player, int apuIdx, int channelType,int tuning, bool pal) : base(player, apuIdx, channelType, tuning, pal)
        {
            channelIdx = channelType - ChannelType.EPSMrythm1;
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            Debug.Assert(instrument.IsEpsm);
            if (instrument != null)
            { 
                stereoFlags = instrument.EpsmPatchRegs[1] & 0xC0;
            }
        }

        public override void UpdateAPU()
        {
            if (note.IsMusical && noteTriggered)
            {
                var volume = GetVolume();
                WriteEPSMRegister(NesApu.EPSM_REG_RYTHM_LEVEL + channelIdx, stereoFlags | (volume << 1));
                WriteEPSMRegister(NesApu.EPSM_REG_RYTHM, 1 << channelIdx);
            }

            base.UpdateAPU();
        }
    };
}
