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

        // Note.NoteMin (C1) maps to 4.
        public const int MinimumNote = 10;

        // Taken from FamiTracker.
        public static readonly ushort[] NoteTableNTSC = new ushort[]
        {
            0x0000,
            0x0d5b, 0x0c9c, 0x0be6, 0x0b3b, 0x0a9a, 0x0a01, 0x0972, 0x08ea, 0x086a, 0x07f1, 0x077f, 0x0713, // Octave 0                                                                    // Octave 0
            0x06ad, 0x064d, 0x05f3, 0x059d, 0x054c, 0x0500, 0x04b8, 0x0474, 0x0434, 0x03f8, 0x03bf, 0x0389, // Octave 1
            0x0356, 0x0326, 0x02f9, 0x02ce, 0x02a6, 0x0280, 0x025c, 0x023a, 0x021a, 0x01fb, 0x01df, 0x01c4, // Octave 2
            0x01ab, 0x0193, 0x017c, 0x0167, 0x0152, 0x013f, 0x012d, 0x011c, 0x010c, 0x00fd, 0x00ef, 0x00e1, // Octave 3
            0x00d5, 0x00c9, 0x00bd, 0x00b3, 0x00a9, 0x009f, 0x0096, 0x008e, 0x0086, 0x007e, 0x0077, 0x0070, // Octave 4
            0x006a, 0x0064, 0x005e, 0x0059, 0x0054, 0x004f, 0x004b, 0x0046, 0x0042, 0x003f, 0x003b, 0x0038, // Octave 5
            0x0034, 0x0031, 0x002f, 0x002c, 0x0029, 0x0027, 0x0025, 0x0023, 0x0021, 0x001f, 0x001d, 0x001b, // Octave 6
            0x001a, 0x0018, 0x0017, 0x0015, 0x0014, 0x0013, 0x0012, 0x0011, 0x0010, 0x000f, 0x000e, 0x000d  // Octave 7
        };

        // Taken from FamiTracker.
        public static readonly ushort[] NoteTablePAL = new ushort[]
        {
            0x0000,
            0x0c68, 0x0bb6, 0x0b0e, 0x0a6f, 0x09d9, 0x094b, 0x08c6, 0x0848, 0x07d1, 0x0760, 0x06f6, 0x0692, // Octave 0                                                                      // Octave 0
            0x0634, 0x05db, 0x0586, 0x0537, 0x04ec, 0x04a5, 0x0462, 0x0423, 0x03e8, 0x03b0, 0x037b, 0x0349, // Octave 1
            0x0319, 0x02ed, 0x02c3, 0x029b, 0x0275, 0x0252, 0x0231, 0x0211, 0x01f3, 0x01d7, 0x01bd, 0x01a4, // Octave 2
            0x018c, 0x0176, 0x0161, 0x014d, 0x013a, 0x0129, 0x0118, 0x0108, 0x00f9, 0x00eb, 0x00de, 0x00d1, // Octave 3
            0x00c6, 0x00ba, 0x00b0, 0x00a6, 0x009d, 0x0094, 0x008b, 0x0084, 0x007c, 0x0075, 0x006e, 0x0068, // Octave 4
            0x0062, 0x005d, 0x0057, 0x0052, 0x004e, 0x0049, 0x0045, 0x0041, 0x003e, 0x003a, 0x0037, 0x0034, // Octave 5
            0x0031, 0x002e, 0x002b, 0x0029, 0x0026, 0x0024, 0x0022, 0x0020, 0x001e, 0x001d, 0x001b, 0x0019, // Octave 6
            0x0018, 0x0016, 0x0015, 0x0014, 0x0013, 0x0012, 0x0011, 0x0010, 0x000f, 0x000e, 0x000d, 0x000c  // Octave 7
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
