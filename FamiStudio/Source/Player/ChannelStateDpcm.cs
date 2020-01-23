namespace FamiStudio
{
    public class ChannelStateDpcm : ChannelState
    {
        public ChannelStateDpcm(int apuIdx, int channelIdx) : base(apuIdx, channelIdx)
        {
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.APU_SND_CHN, 0x0f);
            }
            else if (note.IsValid)
            {
                WriteRegister(NesApu.APU_SND_CHN, 0x0f);

                var mapping = FamiStudio.StaticProject.GetDPCMMapping(note.Value);
                if (mapping != null && mapping.Sample != null)
                {
                    WriteRegister(NesApu.APU_DMC_START, FamiStudio.StaticProject.GetAddressForSample(mapping.Sample) >> 6);
                    WriteRegister(NesApu.APU_DMC_LEN, mapping.Sample.Data.Length >> 4);
                    WriteRegister(NesApu.APU_DMC_FREQ, mapping.Pitch | (mapping.Loop ? 0x40 : 0x00));
                    WriteRegister(NesApu.APU_DMC_RAW, 32);
                    WriteRegister(NesApu.APU_SND_CHN, 0x1f);
                }

                // To prevent from re-triggering.
                note.IsValid = false;
            }
        }
    }
}
