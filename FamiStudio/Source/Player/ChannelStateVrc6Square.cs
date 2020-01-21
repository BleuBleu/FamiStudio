using System;

namespace FamiStudio
{
    public class ChannelStateVrc6Square : ChannelState
    {
        int regOffset = 0;

        public ChannelStateVrc6Square(int apuIdx, int channelType) : base(apuIdx, channelType)
        {
            regOffset = (channelType - Channel.VRC6Square1) * 0x1000;
            maximumPeriod = NesApu.MaximumPeriod12Bit;
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                NesApu.WriteRegister(apuIdx, NesApu.VRC6_PL1_VOL + regOffset, (duty << 4));
            }
            else if (note.IsValid)
            {
                var noteVal = Utils.Clamp(note.Value + envelopeValues[Envelope.Arpeggio], 0, noteTable.Length - 1);
                var period = Utils.Clamp(noteTable[noteVal] + GetSlidePitch() + envelopeValues[Envelope.Pitch], 0, maximumPeriod);
                var volume = MultiplyVolumes(note.Volume, envelopeValues[Envelope.Volume]);

                NesApu.WriteRegister(apuIdx, NesApu.VRC6_PL1_LO  + regOffset, period & 0xff);
                NesApu.WriteRegister(apuIdx, NesApu.VRC6_PL1_HI  + regOffset, ((period >> 8) & 0x0f) | 0x80);
                NesApu.WriteRegister(apuIdx, NesApu.VRC6_PL1_VOL + regOffset, (duty << 4) | volume);
            }
        }
    };
}
