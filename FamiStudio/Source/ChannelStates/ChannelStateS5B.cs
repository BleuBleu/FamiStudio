using System;

namespace FamiStudio
{
    public class ChannelStateS5B : ChannelState
    {
        int channelIdx = 0;
        int toneReg = 0x38;

        public ChannelStateS5B(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
            channelIdx = channelType - ChannelType.S5BSquare1;
        }

        public override void UpdateYMMixerSettingsNotify(int  ymMixerSettings)
        {
            toneReg = ymMixerSettings;
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_VOL_A + channelIdx);
                WriteRegister(NesApu.S5B_DATA, 0);
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
                    (1L << ChannelType.S5BSquare1) |
                    (1L << ChannelType.S5BSquare2) |
                    (1L << ChannelType.S5BSquare3));
                //int noiseCheck = GetYMMixerSettings() & 0x2;
                //Console.Write(toneReg + "\n");
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_TONE);
                WriteRegister(NesApu.S5B_DATA, toneReg);
                if (noiseFreq > 0)
                {
                    WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_NOISE);
                    WriteRegister(NesApu.S5B_DATA, noiseFreq);
                }
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_LO_A + channelIdx * 2);
                WriteRegister(NesApu.S5B_DATA, periodLo);
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_HI_A + channelIdx * 2);
                WriteRegister(NesApu.S5B_DATA, periodHi);
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_VOL_A + channelIdx);
                WriteRegister(NesApu.S5B_DATA, volume);
            }

            base.UpdateAPU();
        }
    };
}
