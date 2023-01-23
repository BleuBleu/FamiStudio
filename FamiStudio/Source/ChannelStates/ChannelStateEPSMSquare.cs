using System;

namespace FamiStudio
{
    public class ChannelStateEPSMSquare : ChannelState
    {
        int  channelIdx = 0;
        int toneReg = 0x38;

        public ChannelStateEPSMSquare(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
            channelIdx = channelType - ChannelType.EPSMSquare1;
        }

        public override void UpdateYMMixerSettingsNotify(int ymMixerSettings)
        {
            toneReg = ymMixerSettings;
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
                var period = GetPeriod();
                var volume = GetVolume();

                var periodHi = (period >> 8) & 0x0f;
                var periodLo = (period >> 0) & 0xff;
                var ymMixerSettings = envelopeValues[EnvelopeType.YMMixerSettings];
                var noiseFreq = envelopeValues[EnvelopeType.YMNoiseFreq];
                int mask = 0xff;
                mask = mask - (9 << channelIdx);
                player.UpdateYMMixerSettings(
                    ((toneReg & mask) + (GetYMMixerSettings() << channelIdx)),
                    (1L << ChannelType.EPSMSquare1) |
                    (1L << ChannelType.EPSMSquare2) |
                    (1L << ChannelType.EPSMSquare3));
                //int noiseCheck = GetYMMixerSettings() & 0x2;
                Console.Write(toneReg + "\n");
                WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_TONE);
                WriteRegister(NesApu.EPSM_DATA0, toneReg);
                if (noiseFreq > 0)
                {
                    WriteRegister(NesApu.EPSM_ADDR0, NesApu.EPSM_REG_NOISE);
                    WriteRegister(NesApu.EPSM_DATA0, noiseFreq);
                }
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
