using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateN163 : ChannelState
    {
        int channelIndex;
        int regOffset;
        int waveLength;
        int waveIndex = -1;
        int wavePos;
        int channelMask;

        public ChannelStateN163(IPlayerInterface player, int apuIdx, int channelType, int numChannels, bool pal) : base(player, apuIdx, channelType, pal, numChannels)
        {
            channelIndex = channelType - ChannelType.N163Wave1;
            regOffset = 8 * -channelIndex;
            channelMask = (numChannels - 1) << 4;
        }

        private void WriteN163Register(int reg, int data, int skip = 0)
        {
            WriteRegister(NesApu.N163_ADDR, reg);
            WriteRegister(NesApu.N163_DATA, data, skip);
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
            {
                Debug.Assert(instrument.IsN163);

                if (instrument.IsN163)
                {
                    // This can actually trigger if you tweak an instrument while playing a song.
                    //Debug.Assert(instrument.Envelopes[Envelope.N163Waveform].Length == instrument.N163WaveSize);

                    waveIndex = -1;
                    wavePos = instrument.N163WavePos / 2;
                    waveLength = 256 - instrument.N163WaveSize;

                    WriteN163Register(NesApu.N163_REG_WAVE + regOffset, instrument.N163WavePos);
                }
            }
        }

        private void ConditionalLoadWave()
        {
            var newWaveIndex = envelopeIdx[EnvelopeType.WaveformRepeat];
            
            if (newWaveIndex != waveIndex)
            {
                var waveEnv = envelopes[EnvelopeType.N163Waveform];

                // Can be null if the instrument was null.
                if (waveEnv != null)
                { 
                    var wav = waveEnv.GetN163Waveform(newWaveIndex);

                    for (int i = 0; i < wav.Length; i++)
                        WriteN163Register(wavePos + i, wav[i], 18); // 18 cycles approximately mimic our assembly loop.
                }

                waveIndex = newWaveIndex;
            }
        }

        public override int GetEnvelopeFrame(int envIdx)
        {
            if (envIdx == EnvelopeType.N163Waveform)
            {
                // Hi-byte of 24-bit internal counter is the position.
                return waveIndex * (256 - waveLength) + NesApu.GetN163WavePos(apuIdx, channelIndex);
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
                WriteN163Register(NesApu.N163_REG_VOLUME + regOffset, channelMask);
            }
            else if (note.IsMusical)
            {
                ConditionalLoadWave();

                var period = GetPeriod();
                var volume = GetVolume();

                // We clamp our periods to 16-bit for convenience, expand to 18-bit.
                period <<= 2;

                WriteN163Register(NesApu.N163_REG_FREQ_LO  + regOffset, (period >> 0) & 0xff);
                WriteN163Register(NesApu.N163_REG_FREQ_MID + regOffset, (period >> 8) & 0xff);
                WriteN163Register(NesApu.N163_REG_FREQ_HI  + regOffset, waveLength  | ((period >> 16) & 0x03));
                WriteN163Register(NesApu.N163_REG_VOLUME   + regOffset, channelMask | volume);
            }

            base.UpdateAPU();
        }

        protected override void ResetPhase()
        {
            NesApu.SkipCycles(apuIdx, 6); // ldy/lda
            WriteN163Register(NesApu.N163_REG_PHASE_LO  + regOffset, 0);
            NesApu.SkipCycles(apuIdx, 4); // lda
            WriteN163Register(NesApu.N163_REG_PHASE_MID + regOffset, 0);
            NesApu.SkipCycles(apuIdx, 4); // lda
            WriteN163Register(NesApu.N163_REG_PHASE_HI  + regOffset, 0);
        }

        public override void PostUpdate()
        {
            NesApu.SkipCycles(apuIdx, (channelIndex == 0 ? 2 : 0) + (resetPhase ? 10 : 11)); // ldx + lda/and/beq
            if (resetPhase)
            {
                ResetPhase();
                resetPhase = false;
            }
            NesApu.SkipCycles(apuIdx, 7); // inx/cpx/bne
        }
    }
}
