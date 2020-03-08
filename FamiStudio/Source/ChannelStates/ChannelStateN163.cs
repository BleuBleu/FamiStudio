using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateN163 : ChannelState
    {
        int regOffset;
        int waveLength;
        int channelMask;

        public ChannelStateN163(int apuIdx, int channelType, int numChannels, bool pal) : base(apuIdx, channelType, pal)
        {
            regOffset = 8 * -(channelType - Channel.N163Wave1);
            channelMask = (numChannels - 1) << 4;
            maximumPeriod = NesApu.MaximumPeriod16Bit; // Maximum is 18-bit, but we wont go there.
        }

        private void WriteN163Register(int reg, int data)
        {
            WriteRegister(NesApu.N163_ADDR, reg);
            WriteRegister(NesApu.N163_DATA, data);
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
            {
                Debug.Assert(instrument.Envelopes[Envelope.N163Waveform].Length == instrument.N163WaveSize);

                var wave = instrument.Envelopes[Envelope.N163Waveform];

                for (int i = 0; i < wave.Length; i += 2)
                {
                    var pair = (byte)(wave.Values[i + 1] << 4) | (byte)(wave.Values[i + 0]);
                    WriteN163Register(instrument.N163WavePos / 2 + i / 2, pair);
                }

                WriteN163Register(NesApu.N163_REG_WAVE + regOffset, instrument.N163WavePos);
                waveLength = 256 - instrument.N163WaveSize;
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
                var period = GetPeriod(3);
                var volume = GetVolume();

                WriteN163Register(NesApu.N163_REG_FREQ_LO  + regOffset, (period >> 0) & 0xff);
                WriteN163Register(NesApu.N163_REG_FREQ_MID + regOffset, (period >> 8) & 0xff);
                WriteN163Register(NesApu.N163_REG_FREQ_HI  + regOffset, waveLength  | ((period >> 16) & 0x03));
                WriteN163Register(NesApu.N163_REG_VOLUME   + regOffset, channelMask | volume);
            }

            base.UpdateAPU();
        }
    };
}
