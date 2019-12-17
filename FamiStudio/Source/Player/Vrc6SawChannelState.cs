using System;

namespace FamiStudio
{
    public class Vrc6SawChannelState : ChannelState
    {
        public Vrc6SawChannelState(int apuIdx, int channelIdx) : base(apuIdx, channelIdx)
        {
            noteTable = NesApu.NoteTableVrc6Saw;
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteApuRegister(NesApu.VRC6_SAW_VOL, 0x00);
                WriteApuRegister(NesApu.VRC6_SAW_HI,  0x00); // Hi-bit disable channels
            }
            else if (note.IsValid)
            {
                var noteVal = Utils.Clamp(note.Value + envelopeValues[Envelope.Arpeggio], 0, noteTable.Length - 1);
                int period = Math.Min(NesApu.MaximumPeriod, noteTable[noteVal] + envelopeValues[Envelope.Pitch]);
                int volume = MultiplyVolumes(note.Volume, envelopeValues[Envelope.Volume]);

                WriteApuRegister(NesApu.VRC6_SAW_VOL, (volume << 1) | ((duty & 1) << 5)); // Get hi-bit from duty, like FamiTracker.
                WriteApuRegister(NesApu.VRC6_SAW_LO, ((period >> 0) & 0xff));
                WriteApuRegister(NesApu.VRC6_SAW_HI, ((period >> 8) & 0x0f) | 0x80);
            }
        }
    };
}
