using System;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public static class NesApu
    {
        [DllImport("NesSndEmu.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static int NesApuInit(int apuIdx, int sampleRate, [MarshalAs(UnmanagedType.FunctionPtr)] DmcReadDelegate dmcCallback);
        [DllImport("NesSndEmu.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static void NesApuWriteRegister(int apuIdx, int addr, int data);
        [DllImport("NesSndEmu.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static int NesApuSamplesAvailable(int apuIdx);
        [DllImport("NesSndEmu.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static int NesApuReadSamples(int apuIdx, IntPtr buffer, int bufferSize);
        [DllImport("NesSndEmu.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static void NesApuRemoveSamples(int apuIdx, int count);
        [DllImport("NesSndEmu.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static int NesApuReadStatus(int apuIdx);
        [DllImport("NesSndEmu.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static void NesApuEndFrame(int apuIdx);
        [DllImport("NesSndEmu.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static void NesApuReset(int apuIdx);
        [DllImport("NesSndEmu.dll", CallingConvention = CallingConvention.StdCall)]
        public extern static void NesApuEnableChannel(int apuIdx, int idx, int enable);
        [DllImport("Kernel32.dll", EntryPoint = "RtlZeroMemory", SetLastError = false)]
        public static extern void ZeroMemory(IntPtr dest, int size);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int DmcReadDelegate(IntPtr data, int addr);

        public const int APU_SONG       = 0;
        public const int APU_INSTRUMENT = 1;
        public const int APU_WAV_EXPORT = 2;

        public const int APU_PL1_VOL    = 0x4000;
        public const int APU_PL1_SWEEP  = 0x4001;
        public const int APU_PL1_LO     = 0x4002;
        public const int APU_PL1_HI     = 0x4003;
        public const int APU_PL2_VOL    = 0x4004;
        public const int APU_PL2_SWEEP  = 0x4005;
        public const int APU_PL2_LO     = 0x4006;
        public const int APU_PL2_HI     = 0x4007;
        public const int APU_TRI_LINEAR = 0x4008;
        public const int APU_TRI_LO     = 0x400a;
        public const int APU_TRI_HI     = 0x400b;
        public const int APU_NOISE_VOL  = 0x400c;
        public const int APU_NOISE_LO   = 0x400e;
        public const int APU_NOISE_HI   = 0x400f;
        public const int APU_DMC_FREQ   = 0x4010;
        public const int APU_DMC_RAW    = 0x4011;
        public const int APU_DMC_START  = 0x4012;
        public const int APU_DMC_LEN    = 0x4013;
        public const int APU_SND_CHN    = 0x4015;

        public static readonly ushort[] NoteTableNTSC = new ushort[]
        {
            0x0000,0x06ad,0x064d,0x05f2,0x059d,0x054c,0x0500,0x04b8,0x0474,0x0434,0x03f7,0x03be,0x0388,0x0356,0x0326,0x02f8,
            0x02ce,0x02a5,0x027f,0x025b,0x0239,0x0219,0x01fb,0x01de,0x01c3,0x01aa,0x0192,0x017b,0x0166,0x0152,0x013f,0x012d,
            0x011c,0x010c,0x00fd,0x00ee,0x00e1,0x00d4,0x00c8,0x00bd,0x00b2,0x00a8,0x009f,0x0096,0x008d,0x0085,0x007e,0x0076,
            0x0070,0x0069,0x0063,0x005e,0x0058,0x0053,0x004f,0x004a,0x0046,0x0042,0x003e,0x003a,0x0037,0x0034,0x0031,0x002e
        };

        public static int DmcReadCallback(IntPtr data, int addr)
        {
            return FamiStudioForm.StaticProject.GetSampleForAddress(addr - 0xc000);
        }

        public static void Reset(int apuIdx)
        {
            NesApuReset(apuIdx);
            NesApuWriteRegister(apuIdx, APU_SND_CHN,    0x0f); // enable channels, stop DMC
            NesApuWriteRegister(apuIdx, APU_TRI_LINEAR, 0x80); // disable triangle length counter
            NesApuWriteRegister(apuIdx, APU_NOISE_HI,   0x00); // load noise length
            NesApuWriteRegister(apuIdx, APU_PL1_VOL,    0x30); // volumes to 0
            NesApuWriteRegister(apuIdx, APU_PL2_VOL,    0x30);
            NesApuWriteRegister(apuIdx, APU_NOISE_VOL,  0x30);
            NesApuWriteRegister(apuIdx, APU_PL1_SWEEP,  0x08); // no sweep
            NesApuWriteRegister(apuIdx, APU_PL2_SWEEP,  0x08);
        }
    }
}
