namespace FamiStudio
{
    public class ChannelStateNoise : ChannelState
    {
        public ChannelStateNoise(IPlayerInterface player, int apuIdx, int channelIdx, bool pal) : base(player, apuIdx, channelIdx, pal)
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
                var volume = GetVolume();
                var duty   = GetDuty();
                var period = (int)(((note.Value + envelopeValues[EnvelopeType.Arpeggio]) & 0x0f) ^ 0x0f) | ((duty << 7) & 0x80);

                WriteRegister(NesApu.APU_NOISE_LO, period);
                WriteRegister(NesApu.APU_NOISE_VOL, 0x30 | volume);
            }

            base.UpdateAPU();
        }
    }
}
