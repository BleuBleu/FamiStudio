using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class RegisterViewer
    {
        protected LocalizedString HzLabel;
        protected LocalizedString PitchLabel;
        protected LocalizedString VolumeLabel;
        protected LocalizedString DutyLabel;

        // These are really UI specific things... Should be somewhere else.
        // MATTT Change this for strings with name of icon directly.
        protected int IconSquare = 0;
        protected int IconTriangle = 1;
        protected int IconNoise = 2;
        protected int IconDPCM = 3;
        protected int IconSaw = 4;
        protected int IconFM = 5;
        protected int IconWaveTable = 6;
        protected int IconRhythm = 7;

        protected int expansion;

        public static readonly string[] Icons =
        {
            "ChannelSquare",
            "ChannelTriangle",
            "ChannelNoise",
            "ChannelDPCM",
            "ChannelSaw",
            "ChannelFM",
            "ChannelWaveTable",
            "ChannelRythm"
        };

        public string[] InterpreterLabels { get; internal set; }
        public int[] InterpreterIcons { get; internal set; }
        public RegisterViewerRow[] RegisterRows { get; internal set; }
        public RegisterViewerRow[][] InterpeterRows { get; internal set; }
        public int Expansion => expansion;
        public virtual int GetNumInterpreterRows(Project p) => InterpeterRows.Length;

        public delegate object GetRegisterValueDelegate();
        public delegate void DrawRegisterDelegate(CommandList c, Fonts res, Rectangle rect, bool video);

        protected static readonly string[] NoteNamesPadded =
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

        public RegisterViewer(int exp)
        {
            expansion = exp;
        }

        protected double NoteFromFreq(double f)
        {
            return 12.0 * Math.Log(f / NesApu.FreqC0, 2.0);
        }

        protected string GetNoteString(int value)
        {
            int octave = value / 12;
            int note = value % 12;

            return $"{NoteNamesPadded[note]}{octave}";
        }

        protected string GetPitchString(int period, double frequency)
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

        public static RegisterViewer CreateForExpansion(int exp, NesApu.NesRegisterValues r)
        {
            switch (exp)
            {
                case ExpansionType.None: return new ApuRegisterViewer(r);
                case ExpansionType.Vrc6: return new Vrc6RegisterViewer(r);
                case ExpansionType.Vrc7: return new Vrc7RegisterViewer(r);
                case ExpansionType.Fds:  return new FdsRegisterViewer(r);
                case ExpansionType.Mmc5: return new Mmc5RegisterViewer(r);
                case ExpansionType.N163: return new N163RegisterViewer(r);
                case ExpansionType.S5B:  return new S5BRegisterViewer(r);
                case ExpansionType.EPSM: return new EpsmRegisterViewer(r);
            }

            Debug.Assert(false);
            return null;
        }
    }

    public class ApuRegisterViewer : RegisterViewer
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

        public ApuRegisterViewer(NesApu.NesRegisterValues r) : base(ExpansionType.None)
        {
            InterpreterLabels = new string[5];
            for (int j = 0; j < 5; j++){
                InterpreterLabels[j] = ChannelType.LocalizedNames[ChannelType.Square1+j];
            };
            InterpreterIcons = new int[]{ IconSquare, IconSquare, IconTriangle, IconNoise, IconDPCM };
            Localization.Localize(this);
            i = new ApuRegisterInterpreter(r);
            RegisterRows = new[]
            {
                new RegisterViewerRow("$4000", 0x4000, 0x4003),
                new RegisterViewerRow("$4004", 0x4004, 0x4007),
                new RegisterViewerRow("$4008", 0x4008, 0x400b),
                new RegisterViewerRow("$400C", 0x400c, 0x400f),
                new RegisterViewerRow("$4010", 0x4010, 0x4013)
            };
            InterpeterRows = new RegisterViewerRow[5][];
            InterpeterRows[0] = new[]
            {
                new RegisterViewerRow(PitchLabel,     () => GetPitchString(i.GetSquarePeriod(0), i.GetSquareFrequency(0)), true),
                new RegisterViewerRow(VolumeLabel,    () => i.GetSquareVolume(0).ToString("00"), true),
                new RegisterViewerRow(DutyLabel,      () => i.GetSquareDuty(0), true)
            };                                        
            InterpeterRows[1] = new[]                    
            {                                         
                new RegisterViewerRow(PitchLabel,     () => GetPitchString(i.GetSquarePeriod(1), i.GetSquareFrequency(1)), true),
                new RegisterViewerRow(VolumeLabel,    () => i.GetSquareVolume(1).ToString("00"), true),
                new RegisterViewerRow(DutyLabel,      () => i.GetSquareDuty(1), true)
            };                                        
            InterpeterRows[2] = new[]                    
            {                                         
                new RegisterViewerRow(PitchLabel,     () => GetPitchString(i.TrianglePeriod, i.TriangleFrequency), true),
            };                                        
            InterpeterRows[3] = new[]                    
            {                                         
                new RegisterViewerRow(PitchLabel,     () => i.NoisePeriod.ToString("X"), true),
                new RegisterViewerRow(VolumeLabel,    () => i.NoiseVolume.ToString("00"), true),
                new RegisterViewerRow(ModeLabel,      () => i.NoiseMode, true)
            };
            InterpeterRows[4] = new[]
            {
                new RegisterViewerRow(FrequencyLabel, () => DPCMSampleRate.GetString(false, r.Pal, true, true, i.DpcmFrequency), true),
                new RegisterViewerRow(LoopLabel,      () => i.DpcmLoop ? LoopOption : OnceOption, false),
                new RegisterViewerRow(SizeLabel,      () => i.DpcmSize, true),
                new RegisterViewerRow(BytesLeftLabel, () => i.DpcmBytesLeft, true),
                new RegisterViewerRow(DACLabel,       () => i.DpcmDac, true)
            };
        }
    }

    public class Vrc6RegisterViewer : RegisterViewer
    {
        Vrc6RegisterInterpreter i;

        public Vrc6RegisterViewer(NesApu.NesRegisterValues r) : base(ExpansionType.Vrc6)
        {
            InterpreterLabels = new string[3];
            for (int j = 0; j < 3; j++){
                InterpreterLabels[j] = ChannelType.LocalizedNames[ChannelType.Vrc6Square1+j];
            };
            InterpreterIcons = new int[]{ IconSquare, IconSquare, IconSaw };
            Localization.Localize(this);
            i = new Vrc6RegisterInterpreter(r);
            RegisterRows = new[]
            {
                new RegisterViewerRow("$9000", 0x9000, 0x9002),
                new RegisterViewerRow("$A000", 0xA000, 0xA002),
                new RegisterViewerRow("$B000", 0xB000, 0xB002)
            };
            InterpeterRows = new RegisterViewerRow[3][];
            InterpeterRows[0] = new[]
            {
                new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetSquarePeriod(0), i.GetSquareFrequency(0)), true),
                new RegisterViewerRow(VolumeLabel, () => i.GetSquareVolume(0).ToString("00"), true),
                new RegisterViewerRow(DutyLabel,   () => i.GetSquareDuty(0), true)
            };
            InterpeterRows[1] = new[]
            {
                new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetSquarePeriod(1), i.GetSquareFrequency(1)), true),
                new RegisterViewerRow(VolumeLabel, () => i.GetSquareVolume(1).ToString("00"), true),
                new RegisterViewerRow(DutyLabel,   () => i.GetSquareDuty(1), true)
            };
            InterpeterRows[2] = new[]
            {
                new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.SawPeriod, i.SawFrequency), true),
                new RegisterViewerRow(VolumeLabel, () => i.SawVolume.ToString("00"), true),
            };
        }
    }

    public class Vrc7RegisterViewer : RegisterViewer
    {
        LocalizedString PatchLabel;
        Vrc7RegisterIntepreter i;

        public Vrc7RegisterViewer(NesApu.NesRegisterValues r) : base(ExpansionType.Vrc7)
        {
            InterpreterLabels = new string[6];
            InterpreterIcons = new int[6];
            Localization.Localize(this);
            i = new Vrc7RegisterIntepreter(r);
            RegisterRows = new[]
            {
                new RegisterViewerRow("$10", 0x9030, 0x10, 0x16),
                new RegisterViewerRow("$20", 0x9030, 0x20, 0x26),
                new RegisterViewerRow("$30", 0x9030, 0x30, 0x36)
            };
            InterpeterRows = new RegisterViewerRow[6][];
            for (int j = 0; j < 6; j++)
            {
                InterpreterLabels[j] = ChannelType.LocalizedNames[ChannelType.Vrc7Fm1+j];
                InterpreterIcons[j] = IconFM;
                var c = j; // Important, need to make a copy for the lambda.
                InterpeterRows[c] = new[]
                {
                    new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetPeriod(c), i.GetFrequency(c)), true),
                    new RegisterViewerRow(VolumeLabel, () => i.GetVolume(c).ToString("00"), true),
                    new RegisterViewerRow(PatchLabel,  () => i.GetPatch(c), true),
                };
            }
        }
    }

    public class FdsRegisterViewer : RegisterViewer
    {
        LocalizedString ModSpeedLabel;
        LocalizedString ModDepthLabel;
        LocalizedString WaveLabel;
        LocalizedString ModLabel;
        FdsRegisterIntepreter i;

        public FdsRegisterViewer(NesApu.NesRegisterValues r) : base(ExpansionType.Fds)
        {
            InterpreterLabels = new string[] { ChannelType.LocalizedNames[ChannelType.FdsWave] };
            InterpreterIcons = new int[] { IconWaveTable };
            Localization.Localize(this);
            i = new FdsRegisterIntepreter(r);
            RegisterRows = new[]
            {
                new RegisterViewerRow("$4080", 0x4080, 0x4083),
                new RegisterViewerRow("$4084", 0x4084, 0x4087),
                new RegisterViewerRow("$4088", 0x4088, 0x408b),
                new RegisterViewerRow(WaveLabel, DrawWaveTable, 32),
                new RegisterViewerRow(ModLabel,  DrawModTable, 32)
            };
            InterpeterRows = new RegisterViewerRow[1][];
            InterpeterRows[0] = new[]
            {
                new RegisterViewerRow(PitchLabel,    () => GetPitchString(i.WavePeriod, i.WaveFrequency), true), 
                new RegisterViewerRow(VolumeLabel,   () => i.Volume.ToString("00"), true),
                new RegisterViewerRow(ModSpeedLabel, () => $"{i.ModSpeed,-4} ({i.ModFrequency,7:0.00}{HzLabel.ToString()}, {GetPitchString(i.ModSpeed, i.ModFrequency)[..6]})", true),
                new RegisterViewerRow(ModDepthLabel, () => i.ModDepth.ToString("00"), true)
            };
        }

        void DrawInternal(CommandList c, Fonts res, Rectangle rect, byte[] vals, int maxVal, bool signed, bool video)
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

            if (!video)
            {
                c.FillRectangle(64 * sx, 0, 64 * sx, rect.Height, Theme.DarkGreyColor3); 
                c.DrawLine(64 * sx, 0, 64 * sx, rect.Height, Theme.BlackColor);
            }
        }

        void DrawWaveTable(CommandList c, Fonts res, Rectangle rect, bool video)
        {
            DrawInternal(c, res, rect, i.GetWaveTable(), 63, false, video);
        }

        void DrawModTable(CommandList c, Fonts res, Rectangle rect, bool video)
        {
            DrawInternal(c, res, rect, i.GetModTable(), 7, false, video);
        }
    }

    public class Mmc5RegisterViewer : RegisterViewer
    {
        Mmc5RegisterIntepreter i;

        public Mmc5RegisterViewer(NesApu.NesRegisterValues r) : base(ExpansionType.Mmc5)
        {
            InterpreterLabels = new string[] 
            { 
                ChannelType.LocalizedNames[ChannelType.Mmc5Square1],
                ChannelType.LocalizedNames[ChannelType.Mmc5Square2]
            };
            InterpreterIcons = new int[] { IconSquare, IconSquare };
            Localization.Localize(this);
            i = new Mmc5RegisterIntepreter(r);
            RegisterRows = new[]
            {
                new RegisterViewerRow("$5000", 0x5000, 0x5003),
                new RegisterViewerRow("$5004", 0x5004, 0x5007),
            };
            InterpeterRows = new RegisterViewerRow[2][];
            InterpeterRows[0] = new[]
            {
                new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetSquarePeriod(0), i.GetSquareFrequency(0)), true),
                new RegisterViewerRow(VolumeLabel, () => i.GetSquareVolume(0).ToString("00"), true),
                new RegisterViewerRow(DutyLabel,   () => i.GetSquareDuty(0), true)
            };
            InterpeterRows[1] = new[]
            {
                new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetSquarePeriod(1), i.GetSquareFrequency(1)), true),
                new RegisterViewerRow(VolumeLabel, () => i.GetSquareVolume(1).ToString("00"), true),
                new RegisterViewerRow(DutyLabel,   () => i.GetSquareDuty(1), true)
            };

        }
    }

    public class N163RegisterViewer : RegisterViewer
    {
        LocalizedString WavePosLabel;
        LocalizedString WaveSizeLabel;
        N163RegisterIntepreter i;

        public override int GetNumInterpreterRows(Project p) => p.ExpansionNumN163Channels;

        public N163RegisterViewer(NesApu.NesRegisterValues r) : base(ExpansionType.N163)
        {
            InterpreterLabels = new string[8];
            InterpreterIcons = new int[8];
            Localization.Localize(this);
            i = new N163RegisterIntepreter(r);
            RegisterRows = new[]
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
            InterpeterRows = new RegisterViewerRow[8][];
            for (int j = 0; j < 8; j++)
            {
                var c = j; // Important, need to make a copy for the lambda.
                InterpreterLabels[j] = ChannelType.LocalizedNames[ChannelType.N163Wave1+j];
                InterpreterIcons[j] = IconWaveTable;
                InterpeterRows[c] = new[]
                {
                    new RegisterViewerRow(PitchLabel,    () => GetPitchString(i.GetPeriod(c), i.GetFrequency(c)), true),
                    new RegisterViewerRow(VolumeLabel,   () => i.GetVolume(c).ToString("00"), true),
                    new RegisterViewerRow(WavePosLabel,  () => i.GetWavePos(c).ToString()),
                    new RegisterViewerRow(WaveSizeLabel, () => i.GetWaveSize(c).ToString())
                };
            }
        }

        unsafe void DrawRamMap(CommandList c, Fonts res, Rectangle rect, bool video)
        {
            var ramSize   = 128 - i.NumActiveChannels * 8;
            var numValues = ramSize * 2;

            var sx = Math.Max(1, rect.Width  / numValues);
            var sy = rect.Height / 15.0f;
            var h  = rect.Height;
            var instColors = stackalloc Color[8];
            var instIds = stackalloc int[8];

            for (var x = 0; x < ramSize; x++)
            {
                var val = i.Registers.GetRegisterValue(ExpansionType.N163, NesApu.N163_DATA, x);
                var lo = ((val >> 0) & 0xf) * sy;
                var hi = ((val >> 4) & 0xf) * sy;

                // Look at the list of ranges and use the color of the channel that last wrote to that location.
                var mostRecentUpdate = -1;
                var mostRecentInstrumentId = 0;
                var wrongInstrument = false;

                var instCount = 0;
                instColors[0] = Theme.LightGreyColor2;

                // Find the instrument that last wrote to that RAM location.
                for (var j = 0; j < i.NumActiveChannels; j++)
                {
                    var range = i.Registers.N163InstrumentRanges[j];

                    if (x >= range.Pos && x < range.Pos + range.Size)
                    {
                        if (range.LastUpdate > mostRecentUpdate)
                        {
                            mostRecentInstrumentId = range.InstrumentId;
                            mostRecentUpdate = range.LastUpdate;
                        }
                    }
                }

                for (var j = 0; j < i.NumActiveChannels; j++)
                {
                    var range = i.Registers.N163InstrumentRanges[j];
                    var instrumentId = i.Registers.N163InstrumentRanges[j].InstrumentId;

                    if (range.AnyNotePlaying && x >= range.Pos && x < range.Pos + range.Size && instrumentId != 0)
                    {
                        // Dont show conflicts in video export.
                        if (!video)
                        {
                            var alreadySeenInstrument = false;
                            for (var k = 0; k < instCount; k++)
                            {
                                if (instIds[k] == instrumentId)
                                {
                                    alreadySeenInstrument = true;
                                    break;
                                }
                            }

                            if (!alreadySeenInstrument)
                            {
                                instIds[instCount] = instrumentId;
                                instColors[instCount] = i.Registers.InstrumentColors[ChannelType.N163Wave1 + j];
                                instCount++;

                                // Check if another instrument corrupted our RAM earlier in the song.
                                if (instrumentId != mostRecentInstrumentId)
                                    wrongInstrument = true;
                            }
                        }

                        if (range.AnyNotePlaying && range.LastUpdate == mostRecentUpdate && video)
                        {
                            instColors[0] = i.Registers.InstrumentColors[ChannelType.N163Wave1 + j];
                        }
                    }
                }

                // Blink all conflicting colors if there is a conflict.
                var colorIndex = video || instCount == 0 ? 0 : (int)(Platform.TimeSeconds() * 15) % instCount;
                var color = instColors[colorIndex];
                
                // MATTT : Localize these.
                if (instCount > 1)
                {
                    c.DrawText("Wave overlap detected!", res.FontSmall, 0, 0, Theme.LightRedColor, TextFlags.TopLeft | TextFlags.DropShadow);
                }
                else if (wrongInstrument)
                {
                    c.DrawText("Potentially wrong wave playing!", res.FontSmall, 0, 0, Theme.LightRedColor, TextFlags.TopLeft | TextFlags.DropShadow);
                }

                c.FillRectangle((x * 2 + 0) * sx, h - lo, (x * 2 + 1) * sx, h, color);
                c.FillRectangle((x * 2 + 1) * sx, h - hi, (x * 2 + 2) * sx, h, color);
            }

            if (!video)
            {
                c.FillRectangle(numValues * sx, 0, 256 * sx, rect.Height, Theme.DarkGreyColor3); 
                c.DrawLine(256 * sx, 0, 256 * sx, rect.Height, Theme.BlackColor);
            }
        }
    }

    public class YMRegisterViewer : RegisterViewer
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
        protected LocalizedString EnvelopeShapeLabel;

        public YMRegisterViewer(int exp) : base (exp) 
        { 
        }

        
        protected void DrawEnvelopeInternal(CommandList c, Fonts res, Rectangle rect, int shape, bool video)
        {
            if ((shape & 0b1100) == 0b0000) shape = 0b1001;         // Shape: \___
            else if ((shape & 0b1100) == 0b0100) shape = 0b1111;    // Shape: /___

            var bmp = c.Graphics.GetTextureAtlasRef($"S5BEnvelope{shape:X1}");
            c.DrawTextureAtlas(bmp, 0, 0, 1, Theme.LightGreyColor1);

            if (!video)
            {
                c.FillRectangle(rect.Width, 0, rect.Width, rect.Height, Theme.DarkGreyColor3); 
                c.DrawLine(rect.Width, 0, rect.Width, rect.Height, Theme.BlackColor);
            }
        }
    }

    public class S5BRegisterViewer : YMRegisterViewer
    {
        S5BRegisterIntepreter i;

        public S5BRegisterViewer(NesApu.NesRegisterValues r) : base(ExpansionType.S5B)
        {
            InterpreterLabels = new string[]
            {
                ChannelType.LocalizedNames[ChannelType.S5BSquare1],
                ChannelType.LocalizedNames[ChannelType.S5BSquare2],
                ChannelType.LocalizedNames[ChannelType.S5BSquare3],
                null, null
            };
            InterpreterIcons = new int[]{ IconSquare, IconSquare, IconSquare, IconNoise, IconSaw }; //TODO: ACTUAL ENVELOPE ICON
            Localization.Localize(this); 
            InterpreterLabels[3] = NoiseLabel.ToString();
            InterpreterLabels[4] = EnvelopeLabel.ToString();
            i = new S5BRegisterIntepreter(r);
            RegisterRows = new[]
            {
                new RegisterViewerRow("$00", 0xE000, 0x00, 0x01),
                new RegisterViewerRow("$02", 0xE000, 0x02, 0x03),
                new RegisterViewerRow("$04", 0xE000, 0x04, 0x05),
                new RegisterViewerRow("$06", 0xE000, 0x06, 0x07),
                new RegisterViewerRow("$08", 0xE000, 0x08, 0x0a),
                new RegisterViewerRow("$0B", 0xE000, 0x0B, 0x0D),
            };
            InterpeterRows = new RegisterViewerRow[5][];
            for (int j = 0; j < 3; j++)
            {
                var c = j; // Important, need to make a copy for the lambda.
                InterpeterRows[c] = new[]
                {
                    new RegisterViewerRow(ToneLabel, () => i.GetMixerSetting(c) ? ToneEnabledLabel : ToneDisabledLabel, true),
                    new RegisterViewerRow(NoiseLabel, () => i.GetMixerSetting(c+3) ? NoiseEnabledLabel : NoiseDisabledLabel, true),
                    new RegisterViewerRow(EnvelopeLabel, () => i.GetEnvelopeEnabled(c) ? EnvelopeEnabledLabel : EnvelopeDisabledLabel, true),
                    new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetPeriod(c), i.GetFrequency(c)), true),
                    new RegisterViewerRow(VolumeLabel, () => i.GetVolume(c).ToString("00"), true),
                };
            }
            InterpeterRows[3] = new[]
            {
                new RegisterViewerRow(PitchLabel, () => i.GetNoiseFrequency().ToString("00"), true)
            };
            InterpeterRows[4] = new[]
            {
                new RegisterViewerRow(PitchLabel, () => $"{i.GetEnvelopePeriod(),-5} ({i.GetEnvelopeFrequency(),7:0.00}{HzLabel}, {GetPitchString(i.GetEnvelopePeriod(), i.GetEnvelopeFrequency())[..6]})", true),
                new RegisterViewerRow(EnvelopeShapeLabel, DrawEnvelope, 16),
            };
        }

        void DrawEnvelope (CommandList c, Fonts res, Rectangle rect, bool video){
            DrawEnvelopeInternal(c, res, rect, i.GetEnvelopeShape(), video);
        }
    }

    public class EpsmRegisterViewer : YMRegisterViewer
    {
        LocalizedString StereoLabel;
        LocalizedString VolOP1Label;
        LocalizedString VolOP2Label;
        LocalizedString VolOP3Label;
        LocalizedString VolOP4Label;
        EpsmRegisterIntepreter i;

        public EpsmRegisterViewer(NesApu.NesRegisterValues r) : base(ExpansionType.EPSM)
        {
            InterpreterLabels = new string[17];
            InterpreterIcons = new int[17];
            Localization.Localize(this);
            i = new EpsmRegisterIntepreter(r);
            RegisterRows = new[]
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
            InterpeterRows = new RegisterViewerRow[17][];
            for (int j = 0; j < 17; j++)
            {
                if (j < 3)
                {
                    var c = j; // Important, need to make a copy for the lambda.

                    InterpreterLabels[j] = ChannelType.LocalizedNames[ChannelType.EPSMSquare1+j];
                    InterpreterIcons[j] = IconSquare;
                    InterpeterRows[c] = new[]
                    {
                        new RegisterViewerRow(ToneLabel, () => i.GetMixerSetting(c) ? ToneEnabledLabel : ToneDisabledLabel, true),
                        new RegisterViewerRow(NoiseLabel, () => i.GetMixerSetting(c+3) ? NoiseEnabledLabel : NoiseDisabledLabel, true),
                        new RegisterViewerRow(EnvelopeLabel, () => i.GetEnvelopeEnabled(c) ? EnvelopeEnabledLabel : EnvelopeDisabledLabel, true),
                        new RegisterViewerRow(PitchLabel, () => GetPitchString(i.GetPeriod(c), i.GetFrequency(c)), true),
                        new RegisterViewerRow(VolumeLabel, () => i.GetVolume(c).ToString("00"), true),
                    };
                }
                if (j >= 5 && j < 11)
                {
                    var c = j-2; // Important, need to make a copy for the lambda.

                    InterpreterLabels[j] = ChannelType.LocalizedNames[ChannelType.EPSMSquare1+c];
                    InterpreterIcons[j] = IconFM;
                    InterpeterRows[j] = new[]
                    {
                        new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetPeriod(c), i.GetFrequency(c)), true),
                        new RegisterViewerRow(StereoLabel, () => i.GetStereo(c), true),
                        new RegisterViewerRow(VolOP1Label, () => i.GetVolume(c,0).ToString("00"), true),
                        new RegisterViewerRow(VolOP2Label, () => i.GetVolume(c,2).ToString("00"), true),
                        new RegisterViewerRow(VolOP3Label, () => i.GetVolume(c,1).ToString("00"), true),
                        new RegisterViewerRow(VolOP4Label, () => i.GetVolume(c,3).ToString("00"), true),
                    };
                }
                if (j >= 11)
                {
                    var c = j-2; // Important, need to make a copy for the lambda.

                    InterpreterLabels[j] = ChannelType.LocalizedNames[ChannelType.EPSMSquare1+c];
                    InterpreterIcons[j] = IconRhythm;
                    InterpeterRows[j] = new[]
                    {
                        new RegisterViewerRow(StereoLabel, () => i.GetStereo(c), true),
                        new RegisterViewerRow(VolumeLabel, () => i.GetVolume(c).ToString("00"), true),
                    };
                }
            }
            InterpreterLabels[3] = NoiseLabel.ToString();
            InterpreterIcons[3] = IconNoise;
            InterpeterRows[3] = new[]
            {
                new RegisterViewerRow(PitchLabel, () => i.GetNoiseFrequency().ToString("00"), true)
            };
            InterpreterLabels[4] = EnvelopeLabel.ToString();
            InterpreterIcons[4] = IconSaw;  //TODO: ACTUAL ENVELOPE ICON
            InterpeterRows[4] = new[]
            {
                new RegisterViewerRow(PitchLabel, () => $"{i.GetEnvelopePeriod(),-5} ({i.GetEnvelopeFrequency(),7:0.00}{HzLabel}, {GetPitchString(i.GetEnvelopePeriod(), i.GetEnvelopeFrequency())[..6]})", true),
                new RegisterViewerRow(EnvelopeShapeLabel, DrawEnvelope, 16),
            };
        }

        void DrawEnvelope (CommandList c, Fonts res, Rectangle rect, bool video){
            DrawEnvelopeInternal(c, res, rect, i.GetEnvelopeShape(), video);
        }
    }

    public class RegisterViewerRow
    {
        public string Label;
        public int CustomHeight;
        public int AddStart;
        public int AddEnd;
        public int SubStart;
        public int SubEnd;
        public bool Monospace;
        public RegisterViewer.GetRegisterValueDelegate GetValue;
        public RegisterViewer.DrawRegisterDelegate CustomDraw;

        // Address range.
        public RegisterViewerRow(string label, int addStart, int addEnd)
        {
            Label = label;
            AddStart = addStart;
            AddEnd = addEnd;
        }

        // Address range (for internal registers)
        public RegisterViewerRow(string label,int address, int subStart, int subEnd)
        {
            Label = label;
            AddStart = address;
            AddEnd = address;
            SubStart = subStart;
            SubEnd = subEnd;
        }

        // Text label.
        public RegisterViewerRow(string label, RegisterViewer.GetRegisterValueDelegate value, bool mono = false)
        {
            Label = label;
            GetValue = value;
            Monospace = mono;
        }

        // Custom draw.
        public RegisterViewerRow(string label, RegisterViewer.DrawRegisterDelegate draw, int height)
        {
            Label = label;
            CustomHeight = height;
            CustomDraw = draw;
        }
    };
}
