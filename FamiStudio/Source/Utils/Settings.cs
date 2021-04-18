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
        public const int SettingsVersion = 2;

        // Constants for follow.
        public const int FollowModeJump       = 0;
        public const int FollowModeContinuous = 1;

        public const int FollowSyncSequencer = 0;
        public const int FollowSyncPianoRoll = 1;
        public const int FollowSyncBoth      = 2;

        // General section.
        public static int Version = SettingsVersion;
        public static bool CheckUpdates = true;
        public static bool TrackPadControls = false;
        public static bool ShowTutorial = true;

        // User Interface section
        public static int DpiScaling = 0;
        public static int TimeFormat = 0;
        public static bool ShowPianoRollViewRange = true;
        public static bool ReverseTrackPad = false;
        public static int TrackPadMoveSensitity = 1;
        public static int TrackPadZoomSensitity = 8;
        public static int FollowMode = 0;
        public static int FollowSync = 0;
        public static bool ShowNoteLabels = true;
        public static bool ShowScrollBars = false;
        public static bool ShowOscilloscope = false;
        public static bool ForceCompactSequencer = false;

        // QWERTY section, 3 octaves, 12 notes (+ stop note), up to 2 assignments per key.
#if FAMISTUDIO_WINDOWS
        public static readonly int[,] DefaultQwertyKeys = new int[37, 2]
        {
            // Stop note
            { (int)System.Windows.Forms.Keys.D1, -1 },

            // Octave 1
            { (int)System.Windows.Forms.Keys.Z, -1 },
            { (int)System.Windows.Forms.Keys.S, -1 },
            { (int)System.Windows.Forms.Keys.X, -1 },
            { (int)System.Windows.Forms.Keys.D, -1 },
            { (int)System.Windows.Forms.Keys.C, -1 },
            { (int)System.Windows.Forms.Keys.V, -1 },
            { (int)System.Windows.Forms.Keys.G, -1 },
            { (int)System.Windows.Forms.Keys.B, -1 },
            { (int)System.Windows.Forms.Keys.H, -1 },
            { (int)System.Windows.Forms.Keys.N, -1 },
            { (int)System.Windows.Forms.Keys.J, -1 },
            { (int)System.Windows.Forms.Keys.M, -1 },

            // Octave 2
            { (int)System.Windows.Forms.Keys.Q,  (int)System.Windows.Forms.Keys.Oemcomma,  },
            { (int)System.Windows.Forms.Keys.D2, (int)System.Windows.Forms.Keys.L,         },
            { (int)System.Windows.Forms.Keys.W,  (int)System.Windows.Forms.Keys.OemPeriod, },
            { (int)System.Windows.Forms.Keys.D3, (int)System.Windows.Forms.Keys.Oem1,      },
            { (int)System.Windows.Forms.Keys.E,  (int)System.Windows.Forms.Keys.Oem2,      },
            { (int)System.Windows.Forms.Keys.R,  -1 },
            { (int)System.Windows.Forms.Keys.D5, -1 },
            { (int)System.Windows.Forms.Keys.T,  -1 },
            { (int)System.Windows.Forms.Keys.D6, -1 },
            { (int)System.Windows.Forms.Keys.Y,  -1 },
            { (int)System.Windows.Forms.Keys.D7, -1 },
            { (int)System.Windows.Forms.Keys.U,  -1 },

            // Octave 3
            { (int)System.Windows.Forms.Keys.I,       -1 },
            { (int)System.Windows.Forms.Keys.D9,      -1 },
            { (int)System.Windows.Forms.Keys.O,       -1 },
            { (int)System.Windows.Forms.Keys.D0,      -1 },
            { (int)System.Windows.Forms.Keys.P,       -1 },
            { (int)System.Windows.Forms.Keys.Oem4,    -1 },
            { (int)System.Windows.Forms.Keys.Oemplus, -1 },
            { (int)System.Windows.Forms.Keys.Oem6,    -1 },
            { -1, -1 },
            { -1, -1 },
            { -1, -1 },
            { -1, -1 }
        };
#else
        public static readonly int[,] DefaultQwertyKeys = new int[37, 2]
        {
            // Stop note
            { (int)Gdk.Key.Key_1,        -1 },

            // Octave 1
            { (int)Gdk.Key.z,            -1 },
            { (int)Gdk.Key.s,            -1 },
            { (int)Gdk.Key.x,            -1 },
            { (int)Gdk.Key.d,            -1 },
            { (int)Gdk.Key.c,            -1 },
            { (int)Gdk.Key.v,            -1 },
            { (int)Gdk.Key.g,            -1 },
            { (int)Gdk.Key.b,            -1 },
            { (int)Gdk.Key.h,            -1 },
            { (int)Gdk.Key.n,            -1 },
            { (int)Gdk.Key.j,            -1 },
            { (int)Gdk.Key.m,            -1 },

            // Octave 2
            { (int)Gdk.Key.q,            (int)Gdk.Key.comma     },
            { (int)Gdk.Key.Key_2,        (int)Gdk.Key.l         },
            { (int)Gdk.Key.w,            (int)Gdk.Key.period    },
            { (int)Gdk.Key.Key_3,        (int)Gdk.Key.semicolon },
            { (int)Gdk.Key.e,            (int)Gdk.Key.slash     },
            { (int)Gdk.Key.r,            -1 },
            { (int)Gdk.Key.Key_5,        -1 },
            { (int)Gdk.Key.t,            -1 },
            { (int)Gdk.Key.Key_6,        -1 },
            { (int)Gdk.Key.y,            -1 },
            { (int)Gdk.Key.Key_7,        -1 },
            { (int)Gdk.Key.u,            -1 },

            // Octave 3
            { (int)Gdk.Key.i,            -1 },
            { (int)Gdk.Key.Key_9,        -1 },
            { (int)Gdk.Key.o,            -1 },
            { (int)Gdk.Key.Key_0,        -1 },
            { (int)Gdk.Key.p,            -1 },
            { (int)Gdk.Key.bracketleft,  -1 },
            { (int)Gdk.Key.equal,        -1 },
            { (int)Gdk.Key.bracketright, -1 },
            { -1, -1 },
            { -1, -1 },
            { -1, -1 },
            { -1, -1 }
        };
#endif

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

        // Mixer section
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

        // Some of these (-8.87, 8800) were the default values in Nes_Snd_Emu, review eventually.
        // FamiTracker by default has (-24, 12000) respectively.
        public static ExpansionMix[] ExpansionMixerSettings        = new ExpansionMix[ExpansionType.Count];
        public static ExpansionMix[] DefaultExpansionMixerSettings = new ExpansionMix[ExpansionType.Count]
        {
            new ExpansionMix(0.0f,  -8.0f), // None
            new ExpansionMix(0.0f,  -8.0f), // Vrc6
            new ExpansionMix(0.0f, -15.0f), // Vrc7
            new ExpansionMix(0.0f, -15.0f), // Fds
            new ExpansionMix(0.0f,  -8.0f), // Mmc5
            new ExpansionMix(0.0f, -15.0f), // N163
            new ExpansionMix(0.0f,  -8.0f)  // S5B
        };

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

            // General
            CheckUpdates     = ini.GetBool(Version < 2 ? "UI" : "General", "CheckUpdates",     true ); // At version 2 (FamiStudio 3.0.0, changed section)
            TrackPadControls = ini.GetBool(Version < 2 ? "UI" : "General", "TrackPadControls", false); // At version 2 (FamiStudio 3.0.0, changed section)
            ShowTutorial     = ini.GetBool(Version < 2 ? "UI" : "General", "ShowTutorial",     true ); // At version 2 (FamiStudio 3.0.0, changed section)

            // UI
            DpiScaling = ini.GetInt("UI", "DpiScaling", 0);
            TimeFormat = ini.GetInt("UI", "TimeFormat", 0);
            FollowMode = ini.GetInt("UI", "FollowMode", FollowModeContinuous);
            FollowSync = ini.GetInt("UI", "FollowSync", FollowSyncBoth);
            ShowNoteLabels = ini.GetBool("UI", "ShowNoteLabels", true);
            ShowScrollBars = ini.GetBool("UI", "ShowScrollBars", false);
            ShowOscilloscope = ini.GetBool("UI", "ShowOscilloscope", true);
            ForceCompactSequencer = ini.GetBool("UI", "ForceCompactSequencer", false);
            ShowPianoRollViewRange = ini.GetBool("UI", "ShowPianoRollViewRange", true);
            ReverseTrackPad = ini.GetBool("UI", "ReverseTrackPad", false);
            TrackPadMoveSensitity = ini.GetInt("UI", "TrackPadMoveSensitity", 1);
            TrackPadZoomSensitity = ini.GetInt("UI", "TrackPadZoomSensitity", 8);

            // Audio
            NumBufferedAudioFrames = ini.GetInt("Audio", "NumBufferedFrames", DefaultNumBufferedAudioFrames);
            InstrumentStopTime = ini.GetInt("Audio", "InstrumentStopTime", 2);
            SquareSmoothVibrato = ini.GetBool("Audio", "SquareSmoothVibrato", true);
            NoDragSoungWhenPlaying = ini.GetBool("Audio", "NoDragSoungWhenPlaying", false);

            // MIDI
            MidiDevice = ini.GetString("MIDI", "Device", "");

            // Folders
            LastFileFolder = ini.GetString("Folders", "LastFileFolder", "");
            LastInstrumentFolder = ini.GetString("Folders", "LastInstrumentFolder", "");
            LastSampleFolder = ini.GetString("Folders", "LastSampleFolder", "");
            LastExportFolder = ini.GetString("Folders", "LastExportFolder", "");

            // FFmpeg
            FFmpegExecutablePath = ini.GetString("FFmpeg", "ExecutablePath", "");

            // Mixer.
            Array.Copy(DefaultExpansionMixerSettings, ExpansionMixerSettings, ExpansionMixerSettings.Length);

            for (int i = 0; i < ExpansionType.Count; i++)
            {
                var section = "Mixer" + ExpansionType.ShortNames[i];
                ExpansionMixerSettings[i].volume = ini.GetFloat(section, "Volume", DefaultExpansionMixerSettings[i].volume);
                ExpansionMixerSettings[i].treble = ini.GetFloat(section, "Treble", DefaultExpansionMixerSettings[i].treble);
            }

            // QWERTY
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

            // General
            ini.SetInt("General", "Version", SettingsVersion);
            ini.SetBool("General", "CheckUpdates", CheckUpdates);
            ini.SetBool("General", "TrackPadControls", TrackPadControls);
            ini.SetBool("General", "ShowTutorial", ShowTutorial);

            // UI
            ini.SetInt("UI", "DpiScaling", DpiScaling);
            ini.SetInt("UI", "TimeFormat", TimeFormat);
            ini.SetInt("UI", "FollowMode", FollowMode);
            ini.SetInt("UI", "FollowSync", FollowSync);
            ini.SetBool("UI", "ShowNoteLabels", ShowNoteLabels);
            ini.SetBool("UI", "ShowScrollBars", ShowScrollBars);
            ini.SetBool("UI", "ShowOscilloscope", ShowOscilloscope);
            ini.SetBool("UI", "ForceCompactSequencer", ForceCompactSequencer);
            ini.SetBool("UI", "ShowPianoRollViewRange", ShowPianoRollViewRange);
            ini.SetInt("UI", "TrackPadMoveSensitity", TrackPadMoveSensitity);
            ini.SetInt("UI", "TrackPadZoomSensitity", TrackPadZoomSensitity);
            ini.SetBool("UI", "ReverseTrackPad", ReverseTrackPad);

            // Audio
            ini.SetInt("Audio", "NumBufferedFrames", NumBufferedAudioFrames);
            ini.SetInt("Audio", "InstrumentStopTime", InstrumentStopTime);
            ini.SetBool("Audio", "SquareSmoothVibrato", SquareSmoothVibrato);
            ini.SetBool("Audio", "NoDragSoungWhenPlaying", NoDragSoungWhenPlaying);

            // Mixer
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

            // FFmpeg
            ini.SetString("FFmpeg", "ExecutablePath", FFmpegExecutablePath);

            // QWERTY
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
