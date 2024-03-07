namespace FamiStudio
{
    public class ApuRegisterInterpreter
    {
        NesApu.NesRegisterValues regs;

        public ApuRegisterInterpreter(NesApu.NesRegisterValues r)
        {
            regs = r;
        }

        protected double NesPeriodToFreq(int period, int length)
        {
            return regs.CpuFrequency / (period + 1.0) / length;
        }

        public int GetSquarePeriod(int i)
        {
            return regs.GetMergedRegisterValue(ExpansionType.None, NesApu.APU_PL1_LO + i * 4, NesApu.APU_PL1_HI + i * 4, 0x7);
        }

        public double GetSquareFrequency(int i)
        {
            //The square channels are muted with period < 8 in both NTSC and PAL only on the 2A03
            if (GetSquarePeriod(i) < 8)
                return 0;   
            return NesPeriodToFreq(GetSquarePeriod(i), 16);
        }

        public int GetSquareVolume(int i)
        {
            return regs.GetRegisterValue(ExpansionType.None, NesApu.APU_PL1_VOL + i * 4) & 0xf;
        }

        public int GetSquareDuty(int i)
        {
            return (regs.GetRegisterValue(ExpansionType.None, NesApu.APU_PL1_VOL + i * 4) >> 6) & 0x3;
        }

        public int    TrianglePeriod    => (regs.GetMergedRegisterValue(ExpansionType.None, NesApu.APU_TRI_LO, NesApu.APU_TRI_HI, 0x7));
        public double TriangleFrequency => NesPeriodToFreq(TrianglePeriod, 32);
        public int    NoisePeriod       => (regs.GetRegisterValue(ExpansionType.None, NesApu.APU_NOISE_LO) & 0xf);
        public int    NoiseVolume       => (regs.GetRegisterValue(ExpansionType.None, NesApu.APU_NOISE_VOL) & 0xf);
        public int    NoiseMode         => (regs.GetRegisterValue(ExpansionType.None, NesApu.APU_NOISE_LO) >> 7) & 0x1;
        public int    DpcmFrequency     => (regs.GetRegisterValue(ExpansionType.None, NesApu.APU_DMC_FREQ) & 0xf);
        public bool   DpcmLoop          => (regs.GetRegisterValue(ExpansionType.None, NesApu.APU_DMC_FREQ) & 0x40) != 0;
        public int    DpcmSize          => (regs.GetRegisterValue(ExpansionType.None, NesApu.APU_DMC_LEN) << 4);
        public int    DpcmBytesLeft     => regs.Apu.DpcmBytesLeft;
        public int    DpcmDac           => regs.Apu.DpcmDac;
    }

    public class Vrc6RegisterInterpreter
    {
        NesApu.NesRegisterValues regs;

        public Vrc6RegisterInterpreter(NesApu.NesRegisterValues r)
        {
            regs = r;
        }

        protected double NesPeriodToFreq(int period, int length)
        {
            return regs.CpuFrequency / (period + 1.0) / length;
        }

        public int GetSquarePeriod(int i)
        {
            return regs.GetMergedRegisterValue(ExpansionType.Vrc6, NesApu.VRC6_PL1_LO + i * 0x1000, NesApu.VRC6_PL1_HI + i * 0x1000, 0xf);
        }

        public double GetSquareFrequency(int i)
        {
            return NesPeriodToFreq(GetSquarePeriod(i), 16);
        }

        public int GetSquareVolume(int i)
        {
            return regs.GetRegisterValue(ExpansionType.Vrc6, NesApu.VRC6_PL1_VOL + i * 0x1000) & 0xf;
        }

        public int GetSquareDuty(int i)
        {
            return (regs.GetRegisterValue(ExpansionType.Vrc6, NesApu.VRC6_PL1_VOL + i * 0x1000) >> 4) & 0x7;
        }

        public int    SawPeriod    => regs.GetMergedRegisterValue(ExpansionType.Vrc6, NesApu.VRC6_SAW_LO, NesApu.VRC6_SAW_HI, 0xf);
        public double SawFrequency => NesPeriodToFreq(SawPeriod, 14);
        public int    SawVolume    => regs.GetRegisterValue(ExpansionType.Vrc6, NesApu.VRC6_SAW_VOL) & 0x3f;
    }

    public class Vrc7RegisterIntepreter
    {
        NesApu.NesRegisterValues regs;

        public Vrc7RegisterIntepreter(NesApu.NesRegisterValues r)
        {
            regs = r;
        }

        private static double Vrc7PeriodToFrequency(int period, int octave)
        {
            return 49715.0 * period / (1 << (19 - octave));
        }

        public int GetPeriod(int i)
        {
            var periodLo = regs.GetRegisterValue(ExpansionType.Vrc7, NesApu.VRC7_REG_WRITE, NesApu.VRC7_REG_LO_1 + i);
            var periodHi = regs.GetRegisterValue(ExpansionType.Vrc7, NesApu.VRC7_REG_WRITE, NesApu.VRC7_REG_HI_1 + i);
            return ((periodHi & 1) << 8) | periodLo;
        }

        public int GetOctave(int i)
        {
            var periodHi = regs.GetRegisterValue(ExpansionType.Vrc7, NesApu.VRC7_REG_WRITE, NesApu.VRC7_REG_HI_1 + i);
            return (periodHi >> 1) & 0x7;
        }

        public double GetFrequency(int i)
        {
            return Vrc7PeriodToFrequency(GetPeriod(i), GetOctave(i));
        }

        public int GetVolume(int i)
        {
            return 15 - (regs.GetRegisterValue(ExpansionType.Vrc7, NesApu.VRC7_REG_WRITE, NesApu.VRC7_REG_VOL_1 + i) & 0xf);
        }

        public int GetPatch(int i)
        {
            return regs.GetRegisterValue(ExpansionType.Vrc7, NesApu.VRC7_REG_WRITE, NesApu.VRC7_REG_VOL_1 + i) >> 4;
        }
    }

    public class FdsRegisterIntepreter
    {
        NesApu.NesRegisterValues regs;
        public NesApu.NesRegisterValues Registers => regs;

        public FdsRegisterIntepreter(NesApu.NesRegisterValues r)
        {
            regs = r;
        }

        private double FdsPeriodToFrequency(int period)
        {
            return regs.CpuFrequency * (period / 4194304.0);
        }

        public bool   WaveEnabled => (regs.GetRegisterValue(ExpansionType.Fds, NesApu.FDS_FREQ_HI) & 0x80) == 0;
        public int    WavePeriod      => regs.GetMergedRegisterValue(ExpansionType.Fds, NesApu.FDS_FREQ_LO, NesApu.FDS_FREQ_HI, 0xf);
        public int    Volume      => regs.GetRegisterValue(ExpansionType.Fds, NesApu.FDS_VOL_ENV) & 0x1f;
        public double WaveFrequency   => WaveEnabled ? FdsPeriodToFrequency(WavePeriod) : 0;
        public bool   ModEnabled => (regs.GetRegisterValue(ExpansionType.Fds, NesApu.FDS_MOD_HI) & 0x8F) == 0 || (regs.GetRegisterValue(ExpansionType.Fds, NesApu.FDS_SWEEP_ENV) & 0x3f) != 0;
        public int    ModSpeed => regs.GetMergedRegisterValue(ExpansionType.Fds, NesApu.FDS_MOD_LO, NesApu.FDS_MOD_HI, 0xf);
        public int    ModDepth => regs.GetRegisterValue(ExpansionType.Fds, NesApu.FDS_SWEEP_ENV) & 0x3f;
        public double ModFrequency => ModEnabled ? FdsPeriodToFrequency(ModSpeed) : 0;

        public byte[] GetWaveTable()
        {
            var wav = new byte[64];
            for (int i = 0; i < 64; i++)
                wav[i] = regs.GetRegisterValue(ExpansionType.Fds, NesApu.FDS_WAV_START + i);
            return wav;
        }

        public byte[] GetModTable()
        {
            var mod = new byte[64];
            for (int i = 0; i < 64; i++)
                mod[i] = regs.GetRegisterValue(ExpansionType.Fds, NesApu.FDS_MOD_TABLE, i);
            return mod;
        }
    }

    public class Mmc5RegisterIntepreter
    {
        NesApu.NesRegisterValues regs;

        public Mmc5RegisterIntepreter(NesApu.NesRegisterValues r)
        {
            regs = r;
        }

        protected double NesPeriodToFreq(int period, int length)
        {
            return regs.CpuFrequency / (period + 1.0) / length;
        }

        public int GetSquarePeriod(int i)
        {
            return regs.GetMergedRegisterValue(ExpansionType.Mmc5, NesApu.MMC5_PL1_LO + i * 4, NesApu.MMC5_PL1_HI + i * 4, 0x7);
        }

        public double GetSquareFrequency(int i)
        {
            return NesPeriodToFreq(GetSquarePeriod(i), 16);
        }

        public int GetSquareVolume(int i)
        {
            return regs.GetRegisterValue(ExpansionType.Mmc5, NesApu.MMC5_PL1_VOL + i * 4) & 0xf;
        }

        public int GetSquareDuty(int i)
        {
            return (regs.GetRegisterValue(ExpansionType.Mmc5, NesApu.MMC5_PL1_VOL + i * 4) >> 6) & 0x3;
        }
    }

    public class N163RegisterIntepreter
    {
        private NesApu.NesRegisterValues regs;
        public NesApu.NesRegisterValues Registers => regs;

        public N163RegisterIntepreter(NesApu.NesRegisterValues r)
        {
            regs = r;
        }

        public int NumActiveChannels => (regs.GetRegisterValue(ExpansionType.N163, NesApu.N163_DATA, 0x7f) >> 4) + 1;

        private double N163PeriodToFreq(int period, int waveLen, int numChannels)
        {
            return period / (double)waveLen * regs.CpuFrequency / 983040.0 / numChannels;
        }

        public int GetPeriod(int i)
        {
            return regs.GetMergedSubRegisterValue(ExpansionType.N163, NesApu.N163_DATA, NesApu.N163_REG_FREQ_LO - 8 * i, NesApu.N163_REG_FREQ_MID - 8 * i, NesApu.N163_REG_FREQ_HI - 8 * i, 0x3);
        }

        public int GetWaveLength(int i)
        {
            return 256 - (regs.GetRegisterValue(ExpansionType.N163, NesApu.N163_DATA, NesApu.N163_REG_FREQ_HI - 8 * i) & 0xfc);
        }

        public double GetFrequency(int i)
        {
            return N163PeriodToFreq(GetPeriod(i), GetWaveLength(i), NumActiveChannels);
        }

        public int GetVolume(int i)
        {
            return regs.GetRegisterValue(ExpansionType.N163, NesApu.N163_DATA, NesApu.N163_REG_VOLUME - 8 * i) & 0xf;
        }
    }

    public class S5BRegisterIntepreter
    {
        NesApu.NesRegisterValues regs;

        public S5BRegisterIntepreter(NesApu.NesRegisterValues r)
        {
            regs = r;
        }

        protected double YM2149PeriodToFreq(int period, int length)
        {   
            if (period == 0) period = 1;
            return (double)regs.CpuFrequency / period / length;
        }

        public int GetPeriod(int i)
        {
            return regs.GetMergedSubRegisterValue(ExpansionType.S5B, NesApu.S5B_DATA, NesApu.S5B_REG_LO_A + i * 2, NesApu.S5B_REG_HI_A + i * 2, 0xf);
        }

        public double GetFrequency(int i)
        {
            return YM2149PeriodToFreq(GetPeriod(i), 16 * 2); 
            // 32 = 16*2, 16 from YM2149 datasheet (page 5), 2 from halved clock speed 
        }
        public int GetNoiseFrequency()
        {
            return regs.GetRegisterValue(ExpansionType.S5B, NesApu.S5B_DATA, NesApu.S5B_REG_NOISE_FREQ) & 0x1f;
        }
        public int GetVolume(int i)
        {
            return regs.GetRegisterValue(ExpansionType.S5B, NesApu.S5B_DATA, NesApu.S5B_REG_VOL_A + i) & 0xf;
        }
        public int GetEnvelopeShape()
        {
            return regs.GetRegisterValue(ExpansionType.S5B, NesApu.S5B_DATA, NesApu.S5B_REG_SHAPE);
        }
        public int GetEnvelopeLength()
        {
            return (GetEnvelopeShape() & (0b0001 | 0b0010 | 0b1000)) == 0b1010 ? 64 : 32;
            // only true when CONT, ALT, HOLD bits set (only actual triangle envelopes)
        }
        public int GetEnvelopePeriod()
        {
            return regs.GetMergedSubRegisterValue(ExpansionType.S5B, NesApu.S5B_DATA, NesApu.S5B_REG_ENV_LO, NesApu.S5B_REG_ENV_HI, 0xff);
        }
        public double GetEnvelopeFrequency()
        {
            return YM2149PeriodToFreq(GetEnvelopePeriod(), 256/32*2*GetEnvelopeLength());
            // 256, 32 from YM2149 datasheet (page 7), 2 from halved clock speed 
        }
        public bool GetMixerSetting(int channel)  // channel 0-2 - tone for channels ABC; 3-5 - noise 
        {
            return (regs.GetRegisterValue(ExpansionType.S5B, NesApu.S5B_DATA, NesApu.S5B_REG_MIXER_SETTING) & (0x01 << channel)) == 0;
        }
        public bool GetEnvelopeEnabled(int channel)  // channel 0-2 for channels ABC 
        {
            return (regs.GetRegisterValue(ExpansionType.S5B, NesApu.S5B_DATA, NesApu.S5B_REG_VOL_A+channel) & 0x10) != 0;
        }
    }

    public class EpsmRegisterIntepreter
    {
        NesApu.NesRegisterValues regs;

        public EpsmRegisterIntepreter(NesApu.NesRegisterValues r)
        {
            regs = r;
        }

        protected double YM2149PeriodToFreq(int period, int length)
        {
            if (period == 0) period = 1;
            return (double)NesApu.FreqEPSM / period / length;
        }

        public int GetPeriod(int i)
        {
            if (i < 3)
                return regs.GetMergedSubRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, NesApu.EPSM_REG_LO_A + i * 2, NesApu.EPSM_REG_HI_A + i * 2, 0xf);
            if (i >= 3 && i < 6)
            {
                var periodLo = regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, 0xA0 + i-3);
                var periodHi = regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, 0xA4 + i-3);
                return ((periodHi & 7) << 8) | periodLo;
            }
            if (i >= 6 && i < 9)
            {
                var periodLo = regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA1, 0xA0 + i-6);
                var periodHi = regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA1, 0xA4 + i-6);
                return ((periodHi & 7) << 8) | periodLo;
            }
            else
                return 0;
        }
        
        public int GetNoiseFrequency()
        {
            return regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, NesApu.EPSM_REG_NOISE_FREQ) & 0x1f;
        }

        public double GetFrequency(int i)
        {
            if (i < 3)
                return YM2149PeriodToFreq(GetPeriod(i), 64);
                // 64 from 
                // 1. YM2608 datasheet (page 37)
                // 2. YMF288 datasheet (page 14)
                // TODO: YMF288 datasheet page 14 situation of 0 <= Tp <= 7
            else
                return EpsmPeriodToFrequency(GetPeriod(i), GetOctave(i));
        }

        public int GetVolume(int i,int op = 0)
        {
            if(i < 3)
                return regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, NesApu.EPSM_REG_VOL_A + i) & 0xf;
            if (i >= 3 && i < 6)
                return regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, 0x40 + (i-3) + op * 4) & 0x7f;
            if (i >= 6 && i < 9)
                return regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA1, 0x40 + (i-6) + op * 4) & 0x7f;
            else
                return regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, 0x18 + i-9) & 0x1f;
        }

        public bool GetMixerSetting(int channel)  // channel 0-2 - tone for channels ABC; 3-5 - noise 
        {
            return (regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, NesApu.EPSM_REG_MIXER_SETTING) & (0x01 << channel)) == 0;
        }
        public bool GetEnvelopeEnabled(int channel)  // channel 0-2 for channels ABC 
        {
            return (regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, NesApu.EPSM_REG_VOL_A + channel) & 0x10) != 0;
        }

        public int GetEnvelopeShape()
        {
            return regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, NesApu.EPSM_REG_SHAPE);
        }
        public int GetEnvelopeLength()
        {
            return (GetEnvelopeShape() & (0b0001 | 0b0010 | 0b1000)) == 0b1010 ? 64 : 32;
            // only true when CONT, ALT, HOLD bits set (only actual triangle envelopes)
        }
        public int GetEnvelopePeriod()
        {
            return regs.GetMergedSubRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, NesApu.EPSM_REG_ENV_LO, NesApu.EPSM_REG_ENV_HI, 0xff);
        }
        public double GetEnvelopeFrequency()
        {
            return YM2149PeriodToFreq(GetEnvelopePeriod(), 1024/32*GetEnvelopeLength());
            // 1024 from YM2608 datasheet (page 39) 
            //  (not described in YMF288 datasheet, presumably unchanged),
            // 32 from YM2149 datasheet (page 7)
            //  (same term of "envelope repetition frequency")
        }

        public string GetStereo(int i)
        {
            if (i < 3)
                return "";
            if (i >= 3 && i < 6)
            {
                string left = ((regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, 0xb4 + i - 3) & 0x80) != 0) ? "L" : "-";
                string right = ((regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, 0xb4 + i - 3) & 0x40) != 0) ? "R" : "-";
                return left + right;

            }
            if (i >= 6 && i < 9)
            {
                string left = ((regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA1, 0xb4 + i - 6) & 0x80) != 0) ? "L" : "-";
                string right = ((regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA1, 0xb4 + i - 6) & 0x40) != 0) ? "R" : "-";
                return left + right;

            }
            else
            {
                string left = ((regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, 0x18 + i - 9) & 0x80) != 0) ? "L" : "-";
                string right = ((regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, 0x18 + i - 9) & 0x40) != 0) ? "R" : "-";
                return left + right;

            }
        }


        public int GetOctave(int i)
        {
            if (i >= 3 && i < 6)
            {
                var periodHi = regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, 0xA4 + i - 3);
                return ((periodHi & 0x38) >> 3);
            }
            if (i >= 6 && i < 9)
            {
                var periodHi = regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA1, 0xA4 + i - 6);
                return ((periodHi & 0x38) >> 3);
            }
            else
                return 0;
        }

        private static double EpsmPeriodToFrequency(int period, int octave)
        {
            return period / 144 / 2097152.0 * NesApu.FreqEPSM * (1 << octave);
        }

    }
}
