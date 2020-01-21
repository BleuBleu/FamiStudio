using System;

namespace FamiStudio
{
    public class ChannelStateVrc6Saw : ChannelState
    {
        public ChannelStateVrc6Saw(int apuIdx, int channelIdx) : base(apuIdx, channelIdx)
        {
            maximumPeriod = NesApu.MaximumPeriod12Bit;
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                NesApu.WriteRegister(apuIdx, NesApu.VRC6_SAW_VOL, 0x00);
            }
            else if (note.IsValid)
            {
                var noteVal = Utils.Clamp(note.Value + envelopeValues[Envelope.Arpeggio], 0, noteTable.Length - 1);
                var period = Math.Min(maximumPeriod, noteTable[noteVal] + GetSlidePitch() + envelopeValues[Envelope.Pitch]);
                var volume = MultiplyVolumes(note.Volume, envelopeValues[Envelope.Volume]);

                // Get hi-bit from duty, similar to FamiTracker, but taking volume into account.
                // FamiTracker looses ability to output low volume when duty is odd.
                if ((duty & 1) != 0 && volume != 0)
                    volume = (volume << 1) + 1;

                NesApu.WriteRegister(apuIdx, NesApu.VRC6_SAW_VOL, (volume << 1)); 
                NesApu.WriteRegister(apuIdx, NesApu.VRC6_SAW_LO, ((period >> 0) & 0xff));
                NesApu.WriteRegister(apuIdx, NesApu.VRC6_SAW_HI, ((period >> 8) & 0x0f) | 0x80);
            }
        }
    };
}
