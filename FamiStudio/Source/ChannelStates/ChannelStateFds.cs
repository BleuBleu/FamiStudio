using System;
using System.Diagnostics;

namespace FamiStudio
{
    class ChannelStateFds : ChannelState
    {
        public ChannelStateFds(int apuIdx, int channelIdx) : base(apuIdx, channelIdx, false)
        {
            maximumPeriod = NesApu.MaximumPeriod12Bit;
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
            {
                Debug.Assert(instrument.ExpansionType == Project.ExpansionFds);

                var wav = instrument.Envelopes[Envelope.FdsWaveform];
                var mod = instrument.Envelopes[Envelope.FdsModulation].BuildFdsModulationTable();

                Debug.Assert(wav.Length == 0x40);
                Debug.Assert(mod.Length == 0x20);

                WriteRegister(NesApu.FDS_VOL, 0x80);

                for (int i = 0; i < 0x40; ++i)
                    WriteRegister(NesApu.FDS_WAV_START + i, wav.Values[i] & 0xff);

                WriteRegister(NesApu.FDS_VOL, 0x00);
                WriteRegister(NesApu.FDS_MOD_HI, 0x80);
                WriteRegister(NesApu.FDS_SWEEP_BIAS, 0x00);

                for (int i = 0; i < 0x20; ++i)
                    WriteRegister(NesApu.FDS_MOD_TABLE, mod[i] & 0xff);
            }
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.FDS_VOL_ENV, 0x80); // Zero volume
                WriteRegister(NesApu.FDS_FREQ_HI, 0x80); // Disable wave
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod();
                var volume = GetVolume();

                WriteRegister(NesApu.FDS_FREQ_LO, (period >> 0) & 0xff);
                WriteRegister(NesApu.FDS_FREQ_HI, (period >> 8) & 0x0f);
                WriteRegister(NesApu.FDS_VOL_ENV, 0x80 | (volume << 2));

                if (note.Instrument != null)
                {
                    Debug.Assert(note.Instrument.ExpansionType == Project.ExpansionFds);

                    WriteRegister(NesApu.FDS_SWEEP_BIAS, 0); // MATTT: Only do that on new notes.
                    WriteRegister(NesApu.FDS_MOD_LO, (note.Instrument.FdsModRate >> 0) & 0xff);
                    WriteRegister(NesApu.FDS_MOD_HI, (note.Instrument.FdsModRate >> 8) & 0xff);
                    WriteRegister(NesApu.FDS_SWEEP_ENV, 0x80 | note.Instrument.FdsModDepth); // MATTT: My modulation sounds like shit.
                }
                else
                {
                    WriteRegister(NesApu.FDS_MOD_HI, 0x80);
                }
            }
        }
    }
}
