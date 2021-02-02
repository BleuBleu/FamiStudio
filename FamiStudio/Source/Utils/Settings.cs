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
        public static int FollowMode = 0;
        public static int FollowSync = 0;
        public static bool ShowTutorial = true;
        public static bool ShowNoteLabels = true;

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

            Directory.CreateDirectory(GetConfigFilePath());

            ini.Save(GetConfigFileName());
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
