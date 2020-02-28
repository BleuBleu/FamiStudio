using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ChannelStateNamco : ChannelState
    {
        int regOffset;
        int waveLength;
        int channelMask;

        public ChannelStateNamco(int apuIdx, int channelType, int numChannels, bool pal) : base(apuIdx, channelType, pal)
        {
            regOffset = 8 * -(channelType - Channel.NamcoWave1);
            channelMask = (numChannels - 1) << 4;
            maximumPeriod = NesApu.MaximumPeriod18Bit;
        }

        private void WriteNamcoRegister(int reg, int data)
        {
            WriteRegister(NesApu.N163_ADDR, reg);
            WriteRegister(NesApu.N163_DATA, data);
        }

        protected override void LoadInstrument(Instrument instrument)
        {
            if (instrument != null)
            {
                Debug.Assert(instrument.Envelopes[Envelope.NamcoWaveform].Length == instrument.NamcoWaveSize);

                var wave = instrument.Envelopes[Envelope.NamcoWaveform];

                for (int i = 0; i < wave.Length; i += 2)
                {
                    var pair = (byte)(wave.Values[i + 1] << 4) | (byte)(wave.Values[i + 0]);
                    WriteNamcoRegister(instrument.NamcoWavePos + i / 2, pair);
                }

                WriteNamcoRegister(NesApu.N163_REG_WAVE + regOffset, instrument.NamcoWavePos);
                waveLength = 256 - instrument.NamcoWaveSize;
            }
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteNamcoRegister(NesApu.N163_REG_VOLUME + regOffset, channelMask);
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod() << 3; // MATTT
                var volume = GetVolume();

                WriteNamcoRegister(NesApu.N163_REG_FREQ_LO  + regOffset, (period >> 0) & 0xff);
                WriteNamcoRegister(NesApu.N163_REG_FREQ_MID + regOffset, (period >> 8) & 0xff);
                WriteNamcoRegister(NesApu.N163_REG_FREQ_HI  + regOffset, waveLength);
                WriteNamcoRegister(NesApu.N163_REG_VOLUME   + regOffset, channelMask | volume);
            }
        }
    };
}
