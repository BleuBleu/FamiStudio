using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace FamiStudio
{
    public static class NesApu
    {
        private const string NesSndEmuDll = Platform.DllStaticLib ? "__Internal" : Platform.DllPrefix + "NesSndEmu" + Platform.DllExtension;

        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuInit")]
        public extern static int Init(int apuIdx, int sampleRate, int bassFreq, int pal, int seperateTnd, int expansion, [MarshalAs(UnmanagedType.FunctionPtr)] DmcReadDelegate dmcCallback);
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
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuSkipCycles")]
        public extern static int SkipCycles(int apuIdx, int cycles);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuReset")]
        public extern static void Reset(int apuIdx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuEnableChannel")]
        public extern static void EnableChannel(int apuIdx, int exp, int idx, int enable);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuStartSeeking")]
        public extern static void StartSeeking(int apuIdx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuStopSeeking")]
        public extern static void StopSeeking(int apuIdx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuIsSeeking")]
        public extern static int IsSeeking(int apuIdx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuTrebleEq")]
        public extern static void TrebleEq(int apuIdx, int expansion, double treble, int trebleFreq, int sample_rate);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuGetAudioExpansions")]
        public extern static int GetAudioExpansions(int apuIdx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuSetExpansionVolume")]
        public extern static int SetExpansionVolume(int apuIdx, int expansion, double volume);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuGetRegisterValues")]
        public extern unsafe static void GetRegisterValues(int apuIdx, int exp, void* regs);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuGetN163WavePos")]
        public extern static int GetN163WavePos(int apuIdx, int n163ChanIndex);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuGetFdsWavePos")]
        public extern static int GetFdsWavePos(int apuIdx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuResetTriggers")]
        public extern static void ResetTriggers(int apuIdx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuGetChannelTrigger")]
        public extern static int GetChannelTrigger(int apuIdx, int exp, int idx);
        [DllImport(NesSndEmuDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "NesApuSetN163Mix")]
        public extern static void SetN163Mix(int apuIdx, int mix);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int DmcReadDelegate(IntPtr data, int addr);

        public const int APU_SONG           = 0;
        public const int APU_INSTRUMENT     = 1;
        public const int APU_WAV_EXPORT     = 2;

        public const int NUM_WAV_EXPORT_APU = 8;

        public const int APU_EXPANSION_NONE    = 0;
        public const int APU_EXPANSION_VRC6    = 1;
        public const int APU_EXPANSION_VRC7    = 2;
        public const int APU_EXPANSION_FDS     = 3;
        public const int APU_EXPANSION_MMC5    = 4;
        public const int APU_EXPANSION_NAMCO   = 5;
        public const int APU_EXPANSION_SUNSOFT = 6;
        public const int APU_EXPANSION_EPSM    = 7;

        public const int APU_EXPANSION_FIRST   = 1;
        public const int APU_EXPANSION_LAST    = 7;

        public const int APU_EXPANSION_MASK_NONE    = 0;
        public const int APU_EXPANSION_MASK_VRC6    = 1 << 0;
        public const int APU_EXPANSION_MASK_VRC7    = 1 << 1;
        public const int APU_EXPANSION_MASK_FDS     = 1 << 2;
        public const int APU_EXPANSION_MASK_MMC5    = 1 << 3;
        public const int APU_EXPANSION_MASK_NAMCO   = 1 << 4;
        public const int APU_EXPANSION_MASK_SUNSOFT = 1 << 5;
        public const int APU_EXPANSION_MASK_EPSM    = 1 << 6;

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
        public const int S5B_DATA           = 0xe001; // E001 to prevent conflict with VRC7.
                                            
        public const int S5B_REG_LO_A          = 0x00;
        public const int S5B_REG_HI_A          = 0x01;
        public const int S5B_REG_LO_B          = 0x02;
        public const int S5B_REG_HI_B          = 0x03;
        public const int S5B_REG_LO_C          = 0x04;
        public const int S5B_REG_HI_C          = 0x05;
        public const int S5B_REG_NOISE_FREQ    = 0x06;
        public const int S5B_REG_MIXER_SETTING = 0x07;
        public const int S5B_REG_VOL_A         = 0x08;
        public const int S5B_REG_VOL_B         = 0x09;
        public const int S5B_REG_VOL_C         = 0x0a;
        public const int S5B_REG_ENV_LO        = 0x0b;
        public const int S5B_REG_ENV_HI        = 0x0c;
        public const int S5B_REG_SHAPE         = 0x0d;
        public const int S5B_REG_IO_A          = 0x0e;
        public const int S5B_REG_IO_B          = 0x0f;

        public const int EPSM_ADDR0         = 0x401c;
        public const int EPSM_DATA0         = 0x401d;
        public const int EPSM_ADDR1         = 0x401e;
        public const int EPSM_DATA1         = 0x401f;

        public const int EPSM_REG_LO_A          = 0x00;
        public const int EPSM_REG_HI_A          = 0x01;
        public const int EPSM_REG_LO_B          = 0x02;
        public const int EPSM_REG_HI_B          = 0x03;
        public const int EPSM_REG_LO_C          = 0x04;
        public const int EPSM_REG_HI_C          = 0x05;
        public const int EPSM_REG_NOISE_FREQ    = 0x06;
        public const int EPSM_REG_MIXER_SETTING = 0x07;
        public const int EPSM_REG_VOL_A         = 0x08;
        public const int EPSM_REG_VOL_B         = 0x09;
        public const int EPSM_REG_VOL_C         = 0x0a;
        public const int EPSM_REG_ENV_LO        = 0x0b;
        public const int EPSM_REG_ENV_HI        = 0x0c;
        public const int EPSM_REG_SHAPE         = 0x0d;
        public const int EPSM_REG_IO_A          = 0x0e;
        public const int EPSM_REG_IO_B          = 0x0f;
        public const int EPSM_REG_RYTHM         = 0x10;
        public const int EPSM_REG_RYTHM_LEVEL   = 0x18;
        public const int EPSM_REG_FM_LO_A       = 0xA0;
        public const int EPSM_REG_FM_HI_A       = 0xA4;

        // See comment in Simple_Apu.h.
        public const int TND_MODE_SINGLE           = 0;
        public const int TND_MODE_SEPARATE         = 1;
        public const int TND_MODE_SEPARATE_TN_ONLY = 2;

        // Mirrored from Nes_Apu.h.
        public const int TRIGGER_NONE = -2; // Unable to provide trigger, must use fallback.
        public const int TRIGGER_HOLD = -1; // A valid trigger should be coming, hold previous valid one until.

        // NES period was 11 bits.
        public const int MaximumPeriod11Bit = 0x7ff;
        public const int MaximumPeriod12Bit = 0xfff;
        public const int MaximumPeriod15Bit = 0x7fff;
        public const int MaximumPeriod16Bit = 0xffff;

        public const float FpsPAL  = 50.0070f;
        public const float FpsNTSC = 60.0988f;

        // NesSndEmu assumes the same values.
        public const int FreqNtsc = 1789773;
        public const int FreqPal  = 1662607;
        public const int FreqEPSM = 8000000;

        public const double FreqRatioA4ToC1 = 1.0 / 13.454340859610068739450573644169; // Ratio between A4 (440 Hz) to C1 (32.70320 Hz).
        public const double FreqC0 = 16.3516;
        public const double FreqRegMin = 15.8862;  //The minimum frequency displayed in the registers tab, C0 - 49.9893 cents

        // One day this will be a setting.
        public const int EmulationSampleRate = 44100;

        // When playing back in the app, we always put samples at 0xc000. Completely arbitrary.
        public const int DPCMSampleAddr = 0xc000;

        // Volume set in Nes_Apu::volume for the DMC channel. This is simply to 
        // make sure our preview of DPCM sample somewhat matches the volume of 
        // the real emulated one.
        public const float DPCMVolume = 0.42545f;

        // Default "center" value for the DPCM channel. May be configurable one day.
        public const int DACDefaultValue = 64;

        // All of our DPCM processing uses 1/2 values (0-63) since the
        // DMC channel increments/decrements by steps of 2 anyways.
        public const int DACDefaultValueDiv2 = DACDefaultValue / 2;

        // Number of cycles to skip at each EPSM register writes.
        public const int EpsmCycleAddrSkip = 4;
        public const int EpsmCycleDataSkip = 20;
        public const int EpsmCycleDataSkipShort = 10;
        public const int EpsmCycleKeyOnSkip = 36;

        private class NoteTableSet
        {
            public readonly ushort[]   NoteTableNTSC       = new ushort[97];
            public readonly ushort[]   NoteTablePAL        = new ushort[97];
            public readonly ushort[]   NoteTableVrc6Saw    = new ushort[97];
            public readonly ushort[]   NoteTableVrc6SawPAL = new ushort[97];
            public readonly ushort[]   NoteTableVrc7       = new ushort[97];
            public readonly ushort[]   NoteTableFds        = new ushort[97];
            public readonly ushort[]   NoteTableFdsPAL     = new ushort[97];
            public readonly ushort[]   NoteTableEPSM       = new ushort[97];
            public readonly ushort[]   NoteTableEPSMFm     = new ushort[97];
            public readonly ushort[][] NoteTableN163       = new ushort[8][]
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
            public readonly ushort[][] NoteTableN163PAL    = new ushort[8][]
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
        };

        private static Dictionary<int, NoteTableSet> NoteTables = new Dictionary<int, NoteTableSet>();

        public static int ExpansionToMask(int exp)
        {
            return exp == APU_EXPANSION_NONE ? APU_EXPANSION_MASK_NONE : 1 << (exp - 1);
        }

        public static List<string> GetNoteTablesText(int tuning, int expansionMask = ExpansionType.AllMask, int machine = MachineType.NTSC, int numN163Channels = 8)
        {
            var tableSet = GetOrCreateNoteTableSet(tuning);
            var lines = new List<string>();
            
            if (machine == MachineType.NTSC || machine == MachineType.Dual)
            {
                lines.AddRange(GetNoteTableText(tableSet.NoteTableNTSC, "famistudio_note_table", "NTSC version"));
            }
            if (machine == MachineType.PAL || machine == MachineType.Dual)
            {
                lines.AddRange(GetNoteTableText(tableSet.NoteTablePAL, "famistudio_note_table_pal", "PAL version"));
            }
            if ((expansionMask & ExpansionType.Vrc6Mask) != 0)
            {
                if (machine == MachineType.NTSC || machine == MachineType.Dual)
                {
                    lines.AddRange(GetNoteTableText(tableSet.NoteTableVrc6Saw, "famistudio_saw_note_table", "VRC6 Saw"));
                }
                if (machine == MachineType.PAL || machine == MachineType.Dual)
                {
                    lines.AddRange(GetNoteTableText(tableSet.NoteTableVrc6SawPAL, "famistudio_saw_note_table_pal", "VRC6 Saw PAL"));
                }
            }
            if ((expansionMask & ExpansionType.Vrc7Mask) != 0)
            {
                lines.AddRange(GetNoteTableText(tableSet.NoteTableVrc7, "famistudio_vrc7_note_table", "VRC7"));
            }
            if ((expansionMask & ExpansionType.FdsMask) != 0)
            {
                if (machine == MachineType.NTSC || machine == MachineType.Dual)
                {
                    lines.AddRange(GetNoteTableText(tableSet.NoteTableFds, "famistudio_fds_note_table", "FDS"));
                }
                if (machine == MachineType.PAL || machine == MachineType.Dual)
                {
                    lines.AddRange(GetNoteTableText(tableSet.NoteTableFdsPAL, "famistudio_fds_note_table_pal", "FDS PAL"));
                }
            }
            if ((expansionMask & ExpansionType.N163Mask) != 0)
            {
                if (machine == MachineType.NTSC || machine == MachineType.Dual)
                {
                    lines.AddRange(GetNoteTableText(tableSet.NoteTableN163[numN163Channels - 1], $"famistudio_n163_note_table_{numN163Channels}ch", $"N163 ({numN163Channels} channel)"));
                }
                if (machine == MachineType.PAL || machine == MachineType.Dual)
                {
                    lines.AddRange(GetNoteTableText(tableSet.NoteTableN163PAL[numN163Channels - 1], $"famistudio_n163_note_table_{numN163Channels}ch_pal", $"N163 ({numN163Channels} channel) PAL"));
                }
            }
            if ((expansionMask & ExpansionType.EPSMMask) != 0)
            {
                lines.AddRange(GetNoteTableText(tableSet.NoteTableEPSMFm, "famistudio_epsm_note_table", "EPSM FM"));
                lines.AddRange(GetNoteTableText(tableSet.NoteTableEPSM, "famistudio_epsm_s_note_table", "EPSM Square"));
            }

            return lines;
        }

        private static Dictionary<string, byte[]> GetNoteTableBinaryData(List<string> tables)
        {
            var binData = new Dictionary<string, byte[]>();
            var byteList = new List<byte>();
            var name = string.Empty;

            foreach (var line in tables)
            {
                var trimLine = line.Trim();
                if (string.IsNullOrEmpty(trimLine) || trimLine.StartsWith(';'))
                    continue;

                if (trimLine.Contains("famistudio_"))
                {
                    name = trimLine.Split(":")[0];
                    continue;
                }

                var bytes = trimLine
                        .Replace(".byte", "")
                        .Replace("$", "")
                        .Split(';')[0]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(hex => Convert.ToByte(hex.Trim(), 16))
                        .ToArray();

                byteList.AddRange(bytes);

                if (byteList.Count == 97)
                {
                    binData[name] = byteList.ToArray();
                    byteList.Clear();
                }
            }

            return binData;
        }

        public static void DumpNoteTableBin(int tuning = 440, int expansionMask = ExpansionType.NoneMask, int machine = MachineType.NTSC, int numN163Channels = 8, string path = ".")
        {
            var noteTablesText = GetNoteTablesText(tuning, expansionMask, machine, numN163Channels);
            var binData = GetNoteTableBinaryData(noteTablesText);

            foreach (var kv in binData)
            {
                var outputFileName = Path.Combine(path, $"{kv.Key}.bin");

                using FileStream fs = new(outputFileName, FileMode.Create);
                using BinaryWriter writer = new(fs);

                writer.Write(kv.Value);
            }
        }

        public static void DumpNoteTableBinSet(int tuning = 440)
        {
            DumpNoteTableBin(tuning, ExpansionType.NoneMask, MachineType.NTSC);
            DumpNoteTableBin(tuning, ExpansionType.NoneMask, MachineType.PAL);
            DumpNoteTableBin(tuning, ExpansionType.Vrc6Mask, MachineType.NTSC);
            DumpNoteTableBin(tuning, ExpansionType.Vrc6Mask, MachineType.PAL);
            DumpNoteTableBin(tuning, ExpansionType.Vrc7Mask, MachineType.NTSC);
            DumpNoteTableBin(tuning, ExpansionType.FdsMask, MachineType.NTSC);
            DumpNoteTableBin(tuning, ExpansionType.FdsMask, MachineType.PAL);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.NTSC, 1);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.NTSC, 2);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.NTSC, 3);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.NTSC, 4);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.NTSC, 5);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.NTSC, 6);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.NTSC, 7);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.NTSC, 8);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.PAL, 1);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.PAL, 2);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.PAL, 3);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.PAL, 4);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.PAL, 5);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.PAL, 6);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.PAL, 7);
            DumpNoteTableBin(tuning, ExpansionType.N163Mask, MachineType.PAL, 8);
            DumpNoteTableBin(tuning, ExpansionType.EPSMMask, MachineType.NTSC);
        }

        public static void DumpNoteTableSetToFile(int tuning, string filename)
        {
            var lines = new List<string>();

            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.NoneMask, MachineType.NTSC));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.NoneMask, MachineType.PAL));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.Vrc6Mask, MachineType.NTSC));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.Vrc6Mask, MachineType.PAL));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.Vrc7Mask, MachineType.NTSC));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.FdsMask, MachineType.NTSC));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.FdsMask, MachineType.PAL));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.NTSC, 1));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.NTSC, 2));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.NTSC, 3));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.NTSC, 4));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.NTSC, 5));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.NTSC, 6));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.NTSC, 7));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.NTSC, 8));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.PAL, 1));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.PAL, 2));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.PAL, 3));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.PAL, 4));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.PAL, 5));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.PAL, 6));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.PAL, 7));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.N163Mask, MachineType.PAL, 8));
            lines.AddRange(GetNoteTablesText(tuning, ExpansionType.EPSMMask, MachineType.NTSC));

            using (var stream = new StreamWriter(filename))
            {
                foreach (var line in lines)
                {
                    stream.WriteLine(line);
                }
            }
        }

        private static List<string> GetNoteTableText(ushort[] noteTable, string name, string comment = null)
        {
            var lines = new List<string>();

            if (!string.IsNullOrEmpty(comment))
                lines.Add($"; {comment}");

            foreach (var (type, shift) in new[] { ("lsb", 0), ("msb", 8) })
            {
                lines.Add($"{name}_{type}:");
                lines.Add("\t.byte $00");

                for (int j = 0; j < 8; j++)
                    lines.Add($"\t.byte {string.Join(",", noteTable.Select(i => $"${(byte)(i >> shift):x2}").ToArray(), j * 12 + 1, 12)} ; Octave {j}");
            }

            return lines;
        }

        private static NoteTableSet GetOrCreateNoteTableSet(int tuning)
        {
            // MATTT : Use TLS to avoid blocking?
            lock (NoteTables)
            {
                if (NoteTables.TryGetValue(tuning, out var noteTableSet))
                {
                    return noteTableSet;
                }

                noteTableSet = new NoteTableSet();

                double freqC1 = tuning * FreqRatioA4ToC1;


                double clockNtsc = FreqNtsc / 16.0;
                double clockPal  = FreqPal  / 16.0;
                double clockEPSM = FreqEPSM / 32.0;

                for (int i = 1; i < noteTableSet.NoteTableNTSC.Length; ++i)
                {
                    var octave = (i - 1) / 12;
                    var freq = freqC1 * Math.Pow(2.0, (i - 1) / 12.0);
                    noteTableSet.NoteTableNTSC[i]       = (ushort)(clockNtsc / freq - 0.5);
                    noteTableSet.NoteTablePAL[i]        = (ushort)(clockPal  / freq - 0.5);
                    noteTableSet.NoteTableEPSM[i]       = (ushort)Math.Round(clockEPSM / freq);
                    noteTableSet.NoteTableEPSMFm[i]     = octave == 0 ? (ushort)((144 * (double)freq * 1048576 / 8000000)/4) : (ushort)((noteTableSet.NoteTableEPSMFm[(i - 1) % 12 + 1]) << octave);
                    noteTableSet.NoteTableVrc6Saw[i]    = (ushort)((clockNtsc * 16.0) / (freq * 14.0) - 0.5);
                    noteTableSet.NoteTableVrc6SawPAL[i] = (ushort)((clockPal * 16.0) / (freq * 14.0) - 0.5);
                    noteTableSet.NoteTableFds[i]        = (ushort)((freq * 65536.0) / (clockNtsc / 1.0) + 0.5);
                    noteTableSet.NoteTableFdsPAL[i]     = (ushort)((freq * 65536.0) / (clockPal / 1.0) + 0.5);
                    noteTableSet.NoteTableVrc7[i]       = octave == 0 ? (ushort)(freq * 262144.0 / 49715.0 + 0.5) : (ushort)(noteTableSet.NoteTableVrc7[(i - 1) % 12 + 1] << octave);

                    for (int j = 0; j < 8; j++)
                    {
                        noteTableSet.NoteTableN163[j][i]    = (ushort)Math.Min(0xffff, ((freq * (j + 1) * 983040.0) / clockNtsc) / 4);
                        noteTableSet.NoteTableN163PAL[j][i] = (ushort)Math.Min(0xffff, ((freq * (j + 1) * 983040.0) / clockPal) / 4);
                    }
                }

                #if FALSE // For debugging
                    DumpNoteTableSetToFile(noteTableSet, $"NoteTables{tuning}.txt");
                #endif

                NoteTables.Add(tuning, noteTableSet);
                return noteTableSet;
            }
        }

        // These structs must perfectly match the ones in NesSndEmu.
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct ApuRegisterValues
        {
            // $4000 to $4013
            public fixed byte Regs[20];
            public fixed byte Ages[20];

            // Extra internal states.
            public byte DpcmBytesLeft;
            public byte DpcmDac;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct Vrc6RegisterValues
        {
            // $9000 to $9002
            // $A000 to $A002
            // $B000 to $B002
            public fixed byte Regs[9];
            public fixed byte Ages[9];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct Vrc7RegisterValues
        {
            // $e000 
            public fixed byte Regs[1];
            public fixed byte Ages[1];

            // $9030 (Internal registers $00 to $35)
            public fixed byte InternalRegs[54];
            public fixed byte InternalAges[54];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct FdsRegisterValues
        {
            // $4080 to $408A
            public fixed byte Regs[11];
            public fixed byte Ages[11];

            // Waveform + modulation
            public fixed byte Wave[64];
            public fixed byte Mod [64];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct Mmc5RegisterValues
        {
            // $5000 to $5007
            // $5015
            public fixed byte Regs[9];
            public fixed byte Ages[9];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct N163RegisterValues
        {
            // $4800 (Internal registers 0 to 127).
            public fixed byte Regs[128];
            public fixed byte Ages[128];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct S5bRegisterValues
        {
            // e000 (Internal registers 0 to f).
            public fixed byte Regs[16];
            public fixed byte Ages[16];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct EpsmRegisterValues
        {
            // (Internal registers 0 to B4).
            public fixed byte Regs_A0[184];
            public fixed byte Ages_A0[184];
            public fixed byte Regs_A1[184];
            public fixed byte Ages_A1[184];
        }

        public struct N163InstrumentRange
        {
            public byte Pos;
            public byte Size;
            public bool AnyNotePlaying;
            public int  InstrumentId;
            public int  LastUpdate; // Higher number = more recently updated.
        }

        public unsafe class NesRegisterValues
        {
            public ApuRegisterValues  Apu;
            public Vrc6RegisterValues Vrc6;
            public Vrc7RegisterValues Vrc7;
            public FdsRegisterValues  Fds;
            public Mmc5RegisterValues Mmc5;
            public N163RegisterValues N163;
            public S5bRegisterValues  S5B;
            public EpsmRegisterValues Epsm;

            // Extra information for the register viewer.
            public int[] InstrumentIds = new int[ChannelType.Count];
            public Color[] InstrumentColors = new Color[ChannelType.Count];
            public N163InstrumentRange[]  N163InstrumentRanges = new N163InstrumentRange[8];

            private bool pal = false;

            public bool Pal => pal;
            public int  CpuFrequency => pal ? FreqPal : FreqNtsc;

            public void CopyTo(NesRegisterValues other)
            {
                other.Apu  = Apu;
                other.Vrc6 = Vrc6;
                other.Vrc7 = Vrc7;
                other.Fds  = Fds;
                other.Mmc5 = Mmc5;
                other.N163 = N163;
                other.S5B  = S5B;
                other.Epsm = Epsm;
                other.pal  = pal;

                Array.Copy(InstrumentIds, other.InstrumentIds, InstrumentIds.Length);
                Array.Copy(InstrumentColors, other.InstrumentColors, InstrumentColors.Length);
                Array.Copy(N163InstrumentRanges, other.N163InstrumentRanges, N163InstrumentRanges.Length);
            }

            public void ReadRegisterValues(int apuIdx)
            {
                var expansionMask = NesApu.GetAudioExpansions(apuIdx);

                fixed (void* p = &Apu)
                    NesApu.GetRegisterValues(apuIdx, ExpansionType.None, p);

                if ((expansionMask & ExpansionType.Vrc6Mask) != 0)
                {
                    fixed (void* p = &Vrc6)
                        NesApu.GetRegisterValues(apuIdx, ExpansionType.Vrc6, p);
                }

                if ((expansionMask & ExpansionType.Vrc7Mask) != 0)
                {
                    fixed (void* p = &Vrc7)
                        NesApu.GetRegisterValues(apuIdx, ExpansionType.Vrc7, p);
                }

                if ((expansionMask & ExpansionType.FdsMask) != 0)
                {
                    fixed (void* p = &Fds)
                        NesApu.GetRegisterValues(apuIdx, ExpansionType.Fds, p);
                }

                if ((expansionMask & ExpansionType.Mmc5Mask) != 0)
                {
                    fixed (void* p = &Mmc5)
                        NesApu.GetRegisterValues(apuIdx, ExpansionType.Mmc5, p);
                }

                if ((expansionMask & ExpansionType.N163Mask) != 0)
                {
                    fixed (void* p = &N163)
                        NesApu.GetRegisterValues(apuIdx, ExpansionType.N163, p);
                }

                if ((expansionMask & ExpansionType.S5BMask) != 0)
                {
                    fixed (void* p = &S5B)
                        NesApu.GetRegisterValues(apuIdx, ExpansionType.S5B, p);
                }

                if ((expansionMask & ExpansionType.EPSMMask) != 0)
                {
                    fixed (void* p = &Epsm)
                        NesApu.GetRegisterValues(apuIdx, ExpansionType.EPSM, p);
                }
            }

            public void SetPalMode(bool p)
            {
                pal = p;
            }

            private byte GetApuRegisterValue(int reg, out byte age)
            {
                var idx = reg - NesApu.APU_PL1_VOL;
                age = Apu.Ages[idx];
                return Apu.Regs[idx];
            }

            private byte GetVrc6RegisterValue(int reg, out byte age)
            {
                int idx;

                if (reg <= NesApu.VRC6_CTRL)
                    idx = reg - NesApu.VRC6_PL1_VOL;
                else if (reg <= NesApu.VRC6_PL2_HI)
                    idx = reg - NesApu.VRC6_PL2_VOL + 3;
                else
                    idx = reg - NesApu.VRC6_SAW_VOL + 6;

                age = Vrc6.Ages[idx];
                return Vrc6.Regs[idx];
            }

            private byte GetVrc7RegisterValue(int reg, int sub, out byte age)
            {
                Debug.Assert(reg == NesApu.VRC7_REG_WRITE || reg == NesApu.VRC7_SILENCE);

                if (reg == NesApu.VRC7_SILENCE)
                {
                    age = Vrc7.Ages[0];
                    return Vrc7.Regs[0];
                }
                else
                {
                    age = Vrc7.InternalAges[sub];
                    return Vrc7.InternalRegs[sub];
                }
            }

            private byte GetFdsRegisterValue(int reg, int sub, out byte age)
            {
                if (reg < NesApu.FDS_VOL_ENV)
                {
                    age = 0xff;
                    return Fds.Wave[reg - NesApu.FDS_WAV_START];
                }
                else if (reg == NesApu.FDS_MOD_TABLE && sub >= 0) // Hacky wait to access the mod table.
                {
                    age = 0xff;
                    return Fds.Mod[sub];
                }
                else
                {
                    var idx = reg - NesApu.FDS_VOL_ENV;
                    age = Fds.Ages[idx];
                    return Fds.Regs[idx];
                }
            }

            private byte GetN163RegisterValue(int reg, int sub, out byte age)
            {
                Debug.Assert(reg == NesApu.N163_DATA);

                age = N163.Ages[sub];
                return N163.Regs[sub];
            }

            private byte GetMmc5RegisterValue(int reg, out byte age)
            {
                var idx = reg == NesApu.MMC5_SND_CHN ? 8 : reg - NesApu.MMC5_PL1_VOL;
                age = Mmc5.Ages[idx];
                return Mmc5.Regs[idx];
            }

            private byte GetS5BRegisterValue(int reg, int sub, out byte age)
            {
                Debug.Assert(reg == NesApu.S5B_DATA);

                age = S5B.Ages[sub];
                return S5B.Regs[sub];
            }

            private byte GetEpsmRegisterValue_A0(int reg, int sub, out byte age)
            {
                //Debug.Assert(reg == NesApu.EPSM_DATA0 || reg == NesApu.EPSM_DATA1);
                if (reg == NesApu.EPSM_DATA0)
                {
                    age = Epsm.Ages_A0[sub];
                    return Epsm.Regs_A0[sub];
                }
                else
                {
                    age = Epsm.Ages_A1[sub];
                    return Epsm.Regs_A1[sub];
                }
            }

            public byte GetRegisterValue(int exp, int reg, out byte age, int sub = -1)
            {
                switch (exp)
                {
                    case ExpansionType.None: return GetApuRegisterValue(reg, out age);
                    case ExpansionType.Vrc6: return GetVrc6RegisterValue(reg, out age);
                    case ExpansionType.Vrc7: return GetVrc7RegisterValue(reg, sub, out age);
                    case ExpansionType.Fds:  return GetFdsRegisterValue(reg, sub, out age);
                    case ExpansionType.Mmc5: return GetMmc5RegisterValue(reg, out age);
                    case ExpansionType.N163: return GetN163RegisterValue(reg, sub, out age);
                    case ExpansionType.S5B:  return GetS5BRegisterValue(reg, sub, out age);
                    case ExpansionType.EPSM: return GetEpsmRegisterValue_A0(reg, sub, out age);
                }

                Debug.Assert(false);
                age = 0;
                return 0;
            }

            public byte GetRegisterValue(int exp, int reg, int sub = -1)
            {
                return GetRegisterValue(exp, reg, out _, sub);
            }

            public int GetMergedRegisterValue(int exp, int regLo, int regHi, int maskHi)
            {
                var periodLo = GetRegisterValue(exp, regLo);
                var periodHi = GetRegisterValue(exp, regHi) & maskHi;

                return (periodHi << 8) | periodLo;
            }

            public int GetMergedSubRegisterValue(int exp, int reg, int subLo, int subHi, int maskHi)
            {
                var periodLo = GetRegisterValue(exp, reg, subLo);
                var periodHi = GetRegisterValue(exp, reg, subHi) & maskHi;

                return (periodHi << 8) | periodLo;
            }

            public int GetMergedSubRegisterValue(int exp, int reg, int subLo, int subMi, int subHi, int maskHi)
            {
                var periodLo = GetRegisterValue(exp, reg, subLo);
                var periodMd = GetRegisterValue(exp, reg, subMi);
                var periodHi = GetRegisterValue(exp, reg, subHi) & maskHi;

                return (periodHi << 16) | (periodMd << 8) | periodLo;
            }
        };

        public static ushort[] GetNoteTableForChannelType(int channelType, bool pal, int numN163Channels, int tuning = 440)
        {
            var noteTableSet = GetOrCreateNoteTableSet(tuning);

            switch (channelType)
            {
                case ChannelType.Vrc6Saw:
                    return pal ? noteTableSet.NoteTableVrc6SawPAL : noteTableSet.NoteTableVrc6Saw;
                case ChannelType.FdsWave:
                    return pal ? noteTableSet.NoteTableFdsPAL : noteTableSet.NoteTableFds;
                case ChannelType.N163Wave1:
                case ChannelType.N163Wave2:
                case ChannelType.N163Wave3:
                case ChannelType.N163Wave4:
                case ChannelType.N163Wave5:
                case ChannelType.N163Wave6:
                case ChannelType.N163Wave7:
                case ChannelType.N163Wave8:
                    return pal ? noteTableSet.NoteTableN163PAL[numN163Channels - 1] : noteTableSet.NoteTableN163[numN163Channels - 1];
                case ChannelType.Vrc7Fm1:
                case ChannelType.Vrc7Fm2:
                case ChannelType.Vrc7Fm3:
                case ChannelType.Vrc7Fm4:
                case ChannelType.Vrc7Fm5:
                case ChannelType.Vrc7Fm6:
                    return noteTableSet.NoteTableVrc7;
                case ChannelType.EPSMSquare1:
                case ChannelType.EPSMSquare2:
                case ChannelType.EPSMSquare3:
                    return noteTableSet.NoteTableEPSM;
                case ChannelType.EPSMFm1:
                case ChannelType.EPSMFm2:
                case ChannelType.EPSMFm3:
                case ChannelType.EPSMFm4:
                case ChannelType.EPSMFm5:
                case ChannelType.EPSMFm6:
                    return noteTableSet.NoteTableEPSMFm;
                default:
                    return pal ? noteTableSet.NoteTablePAL : noteTableSet.NoteTableNTSC;
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
                case ChannelType.EPSMSquare1:
                case ChannelType.EPSMSquare2:
                case ChannelType.EPSMSquare3:
                case ChannelType.S5BSquare1:
                case ChannelType.S5BSquare2:
                case ChannelType.S5BSquare3:
                    return NesApu.MaximumPeriod12Bit;
                case ChannelType.Vrc7Fm1:
                case ChannelType.Vrc7Fm2:
                case ChannelType.Vrc7Fm3:
                case ChannelType.Vrc7Fm4:
                case ChannelType.Vrc7Fm5:
                case ChannelType.Vrc7Fm6:
                case ChannelType.EPSMFm1:
                case ChannelType.EPSMFm2:
                case ChannelType.EPSMFm3:
                case ChannelType.EPSMFm4:
                case ChannelType.EPSMFm5:
                case ChannelType.EPSMFm6:
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

        public static byte[][] CurrentSample = new byte[2 + NUM_WAV_EXPORT_APU][];

        public static int DmcReadCallback(IntPtr data, int addr)
        {
            var apuIdx = data.ToInt32();
            var sample = CurrentSample[apuIdx];
            var offset = addr - DPCMSampleAddr;
            if (sample == null || offset < 0 || offset >= sample.Length)
                return DACDefaultValue;
            else
                return sample[offset];
        }

        public static void InitAndReset(
            int apuIdx, 
            int sampleRate,
            float globalVolumeDb,
            int bassCutoffHz,
            ExpansionMixer[] expMixerSettings,
            bool pal, 
            bool namcoMix,
            int seperateTndMode, 
            int expansions, 
            int numNamcoChannels, 
            [MarshalAs(UnmanagedType.FunctionPtr)] DmcReadDelegate dmcCallback)
        {
            Init(apuIdx, sampleRate, bassCutoffHz, pal ? 1 : 0, seperateTndMode, expansions, dmcCallback);
            Reset(apuIdx);

            var apuSettings = expMixerSettings[NesApu.APU_EXPANSION_NONE];
            TrebleEq(apuIdx, NesApu.APU_EXPANSION_NONE, apuSettings.TrebleDb, apuSettings.TrebleRolloffHz, sampleRate);
            SetExpansionVolume(apuIdx, NesApu.APU_EXPANSION_NONE, Utils.DbToAmplitude(apuSettings.VolumeDb + globalVolumeDb));

            if (expansions != APU_EXPANSION_MASK_NONE)
            {
                for (int expansion = APU_EXPANSION_FIRST; expansion <= APU_EXPANSION_LAST; expansion++)
                {
                    if ((expansions & ExpansionToMask(expansion)) != 0)
                    {
                        var expSettings = expMixerSettings[expansion];
                        TrebleEq(apuIdx, expansion, expSettings.TrebleDb, expSettings.TrebleRolloffHz, sampleRate);
                        SetExpansionVolume(apuIdx, expansion, Utils.DbToAmplitude(expSettings.VolumeDb + globalVolumeDb));
                    }
                }

                if ((expansions & ExpansionToMask(APU_EXPANSION_NAMCO)) != 0)
                {
                    SetN163Mix(apuIdx, namcoMix ? 1 : 0);
                }
            }

            WriteRegister(apuIdx, APU_SND_CHN,    0x0f); // enable channels, stop DMC
            WriteRegister(apuIdx, APU_TRI_LINEAR, 0x80); // disable triangle length counter
            WriteRegister(apuIdx, APU_NOISE_HI,   0x00); // load noise length
            WriteRegister(apuIdx, APU_PL1_VOL,    0x30); // volumes to 0
            WriteRegister(apuIdx, APU_PL2_VOL,    0x30);
            WriteRegister(apuIdx, APU_NOISE_VOL,  0x30);
            WriteRegister(apuIdx, APU_PL1_SWEEP,  0x08); // no sweep
            WriteRegister(apuIdx, APU_PL2_SWEEP,  0x08);

            if (expansions != APU_EXPANSION_MASK_NONE)
            {
                for (int expansion = APU_EXPANSION_FIRST; expansion <= APU_EXPANSION_LAST; expansion++)
                {
                    if ((expansions & ExpansionToMask(expansion)) != 0)
                    {
                        switch (expansion)
                        {
                            case APU_EXPANSION_VRC6:
                                WriteRegister(apuIdx, VRC6_CTRL, 0x00);  // No halt, no octave change
                                break;
                            case APU_EXPANSION_FDS:
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
                                WriteRegister(apuIdx, N163_DATA, (numNamcoChannels - 1) << 4);
                                break;
                            case APU_EXPANSION_SUNSOFT:
                                WriteRegister(apuIdx, S5B_ADDR, S5B_REG_MIXER_SETTING);
                                WriteRegister(apuIdx, S5B_DATA, 0x38); // No noise, just 3 tones for now.
                                if ((expansions & APU_EXPANSION_MASK_NAMCO) != 0) // See comment in "ChannelStateS5B.cs".
                                    WriteRegister(apuIdx, S5B_ADDR, S5B_REG_IO_A);
                                break;
                            case APU_EXPANSION_EPSM:
                                WriteRegister(apuIdx, EPSM_ADDR0, EPSM_REG_MIXER_SETTING); SkipCycles(apuIdx, EpsmCycleAddrSkip);
                                WriteRegister(apuIdx, EPSM_DATA0, 0x38); SkipCycles(apuIdx, EpsmCycleDataSkip); // No noise, just 3 tones for now.
                                WriteRegister(apuIdx, EPSM_ADDR0, 0x29); SkipCycles(apuIdx, EpsmCycleAddrSkip);
                                WriteRegister(apuIdx, EPSM_DATA0, 0x80); SkipCycles(apuIdx, EpsmCycleDataSkip);
                                WriteRegister(apuIdx, EPSM_ADDR0, 0x27); SkipCycles(apuIdx, EpsmCycleAddrSkip);
                                WriteRegister(apuIdx, EPSM_DATA0, 0x00); SkipCycles(apuIdx, EpsmCycleDataSkip);
                                WriteRegister(apuIdx, EPSM_ADDR0, 0x11); SkipCycles(apuIdx, EpsmCycleAddrSkip);
                                WriteRegister(apuIdx, EPSM_DATA0, 0x37); SkipCycles(apuIdx, EpsmCycleDataSkip);
                                break;
                        }
                    }
                }
            }
        }
    }
}
