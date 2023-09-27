﻿using System.Collections.Generic;
namespace FamiStudio
{
    public class ChannelStateDpcm : ChannelState
    {
        public ChannelStateDpcm(IPlayerInterface player, int apuIdx, int channelIdx, bool pal) : base(player, apuIdx, channelIdx, pal)
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
                HandleMusicalTriggered(ref setCounter);
            }

            if (note.HasDeltaCounter && !setCounter)
            {
                WriteRegister(NesApu.APU_DMC_RAW, note.DeltaCounter);
                note.HasDeltaCounter = false; // HACK : Clear so we don't set multiple times.
            }

            base.UpdateAPU();
        }

        private void HandleMusicalTriggered(ref bool setCounter)
        {
            WriteRegister(NesApu.APU_SND_CHN, 0x0f);

            var instrument = note.Instrument;

            if (instrument == null)
                return;

            var mapping = instrument.GetDPCMMapping(note.Value);

            if (mapping == null)
                return;

            var sample = mapping.Sample;

            if (sample == null)
                return;

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

            NesApu.CurrentSample.Value = sample.ProcessedData;

            WriteRegister(NesApu.APU_DMC_START, 0, 4, new List<int> { sample.Id });
            WriteRegister(NesApu.APU_DMC_LEN, sample.ProcessedData.Length >> 4);
            WriteRegister(NesApu.APU_DMC_FREQ, mapping.Pitch | (mapping.Loop ? 0x40 : 0x00));
            WriteRegister(NesApu.APU_DMC_RAW, dmcInitialValue);
            WriteRegister(NesApu.APU_SND_CHN, 0x1f);

            setCounter = true;
        }
    }
}
