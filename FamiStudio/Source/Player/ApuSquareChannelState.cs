using System;

namespace FamiStudio
{
    public class ApuSquareChannelState : ChannelState
    {
        int regOffset = 0;
        int prevPulseHi = -1;

        public ApuSquareChannelState(int apuIdx, int channelType) : base(apuIdx, channelType)
        {
            regOffset = channelType * 4;
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_PL1_VOL + regOffset, (duty << 6) | (0x30) | 0);
            }
            else if (note.IsValid)
            {
                var noteVal = Utils.Clamp(note.Value + envelopeValues[Envelope.Arpeggio], 0, noteTable.Length - 1);
                var period = Utils.Clamp(noteTable[noteVal] + GetSlidePitch() + envelopeValues[Envelope.Pitch], 0, maximumPeriod);
                var volume = MultiplyVolumes(note.Volume, envelopeValues[Envelope.Volume]);

                NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_PL1_LO + regOffset, period & 0xff);
                period = (period >> 8) & 0x07;

                if (prevPulseHi != period) // Avoid resetting the sequence.
                {
                    prevPulseHi = period;
                    NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_PL1_HI + regOffset, period);
                }

                NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_PL1_VOL + regOffset, (duty << 6) | (0x30) | volume);
            }
        }
    };
}
