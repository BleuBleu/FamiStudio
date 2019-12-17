using System;

namespace FamiStudio
{
    public class ApuTriangleChannelState : ChannelState
    {
        public ApuTriangleChannelState(int apuIdx, int channelIdx) : base(apuIdx, channelIdx)
        {
            noteTable = NesApu.NoteTableNTSC;
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteApuRegister(NesApu.APU_TRI_LINEAR, 0x80);
            }
            else if (note.IsValid)
            {
                var noteVal = Utils.Clamp(note.Value + envelopeValues[Envelope.Arpeggio], 0, noteTable.Length - 1);
                int period = Utils.Clamp(noteTable[noteVal] + portamentoPitch + envelopeValues[Envelope.Pitch], 0, NesApu.MaximumPeriod);

                WriteApuRegister(NesApu.APU_TRI_LO, (period >> 0) & 0xff);
                WriteApuRegister(NesApu.APU_TRI_HI, (period >> 8) & 0x07);
                WriteApuRegister(NesApu.APU_TRI_LINEAR, 0x80 | envelopeValues[Envelope.Volume]);
            }
        }
    }
}
