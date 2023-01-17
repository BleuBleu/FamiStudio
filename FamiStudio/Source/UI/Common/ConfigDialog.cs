using System;
using System.Collections.Generic;

namespace FamiStudio
{
    class ConfigDialog
    {
        enum ConfigSection
        {
            General,
            UserInterface,
            Input,
            Sound,
            Mixer,
            MIDI,
            FFmpeg,
            QWERTY,
            Mobile,
            Max
        };

        readonly string[] ConfigSectionNames =
        {
            "General",
            "Interface",
            "Input",
            "Sound",
            "Mixer",
            "MIDI",
            "FFmpeg",
            "QWERTY",
            "Mobile",
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

        public string[] IdealSequencerHeightStrings =
        {
            "10%",
            "15%",
            "20%",
            "25%",
            "30%",
            "35%",
            "40%",
            "45%",
            "50%",
        };

        // General
        private readonly string CheckUpdatesTooltip             = "When enabled, FamiStudio will check for updates every time you start the app.";
        private readonly string ShowTutorialTooltip             = "When enabled, the first time tutorial will be displayed next time the app is started.";
        private readonly string ClearUndoRedoTooltip            = "When enabled, the undo/redo stack will be cleared every time you save. Disabling this can help keep the memory usage down.";
        private readonly string ReviewAfterPlayTooltip          = "When enabled, FamiStudio will seek back to the previous play position after stopping the song.";
        private readonly string OpenLastTooltip                 = "When enabled, FamiStudio will open the last project you were working on when you last closed the app.";
        private readonly string AutosaveTooltip                 = "When enabled, a copy of your project will be save approximately every 2 minutes. This can prevent loosing data when a crash occurs.";
        private readonly string PatternNamePrefixTooltip        = "Prefix to add to automatically generated pattern names.";
        private readonly string PatternNameNumDigitsTooltip     = "Minimum number of digits to have in automatically generated pattern names.";
        private readonly string AutosaveFolderTooltip           = "Click to open the auto save folder.";

        // Input
        private readonly string TrackpadControlsTooltip         = "When enabled, the control scheme will be more friendly to trackpads/laptops. You will be able to swipe to pan and pinch to zoom. Note that pinch-to-zoom is not supported on Linux, you will have to hold CTRL or ALT and scroll up/down to zoom.";
        private readonly string AltLeftForMiddleTooltip         = "When enabled, Alt + left-click will be interpreted as a middle mouse click. Useful if your mouse does not have a middle button.\n\nNote that this will disable some functionalities that requires Alt, such as the ability to temporarily disable snapping.";
        private readonly string AltZoomAllowedTooltip           = "When enabled, Alt + right-click + panning up/down will zoom in/out. Useful if your mouse does not have a middle button.\n\nNote that this may disable some functionalities that requires Alt, such as the ability to temporarily disable snapping.";

        // UI
        private readonly string ScalingTooltip                  = "Overall scaling of the main FamiStudio window. Leave it to 'System' if you want FamiStudio to automatically detect it based on the system configuration.";
        private readonly string TimeFormatTooltip               = "Affects how time is displayed in the toolbar.";
        private readonly string FollowModeTooltip               = "Scrolling behavior when enabling follow mode in the toolbar.";
        private readonly string FollowingViewsTooltip           = "Affects which views will scroll when enabling follow mode in the toolbar.";
        private readonly string ScrollBarsTooltip               = "Affects the visibility and size of the scroll bars in the app.";
        private readonly string ShowFamitrackerStopNotesTooltip = "When enabled, partially transparent stop notes will be displayed whenever a note ends, when using FamiTracker tempo mode. This can help you to visually align note delays with stop notes.";
        private readonly string IdealSequencerHeightTooltip     = "The size of the sequencer, in % of the height of the window. This size maybe not be achievable if there are too many channels.";
        private readonly string AllowSequencerScrollTooltip     = "When enabled and the ideal sequencer height cannot be achieved, the sequencer will allow vertical scrolling.";
        private readonly string ShowRegisterViewerTooltip       = "When enabled, the 'Register' tab will be visible in the Project Explorer.";
        private readonly string UseOSDialogsTooltip             = "When enabled, FamiStudio will try to use the built-in operating system dialog to open/save files and display error messages.";

        // Sound
        private readonly string NumBufferedFramesTooltip        = "Number of frames the audio system will buffer. Make this as low as possible, increase if the sound becomes choppy. Larger numbers increase latency.";
        private readonly string StopInstrumentTooltip           = "Number of seconds to wait before stopping instruments that have a release part in their volume envelopes.";
        private readonly string PreventPoppingTooltip           = "When enabled, FamiStudio will use the sweep unit to prevent popping around certain notes on the 2 main square channels. Also known as 'Blargg's Smooth Vibrato' technique.";
        private readonly string ClampPeriodsTooltip             = "When enabled, FamiStudio will clamp periods and note values to their valid hardware range. Note that the NSF/Sound Engine does not do that, so disabling this option will result in more hardware-accurate sound where periods and notes can sometimes wrap-around.";
        private readonly string NoDragSoundTooltip              = "When enabled, FamiStudio will not emit sounds when dragging notes in the Piano Roll if the song is playing.";
        private readonly string MetronomeVolumeTooltip          = "Volume of the metronome.";

        // Mixer
        private readonly string GlobalVolumeTooltip             = "Global volume applied to all audio emulation, in db. When using multiple audio expansions, the volume can get very loud. You can use this to lower the overall volume and prevent clipping.";
        private readonly string ExpansionTooltip                = "Select the audio expansion for which you want to adjust the audio.";
        private readonly string ExpansionVolumeTooltip          = "Volume adjustment for the selected expansion, in db.";
        private readonly string ExpansionTrebleTooltip          = "Treble adjustment for the selected expansion, in db.";
        private readonly string ExpansionResetTooltip           = "Resets this expansion to the default settings.";

        // MIDI
        private readonly string MidiDeviceTooltip               = "The MIDI device that will be used to input notes.";

        // Mobile
        private readonly string AllowVibrationTooltip           = "When enabled, the phone will vibrate on long pressed, piano keys, etc.";
        private readonly string ForceLandscapeTooltip           = "When enabled, The app orientation will be forced to landscape.";

        // FFmpeg;
        private readonly string FFmpegPathTooltip               = "Path to FFmpeg executable. On Windows this is ffmpeg.exe. To download and install ffpmeg, check the link below.";


        private PropertyPage[] pages = new PropertyPage[(int)ConfigSection.Max];
        private MultiPropertyDialog dialog;
        private int[,] qwertyKeys; // We keep a copy here in case the user cancels.
        private Settings.ExpansionMix[] expansionMixer = new Settings.ExpansionMix[ExpansionType.Count];

        private int quertyRowIndex;
        private int quertyColIndex;

        public unsafe ConfigDialog(FamiStudioWindow win)
        {
            dialog = new MultiPropertyDialog(win, "FamiStudio Configuration", 550);
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

            dialog.SetPageVisible((int)ConfigSection.Input,   Platform.IsDesktop);
            dialog.SetPageVisible((int)ConfigSection.MIDI,    Platform.IsDesktop);
            dialog.SetPageVisible((int)ConfigSection.FFmpeg,  Platform.IsDesktop);
            dialog.SetPageVisible((int)ConfigSection.QWERTY,  Platform.IsDesktop);
            dialog.SetPageVisible((int)ConfigSection.Mobile,  Platform.IsMobile);
        }

        private string[] BuildDpiScalingList()
        {
            var scalings = DpiScaling.GetAvailableScalings();
            var list = new string[scalings.Length + 1];

            list[0] = "System";
            for (int i = 0; i < scalings.Length; i++)
                list[i + 1] = $"{scalings[i]}%";

            return list;
        }

        private int GetSequencerSizeIndex(int s)
        {
            return Utils.Clamp((s - 10) / 5, 0, IdealSequencerHeightStrings.Length - 1);
        }

        private PropertyPage CreatePropertyPage(PropertyPage page, ConfigSection section)
        {
            switch (section)
            {
                case ConfigSection.General:
                {
                    page.AddCheckBox("Check for updates:", Settings.CheckUpdates, CheckUpdatesTooltip); // 0
                    page.AddCheckBox("Show Tutorial at Startup:", Settings.ShowTutorial, ShowTutorialTooltip); // 1
                    page.AddCheckBox("Clear Undo/Redo on save:", Settings.ClearUndoRedoOnSave, ClearUndoRedoTooltip); // 2
                    page.AddCheckBox("Rewind after play:", Settings.RewindAfterPlay, ReviewAfterPlayTooltip); // 3
                    page.AddCheckBox("Open last project on start:", Settings.OpenLastProjectOnStart, OpenLastTooltip); // 4
                    page.AddCheckBox("Autosave a copy every 2 minutes:", Settings.AutoSaveCopy, AutosaveTooltip); // 5
                    page.AddTextBox("Pattern name prefix:", Settings.PatternNamePrefix, 64, PatternNamePrefixTooltip); // 6
                    page.AddNumericUpDown("Pattern name digits:", Settings.PatternNameNumDigits, 1, 4, PatternNameNumDigitsTooltip); // 7
                    page.AddButton(null, "Open Autosave folder", AutosaveFolderTooltip); // 8
                    page.PropertyClicked += GeneralPage_PropertyClicked;
                    page.SetPropertyVisible(0, Platform.IsDesktop);
                    page.SetPropertyVisible(2, Platform.IsDesktop);
                    page.SetPropertyVisible(4, Platform.IsDesktop);
                    page.SetPropertyVisible(5, Platform.IsDesktop);
                    page.SetPropertyVisible(8, Platform.IsDesktop);
                    break;
                }
                case ConfigSection.UserInterface:
                {
                    var scalingValues   = BuildDpiScalingList();
                    var scalingIndex    = Settings.DpiScaling == 0 ? 0 : Array.IndexOf(scalingValues, $"{Settings.DpiScaling}%");
                    var timeFormatIndex = Settings.TimeFormat < (int)TimeFormat.Max ? Settings.TimeFormat : 0;
                    var followModeIndex = Settings.FollowMode <= 0 ? 0 : Settings.FollowMode % FollowModeStrings.Length;
                    var followSyncIndex = Settings.FollowSync <= 0 ? 0 : Settings.FollowSync % FollowSyncStrings.Length;

                    page.AddDropDownList("Scaling (Requires restart):", scalingValues, scalingValues[scalingIndex], ScalingTooltip); // 0
                    page.AddDropDownList("Time Format:", TimeFormatStrings, TimeFormatStrings[timeFormatIndex], TimeFormatTooltip); // 1
                    page.AddDropDownList("Follow Mode:", FollowModeStrings, FollowModeStrings[followModeIndex], FollowModeTooltip);  // 2
                    page.AddDropDownList("Following Views:", FollowSyncStrings, FollowSyncStrings[followSyncIndex], FollowingViewsTooltip); // 3
                    page.AddDropDownList("Scroll Bars:", ScrollBarsStrings, ScrollBarsStrings[Settings.ScrollBars], ScrollBarsTooltip); // 4
                    page.AddDropDownList("Ideal Sequencer Height:", IdealSequencerHeightStrings, IdealSequencerHeightStrings[GetSequencerSizeIndex(Settings.IdealSequencerSize)], IdealSequencerHeightTooltip); // 5
                    page.AddCheckBox("Allow Sequencer Vertical Scrolling: ", Settings.AllowSequencerVerticalScroll, AllowSequencerScrollTooltip); // 6
                    page.AddCheckBox("Show FamiTracker Stop Notes:", Settings.ShowImplicitStopNotes, ShowFamitrackerStopNotesTooltip); // 7
                    page.AddCheckBox("Show Register Viewer Tab:", Settings.ShowRegisterViewer, ShowRegisterViewerTooltip); // 8
                    page.AddCheckBox("Use Operating System Dialogs:", Settings.UseOSDialogs, UseOSDialogsTooltip); // 9
                        
                    page.SetPropertyVisible(0, !Platform.IsMacOS); // No manual DPI selection on MacOS. 
                    page.SetPropertyVisible(3, Platform.IsDesktop);
                    page.SetPropertyVisible(4, Platform.IsDesktop);
                    page.SetPropertyVisible(5, Platform.IsDesktop);
                    page.SetPropertyVisible(6, Platform.IsDesktop);
                    page.SetPropertyVisible(9, Platform.IsDesktop && Platform.IsWindows); // Linux always has it disabled, MacOS always enabled, Windows can choose.
                    break;
                }
                case ConfigSection.Input:
                { 
                    page.AddCheckBox("Trackpad controls:", Settings.TrackPadControls, TrackpadControlsTooltip); // 0
                    page.AddCheckBox("Reverse trackpad scroll X:", Settings.ReverseTrackPadX); // 1
                    page.AddCheckBox("Reverse trackpad scroll Y:", Settings.ReverseTrackPadY); // 2
                    page.AddSlider("Trackpad scroll sensitivity:", Settings.TrackPadMoveSensitity, 1.0, 20.0, 1.0f, 1, "{0:0.0}"); // 3
                    page.AddSlider("Trackpad zoom sensitivity:", Settings.TrackPadZoomSensitity, 1.0, 20.0, 1.0, 1, "{0:0.0}"); // 4
                    page.AddCheckBox("ALT+Left emulates Middle:", Settings.AltLeftForMiddle, AltLeftForMiddleTooltip); // 5
                    page.AddCheckBox("ALT+Right zooms in/out:", Settings.AltZoomAllowed, AltZoomAllowedTooltip); // 6
                    page.SetPropertyEnabled(1, Settings.TrackPadControls);
                    page.SetPropertyEnabled(2, Settings.TrackPadControls);
                    page.SetPropertyEnabled(3, Settings.TrackPadControls);
                    page.SetPropertyEnabled(4, Settings.TrackPadControls);
                    page.SetPropertyVisible(4, Platform.IsMacOS);
                    page.PropertyChanged += InputPage_PropertyChanged;
                    break;
                }
                case ConfigSection.Sound:
                {
                    page.AddNumericUpDown("Number of buffered frames:", Settings.NumBufferedAudioFrames, 2, 16, NumBufferedFramesTooltip); // 0
                    page.AddNumericUpDown("Stop instruments after (sec):", Settings.InstrumentStopTime, 0, 10, StopInstrumentTooltip); // 1
                    page.AddCheckBox("Prevent popping on square channels:", Settings.SquareSmoothVibrato, PreventPoppingTooltip); // 2
                    page.AddCheckBox("Clamp periods and notes:", Settings.ClampPeriods, ClampPeriodsTooltip); // 3
                    page.AddCheckBox("Mute drag sounds during playback:", Settings.NoDragSoungWhenPlaying, NoDragSoundTooltip); // 4
                    page.AddSlider("Metronome volume:", Settings.MetronomeVolume, 1.0, 200.0, 1.0, 0, null, MetronomeVolumeTooltip); // 5
                    break;
                }
                case ConfigSection.Mixer:
                {
                    page.AddSlider("Global Volume:", Settings.GlobalVolume, -10.0, 3.0, 0.1, 1, "{0:+0.0;-0.0} dB", GlobalVolumeTooltip); // 0
                    page.AddDropDownList("Expansion:", ExpansionType.Names, ExpansionType.Names[0], ExpansionTooltip); // 1
                    page.AddSlider("Expansion Volume:", Settings.ExpansionMixerSettings[ExpansionType.None].volume, -10.0, 10.0, 0.1, 1, "{0:+0.0;-0.0} dB", ExpansionVolumeTooltip); // 2
                    page.AddSlider("Expansion Treble:", Settings.ExpansionMixerSettings[ExpansionType.None].treble, -100.0, 5.0, 0.1, 1, "{0:+0.0;-0.0} dB", ExpansionTrebleTooltip); // 3
                    page.AddButton(Platform.IsDesktop ? null : "Reset", "Reset expansion to default", ExpansionResetTooltip); // 4
                    page.AddLabel(Platform.IsDesktop ? null : "Note", "Note : These will have no effect on NSF, ROM, FDS and sound engine exports.", true); // 5
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

                    page.AddDropDownList("Device :", midiDevices.ToArray(), midiDevice, MidiDeviceTooltip); // 0
                    break;
                }
                case ConfigSection.FFmpeg:
                    page.AddLabel(null, "Video export requires FFmpeg. If you already have it, set the path to the ffmpeg executable by clicking the button below, otherwise follow the download link.", true); // 0
                    page.AddButton(null, Settings.FFmpegExecutablePath, FFmpegPathTooltip); // 1
                    page.AddLinkLabel(null, "Download FFmpeg here", "https://famistudio.org/doc/ffmpeg/"); // 3
                    page.PropertyClicked += FFmpegPage_PropertyClicked;
                    break;
                case ConfigSection.QWERTY:
                {
                    page.AddLabel(null, "Double click in the 2 last columns to assign a key. Right click to clear a key.", true); // 0
                    page.AddGrid(new[] { new ColumnDesc("Octave", 0.2f), new ColumnDesc("Note", 0.2f), new ColumnDesc("Key", 0.3f), new ColumnDesc("Key (alt)", 0.3f) }, GetQwertyMappingStrings(), 14); // 1
                    page.AddButton(null, "Reset to default");
                    page.PropertyClicked += QwertyPage_PropertyClicked;
                    page.SetColumnEnabled(1, 0, false);
                    page.SetColumnEnabled(1, 1, false);
                    break;
                }
                case ConfigSection.Mobile:
                { 
                    page.AddCheckBox("Allow vibration:", Settings.AllowVibration, AllowVibrationTooltip); // 0
                    page.AddCheckBox("Force Landscape:", Settings.ForceLandscape, ForceLandscapeTooltip); // 1
                    break;
                }
            }

            page.Build();
            pages[(int)section] = page;

            return page;
        }

        private void InputPage_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 0)
            {
                props.SetPropertyEnabled(1, (bool)value);
                props.SetPropertyEnabled(2, (bool)value);
                props.SetPropertyEnabled(3, (bool)value);
                props.SetPropertyEnabled(4, (bool)value);
            }
        }

        private void GeneralPage_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (click == ClickType.Button)
            {
                Platform.OpenUrl(Settings.GetAutoSaveFilePath());
            }
        }

        private void FFmpegPage_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (click == ClickType.Button)
            {
                if (propIdx == 1)
                {
                    var ffmpegExeFilter = Platform.IsWindows ? "FFmpeg Executable (ffmpeg.exe)|ffmpeg.exe" : "FFmpeg Executable (ffmpeg)|*.*";
                    var dummy = "";
                    var filename = Platform.ShowOpenFileDialog(dialog.ParentWindow, "Please select FFmpeg executable", ffmpegExeFilter, ref dummy);

                    if (filename != null)
                    {
                        props.SetPropertyValue(propIdx, filename);
                    }
                }
                else if (propIdx == 2)
                {
                    Platform.OpenUrl("https://famistudio.org/doc/ffmpeg/");
                }
            }
        }

        private void QwertyPage_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 1 && colIdx >= 2)
            {
                if (click == ClickType.Double)
                {
                    quertyRowIndex = rowIdx;
                    quertyColIndex = colIdx;

                    var dlg = new PropertyDialog(dialog.ParentWindow, "", 300, false, true);
                    dlg.Properties.AddLabel(null, "Press the new key or ESC to cancel.");
                    dlg.Properties.Build();
                    dlg.DialogKeyDown += Dlg_QwertyKeyDown;

                    dlg.ShowDialogAsync((r) => { });
                }
                else if (click == ClickType.Right)
                {
                    qwertyKeys[rowIdx, colIdx - 2] = -1;
                    pages[(int)ConfigSection.QWERTY].UpdateGrid(1, GetQwertyMappingStrings());
                }
            }
            else if (propIdx == 2 && click == ClickType.Button)
            {
                Array.Copy(Settings.DefaultQwertyScancodes, qwertyKeys, Settings.DefaultQwertyScancodes.Length);
                pages[(int)ConfigSection.QWERTY].UpdateGrid(1, GetQwertyMappingStrings());
            }
        }

        private void Dlg_QwertyKeyDown(Dialog dlg, KeyEventArgs e)
        {
            if (e.Key == Keys.Escape)
            {
                dlg.Close(DialogResult.Cancel);
            }
            else
            {
                AssignQwertyKey(quertyRowIndex, quertyColIndex - 2, e.Scancode);
                pages[(int)ConfigSection.QWERTY].UpdateGrid(1, GetQwertyMappingStrings());
                dlg.Close(DialogResult.OK);
            }
        }

        private string[,] GetQwertyMappingStrings()
        {
            var data = new string[37, 4];

            if (Platform.IsDesktop)
            {
                // Stop note.
                {
                    var k0 = qwertyKeys[0, 0];
                    var k1 = qwertyKeys[0, 1];

                    data[0, 0] = "N/A";
                    data[0, 1] = "Stop Note";
                    data[0, 2] = k0 < 0 ? "" : Platform.ScancodeToString(k0);
                    data[0, 3] = k1 < 0 ? "" : Platform.ScancodeToString(k1);
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
                    data[idx, 2] = k0 < 0 ? "" : Platform.ScancodeToString(k0);
                    data[idx, 3] = k1 < 0 ? "" : Platform.ScancodeToString(k1);
                }
            }
            return data;
        }

        void AssignQwertyKey(int idx, int keyIndex, int scancode)
        {
            // Unbind this key from anything.
            for (int i = 0; i < qwertyKeys.GetLength(0); i++)
            {
                for (int j = 0; j < qwertyKeys.GetLength(1); j++)
                {
                    if (qwertyKeys[i, j] == scancode)
                    {
                        qwertyKeys[i, j] = -1;
                    }
                }
            }

            qwertyKeys[idx, keyIndex] = scancode;
        }

        private void MixerPage_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 4 && click == ClickType.Button)
            {
                var expansion = props.GetSelectedIndex(1);
                expansionMixer[expansion] = Settings.DefaultExpansionMixerSettings[expansion];
                RefreshMixerSettings();
            }
        }

        private void RefreshMixerSettings()
        {
            var props = pages[(int)ConfigSection.Mixer];
            var expansion = props.GetSelectedIndex(1);

            props.SetPropertyValue(2, (double)expansionMixer[expansion].volume);
            props.SetPropertyValue(3, (double)expansionMixer[expansion].treble);
        }

        private void MixerPage_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            var expansion = props.GetSelectedIndex(1);

            if (propIdx == 1)
            {
                RefreshMixerSettings();
            }
            else if (propIdx == 2)
            {
                expansionMixer[expansion].volume = (float)(double)value;
            }
            else if (propIdx == 3)
            {
                expansionMixer[expansion].treble = (float)(double)value;
            }
        }

        public void ShowDialogAsync(Action<DialogResult> callback)
        {
            dialog.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    var pageGeneral = pages[(int)ConfigSection.General];
                    var pageUI = pages[(int)ConfigSection.UserInterface];
                    var pageInput = pages[(int)ConfigSection.Input];
                    var pageSound = pages[(int)ConfigSection.Sound];
                    var pageMixer = pages[(int)ConfigSection.Mixer];
                    var pageMIDI = pages[(int)ConfigSection.MIDI];
                    var pageFFmpeg = pages[(int)ConfigSection.FFmpeg];
                    var pageMobile = pages[(int)ConfigSection.Mobile];

                    // General
                    Settings.CheckUpdates = pageGeneral.GetPropertyValue<bool>(0);
                    Settings.ShowTutorial = pageGeneral.GetPropertyValue<bool>(1);
                    Settings.ClearUndoRedoOnSave = pageGeneral.GetPropertyValue<bool>(2);
                    Settings.RewindAfterPlay = pageGeneral.GetPropertyValue<bool>(3);
                    Settings.OpenLastProjectOnStart = pageGeneral.GetPropertyValue<bool>(4);
                    Settings.AutoSaveCopy = pageGeneral.GetPropertyValue<bool>(5);
                    Settings.PatternNamePrefix = pageGeneral.GetPropertyValue<string>(6);
                    Settings.PatternNameNumDigits = pageGeneral.GetPropertyValue<int>(7);

                    // UI
                    var scalingString = pageUI.GetPropertyValue<string>(0);
                    
                    Settings.DpiScaling = scalingString == "System" ? 0 : Utils.ParseIntWithTrailingGarbage(scalingString);
                    Settings.TimeFormat = pageUI.GetSelectedIndex(1);
                    Settings.FollowMode = pageUI.GetSelectedIndex(2);
                    Settings.FollowSync = pageUI.GetSelectedIndex(3);
                    Settings.ScrollBars = pageUI.GetSelectedIndex(4);
                    Settings.IdealSequencerSize = Utils.ParseIntWithTrailingGarbage(pageUI.GetPropertyValue<string>(5));
                    Settings.AllowSequencerVerticalScroll = pageUI.GetPropertyValue<bool>(6);
                    Settings.ShowImplicitStopNotes = pageUI.GetPropertyValue<bool>(7);
                    Settings.ShowRegisterViewer = pageUI.GetPropertyValue<bool>(8);
                    Settings.UseOSDialogs = pageUI.GetPropertyValue<bool>(9);

                    // Sound
                    Settings.NumBufferedAudioFrames = pageSound.GetPropertyValue<int>(0);
                    Settings.InstrumentStopTime = pageSound.GetPropertyValue<int>(1);
                    Settings.SquareSmoothVibrato = pageSound.GetPropertyValue<bool>(2);
                    Settings.ClampPeriods = pageSound.GetPropertyValue<bool>(3);
                    Settings.NoDragSoungWhenPlaying = pageSound.GetPropertyValue<bool>(4);
                    Settings.MetronomeVolume = (int)pageSound.GetPropertyValue<double>(5);

                    // Input
                    Settings.TrackPadControls = pageInput.GetPropertyValue<bool>(0);
                    Settings.ReverseTrackPadX = pageInput.GetPropertyValue<bool>(1);
                    Settings.ReverseTrackPadY = pageInput.GetPropertyValue<bool>(2);
                    Settings.TrackPadMoveSensitity = (float)pageInput.GetPropertyValue<double>(3);
                    Settings.TrackPadZoomSensitity = (float)pageInput.GetPropertyValue<double>(4);
                    Settings.AltLeftForMiddle = pageInput.GetPropertyValue<bool>(5);
                    Settings.AltZoomAllowed = pageInput.GetPropertyValue<bool>(6);

                    // Mixer.
                    Settings.GlobalVolume = (float)pageMixer.GetPropertyValue<double>(0);
                    Array.Copy(expansionMixer, Settings.ExpansionMixerSettings, Settings.ExpansionMixerSettings.Length);

                    // MIDI
                    Settings.MidiDevice = pageMIDI.GetPropertyValue<string>(0);

                    // FFmpeg
                    Settings.FFmpegExecutablePath = pageFFmpeg.GetPropertyValue<string>(1);

                    // QWERTY
                    Array.Copy(qwertyKeys, Settings.QwertyKeys, Settings.QwertyKeys.Length);
                    Settings.UpdateKeyCodeMaps();

                    // Mobile
                    Settings.AllowVibration = pageMobile.GetPropertyValue<bool>(0);
                    Settings.ForceLandscape = pageMobile.GetPropertyValue<bool>(1);

                    Settings.Save();
                }

                callback(r);
            });
        }
    }
}
