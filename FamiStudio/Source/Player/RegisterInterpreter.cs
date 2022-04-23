using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            return regs.GetMergedRegisterValue(ExpansionType.None, NesApu.APU_PL1_LO + i * 4, NesApu.APU_PL1_HI + i * 4, 0xf);
        }

        public double GetSquareFrequency(int i)
        {
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

        public int    TrianglePeriod    => (regs.GetMergedRegisterValue(ExpansionType.None, NesApu.APU_TRI_LO, NesApu.APU_TRI_HI, 0xf));
        public double TriangleFrequency => NesPeriodToFreq(TrianglePeriod, 16);
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
            return 49715.0 * period / (1 << (18 - octave));
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
            return regs.CpuFrequency * (period / 1048576.0);
        }

        public bool   WaveEnabled => (regs.GetRegisterValue(ExpansionType.Fds, NesApu.FDS_FREQ_HI) & 0x80) == 0;
        public int    Period      => regs.GetMergedRegisterValue(ExpansionType.Fds, NesApu.FDS_FREQ_LO, NesApu.FDS_FREQ_HI, 0xf);
        public int    Volume      => regs.GetRegisterValue(ExpansionType.Fds, NesApu.FDS_VOL_ENV) & 0x1f;
        public double Frequency   => WaveEnabled ? FdsPeriodToFrequency(Period) : 0;

        public byte[] GetWaveTable()
        {
            var wav = new byte[64];
            for (int i = 0; i < 64; i++)
                wav[i] = (byte)(regs.GetRegisterValue(ExpansionType.Fds, NesApu.FDS_WAV_START + i) + 0x20);
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
            return regs.GetMergedRegisterValue(ExpansionType.Mmc5, NesApu.MMC5_PL1_LO + i * 4, NesApu.MMC5_PL1_HI + i * 4, 0xf);
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
            return (regs.GetRegisterValue(ExpansionType.Mmc5, NesApu.MMC5_PL1_VOL + i * 4) >> 4) & 0x7;
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
            return regs.GetRegisterValue(ExpansionType.N163, NesApu.N163_DATA, NesApu.N163_REG_VOLUME + i) & 0xf;
        }
    }

    public class S5BRegisterIntepreter
    {
        NesApu.NesRegisterValues regs;

        public S5BRegisterIntepreter(NesApu.NesRegisterValues r)
        {
            regs = r;
        }

        protected double NesPeriodToFreq(int period, int length)
        {
            return regs.CpuFrequency / (period + 1.0) / length;
        }

        public int GetPeriod(int i)
        {
            return regs.GetMergedSubRegisterValue(ExpansionType.S5B, NesApu.S5B_DATA, NesApu.S5B_REG_LO_A + i * 2, NesApu.S5B_REG_HI_A + i * 2, 0xf);
        }

        public double GetFrequency(int i)
        {
            return NesPeriodToFreq(GetPeriod(i), 16);
        }

        public int GetVolume(int i)
        {
            return regs.GetRegisterValue(ExpansionType.S5B, NesApu.S5B_DATA, NesApu.S5B_REG_VOL_A + i * 2) & 0xf;
        }
    }

    public class EpsmRegisterIntepreter
    {
        NesApu.NesRegisterValues regs;

        public EpsmRegisterIntepreter(NesApu.NesRegisterValues r)
        {
            regs = r;
        }

        protected double NesPeriodToFreq(int period, int length)
        {
            return regs.CpuFrequency / (period + 1.0) / length;
        }

        public int GetPeriod(int i)
        {
            return regs.GetMergedSubRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, NesApu.EPSM_REG_LO_A + i * 2, NesApu.EPSM_REG_HI_A + i * 2, 0xf);
        }

        public double GetFrequency(int i)
        {
            return NesPeriodToFreq(GetPeriod(i), 16);
        }

        public int GetVolume(int i)
        {
            return regs.GetRegisterValue(ExpansionType.EPSM, NesApu.EPSM_DATA0, NesApu.EPSM_REG_VOL_A + i * 2) & 0xf;
        }
    }
}
