using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateVrc6Saw : ChannelState
    {
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
                var duty   = GetDuty();

                var sawMasterVolume = Vrc6SawMasterVolumeType.Full;

                if (note.Instrument != null)
                {
                    Debug.Assert(note.Instrument.ExpansionType == ExpansionType.Vrc6);
                    sawMasterVolume = note.Instrument.Vrc6SawMasterVolume;
                }

                WriteRegister(NesApu.VRC6_SAW_VOL, (volume << (2 - sawMasterVolume))); 
                WriteRegister(NesApu.VRC6_SAW_LO, ((period >> 0) & 0xff));
                WriteRegister(NesApu.VRC6_SAW_HI, ((period >> 8) & 0x0f) | 0x80);
            }

            base.UpdateAPU();
        }
    };
}
