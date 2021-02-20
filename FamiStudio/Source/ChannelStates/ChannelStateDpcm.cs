namespace FamiStudio
{
    public class ChannelStateDpcm : ChannelState
    {
        public ChannelStateDpcm(int apuIdx, int channelIdx, bool pal) : base(apuIdx, channelIdx, pal)
        {
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.APU_SND_CHN, 0x0f);
            }
            else if (note.IsMusical && noteTriggered)
            {
                WriteRegister(NesApu.APU_SND_CHN, 0x0f);

                var mapping = FamiStudio.StaticProject.GetDPCMMapping(note.Value);
                if (mapping != null && mapping.Sample != null)
                {
                    var addr = FamiStudio.StaticProject.GetAddressForSample(mapping.Sample) >> 6;
                    if (addr >= 0 && addr <= 0xff) 
                    {
                        WriteRegister(NesApu.APU_DMC_START, addr);
                        WriteRegister(NesApu.APU_DMC_LEN, mapping.Sample.ProcessedData.Length >> 4);
                        WriteRegister(NesApu.APU_DMC_FREQ, mapping.Pitch | (mapping.Loop ? 0x40 : 0x00));
                        WriteRegister(NesApu.APU_DMC_RAW, NesApu.DACDefaultValue);
                        WriteRegister(NesApu.APU_SND_CHN, 0x1f);
                    }
                }
            }

            base.UpdateAPU();
        }
    }
}
