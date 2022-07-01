using System;
using System.Diagnostics;

namespace FamiStudio
{
    class ChannelStateFds : ChannelState
    {
        byte   modDelayCounter;
        ushort modDepth;
        ushort modSpeed;
        int    waveIndex = -1;
        int    masterVolume;

        public ChannelStateFds(IPlayerInterface player, int apuIdx, int channelIdx) : base(player, apuIdx, channelIdx, false)
        {
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
            {
                Debug.Assert(instrument.IsFdsInstrument);

                if (instrument.IsFdsInstrument)
                {
                    var mod = instrument.Envelopes[EnvelopeType.FdsModulation].BuildFdsModulationTable();

                    Debug.Assert(mod.Length == 0x20);

                    waveIndex = -1;
                    masterVolume = instrument.FdsMasterVolume;

                    WriteRegister(NesApu.FDS_VOL, instrument.FdsMasterVolume);
                    WriteRegister(NesApu.FDS_MOD_HI, 0x80);
                    WriteRegister(NesApu.FDS_SWEEP_BIAS, 0x00);

                    for (int i = 0; i < 0x20; ++i)
                        WriteRegister(NesApu.FDS_MOD_TABLE, mod[i] & 0xff);
                }
            }
        }

        private void ConditionalLoadWave()
        {
            // MATTT : I think we start at wave index 1.
            // MATTT : The +1 fixed it, but test release as well.
            var newWaveIndex = envelopeIdx[EnvelopeType.WaveformRepeat];

            if (newWaveIndex != waveIndex)
            {
                var wav = envelopes[EnvelopeType.FdsWaveform].GetFdsWaveform(newWaveIndex);

                WriteRegister(NesApu.FDS_VOL, 0x80 | masterVolume);
                for (int i = 0; i < 0x40; ++i)
                    WriteRegister(NesApu.FDS_WAV_START + i, wav[i]);
                WriteRegister(NesApu.FDS_VOL, masterVolume);

                waveIndex = newWaveIndex;
            }
        }

        public override int GetEnvelopeFrame(int envIdx)
        {
            if (envIdx == EnvelopeType.FdsWaveform)
            {
                return waveIndex * 64 + NesApu.GetFdsWavePos(apuIdx);
            }
            else
            {
                return base.GetEnvelopeFrame(envIdx);
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
                ConditionalLoadWave();

                var period = GetPeriod();
                var volume = GetVolume();

                WriteRegister(NesApu.FDS_FREQ_HI, (period >> 8) & 0x0f);
                WriteRegister(NesApu.FDS_FREQ_LO, (period >> 0) & 0xff);
                WriteRegister(NesApu.FDS_VOL_ENV, 0x80 | (volume << 1));

                if (noteTriggered)
                {
                    WriteRegister(NesApu.FDS_SWEEP_BIAS, 0);

                    if (note.Instrument != null)
                    {
                        Debug.Assert(note.Instrument.IsFdsInstrument);
                        modDelayCounter = note.Instrument.FdsModDelay;
                        modDepth = note.Instrument.FdsModDepth;
                        modSpeed = note.Instrument.FdsModSpeed;
                    }
                    else
                    {
                        modDelayCounter = 0;
                    }
                }
            }

            if (note.HasFdsModDepth) modDepth = note.FdsModDepth;
            if (note.HasFdsModSpeed) modSpeed = note.FdsModSpeed;

            if (modDelayCounter == 0)
            {
                WriteRegister(NesApu.FDS_MOD_HI, (modSpeed >> 8) & 0xff);
                WriteRegister(NesApu.FDS_MOD_LO, (modSpeed >> 0) & 0xff);
                WriteRegister(NesApu.FDS_SWEEP_ENV, 0x80 | modDepth);
            }
            else
            {
                WriteRegister(NesApu.FDS_MOD_HI, 0x80);
                modDelayCounter--;
            }

            base.UpdateAPU();
        }
    }
}
