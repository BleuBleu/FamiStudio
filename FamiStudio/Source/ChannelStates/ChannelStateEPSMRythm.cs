using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateEPSMRythm : ChannelState
    {
        int channelIdx = 0;
        int[] opStereo = { 0, 0, 0, 0, 0, 0 };

        public ChannelStateEPSMRythm(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
            channelIdx = channelType - ChannelType.EPSMrythm1;
            customRelease = true;

        }
        protected override void LoadInstrument(Instrument instrument)
        {
            Debug.Assert(instrument.ExpansionType == ExpansionType.EPSM);
            opStereo[channelIdx] = instrument.EpsmPatchRegs[1] & 0xC0;
        }

        public override void UpdateAPU()
        {
            if (note.IsMusical)
            {
                if (noteTriggered)
                {
                    var volume = GetVolume();
                    WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_RYTHM_LEVEL + channelIdx);
                    WriteRegister(NesApu.EPSM_DATA0, opStereo[channelIdx] | volume << 1);
                    WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_RYTHM);
                    WriteRegister(NesApu.EPSM_DATA0, (1 << channelIdx));
                }
            }

            base.UpdateAPU();
        }
    };
}
