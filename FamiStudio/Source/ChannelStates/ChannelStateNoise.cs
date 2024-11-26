using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateNoise : ChannelState
    {
        public ChannelStateNoise(IPlayerInterface player, int apuIdx, int channelIdx, int tuning, bool pal) : base(player, apuIdx, channelIdx, tuning, pal)
        {
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.APU_NOISE_VOL, 0xf0);
            }
            else if (note.IsMusical)
            {
                var slide  = slideShift < 0 ? (slidePitch >> -slideShift) : (slidePitch << slideShift); // Remove the fraction part.
                var volume = GetVolume();
                var duty   = GetDuty();
                var period = (int)(((note.Value + slide + envelopeValues[EnvelopeType.Arpeggio]) & 0x0f) ^ 0x0f) | ((duty << 7) & 0x80);

                WriteRegister(NesApu.APU_NOISE_LO, period);
                WriteRegister(NesApu.APU_NOISE_VOL, 0x30 | volume);
            }

            base.UpdateAPU();
        }
    }
}
