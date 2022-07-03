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

        private void WriteN163Register(int reg, int data)
        {
            // HACK : There are conflicts between N163 registers and S5B register, a N163 addr write
            // can be interpreted as a S5B data write. To prevent this, we select a dummy register 
            // for S5B so that the write is discarded.
            //
            // N163: 
            //   f800-ffff (addr)
            //   4800-4fff (data)
            // S5B:
            //   c000-e000 (addr)
            //   f000-ffff (data)

            if ((NesApu.GetAudioExpansions(apuIdx) & NesApu.APU_EXPANSION_MASK_SUNSOFT) != 0)
                WriteRegister(NesApu.S5B_ADDR, NesApu.S5B_REG_IO_A);

            WriteRegister(NesApu.N163_ADDR, reg);
            WriteRegister(NesApu.N163_DATA, data);
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
            {
                Debug.Assert(instrument.IsN163Instrument);

                if (instrument.IsN163Instrument)
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
                var wav = envelopes[EnvelopeType.N163Waveform].GetN163Waveform(newWaveIndex);

                for (int i = 0; i < wav.Length; i++)
                    WriteN163Register(wavePos + i, wav[i]);

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
    }}
