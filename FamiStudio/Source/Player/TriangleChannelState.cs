namespace FamiStudio
{
    public class TriangleChannelState : ChannelState
    {
        public TriangleChannelState(int apuIdx, int channelIdx) : base(apuIdx, channelIdx)
        {
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteApuRegister(NesApu.APU_TRI_LINEAR, 0x80);
            }
            else if (note.IsValid)
            {
                var noteVal = Note.Clamp(note.Value + envelopeValues[Envelope.Arpeggio]);
                int period = NesApu.NoteTableNTSC[noteVal] + envelopeValues[Envelope.Pitch];

                WriteApuRegister(NesApu.APU_TRI_LO, (period >> 0) & 0xff);
                WriteApuRegister(NesApu.APU_TRI_HI, (period >> 8) & 0x07);
                WriteApuRegister(NesApu.APU_TRI_LINEAR, 0x80 | envelopeValues[Envelope.Volume]);
            }
        }
    }
}
