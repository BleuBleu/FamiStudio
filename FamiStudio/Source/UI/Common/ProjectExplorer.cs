using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class ProjectExplorer : Container
    {
        const int DefaultExpandButtonSizeX    = 8;
        const int DefaultExpandButtonPosX     = 3;
        const int DefaultExpandButtonPosY     = 8;
        const int DefaultButtonIconPosX       = 3;
        const int DefaultButtonIconPosY       = 3;
        const int DefaultButtonTextPosX       = 21;
        const int DefaultButtonTextNoIconPosX = 4;
        const int DefaultSubButtonSizeX       = 16;
        const int DefaultSubButtonMarginX     = Platform.IsMobile ? 1 : 2;
        const int DefaultSubButtonSpacingX    = Platform.IsMobile ? 0 : 2;
        const int DefaultSubButtonPosY        = 3;
        const int DefaultScrollBarThickness1  = 10;
        const int DefaultScrollBarThickness2  = 16;
        const int DefaultButtonSizeY          = 21;
        const int DefaultRegisterSizeY        = 14;
        const int DefaultSliderPosX           = Platform.IsMobile ? 88 : 108;
        const int DefaultSliderPosY           = 3;
        const int DefaultSliderSizeX          = Platform.IsMobile ? 84 : 104;
        const int DefaultSliderSizeY          = 15;
        const int DefaultCheckBoxPosX         = 20;
        const int DefaultCheckBoxPosY         = 3;
        const int DefaultDraggedLineSizeY     = 5;
        const int DefaultParamRightPadX       = 4;
        const int DefaultRegisterLabelSizeX   = 58;
        const float ScrollSpeedFactor         = Platform.IsMobile ? 2.0f : 1.0f;

        int expandButtonSizeX;
        int buttonIconPosX;
        int buttonIconPosY;
        int buttonTextPosX;
        int buttonTextNoIconPosX;
        int expandButtonPosX;
        int expandButtonPosY;
        int subButtonSpacingX;
        int subButtonMarginX;
        int subButtonSizeX;
        int subButtonPosY;
        int buttonSizeY;
        int sliderPosX;
        int sliderPosY;
        int sliderSizeX;
        int sliderSizeY;
        int paramButtonSizeX;
        int checkBoxPosX;
        int checkBoxPosY;
        int paramRightPadX;
        int virtualSizeY;
        int scrollAreaSizeY;
        int scrollBarThickness;
        int draggedLineSizeY;
        int registerLabelSizeX;
        int contentSizeX;
        int topTabSizeY;
        bool needsScrollBar;

        enum TabType
        {
            Project,
            Registers,
            Count
        };

        #region Localization

        LocalizedString[] TabNames = new LocalizedString[(int)TabType.Count];

        // Buttons
        LocalizedString SongsHeaderLabel;
        LocalizedString InstrumentHeaderLabel;
        LocalizedString SamplesHeaderLabel;
        LocalizedString ArpeggiosHeaderLabel;
        LocalizedString ArpeggioNoneLabel;
        LocalizedString RegistersExpansionHeaderLabel;

        // Tooltips
        LocalizedString AddNewArpeggioTooltip;
        LocalizedString AddNewInstrumentTooltip;
        LocalizedString AddNewSongTooltip;
        LocalizedString AutoSortArpeggioActiveTooltip;
        LocalizedString AutoSortInstrumentActiveTooltip;
        LocalizedString AutoSortSampleActiveTooltip;
        LocalizedString AutoSortSongActiveTooltip;
        LocalizedString ChangeValueFineTooltip;
        LocalizedString ChangeValueTooltip;
        LocalizedString CopyEnvelopeTooltip;
        LocalizedString CopyReplaceInstrumentTooltip;
        LocalizedString EditEnvelopeTooltip;
        LocalizedString EditSamplesTooltip;
        LocalizedString EditWaveformTooltip;
        LocalizedString ImportInstrumentsTooltip;
        LocalizedString ImportSamplesTooltip;
        LocalizedString ImportSongsTooltip;
        LocalizedString MakeSongCurrentTooltip;
        LocalizedString MoreOptionsTooltip;
        LocalizedString PlaySourceSampleTooltip;
        LocalizedString PreviewProcessedSampleTooltip;
        LocalizedString PropertiesArpeggioTooltip;
        LocalizedString PropertiesInstrumentTooltip;
        LocalizedString PropertiesProjectTooltip;
        LocalizedString PropertiesSongTooltip;
        LocalizedString ReloadSourceDataTooltip;
        LocalizedString ReorderSongsTooltip;
        LocalizedString ReplaceArpeggioTooltip;
        LocalizedString SelectArpeggioTooltip;
        LocalizedString SelectInstrumentTooltip;
        LocalizedString SortArpeggiosTooltip;
        LocalizedString SortInstrumentsTooltip;
        LocalizedString SortSamplesTooltip;
        LocalizedString SortSongTooltip;
        LocalizedString ToggleValueTooltip;

        // Messages
        LocalizedString CopyArpeggioMessage;
        LocalizedString CopyArpeggioTitle;
        LocalizedString ErrorTitle;
        LocalizedString MaxWavFileWarning;
        LocalizedString MaxDmcSizeWarning;
        LocalizedString MaxWavN163Duration;
        LocalizedString AskDeleteSongMessage;
        LocalizedString AskDeleteSongTitle;
        LocalizedString AskDeleteInstrumentMessage;
        LocalizedString AskDeleteInstrumentTitle;
        LocalizedString AskDeleteArpeggioMessage;
        LocalizedString AskDeleteArpeggioTitle;
        LocalizedString AskDeleteSampleMessage;
        LocalizedString AskDeleteSampleTitle;
        LocalizedString AskReplaceInstrumentMessage;
        LocalizedString AskReplaceInstrumentTitle;
        LocalizedString AskReplaceArpeggioMessage;
        LocalizedString AskReplaceArpeggioTitle;
        LocalizedString ClipboardNoValidTextError;
        LocalizedString ClipboardInvalidNumberRegisters;
        LocalizedString CantFindSourceFileError;
        LocalizedString AddInstrumentTitle;
        LocalizedString SelectAudioExpansionLabel;

        // Import songs dialog
        LocalizedString ImportSongsTitle;
        LocalizedString ImportSongsLabel;
        LocalizedString SelectAllLabel;
        LocalizedString SelectNoneLabel ;

        // Import instruments dialog
        LocalizedString ImportInstrumentsTitle;
        LocalizedString ImportInstrumentsLabel ;

        // Import DPCM Samples dialog
        LocalizedString ImportSamplesTitle;
        LocalizedString ImportSamplesLabel;

        // Auto-assign banks dialog
        LocalizedString AutoAssignBanksTitle;
        LocalizedString TargetBankSizeLabel;

        // Project properties dialog
        LocalizedString ProjectPropertiesTitle;
        LocalizedString ProjectTitleLabel;
        LocalizedString ProjectAuthorLabel;
        LocalizedString ProjectCopyrightLabel;
        LocalizedString ProjectTempoModeLabel;
        LocalizedString ProjectMachineLabel;
        LocalizedString ProjectN163ChannelsLabel;
        LocalizedString ProjectExpansionLabel;
        LocalizedString ProjectChangeTempoModeTitle;
        LocalizedString ProjectConvertToFamiTrackerMessage;
        LocalizedString ProjectConvertToFamiStudioMessage;
        LocalizedString ProjectExpansionRemovedMessage;
        LocalizedString ProjectChangedN163ChannelMessage;
        LocalizedString ProjectMultipleExpansionsROMWarning;
        LocalizedString ProjectExpansionAudioTooltip;
        LocalizedString ProjectExpansionNumChannelsTooltip;
        LocalizedString ProjectTempoModeTooltip;
        LocalizedString ProjectAuthoringMachineTooltip ;

        // Song properties dialog
        LocalizedString SongPropertiesTitle;
        LocalizedString SongLengthLabel;
        LocalizedString SongLengthTooltip;
        LocalizedString RenameSongError ;

        // Instrument properties dialog
        LocalizedString InstrumentPropertiesTitle;
        LocalizedString RenameInstrumentError;

        // Arpeggio properties dialog
        LocalizedString ArpeggioPropertiesTitle;
        LocalizedString RenameArpeggioError;

        // DPCM sample properties dialog
        LocalizedString SamplePropertiesTitle;
        LocalizedString RenameSampleError;

        // Context menus
        LocalizedString AddExpInstrumentContext;
        LocalizedString AddRegularInstrumentContext;
        LocalizedString AutoAssignBanksContext;
        LocalizedString AutoSortArpeggiosContext;
        LocalizedString AutoSortInstrumentsContext;
        LocalizedString AutoSortSamplesContext;
        LocalizedString AutoSortSongsContext;
        LocalizedString ClearEnvelopeContext;
        LocalizedString CopyRegisterValueContext;
        LocalizedString DeleteArpeggioContext;
        LocalizedString DeleteInstrumentContext;
        LocalizedString DeleteSampleContext;
        LocalizedString DeleteSongContext;
        LocalizedString DiscardSourceWavDataContext;
        LocalizedString DiscardSourceWavDataTooltip;
        LocalizedString DiscardWavDataContext;
        LocalizedString DuplicateContext;
        LocalizedString DuplicateConvertContext;
        LocalizedString ExportProcessedDmcDataContext;
        LocalizedString ExportSourceDataContext;
        LocalizedString PasteRegisterValueContext;
        LocalizedString PropertiesArpeggioContext;
        LocalizedString PropertiesInstrumentContext;
        LocalizedString PropertiesProjectContext;
        LocalizedString PropertiesSamplesContext;
        LocalizedString PropertiesSongContext;
        LocalizedString ReplaceWithContext;
        LocalizedString ResampleWavContext;
        LocalizedString ResetDefaultValueContext;

        #endregion

        delegate object GetRegisterValueDelegate();
        delegate void   DrawRegisterDelegate(CommandList c, Fonts res, Rectangle rect);

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
                return $"---+{Math.Abs(0):00} ({0,7:0.00}Hz)";
            }
            else
            {
                var noteFloat = NoteFromFreq(frequency);
                Debug.Assert(noteFloat >= -0.5);

                var note = (int)Math.Round(noteFloat);
                var cents = (int)Math.Round((noteFloat - note) * 100.0);

                return $"{GetNoteString(note),-3}{(cents < 0 ? "-" : "+")}{Math.Abs(cents):00} ({frequency,7:0.00}Hz)";
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
                    new RegisterViewerRow(ModSpeedLabel, () => $"{i.ModSpeed,-4} ({i.ModFrequency,7:0.00}Hz, {GetPitchString(i.ModSpeed, i.ModFrequency).Substring(0,6)})", true),
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

        class S5BRegisterViewer : ExpansionRegisterViewer
        {
            S5BRegisterIntepreter i;

            public S5BRegisterViewer(NesApu.NesRegisterValues r)
            {
                Localization.Localize(this);
                i = new S5BRegisterIntepreter(r);
                ExpansionRows = new[]
                {
                    new RegisterViewerRow("$00", 0xE000, 0x00, 0x01),
                    new RegisterViewerRow("$02", 0xE000, 0x02, 0x03),
                    new RegisterViewerRow("$04", 0xE000, 0x04, 0x05),
                    new RegisterViewerRow("$08", 0xE000, 0x08, 0x0a),
                };
                ChannelRows = new RegisterViewerRow[3][];
                for (int j = 0; j < 3; j++)
                {
                    var c = j; // Important, need to make a copy for the lambda.
                    ChannelRows[c] = new[]
                    {
                        new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetPeriod(c), i.GetFrequency(c)), true),
                        new RegisterViewerRow(VolumeLabel, () => i.GetVolume(c).ToString("00"), true),
                    };
                }
            }
        }

        class EpsmRegisterViewer : ExpansionRegisterViewer
        {
            LocalizedString StereoLabel;
            LocalizedString VolOP1Label;
            LocalizedString VolOP2Label;
            LocalizedString VolOP3Label;
            LocalizedString VolOP4Label;
            EpsmRegisterIntepreter i;

            public EpsmRegisterViewer(NesApu.NesRegisterValues r)
            {
                Localization.Localize(this);
                i = new EpsmRegisterIntepreter(r);
                ExpansionRows = new[]
                {
                    new RegisterViewerRow("$00", 0x401d, 0x00, 0x01),
                    new RegisterViewerRow("$02", 0x401d, 0x02, 0x03),
                    new RegisterViewerRow("$04", 0x401d, 0x04, 0x05),
                    new RegisterViewerRow("$08", 0x401d, 0x08, 0x0a),
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
                ChannelRows = new RegisterViewerRow[15][];
                for (int j = 0; j < 15; j++)
                {
                    var c = j; // Important, need to make a copy for the lambda.
                    if (j < 3)
                        ChannelRows[c] = new[]
                        {
                            new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetPeriod(c), i.GetFrequency(c)), true),
                            new RegisterViewerRow(VolumeLabel, () => i.GetVolume(c).ToString("00"), true),
                        };
                    if (j >= 3 && j < 9)
                        ChannelRows[c] = new[]
                        {
                            new RegisterViewerRow(PitchLabel,  () => GetPitchString(i.GetPeriod(c), i.GetFrequency(c)), true),
                            new RegisterViewerRow(StereoLabel, () => i.GetStereo(c), true),
                            new RegisterViewerRow(VolOP1Label, () => i.GetVolume(c,0).ToString("00"), true),
                            new RegisterViewerRow(VolOP2Label, () => i.GetVolume(c,2).ToString("00"), true),
                            new RegisterViewerRow(VolOP3Label, () => i.GetVolume(c,1).ToString("00"), true),
                            new RegisterViewerRow(VolOP4Label, () => i.GetVolume(c,3).ToString("00"), true),
                        };
                    if (j >= 9 )
                        ChannelRows[c] = new[]
                        {
                            new RegisterViewerRow(StereoLabel, () => i.GetStereo(c), true),
                            new RegisterViewerRow(VolumeLabel, () => i.GetVolume(c).ToString("00"), true),
                        };
                }
            }
        }

        enum ButtonType
        {
            // Project explorer buttons.
            ProjectSettings,
            SongHeader,
            Song,
            InstrumentHeader,
            Instrument,
            DpcmHeader,
            Dpcm,
            ArpeggioHeader,
            Arpeggio,
            ParamTabs,
            ParamCheckbox,
            ParamSlider,
            ParamList,
            ParamCustomDraw,

            // Register viewer buttons.
            RegisterExpansionHeader,
            RegisterChannelHeader,
            ExpansionRegistersFirst, // One type per expansion for raw registers.
            ChannelStateFirst = ExpansionRegistersFirst + ExpansionType.Count, // One type per channel for status.

            Max = ChannelStateFirst + ChannelType.Count
        };

        enum SubButtonType
        {
            // Let's keep this enum and Envelope.XXX values in sync for convenience.
            VolumeEnvelope          = EnvelopeType.Volume,
            ArpeggioEnvelope        = EnvelopeType.Arpeggio,
            PitchEnvelope           = EnvelopeType.Pitch,
            DutyCycle               = EnvelopeType.DutyCycle,
            FdsWaveformEnvelope     = EnvelopeType.FdsWaveform,
            FdsModulationEnvelope   = EnvelopeType.FdsModulation,
            N163WaveformEnvelope    = EnvelopeType.N163Waveform,
            YMMixerSettingsEnvelope = EnvelopeType.YMMixerSettings,
            YMNoiseFreqEnvelope     = EnvelopeType.YMNoiseFreq,
            EnvelopeMax             = EnvelopeType.Count,

            // Other buttons
            Add = EnvelopeType.Count,
            DPCM,
            Load,
            Save,
            Sort,
            Reload,
            EditWave,
            Play,
            Expand,
            Overflow,
            Properties,
            Max
        }

        // From right to left. Looks more visually pleasing than the enum order.
        static readonly int[] EnvelopeDisplayOrder =
        {
            EnvelopeType.Arpeggio,
            EnvelopeType.Pitch,
            EnvelopeType.Volume,
            EnvelopeType.DutyCycle,
            EnvelopeType.FdsModulation,
            EnvelopeType.FdsWaveform,
            EnvelopeType.N163Waveform,
            EnvelopeType.YMMixerSettings,
            EnvelopeType.YMNoiseFreq
        };

        enum CaptureOperation
        {
            None,
            DragInstrument,
            DragInstrumentEnvelope,
            DragArpeggio,
            DragArpeggioValues,
            DragSample,
            DragSong,
            MoveSlider,
            SliderButtons,
            ScrollBar,
            MobilePan
        };

        static readonly bool[] captureNeedsThreshold = new[]
        {
            false, // None,
            true,  // DragInstrument,
            true,  // DragInstrumentEnvelope,
            true,  // DragArpeggio,
            true,  // DragArpeggioValues
            false, // DragSample,
            true,  // DragSong,
            false, // MoveSlider,
            false, // SliderButtons
            false, // ScrollBar
            false  // MobilePan
        };

        static readonly bool[] captureWantsRealTimeUpdate = new[]
        {
            false, // None,
            true,  // DragInstrument,
            true,  // DragInstrumentEnvelope,
            false, // DragArpeggio,
            false, // DragArpeggioValues
            false, // DragSample,
            true,  // DragSong,
            false, // MoveSlider,
            true,  // SliderButtons
            false, // ScrollBar
            false  // MobilePan
        };

        int scrollY = 0;
        int mouseLastX = 0;
        int mouseLastY = 0;
        int captureMouseX = -1;
        int captureMouseY = -1;
        int captureButtonRelX = -1;
        int captureButtonRelY = -1;
        int captureButtonIdx = -1;
        int captureScrollY = -1;
        int envelopeDragIdx = -1;
        int highlightedButtonIdx = -1;
        int captureButtonSign = 0;
        float flingVelY = 0.0f;
        float bitmapScale = 1.0f;
        float captureDuration = 0.0f;
        float blinkTimer;
        object blinkObject;
        bool captureThresholdMet = false;
        bool captureRealTimeUpdate = false;
        bool canFling = false;
        TabType selectedTab = TabType.Project;
        Button sliderDragButton = null;
        CaptureOperation captureOperation = CaptureOperation.None;
        CaptureOperation lastCaptureOperation = CaptureOperation.None;
        Instrument draggedInstrument = null;
        Instrument expandedInstrument = null;
        string selectedInstrumentTab = null;
        DPCMSample expandedSample = null;
        Arpeggio draggedArpeggio = null;
        DPCMSample draggedSample = null;
        Song draggedSong = null;
        List<Button> buttons = new List<Button>();

        // Register viewer stuff
        NesApu.NesRegisterValues registerValues;
        ExpansionRegisterViewer[] registerViewers = new ExpansionRegisterViewer[ExpansionType.Count];

        // Hover
        int hoverButtonIndex = -1;
        int hoverSubButtonTypeOrParamIndex = -1;

        Color sliderFillColor = Color.FromArgb(64, Color.Black);
        Color disabledColor   = Color.FromArgb(64, Color.Black);
        Color[] registerColors = new Color[11];
        BitmapAtlasRef bmpExpand;
        BitmapAtlasRef bmpExpanded;
        BitmapAtlasRef bmpOverflow;
        BitmapAtlasRef bmpCheckBoxYes;
        BitmapAtlasRef bmpCheckBoxNo;
        BitmapAtlasRef bmpButtonLeft;
        BitmapAtlasRef bmpButtonRight;
        BitmapAtlasRef bmpButtonMinus;
        BitmapAtlasRef bmpButtonPlus;
        BitmapAtlasRef bmpSong;
        BitmapAtlasRef bmpAdd;
        BitmapAtlasRef bmpSort;
        BitmapAtlasRef bmpPlay;
        BitmapAtlasRef bmpDPCM;
        BitmapAtlasRef bmpLoad;
        BitmapAtlasRef bmpWaveEdit;
        BitmapAtlasRef bmpReload;
        BitmapAtlasRef bmpProperties;
        BitmapAtlasRef[] bmpExpansions;
        BitmapAtlasRef[] bmpEnvelopes;
        BitmapAtlasRef[] bmpChannels;

        class Button
        {
            public string text;
            public Color color = Theme.DarkGreyColor5;
            public Color imageTint = Color.Black;
            public Color textColor;
            public Color textDisabledColor;
            public BitmapAtlasRef bmp;
            public int height;
            public bool gradient = true;
            public string[] tabNames;
            public RegisterViewerRow[] regs;

            public ButtonType type;
            public Song song;
            public Instrument instrument;
            public Arpeggio arpeggio;
            public DPCMSample sample;
            public ProjectExplorer projectExplorer;

            public ParamInfo param;
            public TransactionScope paramScope;
            public int paramObjectId;

            public Button(ProjectExplorer pe)
            {
                projectExplorer = pe;
                textColor = Theme.LightGreyColor2;
                textDisabledColor = pe.disabledColor;
                height = pe.buttonSizeY;
            }

            public SubButtonType[] GetSubButtons(out int active)
            {
                active = -1;

                switch (type)
                {
                    case ButtonType.SongHeader:
                    {
                        var buttons = new[] { SubButtonType.Add, SubButtonType.Load, SubButtonType.Sort };
                        active = projectExplorer.App.Project.AutoSortSongs ? 3 : -1;
                        return buttons;
                    }
                    case ButtonType.InstrumentHeader:
                    {
                        var buttons = new[] { SubButtonType.Add, SubButtonType.Load, SubButtonType.Sort };
                        active = projectExplorer.App.Project.AutoSortInstruments ? 3 : -1;
                        return buttons;
                    }
                    case ButtonType.ArpeggioHeader:
                    {
                        var buttons = new[] { SubButtonType.Add, SubButtonType.Sort };
                        active = projectExplorer.App.Project.AutoSortArpeggios ? 1 : -1;
                        return buttons;
                    }
                    case ButtonType.DpcmHeader:
                    {
                        var buttons = new[] { SubButtonType.Load, SubButtonType.Sort };
                        active = projectExplorer.App.Project.AutoSortSamples ? 1 : -1;
                        return buttons;
                    }
                    case ButtonType.ProjectSettings:
                    case ButtonType.Song:
                    {
                        return new[] { SubButtonType.Properties };
                    }
                    case ButtonType.Instrument:
                    {
                        var expandButton = projectExplorer.ShowExpandButtons() && InstrumentParamProvider.HasParams(instrument);
                        var numSubButtons = instrument.NumVisibleEnvelopes + (expandButton ? 1 : 0) + (instrument.IsRegular ? 1 : 0) + 1;
                        var buttons = new SubButtonType[numSubButtons];
                        var j = 0;

                        buttons[j++] = SubButtonType.Properties;
                        if (instrument.Expansion == ExpansionType.None)
                        {
                            if (!instrument.HasAnyMappedSamples)
                                active &= ~(1 << j);
                            buttons[j++] = SubButtonType.DPCM;
                        }

                        for (int i = 0; i < EnvelopeDisplayOrder.Length; i++)
                        {
                            int idx = EnvelopeDisplayOrder[i];
                            if (instrument.Envelopes[idx] != null)
                            {
                                buttons[j] = (SubButtonType)idx;
                                if (instrument.Envelopes[idx].IsEmpty(idx))
                                    active &= ~(1 << j);
                                j++;
                            }
                        }

                        if (expandButton)
                            buttons[numSubButtons - 1] = SubButtonType.Expand;

                        return buttons;
                    }
                    case ButtonType.Arpeggio:
                    {
                        if (arpeggio != null)
                            return new[] { SubButtonType.Properties, SubButtonType.ArpeggioEnvelope };
                        break;
                    }
                    case ButtonType.Dpcm:
                    {
                        if (Platform.IsMobile)
                        {
                            return new[] { SubButtonType.Properties, SubButtonType.EditWave, SubButtonType.Play, SubButtonType.Expand };
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(sample.SourceFilename))
                                active &= ~(1 << 2);
                            return new[] { SubButtonType.Properties, SubButtonType.EditWave, SubButtonType.Reload, SubButtonType.Play, SubButtonType.Expand };
                        }
                    }
                }

                return null;
            }

            public bool IsParam
            {
                get
                {
                    return
                        type == ButtonType.ParamCheckbox ||
                        type == ButtonType.ParamSlider ||
                        type == ButtonType.ParamCustomDraw ||
                        type == ButtonType.ParamList ||
                        type == ButtonType.ParamTabs;
                }
            }

            public string Text
            {
                get
                {
                    if (type == ButtonType.Dpcm)
                    {
                        return $"{sample.Name} ({sample.ProcessedData.Length} Bytes)";
                    }
                    else if (text != null)
                    {
                        return text;
                    }

                    return "";
                }
            }

            public Font Font
            {
                get
                {
                    if ((type == ButtonType.Song && song == projectExplorer.App.SelectedSong) ||
                        (type == ButtonType.Instrument && instrument == projectExplorer.App.SelectedInstrument) ||
                        (type == ButtonType.Arpeggio && arpeggio == projectExplorer.App.SelectedArpeggio))
                    {
                        return projectExplorer.Fonts.FontMediumBold;
                    }
                    else if (
                        type == ButtonType.ProjectSettings ||
                        type == ButtonType.SongHeader ||
                        type == ButtonType.InstrumentHeader ||
                        type == ButtonType.DpcmHeader ||
                        type == ButtonType.ArpeggioHeader ||
                        type == ButtonType.RegisterExpansionHeader ||
                        type == ButtonType.RegisterChannelHeader)
                    {
                        return projectExplorer.Fonts.FontMediumBold;
                    }
                    else
                    {
                        return projectExplorer.Fonts.FontMedium;
                    }
                }
            }

            public TextFlags TextAlignment
            {
                get
                {
                    if (type == ButtonType.ProjectSettings ||
                        type == ButtonType.SongHeader ||
                        type == ButtonType.InstrumentHeader ||
                        type == ButtonType.DpcmHeader ||
                        type == ButtonType.ArpeggioHeader)
                    {
                        return TextFlags.Center;
                    }
                    else
                    {
                        return TextFlags.Left;
                    }
                }
            }

            public object Object
            {
                get
                {
                    switch (type)
                    {
                        case ButtonType.Song: return song;
                        case ButtonType.Instrument: return instrument;
                        case ButtonType.Arpeggio: return arpeggio;
                        case ButtonType.Dpcm: return sample;
                    }

                    return null;
                }
            }


            public Color SubButtonTint => type == ButtonType.SongHeader || type == ButtonType.InstrumentHeader || type == ButtonType.DpcmHeader || type == ButtonType.ArpeggioHeader || type == ButtonType.ProjectSettings ? Theme.LightGreyColor1 : Color.Black;

            public bool TextEllipsis => type == ButtonType.ProjectSettings || ((type == ButtonType.Song || type == ButtonType.Instrument || type == ButtonType.Dpcm || type == ButtonType.Arpeggio) && Platform.IsMobile);
            
            public BitmapAtlasRef GetIcon(SubButtonType sub)
            {
                switch (sub)
                {
                    case SubButtonType.Add: 
                        return projectExplorer.bmpAdd;
                    case SubButtonType.Play:
                        return projectExplorer.bmpPlay;
                    case SubButtonType.DPCM: 
                        return projectExplorer.bmpDPCM;
                    case SubButtonType.EditWave:
                        return projectExplorer.bmpWaveEdit;
                    case SubButtonType.Reload: 
                        return projectExplorer.bmpReload;
                    case SubButtonType.Sort:
                        return projectExplorer.bmpSort;
                    case SubButtonType.Load: 
                        return projectExplorer.bmpLoad;
                    case SubButtonType.Properties:
                        return projectExplorer.bmpProperties;
                    case SubButtonType.Expand:
                    {
                        if (instrument != null)
                            return projectExplorer.expandedInstrument == instrument ? projectExplorer.bmpExpanded : projectExplorer.bmpExpand;
                        else
                            return projectExplorer.expandedSample == sample ? projectExplorer.bmpExpanded : projectExplorer.bmpExpand;
                    }
                }

                return projectExplorer.bmpEnvelopes[(int)sub];
            }
        }

        public DPCMSample DraggedSample => captureOperation == CaptureOperation.DragSample ? draggedSample : null;
        public bool IsActiveControl => App != null && App.ActiveControl == this;

        public delegate void EmptyDelegate();
        public delegate void BoolDelegate(bool val);
        public delegate void InstrumentEnvelopeDelegate(Instrument instrument, int envelope);
        public delegate void InstrumentDelegate(Instrument instrument);
        public delegate void InstrumentPointDelegate(Instrument instrument, Point pos);
        public delegate void SongDelegate(Song song);
        public delegate void ArpeggioDelegate(Arpeggio arpeggio);
        public delegate void ArpeggioPointDelegate(Arpeggio arpeggio, Point pos);
        public delegate void DPCMSamplePointDelegate(DPCMSample instrument, Point pos);
        public delegate void DPCMSampleDelegate(DPCMSample sample);

        public event InstrumentDelegate InstrumentColorChanged;
        public event InstrumentDelegate InstrumentReplaced;
        public event InstrumentDelegate InstrumentDeleted;
        public event InstrumentPointDelegate InstrumentDroppedOutside;
        public event SongDelegate SongModified;
        public event ArpeggioDelegate ArpeggioColorChanged;
        public event ArpeggioDelegate ArpeggioDeleted;
        public event ArpeggioPointDelegate ArpeggioDroppedOutside;
        public event DPCMSampleDelegate DPCMSampleReloaded;
        public event DPCMSampleDelegate DPCMSampleColorChanged;
        public event DPCMSampleDelegate DPCMSampleDeleted;
        public event DPCMSamplePointDelegate DPCMSampleDraggedOutside;
        public event DPCMSamplePointDelegate DPCMSampleMapped;
        public event EmptyDelegate ProjectModified;

        public ProjectExplorer()
        {
            Localization.Localize(this);

            registerValues = new NesApu.NesRegisterValues();
            registerViewers[ExpansionType.None] = new ApuRegisterViewer(registerValues);
            registerViewers[ExpansionType.Vrc6] = new Vrc6RegisterViewer(registerValues);
            registerViewers[ExpansionType.Vrc7] = new Vrc7RegisterViewer(registerValues);
            registerViewers[ExpansionType.Fds]  = new FdsRegisterViewer(registerValues);
            registerViewers[ExpansionType.Mmc5] = new Mmc5RegisterViewer(registerValues);
            registerViewers[ExpansionType.N163] = new N163RegisterViewer(registerValues);
            registerViewers[ExpansionType.S5B]  = new S5BRegisterViewer(registerValues);
            registerViewers[ExpansionType.EPSM] = new EpsmRegisterViewer(registerValues);
        }

        private void UpdateRenderCoords(bool updateVirtualSizeY = true)
        {
            expandButtonSizeX    = DpiScaling.ScaleForWindow(DefaultExpandButtonSizeX);
            buttonIconPosX       = DpiScaling.ScaleForWindow(DefaultButtonIconPosX);      
            buttonIconPosY       = DpiScaling.ScaleForWindow(DefaultButtonIconPosY);      
            buttonTextPosX       = DpiScaling.ScaleForWindow(DefaultButtonTextPosX);      
            buttonTextNoIconPosX = DpiScaling.ScaleForWindow(DefaultButtonTextNoIconPosX);
            expandButtonPosX     = DpiScaling.ScaleForWindow(DefaultExpandButtonPosX);
            expandButtonPosY     = DpiScaling.ScaleForWindow(DefaultExpandButtonPosY);
            subButtonSpacingX    = DpiScaling.ScaleForWindow(DefaultSubButtonSpacingX);
            subButtonMarginX     = DpiScaling.ScaleForWindow(DefaultSubButtonMarginX);
            subButtonSizeX       = DpiScaling.ScaleForWindow(DefaultSubButtonSizeX);
            subButtonPosY        = DpiScaling.ScaleForWindow(DefaultSubButtonPosY);       
            buttonSizeY          = DpiScaling.ScaleForWindow(DefaultButtonSizeY);
            sliderPosX           = DpiScaling.ScaleForWindow(DefaultSliderPosX);
            sliderPosY           = DpiScaling.ScaleForWindow(DefaultSliderPosY);
            sliderSizeX          = DpiScaling.ScaleForWindow(DefaultSliderSizeX);
            sliderSizeY          = DpiScaling.ScaleForWindow(DefaultSliderSizeY);
            checkBoxPosX         = DpiScaling.ScaleForWindow(DefaultCheckBoxPosX);
            checkBoxPosY         = DpiScaling.ScaleForWindow(DefaultCheckBoxPosY);
            paramRightPadX       = DpiScaling.ScaleForWindow(DefaultParamRightPadX);
            draggedLineSizeY     = DpiScaling.ScaleForWindow(DefaultDraggedLineSizeY);
            registerLabelSizeX   = DpiScaling.ScaleForWindow(DefaultRegisterLabelSizeX);
            paramButtonSizeX     = bmpButtonPlus != null ? DpiScaling.ScaleCustom(bmpButtonPlus.ElementSize.Width, bitmapScale) : 16;
            topTabSizeY          = Settings.ShowRegisterViewer ? buttonSizeY : 0;
            scrollAreaSizeY      = Height - topTabSizeY;
            contentSizeX         = Width;

            if (updateVirtualSizeY)
            {
                if (App != null && App.Project != null)
                {
                    virtualSizeY = 0;
                    foreach (var btn in buttons)
                        virtualSizeY += btn.height;
                }
                else
                {
                    virtualSizeY = Height;
                }

                needsScrollBar = virtualSizeY > scrollAreaSizeY;

                if (needsScrollBar)
                    scrollBarThickness = DpiScaling.ScaleForWindow(Settings.ScrollBars == 1 ? DefaultScrollBarThickness1 : (Settings.ScrollBars == 2 ? DefaultScrollBarThickness2 : 0));
                else
                    scrollBarThickness = 0;

                contentSizeX = Width - scrollBarThickness;
            }
        }

        public void Reset()
        {
            scrollY = 0;
            expandedInstrument = null;
            expandedSample = null;
            selectedInstrumentTab = null;
            RefreshButtons();
            MarkDirty();
        }

        public void LayoutChanged()
        {
            UpdateRenderCoords();
            ClampScroll();
            MarkDirty();
        }

        private ButtonType GetButtonTypeForParam(ParamInfo param)
        {
            var widgetType = ButtonType.ParamSlider;

            if (param.CustomDraw != null)
                widgetType = ButtonType.ParamCustomDraw;
            else if (param.IsList)
                widgetType = ButtonType.ParamList;
            else if (param.GetMaxValue() == 1)
                widgetType = ButtonType.ParamCheckbox;

            return widgetType;
        }

        private int GetHeightForRegisterRows(RegisterViewerRow[] regs)
        {
            var h = 0;
            for (int i = 0; i < regs.Length; i++)
                h += DpiScaling.ScaleForWindow(regs[i].Height);
            return h;
        }

        public void RefreshButtons(bool invalidate = true)
        {
            Debug.Assert(captureOperation != CaptureOperation.MoveSlider);

            if (selectedTab == TabType.Registers && !Settings.ShowRegisterViewer)
                selectedTab = TabType.Project;

            UpdateRenderCoords(false);

            buttons.Clear();
            var project = App.Project;

            if (ParentWindow == null || project == null)
                return;

            if (selectedTab == TabType.Project)
            {
                var projectText = string.IsNullOrEmpty(project.Author) ? $"{project.Name}" : $"{project.Name} ({project.Author})";

                buttons.Add(new Button(this) { type = ButtonType.ProjectSettings, text = projectText });
                buttons.Add(new Button(this) { type = ButtonType.SongHeader, text = SongsHeaderLabel });

                foreach (var song in project.Songs)
                    buttons.Add(new Button(this) { type = ButtonType.Song, song = song, text = song.Name, color = song.Color, bmp = bmpSong, textColor = Theme.BlackColor });

                buttons.Add(new Button(this) { type = ButtonType.InstrumentHeader, text = InstrumentHeaderLabel });

                foreach (var instrument in project.Instruments)
                {
                    buttons.Add(new Button(this) { type = ButtonType.Instrument, instrument = instrument, text = instrument.Name, color = instrument.Color, textColor = Theme.BlackColor, bmp = bmpExpansions[instrument.Expansion] });

                    if (instrument != null && instrument == expandedInstrument)
                    {
                        var instrumentParams = InstrumentParamProvider.GetParams(instrument);

                        if (instrumentParams != null)
                        {
                            List<string> tabNames = null;

                            foreach (var param in instrumentParams)
                            {
                                if (param.HasTab)
                                {
                                    if (tabNames == null)
                                        tabNames = new List<string>();

                                    if (!tabNames.Contains(param.TabName))
                                        tabNames.Add(param.TabName);
                                }
                            }

                            var tabCreated = false;

                            foreach (var param in instrumentParams)
                            {
                                if (!tabCreated && param.HasTab)
                                {
                                    buttons.Add(new Button(this) { type = ButtonType.ParamTabs, param = param, color = instrument.Color, tabNames = tabNames.ToArray() });
                                    tabCreated = true;
                                }

                                if (param.HasTab)
                                {
                                    if (string.IsNullOrEmpty(selectedInstrumentTab) || selectedInstrumentTab == param.TabName)
                                    {
                                        selectedInstrumentTab = param.TabName;
                                    }

                                    if (param.TabName != selectedInstrumentTab)
                                    {
                                        continue;
                                    }
                                }

                                var sizeY = param.CustomHeight > 0 ? param.CustomHeight * buttonSizeY : buttonSizeY;
                                buttons.Add(new Button(this) { type = GetButtonTypeForParam(param), param = param, instrument = instrument, color = instrument.Color, text = param.Name, textColor = Theme.BlackColor, paramScope = TransactionScope.Instrument, paramObjectId = instrument.Id, height = sizeY });
                            }
                        }
                    }
                }

                buttons.Add(new Button(this) { type = ButtonType.DpcmHeader, text= SamplesHeaderLabel });
                foreach (var sample in project.Samples)
                {
                    buttons.Add(new Button(this) { type = ButtonType.Dpcm, sample = sample, color = sample.Color, textColor = Theme.BlackColor, bmp = bmpDPCM });

                    if (sample == expandedSample)
                    {
                        var sampleParams = DPCMSampleParamProvider.GetParams(sample);

                        foreach (var param in sampleParams)
                        {
                            buttons.Add(new Button(this) { type = GetButtonTypeForParam(param), param = param, sample = sample, color = sample.Color, text = param.Name, textColor = Theme.BlackColor, paramScope = TransactionScope.DPCMSample, paramObjectId = sample.Id });
                        }
                    }
                }

                buttons.Add(new Button(this) { type = ButtonType.ArpeggioHeader, text = ArpeggiosHeaderLabel });
                buttons.Add(new Button(this) { type = ButtonType.Arpeggio, text = ArpeggioNoneLabel, color = Theme.LightGreyColor1, textColor = Theme.BlackColor });

                foreach (var arpeggio in project.Arpeggios)
                {
                    buttons.Add(new Button(this) { type = ButtonType.Arpeggio, arpeggio = arpeggio, text = arpeggio.Name, color = arpeggio.Color, textColor = Theme.BlackColor, bmp = bmpEnvelopes[EnvelopeType.Arpeggio] });
                }
            }
            else
            {
                var expansions = project.GetActiveExpansions();
                foreach (var e in expansions)
                {
                    var expRegs = registerViewers[e];

                    if (expRegs != null)
                    {
                        var expName = ExpansionType.GetLocalizedName(e, ExpansionType.LocalizationMode.ChipName);
                        buttons.Add(new Button(this) { type = ButtonType.RegisterExpansionHeader, text = RegistersExpansionHeaderLabel.Format(expName), bmp = bmpExpansions[e], imageTint = Theme.LightGreyColor2 });
                        buttons.Add(new Button(this) { type = ButtonType.ExpansionRegistersFirst + e, height = GetHeightForRegisterRows(expRegs.ExpansionRows), regs = expRegs.ExpansionRows, gradient = false });

                        var channels = Channel.GetChannelsForExpansionMask(ExpansionType.GetMaskFromValue(e), project.ExpansionNumN163Channels);
                        var firstChannel = e == ExpansionType.None ? 0 : ChannelType.ExpansionAudioStart;
                        for (int i = firstChannel; i < channels.Length; i++)
                        {
                            var c = channels[i];
                            var idx = channels[i] - channels[firstChannel]; // Assumes contiguous channels.
                            var chanRegs = expRegs.ChannelRows[idx];

                            if (chanRegs != null && chanRegs.Length > 0)
                            {
                                buttons.Add(new Button(this) { type = ButtonType.RegisterChannelHeader, text = ChannelType.LocalizedNames[c], bmp = bmpChannels[c], imageTint = Theme.LightGreyColor2 });
                                buttons.Add(new Button(this) { type = ButtonType.ChannelStateFirst + c, height = GetHeightForRegisterRows(chanRegs), regs = chanRegs, gradient = false });
                            }
                        }
                    }
                }
            }
            
            flingVelY = 0.0f;
            highlightedButtonIdx = -1;

            UpdateRenderCoords();
            ClampScroll();

            if (invalidate)
                MarkDirty();
        }

        public void BlinkButton(object obj)
        {
            blinkTimer = obj == null ? 0.0f : 2.0f;
            blinkObject = obj;

            if (obj != null)
            {
                // Scroll to that item.
                for (int i = 0; i < buttons.Count; i++)
                {
                    if (buttons[i].Object == obj)
                    {
                        var buttonY = i * buttonSizeY;
                        scrollY = buttonY - Height / 2;
                        ClampScroll();
                        MarkDirty();
                        return;
                    }
                }
            }
        }

        protected override void OnAddedToContainer()
        {
            var g = ParentWindow.Graphics;
            bmpExpansions  = g.GetBitmapAtlasRefs(ExpansionType.Icons);
            bmpEnvelopes   = g.GetBitmapAtlasRefs(EnvelopeType.Icons);
            bmpChannels    = g.GetBitmapAtlasRefs(ChannelType.Icons);
            bmpExpand      = g.GetBitmapAtlasRef("InstrumentExpand");
            bmpExpanded    = g.GetBitmapAtlasRef("InstrumentExpanded");
            bmpOverflow    = g.GetBitmapAtlasRef("Warning");
            bmpCheckBoxYes = g.GetBitmapAtlasRef("CheckBoxYes");
            bmpCheckBoxNo  = g.GetBitmapAtlasRef("CheckBoxNo");
            bmpButtonLeft  = g.GetBitmapAtlasRef("ButtonLeft");
            bmpButtonRight = g.GetBitmapAtlasRef("ButtonRight");
            bmpButtonMinus = g.GetBitmapAtlasRef("ButtonMinus");
            bmpButtonPlus  = g.GetBitmapAtlasRef("ButtonPlus");
            bmpSong        = g.GetBitmapAtlasRef("Music");
            bmpAdd         = g.GetBitmapAtlasRef("Add");
            bmpPlay        = g.GetBitmapAtlasRef("PlaySource");
            bmpDPCM        = g.GetBitmapAtlasRef("ChannelDPCM");
            bmpLoad        = g.GetBitmapAtlasRef("InstrumentOpen");
            bmpWaveEdit    = g.GetBitmapAtlasRef("WaveEdit");
            bmpReload      = g.GetBitmapAtlasRef("Reload");
            bmpAdd         = g.GetBitmapAtlasRef("Add");
            bmpProperties  = g.GetBitmapAtlasRef("Properties");
            bmpSort        = g.GetBitmapAtlasRef("Sort");

            var color0 = Theme.LightGreyColor2; // Grey
            var color1 = Theme.CustomColors[14, 5]; // Orange
            var color2 = Theme.CustomColors[0,  5]; // Red

            for (int i = 0; i < registerColors.Length; i++)
            {
                var alpha = i / (float)(registerColors.Length - 1);
                var color = Color.FromArgb(
                    (int)Utils.Lerp(color2.R, color0.R, alpha),
                    (int)Utils.Lerp(color2.G, color0.G, alpha),
                    (int)Utils.Lerp(color2.B, color0.B, alpha));
                registerColors[i] = color;
            }

            if (Platform.IsMobile)
                bitmapScale = DpiScaling.Window * 0.25f;

            UpdateRenderCoords();
            RefreshButtons();
        }

        protected bool ShowExpandButtons()
        {
            if (App.Project != null)
            {
                if (App.Project.Instruments.Find(i => InstrumentParamProvider.HasParams(i)) != null)
                    return true;

                if (App.Project.Samples.Count > 0)
                    return true;
            }

            return false;
        }
        
        public int ScaleLineForWindow(int width) 
        { 
            return width == 1 ? 1 : DpiScaling.ScaleForWindow(width) | 1; 
        }

        private void RenderDebug(Graphics g)
        {
#if DEBUG
            if (Platform.IsMobile)
            {
                g.OverlayCommandList.FillRectangle(mouseLastX - 30, mouseLastY - 30, mouseLastX + 30, mouseLastY + 30, Theme.WhiteColor);
            }
#endif
        }

        private void RenderTabs(CommandList c)
        {
            var numTabs = (int)TabType.Count;
            var tabSizeX = Width / numTabs;

            for (int i = 0; i < numTabs; i++)
            {
                var activeTab = i == (int)selectedTab;
                var tabColor  = activeTab ? Theme.DarkGreyColor4 : Theme.Darken(Theme.DarkGreyColor4);
                var textBrush = activeTab ? Theme.LightGreyColor2 : Theme.LightGreyColor1;
                var textFont  = activeTab ? Fonts.FontMediumBold : Fonts.FontMedium;
                var x0 = (i + 0) * tabSizeX;
                var x1 = (i + 1) * tabSizeX;
                c.FillAndDrawRectangleGradient(x0, 0, x1, buttonSizeY, tabColor, Color.FromArgb(200, tabColor), Theme.BlackColor, true, buttonSizeY, 1);
                c.DrawText(TabNames[i], textFont, x0, 0, textBrush, TextFlags.MiddleCenter, tabSizeX, buttonSizeY);
            }
        }

        private void RenderRegisterRows(NesApu.NesRegisterValues regValues, CommandList c, Button button, int exp = -1)
        {
            int y = 0;

            for (int i = 0; i < button.regs.Length; i++)
            {
                var reg = button.regs[i];
                var regSizeY = DpiScaling.ScaleForWindow(reg.Height);

                c.PushTranslation(0, y);

                if (i != 0)
                    c.DrawLine(0, -1, contentSizeX, -1, Theme.BlackColor);

                if (reg.CustomDraw != null)
                {
                    var label = reg.Label;
                    c.DrawText(label, Fonts.FontSmall, buttonTextNoIconPosX, 0, Theme.LightGreyColor2, TextFlags.Middle, 0, regSizeY);

                    c.PushTranslation(registerLabelSizeX + 1, 0);
                    reg.CustomDraw(c, Fonts, new Rectangle(0, 0, contentSizeX - registerLabelSizeX - 1, regSizeY));
                    c.PopTransform();
                }
                else if (reg.GetValue != null)
                {
                    var label = reg.Label;
                    var value = reg.GetValue().ToString();
                    var flags = reg.Monospace ? TextFlags.Middle | TextFlags.Monospace : TextFlags.Middle;

                    c.DrawText(label, Fonts.FontSmall, buttonTextNoIconPosX, 0, Theme.LightGreyColor2, TextFlags.Middle, 0, regSizeY);
                    c.DrawText(value, Fonts.FontSmall, buttonTextNoIconPosX + registerLabelSizeX, 0, Theme.LightGreyColor2, flags, 0, regSizeY);
                }
                else
                {
                    Debug.Assert(exp >= 0);

                    c.DrawText(reg.Label, Fonts.FontSmall, buttonTextNoIconPosX, 0, Theme.LightGreyColor2, TextFlags.Middle, 0, regSizeY);

                    var flags = TextFlags.Monospace | TextFlags.Middle;
                    var x = buttonTextNoIconPosX + registerLabelSizeX;

                    for (var r = reg.AddStart; r <= reg.AddEnd; r++)
                    {
                        for (var s = reg.SubStart; s <= reg.SubEnd; s++)
                        {
                            var val = regValues.GetRegisterValue(exp, r, out var age, s);
                            var str = $"${val:X2} ";
                            var color = registerColors[Math.Min(age, registerColors.Length - 1)];

                            c.DrawText(str, Fonts.FontSmall, x, 0, color, flags, 0, regSizeY);
                            x += (int)c.Graphics.MeasureString(str, Fonts.FontSmall, true);
                        }
                    }
                }

                c.PopTransform();
                y += regSizeY;
            }

            c.DrawLine(registerLabelSizeX, 0, registerLabelSizeX, button.height, Theme.BlackColor);
        }

        protected override void OnRender(Graphics g)
        {
            CommandList c = g.DefaultCommandList;

            if (Settings.ShowRegisterViewer)
            {
                RenderTabs(c);
                c.PushTranslation(0, buttonSizeY);
                c.PushClipRegion(0, 0, width, height - buttonSizeY);
            }

            if (selectedTab == TabType.Registers)
            {
                App.ActivePlayer.GetRegisterValues(registerValues);
            }

            c.DrawLine(0, 0, 0, Height, Theme.BlackColor);

            var showExpandButton = ShowExpandButtons();
            var firstParam = true;
            var y = -scrollY;
            var iconSize = DpiScaling.ScaleCustom(bmpEnvelopes[0].ElementSize.Width, bitmapScale);

            var minInstIdx = 1000000;
            var maxInstIdx = 0;
            var minArpIdx  = 1000000;
            var maxArpIdx  = 0;

            var disabledOpacity = 0.25f;
            var disabledColor = Color.FromArgb(64, Color.Black);

            for (int i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                var subButtons = button.GetSubButtons(out var activeMask);
                var firstSubButtonX = subButtons != null ? contentSizeX - subButtonMarginX - (subButtonSpacingX + subButtonSizeX) * subButtons.Count(b => b != SubButtonType.Expand) : contentSizeX;
                var hovered = i == hoverButtonIndex;
                var highlighted = i == highlightedButtonIdx;

                if (y + button.height >= 0)
                {
                    c.PushTranslation(0, y);

                    var groupSizeY = button.height;
                    var drawBackground = true;

                    if (button.IsParam)
                    {
                        if (firstParam)
                        {
                            for (int j = i + 1; j < buttons.Count; j++)
                            {
                                if (!buttons[j].IsParam)
                                    break;

                                groupSizeY += buttons[j].height;
                            }

                            firstParam = false;
                        }
                        else
                        {
                            drawBackground = false;
                        }
                    }

                    if (drawBackground)
                    {
                        if (button.gradient)
                        {
                            var bgColor = button.color;

                            if (blinkTimer != 0.0f && button.Object == blinkObject)
                                bgColor = Theme.Darken(bgColor, (int)(MathF.Sin(blinkTimer * MathF.PI * 8.0f) * 16 + 16));

                            c.FillAndDrawRectangleGradient(0, 0, contentSizeX, groupSizeY, bgColor, Color.FromArgb(200, bgColor), Theme.BlackColor, true, groupSizeY, 1);
                        }
                        else
                        {
                            c.FillAndDrawRectangle(0, 0, contentSizeX, groupSizeY, button.color, Theme.BlackColor, 1);
                        }
                    }

                    if (button.type == ButtonType.Instrument)
                    {
                        if (button.instrument != null)
                        {
                            minInstIdx = Math.Min(minInstIdx, i);
                            maxInstIdx = Math.Max(maxInstIdx, i);
                        }
                    }
                    else if (button.type == ButtonType.Arpeggio)
                    {
                        if (button.arpeggio != null)
                        {
                            minArpIdx = Math.Min(minArpIdx, i);
                            maxArpIdx = Math.Max(maxArpIdx, i);
                        }
                    }

                    var leftPadding = 0;
                    var leftAligned = button.type == ButtonType.Instrument || button.type == ButtonType.Song || button.type == ButtonType.ParamSlider || button.type == ButtonType.ParamCheckbox || button.type == ButtonType.ParamList || button.type == ButtonType.Arpeggio || button.type == ButtonType.Dpcm || button.type == ButtonType.ParamCustomDraw || button.type == ButtonType.ParamTabs;

                    if (showExpandButton && leftAligned)
                    {
                        leftPadding = 1 + expandButtonSizeX;
                        c.PushTranslation(leftPadding, 0);
                    }

                    var enabled = button.param == null || button.param.IsEnabled == null || button.param.IsEnabled();
                    var ellipsisFlag = button.TextEllipsis ? TextFlags.Ellipsis : TextFlags.None;
                    var centered = button.TextAlignment.HasFlag(TextFlags.Center);
                    var player = App.ActivePlayer;

                    if (button.type == ButtonType.ParamCustomDraw)
                    {
                        button.param.CustomDraw(c, Fonts, new Rectangle(0, 0, contentSizeX - leftPadding - paramRightPadX - 1, button.height), button.param.CustomUserData1, button.param.CustomUserData2);
                    }
                    else if (button.type >= ButtonType.ExpansionRegistersFirst && button.type < ButtonType.ChannelStateFirst)
                    {
                        RenderRegisterRows(registerValues, c, button, button.type - ButtonType.ExpansionRegistersFirst);
                    }
                    else if (button.type >= ButtonType.ChannelStateFirst)
                    {
                        RenderRegisterRows(registerValues, c, button);
                    }
                    else
                    {
                        if (button.Text != null)
                        {
                            var textX = button.bmp == null ? buttonTextNoIconPosX : buttonTextPosX;
                            c.DrawText(button.Text, button.Font, textX, 0, enabled ? button.textColor : disabledColor, button.TextAlignment | ellipsisFlag | TextFlags.Middle, (centered ? contentSizeX - textX * 2 : firstSubButtonX - buttonTextPosX - leftPadding), buttonSizeY);
                        }

                        if (button.bmp != null)
                        {
                            c.DrawBitmapAtlas(button.bmp, buttonIconPosX, buttonIconPosY, 1.0f, bitmapScale, button.imageTint);
                            if (highlighted && (button.type == ButtonType.Song || button.type == ButtonType.Instrument || button.type == ButtonType.Dpcm || button.type == ButtonType.Arpeggio))
                            { 
                                c.DrawRectangle(buttonIconPosX, buttonIconPosY, buttonIconPosX + iconSize - 4, buttonIconPosY + iconSize - 4, Theme.WhiteColor, 3, true, true);
                            }
                        }
                    }

                    if (leftPadding != 0)
                        c.PopTransform();

                    if (button.param != null)
                    {
                        var paramVal = button.param.GetValue();
                        var paramStr = button.param.GetValueString();

                        if (button.type == ButtonType.ParamSlider)
                        {
                            var paramMinValue = button.param.GetMinValue();
                            var paramMaxValue = button.param.GetMaxValue();
                            var actualSliderSizeX = sliderSizeX - paramButtonSizeX * 2;
                            var valSizeX = paramMaxValue == paramMinValue ? 0 : (int)Math.Round((paramVal - paramMinValue) / (float)(paramMaxValue - paramMinValue) * actualSliderSizeX);
                            var opacityL = enabled ? (hovered && hoverSubButtonTypeOrParamIndex == 1 ? 0.6f : 1.0f) : disabledOpacity;
                            var opacityR = enabled ? (hovered && hoverSubButtonTypeOrParamIndex == 2 ? 0.6f : 1.0f) : disabledOpacity;

                            c.PushTranslation(contentSizeX - sliderPosX, sliderPosY);
                            c.DrawBitmapAtlas(bmpButtonMinus, 0, 0, opacityL, bitmapScale, Color.Black);
                            c.PushTranslation(paramButtonSizeX, 0);
                            c.FillRectangle(1, 1, valSizeX, sliderSizeY, sliderFillColor);
                            c.DrawRectangle(0, 0, actualSliderSizeX, sliderSizeY, enabled ? Theme.BlackColor : disabledColor, 1);
                            c.DrawText(paramStr, Fonts.FontMedium, 0, -sliderPosY, enabled ? Theme.BlackColor : disabledColor, TextFlags.MiddleCenter, actualSliderSizeX, buttonSizeY);
                            c.PopTransform();
                            c.DrawBitmapAtlas(bmpButtonPlus, paramButtonSizeX + actualSliderSizeX, 0, opacityR, bitmapScale, Color.Black);
                            c.PopTransform();
                        }
                        else if (button.type == ButtonType.ParamCheckbox)
                        {
                            var opacity = enabled ? (hovered && hoverSubButtonTypeOrParamIndex == 1 ? 0.6f : 1.0f) : disabledOpacity;

                            c.PushTranslation(contentSizeX - checkBoxPosX, checkBoxPosY);
                            c.DrawRectangle(0, 0, bmpCheckBoxYes.ElementSize.Width * bitmapScale - 1, bmpCheckBoxYes.ElementSize.Height * bitmapScale - 1, Color.FromArgb(opacity, Color.Black));
                            c.DrawBitmapAtlas(paramVal == 0 ? bmpCheckBoxNo : bmpCheckBoxYes, 0, 0, opacity, bitmapScale, Color.Black);
                            c.PopTransform();
                        }
                        else if (button.type == ButtonType.ParamList)
                        {
                            var paramPrev = button.param.SnapAndClampValue(paramVal - 1);
                            var paramNext = button.param.SnapAndClampValue(paramVal + 1);
                            var opacityL = enabled && paramVal != paramPrev ? (hovered && hoverSubButtonTypeOrParamIndex == 1 ? 0.6f : 1.0f) : disabledOpacity;
                            var opacityR = enabled && paramVal != paramNext ? (hovered && hoverSubButtonTypeOrParamIndex == 2 ? 0.6f : 1.0f) : disabledOpacity;

                            c.PushTranslation(contentSizeX - sliderPosX, sliderPosY);
                            c.DrawBitmapAtlas(bmpButtonLeft, 0, 0, opacityL, bitmapScale, Color.Black);
                            c.DrawBitmapAtlas(bmpButtonRight, sliderSizeX - paramButtonSizeX, 0, opacityR, bitmapScale, Color.Black);
                            c.DrawText(paramStr, Fonts.FontMedium, 0, -sliderPosY, Theme.BlackColor, TextFlags.MiddleCenter, sliderSizeX, button.height);
                            c.PopTransform();
                        }
                        else if (button.type == ButtonType.ParamTabs)
                        {
                            var tabWidth = Utils.DivideAndRoundUp(contentSizeX - leftPadding - paramRightPadX, button.tabNames.Length);

                            for (var j = 0; j < button.tabNames.Length; j++)
                            {
                                var tabName         = button.tabNames[j];
                                var tabHoverOpacity = hovered && hoverSubButtonTypeOrParamIndex == j ? 0.6f : 1.0f;
                                var tabSelect       = tabName == selectedInstrumentTab;
                                var tabLineBrush    = Color.FromArgb((tabSelect ? 1.0f : 0.5f) * tabHoverOpacity, Color.Black);
                                var tabFont         = tabSelect ? Fonts.FontMediumBold : Fonts.FontMedium;
                                var tabLine         = tabSelect ? 3 : 1;

                                c.PushTranslation(leftPadding + tabWidth * j, 0);
                                c.DrawText(tabName, tabFont, 0, 0, tabLineBrush, TextFlags.MiddleCenter, tabWidth, button.height);
                                c.DrawLine(0, button.height - tabLine / 2, tabWidth, button.height - tabLine / 2, tabLineBrush, ScaleLineForWindow(tabLine));
                                c.PopTransform();

                            }
                        }
                    }
                    else
                    {
                        var tint = button.SubButtonTint;

                        if (subButtons != null)
                        {
                            for (int j = 0, x = contentSizeX - subButtonMarginX - subButtonSizeX; j < subButtons.Length; j++, x -= (subButtonSpacingX + subButtonSizeX))
                            {
                                var sub = subButtons[j];
                                var bmp = button.GetIcon(sub);
                                var hoverOpacity = hovered && (int)sub == hoverSubButtonTypeOrParamIndex ? 0.6f : 1.0f;

                                if (sub == SubButtonType.Expand)
                                {
                                    c.DrawBitmapAtlas(bmp, expandButtonPosX, expandButtonPosY, hoverOpacity, bitmapScale, tint);
                                }
                                else
                                {
                                    c.DrawBitmapAtlas(bmp, x, subButtonPosY, ((activeMask & (1 << j)) != 0 ? 1.0f : 0.2f) * hoverOpacity, bitmapScale, tint);

                                    if (highlighted && sub < SubButtonType.EnvelopeMax)
                                        c.DrawRectangle(x, subButtonPosY, x + iconSize - 4, subButtonPosY + iconSize - 4, Theme.WhiteColor, 3, true, true);
                                }
                            }
                        }
                    }

                    c.PopTransform();
                }

                y += button.height;

                if (y > scrollAreaSizeY)
                {
                    break;
                }
            }

            if (captureOperation != CaptureOperation.None && captureThresholdMet)
            {
                if (captureOperation == CaptureOperation.DragSong       ||
                    captureOperation == CaptureOperation.DragInstrument ||
                    captureOperation == CaptureOperation.DragSample     ||
                    captureOperation == CaptureOperation.DragArpeggio)
                {
                    var pt = Platform.IsDesktop ? ScreenToControl(CursorPosition) : new Point(mouseLastX, mouseLastY);
                    if (ClientRectangle.Contains(pt.X, pt.Y))
                    { 
                        var buttonIdx = GetButtonAtCoord(pt.X, pt.Y - buttonSizeY / 2, out _);

                        if (buttonIdx >= 0)
                        {
                            var button = buttons[buttonIdx];

                            if ((captureOperation == CaptureOperation.DragSong       && (button.type == ButtonType.Song       || button.type == ButtonType.SongHeader)) ||
                                (captureOperation == CaptureOperation.DragInstrument && (button.type == ButtonType.Instrument || button.type == ButtonType.InstrumentHeader)) ||
                                (captureOperation == CaptureOperation.DragSample     && (button.type == ButtonType.Dpcm       || button.type == ButtonType.DpcmHeader)) ||
                                (captureOperation == CaptureOperation.DragArpeggio   && (button.type == ButtonType.Arpeggio))) // No header to account for "None" arp.
                            {
                                var lineY = (buttonIdx + 1) * buttonSizeY - scrollY;
                                var lineColor = Color.Black;

                                switch (captureOperation)
                                {
                                    case CaptureOperation.DragSong:       lineColor = draggedSong.Color;       break;
                                    case CaptureOperation.DragInstrument: lineColor = draggedInstrument.Color; break;
                                    case CaptureOperation.DragSample:     lineColor = draggedSample.Color;     break;
                                    case CaptureOperation.DragArpeggio:   lineColor = draggedArpeggio.Color;   break;
                                }

                                c.DrawLine(0, lineY, contentSizeX, lineY, lineColor, draggedLineSizeY);
                            }
                        }
                    }
                }
                else if ((captureOperation == CaptureOperation.DragInstrumentEnvelope || captureOperation == CaptureOperation.DragArpeggioValues) && envelopeDragIdx >= 0)
                {
                    var pt = Platform.IsDesktop ? ScreenToControl(CursorPosition) : new Point(mouseLastX, mouseLastY);
                    if (ClientRectangle.Contains(pt))
                    {
                        var bx = pt.X - captureButtonRelX;
                        var by = pt.Y - captureButtonRelY - topTabSizeY;

                        c.DrawBitmapAtlas(bmpEnvelopes[envelopeDragIdx], bx, by, 0.5f, bitmapScale, Color.Black);

                        if (Platform.IsMobile)
                            c.DrawRectangle(bx, by, bx + iconSize - 4, by + iconSize - 4, Theme.WhiteColor, 3, true, true);
                    }
                }
            }
            if (needsScrollBar)
            {
                int scrollBarSizeY = (int)Math.Round(scrollAreaSizeY * (scrollAreaSizeY / (float)virtualSizeY));
                int scrollBarPosY = (int)Math.Round(scrollAreaSizeY * (scrollY / (float)virtualSizeY));

                c.FillAndDrawRectangle(contentSizeX, 0, Width - 1, Height, Theme.DarkGreyColor4, Theme.BlackColor);
                c.FillAndDrawRectangle(contentSizeX, scrollBarPosY, Width - 1, scrollBarPosY + scrollBarSizeY, Theme.MediumGreyColor1, Theme.BlackColor);
            }

            c.DrawLine(0, 0, Width, 0, Theme.BlackColor);

            if (Settings.ShowRegisterViewer)
            {
                c.PopClipRegion();
                c.PopTransform();
            }

            RenderDebug(g);
        }

        private bool GetScrollBarParams(out int posY, out int sizeY)
        {
            if (scrollBarThickness > 0)
            {
                sizeY = (int)Math.Round(scrollAreaSizeY * (scrollAreaSizeY / (float)virtualSizeY));
                posY  = (int)Math.Round(scrollAreaSizeY * (scrollY         / (float)virtualSizeY));
                return true;
            }
            else
            {
                posY = 0;
                sizeY = 0;
                return false;
            }
        }

        private bool ClampScroll()
        {
            int minScrollY = 0;
            int maxScrollY = Math.Max(virtualSizeY - scrollAreaSizeY, 0);

            var scrolled = true;
            if (scrollY < minScrollY) { scrollY = minScrollY; scrolled = false; }
            if (scrollY > maxScrollY) { scrollY = maxScrollY; scrolled = false; }
            return scrolled;
        }

        private bool DoScroll(int deltaY)
        {
            scrollY -= deltaY;
            MarkDirty();
            return ClampScroll();
        }

        protected void UpdateCursor()
        {
            if ((captureOperation == CaptureOperation.DragInstrumentEnvelope || captureOperation == CaptureOperation.DragArpeggioValues) && captureThresholdMet)
            {
                Cursor = Cursors.CopyCursor;
            }
            else if (
                captureOperation == CaptureOperation.DragSong       ||
                captureOperation == CaptureOperation.DragInstrument ||
                captureOperation == CaptureOperation.DragArpeggio   ||
                captureOperation == CaptureOperation.DragSample)
            {
                Cursor = Cursors.DragCursor;
            }
            else
            {
                Cursor = Cursors.Default;
            }
        }

        private int GetButtonAtCoord(int x, int y, out SubButtonType sub, out int buttonRelX, out int buttonRelY)
        {
            sub = SubButtonType.Max;
            buttonRelX = 0;
            buttonRelY = 0;

            if (needsScrollBar && x >= contentSizeX)
                return -1;

            var absY = y + scrollY;
            var buttonIndex = -1;
            var buttonBaseY = topTabSizeY;

            for (int i = 0; i < buttons.Count; i++)
            {
                var button = buttons[i];
                if (absY >= buttonBaseY && absY < buttonBaseY + button.height)
                {
                    buttonIndex = i;
                    break;
                }
                buttonBaseY += button.height;
            }

            if (buttonIndex >= 0 && buttonIndex < buttons.Count)
            {
                var button = buttons[buttonIndex];

                if (ShowExpandButtons() && x < (expandButtonPosX + expandButtonSizeX) && ((button.instrument != null && InstrumentParamProvider.HasParams(button.instrument)) || (button.sample != null))) 
                {
                    sub = SubButtonType.Expand;
                    return buttonIndex;
                }

                buttonRelX = x;
                buttonRelY = y - buttonBaseY + scrollY;

                var subButtons = button.GetSubButtons(out _);
                if (subButtons != null)
                {
                    y -= (buttonBaseY - scrollY);

                    for (int i = 0; i < subButtons.Length; i++)
                    {
                        if (subButtons[i] == SubButtonType.Expand)
                            continue;

                        int sx = contentSizeX - subButtonMarginX - (subButtonSpacingX + subButtonSizeX) * (i + 1);
                        int sy = subButtonPosY;
                        int dx = x - sx;
                        int dy = y - sy;

                        if (dx >= 0 && dx < 16 * DpiScaling.Window &&
                            dy >= 0 && dy < 16 * DpiScaling.Window)
                        {
                            buttonRelX = dx;
                            buttonRelY = dy;
                            sub = subButtons[i];
                            break;
                        }
                    }
                }

                return buttonIndex;
            }
            else
            {
                return -1;
            }
        }

        private int GetButtonAtCoord(int x, int y, out SubButtonType sub)
        {
            return GetButtonAtCoord(x, y, out sub, out _, out _);
        }

        private void UpdateToolTip(int x, int y)
        {
            var redTooltip = false;
            var tooltip = "";
            var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType);

            // TODO: Store this in the button itself... this is stupid.
            if (buttonIdx >= 0)
            {
                var button = buttons[buttonIdx];
                var buttonType = button.type;

                if (buttonType == ButtonType.SongHeader)
                {
                    if (subButtonType == SubButtonType.Add)
                    {
                        tooltip = $"<MouseLeft> {AddNewSongTooltip}";
                    }
                    else if (subButtonType == SubButtonType.Load)
                    {
                        tooltip = $"<MouseLeft> {ImportSongsTooltip}";
                    }
                    else if (subButtonType == SubButtonType.Sort)
                    {
                        tooltip = App.Project.AutoSortSongs ? AutoSortSongActiveTooltip.Format("<MouseRight>") : $"<MouseLeft> {SortSongTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                    }
                }
                else if (buttonType == ButtonType.Song)
                {
                    if (subButtonType == SubButtonType.Properties)
                    {
                        tooltip = $"<MouseLeft> {PropertiesSongTooltip}";
                    }
                    else
                    {
                        tooltip = $"<MouseLeft> {MakeSongCurrentTooltip} - <MouseLeft><Drag> {ReorderSongsTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                    }
                }
                else if (buttonType == ButtonType.InstrumentHeader)
                {
                    if (subButtonType == SubButtonType.Add)
                    {
                        tooltip = $"<MouseLeft> {AddNewInstrumentTooltip}";
                        if (App.Project.NeedsExpansionInstruments)
                            tooltip += $" - <MouseRight> {MoreOptionsTooltip}";
                    }
                    else if (subButtonType == SubButtonType.Load)
                    {
                        tooltip = $"<MouseLeft> {ImportInstrumentsTooltip}";
                    }
                    else if (subButtonType == SubButtonType.Sort)
                    {
                        tooltip = App.Project.AutoSortInstruments ? AutoSortInstrumentActiveTooltip.Format("<MouseRight>") : $"<MouseLeft> {SortInstrumentsTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                    }
                }
                else if (buttonType == ButtonType.ArpeggioHeader)
                {
                    if (subButtonType == SubButtonType.Add)
                    {
                        tooltip = $"<MouseLeft> {AddNewArpeggioTooltip}";
                    }
                    else if (subButtonType == SubButtonType.Sort)
                    {
                        tooltip = App.Project.AutoSortArpeggios ? AutoSortArpeggioActiveTooltip.Format("<MouseRight>") : $"<MouseLeft> {SortArpeggiosTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                    }
                }
                else if (buttonType == ButtonType.ProjectSettings)
                {
                    if (subButtonType == SubButtonType.Properties)
                    {
                        tooltip = $"<MouseLeft> {PropertiesProjectTooltip}";
                    }
                    else
                    {
                        tooltip = $"<MouseRight> {MoreOptionsTooltip}";
                    }
                }
                else if (buttonType == ButtonType.ParamCheckbox)
                {
                    if (IsPointInCheckbox(x, y))
                    {
                        tooltip = $"<MouseLeft> {ToggleValueTooltip}\n<MouseRight> {MoreOptionsTooltip}.";
                    }
                    else if (button.param.ToolTip != null)
                    {
                        tooltip = button.param.ToolTip;
                    }
                }
                else if (buttonType == ButtonType.ParamSlider)
                {
                    if (x >= contentSizeX - sliderPosX)
                    {
                        tooltip = $"<MouseLeft><Drag> {ChangeValueTooltip} - <Ctrl><MouseLeft><Drag> {ChangeValueFineTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                    }
                    else if (button.param.ToolTip != null)
                    {
                        tooltip = button.param.ToolTip;
                    }
                }
                else if (buttonType == ButtonType.ParamList)
                {
                    if (x >= contentSizeX - sliderPosX)
                    {
                        tooltip = $"<MouseLeft> {ChangeValueTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                    }
                    else if (button.param.ToolTip != null)
                    {
                        tooltip = button.param.ToolTip;
                    }
                }
                else if (buttonType == ButtonType.Instrument)
                {
                    if (subButtonType == SubButtonType.Max)
                    {
                        if (buttons[buttonIdx].instrument == null)
                            tooltip = $"<MouseLeft> {SelectInstrumentTooltip}";
                        else
                            tooltip = $"<MouseLeft> {SelectInstrumentTooltip} - <MouseLeft><Drag> {CopyReplaceInstrumentTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                    }
                    else
                    {
                        if (subButtonType == SubButtonType.DPCM)
                        {
                            tooltip = $"<MouseLeft> {EditSamplesTooltip}";
                        }
                        else if (subButtonType < SubButtonType.EnvelopeMax)
                        {
                            tooltip = $"<MouseLeft> {EditEnvelopeTooltip.Format(EnvelopeType.LocalizedNames[(int)subButtonType].Value.ToLower())} - <MouseLeft><Drag> {CopyEnvelopeTooltip} - <MouseRight> {MoreOptionsTooltip}";
                        }
                        else if (subButtonType == SubButtonType.Properties)
                        {
                            tooltip = $"<MouseLeft> {PropertiesInstrumentTooltip}";
                        }
                    }
                }
                else if (buttonType == ButtonType.Dpcm)
                {
                    if (subButtonType == SubButtonType.Play)
                    {
                        tooltip = $"<MouseLeft> {PreviewProcessedSampleTooltip}\n<MouseRight> {PlaySourceSampleTooltip}";
                    }
                    else if (subButtonType == SubButtonType.EditWave)
                    {
                        tooltip = $"<MouseLeft> {EditWaveformTooltip}";
                    }
                    else if (subButtonType == SubButtonType.Reload)
                    {
                        tooltip = $"<MouseLeft> {ReloadSourceDataTooltip}";
                    }
                    else if (subButtonType == SubButtonType.Max)
                    {
                        tooltip = $"<MouseRight> {MoreOptionsTooltip}";
                    }
                    else if (subButtonType == SubButtonType.Properties)
                    {
                        tooltip = $"<MouseLeft> {PropertiesInstrumentTooltip}";
                    }
                }
                else if (buttonType == ButtonType.DpcmHeader)
                {
                    if (subButtonType == SubButtonType.Load)
                    {
                        tooltip = $"<MouseLeft> {ImportSamplesTooltip}";
                    }
                    else if (subButtonType == SubButtonType.Sort)
                    {
                        tooltip = App.Project.AutoSortSamples ? AutoSortSampleActiveTooltip.Format("<MouseRight>") : $"<MouseLeft> {SortSamplesTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                    }
                }
                else if (buttonType == ButtonType.Arpeggio)
                {
                    if (subButtonType == SubButtonType.Max)
                    {
                        tooltip = $"<MouseLeft> {SelectArpeggioTooltip} - <MouseLeft><Drag> {ReplaceArpeggioTooltip}\n<MouseRight> {MoreOptionsTooltip}";
                    }
                    else if (subButtonType == SubButtonType.Properties)
                    {
                        tooltip = $"<MouseLeft> {PropertiesArpeggioTooltip}";
                    }
                }
            }
            else if (needsScrollBar && x > contentSizeX)
            {
                tooltip = "<MouseLeft><Drag> Scroll";
            }

            App.SetToolTip(tooltip, redTooltip);
        }

        private void ScrollIfNearEdge(int x, int y)
        {
            int minY = Platform.IsMobile && IsLandscape ? 0      : -buttonSizeY;
            int maxY = Platform.IsMobile && IsLandscape ? Height : Height + buttonSizeY;

            scrollY += Utils.ComputeScrollAmount(y, minY, buttonSizeY, App.AverageTickRate * ScrollSpeedFactor, true);
            scrollY += Utils.ComputeScrollAmount(y, maxY, buttonSizeY, App.AverageTickRate * ScrollSpeedFactor, false);

            ClampScroll();
        }

        private void UpdateSlider(int x, int y, bool final)
        {
            if (!final)
            {
                UpdateSliderValue(sliderDragButton, x, y, false);
                MarkDirty();
            }
            else
            {
                App.UndoRedoManager.EndTransaction();
            }
        }

        private void UpdateSliderButtons(bool first, bool final)
        {
            // Transition to auto increment after 350ms.
            if (first || captureDuration >= 0.35f)
            {
                var button = buttons[captureButtonIdx];
                var val = button.param.GetValue();
                var incLarge = button.param.SnapValue * 10;
                var incSmall = button.param.SnapValue;
                var inc = captureDuration > 1.5f && (val % incLarge) == 0 ? incLarge : incSmall;

                val = button.param.SnapAndClampValue(val + inc * captureButtonSign);                
                button.param.SetValue(val);
                MarkDirty();
            }

            if (final)
            {
                App.UndoRedoManager.EndTransaction();
            }
        }

        private void UpdateScrollBar(int x, int y)
        {
            scrollY = captureScrollY + ((y - captureMouseY) * virtualSizeY / Height);
            ClampScroll();
            MarkDirty();
        }

        private void UpdateDrag(int x, int y, bool final)
        {
            if (final)
            {
                var buttonIdx = GetButtonAtCoord(x, y - buttonSizeY / 2, out _);
                var button = buttonIdx >= 0 ? buttons[buttonIdx] : null;
                var inside = ClientRectangle.Contains(x, y);

                if (captureOperation == CaptureOperation.DragSong)
                {
                    if (inside && button != null && (button.type == ButtonType.Song || button.type == ButtonType.SongHeader))
                    {
                        var songBefore = buttons[buttonIdx].song;
                        if (songBefore != draggedSong)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                            App.Project.MoveSong(draggedSong, songBefore);
                            App.Project.AutoSortSongs = false;
                            App.UndoRedoManager.EndTransaction();
                        }
                    }
                }
                else if (captureOperation == CaptureOperation.DragInstrument)
                {
                    if (inside && button != null && (button.type == ButtonType.Instrument || button.type == ButtonType.InstrumentHeader))
                    {
                        var instrumentBefore = buttons[buttonIdx].instrument;
                        if (instrumentBefore != draggedInstrument)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                            App.Project.MoveInstrument(draggedInstrument, instrumentBefore);
                            App.Project.AutoSortInstruments = false;
                            App.UndoRedoManager.EndTransaction();
                        }
                    }
                    else if (Platform.IsDesktop && !inside)
                    {
                        InstrumentDroppedOutside(draggedInstrument, ControlToScreen(new Point(x, y)));
                    }
                }
                else if (captureOperation == CaptureOperation.DragArpeggio)
                {
                    if (inside && button != null && button.type == ButtonType.Arpeggio)
                    {
                        var arpBefore = buttons[buttonIdx].arpeggio;
                        if (arpBefore != draggedArpeggio)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                            App.Project.MoveArpeggio(draggedArpeggio, arpBefore);
                            App.Project.AutoSortArpeggios = false;
                            App.UndoRedoManager.EndTransaction();
                        }
                    }
                    else if (Platform.IsDesktop && !inside)
                    {
                        ArpeggioDroppedOutside(draggedArpeggio, ControlToScreen(new Point(x, y)));
                    }
                }
                else if (captureOperation == CaptureOperation.DragSample)
                {
                    if (inside && button != null && (button.type == ButtonType.Dpcm || button.type == ButtonType.DpcmHeader))
                    {
                        var sampleBefore = buttons[buttonIdx].sample;
                        if (sampleBefore != draggedSample)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                            App.Project.MoveSample(draggedSample, sampleBefore);
                            App.Project.AutoSortSamples = false;
                            App.UndoRedoManager.EndTransaction();
                        }
                    }
                    else if (Platform.IsDesktop && !inside)
                    {
                        var mappingNote = App.GetDPCMSampleMappingNoteAtPos(ControlToScreen(new Point(x, y)), out var instrument);
                        if (instrument != null)
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrument.Id, -1, TransactionFlags.StopAudio);
                            instrument.UnmapDPCMSample(mappingNote);
                            instrument.MapDPCMSample(mappingNote, draggedSample);
                            App.UndoRedoManager.EndTransaction();
                            DPCMSampleMapped?.Invoke(draggedSample, ControlToScreen(new Point(x, y)));
                        }
                    }
                }

                RefreshButtons();
            }
            else
            {
                ScrollIfNearEdge(x, y);
                MarkDirty();

                if (Platform.IsDesktop && captureOperation == CaptureOperation.DragSample && !ClientRectangle.Contains(x, y))
                {
                    DPCMSampleDraggedOutside?.Invoke(draggedSample, ControlToScreen(new Point(x, y)));
                }
            }
        }

        private void UpdateDragInstrumentEnvelope(int x, int y, bool final)
        {
            if (final)
            {
                if (ClientRectangle.Contains(x, y))
                {
                    var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType);

                    var instrumentSrc = draggedInstrument;
                    var instrumentDst = buttonIdx >= 0 && buttons[buttonIdx].type == ButtonType.Instrument ? buttons[buttonIdx].instrument : null;

                    if (instrumentSrc != instrumentDst && instrumentSrc != null && instrumentDst != null && envelopeDragIdx != -1)
                    {
                        if (instrumentSrc.Expansion == instrumentDst.Expansion)
                        {
                            Platform.MessageBoxAsync(ParentWindow, $"Are you sure you want to copy the {EnvelopeType.LocalizedNames[envelopeDragIdx]} envelope of instrument '{instrumentSrc.Name}' to '{instrumentDst.Name}'?", "Copy Envelope", MessageBoxButtons.YesNo, (r) =>
                            {
                                if (r == DialogResult.Yes)
                                {
                                    App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, instrumentDst.Id);
                                    instrumentDst.Envelopes[envelopeDragIdx] = instrumentSrc.Envelopes[envelopeDragIdx].ShallowClone();
                                    instrumentDst.Envelopes[envelopeDragIdx].ClampToValidRange(instrumentDst, envelopeDragIdx);

                                    // HACK : Copy some envelope related stuff. Need to cleanup the envelope code.
                                    switch (envelopeDragIdx)
                                    {
                                        case EnvelopeType.FdsWaveform:
                                            instrumentDst.FdsWavePreset = instrumentSrc.FdsWavePreset;
                                            break;
                                        case EnvelopeType.FdsModulation:
                                            instrumentDst.FdsModPreset = instrumentSrc.FdsModPreset;
                                            break;
                                        case EnvelopeType.N163Waveform:
                                            instrumentDst.N163WavePreset = instrumentSrc.N163WavePreset;
                                            instrumentDst.N163WaveSize = instrumentSrc.N163WaveSize;
                                            break;
                                    }

                                    App.UndoRedoManager.EndTransaction();

                                    if (Platform.IsDesktop)
                                        App.StartEditInstrument(instrumentDst, envelopeDragIdx);
                                }
                            });
                        }
                        else
                        {
                            App.DisplayNotification($"Incompatible audio expansion!"); ;
                        }
                    }
                }
            }
            else
            {
                ScrollIfNearEdge(x, y);
                MarkDirty();
            }
        }

        private void UpdateDragArpeggioValues(int x, int y, bool final)
        {
            if (final)
            {
                if (ClientRectangle.Contains(x, y))
                {
                    var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType);

                    var arpeggioSrc = draggedArpeggio;
                    var arpeggioDst = buttonIdx >= 0 && buttons[buttonIdx].type == ButtonType.Arpeggio ? buttons[buttonIdx].arpeggio : null;

                    if (arpeggioSrc != arpeggioDst && arpeggioSrc != null && arpeggioDst != null && envelopeDragIdx != -1)
                    {
                        Platform.MessageBoxAsync(ParentWindow, CopyArpeggioMessage.Format(arpeggioSrc.Name, arpeggioDst.Name), CopyArpeggioTitle, MessageBoxButtons.YesNo, (r) =>
                        {
                            if (r == DialogResult.Yes)
                            {
                                App.UndoRedoManager.BeginTransaction(TransactionScope.Arpeggio, arpeggioDst.Id);
                                arpeggioDst.Envelope.Length = arpeggioSrc.Envelope.Length;
                                arpeggioDst.Envelope.Loop = arpeggioSrc.Envelope.Loop;
                                Array.Copy(arpeggioSrc.Envelope.Values, arpeggioDst.Envelope.Values, arpeggioDst.Envelope.Values.Length);
                                App.UndoRedoManager.EndTransaction();
                                if (Platform.IsDesktop)
                                    App.StartEditArpeggio(arpeggioDst);
                            }
                        });
                    }
                }
            }
            else
            {
                ScrollIfNearEdge(x, y);
                MarkDirty();
            }
        }

        private void UpdateCaptureOperation(int x, int y, bool realTime = false, float delta = 0.0f)
        {
            const int CaptureThreshold = Platform.IsDesktop ? 5 : 50;

            if (captureOperation != CaptureOperation.None && !captureThresholdMet)
            {
                if (Math.Abs(x - captureMouseX) >= CaptureThreshold ||
                    Math.Abs(y - captureMouseY) >= CaptureThreshold)
                {
                    captureThresholdMet = true;
                }
            }

            if (captureOperation != CaptureOperation.None && realTime)
            {
                captureDuration += delta;
                Debug.WriteLine($"Duration {captureDuration} {delta}");
            }

            if (captureOperation != CaptureOperation.None && captureThresholdMet && (captureRealTimeUpdate || !realTime))
            {
                switch (captureOperation)
                {
                    case CaptureOperation.MoveSlider:
                        UpdateSlider(x, y, false);
                        break;
                    case CaptureOperation.SliderButtons:
                        UpdateSliderButtons(false, false);
                        break;
                    case CaptureOperation.ScrollBar:
                        UpdateScrollBar(x, y);
                        break;
                    case CaptureOperation.DragInstrumentEnvelope:
                        UpdateDragInstrumentEnvelope(x, y, false);
                        break;
                    case CaptureOperation.DragArpeggioValues:
                        UpdateDragArpeggioValues(x, y, false);
                        break;
                    case CaptureOperation.DragSong:
                    case CaptureOperation.DragInstrument:
                    case CaptureOperation.DragArpeggio:
                    case CaptureOperation.DragSample:
                        UpdateDrag(x, y, false);
                        break;
                    case CaptureOperation.MobilePan:
                        DoScroll(y - mouseLastY);
                        break;
                    default:
                        MarkDirty();
                        break;
                }
            }

            lastCaptureOperation = CaptureOperation.None;
        }

        protected void ConditionalShowExpansionIcons(int x, int y)
        {
            var buttonIdx = GetButtonAtCoord(x, y, out _);
            App.SequencerShowExpansionIcons = buttonIdx >= 0 && (buttons[buttonIdx].type == ButtonType.Instrument || buttons[buttonIdx].type == ButtonType.InstrumentHeader);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool middle = e.Middle || (e.Left && ModifierKeys.IsAltDown && Settings.AltLeftForMiddle);

            UpdateCursor();
            UpdateCaptureOperation(e.X, e.Y);
            UpdateHover(e);

            if (middle)
                DoScroll(e.Y - mouseLastY);

            UpdateToolTip(e.X, e.Y);
            ConditionalShowExpansionIcons(e.X, e.Y);

            mouseLastX = e.X;
            mouseLastY = e.Y;
        }

        private void UpdateHover(MouseEventArgs e)
        {
            if (Platform.IsDesktop)
            {
                var buttonIdx = GetButtonAtCoord(e.X, e.Y, out var subButtonType);
                var sub = -1;

                if (buttonIdx >= 0)
                {
                    var button = buttons[buttonIdx];

                    switch (button.type)
                    {
                        case ButtonType.ParamTabs:
                            sub = GetTabIndex(e.X, e.Y, button);
                            break;
                        case ButtonType.ParamCheckbox:
                            if (IsPointInCheckbox(e.X, e.Y)) sub = 1;
                            break;
                        case ButtonType.ParamList:
                        case ButtonType.ParamSlider:
                            if (IsPointInParamListOrSliderButton(e.X, e.Y, true)) sub = 1;
                            else if (IsPointInParamListOrSliderButton(e.X, e.Y, false)) sub = 2;
                            // TODO : Highlight slider here.
                            break;
                        default:
                            sub = (int)subButtonType;
                            break;
                    }
                }

                SetAndMarkDirty(ref hoverButtonIndex, buttonIdx);
                SetAndMarkDirty(ref hoverSubButtonTypeOrParamIndex, sub);
            }
        }

        private void ClearHover()
        {
            if (Platform.IsDesktop)
            {
                SetAndMarkDirty(ref hoverButtonIndex, -1);
                SetAndMarkDirty(ref hoverSubButtonTypeOrParamIndex, -1);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            App.SequencerShowExpansionIcons = false;
            ClearHover();
        }

        protected bool HandleMouseUpButtons(MouseEventArgs e)
        {
            return e.Right && HandleContextMenuButtons(e.X, e.Y);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool middle = e.Middle;
            bool doMouseUp = false;

            if (!middle)
            {
                doMouseUp = captureOperation == CaptureOperation.None;
                EndCaptureOperation(e.X, e.Y);
            }

            UpdateCursor();

            if (doMouseUp)
            {
                if (HandleMouseUpButtons(e)) goto Handled;
                return;
            Handled:
                MarkDirty();
            }
        }

        private void StartCaptureOperation(int x, int y, CaptureOperation op, int buttonIdx = -1, int buttonRelX = 0, int buttonRelY = 0)
        {
            Debug.Assert(captureOperation == CaptureOperation.None);
            mouseLastX = x;
            mouseLastY = y;
            captureMouseX = x;
            captureMouseY = y;
            captureButtonIdx = buttonIdx;
            captureButtonRelX = buttonRelX;
            captureButtonRelY = buttonRelY;
            captureScrollY = scrollY;
            Capture = true;
            canFling = false;
            captureOperation = op;
            lastCaptureOperation = CaptureOperation.None;
            captureThresholdMet = !captureNeedsThreshold[(int)op];
            captureRealTimeUpdate = captureWantsRealTimeUpdate[(int)op];
            captureDuration = 0.0f;
        }

        private void EndCaptureOperation(int x, int y)
        {
            if (captureOperation != CaptureOperation.None && captureThresholdMet)
            {
                switch (captureOperation)
                {
                    case CaptureOperation.DragInstrumentEnvelope:
                        UpdateDragInstrumentEnvelope(x, y, true);
                        break;
                    case CaptureOperation.DragArpeggioValues:
                        UpdateDragArpeggioValues(x, y, true);
                        break;
                    case CaptureOperation.MoveSlider:
                        UpdateSlider(x, y, true);
                        break;
                    case CaptureOperation.SliderButtons:
                        UpdateSliderButtons(false, true);
                        break;
                    case CaptureOperation.DragSong:
                    case CaptureOperation.DragInstrument:
                    case CaptureOperation.DragArpeggio:
                    case CaptureOperation.DragSample:
                        UpdateDrag(x, y, true);
                        break;
                    case CaptureOperation.MobilePan:
                        canFling = true;
                        break;
                }
            }

            draggedArpeggio = null;
            draggedInstrument = null;
            draggedSample = null;
            draggedSong = null;
            sliderDragButton = null;
            lastCaptureOperation = captureOperation;
            captureOperation = CaptureOperation.None;
            Capture = false;
            MarkDirty();
        }

        private void AbortCaptureOperation()
        {
            if (App.UndoRedoManager.HasTransactionInProgress)
                App.UndoRedoManager.AbortTransaction();

            MarkDirty();

            draggedArpeggio = null;
            draggedInstrument = null;
            draggedSample = null;
            draggedSong = null;
            sliderDragButton = null;
            captureOperation = CaptureOperation.None;
            Capture = false;
            canFling = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            DoScroll(e.ScrollY > 0 ? buttonSizeY * 3 : -buttonSizeY * 3);
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateRenderCoords();
            ClampScroll();
        }

        bool UpdateSliderValue(Button button, int x, int y, bool mustBeInside)
        {
            var buttonIdx = buttons.IndexOf(button);
            Debug.Assert(buttonIdx >= 0);

            var ctrl = ModifierKeys.IsControlDown;
            var buttonTopY = 0;

            foreach (var b in buttons)
            {
                if (b == button)
                    break;

                buttonTopY += b.height;
            }

            var buttonX = x;
            var buttonY = y + scrollY - buttonTopY - topTabSizeY;

            var sliderMinX = contentSizeX - sliderPosX + paramButtonSizeX;
            var sliderMaxX = sliderMinX + (sliderSizeX - paramButtonSizeX * 2);

            bool insideSlider = (buttonX > (sliderMinX) &&
                                 buttonX < (sliderMaxX) &&
                                 buttonY > (sliderPosY) &&
                                 buttonY < (sliderPosY + sliderSizeY));

            if (mustBeInside && !insideSlider)
                return false;

            var paramVal = button.param.GetValue();

            if (ctrl)
            {
                var delta = (x - captureMouseX) / 4;
                if (delta != 0)
                {
                    paramVal = Utils.Clamp(paramVal + delta * button.param.SnapValue, button.param.GetMinValue(), button.param.GetMaxValue());
                    captureMouseX = x;
                }
            }
            else
            {
                paramVal = (int)Math.Round(Utils.Lerp(button.param.GetMinValue(), button.param.GetMaxValue(), Utils.Clamp((buttonX - sliderMinX) / (float)(sliderMaxX - sliderMinX), 0.0f, 1.0f)));
                captureMouseX = x;
            }

            paramVal = button.param.SnapAndClampValue(paramVal);
            button.param.SetValue(paramVal);

            App.Project.GetPackedSampleData();

            return insideSlider;
        }

        private void ImportSongs()
        {
            Action<string> ImportSongsAction = (filename) =>
            {
                if (filename != null)
                {
                    App.BeginLogTask();
                    App.OpenProjectFileAsync(filename, false, (otherProject) =>
                    {
                        if (otherProject != null)
                        {
                            var songNames = new List<string>();
                            foreach (var song in otherProject.Songs)
                                songNames.Add(song.Name);

                            var dlg = new PropertyDialog(ParentWindow, ImportSongsTitle, 300);
                            dlg.Properties.AddLabel(null, ImportSongsLabel.Colon); // 0
                            dlg.Properties.AddCheckBoxList(null, songNames.ToArray(), null); // 1
                            dlg.Properties.AddButton(null, SelectAllLabel); // 2
                            dlg.Properties.AddButton(null, SelectNoneLabel); // 3
                            dlg.Properties.PropertyClicked += ImportSongs_PropertyClicked;
                            dlg.Properties.Build();

                            dlg.ShowDialogAsync((r) =>
                            {
                                if (r == DialogResult.OK)
                                {
                                    App.UndoRedoManager.BeginTransaction(TransactionScope.Project, TransactionFlags.StopAudio);

                                    var selected = dlg.Properties.GetPropertyValue<bool[]>(1);
                                    var songIds = new List<int>();

                                    for (int i = 0; i < selected.Length; i++)
                                    {
                                        if (selected[i])
                                            songIds.Add(otherProject.Songs[i].Id);
                                    }

                                    bool success = false;
                                    if (songIds.Count > 0)
                                    {
                                        otherProject.DeleteAllSongsBut(songIds.ToArray());
                                        success = App.Project.MergeProject(otherProject);
                                    }

                                    App.UndoRedoManager.AbortOrEndTransaction(success);
                                    RefreshButtons();

                                    if (!success && Platform.IsMobile && Log.GetLastMessage(LogSeverity.Error) != null)
                                    {
                                        Platform.DelayedMessageBoxAsync(Log.GetLastMessage(LogSeverity.Error), ErrorTitle);
                                    }

                                    App.EndLogTask();
                                }
                                else
                                {
                                    App.AbortLogTask();
                                }
                            });
                        }
                        else
                        {
                            App.AbortLogTask();
                        }
                    });
                }
            };

            if (Platform.IsMobile)
            {
                MobileProjectDialog dlg = new MobileProjectDialog(App, ImportSongsTitle, false, false);
                dlg.ShowDialogAsync((f) => ImportSongsAction(f));
            }
            else
            {
                var filename = Platform.ShowOpenFileDialog("Open File", "All Song Files (*.fms;*.txt;*.ftm)|*.fms;*.txt;*.ftm|FamiStudio Files (*.fms)|*.fms|FamiTracker Files (*.ftm)|*.ftm|FamiTracker Text Export (*.txt)|*.txt|FamiStudio Text Export (*.txt)|*.txt", ref Settings.LastInstrumentFolder);
                ImportSongsAction(filename);
            }
        }

        private void SortSongs()
        {
            if (!App.Project.AutoSortSongs)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                App.Project.SortSongs();
                App.UndoRedoManager.EndTransaction();
                RefreshButtons();
            }
        }

        private void HandleContextMenuSortSongs(int x, int y)
        {
            App.ShowContextMenu(left + x, top + y, new[]
            {
                new ContextMenuOption(AutoSortSongsContext, null, () => 
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                    App.Project.AutoSortSongs = !App.Project.AutoSortSongs;
                    App.UndoRedoManager.EndTransaction();
                    RefreshButtons();
                }, () => App.Project.AutoSortSongs ? ContextMenuCheckState.Checked : ContextMenuCheckState.Unchecked)
            });
        }

        private void ImportSongs_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (click == ClickType.Button)
            {
                var newValues = new bool[props.GetPropertyValue<bool[]>(1).Length];

                if (propIdx == 2)
                {
                    for (int i = 0; i < newValues.Length; i++)
                        newValues[i] = true;
                }

                props.UpdateCheckBoxList(1, newValues);
            }
        }

        private void ImportInstruments()
        {
            Action<string> ImportInstrumentsAction = (filename) =>
            {
                if (filename != null)
                {
                    if (filename.ToLower().EndsWith("fti"))
                    {
                        App.BeginLogTask();
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                        var success = new FamitrackerInstrumentFile().CreateFromFile(App.Project, filename) != null;
                        App.UndoRedoManager.AbortOrEndTransaction(success);
                        RefreshButtons();
                        App.EndLogTask();
                    }
                    if (filename.ToLower().EndsWith("bti"))
                    {
                        App.BeginLogTask();
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                        var success = new BambootrackerInstrumentFile().CreateFromFile(App.Project, filename) != null;
                        App.UndoRedoManager.AbortOrEndTransaction(success);
                        RefreshButtons();
                        App.EndLogTask();
                    }
                    else
                    {
                        App.BeginLogTask();
                        App.OpenProjectFileAsync(filename, false, (instrumentProject) => 
                        {
                            if (instrumentProject != null)
                            {
                                var instruments = new List<Instrument>();
                                var instrumentNames = new List<string>();

                                foreach (var instrument in instrumentProject.Instruments)
                                {
                                    instruments.Add(instrument);
                                    instrumentNames.Add(instrument.NameWithExpansion);
                                }

                                var dlg = new PropertyDialog(ParentWindow, ImportInstrumentsTitle, 300);
                                dlg.Properties.AddLabel(null, ImportInstrumentsLabel.Colon); // 0
                                dlg.Properties.AddCheckBoxList(null, instrumentNames.ToArray(), null); // 1
                                dlg.Properties.AddButton(null, SelectAllLabel); // 2
                                dlg.Properties.AddButton(null, SelectNoneLabel); // 3
                                dlg.Properties.Build();
                                dlg.Properties.PropertyClicked += ImportInstrument_PropertyClicked;

                                dlg.ShowDialogAsync((r) =>
                                {
                                    if (r == DialogResult.OK)
                                    {
                                        var selected = dlg.Properties.GetPropertyValue<bool[]>(1);
                                        var instrumentsIdsToMerge = new List<int>();

                                        for (int i = 0; i < selected.Length; i++)
                                        {
                                            if (selected[i])
                                                instrumentsIdsToMerge.Add(instruments[i].Id);
                                        }

                                        // Wipe everything but the instruments we want.
                                        instrumentProject.DeleteAllSongs();
                                        instrumentProject.DeleteAllArpeggios();
                                        instrumentProject.DeleteAllInstrumentBut(instrumentsIdsToMerge.ToArray());
                                        instrumentProject.DeleteUnmappedSamples();

                                        App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                                        var success = App.Project.MergeProject(instrumentProject);
                                        App.UndoRedoManager.AbortOrEndTransaction(success);
                                        RefreshButtons();

                                        App.EndLogTask();
                                    }
                                    else
                                    {
                                        App.AbortLogTask();
                                    }
                                });
                            }
                            else
                            {
                                App.AbortLogTask();
                            }
                        });
                    }
                }
            };

            if (Platform.IsMobile)
            {
                MobileProjectDialog dlg = new MobileProjectDialog(App, ImportInstrumentsTitle, false, false);
                dlg.ShowDialogAsync((f) => ImportInstrumentsAction(f));
            }
            else
            {
                var filename = Platform.ShowOpenFileDialog("Open File", "All Instrument Files (*.fti;*.fms;*.txt;*.ftm;*.bti)|*.fti;*.fms;*.txt;*.ftm;*.bti|FamiTracker Instrument File (*.fti)|*.fti|BambooTracker Instrument File (*.bti)|*.bti|FamiStudio Files (*.fms)|*.fms|FamiTracker Files (*.ftm)|*.ftm|FamiTracker Text Export (*.txt)|*.txt|FamiStudio Text Export (*.txt)|*.txt", ref Settings.LastInstrumentFolder);
                ImportInstrumentsAction(filename);
            }
        }

        private void ImportInstrument_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (click == ClickType.Button)
            {
                var newValues = new bool[props.GetPropertyValue<bool[]>(1).Length];

                if (propIdx == 2)
                {
                    for (int i = 0; i < newValues.Length; i++)
                        newValues[i] = true;
                }

                props.UpdateCheckBoxList(1, newValues);
            }
        }

        private void SortInstruments()
        {
            if (!App.Project.AutoSortInstruments)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                App.Project.SortInstruments();
                App.UndoRedoManager.EndTransaction();
                RefreshButtons();
            }
        }

        private void HandleContextMenuSortInstruments(int x, int y)
        {
            App.ShowContextMenu(left + x, top + y, new[]
            {
                new ContextMenuOption(AutoSortInstrumentsContext, null, () =>
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                    App.Project.AutoSortInstruments = !App.Project.AutoSortInstruments;
                    App.UndoRedoManager.EndTransaction();
                    RefreshButtons();
                }, () => App.Project.AutoSortInstruments ? ContextMenuCheckState.Checked : ContextMenuCheckState.Unchecked)
            });
        }

        private void LoadDPCMSample()
        {
            Action<string[]> LoadDPCMSampleAction = (filenames) =>
            {
                if (filenames != null && filenames.Length > 0)
                {
                    var numFamiStudioFiles = 0;
                    var numSamplesFiles = 0;
                    foreach (var fn in filenames)
                    {
                        var ext = Path.GetExtension(fn).ToLower();

                        if (ext == ".fms" && Platform.IsDesktop)
                            numFamiStudioFiles++;
                        else if (ext == ".dmc" || ext == ".wav")
                            numSamplesFiles++;
                    }

                    if (numFamiStudioFiles > 1 || (numFamiStudioFiles == 1 && numSamplesFiles != 0))
                    {
                        Platform.MessageBoxAsync(ParentWindow, "You can only select one FamiStudio project to import samples from.", "Error", MessageBoxButtons.OK);
                        return;
                    }
                    else if (numFamiStudioFiles == 1)
                    {
                        App.OpenProjectFileAsync(filenames[0], false, (samplesProject) => 
                        {
                            if (samplesProject != null)
                            {
                                if (samplesProject.Samples.Count == 0)
                                {
                                    Platform.MessageBox(ParentWindow, "The selected project does not contain any samples.", "Error", MessageBoxButtons.OK);
                                    return;
                                }

                                var samplesNames = new List<string>();

                                foreach (var sample in samplesProject.Samples)
                                    samplesNames.Add(sample.Name);

                                var dlg = new PropertyDialog(ParentWindow, ImportSamplesTitle, 300);
                                dlg.Properties.AddLabel(null, ImportSamplesLabel.Colon); // 0
                                dlg.Properties.AddCheckBoxList(null, samplesNames.ToArray(), null); // 1
                                dlg.Properties.AddButton(null, SelectAllLabel); // 2
                                dlg.Properties.AddButton(null, SelectNoneLabel); // 3
                                dlg.Properties.Build();
                                dlg.Properties.PropertyClicked += ImportInstrument_PropertyClicked;

                                dlg.ShowDialogAsync((r) =>
                                {
                                    if (r == DialogResult.OK)
                                    {
                                        App.BeginLogTask();
                                        {
                                            var selected = dlg.Properties.GetPropertyValue<bool[]>(1);
                                            var sampleIdsToMerge = new List<int>();

                                            for (int i = 0; i < selected.Length; i++)
                                            {
                                                if (selected[i])
                                                    sampleIdsToMerge.Add(samplesProject.Samples[i].Id);
                                            }

                                            // Wipe everything but the instruments we want.
                                            samplesProject.DeleteAllSongs();
                                            samplesProject.DeleteAllArpeggios();
                                            samplesProject.DeleteAllSamplesBut(sampleIdsToMerge.ToArray());
                                            samplesProject.DeleteAllInstruments();

                                            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamples);
                                            bool success = App.Project.MergeProject(samplesProject);
                                            App.UndoRedoManager.AbortOrEndTransaction(success);
                                        }
                                        App.EndLogTask();
                                        RefreshButtons();
                                    }
                                });
                            }
                        });
                    }
                    else if (numSamplesFiles > 0)
                    {
                        App.BeginLogTask();
                        {
                            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamples);

                            var importedSamples = new List<DPCMSample>();

                            foreach (var filename in filenames)
                            {
                                var sampleName = Path.GetFileNameWithoutExtension(filename);
                                if (sampleName.Length > 16)
                                    sampleName = sampleName.Substring(0, 16);
                                sampleName = App.Project.GenerateUniqueDPCMSampleName(sampleName);

                                if (Path.GetExtension(filename).ToLower() == ".wav")
                                {
                                    var wavData = WaveFile.Load(filename, out var sampleRate);
                                    if (wavData != null)
                                    {
                                        var maximumSamples = sampleRate * 2;
                                        if (wavData.Length > maximumSamples)
                                        {
                                            Array.Resize(ref wavData, maximumSamples);
                                            Log.LogMessage(LogSeverity.Warning, MaxWavFileWarning.Format(2));
                                        }

                                        var sample = App.Project.CreateDPCMSampleFromWavData(sampleName, wavData, sampleRate, filename);
                                        importedSamples.Add(sample);
                                    }
                                }
                                else if (Path.GetExtension(filename).ToLower() == ".dmc")
                                {
                                    var dmcData = File.ReadAllBytes(filename);
                                    if (dmcData.Length > DPCMSample.MaxSampleSize)
                                    {
                                        Array.Resize(ref dmcData, DPCMSample.MaxSampleSize);
                                        Log.LogMessage(LogSeverity.Warning, MaxDmcSizeWarning.Format(DPCMSample.MaxSampleSize));
                                    }
                                    var sample = App.Project.CreateDPCMSampleFromDmcData(sampleName, dmcData, filename);
                                    importedSamples.Add(sample);
                                }
                            }

                            App.UndoRedoManager.EndTransaction();
                            RefreshButtons();
                            if (importedSamples.Count != 0)
                                BlinkButton(importedSamples[0]);
                        }
                        App.EndLogTask();
                    }
                }
            };

            if (Platform.IsMobile)
            {
                Platform.StartMobileLoadFileOperationAsync("*/*", (f) => LoadDPCMSampleAction(new[] { f }));
            }
            else
            {
                var filenames = Platform.ShowOpenFileDialog("Open File", "All Sample Files (*.wav;*.dmc;*.fms)|*.wav;*.dmc;*.fms|Wav Files (*.wav)|*.wav|DPCM Sample Files (*.dmc)|*.dmc|FamiStudio Files (*.fms)|*.fms", ref Settings.LastSampleFolder, true);
                LoadDPCMSampleAction(filenames);
            }
        }

        private void AddSong()
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
            App.SelectedSong = App.Project.CreateSong();
            App.UndoRedoManager.EndTransaction();
            RefreshButtons();
            BlinkButton(App.SelectedSong);
        }

        private void AskDeleteSong(Song song)
        {
            Platform.MessageBoxAsync(ParentWindow, AskDeleteSongMessage.Format(song.Name), AskDeleteSongTitle, MessageBoxButtons.YesNo, (r) =>
            {
                if (r == DialogResult.Yes)
                {
                    bool selectNewSong = song == App.SelectedSong;
                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                    App.Project.DeleteSong(song);
                    if (selectNewSong)
                        App.SelectedSong = App.Project.Songs[0];
                    App.UndoRedoManager.EndTransaction();
                    RefreshButtons();
                }
            });
        }

        private void AddInstrument(int expansionType)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            App.SelectedInstrument = App.Project.CreateInstrument(expansionType);
            App.UndoRedoManager.EndTransaction();
            RefreshButtons();
            BlinkButton(App.SelectedInstrument);
        }

        private void AskAddInstrument(int x, int y)
        {
            var instrumentType = ExpansionType.None;

            if (App.Project.NeedsExpansionInstruments)
            {
                if (Platform.IsDesktop)
                {
                    HandleContextMenuInstrumentHeaderButton(x, y, SubButtonType.Add);
                }
                else
                {
                    var activeExpansions = App.Project.GetActiveExpansions();
                    var expNames = new List<string>();

                    var dlg = new PropertyDialog(ParentWindow, AddInstrumentTitle, new Point(left + x, top + y), 260, true);
                    dlg.Properties.AddLabel(null, SelectAudioExpansionLabel.Colon); // 0

                    expNames.Add(ExpansionType.GetLocalizedName(ExpansionType.None, ExpansionType.LocalizationMode.Instrument));
                    dlg.Properties.AddRadioButton(Platform.IsMobile ? SelectAudioExpansionLabel : null, expNames[0], true);

                    for (int i = 1; i < activeExpansions.Length; i++)
                    {
                        if (ExpansionType.NeedsExpansionInstrument(activeExpansions[i]))
                        {
                            var expName = ExpansionType.GetLocalizedName(activeExpansions[i], ExpansionType.LocalizationMode.Instrument);
                            dlg.Properties.AddRadioButton(null, expName, false);
                            expNames.Add(expName);
                        }
                    }

                    dlg.Properties.SetPropertyVisible(0, Platform.IsDesktop);
                    dlg.Properties.Build();

                    dlg.ShowDialogAsync((r) =>
                    {
                        if (r == DialogResult.OK)
                        {
                            for (int i = 0; i < expNames.Count; i++)
                            {
                                if (dlg.Properties.GetPropertyValue<bool>(i + 1))
                                {
                                    instrumentType = ExpansionType.GetValueForLocalizedName(expNames[i]);
                                    break;
                                }
                            }

                            AddInstrument(instrumentType);
                        }
                    });
                }
            }
            else
            {
                AddInstrument(instrumentType);
            }
        }

        private void ToggleExpandInstrument(Instrument inst)
        {
            expandedInstrument = expandedInstrument == inst ? null : inst;
            selectedInstrumentTab = null;
            expandedSample = null;
            RefreshButtons(false);
        }

        private void AskDeleteInstrument(Instrument inst)
        {
            Platform.MessageBoxAsync(ParentWindow, AskDeleteInstrumentMessage.Format(inst.Name), AskDeleteInstrumentTitle, MessageBoxButtons.YesNo, (r) =>
            {
                if (r == DialogResult.Yes)
                {
                    bool selectNewInstrument = inst == App.SelectedInstrument;
                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                    App.Project.DeleteInstrument(inst);
                    if (selectNewInstrument)
                        App.SelectedInstrument = App.Project.Instruments.Count > 0 ? App.Project.Instruments[0] : null;
                    InstrumentDeleted?.Invoke(inst);
                    App.UndoRedoManager.EndTransaction();
                    RefreshButtons();
                }
            });
        }

        private void ClearInstrumentEnvelope(Instrument inst, int envelopeType)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, inst.Id);
            inst.Envelopes[envelopeType].ResetToDefault(envelopeType);
            inst.NotifyEnvelopeChanged(envelopeType, true);
            App.UndoRedoManager.EndTransaction();
            MarkDirty();
        }

        private void AddArpeggio()
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            App.SelectedArpeggio = App.Project.CreateArpeggio();
            App.UndoRedoManager.EndTransaction();
            RefreshButtons();
            BlinkButton(App.SelectedArpeggio);
        }

        private void AskDeleteArpeggio(Arpeggio arpeggio)
        {
            Platform.MessageBoxAsync(ParentWindow, AskDeleteArpeggioMessage.Format(arpeggio.Name), AskDeleteArpeggioTitle, MessageBoxButtons.YesNo, (r) =>
            {
                if (r == DialogResult.Yes)
                {
                    bool selectNewArpeggio = arpeggio == App.SelectedArpeggio;
                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                    App.Project.DeleteArpeggio(arpeggio);
                    if (selectNewArpeggio)
                        App.SelectedArpeggio = App.Project.Arpeggios.Count > 0 ? App.Project.Arpeggios[0] : null;
                    ArpeggioDeleted?.Invoke(arpeggio);
                    App.UndoRedoManager.EndTransaction();
                    RefreshButtons();
                }
            });
        }

        private void SortArpeggios()
        {
            if (!App.Project.AutoSortArpeggios)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                App.Project.SortArpeggios();
                App.UndoRedoManager.EndTransaction();
                RefreshButtons();
            }
        }

        private void HandleContextMenuSortArpeggios(int x, int y)
        {
            App.ShowContextMenu(left + x, top + y, new[]
            {
                new ContextMenuOption(AutoSortArpeggiosContext, null, () =>
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                    App.Project.AutoSortArpeggios = !App.Project.AutoSortArpeggios;
                    App.UndoRedoManager.EndTransaction();
                    RefreshButtons();
                }, () => App.Project.AutoSortArpeggios ? ContextMenuCheckState.Checked : ContextMenuCheckState.Unchecked)
            });
        }

        private void ReloadDPCMSampleSourceData(DPCMSample sample)
        {
            if (!string.IsNullOrEmpty(sample.SourceFilename))
            {
                if (File.Exists(sample.SourceFilename))
                {
                    if (sample.SourceDataIsWav)
                    {
                        var wavData = WaveFile.Load(sample.SourceFilename, out var sampleRate);
                        if (wavData != null)
                        {
                            var maximumSamples = sampleRate * 2;
                            if (wavData.Length > maximumSamples)
                                Array.Resize(ref wavData, maximumSamples);

                            App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamples);
                            sample.SetWavSourceData(wavData, sampleRate, sample.SourceFilename, false);
                            sample.Process();
                            App.UndoRedoManager.EndTransaction();
                        }
                    }
                    else
                    {
                        var dmcData = File.ReadAllBytes(sample.SourceFilename);
                        if (dmcData.Length > DPCMSample.MaxSampleSize)
                            Array.Resize(ref dmcData, DPCMSample.MaxSampleSize);

                        App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamples);
                        sample.SetDmcSourceData(dmcData, sample.SourceFilename, false);
                        sample.Process();
                        App.UndoRedoManager.EndTransaction();
                    }

                    DPCMSampleReloaded?.Invoke(sample);
                }
                else
                {
                    App.DisplayNotification(CantFindSourceFileError.Format(sample.SourceFilename));
                }
            }
        }

        private void ExportDPCMSampleProcessedData(DPCMSample sample)
        {
            var filename = Platform.ShowSaveFileDialog("Save File", "DPCM Samples (*.dmc)|*.dmc", ref Settings.LastSampleFolder);
            if (filename != null)
                File.WriteAllBytes(filename, sample.ProcessedData);
        }

        private void ExportDPCMSampleSourceData(DPCMSample sample)
        {
            if (sample.SourceDataIsWav)
            {
                var filename = Platform.ShowSaveFileDialog("Save File", "Wav file (*.wav)|*.wav", ref Settings.LastSampleFolder);
                if (filename != null)
                    WaveFile.Save(sample.SourceWavData.Samples, filename, sample.SourceWavData.SampleRate, 1);
            }
            else
            {
                var filename = Platform.ShowSaveFileDialog("Save File", "DPCM Samples (*.dmc)|*.dmc", ref Settings.LastSampleFolder);
                if (filename != null)
                    File.WriteAllBytes(filename, sample.SourceDmcData.Data);
            }
        }

        private void SortSamples()
        {
            if (!App.Project.AutoSortSamples)
            {
                App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                App.Project.SortSamples();
                App.UndoRedoManager.EndTransaction();
                RefreshButtons();
            }
        }

        private void HandleContextMenuSortSamples(int x, int y)
        {
            App.ShowContextMenu(left + x, top + y, new[]
            {
                new ContextMenuOption(AutoSortSamplesContext, null, () =>
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                    App.Project.AutoSortSamples = !App.Project.AutoSortSamples;
                    App.UndoRedoManager.EndTransaction();
                    RefreshButtons();
                }, () => App.Project.AutoSortSamples ? ContextMenuCheckState.Checked : ContextMenuCheckState.Unchecked)
            });
        }

        private void AutoAssignSampleBanks()
        {
            var dlg = new PropertyDialog(ParentWindow, AutoAssignBanksTitle, 250, true, true);
            dlg.Properties.AddLabel(null, TargetBankSizeLabel.Colon); // 0
            dlg.Properties.AddDropDownList(null, new[] { "4KB", "8KB", "16KB" }, "4KB", null); // 1
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
                    var bankSize = Utils.ParseIntWithTrailingGarbage(dlg.Properties.GetPropertyValue<string>(1)) * 1024;
                    App.Project.AutoAssignSamplesBanks(bankSize, out _);
                    App.UndoRedoManager.EndTransaction();
                    MarkDirty();
                }
            });
        }

        private void ToggleExpandDPCMSample(DPCMSample sample)
        {
            expandedSample = expandedSample == sample ? null : sample;
            expandedInstrument = null;
            selectedInstrumentTab = null;
            RefreshButtons();
        }

        private void AskDeleteDPCMSample(DPCMSample sample)
        {
            Platform.MessageBoxAsync(ParentWindow, AskDeleteSampleMessage.Format(sample.Name), AskDeleteSampleTitle, MessageBoxButtons.YesNo, (r) =>
            {
                if (r == DialogResult.Yes)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSamples, TransactionFlags.StopAudio);
                    App.Project.DeleteSample(sample);
                    DPCMSampleDeleted?.Invoke(sample);
                    App.UndoRedoManager.EndTransaction();
                    RefreshButtons();
                }
            });
        }

        private bool HandleMouseDownPan(MouseEventArgs e)
        {
            bool middle = e.Middle || (e.Left && ModifierKeys.IsAltDown && Settings.AltLeftForMiddle);

            if (middle)
            {
                mouseLastY = e.Y;
                return true;
            }

            return false;
        }

        private bool HandleMouseDownScrollbar(MouseEventArgs e)
        {
            if (e.Left && needsScrollBar && e.X > contentSizeX && GetScrollBarParams(out var scrollBarPosY, out var scrollBarSizeY))
            {
                if (e.Y < scrollBarPosY)
                {
                    scrollY -= Height;
                    ClampScroll();
                    MarkDirty();
                }
                else if (e.Y > (scrollBarPosY + scrollBarSizeY))
                {
                    scrollY += Height;
                    ClampScroll();
                    MarkDirty();
                }
                else
                {
                    StartCaptureOperation(e.X, e.Y, CaptureOperation.ScrollBar);
                }

                return true;
            }

            return false;
        }

        private bool HandleMouseDownSongProjectSettings(MouseEventArgs e, SubButtonType subButtonType)
        {
            if (e.Left)
            {
                if (subButtonType == SubButtonType.Properties)
                    EditProjectProperties(new Point(e.X, e.Y));
            }

            return true;
        }

        private bool HandleMouseDownSongHeaderButton(MouseEventArgs e, SubButtonType subButtonType)
        {
            if (e.Left)
            {
                if (subButtonType == SubButtonType.Add)
                    AddSong();
                else if (subButtonType == SubButtonType.Load)
                    ImportSongs();
                else if (subButtonType == SubButtonType.Sort)
                    SortSongs();
            }
            else if (e.Right)
            {
                if (subButtonType == SubButtonType.Sort)
                    HandleContextMenuSortSongs(e.X, e.Y);
            }

            return true;
        }

        private bool HandleMouseDownSongButton(MouseEventArgs e, Button button, int buttonIdx, SubButtonType subButtonType)
        {
            if (e.Left && subButtonType == SubButtonType.Properties)
            {
                EditSongProperties(new Point(e.X, e.Y), button.song);
            }
            else if (e.Left && subButtonType == SubButtonType.Max)
            {
                App.SelectedSong = button.song;
                StartCaptureOperation(e.X, e.Y, CaptureOperation.DragSong, buttonIdx);
                draggedSong = button.song;
            }

            return true;
        }

        private bool HandleMouseDownInstrumentHeaderButton(MouseEventArgs e, SubButtonType subButtonType)
        {
            if (e.Left)
            {
                if (subButtonType == SubButtonType.Add)
                    AskAddInstrument(e.X, e.Y);
                else if (subButtonType == SubButtonType.Load)
                    ImportInstruments();
                else if (subButtonType == SubButtonType.Sort)
                    SortInstruments();
            }
            else if (e.Right)
            {
                if (subButtonType == SubButtonType.Sort)
                    HandleContextMenuSortInstruments(e.X, e.Y);
            }

            return true;
        }

        private bool HandleMouseDownInstrumentButton(MouseEventArgs e, Button button, SubButtonType subButtonType, int buttonIdx, int buttonRelX, int buttonRelY)
        {
            if (e.Left)
            {
                if (subButtonType == SubButtonType.Expand)
                {
                    ToggleExpandInstrument(button.instrument);
                    return true;
                }
                else if (subButtonType == SubButtonType.DPCM)
                {
                    App.StartEditDPCMMapping(button.instrument);
                    return true;
                }
                else if (subButtonType == SubButtonType.Properties)
                {
                    EditInstrumentProperties(new Point(e.X, e.Y), button.instrument);
                    return true;
                }

                App.SelectedInstrument = button.instrument;

                if (button.instrument != null)
                {
                    draggedInstrument = button.instrument;

                    if (subButtonType < SubButtonType.EnvelopeMax)
                    {
                        envelopeDragIdx = (int)subButtonType;
                        StartCaptureOperation(e.X, e.Y, CaptureOperation.DragInstrumentEnvelope, buttonIdx, buttonRelX, buttonRelY);
                        App.StartEditInstrument(button.instrument, (int)subButtonType);
                    }
                    else
                    {
                        envelopeDragIdx = -1;
                        StartCaptureOperation(e.X, e.Y, CaptureOperation.DragInstrument, buttonIdx);
                    }
                }
            }

            return true;
        }

        private bool StartMoveSlider(int x, int y, Button button, int buttonIdx)
        {
            App.UndoRedoManager.BeginTransaction(button.paramScope, button.paramObjectId);
            captureMouseX = x; // Hack, UpdateSliderValue relies on this.

            if (button.param.IsEnabled() && UpdateSliderValue(button, x, y, true))
            {
                sliderDragButton = button;
                StartCaptureOperation(x, y, CaptureOperation.MoveSlider, buttonIdx);
                MarkDirty();
                return true;
            }
            else
            {
                App.UndoRedoManager.AbortTransaction();
                return false;
            }
        }

        private bool HandleMouseDownParamSliderButton(MouseEventArgs e, Button button, int buttonIdx)
        {
            if (e.Left)
            {
                if (ClickParamListOrSliderButton(e.X, e.Y, button, buttonIdx, true))
                    return true;

                return StartMoveSlider(e.X, e.Y, button, buttonIdx);
            }

            return false;
        }

        private bool IsPointInCheckbox(int x, int y)
        {
            return x >= contentSizeX - checkBoxPosX;
        }

        private bool IsPointInParamListOrSliderButton(int x, int y, bool left)
        {
            if (left)
                return x > (contentSizeX - sliderPosX) && x < (contentSizeX - sliderPosX + paramButtonSizeX);
            else
                return x > (contentSizeX - sliderPosX + sliderSizeX - paramButtonSizeX) && x < (contentSizeX - sliderPosX + sliderSizeX);
        }

        private void ClickParamCheckbox(int x, int y, Button button)
        {
            if (IsPointInCheckbox(x, y))
            {
                App.UndoRedoManager.BeginTransaction(button.paramScope, button.paramObjectId);
                button.param.SetValue(button.param.GetValue() == 0 ? 1 : 0);
                App.UndoRedoManager.EndTransaction();
                MarkDirty();
            }
        }

        private bool ClickParamListOrSliderButton(int x, int y, Button button, int buttonIdx, bool capture)
        {
            var buttonX = x;
            var leftButton  = IsPointInParamListOrSliderButton(x, y, true);
            var rightButton = IsPointInParamListOrSliderButton(x, y, false);
            var delta = leftButton ? -1 : (rightButton ? 1 : 0);

            if ((leftButton || rightButton) && button.param.IsEnabled())
            {
                App.UndoRedoManager.BeginTransaction(button.paramScope, button.paramObjectId);

                if (capture)
                    StartCaptureOperation(x, y, CaptureOperation.SliderButtons, buttonIdx);
                
                captureButtonSign = rightButton ? 1 : -1;
                captureButtonIdx = buttonIdx;
                UpdateSliderButtons(true, false);
                MarkDirty();

                if (!capture)
                    App.UndoRedoManager.EndTransaction();

                return true;
            }

            return false;
        }

        private int GetTabIndex(int x, int y, Button button)
        {
            var tabWidth = Utils.DivideAndRoundUp(contentSizeX - expandButtonSizeX - paramRightPadX, button.tabNames.Length);
            return Utils.Clamp((x - expandButtonSizeX) / tabWidth, 0, button.tabNames.Length - 1);
        }

        private void ClickParamTabsButton(int x, int y, Button button)
        {
            var tabIndex = GetTabIndex(x, y, button);
            selectedInstrumentTab = button.tabNames[tabIndex];
            RefreshButtons();
        }

        private bool HandleMouseDownParamCheckboxButton(MouseEventArgs e, Button button)
        {
            if (e.Left)
                ClickParamCheckbox(e.X, e.Y, button);

            return true;
        }

        private bool HandleMouseDownParamListButton(MouseEventArgs e, Button button, int buttonIdx, bool capture)
        {
            if (e.Left)
                ClickParamListOrSliderButton(e.X, e.Y, button, buttonIdx, capture);

            return true;
        }

        private bool HandleMouseDownParamTabs(MouseEventArgs e, Button button)
        {
            if (e.Left)
                ClickParamTabsButton(e.X, e.Y, button);

            return true;
        }

        private bool HandleMouseDownArpeggioHeaderButton(MouseEventArgs e, SubButtonType subButtonType)
        {
            if (e.Left)
            {
                if (subButtonType == SubButtonType.Add)
                    AddArpeggio();
                else if (subButtonType == SubButtonType.Sort)
                    SortArpeggios();
            }
            else if (e.Right)
            {
                if (subButtonType == SubButtonType.Sort)
                    HandleContextMenuSortArpeggios(e.X, e.Y);
            }

            return true;
        }

        private bool HandleMouseDownArpeggioButton(MouseEventArgs e, Button button, SubButtonType subButtonType, int buttonIdx, int buttonRelX, int buttonRelY)
        {
            if (e.Left)
            {
                if (subButtonType == SubButtonType.Properties)
                {
                    EditArpeggioProperties(new Point(e.X, e.Y), button.arpeggio);
                    return true;
                }

                App.SelectedArpeggio = button.arpeggio;

                envelopeDragIdx = -1;
                draggedArpeggio = button.arpeggio;

                if (subButtonType < SubButtonType.EnvelopeMax)
                {
                    envelopeDragIdx = (int)subButtonType;
                    StartCaptureOperation(e.X, e.Y, CaptureOperation.DragArpeggioValues, buttonIdx, buttonRelX, buttonRelY);
                    App.StartEditArpeggio(button.arpeggio);
                }
                else
                {
                    StartCaptureOperation(e.X, e.Y, CaptureOperation.DragArpeggio, buttonIdx);
                }
            }

            return true;
        }

        private bool HandleMouseDownDpcmHeaderButton(MouseEventArgs e, SubButtonType subButtonType)
        {
            if (e.Left)
            {
                if (subButtonType == SubButtonType.Load)
                    LoadDPCMSample();
                else if (subButtonType == SubButtonType.Sort)
                    SortSamples();
            }
            else if (e.Right)
            {
                if (subButtonType == SubButtonType.Sort)
                    HandleContextMenuSortSamples(e.X, e.Y);
            }

            return true;
        }

        private bool HandleMouseDownDpcmButton(MouseEventArgs e, Button button, SubButtonType subButtonType, int buttonIdx)
        {
            if (e.Left)
            {
                if (subButtonType == SubButtonType.EditWave)
                {
                    App.StartEditDPCMSample(button.sample);
                }
                else if (subButtonType == SubButtonType.Reload)
                {
                    ReloadDPCMSampleSourceData(button.sample);
                }
                else if (subButtonType == SubButtonType.Play)
                {
                    App.PreviewDPCMSample(button.sample, false);
                }
                else if (subButtonType == SubButtonType.Properties)
                {
                    EditDPCMSampleProperties(new Point(e.X, e.Y), button.sample);
                }
                else if (subButtonType == SubButtonType.Expand)
                {
                    ToggleExpandDPCMSample(button.sample);
                }
                else if (subButtonType == SubButtonType.Max)
                {
                    draggedSample = button.sample;
                    StartCaptureOperation(e.X, e.Y, CaptureOperation.DragSample, buttonIdx);
                    MarkDirty();
                }
            }
            else if (e.Right)
            {
                if (subButtonType == SubButtonType.Play)
                {
                    App.PreviewDPCMSample(button.sample, true);
                }
            }

            return true;
        }

        private bool HandleMouseDownTopTabs(MouseEventArgs e)
        {
            if (topTabSizeY > 0 && e.Y < topTabSizeY)
            {
                selectedTab = e.X < Width / 2 ? TabType.Project : TabType.Registers;
                RefreshButtons();
                return true;
            }

            return false;
        }

        private bool HandleMouseDownButtons(MouseEventArgs e)
        {
            var buttonIdx = GetButtonAtCoord(e.X, e.Y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            if (buttonIdx >= 0)
            {
                var button = buttons[buttonIdx];

                switch (button.type)
                {
                    case ButtonType.ProjectSettings:
                        return HandleMouseDownSongProjectSettings(e, subButtonType);
                    case ButtonType.SongHeader:
                        return HandleMouseDownSongHeaderButton(e, subButtonType);
                    case ButtonType.Song:
                        return HandleMouseDownSongButton(e, button, buttonIdx, subButtonType);
                    case ButtonType.InstrumentHeader:
                        return HandleMouseDownInstrumentHeaderButton(e, subButtonType);
                    case ButtonType.Instrument:
                        return HandleMouseDownInstrumentButton(e, button, subButtonType, buttonIdx, buttonRelX, buttonRelY);
                    case ButtonType.ParamSlider:
                        return HandleMouseDownParamSliderButton(e, button, buttonIdx);
                    case ButtonType.ParamCheckbox:
                        return HandleMouseDownParamCheckboxButton(e, button);
                    case ButtonType.ParamList:
                        return HandleMouseDownParamListButton(e, button, buttonIdx, true);
                    case ButtonType.ParamTabs:
                        return HandleMouseDownParamTabs(e, button);
                    case ButtonType.ArpeggioHeader:
                        return HandleMouseDownArpeggioHeaderButton(e, subButtonType);
                    case ButtonType.Arpeggio:
                        return HandleMouseDownArpeggioButton(e, button, subButtonType, buttonIdx, buttonRelX, buttonRelY);
                    case ButtonType.DpcmHeader:
                        return HandleMouseDownDpcmHeaderButton(e, subButtonType);
                    case ButtonType.Dpcm:
                        return HandleMouseDownDpcmButton(e, button, subButtonType, buttonIdx);
                }

                return true;
            }

            return false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (captureOperation != CaptureOperation.None)
                return;

            if (HandleMouseDownPan(e)) goto Handled;
            if (HandleMouseDownScrollbar(e)) goto Handled;
            if (HandleMouseDownTopTabs(e)) goto Handled;
            if (HandleMouseDownButtons(e)) goto Handled;
            return;

        Handled:
            MarkDirty();
        }

        private bool HandleTouchClickProjectSettingsButton(int x, int y, SubButtonType subButtonType)
        {
            if (subButtonType == SubButtonType.Properties)
                EditProjectProperties(new Point(x, y));

            return true;
        }

        private bool HandleTouchClickSongHeaderButton(int x, int y, SubButtonType subButtonType)
        {
            if (subButtonType == SubButtonType.Add)
                AddSong();
            else if (subButtonType == SubButtonType.Load)
                ImportSongs();
            else if (subButtonType == SubButtonType.Sort)
                SortSongs();

            return true;
        }

        private bool HandleTouchClickInstrumentHeaderButton(int x, int y, SubButtonType subButtonType)
        {
            if (subButtonType == SubButtonType.Add)
                AskAddInstrument(x, y);
            else if (subButtonType == SubButtonType.Load)
                ImportInstruments();
            else if (subButtonType == SubButtonType.Sort)
                SortInstruments();

            return true;
        }

        private bool HandleTouchClickSongButton(int x, int y, Button button, int buttonIdx, SubButtonType subButtonType)
        {
            if (subButtonType == SubButtonType.Properties)
            {
                EditSongProperties(new Point(x, y), button.song);
            }
            else
            {
                App.SelectedSong = button.song;
                if (App.Project.Songs.Count > 1)
                    highlightedButtonIdx = highlightedButtonIdx == buttonIdx ? -1 : buttonIdx;
            }

            return true;
        }

        private bool HandleTouchClickInstrumentButton(int x, int y, Button button, SubButtonType subButtonType, int buttonIdx, int buttonRelX, int buttonRelY)
        {
            if (subButtonType == SubButtonType.Max)
                highlightedButtonIdx = highlightedButtonIdx == buttonIdx ? -1 : buttonIdx;

            if (subButtonType == SubButtonType.Properties)
            {
                if (button.instrument != null)
                    EditInstrumentProperties(new Point(x, y), button.instrument);
            }
            else
            {
                App.SelectedInstrument = button.instrument;

                if (subButtonType == SubButtonType.Expand)
                {
                    ToggleExpandInstrument(button.instrument);
                }
                else if (subButtonType == SubButtonType.DPCM)
                {
                    App.StartEditDPCMMapping(button.instrument);
                }
                else if (subButtonType < SubButtonType.EnvelopeMax)
                {
                    App.StartEditInstrument(button.instrument, (int)subButtonType);
                }
            }

            return true;
        }

        private bool HandleTouchClickArpeggioHeaderButton(int x, int y, SubButtonType subButtonType)
        {
            if (subButtonType == SubButtonType.Add)
                AddArpeggio();
            else if (subButtonType == SubButtonType.Sort)
                SortArpeggios();

            return true;
        }

        private bool HandleTouchClickArpeggioButton(int x, int y, Button button, SubButtonType subButtonType, int buttonIdx, int buttonRelX, int buttonRelY)
        {
            if (subButtonType == SubButtonType.Max)
                highlightedButtonIdx = highlightedButtonIdx == buttonIdx ? -1 : buttonIdx;

            if (subButtonType == SubButtonType.Properties)
            {
                EditArpeggioProperties(new Point(x, y), button.arpeggio);
            }
            else
            {
                App.SelectedArpeggio = button.arpeggio;
                if (subButtonType < SubButtonType.EnvelopeMax)
                    App.StartEditArpeggio(button.arpeggio);
            }

            return true;
        }

        private bool HandleTouchClickDpcmHeaderButton(int x, int y, SubButtonType subButtonType)
        {
            if (subButtonType == SubButtonType.Load)
                LoadDPCMSample();
            else if (subButtonType == SubButtonType.Sort)
                SortSamples();

            return true;
        }

        private bool HandleTouchClickDpcmButton(int x, int y, Button button, SubButtonType subButtonType, int buttonIdx)
        {
            if (subButtonType == SubButtonType.Max)
                highlightedButtonIdx = highlightedButtonIdx == buttonIdx ? -1 : buttonIdx;

            if (subButtonType == SubButtonType.Properties)
            {
                EditDPCMSampleProperties(new Point(x, y), button.sample);
            }
            else if (subButtonType == SubButtonType.EditWave)
            {
                App.StartEditDPCMSample(button.sample);
            }
            else if (subButtonType == SubButtonType.Play)
            {
                App.PreviewDPCMSample(button.sample, false);
            }
            else if (subButtonType == SubButtonType.Expand)
            {
                ToggleExpandDPCMSample(button.sample);
            }

            return true;
        }

        private bool HandleTouchClickParamCheckboxButton(int x, int y, Button button)
        {
            ClickParamCheckbox(x, y, button);
            return true;
        }

        private bool HandleTouchClickParamListOrSliderButton(int x, int y, Button button, int buttonIdx)
        {
            // If we just ended a slider button capture op, it means we litterally just 
            // moved our finger up from the button this frame, so we must not increment 
            // again.
            if (lastCaptureOperation != CaptureOperation.SliderButtons)
                ClickParamListOrSliderButton(x, y, button, buttonIdx, false);

            return true;
        }

        private bool HandleTouchClickParamTabsButton(int x, int y, Button button)
        {
            ClickParamTabsButton(x, y, button);
            return true;
        }

        private bool HandleTouchClickButtons(int x, int y)
        {
            var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            if (buttonIdx >= 0)
            {
                var button = buttons[buttonIdx];

                switch (button.type)
                {
                    case ButtonType.ProjectSettings:
                        return HandleTouchClickProjectSettingsButton(x, y, subButtonType);
                    case ButtonType.SongHeader:
                        return HandleTouchClickSongHeaderButton(x, y, subButtonType);
                    case ButtonType.Song:
                        return HandleTouchClickSongButton(x, y, button, buttonIdx, subButtonType);
                    case ButtonType.InstrumentHeader:
                        return HandleTouchClickInstrumentHeaderButton(x, y, subButtonType);
                    case ButtonType.Instrument:
                        return HandleTouchClickInstrumentButton(x, y, button, subButtonType, buttonIdx, buttonRelX, buttonRelY);
                    case ButtonType.ParamCheckbox:
                        return HandleTouchClickParamCheckboxButton(x, y, button);
                    case ButtonType.ParamList:
                    case ButtonType.ParamSlider:
                        return HandleTouchClickParamListOrSliderButton(x, y, button, buttonIdx);
                    case ButtonType.ParamTabs:
                        return HandleTouchClickParamTabsButton(x, y, button);
                    case ButtonType.ArpeggioHeader:
                        return HandleTouchClickArpeggioHeaderButton(x, y, subButtonType);
                    case ButtonType.Arpeggio:
                        return HandleTouchClickArpeggioButton(x, y, button, subButtonType, buttonIdx, buttonRelX, buttonRelY);
                    case ButtonType.DpcmHeader:
                        return HandleTouchClickDpcmHeaderButton(x, y, subButtonType);
                    case ButtonType.Dpcm:
                        return HandleTouchClickDpcmButton(x, y, button, subButtonType, buttonIdx);
                        
                }

                return true;
            }

            return false;
        }

        private bool HandleContextMenuProjectSettings(int x, int y)
        {    
            App.ShowContextMenu(left + x, top + y, new[]
            {
                new ContextMenuOption("MenuProperties", PropertiesProjectContext, () => { EditProjectProperties(new Point(x, y)); })
            });

            return true;
        }

        private void DuplicateSong(Song s)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            var newSong = s.Project.DuplicateSong(s);
            RefreshButtons();
            BlinkButton(newSong);
            App.UndoRedoManager.EndTransaction();
        }

        private void DuplicateInstrument(Instrument inst)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            var newInst = App.Project.DuplicateInstrument(inst);
            RefreshButtons();
            BlinkButton(newInst);
            App.UndoRedoManager.EndTransaction();
        }

        private bool HandleContextMenuSongButton(int x, int y, Button button)
        {
            var menu = new List<ContextMenuOption>();
            if (App.Project.Songs.Count > 1)
                menu.Add(new ContextMenuOption("MenuDelete", DeleteSongContext, () => { AskDeleteSong(button.song); }, ContextMenuSeparator.After));
            menu.Add(new ContextMenuOption("MenuDuplicate", DuplicateContext, () => { DuplicateSong(button.song); }));
            menu.Add(new ContextMenuOption("MenuProperties", PropertiesSongContext, () => { EditSongProperties(new Point(x, y), button.song); }, ContextMenuSeparator.Before));
            App.ShowContextMenu(left + x, top + y, menu.ToArray());
            return true;
        }

        private bool HandleContextMenuSongHeaderButton(int x, int y, Button button, SubButtonType subButtonType)
        {
            if (subButtonType == SubButtonType.Sort)
            { 
                HandleContextMenuSortSongs(x, y);
                return true;
            }

            return false;
        }

        private bool HandleContextMenuSampleHeaderButton(int x, int y, Button button, SubButtonType subButtonType)
        {
            if (subButtonType == SubButtonType.Sort)
            {
                HandleContextMenuSortSamples(x, y);
                return true;
            }

            return false;
        }

        private bool HandleContextMenuArpeggioHeaderButton(int x, int y, Button button, SubButtonType subButtonType)
        {
            if (subButtonType == SubButtonType.Sort)
            {
                HandleContextMenuSortArpeggios(x, y);
                return true;
            }

            return false;
        }

        private void AskReplaceInstrument(Instrument inst)
        {
            var instrumentNames  = new List<string>();
            var instrumentColors = new List<Color>();

            foreach (var i in App.Project.Instruments)
            {
                if (i.Expansion == inst.Expansion && i != inst)
                {
                    instrumentNames.Add(i.Name);
                    instrumentColors.Add(i.Color);
                } 
            }

            if (instrumentNames.Count > 0)
            {                               
                var dlg = new PropertyDialog(ParentWindow, AskReplaceInstrumentTitle, 250, true, true);
                dlg.Properties.AddLabel(null, AskReplaceInstrumentMessage.Format(inst.Name), true); // 0
                dlg.Properties.AddRadioButtonList(null, instrumentNames.ToArray(), 0, null, 12); // 1
                dlg.Properties.Build();

                for (int i = 0; i < instrumentColors.Count; i++)
                    dlg.Properties.SetRowColor(1, i, instrumentColors[i]);

                dlg.ShowDialogAsync((r) =>
                {
                    if (r == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                        App.Project.ReplaceInstrument(inst, App.Project.GetInstrument(instrumentNames[dlg.Properties.GetSelectedIndex(1)]));
                        App.UndoRedoManager.EndTransaction();
                        RefreshButtons();
                        InstrumentReplaced?.Invoke(inst);
                    }
                });
            }
        }

        private void DuplicateConvertInstrument(Instrument instrument, int exp)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
            var newInstrument = App.Project.DuplicateConvertInstrument(instrument, exp);
            App.UndoRedoManager.EndTransaction();
            RefreshButtons();
            BlinkButton(newInstrument);
        }

        private void LoadN163FdsResampleWavFile(Instrument inst)
        {
            Action<string> LoadWavFileAction = (filename) =>
            {
                if (filename != null)
                {
                    var wav = WaveFile.Load(filename, out _);
                    if (wav != null)
                    {
                        App.BeginLogTask();
                        App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, inst.Id);

                        if (wav.Length > Instrument.MaxResampleWavSamples)
                        {
                            Array.Resize(ref wav, Instrument.MaxResampleWavSamples);
                            Log.LogMessage(LogSeverity.Warning, MaxWavN163Duration.Format(Instrument.MaxResampleWavSamples));
                        }

                        if (inst.IsN163)
                            inst.SetN163ResampleWaveData(wav);
                        else
                            inst.SetFdsResampleWaveData(wav);

                        App.UndoRedoManager.EndTransaction();
                        App.EndLogTask();

                        MarkDirty();
                    }
                }
            };

            if (Platform.IsMobile)
            {
                Platform.StartMobileLoadFileOperationAsync("*/*", (f) => LoadWavFileAction(f));
            }
            else
            {
                var filename = Platform.ShowOpenFileDialog("Open File", "Wav Files (*.wav)|*.wav", ref Settings.LastSampleFolder);
                LoadWavFileAction(filename);
            }
        }

        private void ClearN163FdsResampleWavData(Instrument inst)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, inst.Id);

            if (inst.IsN163)
                inst.DeleteN163ResampleWavData();
            else
                inst.DeleteFdsResampleWavData();

            App.UndoRedoManager.EndTransaction();
            MarkDirty();
        }

        private void CopyRegisterValues(Instrument inst)
        {
            Debug.Assert(inst.IsVrc7 || inst.IsEpsm);

            var regs = inst.IsVrc7 ? inst.Vrc7PatchRegs : inst.EpsmPatchRegs;            
            var str = $"{regs[0]:x2}";
            for (var i = 1; i < regs.Length; i++)
                str += $" {regs[i]:x2}";

            Platform.SetClipboardString(str);
        }

        private void PasteRegisterValues(Instrument inst)
        {
            var str = Platform.GetClipboardString();

            if (string.IsNullOrEmpty(str))
            {
                App.DisplayNotification(ClipboardNoValidTextError);
                return;
            }

            var splits = str.Split(new[] { ' ' });
            var regs = inst.IsVrc7 ? inst.Vrc7PatchRegs : inst.EpsmPatchRegs;

            if (splits.Length != regs.Length)
            {
                App.DisplayNotification(ClipboardInvalidNumberRegisters);
                return;
            }

            App.UndoRedoManager.BeginTransaction(TransactionScope.Instrument, inst.Id);

            for (int i = 0; i < splits.Length; i++)
            {
                try { regs[i] = (byte)Convert.ToInt32(splits[i], 16); } catch { }
            }

            if (inst.IsVrc7)
                inst.Vrc7Patch = 0;
            else
                inst.EpsmPatch = 0;

            App.UndoRedoManager.EndTransaction();
        }

        private bool HandleContextMenuInstrumentButton(int x, int y, Button button, SubButtonType subButtonType, int buttonIdx)
        {
            var menu = new List<ContextMenuOption>();
            var inst = button.instrument;
            
            if (inst != null)
            {
                menu.Add(new ContextMenuOption("MenuDelete", DeleteInstrumentContext, () => { AskDeleteInstrument(inst); }, ContextMenuSeparator.After));

                if (subButtonType < SubButtonType.EnvelopeMax)
                {
                    menu.Add(new ContextMenuOption("MenuClearEnvelope", ClearEnvelopeContext, () => { ClearInstrumentEnvelope(inst, (int)subButtonType); }, ContextMenuSeparator.After));
                }

                if (subButtonType == SubButtonType.Max)
                {
                    if (inst.IsN163 || inst.IsFds)
                    {
                        menu.Add(new ContextMenuOption("MenuWave", ResampleWavContext, () => { LoadN163FdsResampleWavFile(inst); }, ContextMenuSeparator.Before)); 

                        if (inst.IsN163 && inst.N163ResampleWaveData != null ||
                            inst.IsFds  && inst.FdsResampleWaveData  != null)
                        {
                            menu.Add(new ContextMenuOption("MenuTrash", DiscardWavDataContext, () => { ClearN163FdsResampleWavData(inst); }));
                        }
                    }

                    if (Platform.IsDesktop && (inst.IsVrc7 || inst.IsEpsm))
                    {
                        menu.Add(new ContextMenuOption("MenuCopy",  CopyRegisterValueContext, () => { CopyRegisterValues(inst); }, ContextMenuSeparator.Before));
                        menu.Add(new ContextMenuOption("MenuPaste", PasteRegisterValueContext, () => { PasteRegisterValues(inst); }));
                    }

                    menu.Add(new ContextMenuOption("MenuDuplicate", DuplicateContext, () => { DuplicateInstrument(inst); }, ContextMenuSeparator.Before));
                    menu.Add(new ContextMenuOption("MenuReplace", ReplaceWithContext, () => { AskReplaceInstrument(inst); }, ContextMenuSeparator.After));

                    if (App.Project.UsesAnyExpansionAudio)
                    {
                        var activeExpansions = App.Project.GetActiveExpansions();

                        foreach (var exp in activeExpansions)
                        {
                            if (exp != inst.Expansion)
                            {
                                var e = exp;
                                menu.Add(new ContextMenuOption(ExpansionType.Icons[exp], DuplicateConvertContext.Format(ExpansionType.GetLocalizedName(exp, ExpansionType.LocalizationMode.Instrument)), () => { DuplicateConvertInstrument(inst, e); }));
                            }
                        }
                    }
                }

                menu.Add(new ContextMenuOption("MenuProperties", PropertiesInstrumentContext, () => { EditInstrumentProperties(new Point(x, y), inst); }, ContextMenuSeparator.Before));
            }

            if (menu.Count > 0)
                App.ShowContextMenu(left + x, top + y, menu.ToArray());

            return true;
        }
        
        private bool HandleContextMenuInstrumentHeaderButton(int x, int y, SubButtonType subButtonType)
        {
            if (App.Project.NeedsExpansionInstruments && subButtonType == SubButtonType.Add && Platform.IsDesktop)
            {
                var activeExpansions = App.Project.GetActiveExpansions();

                List<ContextMenuOption> options = new List<ContextMenuOption>();
                options.Add(new ContextMenuOption(ExpansionType.Icons[0], AddRegularInstrumentContext, () => { AddInstrument(ExpansionType.None); }));

                for (int i = 1; i < activeExpansions.Length; i++)
                {
                    if (ExpansionType.NeedsExpansionInstrument(activeExpansions[i]))
                    {
                        var j = i; // Important, copy for lambda.
                        var expName = ExpansionType.GetLocalizedName(activeExpansions[i], ExpansionType.LocalizationMode.Instrument);
                        options.Add(new ContextMenuOption(ExpansionType.Icons[activeExpansions[i]], AddExpInstrumentContext.Format(expName), () => { AddInstrument(activeExpansions[j]); }));
                    }
                }

                App.ShowContextMenu(left + x, top + y, options.ToArray());
                return true;
            }
            else if (Platform.IsMobile && subButtonType == SubButtonType.Sort)
            {
                HandleContextMenuSortInstruments(x, y);
                return true;
            }

            return false;
        }

        private void DuplicateArpeggio(Arpeggio arp)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
            var newArp = App.Project.DuplicateArpeggio(arp);
            RefreshButtons();
            BlinkButton(newArp);
            App.UndoRedoManager.EndTransaction();
        }

        private void AskReplaceArpeggio(Arpeggio arp)
        {
            var arpeggioNames  = new List<string>();
            var arpeggioColors = new List<Color>();

            foreach (var a in App.Project.Arpeggios)
            {
                if (a != arp)
                {
                    arpeggioNames.Add(a.Name);
                    arpeggioColors.Add(a.Color);
                }
            }

            if (arpeggioNames.Count > 0)
            {
                var dlg = new PropertyDialog(ParentWindow, AskReplaceArpeggioTitle, 250, true, true);
                dlg.Properties.AddLabel(null, AskReplaceArpeggioMessage.Format(arp.Name), true); // 0
                dlg.Properties.AddRadioButtonList(null, arpeggioNames.ToArray(), 0, null, 12); // 1
                dlg.Properties.Build();

                for (int i = 0; i < arpeggioColors.Count; i++)
                    dlg.Properties.SetRowColor(1, i, arpeggioColors[i]);

                dlg.ShowDialogAsync((r) =>
                {
                    if (r == DialogResult.OK)
                    {
                        App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);
                        App.Project.ReplaceArpeggio(arp, App.Project.GetArpeggio(arpeggioNames[dlg.Properties.GetSelectedIndex(1)]));
                        App.UndoRedoManager.EndTransaction();
                        RefreshButtons();
                        InstrumentReplaced?.Invoke(null);
                    }
                });
            }
        }

        private bool HandleContextMenuArpeggioButton(int x, int y, Button button)
        {
            var menu = new List<ContextMenuOption>();
            if (button.arpeggio != null)
            {
                menu.Add(new ContextMenuOption("MenuDelete", DeleteArpeggioContext, () => { AskDeleteArpeggio(button.arpeggio); }, ContextMenuSeparator.After));
                menu.Add(new ContextMenuOption("MenuDuplicate", DuplicateContext, () => { DuplicateArpeggio(button.arpeggio); }));
                menu.Add(new ContextMenuOption("MenuReplace", ReplaceWithContext, () => { AskReplaceArpeggio(button.arpeggio); }));
                menu.Add(new ContextMenuOption("MenuProperties", PropertiesArpeggioContext, () => { EditArpeggioProperties(new Point(x, y), button.arpeggio); }, ContextMenuSeparator.Before));
            }
            if (menu.Count > 0)
                App.ShowContextMenu(left + x, top + y, menu.ToArray());
            return true;
        }

        private void DeleteDpcmSourceWavData(DPCMSample sample)
        {
            App.UndoRedoManager.BeginTransaction(TransactionScope.Project);
            sample.PermanentlyApplyAllProcessing();
            App.UndoRedoManager.EndTransaction();
        }

        private bool HandleContextMenuDpcmButton(int x, int y, Button button, SubButtonType subButtonType, int buttonIdx)
        {
            if (subButtonType != SubButtonType.Max)
                return true;

            var menu = new List<ContextMenuOption>();

            menu.Add(new ContextMenuOption("MenuDelete", DeleteSampleContext, () => { AskDeleteDPCMSample(button.sample); }, ContextMenuSeparator.After));

            if (Platform.IsDesktop)
            {
                menu.Add(new ContextMenuOption("MenuSave", ExportProcessedDmcDataContext, () => { ExportDPCMSampleProcessedData(button.sample); }));
                menu.Add(new ContextMenuOption("MenuSave", ExportSourceDataContext, () => { ExportDPCMSampleSourceData(button.sample); }));
            }

            if (button.sample.SourceDataIsWav)
            {
                menu.Add(new ContextMenuOption("MenuTrash", DiscardSourceWavDataContext, DiscardSourceWavDataTooltip, () => { DeleteDpcmSourceWavData(button.sample); }));
            }

            menu.Add(new ContextMenuOption("MenuBankAssign", AutoAssignBanksContext, () => { AutoAssignSampleBanks(); }, ContextMenuSeparator.Before));
            menu.Add(new ContextMenuOption("MenuProperties", PropertiesSamplesContext, () => { EditDPCMSampleProperties(new Point(x, y), button.sample); }, ContextMenuSeparator.Before));

            App.ShowContextMenu(left + x, top + y, menu.ToArray());

            return true;
        }

        private void ResetParamButtonDefaultValue(Button button)
        {
            App.UndoRedoManager.BeginTransaction(button.paramScope, button.paramObjectId);
            button.param.SetValue(button.param.DefaultValue);
            App.UndoRedoManager.EndTransaction();
            MarkDirty();
        }

        private bool HandleContextMenuParamButton(int x, int y, Button button)
        {
            if (button.param.IsEnabled())
            {
                App.ShowContextMenu(left + x, top + y, new[]
                {
                    new ContextMenuOption("MenuReset", ResetDefaultValueContext, () => { ResetParamButtonDefaultValue(button); })
                });
            }

            return true;
        }

        private bool HandleContextMenuButtons(int x, int y)
        {
            var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            if (buttonIdx >= 0)
            {
                var button = buttons[buttonIdx];

                switch (button.type)
                {
                    case ButtonType.ProjectSettings:
                        return HandleContextMenuProjectSettings(x, y);
                    case ButtonType.Song:
                        return HandleContextMenuSongButton(x, y, button);
                    case ButtonType.SongHeader:
                        return HandleContextMenuSongHeaderButton(x, y, button, subButtonType);
                    case ButtonType.Instrument:
                        return HandleContextMenuInstrumentButton(x, y, button, subButtonType, buttonIdx);
                    case ButtonType.InstrumentHeader:
                        return HandleContextMenuInstrumentHeaderButton(x, y, subButtonType);
                    case ButtonType.ParamSlider:
                    case ButtonType.ParamCheckbox:
                    case ButtonType.ParamList:
                        return HandleContextMenuParamButton(x, y, button);
                    case ButtonType.Arpeggio:
                        return HandleContextMenuArpeggioButton(x, y, button);
                    case ButtonType.ArpeggioHeader:
                        return HandleContextMenuArpeggioHeaderButton(x, y, button, subButtonType);
                    case ButtonType.Dpcm:
                        return HandleContextMenuDpcmButton(x, y, button, subButtonType, buttonIdx);
                    case ButtonType.DpcmHeader:
                        return HandleContextMenuSampleHeaderButton(x, y, button, subButtonType);
                }

                return true;
            }

            return false;
        }

        private bool HandleTouchLongPressButtons(int x, int y)
        {
            return HandleContextMenuButtons(x, y);
        }

        private bool HandleTouchDownParamSliderButton(int x, int y)
        {
            var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            if (buttonIdx >= 0)
            {
                var button = buttons[buttonIdx];

                if (button.type == ButtonType.ParamSlider)
                {
                    if (ClickParamListOrSliderButton(x, y, button, buttonIdx, true))
                        return true;

                    return StartMoveSlider(x, y, buttons[buttonIdx], buttonIdx);
                }
            }

            return false;
        }

        private bool HandleTouchDownParamListButton(int x, int y)
        {
            var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            if (buttonIdx >= 0)
            {
                var button = buttons[buttonIdx];

                if (button.type == ButtonType.ParamList && ClickParamListOrSliderButton(x, y, button, buttonIdx, true))
                    return true;
            }

            return false;
        }

        private bool IsPointInButtonIcon(Button button, int buttonRelX, int buttonRelY)
        {
            var iconSize = DpiScaling.ScaleCustom(bmpEnvelopes[0].ElementSize.Width, bitmapScale);
            var iconRelX = buttonRelX - (buttonIconPosX + (ShowExpandButtons() ? expandButtonPosX + expandButtonSizeX : 0));
            var iconRelY = buttonRelY - buttonIconPosY;

            if (iconRelX < 0 || iconRelX > iconSize ||
                iconRelY < 0 || iconRelY > iconSize)
            {
                return false;
            }

            return true;
        }

        private bool HandleTouchDownDragInstrument(int x, int y)
        {
            var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            if (buttonIdx >= 0)
            {
                var button = buttons[buttonIdx];
                if (button.instrument != null && buttonIdx == highlightedButtonIdx)
                {
                    draggedInstrument = button.instrument;

                    if (subButtonType < SubButtonType.EnvelopeMax)
                    {
                        envelopeDragIdx = (int)subButtonType;
                        StartCaptureOperation(x, y, CaptureOperation.DragInstrumentEnvelope, buttonIdx, buttonRelX, buttonRelY);
                    }
                    else
                    {
                        envelopeDragIdx = -1;
                        StartCaptureOperation(x, y, CaptureOperation.DragInstrument, buttonIdx);
                    }

                    return true;
                }
            }

            return false;
        }

        private bool HandleTouchDownDragSong(int x, int y)
        {
            var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            if (buttonIdx >= 0)
            {
                var button = buttons[buttonIdx];
                if (button.song != null && buttonIdx == highlightedButtonIdx && subButtonType == SubButtonType.Max && IsPointInButtonIcon(button, buttonRelX, buttonRelY))
                {
                    App.SelectedSong = button.song;
                    StartCaptureOperation(x, y, CaptureOperation.DragSong, buttonIdx);
                    draggedSong = button.song;
                    return true;
                }
            }

            return false;
        }

        private bool HandleTouchDownDragDPCMSample(int x, int y)
        {
            var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            if (buttonIdx >= 0)
            {
                var button = buttons[buttonIdx];
                if (button.sample != null && buttonIdx == highlightedButtonIdx && subButtonType == SubButtonType.Max && IsPointInButtonIcon(button, buttonRelX, buttonRelY))
                {
                    StartCaptureOperation(x, y, CaptureOperation.DragSample, buttonIdx);
                    draggedSample = button.sample;
                    return true;
                }
            }

            return false;
        }

        private bool HandleTouchDownDragDPCMArpeggio(int x, int y)
        {
            var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            if (buttonIdx >= 0)
            {
                var button = buttons[buttonIdx];
                if (button.arpeggio != null && buttonIdx == highlightedButtonIdx && subButtonType == SubButtonType.Max && IsPointInButtonIcon(button, buttonRelX, buttonRelY))
                {
                    StartCaptureOperation(x, y, CaptureOperation.DragArpeggio, buttonIdx);
                    draggedArpeggio = button.arpeggio;
                    return true;
                }
            }

            return false;
        }

        private bool HandleTouchDownPan(int x, int y)
        {
            StartCaptureOperation(x, y, CaptureOperation.MobilePan);
            return true;
        }

        protected override void OnTouchDown(int x, int y)
        {
            flingVelY = 0;

            if (HandleTouchDownParamSliderButton(x, y)) goto Handled;
            if (HandleTouchDownParamListButton(x, y)) goto Handled;
            if (HandleTouchDownDragSong(x, y)) goto Handled;
            if (HandleTouchDownDragInstrument(x, y)) goto Handled;
            if (HandleTouchDownDragDPCMSample(x, y)) goto Handled;
            if (HandleTouchDownDragDPCMArpeggio(x, y)) goto Handled;
            if (HandleTouchDownPan(x, y)) goto Handled;
            return;

         Handled:
            MarkDirty();
        }

        private bool HandleTouchClickTopTabs(int x, int y)
        {
            if (topTabSizeY > 0 && y < topTabSizeY)
            {
                selectedTab = x < Width / 2 ? TabType.Project : TabType.Registers;
                RefreshButtons();
                return true;
            }

            return false;
        }

        protected override void OnTouchClick(int x, int y)
        {
            if (captureOperation != CaptureOperation.None)
                return;

            if (HandleTouchClickTopTabs(x, y)) goto Handled;
            if (HandleTouchClickButtons(x, y)) goto Handled;

            return;

        Handled:
            MarkDirty();
        }

        private bool HandleTouchDoubleClickParamListButton(int x, int y, Button button, int buttonIdx)
        {
            return ClickParamListOrSliderButton(x, y, button, buttonIdx, false);
        }

        private bool HandleTouchDoubleClickButtons(int x, int y)
        {
            var buttonIdx = GetButtonAtCoord(x, y, out var subButtonType, out var buttonRelX, out var buttonRelY);

            if (buttonIdx >= 0)
            {
                var button = buttons[buttonIdx];

                switch (button.type)
                {
                    case ButtonType.ParamList:
                    case ButtonType.ParamSlider:
                        return HandleTouchDoubleClickParamListButton(x, y, button, buttonIdx);
                }

                return true;
            }

            return false;
        }

        protected override void OnTouchDoubleClick(int x, int y)
        {
            if (HandleTouchDoubleClickButtons(x, y)) goto Handled;
            return;
            Handled:
            MarkDirty();
        }

        protected override void OnTouchLongPress(int x, int y)
        {
            if (captureOperation == CaptureOperation.SliderButtons)
            {
                return;
            }

            AbortCaptureOperation();

            if (HandleTouchLongPressButtons(x, y)) goto Handled;

            return;

        Handled:
            MarkDirty();
        }

        protected override void OnTouchMove(int x, int y)
        {
            UpdateCursor();
            UpdateCaptureOperation(x, y);

            mouseLastX = x;
            mouseLastY = y;
        }

        protected override void OnTouchUp(int x, int y)
        {
            EndCaptureOperation(x, y);
        }

        protected override void OnTouchFling(int x, int y, float velX, float velY)
        {
            if (canFling)
            {
                EndCaptureOperation(x, y);
                flingVelY = velY;
            }
        }

        private void TickFling(float delta)
        {
            if (flingVelY != 0.0f)
            {
                var deltaPixel = (int)Math.Round(flingVelY * delta);
                if (deltaPixel != 0 && DoScroll(deltaPixel))
                    flingVelY *= (float)Math.Exp(delta * -4.5f);
                else
                    flingVelY = 0.0f;
            }
        }

        private void TickBlink(float delta)
        {
            if (blinkTimer != 0.0f)
            {
                blinkTimer = MathF.Max(0.0f, blinkTimer - delta);
                if (blinkTimer == 0.0f)
                    blinkObject = null;
                MarkDirty();
            }
        }

        public override void Tick(float delta)
        {
            TickFling(delta);
            TickBlink(delta);
            UpdateCaptureOperation(mouseLastX, mouseLastY, true, delta);
        }

        private void EditProjectProperties(Point pt)
        {
            var project = App.Project;

            var numExpansions = ExpansionType.End - ExpansionType.Start + 1;
            var expNames = new string[numExpansions];
            var expBools = new bool[numExpansions];
            for (int i = ExpansionType.Start; i <= ExpansionType.End; i++)
            {
                expNames[i - ExpansionType.Start] = ExpansionType.GetLocalizedName(i);
                expBools[i - ExpansionType.Start] = project.UsesExpansionAudio(i);
            }

            var dlg = new PropertyDialog(ParentWindow, ProjectPropertiesTitle, new Point(left + pt.X, top + pt.Y), 360, true);
            dlg.Properties.ShowWarnings = true;
            dlg.Properties.AddTextBox(ProjectTitleLabel.Colon, project.Name, 31); // 0
            dlg.Properties.AddTextBox(ProjectAuthorLabel.Colon, project.Author, 31); // 1
            dlg.Properties.AddTextBox(ProjectCopyrightLabel.Colon, project.Copyright, 31); // 2
            dlg.Properties.AddDropDownList(ProjectTempoModeLabel.Colon, TempoType.Names, TempoType.Names[project.TempoMode], ProjectTempoModeTooltip); // 3
            dlg.Properties.AddDropDownList(ProjectMachineLabel.Colon, MachineType.NamesNoDual, MachineType.NamesNoDual[project.PalMode ? MachineType.PAL : MachineType.NTSC], ProjectAuthoringMachineTooltip); // 4
            dlg.Properties.AddNumericUpDown(ProjectN163ChannelsLabel.Colon, project.ExpansionNumN163Channels, 1, 8, 1, ProjectExpansionNumChannelsTooltip); // 5 (Namco)
            dlg.Properties.AddCheckBoxList(ProjectExpansionLabel.Colon, expNames, expBools, ProjectExpansionAudioTooltip); // 6
            dlg.Properties.SetPropertyEnabled(4, project.UsesFamiStudioTempo && !project.UsesAnyExpansionAudio);
            dlg.Properties.SetPropertyEnabled(5, project.UsesExpansionAudio(ExpansionType.N163));
            dlg.Properties.PropertyChanged += ProjectProperties_PropertyChanged;
            UpdateProjectPropertiesWarnings(dlg.Properties);
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    var selectedExpansions = dlg.Properties.GetPropertyValue<bool[]>(6);
                    var expansionMask = 0;
                    var expansionRemoved = false;

                    for (int i = 0; i < selectedExpansions.Length; i++)
                    {
                        var selected = selectedExpansions[i];
                        expansionMask |= (selected ? 1 : 0) << i;

                        if (project.UsesExpansionAudio(i + ExpansionType.Start) && !selected)
                            expansionRemoved = true;
                    }

                    var tempoMode = TempoType.GetValueForName(dlg.Properties.GetPropertyValue<string>(3));
                    var palAuthoring = MachineType.GetValueForName(dlg.Properties.GetPropertyValue<string>(4)) == 1;
                    var numChannels = dlg.Properties.GetPropertyValue<int>(5);

                    var changedTempoMode = tempoMode != project.TempoMode;
                    var changedExpansion = expansionMask != project.ExpansionAudioMask;
                    var changedNumChannels = numChannels != project.ExpansionNumN163Channels;
                    var changedAuthoringMachine = palAuthoring != project.PalMode;

                    var transFlags = TransactionFlags.None;

                    if (changedAuthoringMachine || changedExpansion || changedNumChannels)
                        transFlags = TransactionFlags.ReinitializeAudio;
                    else if (changedTempoMode)
                        transFlags = TransactionFlags.StopAudio;

                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, transFlags);

                    project.Name = dlg.Properties.GetPropertyValue<string>(0);
                    project.Author = dlg.Properties.GetPropertyValue<string>(1);
                    project.Copyright = dlg.Properties.GetPropertyValue<string>(2);

                    if (changedExpansion || changedNumChannels)
                    {
                        var numExpansionsSelected = 0;

                        for (int i = 0; i < selectedExpansions.Length; i++)
                        {
                            if (selectedExpansions[i])
                                numExpansionsSelected++;
                        }

                        App.SelectedInstrument = project.Instruments.Count > 0 ? project.Instruments[0] : null;
                        project.SetExpansionAudioMask(expansionMask, numChannels);
                        ProjectModified?.Invoke();
                        Reset();
                    }

                    if (changedTempoMode)
                    {
                        if (tempoMode == TempoType.FamiStudio)
                        {
                            if (!project.AreSongsEmpty && Platform.IsDesktop)
                                Platform.MessageBox(ParentWindow, ProjectConvertToFamiTrackerMessage, ProjectChangeTempoModeTitle, MessageBoxButtons.OK);
                            project.ConvertToFamiStudioTempo();
                        }
                        else if (tempoMode == TempoType.FamiTracker)
                        {
                            if (!project.AreSongsEmpty && Platform.IsDesktop)
                                Platform.MessageBox(ParentWindow, ProjectConvertToFamiStudioMessage, ProjectChangeTempoModeTitle, MessageBoxButtons.OK);
                            project.ConvertToFamiTrackerTempo(project.AreSongsEmpty);
                        }

                        ProjectModified?.Invoke();
                        Reset();
                    }

                    if (changedAuthoringMachine && project.UsesFamiStudioTempo)
                    {
                        project.PalMode = palAuthoring;
                        App.PalPlayback = palAuthoring;
                    }

                    var toast = (string)null;

                    if (expansionRemoved)
                        toast += ProjectExpansionRemovedMessage + "\n";
                    if (changedNumChannels)
                        toast += ProjectChangedN163ChannelMessage;

                    if (!string.IsNullOrEmpty(toast))
                        Platform.ShowToast(window, toast, true);

                    App.UndoRedoManager.EndTransaction();
                    RefreshButtons();
                }
            });
        }

        private void UpdateProjectPropertiesWarnings(PropertyPage props)
        {
            var selectedExpansions = props.GetPropertyValue<bool[]>(6);
            var numExpansionsSelected = 0;

            for (int i = 0; i < selectedExpansions.Length; i++)
            {
                if (selectedExpansions[i])
                    numExpansionsSelected++;
            }

            if (numExpansionsSelected > 1)
                props.SetPropertyWarning(6, CommentType.Warning, ProjectMultipleExpansionsROMWarning);
            else
                props.SetPropertyWarning(6, CommentType.Good, "");
        }

        private void ProjectProperties_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            var selectedExpansions = props.GetPropertyValue<bool[]>(6);
            var anyExpansionsSelected = false;
            var n163Selected = false;

            for (int i = 0; i < selectedExpansions.Length; i++)
            {
                if (selectedExpansions[i])
                {
                    if (i + ExpansionType.Start == ExpansionType.N163)
                        n163Selected = true;
                    anyExpansionsSelected = true;
                }
            }

            if (propIdx == 6) // Expansion
            {
                props.SetPropertyEnabled(5, n163Selected);
                props.SetPropertyEnabled(4, props.GetPropertyValue<string>(3) == "FamiStudio" && !anyExpansionsSelected);

                if (anyExpansionsSelected)
                    props.SetDropDownListIndex(4, 0);
                else
                    props.SetDropDownListIndex(4, App.Project.PalMode ? 1 : 0);
            }
            else if (propIdx == 3) // Tempo Mode
            {
                props.SetPropertyEnabled(4, (string)value == TempoType.Names[TempoType.FamiStudio] && !anyExpansionsSelected);
            }

            UpdateProjectPropertiesWarnings(props);
        }

        private void EditSongProperties(Point pt, Song song)
        {
            var dlg = new PropertyDialog(ParentWindow, SongPropertiesTitle, new Point(left + pt.X, top + pt.Y), 320, true); 

            var tempoProperties = new TempoProperties(dlg.Properties, song);

            dlg.Properties.AddColoredTextBox(song.Name, song.Color); // 0
            dlg.Properties.AddColorPicker(song.Color); // 1
            dlg.Properties.AddNumericUpDown(SongLengthLabel.Colon, song.Length, 1, Song.MaxLength, 1, SongLengthTooltip); // 2
            tempoProperties.AddProperties();
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples, TransactionFlags.StopAudio);
                    App.SeekSong(0);

                    var newName = dlg.Properties.GetPropertyValue<string>(0).Trim();

                    if (App.Project.RenameSong(song, newName))
                    {
                        song.Color = dlg.Properties.GetPropertyValue<Color>(1);
                        song.SetLength(dlg.Properties.GetPropertyValue<int>(2));

                        tempoProperties.ApplyAsync(ParentWindow, false, () =>
                        {
                            SongModified?.Invoke(song);
                            App.UndoRedoManager.EndTransaction();
                            RefreshButtons(false);
                        });
                    }
                    else
                    {
                        App.UndoRedoManager.AbortTransaction();
                        App.DisplayNotification(RenameSongError, true);
                        MarkDirty();
                    }
                }
            });
        }

        private void EditInstrumentProperties(Point pt, Instrument instrument)
        {
            var dlg = new PropertyDialog(ParentWindow, InstrumentPropertiesTitle, new Point(left + pt.X, top + pt.Y), 240, true, pt.Y > Height / 2);
            dlg.Properties.AddColoredTextBox(instrument.Name, instrument.Color); // 0
            dlg.Properties.AddColorPicker(instrument.Color); // 1
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    var newName = dlg.Properties.GetPropertyValue<string>(0).Trim();

                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);

                    if (App.Project.RenameInstrument(instrument, newName))
                    {
                        instrument.Color = dlg.Properties.GetPropertyValue<Color>(1);
                        InstrumentColorChanged?.Invoke(instrument);
                        RefreshButtons();
                        App.UndoRedoManager.EndTransaction();
                    }
                    else
                    {
                        App.UndoRedoManager.AbortTransaction();
                        App.DisplayNotification(RenameInstrumentError, true);
                    }
                }
            });
        }

        private void EditArpeggioProperties(Point pt, Arpeggio arpeggio)
        {
            var dlg = new PropertyDialog(ParentWindow, ArpeggioPropertiesTitle, new Point(left + pt.X, top + pt.Y), 240, true, pt.Y > Height / 2);
            dlg.Properties.AddColoredTextBox(arpeggio.Name, arpeggio.Color); // 0
            dlg.Properties.AddColorPicker(arpeggio.Color); // 1
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    var newName = dlg.Properties.GetPropertyValue<string>(0).Trim();

                    App.UndoRedoManager.BeginTransaction(TransactionScope.ProjectNoDPCMSamples);

                    if (App.Project.RenameArpeggio(arpeggio, newName))
                    {
                        arpeggio.Color = dlg.Properties.GetPropertyValue<Color>(1);
                        ArpeggioColorChanged?.Invoke(arpeggio);
                        RefreshButtons();
                        App.UndoRedoManager.EndTransaction();
                    }
                    else
                    {
                        App.UndoRedoManager.AbortTransaction();
                        App.DisplayNotification(RenameArpeggioError, true);
                    }
                }
            });
        }

        private void EditDPCMSampleProperties(Point pt, DPCMSample sample)
        {
            var dlg = new PropertyDialog(ParentWindow, SamplePropertiesTitle, new Point(left + pt.X, top + pt.Y), 240, true, pt.Y > Height / 2);
            dlg.Properties.AddColoredTextBox(sample.Name, sample.Color); // 0
            dlg.Properties.AddColorPicker(sample.Color); // 1
            dlg.Properties.Build();

            dlg.ShowDialogAsync((r) =>
            {
                var newName = dlg.Properties.GetPropertyValue<string>(0).Trim();

                App.UndoRedoManager.BeginTransaction(TransactionScope.DPCMSample, sample.Id);

                if (App.Project.RenameSample(sample, newName))
                {
                    sample.Color = dlg.Properties.GetPropertyValue<Color>(1);
                    DPCMSampleColorChanged?.Invoke(sample);
                    RefreshButtons();
                    App.UndoRedoManager.EndTransaction();
                }
                else
                {
                    App.UndoRedoManager.AbortTransaction();
                    App.DisplayNotification(RenameSampleError, true);
                }
            });
        }

        private bool HandleMouseDoubleClickSong(MouseEventArgs e, Button button)
        {
            if (App.Project.Songs.Count > 1)
            {
                AskDeleteSong(button.song);
                return true;
            }

            return false;
        }

        private bool HandleMouseDoubleClickInstrument(MouseEventArgs e, Button button)
        {
            if (button.instrument != null)
            {
                AskDeleteInstrument(button.instrument);
                return true;
            }

            return false;
        }

        private bool HandleMouseDoubleClickArpeggio(MouseEventArgs e, Button button)
        {
            AskDeleteArpeggio(button.arpeggio);
            return true;
        }

        private bool HandleMouseDoubleClickDPCMSample(MouseEventArgs e, Button button)
        {
            AskDeleteDPCMSample(button.sample);
            return true;
        }

        private bool HandleMouseDoubleClickParamListButton(MouseEventArgs e, Button button, int buttonIdx)
        {
            return e.Left && ClickParamListOrSliderButton(e.X, e.Y, button, buttonIdx, true);
        }

        private bool HandleMouseDoubleClickButtons(MouseEventArgs e)
        {
            var buttonIdx = GetButtonAtCoord(e.X, e.Y, out var subButtonType);

            if (e.Left && buttonIdx >= 0 && subButtonType == SubButtonType.Max)
            {
                if (captureOperation != CaptureOperation.None)
                    AbortCaptureOperation();

                var button = buttons[buttonIdx];

                switch (button.type)
                {
                    // TODO : Figure out the delete thing.
                    //case ButtonType.Song:
                    //    return HandleMouseDoubleClickSong(e, button);
                    //case ButtonType.Instrument:
                    //    return HandleMouseDoubleClickInstrument(e, button);
                    //case ButtonType.Arpeggio:
                    //    return HandleMouseDoubleClickArpeggio(e, button);
                    //case ButtonType.Dpcm:
                    //    return HandleMouseDoubleClickDPCMSample(e, button);

                    case ButtonType.ParamSlider:
                    case ButtonType.ParamList:
                        // Treat double-clicks as click. These are generated when click
                        // very fast on a button : click -> double click -> click -> double click -> ...
                        return HandleMouseDoubleClickParamListButton(e, button, buttonIdx);
                }

                return true;
            }

            return false;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (HandleMouseDoubleClickButtons(e)) goto Handled;
            OnMouseDown(e);
            return;
        Handled:
            MarkDirty();
        }

        public void ValidateIntegrity()
        {
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref expandedInstrument);
            buffer.Serialize(ref selectedInstrumentTab);
            buffer.Serialize(ref expandedSample);
            buffer.Serialize(ref scrollY);

            if (buffer.IsReading)
            {
                captureOperation = CaptureOperation.None;
                lastCaptureOperation = CaptureOperation.None;
                Capture = false;
                flingVelY = 0.0f;

                ClampScroll();
                RefreshButtons();
                BlinkButton(null);
            }
        }
    }
}
