using System;

namespace FamiStudio
{
    public class ChannelStateTriangle : ChannelState
    {
        public ChannelStateTriangle(int apuIdx, int channelIdx, bool pal) : base(apuIdx, channelIdx, pal)
        {
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.APU_TRI_LINEAR, 0x80);
            }
            else if (note.IsMusical)
            {
                var noteVal = Utils.Clamp(note.Value + envelopeValues[Envelope.Arpeggio], 0, noteTable.Length - 1);
                var period = Utils.Clamp(noteTable[noteVal] + GetSlidePitch() + envelopeValues[Envelope.Pitch], 0, maximumPeriod);

                WriteRegister(NesApu.APU_TRI_LO, (period >> 0) & 0xff);
                WriteRegister(NesApu.APU_TRI_HI, (period >> 8) & 0x07);
                WriteRegister(NesApu.APU_TRI_LINEAR, 0x80 | envelopeValues[Envelope.Volume]);
            }
        }
    }
}
