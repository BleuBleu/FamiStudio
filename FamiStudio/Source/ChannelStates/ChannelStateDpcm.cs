using System.Collections.Generic;
namespace FamiStudio
{
    public class ChannelStateDpcm : ChannelState
    {
        public ChannelStateDpcm(IPlayerInterface player, int apuIdx, int channelIdx, int tuning, bool pal) : base(player, apuIdx, channelIdx, tuning, pal)
        {
        }

        public override void UpdateAPU()
        {
            var setCounter = false;

            if (note.IsStop)
            {
                WriteRegister(NesApu.APU_SND_CHN, 0x0f);
            }
            else if (note.IsMusical && noteTriggered)
            {
                WriteRegister(NesApu.APU_SND_CHN, 0x0f);

                var instrument = note.Instrument;
                if (instrument != null)
                {
                    var mapping = instrument.GetDPCMMapping(note.Value);
                    if (mapping != null)
                    {
                        var sample = mapping.Sample;
                        if (sample != null)
                        {
                            var dmcInitialValue = 0;

                            // Override by mapping, if enabled.
                            if (mapping.OverrideDmcInitialValue)
                            {
                                dmcInitialValue = mapping.DmcInitialValueDiv2 * 2;
                            }
                            else
                            {
                                dmcInitialValue = sample.DmcInitialValueDiv2 * 2;
                            }

                            // Override with effect, if present.
                            if (note.HasDeltaCounter)
                            {
                                dmcInitialValue = note.DeltaCounter;
                                note.HasDeltaCounter = false; // HACK : Clear so we don't set multiple times.
                            }

                            NesApu.CurrentSample[apuIdx] = sample.ProcessedData;

                            WriteRegister(NesApu.APU_DMC_START, 0, 4, sample.Id);
                            WriteRegister(NesApu.APU_DMC_LEN, sample.ProcessedData.Length >> 4);
                            WriteRegister(NesApu.APU_DMC_FREQ, mapping.Pitch | (mapping.Loop ? 0x40 : 0x00));
                            WriteRegister(NesApu.APU_DMC_RAW, dmcInitialValue);
                            WriteRegister(NesApu.APU_SND_CHN, 0x1f);

                            setCounter = true;
                        }
                    }
                }
            }
            
            if (note.HasDeltaCounter && !setCounter)
            {
                WriteRegister(NesApu.APU_DMC_RAW, note.DeltaCounter);
                note.HasDeltaCounter = false; // HACK : Clear so we don't set multiple times.
            }

            base.UpdateAPU();
        }
    }
}
