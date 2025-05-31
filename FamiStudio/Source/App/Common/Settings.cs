using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FamiStudio
{
    static class Settings
    {
        // Version in case we need to do deprecation.
        // Version 0-1  : Any FamiStudio < 3.0.0
        // Version 2    : FamiStudio 3.0.0
        // Version 3    : FamiStudio 3.1.0
        // Version 4    : FamiStudio 3.2.0
        // Version 5    : FamiStudio 3.2.3 (Added snapping tutorial)
        // Version 6    : FamiStudio 3.3.0
        // Version 7    : FamiStudio 4.0.0 (Animated GIF tutorials, control changes, recent files, dialogs)
        // Version 8    : FamiStudio 4.1.0 (Configurable keys)
        // Version 9-10 : FamiStudio 4.2.0 (Latency improvements, more filtering options)
        // Version 11   : FamiStudio 4.4.0 (Separate bass filter for FDS)
        public const int SettingsVersion = 11;
        public const int NumRecentFiles  = 10;

        // Constants for follow.
        public const int FollowModeJump       = 0;
        public const int FollowModeContinuous = 1;

        public const int FollowSyncSequencer = 0;
        public const int FollowSyncPianoRoll = 1;
        public const int FollowSyncBoth      = 2;

        // Constants for scroll bars
        public const int ScrollBarsNone      = 0;
        public const int ScrollBarsThin      = 1;
        public const int ScrollBarsThick     = 2;

        // Constants for Stop notes.
        public const int StopNotesFamiTrackerTempo = 0;
        public const int StopNotesNever            = 1;

        // Constants for DPCM color mode
        public const int ColorModeInstrument = 0;
        public const int ColorModeSample = 1;

        // General section.
        public static int Version = SettingsVersion;
        public static string LanguageCode = "";
        public static bool CheckUpdates = true;
        public static bool ShowTutorial = true;
        public static bool ClearUndoRedoOnSave = true;
        public static bool RestoreViewOnUndoRedo = true;
        public static bool OpenLastProjectOnStart = Platform.IsDesktop;
        public static bool RewindAfterPlay = false;
        public static bool AutoSaveCopy = true;
        public static string PatternNamePrefix = "Pattern ";
        public static int PatternNameNumDigits = 1;
        public static int NewVersionCounter = 3;
        public static string LastProjectFile;

        // User Interface section
        public static int DpiScaling = 0;
        public static int TimeFormat = 1;
        public static int FollowMode = 0;
        public static int FollowSync = 0;
        public static float FollowPercent = 0.75f;
        public static int  ScrollBars = ScrollBarsNone;
        public static int  IdealSequencerSize = 25;
        public static int  DpcmColorMode = ColorModeInstrument;
        public static bool AllowSequencerVerticalScroll = false;
        public static bool ShowImplicitStopNotes = false;
        public static bool ShowRegisterViewer = Platform.IsDesktop;
        public static bool UseOSDialogs;

        // Input section
        public static bool TrackPadControls = false;
        public static bool ReverseTrackPadX = false;
        public static bool ReverseTrackPadY = false;
        public static float TrackPadMoveSensitity = 10.0f;
        public static float TrackPadZoomSensitity = 10.0f;
        public static bool AltLeftForMiddle = false;
        public static bool AltZoomAllowed = false;

        // Keys section
        public static List<Shortcut> AllShortcuts = new List<Shortcut>();
        public static List<Shortcut> DefaultShortcuts;

        #region Localization

        private static LocalizedString FileNewName;
        private static LocalizedString FileOpenName;
        private static LocalizedString FileSaveName;
        private static LocalizedString FileSaveAsName;
        private static LocalizedString FileExportName;
        private static LocalizedString FileExportRepeatName;

        private static LocalizedString CopyName;
        private static LocalizedString CutName;
        private static LocalizedString PasteName;
        private static LocalizedString PasteSpecialName;
        private static LocalizedString DeleteName;
        private static LocalizedString DeleteSpecialName;
        private static LocalizedString SelectAllName;

        private static LocalizedString UndoName;
        private static LocalizedString RedoName;

        private static LocalizedString ToggleQwertyName;
        private static LocalizedString ToggleRecordingName;
        private static LocalizedString ToggleFollowModeName;

        private static LocalizedString PlayName;
        private static LocalizedString PlayFromStartName;
        private static LocalizedString PlayFromPatternName;
        private static LocalizedString PlayFromLoopName;

        private static LocalizedString SeekStartName;
        private static LocalizedString SeekStartPatternName;

        private static LocalizedString QwertySkipName;
        private static LocalizedString QwertyBackName;
        private static LocalizedString QwertyStopName;
        private static LocalizedString QwertyOctaveUpName;
        private static LocalizedString QwertyOctaveDownName;

        private static LocalizedString SnapToggleName;
        private static LocalizedString EffectPanelName;
        private static LocalizedString MaximizePianoRollName;

        private static LocalizedString ReleaseNoteName;
        private static LocalizedString StopNoteName;
        private static LocalizedString SlideNoteName;
        private static LocalizedString AttackName;
        private static LocalizedString EyeDropNoteName;
        private static LocalizedString SetNoteInstrumentName;
        private static LocalizedString SetLoopPointName;

        private static LocalizedString QwertyPrefix;
        private static LocalizedString SetActiveChannelPrefix;
        private static LocalizedString ForceDisplayPrefix;

        #endregion

        public static Shortcut FileNewShortcut;
        public static Shortcut FileOpenShortcut;
        public static Shortcut FileSaveShortcut;
        public static Shortcut FileSaveAsShortcut;
        public static Shortcut FileExportShortcut;
        public static Shortcut FileExportRepeatShortcut;

        public static Shortcut CopyShortcut;
        public static Shortcut CutShortcut;
        public static Shortcut PasteShortcut;
        public static Shortcut PasteSpecialShortcut;
        public static Shortcut DeleteShortcut;
        public static Shortcut DeleteSpecialShortcut;
        public static Shortcut SelectAllShortcut;

        public static Shortcut UndoShortcut;
        public static Shortcut RedoShortcut;

        public static Shortcut QwertyShortcut;
        public static Shortcut RecordingShortcut;
        public static Shortcut FollowModeShortcut;

        public static Shortcut PlayShortcut;
        public static Shortcut PlayFromStartShortcut;
        public static Shortcut PlayFromPatternShortcut;
        public static Shortcut PlayFromLoopShortcut;

        public static Shortcut SeekStartShortcut;
        public static Shortcut SeekStartPatternShortcut;

        public static Shortcut QwertySkipShortcut;
        public static Shortcut QwertyBackShortcut;
        public static Shortcut QwertyStopShortcut;
        public static Shortcut QwertyOctaveUpShortcut;
        public static Shortcut QwertyOctaveDownShortcut;

        public static Shortcut SnapToggleShortcut;
        public static Shortcut EffectPanelShortcut;
        public static Shortcut MaximizePianoRollShortcut;

        public static Shortcut ReleaseNoteShortcut;
        public static Shortcut StopNoteShortcut;
        public static Shortcut SlideNoteShortcut;
        public static Shortcut AttackShortcut;
        public static Shortcut EyeDropNoteShortcut;
        public static Shortcut SetNoteInstrumentShortcut;
        public static Shortcut SetLoopPointShortcut;

        public static Shortcut[] QwertyNoteShortcuts;
        public static Shortcut[] ActiveChannelShortcuts;
        public static Shortcut[] DisplayChannelShortcuts;

        // Audio section
        const int DefaultNumBufferedFrames = 2;
        const int DefaultAudioBufferSize = Platform.IsLinux ? 60 : 30;
        const int EmulationThreadCpuScoreThreshold = 100;

        public static string AudioAPI = "";
        public static int AudioBufferSize = DefaultAudioBufferSize;
        public static int NumBufferedFrames = DefaultNumBufferedFrames;
        public static int InstrumentStopTime = 1;
        public static bool SquareSmoothVibrato = true;
        public static bool N163Mix = true;
        public static bool ClampPeriods = true;
        public static bool AccurateSeek = false;
        public static bool NoDragSoungWhenPlaying = false;
        public static int MetronomeVolume = 50;
        public static int SeparateChannelsExportTndMode = NesApu.TND_MODE_SINGLE;

        // Mixer section
        public const float DefaultGlobalVolumeDb = -2.0f;
        public const int DefaultBassCutoffHz = 24;
        public static float GlobalVolumeDb = DefaultGlobalVolumeDb; // in dB
        public static int BassCutoffHz = DefaultBassCutoffHz; // in Hz
        public static ExpansionMixer[] ExpansionMixerSettings = new ExpansionMixer[ExpansionType.Count];

        // MIDI section
        public static string MidiDevice = "";

        // Last used folders
        public static string LastFileFolder = "";
        public static string LastInstrumentFolder  = "";
        public static string LastSampleFolder = "";
        public static string LastExportFolder = "";

        // Recent files history.
        public static List<string> RecentFiles = new List<string>();

        // Misc
        public static string FFmpegExecutablePath = "";

        // Mobile section
        public static bool AllowVibration = true;
        public static bool ForceLandscape = false;
        public static int  MobilePianoHeight = 25;

        // Piano roll stuff
        public const int DefaultSnapResolution = Platform.IsMobile ? SnapResolutionType.QuarterBeat : SnapResolutionType.Beat;
        public static int SnapResolution = DefaultSnapResolution;
        public static bool SnapEnabled = true;
        public static bool SnapEffects = false;

        public delegate void EmptyDelegate();
        public static event EmptyDelegate KeyboardShortcutsChanged;

        public static void Initialize()
        {
            Localization.LocalizeStatic(typeof(Settings));

            InitShortcuts();
            Load();
        }

        private static void InitShortcuts()
        {
            FileNewShortcut          = new Shortcut(FileNewName,          "FileNew",          Keys.N, ModifierKeys.Control,      false);
            FileOpenShortcut         = new Shortcut(FileOpenName,         "FileOpen",         Keys.O, ModifierKeys.Control,      false);
            FileSaveShortcut         = new Shortcut(FileSaveName,         "FileSave",         Keys.S, ModifierKeys.Control,      false);
            FileSaveAsShortcut       = new Shortcut(FileSaveAsName,       "FileSaveAs",       Keys.S, ModifierKeys.ControlShift, false);
            FileExportShortcut       = new Shortcut(FileExportName,       "FileExport",       Keys.E, ModifierKeys.Control,      false);
            FileExportRepeatShortcut = new Shortcut(FileExportRepeatName, "FileExportRepeat", Keys.E, ModifierKeys.ControlShift, false);

            SelectAllShortcut     = new Shortcut(SelectAllName,     "SelectAll",     Keys.A, ModifierKeys.Control);
            CopyShortcut          = new Shortcut(CopyName,          "Copy",          Keys.C, ModifierKeys.Control);
            CutShortcut           = new Shortcut(CutName,           "Cut",           Keys.X, ModifierKeys.Control);
            PasteShortcut         = new Shortcut(PasteName,         "Paste",         Keys.V, ModifierKeys.Control);
            PasteSpecialShortcut  = new Shortcut(PasteSpecialName,  "PasteSpecial",  Keys.V, ModifierKeys.ControlShift, false);
            DeleteShortcut        = new Shortcut(DeleteName,        "Delete",        Keys.Delete);
            DeleteSpecialShortcut = new Shortcut(DeleteSpecialName, "DeleteSpecial", Keys.Delete, ModifierKeys.ControlShift, false);

            UndoShortcut = new Shortcut(UndoName, "Undo", Keys.Z, ModifierKeys.Control, false);
            RedoShortcut = new Shortcut(RedoName, "Redo", Keys.Y, ModifierKeys.Control, Keys.Z, ModifierKeys.ControlShift, false);

            QwertyShortcut     = new Shortcut(ToggleQwertyName,     "Qwerty",     Keys.Q, ModifierKeys.Shift);
            RecordingShortcut  = new Shortcut(ToggleRecordingName,  "Recording",  Keys.Enter);
            FollowModeShortcut = new Shortcut(ToggleFollowModeName, "FollowMode", Keys.F, ModifierKeys.Shift);

            PlayShortcut             = new Shortcut(PlayName,            "Play",            Keys.Space);
            PlayFromStartShortcut    = new Shortcut(PlayFromStartName,   "PlayFromStart",   Keys.Space, ModifierKeys.Shift);
            PlayFromPatternShortcut  = new Shortcut(PlayFromPatternName, "PlayFromPattern", Keys.Space, ModifierKeys.Control);
            PlayFromLoopShortcut     = new Shortcut(PlayFromLoopName,    "PlayFromLoop",    Keys.Space, ModifierKeys.ControlShift);

            SeekStartShortcut        = new Shortcut(SeekStartName,        "SeekStart",        Keys.Home);
            SeekStartPatternShortcut = new Shortcut(SeekStartPatternName, "SeekStartPattenr", Keys.Home, ModifierKeys.Control);

            QwertySkipShortcut       = new Shortcut(QwertySkipName,       "QwertySkip",       Keys.Tab);
            QwertyBackShortcut       = new Shortcut(QwertyBackName,       "QwertyBack",       Keys.Backspace);
            QwertyStopShortcut       = new Shortcut(QwertyStopName,       "QwertyStop",       Keys.D1);
            QwertyOctaveUpShortcut   = new Shortcut(QwertyOctaveUpName,   "QwertyOctaveUp",   Keys.PageUp);
            QwertyOctaveDownShortcut = new Shortcut(QwertyOctaveDownName, "QwertyOctaveDown", Keys.PageDown);

            SnapToggleShortcut        = new Shortcut(SnapToggleName,        "SnapToggle",        Keys.S, ModifierKeys.Shift);
            EffectPanelShortcut       = new Shortcut(EffectPanelName,       "EffectPanel",       Keys.D1);
            MaximizePianoRollShortcut = new Shortcut(MaximizePianoRollName, "MaximizePianoRoll", Keys.D1, ModifierKeys.Control);

            ReleaseNoteShortcut       = new Shortcut(ReleaseNoteName,       "ReleaseNote",       Keys.R, false);
            StopNoteShortcut          = new Shortcut(StopNoteName,          "StopNote",          Keys.T, false);
            SlideNoteShortcut         = new Shortcut(SlideNoteName,         "SlideNote",         Keys.S, false);
            AttackShortcut            = new Shortcut(AttackName,            "ToggleAttack",      Keys.A, false);
            EyeDropNoteShortcut       = new Shortcut(EyeDropNoteName,       "EyeDrop",           Keys.I, false);
            SetNoteInstrumentShortcut = new Shortcut(SetNoteInstrumentName, "SetNoteInstrument", Keys.O, false);
            SetLoopPointShortcut      = new Shortcut(SetLoopPointName,      "LoopPoint",         Keys.L, false);

            QwertyNoteShortcuts = new Shortcut[]
            {
                new Shortcut(QwertyPrefix.Format("C0"),  "Qwerty00", Keys.Z, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("C#0"), "Qwerty01", Keys.S, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("D0"),  "Qwerty02", Keys.X, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("D#0"), "Qwerty03", Keys.D, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("E0"),  "Qwerty04", Keys.C, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("F0"),  "Qwerty05", Keys.V, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("F#0"), "Qwerty06", Keys.G, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("G0"),  "Qwerty07", Keys.B, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("G#0"), "Qwerty08", Keys.H, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("A0"),  "Qwerty09", Keys.N, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("A#0"), "Qwerty10", Keys.J, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("B0"),  "Qwerty11", Keys.M, Keys.Unknown),

                new Shortcut(QwertyPrefix.Format("C1"),  "Qwerty12", Keys.Q,  Keys.Comma),
                new Shortcut(QwertyPrefix.Format("C#1"), "Qwerty13", Keys.D2, Keys.L),
                new Shortcut(QwertyPrefix.Format("D1"),  "Qwerty14", Keys.W,  Keys.Period),
                new Shortcut(QwertyPrefix.Format("D#1"), "Qwerty15", Keys.D3, Keys.SemiColon),
                new Shortcut(QwertyPrefix.Format("E1"),  "Qwerty16", Keys.E,  Keys.Slash),
                new Shortcut(QwertyPrefix.Format("F1"),  "Qwerty17", Keys.R,  Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("F#1"), "Qwerty18", Keys.D5, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("G1"),  "Qwerty19", Keys.T,  Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("G#1"), "Qwerty20", Keys.D6, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("A1"),  "Qwerty21", Keys.Y,  Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("A#1"), "Qwerty22", Keys.D7, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("B1"),  "Qwerty23", Keys.U,  Keys.Unknown),

                new Shortcut(QwertyPrefix.Format("C2"),  "Qwerty24", Keys.I,            Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("C#2"), "Qwerty25", Keys.D9,           Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("D2"),  "Qwerty26", Keys.O,            Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("D#2"), "Qwerty27", Keys.D0,           Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("E2"),  "Qwerty28", Keys.P,            Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("F2"),  "Qwerty29", Keys.LeftBracket,  Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("F#2"), "Qwerty30", Keys.Equal,        Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("G2"),  "Qwerty31", Keys.RightBracket, Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("G#2"), "Qwerty32", Keys.Unknown,      Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("A2"),  "Qwerty33", Keys.Unknown,      Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("A#2"), "Qwerty34", Keys.Unknown,      Keys.Unknown),
                new Shortcut(QwertyPrefix.Format("B2"),  "Qwerty35", Keys.Unknown,      Keys.Unknown)
            };

            ActiveChannelShortcuts = new Shortcut[]
            {
                new Shortcut(SetActiveChannelPrefix.Format("1"),  "Channel01", Keys.F1,  true, false),
                new Shortcut(SetActiveChannelPrefix.Format("2"),  "Channel02", Keys.F2,  true, false),
                new Shortcut(SetActiveChannelPrefix.Format("3"),  "Channel03", Keys.F3,  true, false),
                new Shortcut(SetActiveChannelPrefix.Format("4"),  "Channel04", Keys.F4,  true, false),
                new Shortcut(SetActiveChannelPrefix.Format("5"),  "Channel05", Keys.F5,  true, false),
                new Shortcut(SetActiveChannelPrefix.Format("6"),  "Channel06", Keys.F6,  true, false),
                new Shortcut(SetActiveChannelPrefix.Format("7"),  "Channel07", Keys.F7,  true, false),
                new Shortcut(SetActiveChannelPrefix.Format("8"),  "Channel08", Keys.F8,  true, false),
                new Shortcut(SetActiveChannelPrefix.Format("9"),  "Channel09", Keys.F9,  true, false),
                new Shortcut(SetActiveChannelPrefix.Format("10"), "Channel10", Keys.F10, true, false),
                new Shortcut(SetActiveChannelPrefix.Format("11"), "Channel11", Keys.F11, true, false),
                new Shortcut(SetActiveChannelPrefix.Format("12"), "Channel12", Keys.F12, true, false),
                new Shortcut(SetActiveChannelPrefix.Format("13"), "Channel13", Keys.F13, true, false),
                new Shortcut(SetActiveChannelPrefix.Format("14"), "Channel14", Keys.F14, true, false),
                new Shortcut(SetActiveChannelPrefix.Format("15"), "Channel15", Keys.F15, true, false),
                new Shortcut(SetActiveChannelPrefix.Format("16"), "Channel16", Keys.F16, true, false),
                new Shortcut(SetActiveChannelPrefix.Format("17"), "Channel17", Keys.F17, true, false),
                new Shortcut(SetActiveChannelPrefix.Format("18"), "Channel18", Keys.F18, true, false),
                new Shortcut(SetActiveChannelPrefix.Format("19"), "Channel19", Keys.F19, true, false),
                new Shortcut(SetActiveChannelPrefix.Format("20"), "Channel20", Keys.F20, true, false),
                new Shortcut(SetActiveChannelPrefix.Format("21"), "Channel21", Keys.F21, true, false),
                new Shortcut(SetActiveChannelPrefix.Format("22"), "Channel22", Keys.F22, true, false),
                new Shortcut(SetActiveChannelPrefix.Format("23"), "Channel23", Keys.F23, true, false),
                new Shortcut(SetActiveChannelPrefix.Format("24"), "Channel24", Keys.F24, true, false),
            };

            DisplayChannelShortcuts = new Shortcut[]
            {
                new Shortcut(ForceDisplayPrefix.Format("1"),  "DisplayChannel01", Keys.F1,  ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("2"),  "DisplayChannel02", Keys.F2,  ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("3"),  "DisplayChannel03", Keys.F3,  ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("4"),  "DisplayChannel04", Keys.F4,  ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("5"),  "DisplayChannel05", Keys.F5,  ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("6"),  "DisplayChannel06", Keys.F6,  ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("7"),  "DisplayChannel07", Keys.F7,  ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("8"),  "DisplayChannel08", Keys.F8,  ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("9"),  "DisplayChannel09", Keys.F9,  ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("10"), "DisplayChannel10", Keys.F10, ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("11"), "DisplayChannel11", Keys.F11, ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("12"), "DisplayChannel12", Keys.F12, ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("13"), "DisplayChannel13", Keys.F13, ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("14"), "DisplayChannel14", Keys.F14, ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("15"), "DisplayChannel15", Keys.F15, ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("16"), "DisplayChannel16", Keys.F16, ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("17"), "DisplayChannel17", Keys.F17, ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("18"), "DisplayChannel18", Keys.F18, ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("19"), "DisplayChannel19", Keys.F19, ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("20"), "DisplayChannel20", Keys.F20, ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("21"), "DisplayChannel21", Keys.F21, ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("22"), "DisplayChannel22", Keys.F22, ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("23"), "DisplayChannel23", Keys.F23, ModifierKeys.Shift),
                new Shortcut(ForceDisplayPrefix.Format("24"), "DisplayChannel24", Keys.F24, ModifierKeys.Shift),
            };

            AllShortcuts.Sort((c1, c2) => c1.ConfigName.CompareTo(c2.ConfigName));
            DefaultShortcuts = Shortcut.CloneList(AllShortcuts);
        }

        public static void Load()
        {
            var ini = new IniFile();
            ini.Load(GetConfigFileName(), false);

            Version = ini.GetInt("General", "Version", 0);

            // General
            LanguageCode = ini.GetString("General", "Language", "");
            CheckUpdates = ini.GetBool(Version < 2 ? "UI" : "General", "CheckUpdates", true ); // At version 2 (FamiStudio 3.0.0, changed section)
            ShowTutorial = ini.GetBool(Version < 2 ? "UI" : "General", "ShowTutorial", true ); // At version 2 (FamiStudio 3.0.0, changed section)
            ClearUndoRedoOnSave = ini.GetBool("General", "ClearUndoRedoOnSave", true);
            RestoreViewOnUndoRedo = ini.GetBool("General", "RestoreViewOnUndoRedo", true);
            RewindAfterPlay = ini.GetBool("General", "RewindAfterPlay", false);
            OpenLastProjectOnStart = ini.GetBool("General", "OpenLastProjectOnStart", true);
            AutoSaveCopy = ini.GetBool("General", "AutoSaveCopy", true);
            PatternNamePrefix = ini.GetString("General", "PatternNamePrefix", "Pattern ");
            PatternNameNumDigits = ini.GetInt("General", "PatternNameNumDigits", 1);
            NewVersionCounter = ini.GetInt("General", "NewVersionCounter", 3);
            LastProjectFile = OpenLastProjectOnStart ? ini.GetString("General", "LastProjectFile", "") : "";

            // Reset new version counter if last version the user used isnt this one.
            if (ini.GetString("General", "LastFamiStudioVersion", "0.0.0.0") != Platform.ApplicationVersion)
                NewVersionCounter = 3;

            // UI
            DpiScaling = ini.GetInt("UI", "DpiScaling", 0);
            TimeFormat = ini.GetInt("UI", "TimeFormat", 0);
            FollowMode = ini.GetInt("UI", "FollowMode", FollowModeContinuous);
            FollowSync = ini.GetInt("UI", "FollowSync", FollowSyncBoth);
            FollowPercent = ini.GetFloat("UI", "FollowPercent", 0.75f);
            ScrollBars = Version < 3 ? (ini.GetBool("UI", "ShowScrollBars", false) ? ScrollBarsThin : ScrollBarsNone) : ini.GetInt("UI", "ScrollBars", ScrollBarsNone);
            IdealSequencerSize = ini.GetInt("UI", "IdealSequencerSize", 25);
            DpcmColorMode = ini.GetInt("UI", "DpcmColorMode", ColorModeInstrument);
            AllowSequencerVerticalScroll = ini.GetBool("UI", "AllowSequencerVerticalScroll", false);
            ShowImplicitStopNotes = ini.GetBool("UI", "ShowImplicitStopNotes", false);
            ShowRegisterViewer = ini.GetBool("UI", "ShowRegisterViewer", Platform.IsDesktop);
            UseOSDialogs = ini.GetBool("UI", "UseOSDialogs", true);

            // Input
            // At version 7 (FamiStudio 4.0.0) we changed the trackpad settings.
            if (Version >= 7)
            {
                TrackPadControls = ini.GetBool("Input", "TrackPadControls", false);
                ReverseTrackPadX = ini.GetBool("Input", "ReverseTrackPadX", false);
                ReverseTrackPadY = ini.GetBool("Input", "ReverseTrackPadY", false);
                TrackPadMoveSensitity = ini.GetFloat("Input", "TrackPadMoveSensitity", 10.0f);
                TrackPadZoomSensitity = ini.GetFloat("Input", "TrackPadZoomSensitity", 10.0f);
                AltLeftForMiddle = ini.GetBool("Input", "AltLeftForMiddle", false);
                AltZoomAllowed = ini.GetBool("Input", "AltZoomAllowed", false);
            }

            // Audio
            var audioAPIs = Platform.GetAvailableAudioAPIs();
            AudioAPI = ini.GetString("Audio", "AudioAPI", audioAPIs[0]);
            AudioBufferSize = ini.GetInt("Audio", "AudioBufferSize", DefaultAudioBufferSize);
            NumBufferedFrames = ini.GetInt("Audio", "NumBufferedFrames", DefaultNumBufferedFrames);
            InstrumentStopTime = ini.GetInt("Audio", "InstrumentStopTime", 1);
            SquareSmoothVibrato = ini.GetBool("Audio", "SquareSmoothVibrato", true);
            N163Mix = ini.GetBool("Audio", "N163Mix", true);
            ClampPeriods = ini.GetBool("Audio", "ClampPeriods", true);
            AccurateSeek = ini.GetBool("Audio", "AccurateSeek", false);
            NoDragSoungWhenPlaying = ini.GetBool("Audio", "NoDragSoungWhenPlaying", false);
            MetronomeVolume = int.Clamp(ini.GetInt("Audio", "MetronomeVolume", 50), 1, 100);
            SeparateChannelsExportTndMode = ini.GetInt("Audio", "SeparateChannelsExportTndMode", NesApu.TND_MODE_SINGLE);

            if (!audioAPIs.Contains(AudioAPI))
            {
                AudioAPI = audioAPIs[0];
            }

            // Latency changes, reset to default.
            if (Version < 10)
            {
                // Only enable "NumBufferedFrames" on very crappy mobile devices. Even very old PCs can emulate
                // a frame of EPSM in 1-3ms. Older phones will need to run emulation threads, thankfully most of
                // these have  4 to 8 cores. For reference, my Pixel 6a (score 176) emulates EPSM in 6-9ms. 
                if (Platform.IsMobile && Utils.BenchmarkCPU() < EmulationThreadCpuScoreThreshold)
                    NumBufferedFrames = 2;
                else
                    NumBufferedFrames = 0;

                AudioBufferSize = DefaultAudioBufferSize;
            }

            // MIDI
            MidiDevice = ini.GetString("MIDI", "Device", "");

            // Folders
            LastFileFolder = ini.GetString("Folders", "LastFileFolder", "");
            LastInstrumentFolder = ini.GetString("Folders", "LastInstrumentFolder", "");
            LastSampleFolder = ini.GetString("Folders", "LastSampleFolder", "");
            LastExportFolder = ini.GetString("Folders", "LastExportFolder", "");

            // Recent files.
            for (int i = 0; i < NumRecentFiles; i++)
            {
                var recentFile = ini.GetString("RecentFiles", $"RecentFile{i}", "");
                if (!string.IsNullOrEmpty(recentFile) && File.Exists(recentFile))
                    RecentFiles.Add(recentFile);
            }

            // FFmpeg
            FFmpegExecutablePath = ini.GetString("FFmpeg", "ExecutablePath", "");

            // Mixer.
            GlobalVolumeDb = ini.GetFloat("Mixer", "GlobalVolume", DefaultGlobalVolumeDb);
            BassCutoffHz = ini.GetInt("Mixer", "BassCutoffHz", DefaultBassCutoffHz);

            Array.Copy(ExpansionMixer.DefaultExpansionMixerSettings, ExpansionMixerSettings, ExpansionMixerSettings.Length);

            // At Version 9 (FamiStudio 4.2.0) we added more filtering options.
            if (Version >= 9)
            { 
                for (int i = 0; i < ExpansionType.Count; i++)
                {
                    var section = "Mixer" + ExpansionType.InternalNames[i];

                    ExpansionMixerSettings[i].VolumeDb         = ini.GetFloat(section, "VolumeDb",      ExpansionMixer.DefaultExpansionMixerSettings[i].VolumeDb);
                    
                    // FDS bass filter (4.4.0).
                    if (Version >= 11 && i == ExpansionType.Fds)
                        ExpansionMixerSettings[i].BassCutoffHz = ini.GetInt(section, "BassCutoffHz",    ExpansionMixer.DefaultExpansionMixerSettings[i].BassCutoffHz);
                    else
                        ExpansionMixerSettings[i].TrebleDb     = ini.GetFloat(section, "TrebleDb",      ExpansionMixer.DefaultExpansionMixerSettings[i].TrebleDb);
                    
                    ExpansionMixerSettings[i].TrebleRolloffHz  = ini.GetInt(section, "TrebleRolloffHz", ExpansionMixer.DefaultExpansionMixerSettings[i].TrebleRolloffHz);
                }
            }

            // At version 7 (FamiStudio 4.1.0) we allowed configuring all the keys.
            if (Version >= 8)
            {
                foreach (var shortcut in AllShortcuts)
                {
                    var shortcutStr1 = ini.GetString("Keys", shortcut.ConfigName, null);
                    if (shortcutStr1 != null)
                        shortcut.FromConfigString(shortcutStr1, 0);

                    var shortcutStr2 = ini.GetString("Keys", shortcut.ConfigName + "_Alt", null);
                    if (shortcutStr2 != null)
                        shortcut.FromConfigString(shortcutStr2, 1);
                }
            }

            if (Array.IndexOf(global::FamiStudio.DpiScaling.GetAvailableScalings(), DpiScaling) < 0)
                DpiScaling = 0;

            InstrumentStopTime = Utils.Clamp(InstrumentStopTime, 0, 10);

            if (MidiDevice == null)
                MidiDevice = "";
            if (!Directory.Exists(LastSampleFolder))
                LastSampleFolder = "";
            if (!Directory.Exists(LastExportFolder))
                LastExportFolder = "";
            if ( Localization.GetIndexForLanguageCode(LanguageCode) < 0)
                LanguageCode = ""; // Empty = system

            var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Need to get out of the package on MacOS. (TODO : Move to Platform).
            if (Platform.IsMacOS)
                appPath = Path.Combine(appPath, "../../..");

            // Try to point to the demo songs initially.
            if (string.IsNullOrEmpty(LastFileFolder) || !Directory.Exists(LastFileFolder))
            {
                var demoSongsPath = Path.Combine(appPath, "Demo Songs");
                LastFileFolder = Directory.Exists(demoSongsPath) ? demoSongsPath : "";
            }

            // Try to point to the demo instruments initially.
            if (string.IsNullOrEmpty(LastInstrumentFolder) || !Directory.Exists(LastInstrumentFolder))
            {
                var demoInstPath = Path.Combine(appPath, "Demo Instruments");
                LastInstrumentFolder = Directory.Exists(demoInstPath) ? demoInstPath : "";
            }

            // Clamp to something reasonable.
            NumBufferedFrames = Utils.Clamp(NumBufferedFrames, 0, 16);

            // Linux or Mac is more likely to have standard path for ffmpeg.
            if (Platform.IsLinux || Platform.IsMacOS)
            {
                if (string.IsNullOrEmpty(FFmpegExecutablePath) || !File.Exists(FFmpegExecutablePath))
                {
                    if (File.Exists("/usr/bin/ffmpeg"))
                        FFmpegExecutablePath = "/usr/bin/ffmpeg";
                    else if (File.Exists("/usr/local/bin/ffmpeg"))
                        FFmpegExecutablePath = "/usr/local/bin/ffmpeg";
                    else
                        FFmpegExecutablePath = "ffmpeg"; // Hope for the best!
                }
            }
            
            // Mobile section
            AllowVibration = ini.GetBool("Mobile", "AllowVibration", true);
            ForceLandscape = ini.GetBool("Mobile", "ForceLandscape", false);
            MobilePianoHeight = ini.GetInt("Mobile", "MobilePianoHeight", 25);

            // Piano roll section
            SnapResolution = Utils.Clamp(ini.GetInt("PianoRoll", "SnapResolution", DefaultSnapResolution), SnapResolutionType.Min, SnapResolutionType.Max);
            SnapEnabled = ini.GetBool("PianoRoll", "SnapEnabled", true);
            SnapEffects = ini.GetBool("PianoRoll", "SnapEffects", false);

            // At 4.0.0, we fixed an issue where the snapping was not saved properly. Reset.
            if (Version < 7)
            {
                SnapResolution = DefaultSnapResolution;
                SnapEnabled = true;
                ShowImplicitStopNotes = false;
            }

            // At 3.2.0, we added a new Discord screen to the tutorial.
            // At 3.2.3, we added a new snapping tutorial screen.
            // At 4.0.0, we changed the controls and need to re-show tutorials and added a new snapping tutorial on mobile.
            if (Version < 7)
                ShowTutorial = true;

            // Re-force time format to the MM:SS:mmm
            if (Version < 6)
                TimeFormat = 1;

            // No deprecation at the moment.
            Version = SettingsVersion;
        }

        public static string LoadLanguageCodeOnly()
        {
            var ini = new IniFile();
            ini.Load(GetConfigFileName(), false);
            return ini.GetString("General", "Language", "");
        }

        public static void Save()
        {
            // Never same settings in command line, we dont have the proper
            // QWERTY keys set up since GLFW isnt initialized.
            if (Platform.IsCommandLine)
                return;

            var ini = new IniFile();

            // General
            ini.SetInt("General", "Version", SettingsVersion);
            ini.SetString("General", "Language", LanguageCode);
            ini.SetBool("General", "CheckUpdates", CheckUpdates);
            ini.SetBool("General", "ShowTutorial", ShowTutorial);
            ini.SetBool("General", "ClearUndoRedoOnSave", ClearUndoRedoOnSave);
            ini.SetBool("General", "RestoreViewOnUndoRedo", RestoreViewOnUndoRedo);
            ini.SetBool("General", "RewindAfterPlay", RewindAfterPlay);
            ini.SetBool("General", "OpenLastProjectOnStart", OpenLastProjectOnStart);
            ini.SetString("General", "LastProjectFile", OpenLastProjectOnStart ? LastProjectFile : "");
            ini.SetString("General", "PatternNamePrefix", PatternNamePrefix);
            ini.SetInt("General", "PatternNameNumDigits", PatternNameNumDigits);
            ini.SetInt("General", "NewVersionCounter", NewVersionCounter);
            ini.SetBool("General", "AutoSaveCopy", AutoSaveCopy);
            ini.SetString("General", "LastFamiStudioVersion", Platform.ApplicationVersion);

            // UI
            ini.SetInt("UI", "DpiScaling", DpiScaling);
            ini.SetInt("UI", "TimeFormat", TimeFormat);
            ini.SetInt("UI", "FollowMode", FollowMode);
            ini.SetInt("UI", "FollowSync", FollowSync);
            ini.SetFloat("UI", "FollowPercent", FollowPercent);
            ini.SetInt("UI", "ScrollBars", ScrollBars);
            ini.SetInt("UI", "IdealSequencerSize", IdealSequencerSize);
            ini.SetInt("UI", "DpcmColorMode", DpcmColorMode);
            ini.SetBool("UI", "AllowSequencerVerticalScroll", AllowSequencerVerticalScroll);
            ini.SetBool("UI", "ShowImplicitStopNotes", ShowImplicitStopNotes);
            ini.SetBool("UI", "ShowRegisterViewer", ShowRegisterViewer);
            ini.SetBool("UI", "UseOSDialogs", UseOSDialogs);

            // Input
            ini.SetBool("Input", "TrackPadControls", TrackPadControls);
            ini.SetFloat("Input", "TrackPadMoveSensitity", TrackPadMoveSensitity);
            ini.SetFloat("Input", "TrackPadZoomSensitity", TrackPadZoomSensitity);
            ini.SetBool("Input", "ReverseTrackPadX", ReverseTrackPadX);
            ini.SetBool("Input", "ReverseTrackPadY", ReverseTrackPadY);
            ini.SetBool("Input", "AltLeftForMiddle", AltLeftForMiddle);
            ini.SetBool("Input", "AltZoomAllowed", AltZoomAllowed);

            // Audio
            ini.SetString("Audio", "AudioAPI", AudioAPI);
            ini.SetInt("Audio", "AudioBufferSize", AudioBufferSize);
            ini.SetInt("Audio", "NumBufferedFrames", NumBufferedFrames);
            ini.SetInt("Audio", "InstrumentStopTime", InstrumentStopTime);
            ini.SetBool("Audio", "SquareSmoothVibrato", SquareSmoothVibrato);
            ini.SetBool("Audio", "N163Mix", N163Mix);
            ini.SetBool("Audio", "ClampPeriods", ClampPeriods);
            ini.SetBool("Audio", "AccurateSeek", AccurateSeek);
            ini.SetBool("Audio", "NoDragSoungWhenPlaying", NoDragSoungWhenPlaying);
            ini.SetInt("Audio", "MetronomeVolume", MetronomeVolume);
            ini.SetInt("Audio", "SeparateChannelsExportTndMode", SeparateChannelsExportTndMode);

            // Mixer
            ini.SetFloat("Mixer", "GlobalVolume", GlobalVolumeDb);
            ini.SetInt("Mixer", "BassCutoffHz", BassCutoffHz);

            for (int i = 0; i < ExpansionType.Count; i++)
            {
                var section = "Mixer" + ExpansionType.InternalNames[i];
                ini.SetFloat(section, "VolumeDb", ExpansionMixerSettings[i].VolumeDb);

                // FDS bass filter.
                if (i == ExpansionType.Fds)
                    ini.SetInt(section, "BassCutoffHz", ExpansionMixerSettings[i].BassCutoffHz);
                else
                    ini.SetFloat(section, "TrebleDb", ExpansionMixerSettings[i].TrebleDb);

                ini.SetFloat(section, "TrebleRolloffHz", ExpansionMixerSettings[i].TrebleRolloffHz);
            }

            // MIDI
            ini.SetString("MIDI", "Device", MidiDevice);

            // Folders
            ini.SetString("Folders", "LastFileFolder", LastFileFolder);
            ini.SetString("Folders", "LastInstrumentFolder", LastInstrumentFolder);
            ini.SetString("Folders", "LastSampleFolder", LastSampleFolder);
            ini.SetString("Folders", "LastExportFolder", LastExportFolder);
            
            // Recent files.
            for (int i = 0; i < RecentFiles.Count; i++)
                ini.SetString("RecentFiles", $"RecentFile{i}", RecentFiles[i]);

            // FFmpeg
            ini.SetString("FFmpeg", "ExecutablePath", FFmpegExecutablePath);

            // Keyboard
            foreach (var shortcut in AllShortcuts)
            {
                ini.SetString("Keys", shortcut.ConfigName, shortcut.ToConfigString(0));
                ini.SetString("Keys", shortcut.ConfigName + "_Alt", shortcut.ToConfigString(1));
            }

            // Mobile
            ini.SetBool("Mobile", "AllowVibration", AllowVibration);
            ini.SetBool("Mobile", "ForceLandscape", ForceLandscape);
            ini.SetInt("Mobile", "MobilePianoHeight", MobilePianoHeight);

            // Piano roll section
            ini.SetInt("PianoRoll", "SnapResolution", SnapResolution);
            ini.SetBool("PianoRoll", "SnapEnabled", SnapEnabled);
            ini.SetBool("PianoRoll", "SnapEffects", SnapEffects);

            Directory.CreateDirectory(GetConfigFilePath());

            ini.Save(GetConfigFileName());
        }

        public static void AddRecentFile(string file)
        {
            RecentFiles.Remove(file);
            RecentFiles.Insert(0, file);

            while (RecentFiles.Count > NumRecentFiles)
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
        }

        public static string GetAutoSaveFilePath()
        {
            return Path.Combine(Settings.GetConfigFilePath(), "AutoSaves");
        }

        private static string GetConfigFilePath()
        {
            if (Platform.IsPortableMode)
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
            else
            {
                return Platform.SettingsDirectory;
            }
        }

        private static string GetConfigFileName()
        {
            return Path.Combine(GetConfigFilePath(), "FamiStudio.ini");
        }

        public static void NotifyKeyboardShortcutsChanged()
        {
            KeyboardShortcutsChanged?.Invoke();
        }
    }
}
