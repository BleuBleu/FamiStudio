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
#if FAMISTUDIO_MACOS
            MacOS,
#endif
            Max
        };

        readonly string[] ConfigSectionNames =
        {
            "Interface",
            "Sound",
            "MIDI",
#if FAMISTUDIO_MACOS
            "MacOS",
#endif
            ""
        };

        public enum TimeFormat
        {
            PatternFrame,
            MinuteSecondsMilliseconds,
            Max
        }

        readonly string[] TimeFormatStrings =
        {
            "Pattern:Frame",
            "MM:SS:mmm"
        };

        public string[] FollowModeStrings =
        {
            "Jump",
            "Continuous"
        };

        public string[] FollowSyncStrings =
        {
            "Sequencer",
            "Piano Roll",
            "Both"
        };

        private PropertyPage[] pages = new PropertyPage[(int)ConfigSection.Max];
        private MultiPropertyDialog dialog;

        public unsafe ConfigDialog()
        {
#if FAMISTUDIO_LINUX
            int width  = 570;
#else
            int width  = 550;
#endif
            int height = 350;

            this.dialog = new MultiPropertyDialog(width, height);

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
                    var scalingIndex = Settings.DpiScaling == 0 ? 0 : Array.IndexOf(scalingValues, $"{Settings.DpiScaling}%");
                    var timeFormatIndex = Settings.TimeFormat < (int)TimeFormat.Max ? Settings.TimeFormat : 0;
                    var followModeIndex = Settings.FollowMode <= 0 ? 0 : Settings.FollowMode % FollowModeStrings.Length;
                    var followSyncIndex = Settings.FollowSync <= 0 ? 0 : Settings.FollowSync % FollowSyncStrings.Length;

                    page.AddStringList("Scaling (Requires restart):", scalingValues, scalingValues[scalingIndex]); // 0
                    page.AddStringList("Time Format:", TimeFormatStrings, TimeFormatStrings[timeFormatIndex]); // 1
                    page.AddStringList("Follow Mode:", FollowModeStrings, FollowModeStrings[followModeIndex]);  // 2
                    page.AddStringList("Following Views:", FollowSyncStrings, FollowSyncStrings[followSyncIndex]); // 3
                    page.AddBoolean("Check for updates:", Settings.CheckUpdates); // 4
                    page.AddBoolean("Show Piano Roll View Range:", Settings.ShowPianoRollViewRange); // 5
                    page.AddBoolean("Show Note Labels:", Settings.ShowNoteLabels); // 6
                    page.AddBoolean("Trackpad controls:", Settings.TrackPadControls); // 7

#if FAMISTUDIO_LINUX
                    page.SetPropertyEnabled(0, false);
#elif FAMISTUDIO_MACOS
                    page.PropertyChanged += Page_PropertyChanged;
#endif

                    break;
                }
                case ConfigSection.Sound:
                {
                    page.AddIntegerRange("Number of buffered frames:", Settings.NumBufferedAudioFrames, 2, 16); // 0
                    page.AddIntegerRange("Stop instruments after (sec):", Settings.InstrumentStopTime, 0, 10); // 1
                    page.AddBoolean("Prevent popping on square channels:", Settings.SquareSmoothVibrato); // 2
                    page.AddBoolean("Mute piano roll interactions during playback:", Settings.NoDragSoungWhenPlaying); // 3
                        
                    break;
                }
                case ConfigSection.MIDI:
                {
                    int midiDeviceCount = Midi.InputCount;
                    var midiDevices = new List<string>();
                    for (int i = 0; i < midiDeviceCount; i++)
                    {
                        var name = Midi.GetDeviceName(i);
                        if (!string.IsNullOrEmpty(name))
                            midiDevices.Add(name);
                    }

                    var midiDevice = "";

                    if (!string.IsNullOrEmpty(Settings.MidiDevice) && midiDevices.Contains(Settings.MidiDevice))
                        midiDevice = Settings.MidiDevice;
                    else if (midiDevices.Count > 0)
                        midiDevice = midiDevices[0];

                    page.AddStringList("Device :", midiDevices.ToArray(), midiDevice); // 0
                    break;
                }
                case ConfigSection.MacOS:
                { 
                    page.AddBoolean("Reverse trackpad direction:", Settings.ReverseTrackPad); // 0
                    page.AddIntegerRange("Trackpad movement sensitivity:", Settings.TrackPadMoveSensitity, 1, 16); // 1
                    page.AddIntegerRange("Trackpad zoom sensitivity:", Settings.TrackPadZoomSensitity, 1, 16); // 2
                    page.SetPropertyEnabled(0, Settings.TrackPadControls);
                    page.SetPropertyEnabled(1, Settings.TrackPadControls);
                    page.SetPropertyEnabled(2, Settings.TrackPadControls);
                    break;
                }
            }

            page.Build();
            pages[(int)section] = page;

            return page;
        }

#if FAMISTUDIO_MACOS
        private void Page_PropertyChanged(PropertyPage props, int idx, object value)
        {
            if (props == pages[(int)ConfigSection.UserInterface] && idx == 7)
            {
                var macOsPage = pages[(int)ConfigSection.MacOS];
                macOsPage.SetPropertyEnabled(0, (bool)value);
                macOsPage.SetPropertyEnabled(1, (bool)value);
                macOsPage.SetPropertyEnabled(2, (bool)value);
            }
        }
#endif

        public DialogResult ShowDialog(FamiStudioForm parent)
        {
            var dialogResult = dialog.ShowDialog(parent);

            if (dialogResult == DialogResult.OK)
            {
                // UI
                var pageUI = pages[(int)ConfigSection.UserInterface];
                var pageSound = pages[(int)ConfigSection.Sound];
                var scalingString = pageUI.GetPropertyValue<string>(0);
                var timeFormatString = pageUI.GetPropertyValue<string>(1);
                var followModeString = pageUI.GetPropertyValue<string>(2);
                var followSyncString = pageUI.GetPropertyValue<string>(3);

                Settings.DpiScaling = scalingString == "System" ? 0 : int.Parse(scalingString.Substring(0, 3));
                Settings.TimeFormat = Array.IndexOf(TimeFormatStrings, timeFormatString);
                Settings.FollowMode = Array.IndexOf(FollowModeStrings, followModeString);
                Settings.FollowSync = Array.IndexOf(FollowSyncStrings, followSyncString);
                Settings.CheckUpdates = pageUI.GetPropertyValue<bool>(4);
                Settings.ShowPianoRollViewRange = pageUI.GetPropertyValue<bool>(5);
                Settings.ShowNoteLabels = pageUI.GetPropertyValue<bool>(6);
                Settings.TrackPadControls = pageUI.GetPropertyValue<bool>(7);

#if FAMISTUDIO_MACOS
                var pageMacOS = pages[(int)ConfigSection.MacOS];
                Settings.ReverseTrackPad   = pageMacOS.GetPropertyValue<bool>(0);
                Settings.TrackPadMoveSensitity = pageMacOS.GetPropertyValue<int>(1);
                Settings.TrackPadZoomSensitity = pageMacOS.GetPropertyValue<int>(2);
#endif

                // Sound
                Settings.NumBufferedAudioFrames = pageSound.GetPropertyValue<int>(0);
                Settings.InstrumentStopTime = pageSound.GetPropertyValue<int>(1);
                Settings.SquareSmoothVibrato = pageSound.GetPropertyValue<bool>(2);
                Settings.NoDragSoungWhenPlaying = pageSound.GetPropertyValue<bool>(3);

                // MIDI
                var pageMIDI = pages[(int)ConfigSection.MIDI];

                Settings.MidiDevice = pageMIDI.GetPropertyValue<string>(0);

                Settings.Save();
            }

            return dialogResult;
        }
    }
}
