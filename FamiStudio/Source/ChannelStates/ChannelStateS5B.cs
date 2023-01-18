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

        public override void UpdateToneNoiseNotify(int  toneNoise)
        {
            toneReg = toneNoise;
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
                var toneNoise = envelopeValues[EnvelopeType.S5BToneNoise];
                var noiseFreq = envelopeValues[EnvelopeType.S5BNoiseFreq];
                int mask = 0xff;
                mask = mask - (9 << channelIdx);
                player.UpdateToneNoise(
                    ((toneReg & mask) + (GetToneNoise() << channelIdx)),
                    (1L << ChannelType.S5BSquare1) |
                    (1L << ChannelType.S5BSquare2) |
                    (1L << ChannelType.S5BSquare3));
                int noiseCheck = GetToneNoise() & 0x2;
                Console.Write(toneReg + "\n");
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_TONE);
                WriteRegister(NesApu.S5B_DATA, toneReg);
                if (noiseCheck == 0)
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
