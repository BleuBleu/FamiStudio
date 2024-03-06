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
            Keys,
            Mobile,
            Max
        };

        public enum TimeFormat
        {
            PatternFrame,
            MinuteSecondsMilliseconds,
            Max
        }

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

        #region Localization

        LocalizedString[] TimeFormatStrings    = new LocalizedString[(int)TimeFormat.Max];
        LocalizedString[] FollowModeStrings    = new LocalizedString[2];
        LocalizedString[] FollowSyncStrings    = new LocalizedString[3];
        LocalizedString[] ScrollBarsStrings    = new LocalizedString[3];
        LocalizedString[] DpcmColorModeStrings = new LocalizedString[2];
        LocalizedString[] ConfigSectionNames   = new LocalizedString[(int)ConfigSection.Max];

        // Title
        LocalizedString Title;
        LocalizedString Verb;

        // General tooltips
        LocalizedString LanguageTooltip;
        LocalizedString CheckUpdatesTooltip;
        LocalizedString ShowTutorialTooltip;
        LocalizedString ClearUndoRedoTooltip;
        LocalizedString ReviewAfterPlayTooltip;
        LocalizedString OpenLastTooltip;
        LocalizedString AutosaveTooltip;
        LocalizedString PatternNamePrefixTooltip;
        LocalizedString PatternNameNumDigitsTooltip;
        LocalizedString AutosaveFolderTooltip;

        // General labels
        LocalizedString LanguageLabel;
        LocalizedString CheckForUpdatesLabel;
        LocalizedString ShowTutorialLabel;
        LocalizedString ClearUndoLabel;
        LocalizedString RewindAfterPlayLabel;
        LocalizedString OpenLastProjectLabel;
        LocalizedString AutosaveLabel;
        LocalizedString PatternPrefixLabel;
        LocalizedString PatternDigitsLabel;
        LocalizedString OpenAutosaveFolderLabel;

        // UI tooltips
        LocalizedString ScalingTooltip;
        LocalizedString TimeFormatTooltip;
        LocalizedString FollowModeTooltip;
        LocalizedString FollowingViewsTooltip;
        LocalizedString FollowRangeTooltip;
        LocalizedString ScrollBarsTooltip;
        LocalizedString ShowFamitrackerStopNotesTooltip;
        LocalizedString IdealSequencerHeightTooltip;
        LocalizedString DpcmColorModeTooltip;
        LocalizedString AllowSequencerScrollTooltip;
        LocalizedString ShowRegisterViewerTooltip;
        LocalizedString UseOSDialogsTooltip;
        LocalizedString SystemOption;

        // UI labels
        LocalizedString ScalingLabel;
        LocalizedString TimeFormatLabel;
        LocalizedString FollowModeLabel;
        LocalizedString FollowViewsLabel;
        LocalizedString FollowRangeLabel;
        LocalizedString ScrollBarsLabel;
        LocalizedString IdealSeqHeightLabel;
        LocalizedString DpcmColorModeLabel;
        LocalizedString AllowSeqVertScrollLabel;
        LocalizedString ShowFamitrackerStopLabel;
        LocalizedString ShowRegisterViewerLabel;
        LocalizedString UseOSDialogsLabel;

        // Input tooltips
        LocalizedString TrackpadControlsTooltip;
        LocalizedString AltLeftForMiddleTooltip;
        LocalizedString AltZoomAllowedTooltip;

        // Input labels
        LocalizedString TrackpadControlsLabel;
        LocalizedString ReverseTrackpadXLabel;
        LocalizedString ReverseTrackpadYLabel;
        LocalizedString TrackpadSensitivityLabel;
        LocalizedString TrackpadZoomSensitivityLabel;
        LocalizedString AltLeftEmulatesMiddle;
        LocalizedString AltRightZoomsInOut;

        // Sound tooltips
        LocalizedString AudioBufferSizeTooltip;
        LocalizedString NumBufferedFramesTooltip;
        LocalizedString StopInstrumentTooltip;
        LocalizedString PreventPoppingTooltip;
        LocalizedString N163MixerTooltip;
        LocalizedString ClampPeriodsTooltip;
        LocalizedString NoDragSoundTooltip;
        LocalizedString MetronomeVolumeTooltip;

        // Sound labels
        LocalizedString AudioBufferSizeLabel;
        LocalizedString NumBufferFramesLabel;
        LocalizedString StopInstrumentAfterLabel;
        LocalizedString PreventPoppingLabel;
        LocalizedString MixN163Label;
        LocalizedString ClampPeriodsLabel;
        LocalizedString MuteDragSoundsLabel;
        LocalizedString MetronomeVolumeLabel;

        // MIDI tooltips
        LocalizedString MidiDeviceTooltip;

        // MIDI labels
        LocalizedString DeviceLabel;

        // FFmpeg tooltips
        LocalizedString FFmpegPathTooltip;

        // FFmpeg labels
        LocalizedString FFmpegRequiredLabel;
        LocalizedString FFmpegDownloadLabel;
        LocalizedString FFmpegSelectExeTitle;

        // Keys labels
        LocalizedString DoubleClickLabel;
        LocalizedString ResetDefaultLabel;
        LocalizedString PressKeyOrCancel;
        LocalizedString ActionColumn;
        LocalizedString KeyColumn;
        LocalizedString KeyAltColumn;

        // Mobile tooltips
        LocalizedString AllowVibrationTooltip;
        LocalizedString ForceLandscapeTooltip;

        // Mobile labels
        LocalizedString AllowVibrationLabel;
        LocalizedString ForceLandscapeLabel;

        #endregion

        private PropertyPage[] pages = new PropertyPage[(int)ConfigSection.Max];
        private MultiPropertyDialog dialog;
        private List<Shortcut> shortcuts; // We keep a copy here in case the user cancels.
        private ExpansionMixer[] expansionMixer = new ExpansionMixer[ExpansionType.Count];
        private MixerProperties mixerProperties;

        private int keysRowIndex;
        private int keysColIndex;

        public unsafe ConfigDialog(FamiStudioWindow win)
        {
            Localization.Localize(this);

            dialog = new MultiPropertyDialog(win, Title, 570);
            dialog.SetVerb(Verb, true);

            // Keep a copy of keyboart shortcuts
            shortcuts = Shortcut.CloneList(Settings.AllShortcuts);

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
            dialog.SetPageVisible((int)ConfigSection.Keys,    Platform.IsDesktop);
            dialog.SetPageVisible((int)ConfigSection.Mobile,  Platform.IsMobile);
        }

        private string[] BuildDpiScalingList()
        {
            var scalings = DpiScaling.GetAvailableScalings();
            var list = new string[scalings.Length + 1];

            list[0] = SystemOption;
            for (int i = 0; i < scalings.Length; i++)
                list[i + 1] = $"{scalings[i]}%";

            return list;
        }

        private string[] BuildLanguageList()
        {
            var languages = Localization.ToStringArray(LanguageType.LocalizedNames);
            var list = new string[languages.Length + 1];

            list[0] = SystemOption;
            for (int i = 0; i < languages.Length; i++)
                list[i + 1] = languages[i];

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
                    var languageValues = BuildLanguageList();
                    var languageIndex  = Localization.GetIndexForLanguageCode(Settings.LanguageCode) + 1;

                    page.AddDropDownList(LanguageLabel.Colon, languageValues, languageValues[languageIndex], LanguageTooltip); // 0
                    page.AddCheckBox(CheckForUpdatesLabel.Colon, Settings.CheckUpdates, CheckUpdatesTooltip); // 1
                    page.AddCheckBox(ShowTutorialLabel.Colon, Settings.ShowTutorial, ShowTutorialTooltip); // 2
                    page.AddCheckBox(ClearUndoLabel.Colon, Settings.ClearUndoRedoOnSave, ClearUndoRedoTooltip); // 3
                    page.AddCheckBox(RewindAfterPlayLabel.Colon, Settings.RewindAfterPlay, ReviewAfterPlayTooltip); // 4
                    page.AddCheckBox(OpenLastProjectLabel.Colon, Settings.OpenLastProjectOnStart, OpenLastTooltip); // 5
                    page.AddCheckBox(AutosaveLabel.Colon, Settings.AutoSaveCopy, AutosaveTooltip); // 6
                    page.AddTextBox(PatternPrefixLabel.Colon, Settings.PatternNamePrefix, 64, false, PatternNamePrefixTooltip); // 7
                    page.AddNumericUpDown(PatternDigitsLabel.Colon, Settings.PatternNameNumDigits, 1, 4, 1, PatternNameNumDigitsTooltip); // 8
                    page.AddButton(null, OpenAutosaveFolderLabel, AutosaveFolderTooltip); // 9
                    page.PropertyClicked += GeneralPage_PropertyClicked;
                    page.SetPropertyVisible(1, Platform.IsDesktop);
                    page.SetPropertyVisible(3, Platform.IsDesktop);
                    page.SetPropertyVisible(5, Platform.IsDesktop);
                    page.SetPropertyVisible(6, Platform.IsDesktop);
                    page.SetPropertyVisible(9, Platform.IsDesktop);
                    break;
                }
                case ConfigSection.UserInterface:
                {
                    var scalingValues   = BuildDpiScalingList();
                    var scalingIndex    = Settings.DpiScaling == 0 ? 0 : Array.IndexOf(scalingValues, $"{Settings.DpiScaling}%");
                    var timeFormatIndex = Settings.TimeFormat < (int)TimeFormat.Max ? Settings.TimeFormat : 0;
                    var followModeIndex = Settings.FollowMode <= 0 ? 0 : Settings.FollowMode % FollowModeStrings.Length;
                    var followSyncIndex = Settings.FollowSync <= 0 ? 0 : Settings.FollowSync % FollowSyncStrings.Length;

                    page.AddDropDownList(ScalingLabel.Colon, scalingValues, scalingValues[scalingIndex], ScalingTooltip); // 0
                    page.AddDropDownList(TimeFormatLabel.Colon, Localization.ToStringArray(TimeFormatStrings), TimeFormatStrings[timeFormatIndex], TimeFormatTooltip); // 1
                    page.AddDropDownList(FollowModeLabel.Colon, Localization.ToStringArray(FollowModeStrings), FollowModeStrings[followModeIndex], FollowModeTooltip);  // 2
                    page.AddDropDownList(FollowViewsLabel.Colon, Localization.ToStringArray(FollowSyncStrings), FollowSyncStrings[followSyncIndex], FollowingViewsTooltip); // 3
                    page.AddSlider(FollowRangeLabel.Colon, Settings.FollowPercent, 0.05, 0.95, 0.01f, 2, "{0:P0}", FollowRangeTooltip); // 4
                    page.AddDropDownList(ScrollBarsLabel.Colon, Localization.ToStringArray(ScrollBarsStrings), ScrollBarsStrings[Settings.ScrollBars], ScrollBarsTooltip); // 5
                    page.AddDropDownList(IdealSeqHeightLabel.Colon, IdealSequencerHeightStrings, IdealSequencerHeightStrings[GetSequencerSizeIndex(Settings.IdealSequencerSize)], IdealSequencerHeightTooltip); // 6
                    page.AddDropDownList(DpcmColorModeLabel.Colon, Localization.ToStringArray(DpcmColorModeStrings), DpcmColorModeStrings[Settings.DpcmColorMode], DpcmColorModeTooltip); // 7
                    page.AddCheckBox(AllowSeqVertScrollLabel.Colon, Settings.AllowSequencerVerticalScroll, AllowSequencerScrollTooltip); // 8
                    page.AddCheckBox(ShowFamitrackerStopLabel.Colon, Settings.ShowImplicitStopNotes, ShowFamitrackerStopNotesTooltip); // 9
                    page.AddCheckBox(ShowRegisterViewerLabel.Colon, Settings.ShowRegisterViewer, ShowRegisterViewerTooltip); // 10
                    page.AddCheckBox(UseOSDialogsLabel.Colon, Settings.UseOSDialogs, UseOSDialogsTooltip); // 11
                        
                    page.SetPropertyVisible(0, !Platform.IsMacOS); // No manual DPI selection on MacOS. 
                    page.SetPropertyVisible(3, Platform.IsDesktop);
                    page.SetPropertyVisible(5, Platform.IsDesktop);
                    page.SetPropertyVisible(6, Platform.IsDesktop);
                    page.SetPropertyVisible(8, Platform.IsDesktop);
                    page.SetPropertyVisible(11, Platform.IsDesktop && Platform.IsWindows); // Linux always has it disabled, MacOS always enabled, Windows can choose.
                    break;
                }
                case ConfigSection.Input:
                { 
                    page.AddCheckBox(TrackpadControlsLabel.Colon, Settings.TrackPadControls, TrackpadControlsTooltip); // 0
                    page.AddCheckBox(ReverseTrackpadXLabel.Colon, Settings.ReverseTrackPadX); // 1
                    page.AddCheckBox(ReverseTrackpadYLabel.Colon, Settings.ReverseTrackPadY); // 2
                    page.AddSlider(TrackpadSensitivityLabel.Colon, Settings.TrackPadMoveSensitity, 1.0, 20.0, 1.0f, 1, "{0:0.0}"); // 3
                    page.AddSlider(TrackpadZoomSensitivityLabel.Colon, Settings.TrackPadZoomSensitity, 1.0, 20.0, 1.0, 1, "{0:0.0}"); // 4
                    page.AddCheckBox(AltLeftEmulatesMiddle.Colon, Settings.AltLeftForMiddle, AltLeftForMiddleTooltip); // 5
                    page.AddCheckBox(AltRightZoomsInOut.Colon, Settings.AltZoomAllowed, AltZoomAllowedTooltip); // 6
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
                    page.AddNumericUpDown(AudioBufferSizeLabel.Colon, Settings.AudioBufferSize, 1, 500, 1, AudioBufferSizeTooltip); // 0
                    page.AddNumericUpDown(NumBufferFramesLabel.Colon, Settings.NumBufferedFrames, 0, 16, 1, NumBufferedFramesTooltip); // 1
                    page.AddNumericUpDown(StopInstrumentAfterLabel.Colon, Settings.InstrumentStopTime, 0, 10, 1, StopInstrumentTooltip); // 2
                    page.AddCheckBox(PreventPoppingLabel.Colon, Settings.SquareSmoothVibrato, PreventPoppingTooltip); // 3
                    page.AddCheckBox(MixN163Label.Colon, Settings.N163Mix, N163MixerTooltip); // 4
                    page.AddCheckBox(ClampPeriodsLabel.Colon, Settings.ClampPeriods, ClampPeriodsTooltip); // 5
                    page.AddCheckBox(MuteDragSoundsLabel.Colon, Settings.NoDragSoungWhenPlaying, NoDragSoundTooltip); // 6
                    page.AddSlider(MetronomeVolumeLabel.Colon, Settings.MetronomeVolume, 1.0, 200.0, 1.0, 0, null, MetronomeVolumeTooltip); // 7
                    break;
                }
                case ConfigSection.Mixer:
                {
                    page.SetScrolling(400); // MATTT
                    mixerProperties = new MixerProperties(page, null);
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

                    page.AddDropDownList(DeviceLabel.Colon, midiDevices.ToArray(), midiDevice, MidiDeviceTooltip); // 0
                    break;
                }
                case ConfigSection.FFmpeg:
                    page.AddLabel(null, FFmpegRequiredLabel, true); // 0
                    page.AddFileTextBox(null, Settings.FFmpegExecutablePath, 0, FFmpegPathTooltip); // 1
                    page.AddLinkLabel(null, FFmpegDownloadLabel, "https://famistudio.org/doc/ffmpeg/"); // 3
                    page.PropertyClicked += FFmpegPage_PropertyClicked;
                    break;
                case ConfigSection.Keys:
                {
                    page.AddLabel(null, DoubleClickLabel, true); // 0
                    page.AddGrid("Keys", new[] { new ColumnDesc(ActionColumn, 0.4f), new ColumnDesc(KeyColumn, 0.3f), new ColumnDesc(KeyAltColumn, 0.3f) }, GetKeyboardShortcutStrings(), 14); // 1
                    page.AddButton(null, ResetDefaultLabel);
                    page.PropertyClicked += KeyboardPage_PropertyClicked;
                    page.PropertyCellEnabled += KeyboardPage_PropertyCellEnabled;
                    page.SetColumnEnabled(1, 0, false);
                    break;
                }
                case ConfigSection.Mobile:
                { 
                    page.AddCheckBox(AllowVibrationLabel.Colon, Settings.AllowVibration, AllowVibrationTooltip); // 0
                    page.AddCheckBox(ForceLandscapeLabel.Colon, Settings.ForceLandscape, ForceLandscapeTooltip); // 1
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
                    var filename = Platform.ShowOpenFileDialog(FFmpegSelectExeTitle, ffmpegExeFilter, ref dummy);

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

        private void KeyboardPage_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 1 && colIdx >= 1)
            {
                if (click == ClickType.Double)
                {
                    keysRowIndex = rowIdx;
                    keysColIndex = colIdx;

                    var dlg = new PropertyDialog(dialog.ParentWindow, "", 300, false, true);
                    dlg.Properties.AddLabel(null, PressKeyOrCancel);
                    dlg.Properties.Build();
                    dlg.DialogKeyDown += Dlg_KeyboardKeyDown;

                    dlg.ShowDialogAsync((r) => { });
                }
                else if (click == ClickType.Right)
                {
                    shortcuts[rowIdx].Clear(colIdx - 1);
                    pages[(int)ConfigSection.Keys].UpdateGrid(1, GetKeyboardShortcutStrings());
                }
            }
            else if (propIdx == 2 && click == ClickType.Button)
            {
                shortcuts = Shortcut.CloneList(Settings.DefaultShortcuts);
                pages[(int)ConfigSection.Keys].UpdateGrid(1, GetKeyboardShortcutStrings());
            }
        }

        private bool KeyboardPage_PropertyCellEnabled(PropertyPage props, int propIdx, int rowIdx, int colIdx)
        {
            return colIdx != 2 || shortcuts[rowIdx].AllowTwoShortcuts;
        }

        private void Dlg_KeyboardKeyDown(Dialog dlg, KeyEventArgs e)
        {
            if (e.Key == Keys.Escape)
            {
                dlg.Close(DialogResult.Cancel);
            }
            else if (
                e.Key != Keys.LeftControl &&
                e.Key != Keys.LeftAlt &&
                e.Key != Keys.LeftShift &&
                e.Key != Keys.LeftSuper &&
                e.Key != Keys.RightControl &&
                e.Key != Keys.RightAlt &&
                e.Key != Keys.RightShift &&
                e.Key != Keys.RightSuper)
            {
                if (!shortcuts[keysRowIndex].AllowModifiers)
                    e = new KeyEventArgs(e.Key, new ModifierKeys(0), false, 0);

                AssignKeyboardKey(keysRowIndex, keysColIndex - 1, e);
                pages[(int)ConfigSection.Keys].UpdateGrid(1, GetKeyboardShortcutStrings());
                dlg.Close(DialogResult.OK);
            }

            e.Handled = true;
        }

        private string[,] GetKeyboardShortcutStrings()
        {
            var data = new string[shortcuts.Count, 3];

            if (Platform.IsDesktop)
            {
                for (int i = 0; i < shortcuts.Count; i++)
                {
                    var s = shortcuts[i];

                    data[i, 0] = s.DisplayName;
                    data[i, 1] = s.ToDisplayString(0);
                    data[i, 2] = s.ToDisplayString(1);
                }
            }

            return data;
        }

        void AssignKeyboardKey(int idx, int keyIndex, KeyEventArgs e)
        {
            // Unbind this key from anything.
            for (int i = 0; i < shortcuts.Count; i++)
            {
                var s = shortcuts[i];

                for (int j = 0; j < 2; j++)
                {
                    if (s.Matches(e, j))
                    {
                        s.Clear(j);
                    }
                }
            }

            //shortcuts[idx].ScanCodes[keyIndex] = e.Scancode;
            shortcuts[idx].KeyValues[keyIndex] = e.Key;
            shortcuts[idx].Modifiers[keyIndex] = e.Modifiers;
        }

        private void MixerPage_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 7 && click == ClickType.Button)
            {
                var expansion = props.GetSelectedIndex(3);
                expansionMixer[expansion] = ExpansionMixer.DefaultExpansionMixerSettings[expansion];
                RefreshMixerSettings();
            }
        }

        private void RefreshMixerSettings()
        {
            var props = pages[(int)ConfigSection.Mixer];
            var expansion = props.GetSelectedIndex(3);

            props.SetPropertyValue(4, (double)expansionMixer[expansion].VolumeDb);
            props.SetPropertyValue(5, (double)expansionMixer[expansion].TrebleDb);
            props.SetPropertyValue(6, (double)expansionMixer[expansion].TrebleRolloffHz);
        }

        private void MixerPage_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            var expansion = props.GetSelectedIndex(3);

            if (propIdx == 3)
            {
                RefreshMixerSettings();
            }
            else if (propIdx == 4)
            {
                expansionMixer[expansion].VolumeDb = (float)(double)value;
            }
            else if (propIdx == 5)
            {
                expansionMixer[expansion].TrebleDb = (float)(double)value;
            }
            else if (propIdx == 6)
            {
                expansionMixer[expansion].TrebleRolloffHz = (int)(double)value;
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
                    Settings.LanguageCode = pageGeneral.GetSelectedIndex(0) == 0 ? "" : Localization.LanguageCodes[pageGeneral.GetSelectedIndex(0) - 1];
                    Settings.CheckUpdates = pageGeneral.GetPropertyValue<bool>(1);
                    Settings.ShowTutorial = pageGeneral.GetPropertyValue<bool>(2);
                    Settings.ClearUndoRedoOnSave = pageGeneral.GetPropertyValue<bool>(3);
                    Settings.RewindAfterPlay = pageGeneral.GetPropertyValue<bool>(4);
                    Settings.OpenLastProjectOnStart = pageGeneral.GetPropertyValue<bool>(5);
                    Settings.AutoSaveCopy = pageGeneral.GetPropertyValue<bool>(6);
                    Settings.PatternNamePrefix = pageGeneral.GetPropertyValue<string>(7);
                    Settings.PatternNameNumDigits = pageGeneral.GetPropertyValue<int>(8);

                    // UI
                    var scalingString = pageUI.GetPropertyValue<string>(0);
                    
                    Settings.DpiScaling = scalingString == SystemOption ? 0 : Utils.ParseIntWithTrailingGarbage(scalingString);
                    Settings.TimeFormat = pageUI.GetSelectedIndex(1);
                    Settings.FollowMode = pageUI.GetSelectedIndex(2);
                    Settings.FollowSync = pageUI.GetSelectedIndex(3);
                    Settings.FollowPercent = (float)pageUI.GetPropertyValue<double>(4);
                    Settings.ScrollBars = pageUI.GetSelectedIndex(5);
                    Settings.IdealSequencerSize = Utils.ParseIntWithTrailingGarbage(pageUI.GetPropertyValue<string>(6));
                    Settings.DpcmColorMode = pageUI.GetSelectedIndex(7);
                    Settings.AllowSequencerVerticalScroll = pageUI.GetPropertyValue<bool>(8);
                    Settings.ShowImplicitStopNotes = pageUI.GetPropertyValue<bool>(9);
                    Settings.ShowRegisterViewer = pageUI.GetPropertyValue<bool>(10);
                    Settings.UseOSDialogs = pageUI.GetPropertyValue<bool>(11);

                    // Sound
                    var newAudioBufferSize = pageSound.GetPropertyValue<int>(0);
                    var newNumBufferedFrames = pageSound.GetPropertyValue<int>(1);
                    var newN163Mix = pageSound.GetPropertyValue<bool>(4);

                    if (Settings.AudioBufferSize   != newAudioBufferSize   ||
                        Settings.NumBufferedFrames != newNumBufferedFrames ||
                        Settings.N163Mix           != newN163Mix)
                    {
                        // Use "Yes" as a special code to mean to recreate audio players.
                        r = DialogResult.Yes;
                    }

                    Settings.AudioBufferSize = newAudioBufferSize;
                    Settings.NumBufferedFrames = newNumBufferedFrames;
                    Settings.InstrumentStopTime = pageSound.GetPropertyValue<int>(2);
                    Settings.SquareSmoothVibrato = pageSound.GetPropertyValue<bool>(3);
                    Settings.N163Mix = newN163Mix;
                    Settings.ClampPeriods = pageSound.GetPropertyValue<bool>(5);
                    Settings.NoDragSoungWhenPlaying = pageSound.GetPropertyValue<bool>(6);
                    Settings.MetronomeVolume = (int)pageSound.GetPropertyValue<double>(7);

                    // Input
                    Settings.TrackPadControls = pageInput.GetPropertyValue<bool>(0);
                    Settings.ReverseTrackPadX = pageInput.GetPropertyValue<bool>(1);
                    Settings.ReverseTrackPadY = pageInput.GetPropertyValue<bool>(2);
                    Settings.TrackPadMoveSensitity = (float)pageInput.GetPropertyValue<double>(3);
                    Settings.TrackPadZoomSensitity = (float)pageInput.GetPropertyValue<double>(4);
                    Settings.AltLeftForMiddle = pageInput.GetPropertyValue<bool>(5);
                    Settings.AltZoomAllowed = pageInput.GetPropertyValue<bool>(6);

                    // Mixer.
                    mixerProperties.Apply();

                    // MIDI
                    Settings.MidiDevice = pageMIDI.GetPropertyValue<string>(0);

                    // FFmpeg
                    Settings.FFmpegExecutablePath = pageFFmpeg.GetPropertyValue<string>(1);

                    // Keys
                    for (int i = 0; i < shortcuts.Count; i++)
                    {
                        //Settings.AllShortcuts[i].ScanCodes[0] = shortcuts[i].ScanCodes[0];
                        //Settings.AllShortcuts[i].ScanCodes[1] = shortcuts[i].ScanCodes[1];
                        Settings.AllShortcuts[i].KeyValues[0] = shortcuts[i].KeyValues[0];
                        Settings.AllShortcuts[i].KeyValues[1] = shortcuts[i].KeyValues[1];
                        Settings.AllShortcuts[i].Modifiers[0] = shortcuts[i].Modifiers[0];
                        Settings.AllShortcuts[i].Modifiers[1] = shortcuts[i].Modifiers[1];
                    }

                    // Mobile
                    Settings.AllowVibration = pageMobile.GetPropertyValue<bool>(0);
                    Settings.ForceLandscape = pageMobile.GetPropertyValue<bool>(1);

                    Settings.Save();
                    Settings.NotifyKeyboardShortcutsChanged();
                }

                callback(r);
            });
        }
    }
}
