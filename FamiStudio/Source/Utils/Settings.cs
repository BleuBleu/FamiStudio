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
        public const int SettingsVersion = 7;
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
        public static bool AutoSaveCopy = true;
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

        public struct QwertyKeyAssignment
        {
            public QwertyKeyAssignment(Keys k, int scan = -1)
            {
                Key = k;
                Scancode = scan;
            }

            public override string ToString()
            {
                return $"{Key} ({Scancode})";
            }

            public void Clear()
            {
                Key = Keys.Unknown;
                Scancode = 0;
            }

            public bool IsValid => Key != Keys.Unknown && (int)Key != 0 && Scancode >= 0;

            public Keys Key;
            public int  Scancode;
        }

        // QWERTY section, 3 octaves, 12 notes (+ stop note), up to 2 assignments per key.
        public static readonly Keys[,] DefaultQwertyKeys = new Keys[37, 2]
        {
            // Stop note
            { Keys.D1,           Keys.Unknown   },

            // Octave 1
            { Keys.Z,            Keys.Unknown   },
            { Keys.S,            Keys.Unknown   },
            { Keys.X,            Keys.Unknown   },
            { Keys.D,            Keys.Unknown   },
            { Keys.C,            Keys.Unknown   },
            { Keys.V,            Keys.Unknown   },
            { Keys.G,            Keys.Unknown   },
            { Keys.B,            Keys.Unknown   },
            { Keys.H,            Keys.Unknown   },
            { Keys.N,            Keys.Unknown   },
            { Keys.J,            Keys.Unknown   },
            { Keys.M,            Keys.Unknown   },

            // Octave 2
            { Keys.Q,            Keys.Comma     },
            { Keys.D2,           Keys.L         },
            { Keys.W,            Keys.Period    },
            { Keys.D3,           Keys.SemiColon }, 
            { Keys.E,            Keys.Slash     },
            { Keys.R,            Keys.Unknown   },
            { Keys.D5,           Keys.Unknown   },
            { Keys.T,            Keys.Unknown   },
            { Keys.D6,           Keys.Unknown   },
            { Keys.Y,            Keys.Unknown   },
            { Keys.D7,           Keys.Unknown   },
            { Keys.U,            Keys.Unknown   },

            // Octave 3
            { Keys.I,            Keys.Unknown   },
            { Keys.D9,           Keys.Unknown   },
            { Keys.O,            Keys.Unknown   },
            { Keys.D0,           Keys.Unknown   },
            { Keys.P,            Keys.Unknown   },
            { Keys.LeftBracket,  Keys.Unknown   }, 
            { Keys.Equal,        Keys.Unknown   }, 
            { Keys.RightBracket, Keys.Unknown   }, 
            { Keys.Unknown,      Keys.Unknown   },
            { Keys.Unknown,      Keys.Unknown   },
            { Keys.Unknown,      Keys.Unknown   },
            { Keys.Unknown,      Keys.Unknown   }
        };

        public static int[,] DefaultQwertyScancodes = new int[37, 2];
        public static int[,] QwertyKeys = new int[37, 2];
        public static Dictionary<int, int> ScanCodeToNoteMap = new Dictionary<int, int>();

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

        // Piano roll stuff
        public static int SnapResolution = SnapResolutionType.OneBeat;
        public static bool SnapEnabled = true;
        public static bool SnapEffects = false;

        public static void Initialize()
        {
            if (Platform.IsDesktop)
            {
                for (int i = 0; i < DefaultQwertyKeys.GetLength(0); i++)
                {
                    DefaultQwertyScancodes[i, 0] = DefaultQwertyKeys[i, 0] == Keys.Unknown ? -1 : Platform.GetKeyScancode(DefaultQwertyKeys[i, 0]);
                    DefaultQwertyScancodes[i, 1] = DefaultQwertyKeys[i, 1] == Keys.Unknown ? -1 : Platform.GetKeyScancode(DefaultQwertyKeys[i, 1]);
                }
            }

            Load();
        }

        public static void Load()
        {
            var ini = new IniFile();
            ini.Load(GetConfigFileName());

            Version = ini.GetInt("General", "Version", 0);

            // General
            CheckUpdates = ini.GetBool(Version < 2 ? "UI" : "General", "CheckUpdates",     true ); // At version 2 (FamiStudio 3.0.0, changed section)
            ShowTutorial = ini.GetBool(Version < 2 ? "UI" : "General", "ShowTutorial",     true ); // At version 2 (FamiStudio 3.0.0, changed section)
            ClearUndoRedoOnSave = ini.GetBool("General", "ClearUndoRedoOnSave", true);
            OpenLastProjectOnStart = ini.GetBool("General", "OpenLastProjectOnStart", true);
            AutoSaveCopy = ini.GetBool("General", "AutoSaveCopy", true);
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

            // QWERTY
            Array.Copy(DefaultQwertyScancodes, QwertyKeys, DefaultQwertyScancodes.Length);

            // At version 7 (FamiStudio 4.0.0) we changed how the QWERTY keys are saved.
            if (Version >= 7)
            {
                // Stop note.
                {
                    if (ini.HasKey("QWERTY", "StopNote"))
                        QwertyKeys[0, 0] = ini.GetInt("QWERTY", "StopNote", QwertyKeys[0, 0]);
                    if (ini.HasKey("QWERTY", "StopNoteAlt"))
                        QwertyKeys[0, 1] = ini.GetInt("QWERTY", "StopNoteAlt", QwertyKeys[0, 1]);
                }

                // Regular notes.
                for (int idx = 1; idx < QwertyKeys.GetLength(0); idx++)
                {
                    var octave = (idx - 1) / 12;
                    var note = (idx - 1) % 12;
                    var keyName1 = $"Octave{octave}Note{note}";
                    var keyName2 = $"Octave{octave}Note{note}Alt";

                    if (ini.HasKey("QWERTY", keyName1))
                        QwertyKeys[idx, 0] = ini.GetInt("QWERTY", keyName1, QwertyKeys[idx, 0]);
                    if (ini.HasKey("QWERTY", keyName2))
                        QwertyKeys[idx, 1] = ini.GetInt("QWERTY", keyName2, QwertyKeys[idx, 1]);
                }
            }

            UpdateKeyCodeMaps();

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
            ini.SetBool("General", "OpenLastProjectOnStart", OpenLastProjectOnStart);
            ini.SetString("General", "LastProjectFile", OpenLastProjectOnStart ? LastProjectFile : "");
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

            // QWERTY
            // Stop note.
            {
                ini.SetInt("QWERTY", "StopNote", QwertyKeys[0, 0]);
                ini.SetInt("QWERTY", "StopNoteAlt", QwertyKeys[0, 1]);
            }

            // Regular notes.
            for (int idx = 1; idx < QwertyKeys.GetLength(0); idx++)
            {
                var octave = (idx - 1) / 12;
                var note = (idx - 1) % 12;
                var keyName0 = $"Octave{octave}Note{note}";
                var keyName1 = $"Octave{octave}Note{note}Alt";

                ini.SetInt("QWERTY", keyName0, QwertyKeys[idx, 0]);
                ini.SetInt("QWERTY", keyName1, QwertyKeys[idx, 1]);
            }

            // Mobile
            ini.SetBool("Mobile", "AllowVibration", AllowVibration);

            // Piano roll section
            ini.SetInt("PianoRoll", "SnapResolution", SnapResolution);
            ini.SetBool("PianoRoll", "SnapEnabled", SnapEnabled);
            ini.SetBool("PianoRoll", "SnapEffects", SnapEffects);

            Directory.CreateDirectory(GetConfigFilePath());

            ini.Save(GetConfigFileName());
        }

        public static void UpdateKeyCodeMaps()
        {
            ScanCodeToNoteMap.Clear();

            for (int idx = 1; idx < QwertyKeys.GetLength(0); idx++)
            {
                var k0 = QwertyKeys[idx, 0];
                var k1 = QwertyKeys[idx, 1];

                if (k0 >= 0)
                    ScanCodeToNoteMap[k0] = idx;
                if (k1 >= 0)
                    ScanCodeToNoteMap[k1] = idx;
            }
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
