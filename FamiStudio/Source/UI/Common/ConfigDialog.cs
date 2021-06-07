using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FamiStudio
{
    class ConfigDialog
    {
        enum ConfigSection
        {
            General,
            UserInterface,
            Sound,
            Mixer,
            MIDI,
            QWERTY,
#if FAMISTUDIO_MACOS
            MacOS,
#endif
            Max
        };

        readonly string[] ConfigSectionNames =
        {
            "General",
            "Interface",
            "Sound",
            "Mixer",
            "MIDI",
            "QWERTY",
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
        private int[,] qwertyKeys; // We keep a copy here in case the user cancels.
        private Settings.ExpansionMix[] expansionMixer = new Settings.ExpansionMix[ExpansionType.Count];

        public unsafe ConfigDialog()
        {
#if FAMISTUDIO_WINDOWS
            int width  = 550;
            int height = 350;
#else
            int width  = 570;
            int height = 450;
#endif

            this.dialog = new MultiPropertyDialog(width, height);

            // Keep a copy of QWERTY keys.
            qwertyKeys = new int[37, 2];
            Array.Copy(Settings.QwertyKeys, qwertyKeys, Settings.QwertyKeys.Length);

            // Keep a copy of mixer settings.
            Array.Copy(Settings.ExpansionMixerSettings, expansionMixer, Settings.ExpansionMixerSettings.Length);

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
                case ConfigSection.General:
                {
                    page.AddCheckBox("Check for updates:", Settings.CheckUpdates); // 0
                    page.AddCheckBox("Trackpad controls:", Settings.TrackPadControls); // 1
#if FAMISTUDIO_MACOS
                    page.PropertyChanged += PageGeneral_PropertyChanged;
#endif
                    break;
                }

                case ConfigSection.UserInterface:
                {
#if FAMISTUDIO_WINDOWS || FAMISTUDIO_LINUX
                    var scalingValues = new[] { "System", "100%", "150%", "200%" };
#elif FAMISTUDIO_MACOS
                    var scalingValues = new[] { "System", "100%", "200%" };
#endif
                    var scalingIndex    = Settings.DpiScaling == 0 ? 0 : Array.IndexOf(scalingValues, $"{Settings.DpiScaling}%");
                    var timeFormatIndex = Settings.TimeFormat < (int)TimeFormat.Max ? Settings.TimeFormat : 0;
                    var followModeIndex = Settings.FollowMode <= 0 ? 0 : Settings.FollowMode % FollowModeStrings.Length;
                    var followSyncIndex = Settings.FollowSync <= 0 ? 0 : Settings.FollowSync % FollowSyncStrings.Length;

                    page.AddDropDownList("Scaling (Requires restart):", scalingValues, scalingValues[scalingIndex]); // 0
                    page.AddDropDownList("Time Format:", TimeFormatStrings, TimeFormatStrings[timeFormatIndex]); // 1
                    page.AddDropDownList("Follow Mode:", FollowModeStrings, FollowModeStrings[followModeIndex]);  // 2
                    page.AddDropDownList("Following Views:", FollowSyncStrings, FollowSyncStrings[followSyncIndex]); // 3
                    page.AddCheckBox("Show Piano Roll View Range:", Settings.ShowPianoRollViewRange); // 4
                    page.AddCheckBox("Show Note Labels:", Settings.ShowNoteLabels); // 5
                    page.AddCheckBox("Show FamiTracker Stop Notes:", Settings.ShowImplicitStopNotes); // 6
                    page.AddCheckBox("Show Scroll Bars:", Settings.ShowScrollBars); // 7
                    page.AddCheckBox("Show Oscilloscope:", Settings.ShowOscilloscope); // 8
                    page.AddCheckBox("Force Compact Sequencer:", Settings.ForceCompactSequencer); // 9
                    break;
                    }
                case ConfigSection.Sound:
                {
                    page.AddIntegerRange("Number of buffered frames:", Settings.NumBufferedAudioFrames, 2, 16); // 0
                    page.AddIntegerRange("Stop instruments after (sec):", Settings.InstrumentStopTime, 0, 10); // 1
                    page.AddCheckBox("Prevent popping on square channels:", Settings.SquareSmoothVibrato); // 2
                    page.AddCheckBox("Mute piano roll interactions during playback:", Settings.NoDragSoungWhenPlaying); // 3
                    break;
                }
                case ConfigSection.Mixer:
                {
                    // TODO : Tooltips.
                    page.AddDropDownList("Expansion:", ExpansionType.Names, ExpansionType.Names[0]); // 0
                    page.AddSlider("Volume:", Settings.ExpansionMixerSettings[ExpansionType.None].volume, -10.0, 10.0, 0.1, 1, FormatDecibels); // 1
                    page.AddSlider("Treble:", Settings.ExpansionMixerSettings[ExpansionType.None].treble, -100.0, 5.0, 0.1, 1, FormatDecibels); // 2
                    page.AddButton(null, "Reset to default", ResetMixerClicked, "Resets this expansion to the default settings.");
                    page.AddLabel(null, "Note : These will have no effect on NSF, ROM, FDS and sound engine exports.", true); // 4
                    page.PropertyChanged += MixerPage_PropertyChanged;
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

                    page.AddDropDownList("Device :", midiDevices.ToArray(), midiDevice); // 0
                    break;
                }
                case ConfigSection.QWERTY:
                {
                    page.AddLabel(null, "Double click in the 2 last columns to assign a key. Right click to clear a key.", true); // 0
                    page.AddMultiColumnList(new[] { new ColumnDesc("Octave"), new ColumnDesc("Note"), new ColumnDesc("Key"), new ColumnDesc("Key (alt)") }, GetQwertyMappingStrings(), null, QwertyListDoubleClicked, QwertyListRightClicked); // 1
                    page.AddButton(null, "Reset to default", ResetQwertyClicked); 
                    break;
                }
#if FAMISTUDIO_MACOS
                case ConfigSection.MacOS:
                { 
                    page.AddCheckBox("Reverse trackpad direction:", Settings.ReverseTrackPad); // 0
                    page.AddIntegerRange("Trackpad movement sensitivity:", Settings.TrackPadMoveSensitity, 1, 16); // 1
                    page.AddIntegerRange("Trackpad zoom sensitivity:", Settings.TrackPadZoomSensitity, 1, 32); // 2
                    page.SetPropertyEnabled(0, Settings.TrackPadControls);
                    page.SetPropertyEnabled(1, Settings.TrackPadControls);
                    page.SetPropertyEnabled(2, Settings.TrackPadControls);
                    break;
                }
#endif
            }

            page.Build();
            pages[(int)section] = page;

            return page;
        }

        private string FormatDecibels(double value)
        {
            return $"{(value >= 0 ? "+" : "")}{value:N1} dB";
        }

        private string FormatHz(double value)
        {
            return $"{value:N0} Hz";
        }

        private void ResetQwertyClicked(PropertyPage props, int propertyIndex)
        {
            Array.Copy(Settings.DefaultQwertyKeys, qwertyKeys, Settings.DefaultQwertyKeys.Length);
            pages[(int)ConfigSection.QWERTY].UpdateMultiColumnList(1, GetQwertyMappingStrings());
        }

        private string[,] GetQwertyMappingStrings()
        {
            var data = new string[37, 4];

            // Stop note.
            {
                var k0 = qwertyKeys[0, 0];
                var k1 = qwertyKeys[0, 1];

                data[0, 0] = "N/A";
                data[0, 1] = "Stop Note";
                data[0, 2] = k0 < 0 ? "" : PlatformUtils.KeyCodeToString(qwertyKeys[0, 0]);
                data[0, 3] = k1 < 0 ? "" : PlatformUtils.KeyCodeToString(qwertyKeys[0, 1]);
            }

            // Regular notes.
            for (int idx = 1; idx < data.GetLength(0); idx++)
            {
                var octave = (idx - 1) / 12;
                var note   = (idx - 1) % 12;

                var k0 = qwertyKeys[idx, 0];
                var k1 = qwertyKeys[idx, 1];

                data[idx, 0] = octave.ToString();
                data[idx, 1] = Note.NoteNames[note];
                data[idx, 2] = k0 < 0 ? "" : PlatformUtils.KeyCodeToString(qwertyKeys[idx, 0]);
                data[idx, 3] = k1 < 0 ? "" : PlatformUtils.KeyCodeToString(qwertyKeys[idx, 1]);
            }

            return data;
        }

        void AssignQwertyKey(int idx, int keyIndex, int keyCode)
        {
            // Unbind this key from anything.
            for (int i = 0; i < qwertyKeys.GetLength(0); i++)
            {
                for (int j = 0; j < qwertyKeys.GetLength(1); j++)
                {
                    if (qwertyKeys[i, j] == keyCode)
                        qwertyKeys[i, j] = -1;
                }
            }

            qwertyKeys[idx, keyIndex] = keyCode;
        }

        void QwertyListDoubleClicked(PropertyPage props, int propertyIndex, int itemIndex, int columnIndex)
        {
            if (columnIndex < 2)
                return;

            var dlg = new PropertyDialog(300, false, true, dialog);
            dlg.Properties.AddLabel(null, "Press the new key or ESC to cancel.");
            dlg.Properties.Build();

            // TODO : Make this cross-platform.
#if FAMISTUDIO_WINDOWS
            dlg.KeyDown += (sender, e) => 
            {
                if (PlatformUtils.KeyCodeToString((int)e.KeyCode) != null) 
                {
                    if (e.KeyCode != Keys.Escape)
                        AssignQwertyKey(itemIndex, columnIndex - 2, (int)e.KeyCode);
                    dlg.Close();
                }
            };
#else
            dlg.KeyPressEvent += (o, args) =>
            {
                // These 2 keys are used by the QWERTY input.
                if (args.Event.Key != Gdk.Key.Tab &&
                    args.Event.Key != Gdk.Key.BackSpace && 
                    PlatformUtils.KeyCodeToString((int)args.Event.Key) != null)
                {
                    if (args.Event.Key != Gdk.Key.Escape)
                        AssignQwertyKey(itemIndex, columnIndex - 2, (int)args.Event.Key);
                    dlg.Accept();
                }
            };
#endif
            dlg.ShowDialog(null);

            pages[(int)ConfigSection.QWERTY].UpdateMultiColumnList(1, GetQwertyMappingStrings());
        }

        void QwertyListRightClicked(PropertyPage props, int propertyIndex, int itemIndex, int columnIndex)
        {
            if (columnIndex >= 2)
            {
                qwertyKeys[itemIndex, columnIndex - 2] = -1;
                pages[(int)ConfigSection.QWERTY].UpdateMultiColumnList(1, GetQwertyMappingStrings());
            }
        }

        private void RefreshMixerSettings()
        {
            var props = pages[(int)ConfigSection.Mixer];
            var expansion = props.GetSelectedIndex(0);

            props.SetPropertyValue(1, (double)expansionMixer[expansion].volume);
            props.SetPropertyValue(2, (double)expansionMixer[expansion].treble);
        }

        private void MixerPage_PropertyChanged(PropertyPage props, int idx, object value)
        {
            var expansion = props.GetSelectedIndex(0);

            if (idx == 0)
            {
                RefreshMixerSettings();
            }
            else if (idx == 1)
            {
                // MATTT : Make sure slider updates on Linux / Mac.
                expansionMixer[expansion].volume = (float)(double)value;
            }
            else if (idx == 2)
            {
                expansionMixer[expansion].treble = (float)(double)value;
            }
        }

        private void ResetMixerClicked(PropertyPage props, int propertyIndex)
        {
            var expansion = props.GetSelectedIndex(0);
            expansionMixer[expansion] = Settings.DefaultExpansionMixerSettings[expansion];
            RefreshMixerSettings();
        }

#if FAMISTUDIO_MACOS
        private void PageGeneral_PropertyChanged(PropertyPage props, int idx, object value)
        {
            if (props == pages[(int)ConfigSection.General] && idx == 1)
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
                var pageGeneral = pages[(int)ConfigSection.General];
                var pageUI = pages[(int)ConfigSection.UserInterface];
                var pageSound = pages[(int)ConfigSection.Sound];
                var pageMixer = pages[(int)ConfigSection.Mixer];

                // General
                Settings.CheckUpdates = pageGeneral.GetPropertyValue<bool>(0);
                Settings.TrackPadControls = pageGeneral.GetPropertyValue<bool>(1);

                // UI
                var scalingString = pageUI.GetPropertyValue<string>(0);

                Settings.DpiScaling = scalingString == "System" ? 0 : int.Parse(scalingString.Substring(0, 3));
                Settings.TimeFormat = Array.IndexOf(TimeFormatStrings, pageUI.GetPropertyValue<string>(1));
                Settings.FollowMode = Array.IndexOf(FollowModeStrings, pageUI.GetPropertyValue<string>(2));
                Settings.FollowSync = Array.IndexOf(FollowSyncStrings, pageUI.GetPropertyValue<string>(3));
                Settings.ShowPianoRollViewRange = pageUI.GetPropertyValue<bool>(4);
                Settings.ShowNoteLabels = pageUI.GetPropertyValue<bool>(5);
                Settings.ShowImplicitStopNotes = pageUI.GetPropertyValue<bool>(6);
                Settings.ShowScrollBars = pageUI.GetPropertyValue<bool>(7);
                Settings.ShowOscilloscope = pageUI.GetPropertyValue<bool>(8);
                Settings.ForceCompactSequencer = pageUI.GetPropertyValue<bool>(9);

                // Sound
                Settings.NumBufferedAudioFrames = pageSound.GetPropertyValue<int>(0);
                Settings.InstrumentStopTime = pageSound.GetPropertyValue<int>(1);
                Settings.SquareSmoothVibrato = pageSound.GetPropertyValue<bool>(2);
                Settings.NoDragSoungWhenPlaying = pageSound.GetPropertyValue<bool>(3);

                // Mixer.
                Array.Copy(expansionMixer, Settings.ExpansionMixerSettings, Settings.ExpansionMixerSettings.Length);

                // MIDI
                var pageMIDI = pages[(int)ConfigSection.MIDI];

                Settings.MidiDevice = pageMIDI.GetPropertyValue<string>(0);

                // QWERTY
                Array.Copy(qwertyKeys, Settings.QwertyKeys, Settings.QwertyKeys.Length);
                Settings.UpdateKeyCodeMaps();

#if FAMISTUDIO_MACOS
                // Mac OS
                var pageMacOS = pages[(int)ConfigSection.MacOS];
                Settings.ReverseTrackPad   = pageMacOS.GetPropertyValue<bool>(0);
                Settings.TrackPadMoveSensitity = pageMacOS.GetPropertyValue<int>(1);
                Settings.TrackPadZoomSensitity = pageMacOS.GetPropertyValue<int>(2);
#endif

                Settings.Save();
            }

            return dialogResult;
        }
    }
}
