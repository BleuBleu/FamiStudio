using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
    class ConfigDialog
    {
        enum ConfigSection
        {
            UserInterface,
            Sound,
            MIDI,
            Max
        };

        readonly string[] ConfigSectionNames =
        {
            "Interface",
            "Sound",
            "MIDI",
            ""
        };

        private PropertyPage[] pages = new PropertyPage[(int)ConfigSection.Max];
        private MultiPropertyDialog dialog;

        public unsafe ConfigDialog(Rectangle mainWinRect)
        {
            int width  = 480;
            int height = 300;
            int x = mainWinRect.Left + (mainWinRect.Width  - width)  / 2;
            int y = mainWinRect.Top  + (mainWinRect.Height - height) / 2;

            this.dialog = new MultiPropertyDialog(x, y, width, height);

            for (int i = 0; i < (int)ConfigSection.Max; i++)
            {
                var section = (ConfigSection)i;
                var page = dialog.AddPropertyPage(ConfigSectionNames[i], "Config" + section.ToString());
                CreatePropertyPage(page, section);
            }
        }

        private PropertyPage CreatePropertyPage(PropertyPage page, ConfigSection section)
        {
            switch (section)
            {
                case ConfigSection.UserInterface:
                {
#if FAMISTUDIO_WINDOWS
                    var scalingValues = new[] { "System", "100%", "150%", "200%" };
#elif FAMISTUDIO_MACOS
                    var scalingValues = new[] { "System", "100%", "200%" };
#else
                    var scalingValues = new[] { "System" };
#endif
                    var idx = Settings.DpiScaling == 0 ? 0 : Array.IndexOf(scalingValues, $"{Settings.DpiScaling}%");

                    page.AddStringList("Scaling (Requires restart):", scalingValues, scalingValues[idx]); // 0
                    page.AddBoolean("Check for updates:", true); // 1
                    break;
                }
                case ConfigSection.Sound:
                {
                    page.AddIntegerRange("Stop instruments after (sec):", Settings.InstrumentStopTime, 0, 10); // 0
                    page.AddBoolean("Prevent popping on square channels:", Settings.SquareSmoothVibrato); // 1
                    break;
                }
                case ConfigSection.MIDI:
                {
                    int midiDeviceCount = Midi.InputCount;
                    var midiDevices = new List<string>();
                    for (int i = 0; i < midiDeviceCount; i++)
                    {
                        midiDevices.Add(Midi.GetDeviceName(i));
                    }

                    var midiDevice = "";

                    if (Settings.MidiDevice.Length > 0 && midiDevices.Contains(Settings.MidiDevice))
                        midiDevice = Settings.MidiDevice;
                    else if (midiDevices.Count > 0)
                        midiDevice = midiDevices[0];

                    page.AddStringList("Device :", midiDevices.ToArray(), midiDevice); // 0
                    break;
                }
            }

            page.Build();
            pages[(int)section] = page;

#if FAMISTUDIO_LINUX
            page.SetPropertyEnabled(0, false);
#endif

            return page;
        }

        public DialogResult ShowDialog()
        {
            var dialogResult = dialog.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                // UI
                var pageUI = pages[(int)ConfigSection.UserInterface];
                var pageSound = pages[(int)ConfigSection.Sound];
                var scalingString = pageUI.GetPropertyValue<string>(0);

                Settings.DpiScaling = scalingString == "System" ? 0 : int.Parse(scalingString.Substring(0, 3));
                Settings.CheckUpdates = pageUI.GetPropertyValue<bool>(1);

                // Sound
                Settings.InstrumentStopTime = pageSound.GetPropertyValue<int>(0);
                Settings.SquareSmoothVibrato = pageSound.GetPropertyValue<bool>(1);

                // MIDI
                var pageMIDI = pages[(int)ConfigSection.MIDI];

                Settings.MidiDevice = pageMIDI.GetPropertyValue<string>(0);

                Settings.Save();
            }

            return dialogResult;
        }
    }
}
