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
            channelMask = numChannels << 4;
            maximumPeriod = NesApu.MaximumPeriod18Bit;
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
                    WriteRegister(NesApu.N163_ADDR, instrument.NamcoWavePos + i / 2);
                    WriteRegister(NesApu.N163_DATA, pair);
                }

                WriteRegister(NesApu.N163_ADDR, 0x7e + regOffset);
                WriteRegister(NesApu.N163_DATA, instrument.NamcoWavePos);

                waveLength = 256 - instrument.NamcoWaveSize;
            }
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.N163_ADDR, 0x7f + regOffset);
                WriteRegister(NesApu.N163_DATA, channelMask);
            }
            else if (note.IsMusical)
            {
                var period = GetPeriod() << 3; // MATTT
                var volume = GetVolume();

                // MATTT: Create constants for these internal regs.
                WriteRegister(NesApu.N163_ADDR, 0x78 + regOffset);
                WriteRegister(NesApu.N163_DATA, period & 0xff);
                WriteRegister(NesApu.N163_ADDR, 0x7a + regOffset);
                WriteRegister(NesApu.N163_DATA, (period >> 8) & 0xff);
                WriteRegister(NesApu.N163_ADDR, 0x7c + regOffset);
                WriteRegister(NesApu.N163_DATA, waveLength);
                WriteRegister(NesApu.N163_ADDR, 0x7f + regOffset);
                WriteRegister(NesApu.N163_DATA, channelMask | volume);
            }
        }
    };
}
