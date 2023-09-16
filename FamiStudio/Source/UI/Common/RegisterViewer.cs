using System;
using System.Diagnostics;

namespace FamiStudio
{
    // TODO : This currently embedded in the project explorer. Would be nice
    // to separate it, this way we could rendering registers in videos.
    public partial class ProjectExplorer : Container
    {
        delegate object GetRegisterValueDelegate();
        delegate void DrawRegisterDelegate(CommandList c, Fonts res, Rectangle rect);

        public static readonly string[] NoteNamesPadded =
        {
            "C-",
            "C#",
            "D-",
            "D#",
            "E-",
            "F-",
            "F#",
            "G-",
            "G#",
            "A-",
            "A#",
            "B-"
        };

        private static double NoteFromFreq(double f)
        {
            return 12.0 * Math.Log(f / NesApu.FreqC0, 2.0);
        }

        private static string GetNoteString(int value)
        {
            int octave = value / 12;
            int note = value % 12;

            return $"{NoteNamesPadded[note]}{octave}";
        }

        private static string GetPitchString(int period, double frequency)
        {
            if (period == 0 || frequency < NesApu.FreqRegMin)
            {
                return $"---+{Math.Abs(0):00} ({0,7:0.00}{HzLabel})";
            }
            else
            {
                var noteFloat = NoteFromFreq(frequency);
                Debug.Assert(noteFloat >= -0.5);

                var note = (int)Math.Round(noteFloat);
                var cents = (int)Math.Round((noteFloat - note) * 100.0);

                return $"{GetNoteString(note),-3}{(cents < 0 ? "-" : "+")}{Math.Abs(cents):00} ({frequency,7:0.00}{HzLabel.ToString()})";
            }
        }

        class RegisterViewerRow
        {
            public string Label;
            public int Height = DefaultRegisterSizeY;
            public int AddStart;
            public int AddEnd;
            public int SubStart;
            public int SubEnd;
            public bool Monospace;
            public GetRegisterValueDelegate GetValue;
            public DrawRegisterDelegate CustomDraw;

            // Address range.
            public RegisterViewerRow(string label, int addStart, int addEnd)
            {
                Label = label;
                AddStart = addStart;
                AddEnd = addEnd;
            }

            // Address range (for internal registers)
            public RegisterViewerRow(string label, int address, int subStart, int subEnd)
            {
                Label = label;
                AddStart = address;
                AddEnd = address;
                SubStart = subStart;
                SubEnd = subEnd;
            }

            // Text label.
            public RegisterViewerRow(string label, GetRegisterValueDelegate value, bool mono = false)
            {
                Label = label;
                GetValue = value;
                Monospace = mono;
            }

            // Custom draw.
            public RegisterViewerRow(string label, DrawRegisterDelegate draw, int height = DefaultRegisterSizeY)
            {
                Label = label;
                Height = height;
                CustomDraw = draw;
            }
        };

        class ExpansionRegisterViewer
        {
            // Shared across most chips
            protected LocalizedString PitchLabel;
            protected LocalizedString VolumeLabel;
            protected LocalizedString DutyLabel;

            protected int IconSquare = 0;
            protected int IconTriangle = 1;
            protected int IconNoise = 2;
            protected int IconDPCM = 3;
            protected int IconSaw = 4;
            protected int IconFM = 5;
            protected int IconWaveTable = 6;
            protected int IconRhythm = 7;

            public string[] Labels { get; internal set;}
            public int[] Icons { get; internal set;}
            public RegisterViewerRow[]   ExpansionRows { get; internal set; }
            public RegisterViewerRow[][] ChannelRows { get; internal set; }
        }

        class ApuRegisterViewer : ExpansionRegisterViewer
        {
            LocalizedString ModeLabel;
            LocalizedString FrequencyLabel;
            LocalizedString LoopLabel;
            LocalizedString LoopOption;
            LocalizedString OnceOption;
            LocalizedString SizeLabel;
            LocalizedString BytesLeftLabel;
            LocalizedString DACLabel;

            ApuRegisterInterpreter i;

            public ApuRegisterViewer(NesApu.NesRegisterValues r)
            {
                Labels = new string[5];
                for (int j = 0; j < 5; j++){
                    Labels[j] = ChannelType.LocalizedNames[ChannelType.Square1+j];
                };
                Icons = new int[]{ IconSquare, IconSquare, IconTriangle, IconNoise, IconDPCM };
                Localization.Localize(this);
                i = new ApuRegisterInterpreter(r);
                ExpansionRows = new[]
                {
                    new RegisterViewerRow("$4000", 0x4000, 0x4003),
                    new RegisterViewerRow("$4004", 0x4004, 0x4007),
                    new RegisterViewerRow("$4008", 0x4008, 0x400b),
                    new RegisterViewerRow("$400C", 0x400c, 0x400f),
                    new RegisterViewerRow("$4010", 0x4010, 0x4013)
                };
                ChannelRows = new RegisterViewerRow[5][];
                ChannelRows[0] = new[]
                {
                    new RegisterViewerRow(PitchLabel,     () => GetPitchString(i.GetSquarePeriod(0), i.GetSquareFrequency(0)), true),
                    new RegisterViewerRow(VolumeLabel,    () => i.GetSquareVolume(0).ToString("00"), true),
                    new RegisterViewerRow(DutyLabel,      () => i.GetSquareDuty(0), true)
                };                                        
                ChannelRows[1] = new[]                    
                {                                         
                    new RegisterViewerRow(PitchLabel,     () => GetPitchString(i.GetSquarePeriod(1), i.GetSquareFrequency(1)), true),
                    new RegisterViewerRow(VolumeLabel,    () => i.GetSquareVolume(1).ToString("00"), true),
                    new RegisterViewerRow(DutyLabel,      () => i.GetSquareDuty(1), true)
                };                                        
                ChannelRows[2] = new[]                    
                {                                         
                    new RegisterViewerRow(PitchLabel,     () => GetPitchString(i.TrianglePeriod, i.TriangleFrequency), true),
                };                                        
                ChannelRows[3] = new[]                    
                {                                         
                    new RegisterViewerRow(PitchLabel,     () => i.NoisePeriod.ToString("X"), true),
                    new RegisterViewerRow(VolumeLabel,    () => i.NoiseVolume.ToString("00"), true),
                    new RegisterViewerRow(ModeLabel,      () => i.NoiseMode, true)
                };
                ChannelRows[4] = new[]
                {
                    new RegisterViewerRow(FrequencyLabel, () => DPCMSampleRate.GetString(false, r.Pal, true, true, i.DpcmFrequency), true),
                    new RegisterViewerRow(LoopLabel,      () => i.DpcmLoop ? LoopOption : OnceOption, false),
                    new RegisterViewerRow(SizeLabel,      () => i.DpcmSize, true),
                    new RegisterViewerRow(BytesLeftLabel, () => i.DpcmBytesLeft, true),
                    new RegisterViewerRow(DACLabel,       () => i.DpcmDac, true)
                };
            }
        }

        class Vrc6RegisterViewer : ExpansionRegisterViewer
        {
            Vrc6RegisterInterpreter i;

            public Vrc6RegisterViewer(NesApu.NesRegisterValues r)
            {
                Labels = new string[3];
                for (int j = 0; j < 3; j++){
                    Labels[j] = ChannelType.LocalizedNames[ChannelType.Vrc6Square1+j];
                };
                Icons = new int[]{ IconSquare, IconSquare, IconSaw };
                Localization.Localize(this);
                i = new Vrc6RegisterInterpreter(r);
                ExpansionRows = new[]
                {
                    new RegisterViewerRow("$9000", 0x9000, 0x9002),
                    new RegisterViewerRow("$A000", 0xA000, 0xA002),
                    new RegisterViewerRow("$B000", 0xB000, 0xB002)
                };
                ChannelRows = new RegisterViewerRow[3][];
                ChannelRows[0] = new[]
                {
                    new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetSquarePeriod(0), i.GetSquareFrequency(0)), true),
                    new RegisterViewerRow(VolumeLabel, () => i.GetSquareVolume(0).ToString("00"), true),
                    new RegisterViewerRow(DutyLabel,   () => i.GetSquareDuty(0), true)
                };
                ChannelRows[1] = new[]
                {
                    new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetSquarePeriod(1), i.GetSquareFrequency(1)), true),
                    new RegisterViewerRow(VolumeLabel, () => i.GetSquareVolume(1).ToString("00"), true),
                    new RegisterViewerRow(DutyLabel,   () => i.GetSquareDuty(1), true)
                };
                ChannelRows[2] = new[]
                {
                    new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.SawPeriod, i.SawFrequency), true),
                    new RegisterViewerRow(VolumeLabel, () => i.SawVolume.ToString("00"), true),
                };
            }
        }

        class Vrc7RegisterViewer : ExpansionRegisterViewer
        {
            LocalizedString PatchLabel;
            Vrc7RegisterIntepreter i;

            public Vrc7RegisterViewer(NesApu.NesRegisterValues r)
            {
                Labels = new string[6];
                Icons = new int[6];
                Localization.Localize(this);
                i = new Vrc7RegisterIntepreter(r);
                ExpansionRows = new[]
                {
                    new RegisterViewerRow("$10", 0x9030, 0x10, 0x16),
                    new RegisterViewerRow("$20", 0x9030, 0x20, 0x26),
                    new RegisterViewerRow("$30", 0x9030, 0x30, 0x36)
                };
                ChannelRows = new RegisterViewerRow[6][];
                for (int j = 0; j < 6; j++)
                {
                    Labels[j] = ChannelType.LocalizedNames[ChannelType.Vrc7Fm1+j];
                    Icons[j] = IconFM;
                    var c = j; // Important, need to make a copy for the lambda.
                    ChannelRows[c] = new[]
                    {
                        new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetPeriod(c), i.GetFrequency(c)), true),
                        new RegisterViewerRow(VolumeLabel, () => i.GetVolume(c).ToString("00"), true),
                        new RegisterViewerRow(PatchLabel,  () => i.GetPatch(c), true),
                    };
                }
            }
        }

        class FdsRegisterViewer : ExpansionRegisterViewer
        {
            LocalizedString ModSpeedLabel;
            LocalizedString ModDepthLabel;
            LocalizedString WaveLabel;
            LocalizedString ModLabel;
            FdsRegisterIntepreter i;

            public FdsRegisterViewer(NesApu.NesRegisterValues r)
            {
                Labels = new string[] { ChannelType.LocalizedNames[ChannelType.FdsWave] };
                Icons = new int[] { IconWaveTable };
                Localization.Localize(this);
                i = new FdsRegisterIntepreter(r);
                ExpansionRows = new[]
                {
                    new RegisterViewerRow("$4080", 0x4080, 0x4083),
                    new RegisterViewerRow("$4084", 0x4084, 0x4087),
                    new RegisterViewerRow("$4088", 0x4088, 0x408b),
                };
                ChannelRows = new RegisterViewerRow[1][];
                ChannelRows[0] = new[]
                {
                    new RegisterViewerRow(PitchLabel,    () => GetPitchString(i.WavePeriod, i.WaveFrequency), true), 
                    new RegisterViewerRow(VolumeLabel,   () => i.Volume.ToString("00"), true),
                    new RegisterViewerRow(ModSpeedLabel, () => $"{i.ModSpeed,-4} ({i.ModFrequency,7:0.00}{HzLabel.ToString()}, {GetPitchString(i.ModSpeed, i.ModFrequency).Substring(0,6)})", true),
                    new RegisterViewerRow(ModDepthLabel, () => i.ModDepth.ToString("00"), true),
                    new RegisterViewerRow(WaveLabel, DrawWaveTable, 32),
                    new RegisterViewerRow(ModLabel,  DrawModTable, 32),
                };
            }

            void DrawInternal(CommandList c, Fonts res, Rectangle rect, byte[] vals, int maxVal, bool signed)
            {
                var sx = rect.Width  / 64;
                var sy = rect.Height / (float)maxVal;
                var h = rect.Height;

                for (int x = 0; x < 64; x++)
                {
                    var y = vals[x] * sy;
                    var color = i.Registers.InstrumentColors[ChannelType.FdsWave];

                    if (color.A == 0)
                        color = Theme.LightGreyColor2;

                    if (signed)
                        c.FillRectangle(x * sx, h - y, (x + 1) * sx, h / 2, color);
                    else
                        c.FillRectangle(x * sx, h - y, (x + 1) * sx, h, color);
                }

                c.FillRectangle(64 * sx, 0, 64 * sx, rect.Height, Theme.DarkGreyColor3);
                c.DrawLine(64 * sx, 0, 64 * sx, rect.Height, Theme.BlackColor);
            }

            void DrawWaveTable(CommandList c, Fonts res, Rectangle rect)
            {
                DrawInternal(c, res, rect, i.GetWaveTable(), 63, true);
            }

            void DrawModTable(CommandList c, Fonts res, Rectangle rect)
            {
                DrawInternal(c, res, rect, i.GetModTable(), 7, false);
            }
        }

        class Mmc5RegisterViewer : ExpansionRegisterViewer
        {
            Mmc5RegisterIntepreter i;

            public Mmc5RegisterViewer(NesApu.NesRegisterValues r)
            {
                Labels = new string[] 
                { 
                    ChannelType.LocalizedNames[ChannelType.Mmc5Square1],
                    ChannelType.LocalizedNames[ChannelType.Mmc5Square2]
                };
                Icons = new int[] { IconSquare, IconSquare };
                Localization.Localize(this);
                i = new Mmc5RegisterIntepreter(r);
                ExpansionRows = new[]
                {
                    new RegisterViewerRow("$5000", 0x5000, 0x5003),
                    new RegisterViewerRow("$5004", 0x5004, 0x5007),
                };
                ChannelRows = new RegisterViewerRow[2][];
                ChannelRows[0] = new[]
                {
                    new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetSquarePeriod(0), i.GetSquareFrequency(0)), true),
                    new RegisterViewerRow(VolumeLabel, () => i.GetSquareVolume(0).ToString("00"), true),
                    new RegisterViewerRow(DutyLabel,   () => i.GetSquareDuty(0), true)
                };
                ChannelRows[1] = new[]
                {
                    new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetSquarePeriod(1), i.GetSquareFrequency(1)), true),
                    new RegisterViewerRow(VolumeLabel, () => i.GetSquareVolume(1).ToString("00"), true),
                    new RegisterViewerRow(DutyLabel,   () => i.GetSquareDuty(1), true)
                };

            }
        }

        class N163RegisterViewer : ExpansionRegisterViewer
        {
            N163RegisterIntepreter i;

            public N163RegisterViewer(NesApu.NesRegisterValues r)
            {
                Labels = new string[8];
                Icons = new int[8];
                Localization.Localize(this);
                i = new N163RegisterIntepreter(r);
                ExpansionRows = new[]
                {
                    new RegisterViewerRow("$00", 0x4800, 0x00, 0x07 ),
                    new RegisterViewerRow("$08", 0x4800, 0x08, 0x0f ),
                    new RegisterViewerRow("$10", 0x4800, 0x10, 0x17 ),
                    new RegisterViewerRow("$18", 0x4800, 0x18, 0x1f ),
                    new RegisterViewerRow("$20", 0x4800, 0x20, 0x27 ),
                    new RegisterViewerRow("$28", 0x4800, 0x28, 0x2f ),
                    new RegisterViewerRow("$30", 0x4800, 0x30, 0x37 ),
                    new RegisterViewerRow("$38", 0x4800, 0x38, 0x3f ),
                    new RegisterViewerRow("$40", 0x4800, 0x40, 0x47 ),
                    new RegisterViewerRow("$48", 0x4800, 0x48, 0x4f ),
                    new RegisterViewerRow("$50", 0x4800, 0x50, 0x57 ),
                    new RegisterViewerRow("$58", 0x4800, 0x58, 0x5f ),
                    new RegisterViewerRow("$60", 0x4800, 0x60, 0x67 ),
                    new RegisterViewerRow("$68", 0x4800, 0x68, 0x6f ),
                    new RegisterViewerRow("$70", 0x4800, 0x70, 0x77 ),
                    new RegisterViewerRow("$78", 0x4800, 0x78, 0x7f ),
                    new RegisterViewerRow("RAM", DrawRamMap, 32),
                };
                ChannelRows = new RegisterViewerRow[8][];
                for (int j = 0; j < 8; j++)
                {
                    var c = j; // Important, need to make a copy for the lambda.
                    Labels[j] = ChannelType.LocalizedNames[ChannelType.N163Wave1+j];
                    Icons[j] = IconWaveTable;
                    ChannelRows[c] = new[]
                    {
                        new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetPeriod(c), i.GetFrequency(c)), true),
                        new RegisterViewerRow(VolumeLabel, () => i.GetVolume(c).ToString("00"), true),
                    };
                }
            }

            void DrawRamMap(CommandList c, Fonts res, Rectangle rect)
            {
                var ramSize   = 128 - i.NumActiveChannels * 8;
                var numValues = ramSize * 2;

                var sx = Math.Max(1, rect.Width  / numValues);
                var sy = rect.Height / 15.0f;
                var h  = rect.Height;

                for (int x = 0; x < ramSize; x++)
                {
                    var val = i.Registers.GetRegisterValue(ExpansionType.N163, NesApu.N163_DATA, x);
                    var lo = ((val >> 0) & 0xf) * sy;
                    var hi = ((val >> 4) & 0xf) * sy;
                    
                    // See if the RAM address matches any of the instrument.
                    // This isn't very accurate since we don't actually know
                    // which instrument last wrote to RAM at the moment, but
                    // it will work when there is no overlap.
                    var channelIndex = -1;
                    for (int j = 0; j < i.NumActiveChannels; j++)
                    {
                        if (x * 2 >= i.Registers.N163InstrumentRanges[j].Position &&
                            x * 2 <  i.Registers.N163InstrumentRanges[j].Position + i.Registers.N163InstrumentRanges[j].Size)
                        {
                            channelIndex = j;
                            break;
                        }
                    }

                    var color = channelIndex >= 0 ? i.Registers.InstrumentColors[ChannelType.N163Wave1 + channelIndex] : Theme.LightGreyColor2;

                    c.FillRectangle((x * 2 + 0) * sx, h - lo, (x * 2 + 1) * sx, h, color);
                    c.FillRectangle((x * 2 + 1) * sx, h - hi, (x * 2 + 2) * sx, h, color);
                }

                c.FillRectangle(numValues * sx, 0, 256 * sx, rect.Height, Theme.DarkGreyColor3);
                c.DrawLine(256 * sx, 0, 256 * sx, rect.Height, Theme.BlackColor);
            }
        }

        class YMRegisterViewer : ExpansionRegisterViewer
        {
            protected LocalizedString ToneLabel;
            protected LocalizedString ToneEnabledLabel;
            protected LocalizedString ToneDisabledLabel;
            protected LocalizedString NoiseLabel;
            protected LocalizedString NoiseEnabledLabel;
            protected LocalizedString NoiseDisabledLabel;
            protected LocalizedString EnvelopeLabel;
            protected LocalizedString EnvelopeEnabledLabel;
            protected LocalizedString EnvelopeDisabledLabel;
        }

        class S5BRegisterViewer : YMRegisterViewer
        {
            S5BRegisterIntepreter i;

            public S5BRegisterViewer(NesApu.NesRegisterValues r)
            {
                Labels = new string[]
                {
                    ChannelType.LocalizedNames[ChannelType.S5BSquare1],
                    ChannelType.LocalizedNames[ChannelType.S5BSquare2],
                    ChannelType.LocalizedNames[ChannelType.S5BSquare3],
                    ChannelType.LocalizedNames[ChannelType.Noise]
                };
                Icons = new int[]{ IconSquare, IconSquare, IconSquare, IconNoise };
                Localization.Localize(this);
                Labels[3] = NoiseLabel.ToString();
                i = new S5BRegisterIntepreter(r);
                ExpansionRows = new[]
                {
                    new RegisterViewerRow("$00", 0xE000, 0x00, 0x01),
                    new RegisterViewerRow("$02", 0xE000, 0x02, 0x03),
                    new RegisterViewerRow("$04", 0xE000, 0x04, 0x05),
                    new RegisterViewerRow("$06", 0xE000, 0x06, 0x07),
                    new RegisterViewerRow("$08", 0xE000, 0x08, 0x0a),
                    new RegisterViewerRow("$0B", 0xE000, 0x0B, 0x0D),
                };
                ChannelRows = new RegisterViewerRow[4][];
                for (int j = 0; j < 3; j++)
                {
                    var c = j; // Important, need to make a copy for the lambda.
                    ChannelRows[c] = new[]
                    {
                        new RegisterViewerRow(ToneLabel, () => i.GetMixerSetting(c) ? ToneEnabledLabel : ToneDisabledLabel, true),
                        new RegisterViewerRow(NoiseLabel, () => i.GetMixerSetting(c+3) ? NoiseEnabledLabel : NoiseDisabledLabel, true),
                        new RegisterViewerRow(EnvelopeLabel, () => i.GetEnvelopeEnabled(c) ? EnvelopeEnabledLabel : EnvelopeDisabledLabel, true),
                        new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetPeriod(c), i.GetToneFrequency(c)), true),
                        new RegisterViewerRow(VolumeLabel, () => i.GetVolume(c).ToString("00"), true),
                    };
                }
                ChannelRows[3] = new[]
                {
                    new RegisterViewerRow(PitchLabel, () => i.GetNoiseFrequency().ToString("00"), true)
                };
            }
        }

        class EpsmRegisterViewer : YMRegisterViewer
        {
            LocalizedString StereoLabel;
            LocalizedString VolOP1Label;
            LocalizedString VolOP2Label;
            LocalizedString VolOP3Label;
            LocalizedString VolOP4Label;
            EpsmRegisterIntepreter i;

            public EpsmRegisterViewer(NesApu.NesRegisterValues r)
            {
                Labels = new string[16];
                Icons = new int[16];
                Localization.Localize(this);
                i = new EpsmRegisterIntepreter(r);
                ExpansionRows = new[]
                {
                    new RegisterViewerRow("$00", 0x401d, 0x00, 0x01),
                    new RegisterViewerRow("$02", 0x401d, 0x02, 0x03),
                    new RegisterViewerRow("$04", 0x401d, 0x04, 0x05),
                    new RegisterViewerRow("$06", 0x401d, 0x06, 0x07),
                    new RegisterViewerRow("$08", 0x401d, 0x08, 0x0a),
                    new RegisterViewerRow("$0B", 0x401d, 0x0B, 0x0D),
                    new RegisterViewerRow("$10", 0x401d, 0x10, 0x12),
                    new RegisterViewerRow("$18", 0x401d, 0x18, 0x1d),
                    new RegisterViewerRow("$22", 0x401d, 0x22, 0x22),
                    new RegisterViewerRow("$28", 0x401d, 0x28, 0x28),
                    new RegisterViewerRow("$30 A0", 0x401d, 0x30, 0x37),
                    new RegisterViewerRow("$38 A0", 0x401d, 0x38, 0x3f),
                    new RegisterViewerRow("$40 A0", 0x401d, 0x40, 0x47),
                    new RegisterViewerRow("$48 A0", 0x401d, 0x48, 0x4f),
                    new RegisterViewerRow("$50 A0", 0x401d, 0x50, 0x57),
                    new RegisterViewerRow("$58 A0", 0x401d, 0x58, 0x5f),
                    new RegisterViewerRow("$60 A0", 0x401d, 0x60, 0x67),
                    new RegisterViewerRow("$68 A0", 0x401d, 0x68, 0x6f),
                    new RegisterViewerRow("$70 A0", 0x401d, 0x70, 0x77),
                    new RegisterViewerRow("$78 A0", 0x401d, 0x78, 0x7f),
                    new RegisterViewerRow("$80 A0", 0x401d, 0x80, 0x87),
                    new RegisterViewerRow("$88 A0", 0x401d, 0x88, 0x8f),
                    new RegisterViewerRow("$90 A0", 0x401d, 0x90, 0x97),
                    new RegisterViewerRow("$98 A0", 0x401d, 0x98, 0x9f),
                    new RegisterViewerRow("$A0 A0", 0x401d, 0xa0, 0xa7),
                    new RegisterViewerRow("$B0 A0", 0x401d, 0xb0, 0xb7),
                    new RegisterViewerRow("$30 A1", 0x401f, 0x30, 0x37),
                    new RegisterViewerRow("$38 A1", 0x401f, 0x38, 0x3f),
                    new RegisterViewerRow("$40 A1", 0x401f, 0x40, 0x47),
                    new RegisterViewerRow("$48 A1", 0x401f, 0x48, 0x4f),
                    new RegisterViewerRow("$50 A1", 0x401f, 0x50, 0x57),
                    new RegisterViewerRow("$58 A1", 0x401f, 0x58, 0x5f),
                    new RegisterViewerRow("$60 A1", 0x401f, 0x60, 0x67),
                    new RegisterViewerRow("$68 A1", 0x401f, 0x68, 0x6f),
                    new RegisterViewerRow("$70 A1", 0x401f, 0x70, 0x77),
                    new RegisterViewerRow("$78 A1", 0x401f, 0x78, 0x7f),
                    new RegisterViewerRow("$80 A1", 0x401f, 0x80, 0x87),
                    new RegisterViewerRow("$88 A1", 0x401f, 0x88, 0x8f),
                    new RegisterViewerRow("$90 A1", 0x401f, 0x90, 0x97),
                    new RegisterViewerRow("$98 A1", 0x401f, 0x98, 0x9f),
                    new RegisterViewerRow("$A0 A1", 0x401f, 0xa0, 0xa7),
                    new RegisterViewerRow("$B0 A1", 0x401f, 0xb0, 0xb7),
                };
                ChannelRows = new RegisterViewerRow[16][];
                for (int j = 0; j < 16; j++)
                {
                    var c = j; // Important, need to make a copy for the lambda.
                    if (j < 3)
                    {
                        Labels[j] = ChannelType.LocalizedNames[ChannelType.EPSMSquare1+j];
                        Icons[j] = IconSquare;
                        ChannelRows[c] = new[]
                        {
                            new RegisterViewerRow(ToneLabel, () => i.GetMixerSetting(c) ? ToneEnabledLabel : ToneDisabledLabel, true),
                            new RegisterViewerRow(NoiseLabel, () => i.GetMixerSetting(c+3) ? NoiseEnabledLabel : NoiseDisabledLabel, true),
                            new RegisterViewerRow(EnvelopeLabel, () => i.GetEnvelopeEnabled(c) ? EnvelopeEnabledLabel : EnvelopeDisabledLabel, true),
                            new RegisterViewerRow(PitchLabel, () => GetPitchString(i.GetPeriod(c), i.GetFrequency(c)), true),
                            new RegisterViewerRow(VolumeLabel, () => i.GetVolume(c).ToString("00"), true),
                        };
                    }
                    if (j >= 4 && j < 10)
                    {
                        Labels[j] = ChannelType.LocalizedNames[ChannelType.EPSMSquare1+j-1];
                        Icons[j] = IconFM;
                        ChannelRows[c] = new[]
                        {
                            new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetPeriod(c-1), i.GetFrequency(c-1)), true),
                            new RegisterViewerRow(StereoLabel, () => i.GetStereo(c-1), true),
                            new RegisterViewerRow(VolOP1Label, () => i.GetVolume(c-1,0).ToString("00"), true),
                            new RegisterViewerRow(VolOP2Label, () => i.GetVolume(c-1,2).ToString("00"), true),
                            new RegisterViewerRow(VolOP3Label, () => i.GetVolume(c-1,1).ToString("00"), true),
                            new RegisterViewerRow(VolOP4Label, () => i.GetVolume(c-1,3).ToString("00"), true),
                        };
                    }
                    if (j >= 10)
                    {
                        Labels[j] = ChannelType.LocalizedNames[ChannelType.EPSMSquare1+j-1];
                        Icons[j] = IconRhythm;
                        ChannelRows[c] = new[]
                        {
                            new RegisterViewerRow(StereoLabel, () => i.GetStereo(c-1), true),
                            new RegisterViewerRow(VolumeLabel, () => i.GetVolume(c-1).ToString("00"), true),
                        };
                    }
                }
                Labels[3] = NoiseLabel.ToString();
                Icons[3] = IconNoise;
                ChannelRows[3] = new[]
                {
                    new RegisterViewerRow(PitchLabel, () => i.GetNoiseFrequency().ToString("00"), true)
                };
            }
        }
    }
}
