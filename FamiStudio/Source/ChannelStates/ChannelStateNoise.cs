namespace FamiStudio
{
    public class ChannelStateNoise : ChannelState
    {
        public ChannelStateNoise(int apuIdx, int channelIdx, bool pal) : base(apuIdx, channelIdx, pal)
        {
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.APU_NOISE_VOL, 0xf0);
            }
            else if (note.IsValid)
            {
                var noteVal = (int)(((note.Value + envelopeValues[Envelope.Arpeggio]) & 0x0f) ^ 0x0f) | ((duty << 7) & 0x80);
                var volume = GetVolume();

                WriteRegister(NesApu.APU_NOISE_LO, noteVal);
                WriteRegister(NesApu.APU_NOISE_VOL, 0xf0 | volume);
            }
        }
    }
}
