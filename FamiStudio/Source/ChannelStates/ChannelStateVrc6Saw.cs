using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateVrc6Saw : ChannelState
    {
        int prevPeriodHi;

        public ChannelStateVrc6Saw(IPlayerInterface player, int apuIdx, int channelType) : base(player, apuIdx, channelType, false)
        {
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.VRC6_SAW_VOL, 0x00);
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod();
                var volume = GetVolume();
                var sawMasterVolume = Vrc6SawMasterVolumeType.Full;

                var periodHi = ((period >> 8) & 0x0f);
                prevPeriodHi = periodHi;

                if (note.Instrument != null)
                {
                    Debug.Assert(note.Instrument.IsVrc6);
                    sawMasterVolume = note.Instrument.Vrc6SawMasterVolume;
                }

                WriteRegister(NesApu.VRC6_SAW_VOL, (volume << (2 - sawMasterVolume))); 
                WriteRegister(NesApu.VRC6_SAW_LO, ((period >> 0) & 0xff));
                WriteRegister(NesApu.VRC6_SAW_HI, periodHi | 0x80);
            }

            base.UpdateAPU();
        }

        protected override void ResetPhase()
        {
            // Clear and set the hi-bit of B002 to reset phase.
            SkipCycles(6); // tax/lda
            WriteRegister(NesApu.VRC6_SAW_HI, prevPeriodHi);
            SkipCycles(2); // ora
            WriteRegister(NesApu.VRC6_SAW_HI, prevPeriodHi | 0x80);
            SkipCycles(2); // txa
        }
    }
}
