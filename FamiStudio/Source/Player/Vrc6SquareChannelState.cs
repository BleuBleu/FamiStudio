using System;

namespace FamiStudio
{
    public class Vrc6SquareChannelState : ChannelState
    {
        int regOffset = 0;
        int prevPulseHi = -1;

        public Vrc6SquareChannelState(int apuIdx, int channelType) : base(apuIdx, channelType)
        {
            regOffset = (channelType - Channel.VRC6Square1) * 0x1000;
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteApuRegister(NesApu.VRC6_PL1_VOL + regOffset, (duty << 4));
            }
            else if (note.IsValid)
            {
                var noteVal = Utils.Clamp(note.Value + envelopeValues[Envelope.Arpeggio], 0, noteTable.Length - 1);
                var period = Utils.Clamp(noteTable[noteVal] + slidePitch + envelopeValues[Envelope.Pitch], 0, maximumPeriod);
                var volume = MultiplyVolumes(note.Volume, envelopeValues[Envelope.Volume]);

                WriteApuRegister(NesApu.VRC6_PL1_LO  + regOffset, period & 0xff);
                WriteApuRegister(NesApu.VRC6_PL1_HI  + regOffset, ((period >> 8) & 0x0f) | 0x80);
                WriteApuRegister(NesApu.VRC6_PL1_VOL + regOffset, (duty << 4) | volume);
            }
        }
    };
}
