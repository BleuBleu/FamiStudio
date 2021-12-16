using System;

namespace FamiStudio
{
    public class ChannelStateTriangle : ChannelState
    {
        bool isStop = false;
        public ChannelStateTriangle(IPlayerInterface player, int apuIdx, int channelType, bool pal) : base(player, apuIdx, channelType, pal)
        {
        }

        public override void UpdateAPU()
        {
            if (note.IsStop && !isStop)
            {
                isStop = true;
                WriteRegister(NesApu.APU_TRI_LINEAR, 0x80);
            }
            else if (note.IsMusical)
            {
                isStop = false;
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
