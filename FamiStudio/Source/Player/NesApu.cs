using System;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public static class NesApu
    {
#if FAMISTUDIO_WINDOWS
        private const string NesSndEmuDll = "NesSndEmu.dll";
#elif FAMISTUDIO_MACOS
        private const string NesSndEmuDll = "NesSndEmu.dylib";
#else
        private const string NesSndEmuDll = "NesSndEmu.so";
#endif

        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuInit")]
        public extern static int Init(int apuIdx, int sampleRate, int pal, int expansion, [MarshalAs(UnmanagedType.FunctionPtr)] DmcReadDelegate dmcCallback);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuWriteRegister")]
        public extern static void WriteRegister(int apuIdx, int addr, int data);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuSamplesAvailable")]
        public extern static int SamplesAvailable(int apuIdx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuReadSamples")]
        public extern static int ReadSamples(int apuIdx, IntPtr buffer, int bufferSize);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuRemoveSamples")]
        public extern static void RemoveSamples(int apuIdx, int count);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuReadStatus")]
        public extern static int ReadStatus(int apuIdx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuEndFrame")]
        public extern static void EndFrame(int apuIdx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuReset")]
        public extern static void Reset(int apuIdx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuEnableChannel")]
        public extern static void EnableChannel(int apuIdx, int idx, int enable);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuStartSeeking")]
        public extern static void StartSeeking(int apuIdx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuStopSeeking")]
        public extern static void StopSeeking(int apuIdx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuIsSeeking")]
        public extern static int IsSeeking(int apuIdx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuTrebleEq")]
        public extern static void TrebleEq(int apuIdx, int expansion, double treble, int cutoff, int sample_rate);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuGetAudioExpansion")]
        public extern static int GetAudioExpansion(int apuIdx);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int DmcReadDelegate(IntPtr data, int addr);

        public const int APU_SONG       = 0;
        public const int APU_INSTRUMENT = 1;
        public const int APU_WAV_EXPORT = 2;
        public const int APU_COUNT      = 3;

        public const int APU_EXPANSION_NONE    = 0;
        public const int APU_EXPANSION_VRC6    = 1;
        public const int APU_EXPANSION_VRC7    = 2;
        public const int APU_EXPANSION_FDS     = 3;
        public const int APU_EXPANSION_MMC5    = 4;
        public const int APU_EXPANSION_NAMCO   = 5;
        public const int APU_EXPANSION_SUNSOFT = 6;

        public const int APU_PL1_VOL        = 0x4000;
        public const int APU_PL1_SWEEP      = 0x4001;
        public const int APU_PL1_LO         = 0x4002;
        public const int APU_PL1_HI         = 0x4003;
        public const int APU_PL2_VOL        = 0x4004;
        public const int APU_PL2_SWEEP      = 0x4005;
        public const int APU_PL2_LO         = 0x4006;
        public const int APU_PL2_HI         = 0x4007;
        public const int APU_TRI_LINEAR     = 0x4008;
        public const int APU_TRI_LO         = 0x400a;
        public const int APU_TRI_HI         = 0x400b;
        public const int APU_NOISE_VOL      = 0x400c;
        public const int APU_NOISE_LO       = 0x400e;
        public const int APU_NOISE_HI       = 0x400f;
        public const int APU_DMC_FREQ       = 0x4010;
        public const int APU_DMC_RAW        = 0x4011;
        public const int APU_DMC_START      = 0x4012;
        public const int APU_DMC_LEN        = 0x4013;
        public const int APU_SND_CHN        = 0x4015;
        public const int APU_FRAME_CNT      = 0x4017;
                                            
        public const int VRC6_CTRL          = 0x9003;
        public const int VRC6_PL1_VOL       = 0x9000;
        public const int VRC6_PL1_LO        = 0x9001;
        public const int VRC6_PL1_HI        = 0x9002;
        public const int VRC6_PL2_VOL       = 0xA000;
        public const int VRC6_PL2_LO        = 0xA001;
        public const int VRC6_PL2_HI        = 0xA002;
        public const int VRC6_SAW_VOL       = 0xB000;
        public const int VRC6_SAW_LO        = 0xB001;
        public const int VRC6_SAW_HI        = 0xB002;
                                            
        public const int VRC7_SILENCE       = 0xe000;
        public const int VRC7_REG_SEL       = 0x9010;
        public const int VRC7_REG_WRITE     = 0x9030;

        public const int VRC7_REG_LO_1      = 0x10;
        public const int VRC7_REG_LO_2      = 0x11;
        public const int VRC7_REG_LO_3      = 0x12;
        public const int VRC7_REG_LO_4      = 0x13;
        public const int VRC7_REG_LO_5      = 0x14;
        public const int VRC7_REG_LO_6      = 0x15;
        public const int VRC7_REG_HI_1      = 0x20;
        public const int VRC7_REG_HI_2      = 0x21;
        public const int VRC7_REG_HI_3      = 0x22;
        public const int VRC7_REG_HI_4      = 0x23;
        public const int VRC7_REG_HI_5      = 0x24;
        public const int VRC7_REG_HI_6      = 0x25;
        public const int VRC7_REG_VOL_1     = 0x30;
        public const int VRC7_REG_VOL_2     = 0x31;
        public const int VRC7_REG_VOL_3     = 0x32;
        public const int VRC7_REG_VOL_4     = 0x33;
        public const int VRC7_REG_VOL_5     = 0x34;
        public const int VRC7_REG_VOL_6     = 0x35;

        public const int FDS_WAV_START      = 0x4040;
        public const int FDS_VOL_ENV        = 0x4080;
        public const int FDS_FREQ_LO        = 0x4082;
        public const int FDS_FREQ_HI        = 0x4083;
        public const int FDS_SWEEP_ENV      = 0x4084;
        public const int FDS_SWEEP_BIAS     = 0x4085;
        public const int FDS_MOD_LO         = 0x4086;
        public const int FDS_MOD_HI         = 0x4087;
        public const int FDS_MOD_TABLE      = 0x4088;
        public const int FDS_VOL            = 0x4089;
        public const int FDS_ENV_SPEED      = 0x408A;

        public const int MMC5_PL1_VOL       = 0x5000;
        public const int MMC5_PL1_SWEEP     = 0x5001;
        public const int MMC5_PL1_LO        = 0x5002;
        public const int MMC5_PL1_HI        = 0x5003;
        public const int MMC5_PL2_VOL       = 0x5004;
        public const int MMC5_PL2_SWEEP     = 0x5005;
        public const int MMC5_PL2_LO        = 0x5006;
        public const int MMC5_PL2_HI        = 0x5007;
        public const int MMC5_SND_CHN       = 0x5015;
                                            
        public const int N163_SILENCE       = 0xe000;
        public const int N163_ADDR          = 0xf800;
        public const int N163_DATA          = 0x4800;
        
        public const int N163_REG_FREQ_LO   = 0x78;
        public const int N163_REG_PHASE_LO  = 0x79;
        public const int N163_REG_FREQ_MID  = 0x7a;
        public const int N163_REG_PHASE_MID = 0x7b;
        public const int N163_REG_FREQ_HI   = 0x7c;
        public const int N163_REG_PHASE_HI  = 0x7d;
        public const int N163_REG_WAVE      = 0x7e;
        public const int N163_REG_VOLUME    = 0x7f;

        public const int S5B_ADDR           = 0xc000;
        public const int S5B_DATA           = 0xe000;
                                            
        public const int S5B_REG_LO_A       = 0x00;
        public const int S5B_REG_HI_A       = 0x01;
        public const int S5B_REG_LO_B       = 0x02;
        public const int S5B_REG_HI_B       = 0x03;
        public const int S5B_REG_LO_C       = 0x04;
        public const int S5B_REG_HI_C       = 0x05;
        public const int S5B_REG_NOISE      = 0x06;
        public const int S5B_REG_TONE       = 0x07;
        public const int S5B_REG_VOL_A      = 0x08;
        public const int S5B_REG_VOL_B      = 0x09;
        public const int S5B_REG_VOL_C      = 0x0a;
        public const int S5B_REG_ENV_LO     = 0x0b;
        public const int S5B_REG_ENV_HI     = 0x0c;
        public const int S5B_REG_SHAPE      = 0x0d;
        public const int S5B_REG_IO_A       = 0x0e;
        public const int S5B_REG_IO_B       = 0x0f;

        // NES period was 11 bits.
        public const int MaximumPeriod11Bit = 0x7ff;
        public const int MaximumPeriod12Bit = 0xfff;
        public const int MaximumPeriod18Bit = 0x3ffff;

#if DEBUG
        public static bool seeking = false;
#endif

        // Taken from FamiTracker.
        public static readonly ushort[] NoteTableNTSC = new ushort[]
        {
            0x0000,
            0x0d5b, 0x0c9c, 0x0be6, 0x0b3b, 0x0a9a, 0x0a01, 0x0972, 0x08ea, 0x086a, 0x07f1, 0x077f, 0x0713, // Octave 0
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
            0x0c68, 0x0bb6, 0x0b0e, 0x0a6f, 0x09d9, 0x094b, 0x08c6, 0x0848, 0x07d1, 0x0760, 0x06f6, 0x0692, // Octave 0
            0x0634, 0x05db, 0x0586, 0x0537, 0x04ec, 0x04a5, 0x0462, 0x0423, 0x03e8, 0x03b0, 0x037b, 0x0349, // Octave 1
            0x0319, 0x02ed, 0x02c3, 0x029b, 0x0275, 0x0252, 0x0231, 0x0211, 0x01f3, 0x01d7, 0x01bd, 0x01a4, // Octave 2
            0x018c, 0x0176, 0x0161, 0x014d, 0x013a, 0x0129, 0x0118, 0x0108, 0x00f9, 0x00eb, 0x00de, 0x00d1, // Octave 3
            0x00c6, 0x00ba, 0x00b0, 0x00a6, 0x009d, 0x0094, 0x008b, 0x0084, 0x007c, 0x0075, 0x006e, 0x0068, // Octave 4
            0x0062, 0x005d, 0x0057, 0x0052, 0x004e, 0x0049, 0x0045, 0x0041, 0x003e, 0x003a, 0x0037, 0x0034, // Octave 5
            0x0031, 0x002e, 0x002b, 0x0029, 0x0026, 0x0024, 0x0022, 0x0020, 0x001e, 0x001d, 0x001b, 0x0019, // Octave 6
            0x0018, 0x0016, 0x0015, 0x0014, 0x0013, 0x0012, 0x0011, 0x0010, 0x000f, 0x000e, 0x000d, 0x000c  // Octave 7
        };

        // Taken from FamiTracker.
        public static readonly ushort[] NoteTableVrc6Saw = new ushort[]
        {
            0x0000,
	        0x0f44, 0x0e69, 0x0d9a, 0x0cd6, 0x0c1e, 0x0b70, 0x0acb, 0x0a30, 0x099e, 0x0913, 0x0891, 0x0816,  // Octave 0
            0x07a2, 0x0734, 0x06cc, 0x066b, 0x060e, 0x05b7, 0x0565, 0x0518, 0x04ce, 0x0489, 0x0448, 0x040a,  // Octave 1
            0x03d0, 0x0399, 0x0366, 0x0335, 0x0307, 0x02db, 0x02b2, 0x028b, 0x0267, 0x0244, 0x0223, 0x0205,  // Octave 2
            0x01e8, 0x01cc, 0x01b2, 0x019a, 0x0183, 0x016d, 0x0159, 0x0145, 0x0133, 0x0122, 0x0111, 0x0102,  // Octave 3
            0x00f3, 0x00e6, 0x00d9, 0x00cc, 0x00c1, 0x00b6, 0x00ac, 0x00a2, 0x0099, 0x0090, 0x0088, 0x0080,  // Octave 4
            0x0079, 0x0072, 0x006c, 0x0066, 0x0060, 0x005b, 0x0055, 0x0051, 0x004c, 0x0048, 0x0044, 0x0040,  // Octave 5
            0x003c, 0x0039, 0x0035, 0x0032, 0x002f, 0x002d, 0x002a, 0x0028, 0x0025, 0x0023, 0x0021, 0x001f,  // Octave 6
            0x001e, 0x001c, 0x001a, 0x0019, 0x0017, 0x0016, 0x0015, 0x0013, 0x0012, 0x0011, 0x0010, 0x000f   // Octave 7
        };

        // Taken from FamiTracker (same transpose convention)
        public static readonly ushort[] NoteTableFds = new ushort[]
        {
            0x0000,
            0x0013, 0x0014, 0x0016, 0x0017, 0x0018, 0x001a, 0x001b, 0x001d, 0x001e, 0x0020, 0x0022, 0x0024,  // Octave 0
            0x0026, 0x0029, 0x002b, 0x002e, 0x0030, 0x0033, 0x0036, 0x0039, 0x003d, 0x0040, 0x0044, 0x0048,  // Octave 1
            0x004d, 0x0051, 0x0056, 0x005b, 0x0061, 0x0066, 0x006c, 0x0073, 0x007a, 0x0081, 0x0089, 0x0091,  // Octave 2
            0x0099, 0x00a2, 0x00ac, 0x00b6, 0x00c1, 0x00cd, 0x00d9, 0x00e6, 0x00f3, 0x0102, 0x0111, 0x0121,  // Octave 3
            0x0133, 0x0145, 0x0158, 0x016d, 0x0182, 0x0199, 0x01b2, 0x01cb, 0x01e7, 0x0204, 0x0222, 0x0243,  // Octave 4
            0x0265, 0x028a, 0x02b0, 0x02d9, 0x0304, 0x0332, 0x0363, 0x0397, 0x03cd, 0x0407, 0x0444, 0x0485,  // Octave 5
            0x04ca, 0x0513, 0x0560, 0x05b2, 0x0609, 0x0665, 0x06c6, 0x072d, 0x079b, 0x080e, 0x0889, 0x090b,  // Octave 6
            0x0994, 0x0a26, 0x0ac1, 0x0b64, 0x0c12, 0x0cca, 0x0d8c, 0x0e5b, 0x0f35, 0x101d, 0x1112, 0x1216,  // Octave 7
        };

        // Taken from FamiTracker (same transpose convention)
        public static readonly ushort[] NoteTableN163 = new ushort[]
        {
            0x0000,
            0x0047, 0x004c, 0x0050, 0x0055, 0x005a, 0x005f, 0x0065, 0x006b, 0x0072, 0x0078, 0x0080, 0x0087,  // Octave 0
            0x008f, 0x0098, 0x00a1, 0x00aa, 0x00b5, 0x00bf, 0x00cb, 0x00d7, 0x00e4, 0x00f1, 0x0100, 0x010f,  // Octave 1
            0x011f, 0x0130, 0x0142, 0x0155, 0x016a, 0x017f, 0x0196, 0x01ae, 0x01c8, 0x01e3, 0x0200, 0x021e,  // Octave 2
            0x023e, 0x0260, 0x0285, 0x02ab, 0x02d4, 0x02ff, 0x032c, 0x035d, 0x0390, 0x03c6, 0x0400, 0x043d,  // Octave 3
            0x047d, 0x04c1, 0x050a, 0x0557, 0x05a8, 0x05fe, 0x0659, 0x06ba, 0x0720, 0x078d, 0x0800, 0x087a,  // Octave 4
            0x08fb, 0x0983, 0x0a14, 0x0aae, 0x0b50, 0x0bfd, 0x0cb3, 0x0d74, 0x0e41, 0x0f1a, 0x1000, 0x10f4,  // Octave 5
            0x11f6, 0x1307, 0x1429, 0x155c, 0x16a1, 0x17fa, 0x1967, 0x1ae9, 0x1c83, 0x1e35, 0x2001, 0x21e8,  // Octave 6
            0x23ec, 0x260f, 0x2852, 0x2ab8, 0x2d43, 0x2ff4, 0x32ce, 0x35d3, 0x3906, 0x3c6a, 0x4002, 0x43d1,  // Octave 7
        };

        // Taken from FamiTracker.
        public static readonly ushort[] NoteTableVrc7 = new ushort[]
        {
            688, 732, 776, 820, 868, 920, 976, 1032, 1096, 1160, 1228, 1304 // All octaves
        };

        public static ushort[] GetNoteTableForChannelType(int channelType, bool pal)
        {
            // TODO: PAL
            switch (channelType)
            {
                case Channel.Vrc6Saw:
                    return NoteTableVrc6Saw;
                case Channel.FdsWave:
                    return NoteTableFds;
                case Channel.N163Wave1:
                case Channel.N163Wave2:
                case Channel.N163Wave3:
                case Channel.N163Wave4:
                case Channel.N163Wave5:
                case Channel.N163Wave6:
                case Channel.N163Wave7:
                case Channel.N163Wave8:
                    return NoteTableN163;
                default:
                    return pal ? NoteTablePAL : NoteTableNTSC;
            }
        }

        public static ushort GetPitchLimitForChannelType(int channelType)
        {
            return (ushort)(channelType == Channel.Vrc6Saw ? 0xfff : 0x7ff);
        }

        public static int DmcReadCallback(IntPtr data, int addr)
        {
            return FamiStudio.StaticProject.GetSampleForAddress(addr - 0xc000);
        }

        public static void InitAndReset(int apuIdx, int sampleRate, bool pal, int expansion, [MarshalAs(UnmanagedType.FunctionPtr)] DmcReadDelegate dmcCallback)
        {
            Init(apuIdx, sampleRate, pal ? 1 : 0, expansion, dmcCallback);
            Reset(apuIdx);
            WriteRegister(apuIdx, APU_SND_CHN,    0x0f); // enable channels, stop DMC
            WriteRegister(apuIdx, APU_TRI_LINEAR, 0x80); // disable triangle length counter
            WriteRegister(apuIdx, APU_NOISE_HI,   0x00); // load noise length
            WriteRegister(apuIdx, APU_PL1_VOL,    0x30); // volumes to 0
            WriteRegister(apuIdx, APU_PL2_VOL,    0x30);
            WriteRegister(apuIdx, APU_NOISE_VOL,  0x30);
            WriteRegister(apuIdx, APU_PL1_SWEEP,  0x08); // no sweep
            WriteRegister(apuIdx, APU_PL2_SWEEP,  0x08);

            // These were the default values in Nes_Snd_Emu, review eventually.
            // FamiTracker by default has -24, 12000 respectively.
            const double treble = -8.87;
            const int    cutoff =  8800;

            TrebleEq(apuIdx, NesApu.APU_EXPANSION_NONE, treble, cutoff, sampleRate);

            switch (expansion)
            {
                case APU_EXPANSION_VRC6:
                    WriteRegister(apuIdx, VRC6_CTRL, 0x00);  // No halt, no octave change
                    TrebleEq(apuIdx, expansion, treble, cutoff, sampleRate);
                    break;
                case APU_EXPANSION_FDS:
                    // These are taken from FamiTracker. They smooth out the waveform extremely nicely!
                    TrebleEq(apuIdx, expansion, -48, 1000, sampleRate);
                    break;
                case APU_EXPANSION_MMC5:
                    WriteRegister(apuIdx, MMC5_SND_CHN, 0x03); // Enable both square channels.
                    break;
                case APU_EXPANSION_VRC7:
                    WriteRegister(apuIdx, VRC7_SILENCE, 0x00); // Enable VRC7 audio.
                    break;
                case APU_EXPANSION_SUNSOFT:
                    WriteRegister(apuIdx, S5B_ADDR, S5B_REG_TONE);
                    WriteRegister(apuIdx, S5B_DATA, 0x38); // No noise, just 3 tones for now.
                    break;
            }
        }
    }
}
