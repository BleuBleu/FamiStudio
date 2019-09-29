namespace FamiStudio
{
    public class NoiseChannelState : ChannelState
    {
        public NoiseChannelState(int apuIdx, int channelIdx) : base(apuIdx, channelIdx)
        {
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteApuRegister(NesApu.APU_NOISE_VOL, 0xf0);
            }
            else if (note.IsValid)
            {
                var noteVal = (int)(((note.Value + envelopeValues[Envelope.Arpeggio]) & 0x0f) ^ 0x0f) | ((duty << 7) & 0x80);

                WriteApuRegister(NesApu.APU_NOISE_LO, noteVal);
                WriteApuRegister(NesApu.APU_NOISE_VOL, 0xf0 | MultiplyVolumes(note.Volume, envelopeValues[Envelope.Volume]));
            }
        }
    }
}
