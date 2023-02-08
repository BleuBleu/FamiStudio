using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    static class Settings
    {
        // Version in case we need to do deprecation.
        // Version 0-1 : Any FamiStudio < 3.0.0
        // Version 2   : FamiStudio 3.0.0
        // Version 3   : FamiStudio 3.1.0
        // Version 4   : FamiStudio 3.2.0
        // Version 5   : FamiStudio 3.2.3 (Added snapping tutorial)
        // Version 6   : FamiStudio 3.3.0
        // Version 7   : FamiStudio 4.0.0 (Animated GIF tutorials, control changes, recent files, dialogs)
        // Version 8   : FamiStudio 4.1.0 (Configurable keys)
        public const int SettingsVersion = 8;
        public const int NumRecentFiles = 10;

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

        // General section.
        public static int Version = SettingsVersion;
        public static bool CheckUpdates = true;
        public static bool ShowTutorial = true;
        public static bool ClearUndoRedoOnSave = true;
        public static bool OpenLastProjectOnStart = Platform.IsDesktop;
        public static bool RewindAfterPlay = false;
        public static bool AutoSaveCopy = true;
        public static string PatternNamePrefix = "Pattern ";
        public static int PatternNameNumDigits = 1;
        public static string LastProjectFile;

        // User Interface section
        public static int DpiScaling = 0;
        public static int TimeFormat = 1;
        public static int FollowMode = 0;
        public static int FollowSync = 0;
        public static int  ScrollBars = ScrollBarsNone;
        public static int  IdealSequencerSize = 25;
        public static bool AllowSequencerVerticalScroll = false;
        public static bool ShowImplicitStopNotes = false;
        public static bool ShowRegisterViewer = Platform.IsDesktop;
        public static bool UseOSDialogs = !Platform.IsLinux;

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

        public static Shortcut FileNewShortcut          = new Shortcut("New Project",        "FileNew",          Keys.N, ModifierKeys.Control);
        public static Shortcut FileOpenShortcut         = new Shortcut("Open Project",       "FileOpen",         Keys.O, ModifierKeys.Control);
        public static Shortcut FileSaveShortcut         = new Shortcut("Save Project",       "FileSave",         Keys.S, ModifierKeys.Control);
        public static Shortcut FileSaveAsShortcut       = new Shortcut("Save Project As",    "FileSaveAs",       Keys.S, ModifierKeys.ControlShift);
        public static Shortcut FileExportShortcut       = new Shortcut("Export",             "FileExport",       Keys.E, ModifierKeys.Control);
        public static Shortcut FileExportRepeatShortcut = new Shortcut("Repeat Last Export", "FileExportRepeat", Keys.E, ModifierKeys.ControlShift);

        public static Shortcut CopyShortcut          = new Shortcut("Copy",           "Copy",          Keys.C, ModifierKeys.Control);
        public static Shortcut CutShortcut           = new Shortcut("Cut",            "Cut",           Keys.X, ModifierKeys.Control);
        public static Shortcut PasteShortcut         = new Shortcut("Paste",          "Paste",         Keys.V, ModifierKeys.Control);
        public static Shortcut PasteSpecialShortcut  = new Shortcut("Paste Special",  "PasteSpecial",  Keys.V, ModifierKeys.ControlShift);
        public static Shortcut DeleteShortcut        = new Shortcut("Delete",         "Delete",        Keys.Delete, ModifierKeys.Control);
        public static Shortcut DeleteSpecialShortcut = new Shortcut("Delete Special", "DeleteSpecial", Keys.Delete, ModifierKeys.ControlShift);

        public static Shortcut UndoShortcut       = new Shortcut("Undo", "Undo", Keys.Z, ModifierKeys.Control);
        public static Shortcut RedoShortcut       = new Shortcut("Redo", "Redo", Keys.Y, ModifierKeys.Control, Keys.Z, ModifierKeys.ControlShift);

        public static Shortcut QwertyShortcut     = new Shortcut("Toggle QWERTY Input",   "Qwerty",     Keys.Q, ModifierKeys.Shift);
        public static Shortcut RecordingShortcut  = new Shortcut("Toggle Recording Mode", "Recording",  Keys.Enter);
        public static Shortcut FollowModeShortcut = new Shortcut("Toggle Follow Mode",    "FollowMode", Keys.F, ModifierKeys.Shift);
                                                  
        public static Shortcut PlayShortcut             = new Shortcut("Play",                 "Play",            Keys.Space);
        public static Shortcut PlayFromStartShortcut    = new Shortcut("Play From Start",      "PlayFromStart",   Keys.Space, ModifierKeys.Shift);
        public static Shortcut PlayFromPatternShortcut  = new Shortcut("Play From Pattern",    "PlayFromPattern", Keys.Space, ModifierKeys.Shift);
        public static Shortcut PlayFromLoopShortcut     = new Shortcut("Play From Loop Point", "PlayFromLoop",    Keys.Space, ModifierKeys.ControlShift);

        public static Shortcut SeekStartShortcut        = new Shortcut("Seek to Start",            "SeekStart",        Keys.Home);
        public static Shortcut SeekStartPatternShortcut = new Shortcut("Seek to Start of Pattern", "SeekStartPattenr", Keys.Home, ModifierKeys.Control);

        public static Shortcut QwertySkipShortcut       = new Shortcut("QWERTY Skip",        "QwertySkip",       Keys.Tab);
        public static Shortcut QwertyBackShortcut       = new Shortcut("QWERTY Delete",      "QwertyBack",       Keys.Backspace);
        public static Shortcut QwertyStopShortcut       = new Shortcut("QWERTY Stop Note",   "QwertyStop",       Keys.D1);
        public static Shortcut QwertyOctaveUpShortcut   = new Shortcut("QWERTY Octave Up",   "QwertyOctaveUp",   Keys.PageUp);
        public static Shortcut QwertyOctaveDownShortcut = new Shortcut("QWERTY Octave Down", "QwertyOctaveDown", Keys.PageDown);

        public static Shortcut SnapToggleShortcut        = new Shortcut("Toggle Snapping",     "SnapToggle",        Keys.S, ModifierKeys.Shift);
        public static Shortcut EffectPanelShortcut       = new Shortcut("Toggle Effect Panel", "EffectPanel",       Keys.D1);
        public static Shortcut MaximizePianoRollShortcut = new Shortcut("Maximize Piano Roll", "MaximizePianoRoll", Keys.D1, ModifierKeys.Control);

        public static Shortcut[] QwertyNoteShortcuts = new Shortcut[]
        {
            new Shortcut("QWERTY C0",  "Qwerty00", Keys.Z, Keys.Unknown),
            new Shortcut("QWERTY C#0", "Qwerty01", Keys.S, Keys.Unknown),
            new Shortcut("QWERTY D0",  "Qwerty02", Keys.X, Keys.Unknown),
            new Shortcut("QWERTY D#0", "Qwerty03", Keys.D, Keys.Unknown),
            new Shortcut("QWERTY E0",  "Qwerty04", Keys.C, Keys.Unknown),
            new Shortcut("QWERTY F0",  "Qwerty05", Keys.V, Keys.Unknown),
            new Shortcut("QWERTY F#0", "Qwerty06", Keys.G, Keys.Unknown),
            new Shortcut("QWERTY G0",  "Qwerty07", Keys.B, Keys.Unknown),
            new Shortcut("QWERTY G#0", "Qwerty08", Keys.H, Keys.Unknown),
            new Shortcut("QWERTY A0",  "Qwerty09", Keys.N, Keys.Unknown),
            new Shortcut("QWERTY A#0", "Qwerty10", Keys.J, Keys.Unknown),
            new Shortcut("QWERTY B0",  "Qwerty11", Keys.M, Keys.Unknown),

            new Shortcut("QWERTY C1",  "Qwerty12", Keys.Q,  Keys.Comma),
            new Shortcut("QWERTY C#1", "Qwerty13", Keys.D2, Keys.L),
            new Shortcut("QWERTY D1",  "Qwerty14", Keys.W,  Keys.Period),
            new Shortcut("QWERTY D#1", "Qwerty15", Keys.D3, Keys.SemiColon),
            new Shortcut("QWERTY E1",  "Qwerty16", Keys.E,  Keys.Slash),
            new Shortcut("QWERTY F1",  "Qwerty17", Keys.R,  Keys.Unknown),
            new Shortcut("QWERTY F#1", "Qwerty18", Keys.D5, Keys.Unknown),
            new Shortcut("QWERTY G1",  "Qwerty19", Keys.T,  Keys.Unknown),
            new Shortcut("QWERTY G#1", "Qwerty20", Keys.D6, Keys.Unknown),
            new Shortcut("QWERTY A1",  "Qwerty21", Keys.Y,  Keys.Unknown),
            new Shortcut("QWERTY A#1", "Qwerty22", Keys.D7, Keys.Unknown),
            new Shortcut("QWERTY B1",  "Qwerty23", Keys.U,  Keys.Unknown),

            new Shortcut("QWERTY C2",  "Qwerty24", Keys.I,            Keys.Unknown),
            new Shortcut("QWERTY C#2", "Qwerty25", Keys.D9,           Keys.Unknown),
            new Shortcut("QWERTY D2",  "Qwerty26", Keys.O,            Keys.Unknown),
            new Shortcut("QWERTY D#2", "Qwerty27", Keys.D0,           Keys.Unknown),
            new Shortcut("QWERTY E2",  "Qwerty28", Keys.P,            Keys.Unknown),
            new Shortcut("QWERTY F2",  "Qwerty29", Keys.LeftBracket,  Keys.Unknown),
            new Shortcut("QWERTY F#2", "Qwerty30", Keys.Equal,        Keys.Unknown),
            new Shortcut("QWERTY G2",  "Qwerty31", Keys.RightBracket, Keys.Unknown),
            new Shortcut("QWERTY G#2", "Qwerty32", Keys.Unknown,      Keys.Unknown),
            new Shortcut("QWERTY A2",  "Qwerty33", Keys.Unknown,      Keys.Unknown),
            new Shortcut("QWERTY A#2", "Qwerty34", Keys.Unknown,      Keys.Unknown),
            new Shortcut("QWERTY B2",  "Qwerty35", Keys.Unknown,      Keys.Unknown)
        };

        public static Shortcut[] ActiveChannelShortcuts = new Shortcut[]
        {
            new Shortcut("Set Active Channel 1",  "Channel01", Keys.F1),
            new Shortcut("Set Active Channel 2",  "Channel02", Keys.F2),
            new Shortcut("Set Active Channel 3",  "Channel03", Keys.F3),
            new Shortcut("Set Active Channel 4",  "Channel04", Keys.F4),
            new Shortcut("Set Active Channel 5",  "Channel05", Keys.F5),
            new Shortcut("Set Active Channel 6",  "Channel06", Keys.F6),
            new Shortcut("Set Active Channel 7",  "Channel07", Keys.F7),
            new Shortcut("Set Active Channel 8",  "Channel08", Keys.F8),
            new Shortcut("Set Active Channel 9",  "Channel09", Keys.F9),
            new Shortcut("Set Active Channel 10", "Channel10", Keys.F10),
            new Shortcut("Set Active Channel 11", "Channel11", Keys.F11),
            new Shortcut("Set Active Channel 12", "Channel12", Keys.F12),
            new Shortcut("Set Active Channel 13", "Channel13", Keys.F13),
            new Shortcut("Set Active Channel 14", "Channel14", Keys.F14),
            new Shortcut("Set Active Channel 15", "Channel15", Keys.F15),
            new Shortcut("Set Active Channel 16", "Channel16", Keys.F16),
            new Shortcut("Set Active Channel 17", "Channel17", Keys.F17),
            new Shortcut("Set Active Channel 18", "Channel18", Keys.F18),
            new Shortcut("Set Active Channel 19", "Channel19", Keys.F19),
            new Shortcut("Set Active Channel 20", "Channel20", Keys.F20),
            new Shortcut("Set Active Channel 21", "Channel21", Keys.F21),
            new Shortcut("Set Active Channel 22", "Channel22", Keys.F22),
            new Shortcut("Set Active Channel 23", "Channel23", Keys.F23),
            new Shortcut("Set Active Channel 24", "Channel24", Keys.F24),
        };

        public static Shortcut[] DisplayChannelShortcuts = new Shortcut[]
        {
            new Shortcut("Force Display Channel 1",  "DisplayChannel01", Keys.F1,  ModifierKeys.Control),
            new Shortcut("Force Display Channel 2",  "DisplayChannel02", Keys.F2,  ModifierKeys.Control),
            new Shortcut("Force Display Channel 3",  "DisplayChannel03", Keys.F3,  ModifierKeys.Control),
            new Shortcut("Force Display Channel 4",  "DisplayChannel04", Keys.F4,  ModifierKeys.Control),
            new Shortcut("Force Display Channel 5",  "DisplayChannel05", Keys.F5,  ModifierKeys.Control),
            new Shortcut("Force Display Channel 6",  "DisplayChannel06", Keys.F6,  ModifierKeys.Control),
            new Shortcut("Force Display Channel 7",  "DisplayChannel07", Keys.F7,  ModifierKeys.Control),
            new Shortcut("Force Display Channel 8",  "DisplayChannel08", Keys.F8,  ModifierKeys.Control),
            new Shortcut("Force Display Channel 9",  "DisplayChannel09", Keys.F9,  ModifierKeys.Control),
            new Shortcut("Force Display Channel 10", "DisplayChannel10", Keys.F10, ModifierKeys.Control),
            new Shortcut("Force Display Channel 11", "DisplayChannel11", Keys.F11, ModifierKeys.Control),
            new Shortcut("Force Display Channel 12", "DisplayChannel12", Keys.F12, ModifierKeys.Control),
            new Shortcut("Force Display Channel 13", "DisplayChannel13", Keys.F13, ModifierKeys.Control),
            new Shortcut("Force Display Channel 14", "DisplayChannel14", Keys.F14, ModifierKeys.Control),
            new Shortcut("Force Display Channel 15", "DisplayChannel15", Keys.F15, ModifierKeys.Control),
            new Shortcut("Force Display Channel 16", "DisplayChannel16", Keys.F16, ModifierKeys.Control),
            new Shortcut("Force Display Channel 17", "DisplayChannel17", Keys.F17, ModifierKeys.Control),
            new Shortcut("Force Display Channel 18", "DisplayChannel18", Keys.F18, ModifierKeys.Control),
            new Shortcut("Force Display Channel 19", "DisplayChannel19", Keys.F19, ModifierKeys.Control),
            new Shortcut("Force Display Channel 20", "DisplayChannel20", Keys.F20, ModifierKeys.Control),
            new Shortcut("Force Display Channel 21", "DisplayChannel21", Keys.F21, ModifierKeys.Control),
            new Shortcut("Force Display Channel 22", "DisplayChannel22", Keys.F22, ModifierKeys.Control),
            new Shortcut("Force Display Channel 23", "DisplayChannel23", Keys.F23, ModifierKeys.Control),
            new Shortcut("Force Display Channel 24", "DisplayChannel24", Keys.F24, ModifierKeys.Control),
        };

        // Audio section
        const int DefaultNumBufferedAudioFrames = Platform.IsLinux ? 4 : Platform.IsAndroid ? 2 : 3;
        public static int NumBufferedAudioFrames = DefaultNumBufferedAudioFrames;
        public static int InstrumentStopTime = 1;
        public static bool SquareSmoothVibrato = true;
        public static bool ClampPeriods = true;
        public static bool NoDragSoungWhenPlaying = false;
        public static int MetronomeVolume = 50;
        public static int SeparateChannelsExportTndMode = NesApu.TND_MODE_SINGLE;

        // Mixer section
        public static float GlobalVolume = -2.0f; // in dB

        public struct ExpansionMix
        {
            public ExpansionMix(float v, float t)
            {
                volume = v;
                treble = t;
            }

            public float volume; // in dB
            public float treble; // in dB
        }

        public static ExpansionMix[] ExpansionMixerSettings        = new ExpansionMix[ExpansionType.Count];
        public static ExpansionMix[] DefaultExpansionMixerSettings = new ExpansionMix[ExpansionType.Count]
        {
            new ExpansionMix(0.0f,  -5.0f), // None
            new ExpansionMix(0.0f,  -5.0f), // Vrc6
            new ExpansionMix(0.0f, -15.0f), // Vrc7
            new ExpansionMix(0.0f, -15.0f), // Fds
            new ExpansionMix(0.0f,  -5.0f), // Mmc5
            new ExpansionMix(0.0f, -15.0f), // N163
            new ExpansionMix(0.0f,  -5.0f), // S5B
            new ExpansionMix(0.0f,  -5.0f)  // EPSM
        };

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

        // Piano roll stuff
        public static int SnapResolution = SnapResolutionType.OneBeat;
        public static bool SnapEnabled = true;
        public static bool SnapEffects = false;

        public static void Initialize()
        {
            InitShortcuts();
            Load();
        }

        private static void InitShortcuts()
        {
            AllShortcuts.Sort((c1, c2) => c1.ConfigName.CompareTo(c2.ConfigName));
            DefaultShortcuts = Shortcut.CloneList(AllShortcuts);
        }

        public static void Load()
        {
            var ini = new IniFile();
            ini.Load(GetConfigFileName());

            Version = ini.GetInt("General", "Version", 0);

            // General
            CheckUpdates = ini.GetBool(Version < 2 ? "UI" : "General", "CheckUpdates", true ); // At version 2 (FamiStudio 3.0.0, changed section)
            ShowTutorial = ini.GetBool(Version < 2 ? "UI" : "General", "ShowTutorial", true ); // At version 2 (FamiStudio 3.0.0, changed section)
            ClearUndoRedoOnSave = ini.GetBool("General", "ClearUndoRedoOnSave", true);
            RewindAfterPlay = ini.GetBool("General", "RewindAfterPlay", false);
            OpenLastProjectOnStart = ini.GetBool("General", "OpenLastProjectOnStart", true);
            AutoSaveCopy = ini.GetBool("General", "AutoSaveCopy", true);
            PatternNamePrefix = ini.GetString("General", "PatternNamePrefix", "Pattern ");
            PatternNameNumDigits = ini.GetInt("General", "PatternNameNumDigits", 1);
            LastProjectFile = OpenLastProjectOnStart ? ini.GetString("General", "LastProjectFile", "") : "";

            // UI
            DpiScaling = ini.GetInt("UI", "DpiScaling", 0);
            TimeFormat = ini.GetInt("UI", "TimeFormat", 0);
            FollowMode = ini.GetInt("UI", "FollowMode", FollowModeContinuous);
            FollowSync = ini.GetInt("UI", "FollowSync", FollowSyncBoth);
            ScrollBars = Version < 3 ? (ini.GetBool("UI", "ShowScrollBars", false) ? ScrollBarsThin : ScrollBarsNone) : ini.GetInt("UI", "ScrollBars", ScrollBarsNone);
            IdealSequencerSize = ini.GetInt("UI", "IdealSequencerSize", 25);
            AllowSequencerVerticalScroll = ini.GetBool("UI", "AllowSequencerVerticalScroll", false);
            ShowImplicitStopNotes = ini.GetBool("UI", "ShowImplicitStopNotes", false);
            ShowRegisterViewer = ini.GetBool("UI", "ShowRegisterViewer", Platform.IsDesktop);
            UseOSDialogs = ini.GetBool("UI", "UseOSDialogs", !Platform.IsLinux);

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
            NumBufferedAudioFrames = ini.GetInt("Audio", "NumBufferedFrames", DefaultNumBufferedAudioFrames);
            InstrumentStopTime = ini.GetInt("Audio", "InstrumentStopTime", 2);
            SquareSmoothVibrato = ini.GetBool("Audio", "SquareSmoothVibrato", true);
            ClampPeriods = ini.GetBool("Audio", "ClampPeriods", true);
            NoDragSoungWhenPlaying = ini.GetBool("Audio", "NoDragSoungWhenPlaying", false);
            MetronomeVolume = ini.GetInt("Audio", "MetronomeVolume", 50);
            SeparateChannelsExportTndMode = ini.GetInt("Audio", "SeparateChannelsExportTndMode", NesApu.TND_MODE_SINGLE);

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
            GlobalVolume = ini.GetFloat("Mixer", "GlobalVolume", -3.0f);

            Array.Copy(DefaultExpansionMixerSettings, ExpansionMixerSettings, ExpansionMixerSettings.Length);

            for (int i = 0; i < ExpansionType.Count; i++)
            {
                var section = "Mixer" + ExpansionType.ShortNames[i];
                ExpansionMixerSettings[i].volume = ini.GetFloat(section, "Volume", DefaultExpansionMixerSettings[i].volume);
                ExpansionMixerSettings[i].treble = ini.GetFloat(section, "Treble", DefaultExpansionMixerSettings[i].treble);
            }

            // At version 7 (FamiStudio 4.1.0) we allowed configuring all the keys.
            if (Version >= 8)
            {
                foreach (var shortcut in AllShortcuts)
                {
                    var shortcutStr1 = ini.GetString("Keys", shortcut.ConfigName, null);
                    if (shortcutStr1 != null)
                        shortcut.FromConfigString(shortcutStr1, 0);

                    if (shortcut.AllowTwoShortcuts)
                    {
                        var shortcutStr2 = ini.GetString("Keys", shortcut.ConfigName + "_Alt", null);
                        if (shortcutStr2 != null)
                            shortcut.FromConfigString(shortcutStr2, 1);
                    }
                }
            }

            if (Array.IndexOf(global::FamiStudio.DpiScaling.GetAvailableScalings(), DpiScaling) < 0)
                DpiScaling = 0;

            InstrumentStopTime = Utils.Clamp(InstrumentStopTime, 0, 10);

            if (MidiDevice == null)
                MidiDevice = "";
            if (!Directory.Exists(LastInstrumentFolder))
                LastInstrumentFolder = "";
            if (!Directory.Exists(LastSampleFolder))
                LastSampleFolder = "";
            if (!Directory.Exists(LastExportFolder))
                LastExportFolder = "";

            // Try to point to the demo songs initially.
            if (string.IsNullOrEmpty(LastFileFolder) || !Directory.Exists(LastFileFolder))
            {
                var appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var demoSongsPath = Path.Combine(appPath, "Demo Songs");
                LastFileFolder = Directory.Exists(demoSongsPath) ? demoSongsPath : "";
            }

            // Clamp to something reasonable.
            NumBufferedAudioFrames = Utils.Clamp(NumBufferedAudioFrames, 2, 16);

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

            // Piano roll section
            SnapResolution = Utils.Clamp(ini.GetInt("PianoRoll", "SnapResolution", SnapResolutionType.OneBeat), SnapResolutionType.Min, SnapResolutionType.Max);
            SnapEnabled = ini.GetBool("PianoRoll", "SnapEnabled", true);
            SnapEffects = ini.GetBool("PianoRoll", "SnapEffects", false);

            // At 4.0.0, we fixed an issue where the snapping was not saved properly. Reset.
            if (Version < 7)
            {
                SnapResolution = SnapResolutionType.OneBeat;
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

        public static void Save()
        {
            // Never same settings in command line, we dont have the proper
            // QWERTY keys set up since GLFW isnt initialized.
            if (Platform.IsCommandLine)
                return;

            var ini = new IniFile();

            // General
            ini.SetInt("General", "Version", SettingsVersion);
            ini.SetBool("General", "CheckUpdates", CheckUpdates);
            ini.SetBool("General", "ShowTutorial", ShowTutorial);
            ini.SetBool("General", "ClearUndoRedoOnSave", ClearUndoRedoOnSave);
            ini.SetBool("General", "RewindAfterPlay", RewindAfterPlay);
            ini.SetBool("General", "OpenLastProjectOnStart", OpenLastProjectOnStart);
            ini.SetString("General", "LastProjectFile", OpenLastProjectOnStart ? LastProjectFile : "");
            ini.SetString("General", "PatternNamePrefix", PatternNamePrefix);
            ini.SetInt("General", "PatternNameNumDigits", PatternNameNumDigits);
            ini.SetBool("General", "AutoSaveCopy", AutoSaveCopy);

            // UI
            ini.SetInt("UI", "DpiScaling", DpiScaling);
            ini.SetInt("UI", "TimeFormat", TimeFormat);
            ini.SetInt("UI", "FollowMode", FollowMode);
            ini.SetInt("UI", "FollowSync", FollowSync);
            ini.SetInt("UI", "ScrollBars", ScrollBars);
            ini.SetInt("UI", "IdealSequencerSize", IdealSequencerSize);
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
            ini.SetInt("Audio", "NumBufferedFrames", NumBufferedAudioFrames);
            ini.SetInt("Audio", "InstrumentStopTime", InstrumentStopTime);
            ini.SetBool("Audio", "SquareSmoothVibrato", SquareSmoothVibrato);
            ini.SetBool("Audio", "ClampPeriods", ClampPeriods);
            ini.SetBool("Audio", "NoDragSoungWhenPlaying", NoDragSoungWhenPlaying);
            ini.SetInt("Audio", "MetronomeVolume", MetronomeVolume);
            ini.SetInt("Audio", "SeparateChannelsExportTndMode", SeparateChannelsExportTndMode);

            // Mixer
            ini.SetFloat("Mixer", "GlobalVolume", GlobalVolume);

            for (int i = 0; i < ExpansionType.Count; i++)
            {
                var section = "Mixer" + ExpansionType.ShortNames[i];
                ini.SetFloat(section, "Volume", ExpansionMixerSettings[i].volume);
                ini.SetFloat(section, "Treble", ExpansionMixerSettings[i].treble);
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
                if (shortcut.AllowTwoShortcuts)
                    ini.SetString("Keys", shortcut.ConfigName + "_Alt", shortcut.ToConfigString(1));
            }

            // Mobile
            ini.SetBool("Mobile", "AllowVibration", AllowVibration);
            ini.SetBool("Mobile", "ForceLandscape", ForceLandscape);

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
    }
}
