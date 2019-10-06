using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    static class Settings
    {
        // User Interface section
        public static int DpiScaling { get; set; }
        public static bool CheckUpdates { get; set; } = true;

        // Audio section
        public static int InstrumentStopTime { get; set; } = 2;

        // MIDI section
        public static string MidiDevice { get; set; } = "";

        public static void Load()
        {
            try
            {
                var ini = IniFile.ParseINI(GetConfigFileName());

                DpiScaling = int.Parse(ini["UI"]["DpiScaling"]);
                CheckUpdates = bool.Parse(ini["UI"]["CheckUpdates"]);
                InstrumentStopTime = int.Parse(ini["Audio"]["InstrumentStopTime"]);
                MidiDevice = ini["MIDI"]["Device"];
            }
            catch
            {
            }

            if (DpiScaling != 100 && DpiScaling != 150 && DpiScaling != 200)
                DpiScaling = 0;

            InstrumentStopTime = Utils.Clamp(InstrumentStopTime, 0, 10);

            if (MidiDevice == null)
                MidiDevice = "";
        }

        public static void Save()
        {
            var ini = new Dictionary<string, Dictionary<string, string>>();

            ini["UI"] = new Dictionary<string, string>();
            ini["Audio"] = new Dictionary<string, string>();
            ini["MIDI"] = new Dictionary<string, string>();

            ini["UI"]["DpiScaling"]   = DpiScaling.ToString();
            ini["UI"]["CheckUpdates"] = CheckUpdates.ToString();
            ini["Audio"]["InstrumentStopTime"] = InstrumentStopTime.ToString();
            ini["MIDI"]["Device"]     = MidiDevice;

            Directory.CreateDirectory(GetConfigFilePath());

            IniFile.WriteINI(GetConfigFileName(), ini);
        }

        private static string GetConfigFilePath()
        {
#if FAMISTUDIO_WINDOWS
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FamiStudio");
#elif FAMISTUDIO_MACOS
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Application Support/FamiStudio");
#else
            Debug.Assert(false);
            return "";
#endif
        }

        private static string GetConfigFileName()
        {
            return Path.Combine(GetConfigFilePath(), "FamiStudio.ini");
        }
    }
}
