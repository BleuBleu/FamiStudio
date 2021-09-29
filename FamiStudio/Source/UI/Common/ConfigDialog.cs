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
            FFmpeg,
            QWERTY,
            MacOS,
            Max
        };

        readonly string[] ConfigSectionNames =
        {
            "General",
            "Interface",
            "Sound",
            "Mixer",
            "MIDI",
            "FFmpeg",
            "QWERTY",
            "MacOS",
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

        public string[] ScrollBarsStrings =
        {
            "None",
            "Thin",
            "Thick"
        };

        private PropertyPage[] pages = new PropertyPage[(int)ConfigSection.Max];
        private MultiPropertyDialog dialog;
        private int[,] qwertyKeys; // We keep a copy here in case the user cancels.
        private Settings.ExpansionMix[] expansionMixer = new Settings.ExpansionMix[ExpansionType.Count];

        public unsafe ConfigDialog()
        {
            int width  = PlatformUtils.IsWindows ? 550 : 570;
            int height = PlatformUtils.IsWindows ? 350 : 450;

            dialog = new MultiPropertyDialog("FamiStudio Configuration", width, height);
            dialog.SetVerb("Apply", true);

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

            dialog.SetPageVisible((int)ConfigSection.MacOS,  PlatformUtils.IsMacOS);
            dialog.SetPageVisible((int)ConfigSection.MIDI,   PlatformUtils.IsDesktop);
            dialog.SetPageVisible((int)ConfigSection.FFmpeg, PlatformUtils.IsDesktop);
            dialog.SetPageVisible((int)ConfigSection.QWERTY, PlatformUtils.IsDesktop);
        }

        private PropertyPage CreatePropertyPage(PropertyPage page, ConfigSection section)
        {
            switch (section)
            {
                case ConfigSection.General:
                {
                    page.AddCheckBox("Check for updates:", Settings.CheckUpdates); // 0
                    page.AddCheckBox("Trackpad controls:", Settings.TrackPadControls); // 1
                    page.AddCheckBox("Clear Undo/Redo on save:", Settings.ClearUndoRedoOnSave); // 2
                    page.AddCheckBox("Open last project on start:", Settings.OpenLastProjectOnStart); // 3
                    page.AddCheckBox("Autosave a copy every 2 minutes:", Settings.AutoSaveCopy); // 4
                    page.AddButton(null, "Open Autosave folder"); // 4
                    page.PropertyClicked += PageGeneral_PropertyClicked;
                    page.PropertyChanged += PageGeneral_PropertyChanged;
                    break;
                }

                case ConfigSection.UserInterface:
                {
                    // DROIDTODO: Make this cleaner, maybe move to DpiScaling?
#if FAMISTUDIO_WINDOWS || FAMISTUDIO_LINUX
                    var scalingValues = new[] { "System", "100%", "150%", "200%" };
#elif FAMISTUDIO_MACOS
                    var scalingValues = new[] { "System", "100%", "200%" };
#elif FAMISTUDIO_ANDROID
                    var scalingValues = new[] { "System" };
#endif
                    var scalingIndex    = Settings.DpiScaling == 0 ? 0 : Array.IndexOf(scalingValues, $"{Settings.DpiScaling}%");
                    var timeFormatIndex = Settings.TimeFormat < (int)TimeFormat.Max ? Settings.TimeFormat : 0;
                    var followModeIndex = Settings.FollowMode <= 0 ? 0 : Settings.FollowMode % FollowModeStrings.Length;
                    var followSyncIndex = Settings.FollowSync <= 0 ? 0 : Settings.FollowSync % FollowSyncStrings.Length;

                    page.AddDropDownList("Scaling (Requires restart):", scalingValues, scalingValues[scalingIndex]); // 0
                    page.AddDropDownList("Time Format:", TimeFormatStrings, TimeFormatStrings[timeFormatIndex]); // 1
                    page.AddDropDownList("Follow Mode:", FollowModeStrings, FollowModeStrings[followModeIndex]);  // 2
                    page.AddDropDownList("Following Views:", FollowSyncStrings, FollowSyncStrings[followSyncIndex]); // 3
                    page.AddDropDownList("Scroll Bars:", ScrollBarsStrings, ScrollBarsStrings[Settings.ScrollBars]); // 4
                    page.AddCheckBox("Show Piano Roll View Range:", Settings.ShowPianoRollViewRange); // 5
                    page.AddCheckBox("Show Note Labels:", Settings.ShowNoteLabels); // 6
                    page.AddCheckBox("Show FamiTracker Stop Notes:", Settings.ShowImplicitStopNotes); // 7
                    page.AddCheckBox("Show Oscilloscope:", Settings.ShowOscilloscope); // 8
                    page.AddCheckBox("Force Compact Sequencer:", Settings.ForceCompactSequencer); // 9
                    break;
                    }
                case ConfigSection.Sound:
                {
                    page.AddNumericUpDown("Number of buffered frames:", Settings.NumBufferedAudioFrames, 2, 16); // 0
                    page.AddNumericUpDown("Stop instruments after (sec):", Settings.InstrumentStopTime, 0, 10); // 1
                    page.AddCheckBox("Prevent popping on square channels:", Settings.SquareSmoothVibrato); // 2
                    page.AddCheckBox("Mute drag sounds during playback:", Settings.NoDragSoungWhenPlaying); // 3
                    page.AddSlider("Metronome volume:", Settings.MetronomeVolume, 1.0, 200.0, 1.0, 0, null); // 4
                    break;
                }
                case ConfigSection.Mixer:
                {
                    // TODO : Tooltips.
                    page.AddDropDownList("Expansion:", ExpansionType.Names, ExpansionType.Names[0]); // 0
                    page.AddSlider("Volume:", Settings.ExpansionMixerSettings[ExpansionType.None].volume, -10.0, 10.0, 0.1, 1, "{0:+0.0;-0.0} dB"); // 1
                    page.AddSlider("Treble:", Settings.ExpansionMixerSettings[ExpansionType.None].treble, -100.0, 5.0, 0.1, 1, "{0:+0.0;-0.0} dB"); // 2
                    page.AddButton(PlatformUtils.IsDesktop ? null : "Reset", "Reset to default", "Resets this expansion to the default settings."); // 3
                    page.AddLabel(PlatformUtils.IsDesktop ? null : "Note", "Note : These will have no effect on NSF, ROM, FDS and sound engine exports.", true); // 4
                    page.PropertyChanged += MixerPage_PropertyChanged;
                    page.PropertyClicked += MixerPage_PropertyClicked;
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
                case ConfigSection.FFmpeg:
                    page.AddLabel(null, "Video export requires FFmpeg. If you already have it, set the path to the ffmpeg executable by clicking the button below, otherwise follow the download link.", true); // 0
                    page.AddButton(null, Settings.FFmpegExecutablePath, "Path to FFmpeg executable. On Windows this is ffmpeg.exe. To download and install ffpmeg, check the link below."); // 1
                    // GTK LinkButtons dont work on MacOS, use a button (https://github.com/quodlibet/quodlibet/issues/2306)
                    if (PlatformUtils.IsMacOS)
                        page.AddButton(" ", "Download FFmpeg here"); // 2
                    else
                        page.AddLinkLabel(" ", "Download FFmpeg here", "https://famistudio.org/doc/ffmpeg/"); // 3
                    page.PropertyClicked += FFmpegPage_PropertyClicked;
                    break;
                case ConfigSection.QWERTY:
                {
                    page.AddLabel(null, "Double click in the 2 last columns to assign a key. Right click to clear a key.", true); // 0
                    page.AddMultiColumnList(new[] { new ColumnDesc("Octave", 0.2f), new ColumnDesc("Note", 0.2f), new ColumnDesc("Key", 0.3f), new ColumnDesc("Key (alt)", 0.3f) }, GetQwertyMappingStrings()); // 1
                    page.AddButton(null, "Reset to default");
                    page.PropertyClicked += QwertyPage_PropertyClicked;
                    break;
                }
                case ConfigSection.MacOS:
                { 
                    page.AddCheckBox("Reverse trackpad direction:", Settings.ReverseTrackPad); // 0
                    page.AddNumericUpDown("Trackpad movement sensitivity:", Settings.TrackPadMoveSensitity, 1, 16); // 1
                    page.AddNumericUpDown("Trackpad zoom sensitivity:", Settings.TrackPadZoomSensitity, 1, 32); // 2
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

        private void PageGeneral_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (click == ClickType.Button)
            {
                PlatformUtils.OpenUrl(Settings.GetAutoSaveFilePath());
            }
        }

        private void FFmpegPage_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (click == ClickType.Button)
            {
                if (propIdx == 1)
                {
                    var ffmpegExeFilter = PlatformUtils.IsWindows ? "FFmpeg Executable (ffmpeg.exe)|ffmpeg.exe" : "FFmpeg Executable (ffmpeg)|*.*";
                    var dummy = "";
                    var filename = PlatformUtils.ShowOpenFileDialog("Please select FFmpeg executable", ffmpegExeFilter, ref dummy, dialog);

                    if (filename != null)
                    {
                        props.SetPropertyValue(propIdx, filename);
                    }
                }
                else if (propIdx == 2)
                {
                    PlatformUtils.OpenUrl("https://famistudio.org/doc/ffmpeg/");
                }
            }
        }

        private void QwertyPage_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 1 && colIdx >= 2)
            {
                if (click == ClickType.Double)
                {
                    var dlg = new PropertyDialog("", 300, false, true, dialog);
                    dlg.Properties.AddLabel(null, "Press the new key or ESC to cancel.");
                    dlg.Properties.Build();

                    // TODO : Make this cross-platform.
#if FAMISTUDIO_WINDOWS
                    dlg.KeyDown += (sender, e) =>
                    {
                        if (PlatformUtils.KeyCodeToString((int)e.KeyCode) != null)
                        {
                            if (e.KeyCode != Keys.Escape)
                                AssignQwertyKey(rowIdx, colIdx - 2, (int)e.KeyCode);
                            dlg.Close();
                        }
                    };
#elif FAMISTUDIO_LINUX || FAMISTUDIO_MACOS
                    dlg.KeyPressEvent += (o, args) =>
                    {
                        // These 2 keys are used by the QWERTY input.
                        if (args.Event.Key != Gdk.Key.Tab &&
                            args.Event.Key != Gdk.Key.BackSpace && 
                            PlatformUtils.KeyCodeToString((int)args.Event.Key) != null)
                        {
                            if (args.Event.Key != Gdk.Key.Escape)
                                AssignQwertyKey(rowIdx, colIdx - 2, (int)args.Event.Key);
                            dlg.Accept();
                        }
                    };
#endif
                    dlg.ShowDialogAsync(null, (r) => { });

                    pages[(int)ConfigSection.QWERTY].UpdateMultiColumnList(1, GetQwertyMappingStrings());
                }
                else if (click == ClickType.Right)
                {
                    qwertyKeys[rowIdx, colIdx - 2] = -1;
                    pages[(int)ConfigSection.QWERTY].UpdateMultiColumnList(1, GetQwertyMappingStrings());
                }
            }
            else if (propIdx == 2 && click == ClickType.Button)
            {
                Array.Copy(Settings.DefaultQwertyKeys, qwertyKeys, Settings.DefaultQwertyKeys.Length);
                pages[(int)ConfigSection.QWERTY].UpdateMultiColumnList(1, GetQwertyMappingStrings());
            }
        }

        private string[,] GetQwertyMappingStrings()
        {
            var data = new string[37, 4];

            if (PlatformUtils.IsDesktop)
            {
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
                    var note = (idx - 1) % 12;

                    var k0 = qwertyKeys[idx, 0];
                    var k1 = qwertyKeys[idx, 1];

                    data[idx, 0] = octave.ToString();
                    data[idx, 1] = Note.NoteNames[note];
                    data[idx, 2] = k0 < 0 ? "" : PlatformUtils.KeyCodeToString(qwertyKeys[idx, 0]);
                    data[idx, 3] = k1 < 0 ? "" : PlatformUtils.KeyCodeToString(qwertyKeys[idx, 1]);
                }
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

        private void MixerPage_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 3 && click == ClickType.Button)
            {
                var expansion = props.GetSelectedIndex(0);
                expansionMixer[expansion] = Settings.DefaultExpansionMixerSettings[expansion];
                RefreshMixerSettings();
            }
        }

        private void RefreshMixerSettings()
        {
            var props = pages[(int)ConfigSection.Mixer];
            var expansion = props.GetSelectedIndex(0);

            props.SetPropertyValue(1, (double)expansionMixer[expansion].volume);
            props.SetPropertyValue(2, (double)expansionMixer[expansion].treble);
        }

        private void MixerPage_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            var expansion = props.GetSelectedIndex(0);

            if (propIdx == 0)
            {
                RefreshMixerSettings();
            }
            else if (propIdx == 1)
            {
                expansionMixer[expansion].volume = (float)(double)value;
            }
            else if (propIdx == 2)
            {
                expansionMixer[expansion].treble = (float)(double)value;
            }
        }

        private void PageGeneral_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (props == pages[(int)ConfigSection.General] && propIdx == 1)
            {
                var macOsPage = pages[(int)ConfigSection.MacOS];
                macOsPage.SetPropertyEnabled(0, (bool)value);
                macOsPage.SetPropertyEnabled(1, (bool)value);
                macOsPage.SetPropertyEnabled(2, (bool)value);
            }
        }

        public void ShowDialogAsync(FamiStudioForm parent, Action<DialogResult> callback)
        {
            dialog.ShowDialogAsync(parent, (r) =>
            {
                if (r == DialogResult.OK)
                {
                    var pageGeneral = pages[(int)ConfigSection.General];
                    var pageUI = pages[(int)ConfigSection.UserInterface];
                    var pageSound = pages[(int)ConfigSection.Sound];
                    var pageMixer = pages[(int)ConfigSection.Mixer];

                    // General
                    Settings.CheckUpdates = pageGeneral.GetPropertyValue<bool>(0);
                    Settings.TrackPadControls = pageGeneral.GetPropertyValue<bool>(1);
                    Settings.ClearUndoRedoOnSave = pageGeneral.GetPropertyValue<bool>(2);
                    Settings.OpenLastProjectOnStart = pageGeneral.GetPropertyValue<bool>(3);
                    Settings.AutoSaveCopy = pageGeneral.GetPropertyValue<bool>(4);

                    // UI
                    var scalingString = pageUI.GetPropertyValue<string>(0);

                    Settings.DpiScaling = scalingString == "System" ? 0 : int.Parse(scalingString.Substring(0, 3));
                    Settings.TimeFormat = pageUI.GetSelectedIndex(1);
                    Settings.FollowMode = pageUI.GetSelectedIndex(2);
                    Settings.FollowSync = pageUI.GetSelectedIndex(3);
                    Settings.ScrollBars = pageUI.GetSelectedIndex(4);
                    Settings.ShowPianoRollViewRange = pageUI.GetPropertyValue<bool>(5);
                    Settings.ShowNoteLabels = pageUI.GetPropertyValue<bool>(6);
                    Settings.ShowImplicitStopNotes = pageUI.GetPropertyValue<bool>(7);
                    Settings.ShowOscilloscope = pageUI.GetPropertyValue<bool>(8);
                    Settings.ForceCompactSequencer = pageUI.GetPropertyValue<bool>(9);

                    // Sound
                    Settings.NumBufferedAudioFrames = pageSound.GetPropertyValue<int>(0);
                    Settings.InstrumentStopTime = pageSound.GetPropertyValue<int>(1);
                    Settings.SquareSmoothVibrato = pageSound.GetPropertyValue<bool>(2);
                    Settings.NoDragSoungWhenPlaying = pageSound.GetPropertyValue<bool>(3);
                    Settings.MetronomeVolume = (int)pageSound.GetPropertyValue<double>(4);

                    // Mixer.
                    Array.Copy(expansionMixer, Settings.ExpansionMixerSettings, Settings.ExpansionMixerSettings.Length);

                    // MIDI
                    var pageMIDI = pages[(int)ConfigSection.MIDI];

                    Settings.MidiDevice = pageMIDI.GetPropertyValue<string>(0);

                    // FFmpeg
                    var pageFFmpeg = pages[(int)ConfigSection.FFmpeg];

                    Settings.FFmpegExecutablePath = pageFFmpeg.GetPropertyValue<string>(1);

                    // QWERTY
                    Array.Copy(qwertyKeys, Settings.QwertyKeys, Settings.QwertyKeys.Length);
                    Settings.UpdateKeyCodeMaps();

                    // Mac OS
                    var pageMacOS = pages[(int)ConfigSection.MacOS];
                    Settings.ReverseTrackPad   = pageMacOS.GetPropertyValue<bool>(0);
                    Settings.TrackPadMoveSensitity = pageMacOS.GetPropertyValue<int>(1);
                    Settings.TrackPadZoomSensitity = pageMacOS.GetPropertyValue<int>(2);

                    Settings.Save();
                }

                callback(r);
            });
        }
    }
}
