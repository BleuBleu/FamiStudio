using System;

namespace FamiStudio
{
    public class ChannelStateMmc5Square : ChannelState
    {
        int regOffset = 0;
        int prevPeriodHi = 1000;

        public ChannelStateMmc5Square(int apuIdx, int channelType) : base(apuIdx, channelType)
        {
            regOffset = (channelType - Channel.Mmc5Square1) * 4;
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.MMC5_PL1_VOL + regOffset, (duty << 6) | (0x30) | 0);
            }
            else if (note.IsValid)
            {
                var noteVal = Utils.Clamp(note.Value + envelopeValues[Envelope.Arpeggio], 0, noteTable.Length - 1);
                var period = Utils.Clamp(noteTable[noteVal] + GetSlidePitch() + envelopeValues[Envelope.Pitch], 0, maximumPeriod);
                var volume = MultiplyVolumes(note.Volume, envelopeValues[Envelope.Volume]);

                var periodHi = (period >> 8) & 0x07;
                var periodLo = period & 0xff;
                int deltaHi  = periodHi - prevPeriodHi;

                if (periodHi != prevPeriodHi) // Avoid resetting the sequence.
                {
                    WriteRegister(NesApu.MMC5_PL1_HI + regOffset, periodHi);
                    prevPeriodHi = periodHi;
                }

                WriteRegister(NesApu.MMC5_PL1_LO + regOffset, periodLo);
                WriteRegister(NesApu.MMC5_PL1_VOL + regOffset, (duty << 6) | (0x30) | volume);
            }
        }
    };
}
