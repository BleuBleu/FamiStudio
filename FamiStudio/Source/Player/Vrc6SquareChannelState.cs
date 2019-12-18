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
            //if (note.IsStop)
            //{
            //    WriteApuRegister(NesApu.APU_PL1_VOL + regOffset, (duty << 6) | (0x30) | 0);
            //}
            //else if (note.IsValid)
            //{
            //    var noteVal = Utils.Clamp(note.Value + envelopeValues[Envelope.Arpeggio], 0, noteTable.Length - 1);
            //    int period = Math.Min(NesApu.MaximumPeriod, noteTable[noteVal] + envelopeValues[Envelope.Pitch]);

            //    WriteApuRegister(NesApu.APU_PL1_LO + regOffset, period & 0xff);
            //    period = (period >> 8) & 0x07;

            //    if (prevPulseHi != period)
            //    {
            //        prevPulseHi = period;
            //        WriteApuRegister(NesApu.APU_PL1_HI + regOffset, period);
            //    }

            //    WriteApuRegister(NesApu.APU_PL1_VOL + regOffset, (duty << 6) | (0x30) | MultiplyVolumes(note.Volume, envelopeValues[Envelope.Volume]));
            //}
        }
    };
}
