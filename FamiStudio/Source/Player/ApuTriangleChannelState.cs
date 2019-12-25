using System;

namespace FamiStudio
{
    public class ApuTriangleChannelState : ChannelState
    {
        public ApuTriangleChannelState(int apuIdx, int channelIdx) : base(apuIdx, channelIdx)
        {
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_TRI_LINEAR, 0x80);
            }
            else if (note.IsValid)
            {
                var noteVal = Utils.Clamp(note.Value + envelopeValues[Envelope.Arpeggio], 0, noteTable.Length - 1);
                var period = Utils.Clamp(noteTable[noteVal] + GetSlidePitch() + envelopeValues[Envelope.Pitch], 0, maximumPeriod);

                NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_TRI_LO, (period >> 0) & 0xff);
                NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_TRI_HI, (period >> 8) & 0x07);
                NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_TRI_LINEAR, 0x80 | envelopeValues[Envelope.Volume]);
            }
        }
    }
}
