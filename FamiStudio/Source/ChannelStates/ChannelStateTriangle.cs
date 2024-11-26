using System;

namespace FamiStudio
{
    public class ChannelStateTriangle : ChannelState
    {
        public ChannelStateTriangle(IPlayerInterface player, int apuIdx, int channelType, int tuning, bool pal) : base(player, apuIdx, channelType, tuning, pal)
        {
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.APU_TRI_LINEAR, 0x80);
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod();
                var volume = GetVolume();

                WriteRegister(NesApu.APU_TRI_LO, (period >> 0) & 0xff);
                WriteRegister(NesApu.APU_TRI_HI, (period >> 8) & 0x07);
                WriteRegister(NesApu.APU_TRI_LINEAR, 0x80 | volume);
            }

            base.UpdateAPU();
        }
    }
}
