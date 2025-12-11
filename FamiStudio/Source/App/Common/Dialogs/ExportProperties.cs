using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// WavMp3,
// Video,
// Nsf,
// Rom,
// Midi,
// VGM,
// Text,
// FamiTracker,
// FamiStudioMusic,
// FamiStudioSfx,
// FamiTone2Music,
// FamiTone2Sfx,
// Share,
namespace FamiStudio
{
    class WavMp3ExportSettings
    {
        private PropertyPage props;
        private Song song;

        private int patternIdx = -1;
        private int minPatternIdx = -1;
        private int maxPatternIdx = -1;

        private int firstPropIdx = -1;
        private int famitrackerTempoPropIdx = -1;
        private int famitrackerSpeedPropIdx = -1;
        private int notesPerBeatPropIdx = -1;
        private int notesPerPatternPropIdx = -1;
        private int bpmLabelPropIdx = -1;
        private int famistudioBpmPropIdx = -1;
        private int framesPerNotePropIdx = -1;
        private int groovePropIdx = -1;
        private int groovePadPropIdx = -1;

        // WAV/MP3 tooltips           
        LocalizedString WavFormatTooltip;
        LocalizedString SampleRateTooltip;
        LocalizedString AudioBitRateTooltip;
        LocalizedString LoopModeTooltip;
        LocalizedString LoopCountTooltip;
        LocalizedString DurationTooltip;
        LocalizedString DelayTooltip;
        LocalizedString SeperateFilesTooltip;
        LocalizedString SeperateIntroTooltip;
        LocalizedString StereoTooltip;
        LocalizedString ChannelGridTooltip;
        LocalizedString ChannelGridTooltipVid;

        // WAV/MP3 labels
        LocalizedString FormatLabel;
        LocalizedString SampleRateLabel;
        LocalizedString BitRateLabel;
        LocalizedString ModeLabel;
        LocalizedString DurationSecLabel;
        LocalizedString SeparateChannelFilesLabel;
        LocalizedString SeparateIntroFileLabel;
        LocalizedString StereoLabel;
        LocalizedString ChannelsLabel;
        LocalizedString LoopNTimesOption;
        LocalizedString DurationOption;


        public WavMp3ExportSettings(PropertyPage props, Song song)
        {
            Localization.Localize(this);

            this.song = song;
            this.props = props;

            props.PropertyChanged += Props_PropertyChanged;
        }

        public void AddProperties()
        {
            props.AddDropDownList(SongLabel.Colon, songNames, app.SelectedSong.Name, SingleSongTooltip); // 0
            props.AddDropDownList(FormatLabel.Colon, AudioFormatType.Names, AudioFormatType.Names[0], WavFormatTooltip); // 1
            props.AddDropDownList(SampleRateLabel.Colon, new[] { "11025", "22050", "44100", "48000" }, "44100", SampleRateTooltip); // 2
            props.AddDropDownList(BitRateLabel.Colon, new[] { "96", "112", "128", "160", "192", "224", "256" }, "192", AudioBitRateTooltip); // 3
            props.AddDropDownList(ModeLabel.Colon, new string[] { LoopNTimesOption, DurationOption }, LoopNTimesOption, LoopModeTooltip); // 4
            props.AddNumericUpDown(LoopCountLabel.Colon, 1, 1, 10, 1, LoopCountTooltip); // 5
            props.AddNumericUpDown(DurationSecLabel.Colon, 120, 1, 1000, 1, DurationTooltip); // 6
            props.AddNumericUpDown(AudioDelayMsLabel.Colon, 0, 0, 100, 1, DelayTooltip); // 7
            props.AddCheckBox(SeparateChannelFilesLabel.Colon, false, SeperateFilesTooltip); // 8
            props.AddCheckBox(SeparateIntroFileLabel.Colon, false, SeperateIntroTooltip); // 9
            props.AddCheckBox(StereoLabel.Colon, project.OutputsStereoAudio, StereoTooltip); // 10
            props.AddGrid(ChannelsLabel, new[] {
                        new ColumnDesc("", 0.0f, ColumnType.CheckBox),
                        new ColumnDesc(ChannelColumn, 0.4f),
                        new ColumnDesc(PanColumn, 0.6f, 0, 100, (o) => FormattableString.Invariant($"{(int)o} %"))
                    }, GetDefaultChannelsGridData(false, false, app.SelectedSong, out _), 7, ChannelGridTooltip); // 11
            props.SetPropertyEnabled(3, false);
            props.SetPropertyEnabled(6, false);
            props.SetPropertyVisible(8, Platform.IsDesktop); // No separate files on mobile.
            props.SetPropertyVisible(9, Platform.IsDesktop); // No separate intro on mobile.
            props.SetPropertyEnabled(10, !project.OutputsStereoAudio); // Force stereo for EPSM.
            props.SetColumnEnabled(11, 2, project.OutputsStereoAudio);
            props.PropertyChanged += WavMp3_PropertyChanged;
            props.PropertyClicked += WavMp3_PropertyClicked;

        }

        private void Props_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
        }

        private void UpdateWarnings()
        {
        }

        public void EnableProperties(bool enabled)
        {
            for (var i = firstPropIdx; i < props.PropertyCount; i++)
                props.SetPropertyEnabled(i, enabled);
        }

        private void FinishApply(Action callback)
        {
        }

        public void ApplyAsync(FamiStudioWindow win, bool custom, Action callback)
        {
        }
    }
}
