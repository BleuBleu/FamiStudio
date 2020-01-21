using System;

namespace FamiStudio
{
    class ChannelStateFds : ChannelState
    {
        private bool first = true;

        public ChannelStateFds(int apuIdx, int channelIdx) : base(apuIdx, channelIdx)
        {
            maximumPeriod = NesApu.MaximumPeriod12Bit;
        }

        public void LoadTestWave()
        {
            //var wav = new[] { 33, 36, 39, 42, 45, 48, 50, 53, 55, 57, 59, 60, 61, 62, 63, 63, 63, 63, 62, 61, 60, 59, 57, 55, 53, 50, 48, 45, 42, 39, 36, 33, 30, 27, 24, 21, 18, 15, 13, 10,  8,  6,  4,  3,  2,  1,  0,  0,  0,  0,  1,  2,  3,  4,  6,  8, 10, 13, 15, 18, 21, 24, 27, 30 };
            var wav = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63, 63 };
            var mod = new[] { 7, 7, 7, 7, 7, 7, 7, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 7, 7, 7, 7, 7, 7 };

            WriteRegister(NesApu.FDS_VOL, 0x80);

            for (int i = 0; i < 0x40; ++i)
                WriteRegister(NesApu.FDS_WAV_START + i, wav[i]);

            WriteRegister(NesApu.FDS_VOL, 0x00);
            WriteRegister(NesApu.FDS_MOD_HI, 0x80);
            WriteRegister(NesApu.FDS_SWEEP_BIAS, 0x00);

            for (int i = 0; i < 0x20; ++i)
                WriteRegister(NesApu.FDS_MOD_TABLE, mod[i]);
        }

        public override void UpdateAPU()
        {
            if (note.IsStop)
            {
                WriteRegister(NesApu.FDS_VOL_ENV, 0x80); // Zero volume
                WriteRegister(NesApu.FDS_FREQ_HI, 0x80); // Disable wave
            }
            else if (note.IsValid)
            {
                if (first)
                    LoadTestWave();

                var noteVal = Utils.Clamp(note.Value + envelopeValues[Envelope.Arpeggio], 0, noteTable.Length - 1);
                var period = Math.Min(maximumPeriod, noteTable[noteVal] + GetSlidePitch() + envelopeValues[Envelope.Pitch]);
                var volume = MultiplyVolumes(note.Volume, envelopeValues[Envelope.Volume]);

                WriteRegister(NesApu.FDS_FREQ_LO, (period >> 0) & 0xff);
                WriteRegister(NesApu.FDS_FREQ_HI, (period >> 8) & 0x0f);
                WriteRegister(NesApu.FDS_VOL_ENV, 0x80 | (volume << 2));

                if (first)
                {
                    WriteRegister(NesApu.FDS_SWEEP_BIAS, 0);
                    WriteRegister(NesApu.FDS_MOD_LO, 20);
                    WriteRegister(NesApu.FDS_MOD_HI,  0);
                    WriteRegister(NesApu.FDS_SWEEP_ENV, 0x80 | 20);
                }

                first = false;
            }
        }
    }
}
