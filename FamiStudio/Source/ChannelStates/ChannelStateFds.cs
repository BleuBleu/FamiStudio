using System;
using System.Diagnostics;

namespace FamiStudio
{
    class ChannelStateFds : ChannelState
    {
        private byte   modDelayCounter;
        private ushort modDepth;
        private ushort modSpeed;
        private int    prevPeriodHi;

        public ChannelStateFds(IPlayerInterface player, int apuIdx, int channelIdx, int tuning, bool pal) : base(player, apuIdx, channelIdx, tuning, pal)
        {
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
            {
                Debug.Assert(instrument.IsFds);

                if (instrument.IsFds)
                {
                    var wav = instrument.Envelopes[EnvelopeType.FdsWaveform];
                    var mod = instrument.Envelopes[EnvelopeType.FdsModulation].BuildFdsModulationTable();

                    Debug.Assert(wav.Length == 0x40);
                    Debug.Assert(mod.Length == 0x20);

                    WriteRegister(NesApu.FDS_VOL, 0x80 | instrument.FdsMasterVolume);

                    for (int i = 0; i < 0x40; ++i)
                        WriteRegister(NesApu.FDS_WAV_START + i, wav.Values[i] & 0xff, 16); // 16 cycles to mimic ASM loop.
                    
                    WriteRegister(NesApu.FDS_VOL, instrument.FdsMasterVolume);
                    WriteRegister(NesApu.FDS_MOD_HI, 0x80);
                    WriteRegister(NesApu.FDS_SWEEP_BIAS, 0x00);

                    for (int i = 0; i < 0x20; ++i)
                        WriteRegister(NesApu.FDS_MOD_TABLE, mod[i] & 0xff, 16); // 16 cycles to mimic ASM loop.
                }
            }
        }

        public override int GetEnvelopeFrame(int envIdx)
        {
            if (envIdx == EnvelopeType.FdsWaveform)
            {
                return + NesApu.GetFdsWavePos(apuIdx);
            }
            else
            {
                return base.GetEnvelopeFrame(envIdx);
            }
        }

        public override void UpdateAPU()
        {
            var modDepthEffect = false;
            var modSpeedEffect = false;

            if (note.HasFdsModDepth) 
            { 
                modDepth = note.FdsModDepth; 
                modDepthEffect = true; 
            }

            if (note.HasFdsModSpeed) 
            { 
                modSpeed = note.FdsModSpeed; 
                modSpeedEffect = true; 
            }

            if (note.IsStop)
            {
                WriteRegister(NesApu.FDS_VOL_ENV, 0x80); // Zero volume
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod();
                var volume = GetVolume();
                var instrument = note.Instrument;

                var periodHi = (period >> 8) & 0x0f;
                prevPeriodHi = periodHi;

                WriteRegister(NesApu.FDS_FREQ_HI, periodHi);
                WriteRegister(NesApu.FDS_FREQ_LO, (period >> 0) & 0xff);
                WriteRegister(NesApu.FDS_VOL_ENV, 0x80 | (volume << 1));

                if (noteTriggered)
                {
                    // TODO: We used to set the modulation value here, but that's bad.
                    // https://www.nesdev.org/wiki/FDS_audio#Mod_frequency_high_($4087)
                    // WriteRegister(NesApu.FDS_MOD_HI, 0x80);
                    // WriteRegister(NesApu.FDS_SWEEP_BIAS, 0);

                    if (instrument != null)
                    {
                        Debug.Assert(instrument.IsFds);
                        modDelayCounter = instrument.FdsModDelay;
                        
                        // If there was a mod depth/speed this frame, it takes over the instrument.
                        if (!modDepthEffect)
                            modDepth = instrument.FdsModDepth;
                        if (!modSpeedEffect)
                            modSpeed = instrument.FdsModSpeed;
                    }
                    else
                    {
                        modDelayCounter = 0;
                    }
                }

                if (modDelayCounter == 0)
                {
                    var finalModSpeed = instrument != null && instrument.FdsAutoMod && modDepth > 0 ?
                        (ushort)Math.Min(period * instrument.FdsAutoModNumer / instrument.FdsAutoModDenom, 0xfff) : 
                        modSpeed;

                    WriteRegister(NesApu.FDS_MOD_HI, (finalModSpeed >> 8) & 0xff);
                    WriteRegister(NesApu.FDS_MOD_LO, (finalModSpeed >> 0) & 0xff);
                    WriteRegister(NesApu.FDS_SWEEP_ENV, 0x80 | modDepth);
                }
                else
                {
                    WriteRegister(NesApu.FDS_MOD_HI, 0x80);
                    modDelayCounter--;
                }
            }

            base.UpdateAPU();
        }

        protected override void ResetPhase()
        {
            SkipCycles(6); // lda/ora
            WriteRegister(NesApu.FDS_FREQ_HI, prevPeriodHi | 0x80);
            SkipCycles(2); // and
            WriteRegister(NesApu.FDS_FREQ_HI, prevPeriodHi);
        }
    }
}
