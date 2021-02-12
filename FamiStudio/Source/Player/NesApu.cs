using System;
using System.Diagnostics;
using System.Linq;
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

        public const int APU_SONG         = 0;
        public const int APU_INSTRUMENT   = 1;
        public const int APU_WAV_EXPORT   = 2;

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
        public const int MaximumPeriod15Bit = 0x7fff;
        public const int MaximumPeriod16Bit = 0xffff;

        public const float FpsPAL  = 50.0070f;
        public const float FpsNTSC = 60.0988f;

        // Volume set in Nes_Apu::volume for the DMC channel. 
        public const float DPCMVolume = 0.42545f;

        // Default "center" value for the DPCM channel. May be configurable one day.
        public const int DACDefaultValue = 64;

        // All of our DPCM processing uses 1/2 values (0-63) since the
        // DMC channel increments/decrements by steps of 2 anyways.
        public const int DACDefaultValueDiv2 = DACDefaultValue / 2;

        public static readonly ushort[]   NoteTableNTSC    = new ushort[97];
        public static readonly ushort[]   NoteTablePAL     = new ushort[97];
        public static readonly ushort[]   NoteTableVrc6Saw = new ushort[97];
        public static readonly ushort[]   NoteTableVrc7    = new ushort[97];
        public static readonly ushort[]   NoteTableFds     = new ushort[97];
        public static readonly ushort[][] NoteTableN163    = new ushort[8][]
        {
            new ushort[97], 
            new ushort[97],
            new ushort[97],
            new ushort[97],
            new ushort[97],
            new ushort[97],
            new ushort[97],
            new ushort[97]
        };

#if DEBUG
        private static void DumpNoteTable(ushort[] noteTable, string name = "")
        {
            Debug.WriteLine($"_FT2{name}NoteTableLSB:");
            Debug.WriteLine($"\t.byte $00");
            for (int j = 0; j < 8; j++)
                Debug.WriteLine($"\t.byte {String.Join(",", noteTable.Select(i => $"${(byte)(i >> 0):x2}").ToArray(), j * 12 + 1, 12)} ; Octave {j}");

            Debug.WriteLine($"_FT2{name}NoteTableMSB:");
            Debug.WriteLine($"\t.byte $00");
            for (int j = 0; j < 8; j++)
                Debug.WriteLine($"\t.byte {String.Join(",", noteTable.Select(i => $"${(byte)(i >> 8):x2}").ToArray(), j * 12 + 1, 12)} ; Octave {j}");
        }
#endif

        // Taken from FamiTracker 
        public static void InitializeNoteTables()
        {
            const double BaseFreq = 32.7032; /// C0

            double clockNtsc = 1789773 / 16.0;
            double clockPal  = 1662607 / 16.0;

            for (int i = 1; i < NoteTableNTSC.Length; ++i)
            {
                var octave = (i - 1) / 12;
                var freq = BaseFreq * Math.Pow(2.0, (i - 1) / 12.0);

                NoteTableNTSC[i]    = (ushort)(clockNtsc / freq - 0.5);
                NoteTablePAL[i]     = (ushort)(clockPal  / freq - 0.5);
                NoteTableVrc6Saw[i] = (ushort)((clockNtsc * 16.0) / (freq * 14.0) - 0.5);
                NoteTableFds[i]     = (ushort)((freq * 65536.0) / (clockNtsc / 1.0) + 0.5);
                NoteTableVrc7[i]    = octave == 0 ? (ushort)(freq * 262144.0 / 49716.0 + 0.5) : (ushort)(NoteTableVrc7[(i - 1) % 12 + 1] << octave);

                for (int j = 0; j < 8; j++)
                    NoteTableN163[j][i] = (ushort)Math.Min(0xffff, ((freq * (j + 1) * 983040.0) / clockNtsc) / 4);
            }

#if FALSE //TRUE
            DumpNoteTable(NoteTableNTSC);
            DumpNoteTable(NoteTablePAL);
            DumpNoteTable(NoteTableVrc6Saw, "Saw");
            DumpNoteTable(NoteTableVrc7, "Vrc7");
            DumpNoteTable(NoteTableFds, "Fds");
            DumpNoteTable(NoteTableN163[0], "N163");
            DumpNoteTable(NoteTableN163[1], "N163");
            DumpNoteTable(NoteTableN163[2], "N163");
            DumpNoteTable(NoteTableN163[3], "N163");
            DumpNoteTable(NoteTableN163[4], "N163");
            DumpNoteTable(NoteTableN163[5], "N163");
            DumpNoteTable(NoteTableN163[6], "N163");
            DumpNoteTable(NoteTableN163[7], "N163");
#endif
        }

        public static ushort[] GetNoteTableForChannelType(int channelType, bool pal, int numN163Channels)
        {
            switch (channelType)
            {
                case ChannelType.Vrc6Saw:
                    return NoteTableVrc6Saw;
                case ChannelType.FdsWave:
                    return NoteTableFds;
                case ChannelType.N163Wave1:
                case ChannelType.N163Wave2:
                case ChannelType.N163Wave3:
                case ChannelType.N163Wave4:
                case ChannelType.N163Wave5:
                case ChannelType.N163Wave6:
                case ChannelType.N163Wave7:
                case ChannelType.N163Wave8:
                    return NoteTableN163[numN163Channels - 1];
                case ChannelType.Vrc7Fm1:
                case ChannelType.Vrc7Fm2:
                case ChannelType.Vrc7Fm3:
                case ChannelType.Vrc7Fm4:
                case ChannelType.Vrc7Fm5:
                case ChannelType.Vrc7Fm6:
                    return NoteTableVrc7;
                default:
                    return pal ? NoteTablePAL : NoteTableNTSC;
            }
        }

        public static ushort GetPitchLimitForChannelType(int channelType)
        {
            switch (channelType)
            {
                case ChannelType.FdsWave:
                case ChannelType.Vrc6Saw:
                case ChannelType.Vrc6Square1:
                case ChannelType.Vrc6Square2:
                    return NesApu.MaximumPeriod12Bit;
                case ChannelType.Vrc7Fm1:
                case ChannelType.Vrc7Fm2:
                case ChannelType.Vrc7Fm3:
                case ChannelType.Vrc7Fm4:
                case ChannelType.Vrc7Fm5:
                case ChannelType.Vrc7Fm6:
                    return NesApu.MaximumPeriod15Bit;
                case ChannelType.N163Wave1:
                case ChannelType.N163Wave2:
                case ChannelType.N163Wave3:
                case ChannelType.N163Wave4:
                case ChannelType.N163Wave5:
                case ChannelType.N163Wave6:
                case ChannelType.N163Wave7:
                case ChannelType.N163Wave8:
                    return NesApu.MaximumPeriod16Bit;
            }

            return NesApu.MaximumPeriod11Bit;
        }

        public static int DmcReadCallback(IntPtr data, int addr)
        {
            return FamiStudio.StaticProject.GetSampleForAddress(addr - 0xc000);
        }

        public static void InitAndReset(int apuIdx, int sampleRate, bool pal, int expansion, int numExpansionChannels, [MarshalAs(UnmanagedType.FunctionPtr)] DmcReadDelegate dmcCallback)
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
                    //TrebleEq(apuIdx, expansion, -48, 1000, sampleRate);
                    TrebleEq(apuIdx, expansion, -15, 2000, sampleRate);
                    break;
                case APU_EXPANSION_MMC5:
                    WriteRegister(apuIdx, MMC5_PL1_VOL, 0x10);
                    WriteRegister(apuIdx, MMC5_PL2_VOL, 0x10);
                    WriteRegister(apuIdx, MMC5_SND_CHN, 0x03); // Enable both square channels.
                    break;
                case APU_EXPANSION_VRC7:
                    WriteRegister(apuIdx, VRC7_SILENCE, 0x00); // Enable VRC7 audio.
                    break;
                case APU_EXPANSION_NAMCO:
                    // This is mainly because the instrument player might not update all the channels all the time.
                    WriteRegister(apuIdx, N163_ADDR, N163_REG_VOLUME); 
                    WriteRegister(apuIdx, N163_DATA, (numExpansionChannels - 1) << 4);
                    TrebleEq(apuIdx, expansion, -15, 4000, sampleRate);
                    break;
                case APU_EXPANSION_SUNSOFT:
                    WriteRegister(apuIdx, S5B_ADDR, S5B_REG_TONE);
                    WriteRegister(apuIdx, S5B_DATA, 0x38); // No noise, just 3 tones for now.
                    break;
            }
        }
    }
}
