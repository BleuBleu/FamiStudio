namespace FamiStudio
{
    public class DPCMChannelState : ChannelState
    {
        public DPCMChannelState(int apuIdx, int channelIdx) : base(apuIdx, channelIdx)
        {
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteApuRegister(NesApu.APU_SND_CHN, 0x0f);
            }
            else if (newNote && note.IsValid)
            {
                WriteApuRegister(NesApu.APU_SND_CHN, 0x0f);

                var mapping = FamiStudio.StaticProject.SamplesMapping[note.Value];
                if (mapping != null && mapping.Sample != null)
                {
                    WriteApuRegister(NesApu.APU_DMC_START, FamiStudio.StaticProject.GetAddressForSample(mapping.Sample) >> 6);
                    WriteApuRegister(NesApu.APU_DMC_LEN, mapping.Sample.Data.Length >> 4);
                    WriteApuRegister(NesApu.APU_DMC_FREQ, mapping.Pitch | (mapping.Loop ? 0x40 : 0x00));
                    WriteApuRegister(NesApu.APU_DMC_RAW, 32);
                    WriteApuRegister(NesApu.APU_SND_CHN, 0x1f);
                }

                newNote = false;
            }
        }
    }
}
