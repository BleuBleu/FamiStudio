namespace FamiStudio
{
    public class ApuDpcmChannelState : ChannelState
    {
        public ApuDpcmChannelState(int apuIdx, int channelIdx) : base(apuIdx, channelIdx)
        {
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_SND_CHN, 0x0f);
            }
            else if (newNote && note.IsValid)
            {
                NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_SND_CHN, 0x0f);

                var mapping = FamiStudio.StaticProject.GetDPCMMapping(note.Value);
                if (mapping != null && mapping.Sample != null)
                {
                    NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_DMC_START, FamiStudio.StaticProject.GetAddressForSample(mapping.Sample) >> 6);
                    NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_DMC_LEN, mapping.Sample.Data.Length >> 4);
                    NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_DMC_FREQ, mapping.Pitch | (mapping.Loop ? 0x40 : 0x00));
                    NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_DMC_RAW, 32);
                    NesApu.NesApuWriteRegister(apuIdx, NesApu.APU_SND_CHN, 0x1f);
                }

                newNote = false;
            }
        }
    }
}
