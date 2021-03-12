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
        public const int SettingsVersion = 1;

        // Constants for follow.
        public const int FollowModeJump       = 0;
        public const int FollowModeContinuous = 1;

        public const int FollowSyncSequencer = 0;
        public const int FollowSyncPianoRoll = 1;
        public const int FollowSyncBoth      = 2;

        // General section.
        public static int Version = SettingsVersion;

        // User Interface section
        public static int DpiScaling = 0;
        public static int TimeFormat = 0;
        public static bool CheckUpdates = true;
        public static bool ShowPianoRollViewRange = true;
        public static bool TrackPadControls = false;
        public static bool ReverseTrackPad = false;
        public static int TrackPadMoveSensitity = 1;
        public static int TrackPadZoomSensitity = 8;
        public static int FollowMode = 0;
        public static int FollowSync = 0;
        public static bool ShowTutorial = true;
        public static bool ShowNoteLabels = true;

        // QWERTY section, 3 octaves, 12 notes (+ stop note), up to 2 assignments per key.
        //  - On Windows, these are the numerical values of System.Windows.Forms.Keys enum for a regular US keyboard.
        //  - On GTK, these are the raw keycodes (MATTT : Verify what its gonna be for real).
        // MATTT : Make sure the stop note key is configurable too.
        public static readonly int[,] DefaultQwertyKeys = new int[37, 2]
        {
            // Stop note
            { 49, -1 },

            // Octave 1
            { 90, -1 },
            { 83, -1 },
            { 88, -1 },
            { 68, -1 },
            { 67, -1 },
            { 86, -1 },
            { 71, -1 },
            { 66, -1 },
            { 72, -1 },
            { 78, -1 },
            { 74, -1 },
            { 77, -1 },

            // Octave 2
            { 81, 188 },
            { 50, 76  },
            { 87, 190 },
            { 51, 186 },
            { 69, 191 },
            { 82, -1 },
            { 53, -1 },
            { 84, -1 },
            { 54, -1 },
            { 89, -1 },
            { 55, -1 },
            { 85, -1 },

            // Octave 3
            { 73, -1 },
            { 57, -1 },
            { 79, -1 },
            { 48, -1 },
            { 80, -1 },
            { 219, -1 },
            { 187, -1 },
            { 221, -1 },
            { -1, -1 },
            { -1, -1 },
            { -1, -1 },
            { -1, -1 }
        };

        public static int[,] QwertyKeys = new int[37, 2];
        public static Dictionary<int, int> KeyCodeToNoteMap = new Dictionary<int, int>();

        // Audio section
#if FAMISTUDIO_LINUX
        const int DefaultNumBufferedAudioFrames = 4; // ALSA seems to like to have one extra buffer.
#else
        const int DefaultNumBufferedAudioFrames = 3;
#endif
        public static int NumBufferedAudioFrames = DefaultNumBufferedAudioFrames;
        public static int InstrumentStopTime = 2;
        public static bool SquareSmoothVibrato = true;
        public static bool NoDragSoungWhenPlaying = false;

        // MIDI section
        public static string MidiDevice = "";

        // Last used folders
        public static string LastFileFolder = "";
        public static string LastInstrumentFolder  = "";
        public static string LastSampleFolder = "";
        public static string LastExportFolder = "";

        // Misc
        public static string FFmpegExecutablePath = "";

        public static void Load()
        {
            var ini = new IniFile();
            ini.Load(GetConfigFileName());

            Version = ini.GetInt("General", "Version", 0);
            ShowTutorial = ini.GetBool("UI", "ShowTutorial", true);
            DpiScaling = ini.GetInt("UI", "DpiScaling", 0);
            TimeFormat = ini.GetInt("UI", "TimeFormat", 0);
            FollowMode = ini.GetInt("UI", "FollowMode", FollowModeContinuous);
            FollowSync = ini.GetInt("UI", "FollowSync", FollowSyncBoth);
            CheckUpdates = ini.GetBool("UI", "CheckUpdates", true);
            ShowNoteLabels = ini.GetBool("UI", "ShowNoteLabels", true);
            ShowPianoRollViewRange = ini.GetBool("UI", "ShowPianoRollViewRange", true);
            TrackPadControls = ini.GetBool("UI", "TrackPadControls", false);
            ReverseTrackPad = ini.GetBool("UI", "ReverseTrackPad", false);
            TrackPadMoveSensitity = ini.GetInt("UI", "TrackPadMoveSensitity", 1);
            TrackPadZoomSensitity = ini.GetInt("UI", "TrackPadZoomSensitity", 8);
            NumBufferedAudioFrames = ini.GetInt("Audio", "NumBufferedFrames", DefaultNumBufferedAudioFrames);
            InstrumentStopTime = ini.GetInt("Audio", "InstrumentStopTime", 2);
            SquareSmoothVibrato = ini.GetBool("Audio", "SquareSmoothVibrato", true);
            NoDragSoungWhenPlaying = ini.GetBool("Audio", "NoDragSoungWhenPlaying", false);
            MidiDevice = ini.GetString("MIDI", "Device", "");
            LastFileFolder = ini.GetString("Folders", "LastFileFolder", "");
            LastInstrumentFolder = ini.GetString("Folders", "LastInstrumentFolder", "");
            LastSampleFolder = ini.GetString("Folders", "LastSampleFolder", "");
            LastExportFolder = ini.GetString("Folders", "LastExportFolder", "");
            FFmpegExecutablePath = ini.GetString("FFmpeg", "ExecutablePath", "");

            Array.Copy(DefaultQwertyKeys, QwertyKeys, DefaultQwertyKeys.Length);

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
                var note   = (idx - 1) % 12;

                var keyName0 = $"Octave{octave}Note{note}";
                var keyName1 = $"Octave{octave}Note{note}Alt";

                if (ini.HasKey("QWERTY", keyName0))
                    QwertyKeys[idx, 0] = ini.GetInt("QWERTY", keyName0, QwertyKeys[idx, 0]);
                if (ini.HasKey("QWERTY", keyName1))
                    QwertyKeys[idx, 1] = ini.GetInt("QWERTY", keyName1, QwertyKeys[idx, 1]);
            }

            UpdateKeyCodeMaps();

            if (DpiScaling != 100 && DpiScaling != 150 && DpiScaling != 200)
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

#if FAMISTUDIO_LINUX || FAMISTUDIO_MACOS
            // Linux or Mac is more likely to have standard path for ffmpeg.
            if (string.IsNullOrEmpty(FFmpegExecutablePath) || !File.Exists(FFmpegExecutablePath))
            {
                if (File.Exists("/usr/bin/ffmpeg"))
                    FFmpegExecutablePath = "/usr/bin/ffmpeg";
                else if (File.Exists("/usr/local/bin/ffmpeg"))
                    FFmpegExecutablePath = "/usr/local/bin/ffmpeg";
                else
                    FFmpegExecutablePath = "ffmpeg"; // Hope for the best!
            }
#endif

            // No deprecation at the moment.
            Version = SettingsVersion;
        }

        public static void Save()
        {
            var ini = new IniFile();

            ini.SetInt("General", "Version", SettingsVersion);
            ini.SetBool("UI", "ShowTutorial", ShowTutorial);
            ini.SetInt("UI", "DpiScaling", DpiScaling);
            ini.SetInt("UI", "TimeFormat", TimeFormat);
            ini.SetInt("UI", "FollowMode", FollowMode);
            ini.SetInt("UI", "FollowSync", FollowSync);
            ini.SetBool("UI", "CheckUpdates", CheckUpdates);
            ini.SetBool("UI", "ShowNoteLabels", ShowNoteLabels);
            ini.SetBool("UI", "ShowPianoRollViewRange", ShowPianoRollViewRange);
            ini.SetBool("UI", "TrackPadControls", TrackPadControls);
            ini.SetInt("UI", "TrackPadMoveSensitity", TrackPadMoveSensitity);
            ini.SetInt("UI", "TrackPadZoomSensitity", TrackPadZoomSensitity);
            ini.SetBool("UI", "ReverseTrackPad", ReverseTrackPad);
            ini.SetInt("Audio", "NumBufferedFrames", NumBufferedAudioFrames);
            ini.SetInt("Audio", "InstrumentStopTime", InstrumentStopTime);
            ini.SetBool("Audio", "SquareSmoothVibrato", SquareSmoothVibrato);
            ini.SetBool("Audio", "NoDragSoungWhenPlaying", NoDragSoungWhenPlaying);
            ini.SetString("MIDI", "Device", MidiDevice);
            ini.SetString("Folders", "LastFileFolder", LastFileFolder);
            ini.SetString("Folders", "LastInstrumentFolder", LastInstrumentFolder);
            ini.SetString("Folders", "LastSampleFolder", LastSampleFolder);
            ini.SetString("Folders", "LastExportFolder", LastExportFolder);
            ini.SetString("FFmpeg", "ExecutablePath", FFmpegExecutablePath);

            // Stop note.
            {
                if (QwertyKeys[0, 0] >= 0)
                    ini.SetInt("QWERTY", "StopNote", QwertyKeys[0, 0]);
                if (QwertyKeys[0, 1] >= 0)
                    ini.SetInt("QWERTY", "StopNoteAlt", QwertyKeys[0, 1]);
            }

            // Regular notes.
            for (int idx = 1; idx < QwertyKeys.GetLength(0); idx++)
            {
                var octave = (idx - 1) / 12;
                var note   = (idx - 1) % 12;

                var keyName0 = $"Octave{octave}Note{note}";
                var keyName1 = $"Octave{octave}Note{note}Alt";

                if (QwertyKeys[idx, 0] >= 0)
                    ini.SetInt("QWERTY", keyName0, QwertyKeys[idx, 0]);
                if (QwertyKeys[idx, 1] >= 0)
                    ini.SetInt("QWERTY", keyName1, QwertyKeys[idx, 1]);
            }

            Directory.CreateDirectory(GetConfigFilePath());

            ini.Save(GetConfigFileName());
        }

        public static void UpdateKeyCodeMaps()
        {
            KeyCodeToNoteMap.Clear();

            for (int idx = 1; idx < QwertyKeys.GetLength(0); idx++)
            {
                var k0 = QwertyKeys[idx, 0];
                var k1 = QwertyKeys[idx, 0];

                if (k0 >= 0)
                    KeyCodeToNoteMap[k0] = idx;
                if (k1 >= 0)
                    KeyCodeToNoteMap[k1] = idx;
            }
        }

        private static string GetConfigFilePath()
        {
#if FAMISTUDIO_WINDOWS
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FamiStudio");
#elif FAMISTUDIO_LINUX
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config/FamiStudio");
#else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Application Support/FamiStudio");
#endif
        }

        private static string GetConfigFileName()
        {
            return Path.Combine(GetConfigFilePath(), "FamiStudio.ini");
        }
    }
}
