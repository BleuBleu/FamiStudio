using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class ExportDialog
    {
        enum ExportFormat
        {
            WavMp3,
            Video,
            Nsf,
            Rom,
            Midi,
            VGM,
            Text,
            FamiTracker,
            FamiStudioMusic,
            FamiStudioSfx,
            FamiTone2Music,
            FamiTone2Sfx,
            Share,
            Max
        };

        LocalizedString[] ExportFormatNames = new LocalizedString[(int)ExportFormat.Max];

        private static readonly string[] ExportIcons =
        {
            "ExportWav",
            "ExportVideo",
            "ExportNsf",
            "ExportRom",
            "ExportMIDI",
            "ExportVGM",
            "ExportText",
            "ExportFamiTracker",
            "ExportFamiStudioEngine",
            "ExportFamiStudioEngine",
            "ExportFamiTone2",
            "ExportFamiTone2",
            "ExportShare"
        };

        //private static readonly int[] ExportScrolling = new[] { 0, 500, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        private Project project;
        private MultiPropertyDialog dialog;
        private uint lastProjectCrc;
        private string lastExportFilename;
        private FamiStudio app;

        private bool canExportToRom         = true;
        private bool canExportToFamiTracker = true;
        private bool canExportToFamiTone2   = true;
        private bool canExportToSoundEngine = true;
        private bool canExportToVideo       = true;

        public delegate void EmptyDelegate();
        public event EmptyDelegate Exporting;

        #region Localization

        // Title
        LocalizedString Title;

        // Reset
        LocalizedString ResetTitle;
        LocalizedString ResetMessage;
        LocalizedString ResetDefaultsLabel;

        // Export formats (for result message)
        LocalizedString FormatAudioMessage;
        LocalizedString FormatVideoMessage;
        LocalizedString FormatNsfMessage;
        LocalizedString FormatRomMessage;
        LocalizedString FormatFdsMessage;
        LocalizedString FormatMidiMessage;
        LocalizedString FormatFamiStudioTextMessage;
        LocalizedString FormatFamiTrackerMessage;
        LocalizedString FormatVgmMessage;
        LocalizedString FormatAssemblyMessage;

        // Export results
        LocalizedString SucessMessage;
        LocalizedString FailedMessage;

        // General tooltips
        LocalizedString SingleSongTooltip;
        LocalizedString SongListTooltip;
        LocalizedString MachineTooltip;

        // General labels
        LocalizedString SongLabel;
        LocalizedString SongsLabel;

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

        // Video tooltips
        LocalizedString VideoModeTooltip;
        LocalizedString VideoResTooltip;
        LocalizedString FpsTooltip;
        LocalizedString VideoBitRateTooltip;
        LocalizedString OscWindowTooltip;
        LocalizedString OscColumnsTooltip;
        LocalizedString OscThicknessTooltip;
        LocalizedString PianoRollNoteWidthTooltip;
        LocalizedString PianoRollZoomTooltip;
        LocalizedString PianoRollNumRowsTooltip;
        LocalizedString PianoRollPerspectiveTooltip;
        LocalizedString VideoOverlayRegistersTooltip;
        LocalizedString MobileExportVideoMessage;

        // Video labels
        LocalizedString VideoModeLabel;
        LocalizedString ResolutionLabel;
        LocalizedString FrameRateLabel;
        LocalizedString AudioBitRateLabel;
        LocalizedString VideoBitRateLabel;
        LocalizedString LoopCountLabel;
        LocalizedString AudioDelayMsLabel;
        LocalizedString OscilloscopeWindowLabel;
        LocalizedString RequireFFMpegLabel;
        LocalizedString PianoRollNoteWidthLabel;
        LocalizedString PianoRollZoomLabel;
        LocalizedString PianoRollNumRowsLabel;
        LocalizedString PianoRollPerspectiveLabel;
        LocalizedString OscColumnsLabel;
        LocalizedString OscThicknessLabel;
        LocalizedString OscColorLabel;
        LocalizedString ExportingVideoLabel;
        LocalizedString VideoOverlayRegistersLabel;
        LocalizedString PreviewLabel;

        // Video grid
        LocalizedString ChannelColumn;
        LocalizedString PanColumn;
        LocalizedString TransposeColumn;
        LocalizedString TriggerColumn;
        LocalizedString EmulationOption;
        LocalizedString PeakSpeedOption;

        // NSF tooltips                    
        LocalizedString NsfFormatTooltip;

        // NSF labels
        LocalizedString NameLabel;
        LocalizedString ArtistLabel;
        LocalizedString CopyrightLabel;

        // ROM/FDS tooltips           
        LocalizedString RomFdsFormatTooltip;

        // ROM/FDS labels
        LocalizedString TypeLabel;
        LocalizedString RomMultipleExpansionsLabel;

        // MIDI tooltips
        LocalizedString MidiVelocityTooltip;
        LocalizedString MidiPitchTooltip;
        LocalizedString MidiPitchRangeTooltip;
        LocalizedString MidiInstrumentTooltip;
        LocalizedString MidiInstGridTooltip;

        // MIDI labels
        LocalizedString ExportVolumeAsVelocityLabel;
        LocalizedString ExportSlideAsPitchWheelLabel;
        LocalizedString PitchWheelRangeLabel;
        LocalizedString InstrumentModeLabel;
        LocalizedString InstrumentsLabels;

        // FamiStudio Text tooltips
        LocalizedString DeleteUnusedTooltip;

        // FamiStudio Text labels
        LocalizedString DeleteUnusedDataLabel;

        // Music ASM tooltips
        LocalizedString FT2AssemblyTooltip;
        LocalizedString FT2SepFilesTooltip;
        LocalizedString FT2SepFilesFmtTooltip;
        LocalizedString FT2DmcFmtTooltip;
        LocalizedString FT2DmcExportModeTooltip;
        LocalizedString FT2ExportUnusedMappingsLabel;
        LocalizedString FT2SongListTooltip;
        LocalizedString FT2SfxSongListTooltip;

        // Music ASM labels
        LocalizedString FamiTone2ExpLabel;
        LocalizedString SoundEngineMultExpLabel;
        LocalizedString SeparateFilesLabel;
        LocalizedString SongNamePatternLabel;
        LocalizedString DmcNamePatternLabel;
        LocalizedString DmcExportModeLabel;
        LocalizedString ExportUnusedMappingsLabel;
        LocalizedString GenerateSongListIncludeLabel;

        // Share tooltips
        LocalizedString ShareTooltip;

        // Share labels
        LocalizedString SharingModeLabel;
        LocalizedString CopyToStorageOption;
        LocalizedString ShareOption;

        // FamiTracker Text labels
        LocalizedString FamiTrackerMultipleExpLabel;

        // SFX ASM labels
        LocalizedString GenerateSfxInclude;

        // VGM tooltips
        LocalizedString TrackTitleEnglishTooltip;
        LocalizedString GameNameEnglishTooltip;
        LocalizedString SystemNameEnglishTooltip;
        LocalizedString ComposerEnglishTooltip;
        LocalizedString ReleaseDateTooltip;
        LocalizedString VGMByTooltip;
        LocalizedString SmoothLoopingTooltip;

        // VGM labels
        LocalizedString TrackTitleEnglishLabel;
        // LocalizedString TrackNameOriginalLabel;
        LocalizedString GameNameEnglishLabel;
        // LocalizedString GameNameOriginalLabel;
        LocalizedString SystemEnglishLabel;
        // LocalizedString SystemOriginalLabel;
        LocalizedString ComposerEnglishLabel;
        // LocalizedString ComposerOriginalLabel;
        LocalizedString ReleaseDateLabel;
        LocalizedString VGMByLabel;
        LocalizedString NotesLabel;
        LocalizedString SmoothLoopingLabel;
        LocalizedString VGMUnsupportedExpLabel;
        #endregion

        // Audio Export Strings
        private readonly string[] audioSampleRates = ["11025", "22050", "44100", "48000"];
        private readonly string[] audioBitRates    = ["96", "112", "128", "160", "192", "224", "256"];

        // Video Export Strings
        private readonly string[] videoFrameRates        = ["50/60 FPS", "25/30 FPS"];
        private readonly string[] videoAudioBitRates     = ["64", "96", "112", "128", "160", "192", "224", "256", "320"];
        private readonly string[] videoVideoBitRates     = ["250", "500", "750", "1000", "1500", "2000", "3000", "4000", "5000", "8000", "10000", "20000", "30000"];
        private readonly string[] videoPianoWidths       = ["Auto", "50%", "75%", "100%", "125%", "150%", "175%", "200%"];
        private readonly string[] videoPianoZoomLevels   = ["6.25%", "12.5%", "25%", "50%", "100%", "200%", "400%", "800%"];
        private readonly string[] videoPianoPerspectives = ["0°", "30°", "45°", "60°", "75°"];

        // NSF Export Strings
        private readonly string[] nsfFormats = ["NSF", "NSFe"];

        // ROM / NSF Export Strings
        private readonly string[] romFdsTypes;
        private readonly string[] romFdsModes = ["NTSC", "PAL"];

        // VGM Export Strings
        private readonly string[] vgmSystems = ["NTSC NES/Famicom", "PAL NES"];

        // Settings for grids where toggling song reloads them.
        List<ChannelExportSettings> audioChannelSettings = new();
        List<ChannelExportSettings> videoChannelSettings = new();
        List<MidiInstrumentExportSettings> midiInstSettings = new();

        public unsafe ExportDialog(FamiStudioWindow win)
        {
            Localization.Localize(this);
            
            app = win.FamiStudio;
            project = app.Project;

            // Dropdown lists that need to be localised.
            romFdsTypes = [FormatRomMessage, FormatFdsMessage];

            // Make copies of some current grid settings to work on.
            // NOTE: These need to be initialised before the dialog.
            audioChannelSettings = project.AudioExportConfig.Channels.ToList();
            videoChannelSettings = project.VideoExportConfig.Channels.ToList();
            midiInstSettings = project.MidiExportConfig.MidiInstruments.ToList();

            dialog = new MultiPropertyDialog(win, Title, 640, false, 200);

            for (int i = 0; i < (int)ExportFormat.Max; i++)
            {
                var format = (ExportFormat)i;
                var scroll = i == (int)ExportFormat.Video ? 400 : 0;
                var page = dialog.AddPropertyPage(ExportFormatNames[i], ExportIcons[i], DpiScaling.ScaleForWindow(scroll));
                CreatePropertyPage(page, format);
            }

            // Hide a few formats we don't care about on mobile.
            dialog.SetPageVisible((int)ExportFormat.Text, Platform.IsDesktop);
            dialog.SetPageVisible((int)ExportFormat.FamiTracker, Platform.IsDesktop);
            dialog.SetPageVisible((int)ExportFormat.FamiStudioMusic, Platform.IsDesktop);
            dialog.SetPageVisible((int)ExportFormat.FamiStudioSfx, Platform.IsDesktop);
            dialog.SetPageVisible((int)ExportFormat.FamiTone2Music, Platform.IsDesktop);
            dialog.SetPageVisible((int)ExportFormat.FamiTone2Sfx, Platform.IsDesktop);
            dialog.SetPageVisible((int)ExportFormat.Share, Platform.IsMobile);
            dialog.SetPageCustomAction((int)ExportFormat.Video, PreviewLabel);
            dialog.PageCustomActionActivated += Dialog_PageCustomActionActivated;

            UpdateMidiInstrumentMapping();
        }

        private string[] GetSongNames()
        {
            var names = new string[project.Songs.Count];
            for (var i = 0; i < project.Songs.Count; i++)
                names[i] = project.Songs[i].Name;
            return names;
        }

        private string[] GetChannelNames()
        {
            var channelTypes = project.GetActiveChannelList();
            var channelNames = new string[channelTypes.Length];
            for (int i = 0; i < channelTypes.Length; i++)
            {
                channelNames[i] = ChannelType.GetLocalizedNameWithExpansion(channelTypes[i]);
            }

            return channelNames;
        }

        private bool[] GetDefaultActiveChannels()
        {
            // Find all channels used by the project.
            var anyChannelActive = false;
            var channelActives = new bool[project.GetActiveChannelCount()];

            foreach (var song in project.Songs)
            {
                for (int i = 0; i < song.Channels.Length; i++)
                {
                    var channel = song.Channels[i];
                    if (channel.Patterns.Count > 0)
                    {
                        anyChannelActive = true;
                        channelActives[i] = true;
                    }
                }
            }

            if (!anyChannelActive)
                return null;

            return channelActives;
        }

        private object[,] GetDefaultChannelsGridData(bool tranpose, bool trigger, Song song, out int numActiveChannels)
        {
            // Find all channels used by the project.
            var anyChannelActive = false;
            var channelActives = new bool[project.GetActiveChannelCount()];
            var songs = song != null ? new [] { song } : project.Songs.ToArray();

            numActiveChannels = 0;

            foreach (var s in songs)
            {
                for (int i = 0; i < s.Channels.Length; i++)
                {
                    var channel = s.Channels[i];
                    if (channel.Patterns.Count > 0)
                    {
                        anyChannelActive = true;
                        channelActives[i] = true;
                        numActiveChannels++;
                    }
                }
            }

            var channelTypes = project.GetActiveChannelList();
            var data = new object[channelTypes.Length, trigger ? 5 : 3];

            var channelData = trigger ? videoChannelSettings : audioChannelSettings;
            var songHasData = channelData.Any(c => c.SongId == song.Id); // Default to true if song has saved data.

            for (var i = 0; i < channelTypes.Length; i++)
            {
                var j = 0;
                var type = channelTypes[i];
                var saved = channelData.Find(c => c.SongId == song.Id && c.ChannelType == type);
                var exists = saved != null;

                data[i, j++] = exists ? saved.Enabled : (!anyChannelActive || channelActives[i] || songHasData);
                data[i, j++] = ChannelType.GetLocalizedNameWithExpansion(type);
                data[i, j++] = exists ? saved.Panning : 50;

                if (tranpose) data[i, j++] = 0;
                if (trigger)  data[i, j++] = exists ? saved.Trigger == 0 ? EmulationOption.Value : PeakSpeedOption.Value : channelTypes[i] != ChannelType.Dpcm && channelTypes[i] != ChannelType.Noise && !ChannelType.IsEPSMRythmChannel(channelTypes[i]) ? EmulationOption.Value : PeakSpeedOption.Value;
            }

            return data;
        }

        private void CreatePropertyPage(PropertyPage page, ExportFormat format)
        {
            var songNames = GetSongNames();

            switch (format)
            {
                case ExportFormat.WavMp3:
                    var audioSettings = project.AudioExportConfig;
                    var loopModes     = new string[] { LoopNTimesOption, DurationOption };
                    page.AddDropDownList(SongLabel.Colon, songNames, app.SelectedSong.Name, SingleSongTooltip); // 0
                    page.AddDropDownList(FormatLabel.Colon, AudioFormatType.Names, AudioFormatType.Names.Contains(audioSettings.Format) ? audioSettings.Format : "WAV", WavFormatTooltip); // 1
                    page.AddDropDownList(SampleRateLabel.Colon, audioSampleRates, audioSampleRates.Contains(audioSettings.SampleRate) ? audioSettings.SampleRate : "44100", SampleRateTooltip); // 2
                    page.AddDropDownList(BitRateLabel.Colon, audioBitRates, audioBitRates.Contains(audioSettings.BitRate) ? audioSettings.BitRate : "192", AudioBitRateTooltip); // 3
                    page.AddDropDownList(ModeLabel.Colon, loopModes, loopModes.Contains(audioSettings.LoopMode) ? audioSettings.LoopMode : LoopNTimesOption, LoopModeTooltip); // 4
                    page.AddNumericUpDown(LoopCountLabel.Colon, audioSettings.LoopCount, 1, 10, 1, LoopCountTooltip); // 5
                    page.AddNumericUpDown(DurationSecLabel.Colon, audioSettings.Duration, 1, 1000, 1, DurationTooltip); // 6
                    page.AddNumericUpDown(AudioDelayMsLabel.Colon, audioSettings.Delay, 0, 100, 1, DelayTooltip); // 7
                    page.AddCheckBox(SeparateChannelFilesLabel.Colon, audioSettings.SeparateFiles, SeperateFilesTooltip); // 8
                    page.AddCheckBox(SeparateIntroFileLabel.Colon, audioSettings.SeparateIntro, SeperateIntroTooltip); // 9
                    page.AddCheckBox(StereoLabel.Colon, audioSettings.Stereo || project.OutputsStereoAudio, StereoTooltip); // 10
                    page.AddGrid(ChannelsLabel, new[] { 
                        new ColumnDesc("", 0.0f, ColumnType.CheckBox), 
                        new ColumnDesc(ChannelColumn, 0.4f), 
                        new ColumnDesc(PanColumn, 0.6f, 0, 100, (o) => FormattableString.Invariant($"{(int)o} %")) 
                    }, GetDefaultChannelsGridData(false, false, app.SelectedSong, out _), 7, ChannelGridTooltip); // 11
                    page.AddButton(null, ResetDefaultsLabel.Format(FormatAudioMessage.ToString().ToLowerInvariant())); // 12
                    page.SetPropertyEnabled( 3, audioSettings.Format == "MP3" || audioSettings.Format == "Ogg Vorbis");
                    page.SetPropertyEnabled( 5, audioSettings.LoopMode != DurationOption);
                    page.SetPropertyEnabled( 6, audioSettings.LoopMode == DurationOption);
                    page.SetPropertyVisible( 8, Platform.IsDesktop); // No separate files on mobile.
                    page.SetPropertyVisible( 9, Platform.IsDesktop); // No separate intro on mobile.
                    page.SetPropertyEnabled(10, !project.OutputsStereoAudio); // Force stereo for EPSM.
                    page.SetColumnEnabled(11, 2, audioSettings.Stereo || project.OutputsStereoAudio);
                    page.PropertyChanged += WavMp3_PropertyChanged;
                    page.PropertyClicked += WavMp3_PropertyClicked;
                    break;
                case ExportFormat.Video:
                    if (Platform.CanExportToVideo)
                    {
                        var videoSettings     = project.VideoExportConfig;
                        var videoModes        = Localization.ToStringArray(VideoMode.LocalizedNames);
                        var videoResolutions  = Localization.ToStringArray(VideoResolution.LocalizedNames);
                        var videoOscColors    = Localization.ToStringArray(OscilloscopeColorType.LocalizedNames);
                        var channelsGridData  = GetDefaultChannelsGridData(true, true, app.SelectedSong, out var numActiveChannels);
                        page.AddDropDownList(VideoModeLabel.Colon, videoModes, videoModes.Contains(videoSettings.Mode) ? videoSettings.Mode : videoModes[VideoMode.Oscilloscope], VideoModeTooltip); // 0
                        page.AddDropDownList(SongLabel.Colon, songNames, app.SelectedSong.Name, SingleSongTooltip); // 1
                        page.AddDropDownList(ResolutionLabel.Colon, videoResolutions, videoResolutions.Contains(videoSettings.Resolution) ? videoSettings.Resolution : videoResolutions[0], VideoResTooltip); // 2
                        page.AddDropDownList(FrameRateLabel.Colon, videoFrameRates, videoFrameRates.Contains(videoSettings.FrameRate) ? videoSettings.FrameRate : "50/60 FPS", FpsTooltip); // 3
                        page.AddDropDownList(AudioBitRateLabel.Colon, videoAudioBitRates, videoAudioBitRates.Contains(videoSettings.AudioBitRate) ? videoSettings.AudioBitRate : "192", AudioBitRateTooltip); // 4
                        page.AddDropDownList(VideoBitRateLabel.Colon, videoVideoBitRates, videoVideoBitRates.Contains(videoSettings.VideoBitRate) ? videoSettings.VideoBitRate : "8000", VideoBitRateTooltip); // 5
                        page.AddNumericUpDown(LoopCountLabel.Colon, videoSettings.LoopCount, 1, 8, 1, LoopCountTooltip); // 6
                        page.AddNumericUpDown(AudioDelayMsLabel.Colon, videoSettings.Delay, 0, 100, 1, DelayTooltip); // 7
                        page.AddNumericUpDown(OscColumnsLabel.Colon, videoSettings.OscColumns >= 1 ? videoSettings.OscColumns : int.Max(1, Utils.DivideAndRoundUp(numActiveChannels, 8)), 1, 5, 1, OscColumnsTooltip); // 8
                        page.AddNumericUpDown(OscilloscopeWindowLabel.Colon, videoSettings.OscWindow, 1, 4, 1, OscWindowTooltip); // 9
                        page.AddNumericUpDown(OscThicknessLabel.Colon, videoSettings.OscThickness, 2, 10, 2, OscThicknessTooltip); // 10
                        page.AddDropDownList(OscColorLabel.Colon, videoOscColors, videoOscColors.Contains(videoSettings.OscColour) ? videoSettings.OscColour : videoOscColors[OscilloscopeColorType.Instruments]); // 11
                        page.AddDropDownList(PianoRollNoteWidthLabel.Colon, videoPianoWidths, videoPianoWidths.Contains(videoSettings.PianoRollWidth) ? videoSettings.PianoRollWidth : "Auto", PianoRollNoteWidthTooltip); // 12
                        page.AddDropDownList(PianoRollZoomLabel.Colon, videoPianoZoomLevels, videoPianoZoomLevels.Contains(videoSettings.PianoRollZoom) ? videoSettings.PianoRollZoom : project.UsesFamiTrackerTempo ? "100%" : "25%", PianoRollZoomTooltip); // 13
                        page.AddNumericUpDown(PianoRollNumRowsLabel.Colon, videoSettings.PianoRollRows >= 1 ? videoSettings.PianoRollRows : int.Max(1, Utils.DivideAndRoundUp(numActiveChannels, 8)), 1, 16, 1, PianoRollNumRowsTooltip); // 14
                        page.AddDropDownList(PianoRollPerspectiveLabel.Colon, videoPianoPerspectives, videoPianoPerspectives.Contains(videoSettings.PianoRollPerspective) ? videoSettings.PianoRollPerspective : "60°", PianoRollPerspectiveTooltip); // 15
                        page.AddCheckBox(VideoOverlayRegistersLabel.Colon, videoSettings.OverlayRegisters, VideoOverlayRegistersTooltip); // 16
                        page.AddCheckBox(StereoLabel.Colon, videoSettings.Stereo || project.OutputsStereoAudio, StereoTooltip); // 17
                        page.AddGrid(ChannelsLabel, new[] {
                            new ColumnDesc("", 0.0f, ColumnType.CheckBox),
                            new ColumnDesc(ChannelColumn, 0.3f),
                            new ColumnDesc(PanColumn, 0.2f, 0, 100, (o) => FormattableString.Invariant($"{(int)o} %")),
                            new ColumnDesc(TransposeColumn, 0.2f, -8, 8),
                            new ColumnDesc(TriggerColumn, 0.2f, new string[] { EmulationOption, PeakSpeedOption })
                        }, channelsGridData, project.GetActiveChannelCount() + 1, ChannelGridTooltipVid); // 18
                        page.AddButton(null, PreviewLabel); // 19
                        page.AddButton(null, ResetDefaultsLabel.Format(FormatVideoMessage.ToString().ToLowerInvariant())); // 20
                        page.SetPropertyEnabled(12, videoSettings.Mode == videoModes[VideoMode.PianoRollSeparateChannels] || videoSettings.Mode == videoModes[VideoMode.PianoRollUnified]);
                        page.SetPropertyEnabled(13, videoSettings.Mode == videoModes[VideoMode.PianoRollSeparateChannels] || videoSettings.Mode == videoModes[VideoMode.PianoRollUnified]);
                        page.SetPropertyEnabled(14, videoSettings.Mode == videoModes[VideoMode.PianoRollSeparateChannels]);
                        page.SetPropertyEnabled(15, videoSettings.Mode == videoModes[VideoMode.PianoRollSeparateChannels] || videoSettings.Mode == videoModes[VideoMode.PianoRollUnified]);
                        page.SetPropertyEnabled(17, !project.OutputsStereoAudio); // Force stereo for EPSM.
                        page.SetPropertyVisible(19, Platform.IsDesktop);
                        page.SetColumnEnabled(18, 2, videoSettings.Stereo || project.OutputsStereoAudio);
                        page.SetColumnEnabled(18, 3, false);
                        page.PropertyChanged += VideoPage_PropertyChanged;
                        page.PropertyClicked += VideoPage_PropertyClicked;
                    }
                    else
                    {
                        page.AddLabel(null, RequireFFMpegLabel, true);
                        canExportToVideo = false;
                    }
                    break;
                case ExportFormat.Nsf:
                    var nsfSettings = project.NsfExportConfig;
                    var nsfModes    = Localization.ToStringArray(MachineType.LocalizedNames);
                    var nsfMap      = project.NsfExportConfig.SongList.ToDictionary(s => s.SongId, s => s.Enabled);
                    bool[] nsfBools = project.Songs.Select(s => !nsfMap.ContainsKey(s.Id) || nsfMap[s.Id]).ToArray();
                    page.AddTextBox(NameLabel.Colon, !string.IsNullOrEmpty(nsfSettings.Name) ? nsfSettings.Name : project.Name, 31); // 0
                    page.AddTextBox(ArtistLabel.Colon, !string.IsNullOrEmpty(nsfSettings.Artist) ? nsfSettings.Artist : project.Author, 31); // 1
                    page.AddTextBox(CopyrightLabel.Colon, !string.IsNullOrEmpty(nsfSettings.Copyright) ? nsfSettings.Copyright : project.Copyright, 31); // 2
                    page.AddDropDownList(FormatLabel.Colon, nsfFormats, nsfFormats.Contains(nsfSettings.Format) ? nsfSettings.Format : "NSF", NsfFormatTooltip); // 3
                    page.AddDropDownList(ModeLabel.Colon, nsfModes, nsfModes.Contains(nsfSettings.Mode) ? nsfSettings.Mode : nsfModes[project.PalMode ? MachineType.PAL : MachineType.NTSC], MachineTooltip); // 4
                    page.AddCheckBoxList(Platform.IsDesktop ? null : SongsLabel, songNames, nsfBools, SongListTooltip, 12); // 5
                    page.AddButton(null, ResetDefaultsLabel.Format(ExportFormatNames[2])); // 6
#if DEBUG
                    page.AddDropDownList("Engine :", FamiToneKernel.Names, FamiToneKernel.Names[FamiToneKernel.FamiStudio]); // 7
#endif
                    page.PropertyClicked += NsfPage_PropertyClicked;
                    break;
                case ExportFormat.Rom:
                    if (!project.UsesMultipleExpansionAudios)
                    {
                        var romSettings = project.RomFdsExportConfig;
                        var romName     = !string.IsNullOrEmpty(romSettings.Name)   ? romSettings.Name   : project.Name;
                        var romAuthor   = !string.IsNullOrEmpty(romSettings.Artist) ? romSettings.Artist : project.Author;
                        var romMap      = romSettings.SongList.ToDictionary(s => s.SongId, s => s.Enabled);
                        bool[] romBools = project.Songs.Select(s => romMap.ContainsKey(s.Id) ? romMap[s.Id] : true).ToArray();
                        page.AddDropDownList(TypeLabel.Colon, romFdsTypes, !project.UsesFdsExpansion ? FormatRomMessage : romFdsTypes.Contains(romSettings.Type) ? romSettings.Type : FormatFdsMessage, RomFdsFormatTooltip); // 0
                        page.AddTextBox(NameLabel.Colon, romName.Substring(0, Math.Min(28, romName.Length)), 28); // 1
                        page.AddTextBox(ArtistLabel.Colon, romAuthor.Substring(0, Math.Min(28, romAuthor.Length)), 28); // 2
                        page.AddDropDownList(ModeLabel.Colon, romFdsModes, romFdsModes.Contains(romSettings.Mode) ? romSettings.Mode : project.PalMode ? "PAL" : "NTSC", MachineTooltip); // 3
                        page.AddCheckBoxList(Platform.IsDesktop ? null : SongsLabel, songNames, romBools, SongListTooltip, 12); // 4
                        page.AddButton(null, ResetDefaultsLabel.Format(ExportFormatNames[3])); // 5
                        page.SetPropertyEnabled(0, project.UsesFdsExpansion);
                        page.PropertyClicked += RomFdsPage_PropertyClicked;
                    }
                    else
                    {
                        page.AddLabel(null, RomMultipleExpansionsLabel, true);
                        canExportToRom = false;
                    }
                    break;
                case ExportFormat.Midi:
                    var midiSettings = project.MidiExportConfig;
                    var midiModes    = Localization.ToStringArray(MidiExportInstrumentMode.LocalizedNames);
                    page.AddDropDownList(SongLabel.Colon, songNames, app.SelectedSong.Name, SingleSongTooltip); // 0
                    page.AddCheckBox(ExportVolumeAsVelocityLabel.Colon, midiSettings.VolumeVelocity, MidiVelocityTooltip); // 1
                    page.AddCheckBox(ExportSlideAsPitchWheelLabel.Colon, midiSettings.SlidesAsPitch, MidiPitchTooltip); // 2
                    page.AddNumericUpDown(PitchWheelRangeLabel.Colon, midiSettings.PitchWheelRange, 1, 24, 1, MidiPitchRangeTooltip); // 3
                    page.AddDropDownList(InstrumentModeLabel.Colon, midiModes, midiModes.Contains(midiSettings.Mode) ? midiSettings.Mode : midiModes[0], MidiInstrumentTooltip); // 4
                    page.AddGrid(InstrumentsLabels, new[] { new ColumnDesc("", 0.4f), new ColumnDesc("", 0.6f, MidiFileReader.MidiInstrumentNames) }, GetMidiInstrumentData(MidiExportInstrumentMode.Instrument, out _), 14, MidiInstGridTooltip, GridOptions.MobileTwoColumnLayout); // 5
                    page.AddButton(null, ResetDefaultsLabel.Format(ExportFormatNames[4])); // 6
                    page.PropertyChanged += Midi_PropertyChanged;
                    page.PropertyClicked += Midi_PropertyClicked;
                    break;
                case ExportFormat.Text:
                    var textSettings = project.FamiStudioTextExportConfig;
                    var textMap = textSettings.SongList.ToDictionary(s => s.SongId, s => s.Enabled);
                    bool[] textBools = project.Songs.Select(s => textMap.ContainsKey(s.Id) ? textMap[s.Id] : true).ToArray();

                    page.AddCheckBoxList(null, songNames, textBools, SongListTooltip, 12); // 0
                    page.AddCheckBox(DeleteUnusedDataLabel.Colon, textSettings.DeleteUnusedData, DeleteUnusedTooltip); // 1
                    page.AddButton(null, ResetDefaultsLabel.Format(ExportFormatNames[6])); // 6
                    page.PropertyClicked += TextPage_PropertyClicked;
                    break;
                case ExportFormat.FamiTracker:
                    if (!project.UsesMultipleExpansionAudios)
                    {
                        var famitrackerSettings = project.FamiTrackerTextExportConfig;
                        var famitrackerMap      = famitrackerSettings.SongList.ToDictionary(s => s.SongId, s => s.Enabled);
                        bool[] famiTrackerBools = project.Songs.Select(s => famitrackerMap.ContainsKey(s.Id) ? famitrackerMap[s.Id] : true).ToArray();

                        page.AddCheckBoxList(null, songNames, famiTrackerBools, SongListTooltip, 12); // 0
                        page.AddButton(null, ResetDefaultsLabel.Format(ExportFormatNames[7])); // 1
                        canExportToFamiTracker = true;
                        page.PropertyClicked += FamiTrackerPage_PropertyClicked;
                    }
                    else
                    {
                        page.AddLabel(null, FamiTrackerMultipleExpLabel, true);
                        canExportToFamiTracker = false;
                    }
                    break;
                case ExportFormat.FamiTone2Music:
                case ExportFormat.FamiStudioMusic:
                    if (format == ExportFormat.FamiTone2Music && project.UsesAnyExpansionAudio)
                    {
                        page.AddLabel(null, FamiTone2ExpLabel, true);
                        canExportToFamiTone2 = false;
                    }
                    else if (format == ExportFormat.FamiStudioMusic && project.UsesMultipleExpansionAudios)
                    {
                        page.AddLabel(null, SoundEngineMultExpLabel, true);
                        canExportToSoundEngine = false;
                    }
                    else
                    {
                        var musicCodeSettings = format == ExportFormat.FamiStudioMusic ? project.FamiStudioMusicExportConfig : project.FamiTone2MusicExportConfig;
                        var musicCodeFormats = AssemblyFormat.Names;
                        var musicCodeDmcModes = Localization.ToStringArray(DpcmExportMode.LocalizedNames);
                        var musicCodeMap = musicCodeSettings.SongList.ToDictionary(s => s.SongId, s => s.Enabled);
                        bool[] musicCodeBools = project.Songs.Select(s => !musicCodeMap.ContainsKey(s.Id) || musicCodeMap[s.Id]).ToArray();
                        page.AddDropDownList(FormatLabel.Colon, musicCodeFormats, musicCodeFormats.Contains(musicCodeSettings.Format) ? musicCodeSettings.Format : "CA65", FT2AssemblyTooltip); // 0
                        page.AddCheckBox(SeparateFilesLabel.Colon, musicCodeSettings.Separate, FT2SepFilesTooltip); // 1
                        page.AddTextBox(SongNamePatternLabel.Colon, musicCodeSettings.SongName, 0, false, FT2SepFilesFmtTooltip); // 2
                        page.AddTextBox(DmcNamePatternLabel.Colon, musicCodeSettings.DmcName, 0, false, FT2DmcFmtTooltip); // 3
                        page.AddDropDownList(DmcExportModeLabel.Colon, musicCodeDmcModes, musicCodeDmcModes.Contains(musicCodeSettings.DmcExportMode) ? musicCodeSettings.DmcExportMode : musicCodeDmcModes[DpcmExportMode.Minimum], FT2DmcExportModeTooltip); // 4
                        page.AddCheckBox(ExportUnusedMappingsLabel.Colon, musicCodeSettings.UnusedMappings, FT2ExportUnusedMappingsLabel); // 5
                        page.AddCheckBox(GenerateSongListIncludeLabel.Colon, musicCodeSettings.SongListInclude, FT2SongListTooltip); // 6
                        page.AddCheckBoxList(null, songNames, musicCodeBools, SongListTooltip, 12); // 7
                        page.AddButton(null, ResetDefaultsLabel.Format(ExportFormatNames[format == ExportFormat.FamiStudioMusic ? 8 : 10])); // 8
                        page.SetPropertyEnabled(2, musicCodeSettings.Separate);
                        page.SetPropertyEnabled(3, musicCodeSettings.Separate);
                        page.SetPropertyEnabled(5, musicCodeSettings.DmcExportMode != musicCodeDmcModes[DpcmExportMode.Minimum]);
                        page.PropertyChanged += SoundEngine_PropertyChanged;
                        page.PropertyClicked += SoundEngine_PropertyClicked;
                    }
                    break;
                case ExportFormat.FamiTone2Sfx:
                case ExportFormat.FamiStudioSfx:
                    var sfxCodeSettings = format == ExportFormat.FamiStudioSfx ? project.FamiStudioSfxExportConfig : project.FamiTone2SfxExportConfig;
                    var sfxCodeFormats = AssemblyFormat.Names;
                    var sfxCodeTypes = Localization.ToStringArray(MachineType.LocalizedNames);
                    var sfxCodeMap = sfxCodeSettings.SongList.ToDictionary(s => s.SongId, s => s.Enabled);
                    bool[] sfxCodeBools = project.Songs.Select(s => !sfxCodeMap.ContainsKey(s.Id) || sfxCodeMap[s.Id]).ToArray();
                    page.AddDropDownList(FormatLabel.Colon, sfxCodeFormats, sfxCodeFormats.Contains(sfxCodeSettings.Format) ? sfxCodeSettings.Format : "CA65", FT2AssemblyTooltip); // 0
                    page.AddDropDownList(ModeLabel.Colon, sfxCodeTypes, sfxCodeTypes.Contains(sfxCodeSettings.Mode) ? sfxCodeSettings.Mode : project.PalMode ? "PAL" : "NTSC", MachineTooltip); // 1
                    page.AddCheckBox(GenerateSfxInclude.Colon, sfxCodeSettings.Include, FT2SfxSongListTooltip); // 2
                    page.AddCheckBoxList(null, songNames, sfxCodeBools, SongListTooltip, 12); // 3
                    page.AddButton(null, ResetDefaultsLabel.Format(ExportFormatNames[format == ExportFormat.FamiStudioSfx ? 9 : 11])); // 4
                    page.PropertyClicked += SfxEngine_PropertyClicked;
                    break;
                case ExportFormat.VGM:
                    var vgmSettings = project.VgmExportConfig;
                    int VGMWarnID;
                    const int unsupportedExpansionMask = ExpansionType.Vrc6Mask | ExpansionType.Mmc5Mask | ExpansionType.N163Mask;
                    if (Platform.IsMobile)
                        VGMWarnID = page.AddLabel(null, VGMUnsupportedExpLabel.Format(ExpansionType.GetStringForMask(project.ExpansionAudioMask & unsupportedExpansionMask)), true); // 0
                    int VGMSongSelect = page.AddDropDownList(SongLabel.Colon, songNames, app.SelectedSong.Name, SongListTooltip); // 0/1
                    page.AddTextBox(TrackTitleEnglishLabel.Colon, !string.IsNullOrEmpty(vgmSettings.TrackTitle) ? vgmSettings.TrackTitle : page.GetPropertyValue<string>(VGMSongSelect), 0, false, TrackTitleEnglishTooltip); // 1/2
                    page.AddTextBox(GameNameEnglishLabel.Colon, !string.IsNullOrEmpty(vgmSettings.GameName) ? vgmSettings.GameName : project.Name, 0, false, GameNameEnglishTooltip); // 2/3
                    page.AddTextBox(SystemEnglishLabel.Colon, !string.IsNullOrEmpty(vgmSettings.System) ? vgmSettings.System :
                    vgmSystems[project.PalMode ? 1 : 0] +
                    (project.UsesVrc7Expansion ? $" + {ExpansionType.GetLocalizedName(ExpansionType.Vrc7)}" : "") +
                    (project.UsesFdsExpansion ? $" + {ExpansionType.GetLocalizedName(ExpansionType.Fds)}" : "") +
                    (project.UsesS5BExpansion ? $" + {ExpansionType.GetLocalizedName(ExpansionType.S5B)}" : "") +
                    (project.UsesEPSMExpansion ? $" + {ExpansionType.GetLocalizedName(ExpansionType.EPSM)}" : ""),
                    0, false, SystemNameEnglishTooltip); // 3/4
                    page.AddTextBox(ComposerEnglishLabel.Colon, !string.IsNullOrEmpty(vgmSettings.Composer) ? vgmSettings.Composer : project.Author, 0, false, ComposerEnglishTooltip); // 4/5
                    page.AddTextBox(ReleaseDateLabel.Colon, !string.IsNullOrEmpty(vgmSettings.Date) ? vgmSettings.Date : DateTime.Now.ToString("yyyy\\/MM\\/dd"), 0, false, ReleaseDateTooltip); // 5/6
                    page.AddTextBox(VGMByLabel.Colon, !string.IsNullOrEmpty(vgmSettings.VgmBy) ? vgmSettings.VgmBy : "FamiStudio Export", 0, false, VGMByTooltip); // 6/7
                    page.AddTextBox(NotesLabel.Colon, !string.IsNullOrEmpty(vgmSettings.Notes) ? vgmSettings.Notes : project.Copyright, 0); // 7/8
                    page.AddCheckBox(SmoothLoopingLabel.Colon, vgmSettings.SmoothLoop, SmoothLoopingTooltip); // 8/9
                    page.AddButton(null, ResetDefaultsLabel.Format(ExportFormatNames[5])); // 9/10
                    if (Platform.IsDesktop)
                        VGMWarnID = page.AddLabel(null, VGMUnsupportedExpLabel.Format(ExpansionType.GetStringForMask(project.ExpansionAudioMask & unsupportedExpansionMask)), true); // 10
                    page.SetPropertyVisible(VGMWarnID, (project.ExpansionAudioMask & unsupportedExpansionMask) != 0);  // Unsupported expansions
                    page.SetPropertyEnabled(VGMSongSelect+8, project.GetSong(page.GetPropertyValue<string>(VGMSongSelect)).LoopPoint >= 0);
                    page.PropertyChanged += VGM_PropertyChanged;
                    page.PropertyClicked += VGM_PropertyClicked;
                    break;
                case ExportFormat.Share:
                    if (Platform.IsAndroid)
                    {
                        page.AddRadioButtonList(SharingModeLabel.Colon, new string[] { CopyToStorageOption, ShareOption }, 0, ShareTooltip);
                    }
                    else
                    {
                        page.AddLabel(null, ShareTooltip, true);
                    }
                    break;
            }

            page.Build();
        }

        private void LaunchVideoPreview()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.Video);
            var halfFrameRate = props.GetSelectedIndex(3) == 1;
            var resolutionIdx = props.GetSelectedIndex(2);
            var previewResX = VideoResolution.ResolutionX[resolutionIdx];
            var previewResY = VideoResolution.ResolutionY[resolutionIdx];
            var previewDialog = new VideoPreviewDialog(dialog.ParentWindow, previewResX, previewResY, (project.PalMode ? NesApu.FpsPAL : NesApu.FpsNTSC) * (halfFrameRate ? 0.5f : 1.0f));

            Exporting?.Invoke(); // Needed to stop the song.

            previewDialog.ShowDialogAsync();
            Log.SetLogOutput(previewDialog);

            if (Platform.IsMobile)
            {
                new Thread(() =>
                {
                    LaunchVideoEncoding(null, true, previewDialog);
                    Log.ClearLogOutput();
                }).Start();
            }
            else
            {
                LaunchVideoEncoding(null, true, previewDialog);
                Log.ClearLogOutput();
            }
        }

        private void Dialog_PageCustomActionActivated(int page)
        {
            if (page == (int)ExportFormat.Video)
            {
                LaunchVideoPreview();
            }
        }

        private void VideoPage_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 19)
            {
                LaunchVideoPreview();
            }
            else if (propIdx == 20)
            {
                Platform.MessageBoxAsync(dialog.ParentWindow, ResetMessage.Format(FormatVideoMessage.ToString().ToLowerInvariant()), ResetTitle, MessageBoxButtons.YesNo, (r) =>
                {
                    if (r == DialogResult.Yes)
                    {
                        project.ResetVideoExportSettings();
                        videoChannelSettings.Clear();

                        var settings = project.VideoExportConfig;
                        var channelCount = project.GetActiveChannelCount();

                        props.SetDropDownListIndex(0, VideoMode.Oscilloscope);
                        props.SetDropDownListIndex(1, project.Songs.IndexOf(app.SelectedSong));
                        props.SetDropDownListIndex(2, 0); // 1080p landscape.
                        props.SetDropDownListIndex(3, Array.IndexOf(videoFrameRates, "50/60 FPS"));
                        props.SetDropDownListIndex(4, Array.IndexOf(videoAudioBitRates, "192"));
                        props.SetDropDownListIndex(5, Array.IndexOf(videoVideoBitRates, "8000"));
                        props.SetPropertyValue(6, settings.LoopCount);
                        props.SetPropertyValue(7, settings.Delay);
                        props.SetPropertyValue(8, int.Max(1, Utils.DivideAndRoundUp(channelCount, 8)));
                        props.SetPropertyValue(9, settings.OscWindow);
                        props.SetPropertyValue(10, settings.OscThickness);
                        props.SetDropDownListIndex(11, OscilloscopeColorType.Instruments);
                        props.SetDropDownListIndex(12, Array.IndexOf(videoPianoWidths, "Auto"));
                        props.SetPropertyValue(13, Array.IndexOf(videoPianoZoomLevels, project.UsesFamiTrackerTempo ? "100%" : "25%"));
                        props.SetPropertyValue(14, int.Max(1, Utils.DivideAndRoundUp(channelCount, 8)));
                        props.SetDropDownListIndex(15, Array.IndexOf(videoPianoPerspectives, "60°"));
                        props.SetPropertyValue(16, settings.OverlayRegisters);
                        props.SetPropertyValue(17, settings.Stereo || project.OutputsStereoAudio);
                        props.UpdateGrid(18, GetDefaultChannelsGridData(true, true, app.SelectedSong, out _));
                    }
                });
            }
        }

        private void VideoPage_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 0) // Video mode
            {
                var newMode = props.GetSelectedIndex(propIdx);
                props.SetPropertyEnabled(8,  newMode == VideoMode.Oscilloscope);
                props.SetPropertyEnabled(12, newMode != VideoMode.Oscilloscope);
                props.SetPropertyEnabled(13, newMode != VideoMode.Oscilloscope);
                props.SetPropertyEnabled(14, newMode == VideoMode.PianoRollSeparateChannels);
                props.SetPropertyEnabled(15, newMode != VideoMode.Oscilloscope);
                props.SetColumnEnabled(18, 3, newMode == VideoMode.PianoRollUnified);
            }
            else if (propIdx == 1) // Song
            {
                props.UpdateGrid(18, GetDefaultChannelsGridData(true, true, project.Songs[props.GetSelectedIndex(1)], out _));
            }
            else if (propIdx == 17) // Stereo
            {
                props.SetColumnEnabled(18, 2, (bool)value);
            }
            else if (propIdx == 18) // Grid
            {
                var type      = project.GetActiveChannelList()[rowIdx];
                var isEnabled = props.GetPropertyValue<bool>(18, rowIdx, 0);
                var panning   = props.GetPropertyValue<int>(18, rowIdx, 2);
                var transpose = props.GetPropertyValue<int>(18, rowIdx, 3);
                var trigger   = props.GetPropertyValue<string>(18, rowIdx, 4) == PeakSpeedOption ? 1 : 0;
                var song      = project.Songs[props.GetSelectedIndex(1)];

                var found  = videoChannelSettings.Find(s => s.SongId == song.Id && s.ChannelType == type);         
                if (found != null)
                {
                    found.Enabled   = isEnabled;
                    found.Panning   = panning;
                    found.Transpose = transpose;
                    found.Trigger   = trigger;
                }
                else
                {
                    videoChannelSettings.Add(new ChannelExportSettings()
                    {
                        SongId      = song.Id,
                        ChannelType = type,
                        Enabled     = isEnabled,
                        Panning     = panning,
                        Transpose   = transpose,
                        Trigger     = trigger
                    });
                }
            }
        }

        private void WavMp3_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 0)
            {
                props.UpdateGrid(11, GetDefaultChannelsGridData(false, false, project.Songs[props.GetSelectedIndex(0)], out _));
            }
            else if (propIdx == 1)
            {
                props.SetPropertyEnabled(3, (string)value != AudioFormatType.Names[0]);
            }
            else if (propIdx == 4)
            {
                props.SetPropertyEnabled(5, (string)value != DurationOption);
                props.SetPropertyEnabled(6, (string)value == DurationOption);
            }
            else if (propIdx == 8)
            {
                var separateChannels = (bool)value;

                props.SetPropertyEnabled(7, !separateChannels);
                props.SetPropertyEnabled(10, !separateChannels && !project.OutputsStereoAudio);

                if (separateChannels)
                {
                    props.SetPropertyValue(7, 0);
                    props.SetPropertyValue(10, project.OutputsStereoAudio);
                }

                props.SetColumnEnabled(11, 2, props.GetPropertyValue<bool>(10) && !separateChannels);
            }
            else if (propIdx == 10)
            {
                props.SetColumnEnabled(11, 2, (bool)value);
            }
            else if (propIdx == 11) // Grid
            {
                var type    = project.GetActiveChannelList()[rowIdx];
                var enabled = props.GetPropertyValue<bool>(11, rowIdx, 0);
                var panning = props.GetPropertyValue<int>(11, rowIdx, 2);
                var song    = project.Songs[props.GetSelectedIndex(0)];

                var found  = audioChannelSettings.Find(s => s.SongId == song.Id && s.ChannelType == type);         
                if (found != null)
                {
                    found.Enabled = enabled;
                    found.Panning = panning;
                }
                else
                {
                    audioChannelSettings.Add(new ChannelExportSettings()
                    {
                        SongId      = song.Id,
                        ChannelType = type,
                        Enabled     = enabled,
                        Panning     = panning
                    });
                }
            }
        }

        private void WavMp3_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 10 && click == ClickType.Right && colIdx == 2)
            {
                props.UpdateGrid(propIdx, rowIdx, colIdx, 50);
            }
            else if (propIdx == 12 && click == ClickType.Button)
            {
                Platform.MessageBoxAsync(dialog.ParentWindow, ResetMessage.Format(FormatAudioMessage.ToString().ToLowerInvariant()), ResetTitle, MessageBoxButtons.YesNo, (r) =>
                {
                    if (r == DialogResult.Yes)
                    {
                        project.ResetAudioExportSettings();
                        audioChannelSettings.Clear();

                        var settings = project.AudioExportConfig;

                        props.SetDropDownListIndex(0, project.Songs.IndexOf(app.SelectedSong));
                        props.SetDropDownListIndex(1, Array.IndexOf(AudioFormatType.Names, "WAV"));
                        props.SetDropDownListIndex(2, Array.IndexOf(audioSampleRates, "44100"));
                        props.SetDropDownListIndex(3, Array.IndexOf(audioBitRates, "192"));
                        props.SetPropertyValue(4, LoopNTimesOption);
                        props.SetPropertyValue(5, settings.LoopCount);
                        props.SetPropertyValue(6, settings.Duration);
                        props.SetPropertyValue(7, settings.Delay);
                        props.SetPropertyValue(8, settings.SeparateFiles);
                        props.SetPropertyValue(9, settings.SeparateIntro);
                        props.SetPropertyValue(10, settings.Stereo || project.OutputsStereoAudio);
                        props.UpdateGrid(11, GetDefaultChannelsGridData(false, false, app.SelectedSong, out _));
                    }
                });
            }
        }

        private void NsfPage_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 6 && click == ClickType.Button)
            {
                Platform.MessageBoxAsync(dialog.ParentWindow, ResetMessage.Format(ExportFormatNames[2]), ResetTitle, MessageBoxButtons.YesNo, (r) =>
                {
                    if (r == DialogResult.Yes)
                    {
                        project.ResetNsfExportSettings();

                        var settings = project.NsfExportConfig;
                        bool[] trues = new bool[project.Songs.Count];
                        Array.Fill(trues, true);

                        props.SetPropertyValue(0, project.Name);
                        props.SetPropertyValue(1, project.Author);
                        props.SetPropertyValue(2, project.Copyright);
                        props.SetPropertyValue(3, Array.IndexOf(nsfFormats, "NSF"));
                        props.SetDropDownListIndex(4, project.PalMode ? MachineType.PAL : MachineType.NTSC);
                        props.UpdateCheckBoxList(5, GetSongNames(), trues);
                    }
                });
            }
        }

        private void RomFdsPage_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 5 && click == ClickType.Button)
            {
                Platform.MessageBoxAsync(dialog.ParentWindow, ResetMessage.Format(ExportFormatNames[3]), ResetTitle, MessageBoxButtons.YesNo, (r) =>
                {
                    if (r == DialogResult.Yes)
                    {
                        project.ResetRomFdsExportSettings();

                        var settings = project.RomFdsExportConfig;
                        bool[] trues = new bool[project.Songs.Count];
                        Array.Fill(trues, true);

                        props.SetDropDownListIndex(0, Array.IndexOf(romFdsTypes, project.UsesFdsExpansion ? FormatFdsMessage : FormatRomMessage));
                        props.SetPropertyValue(1, project.Name);
                        props.SetPropertyValue(2, project.Author);
                        props.SetDropDownListIndex(3, project.PalMode ? MachineType.PAL : MachineType.NTSC);
                        props.UpdateCheckBoxList(4, GetSongNames(), trues);
                    }
                });
            }
        }

        private int[] GetSongIds(bool[] selectedSongs)
        {
            var songIds = new List<int>();

            for (int i = 0; i < selectedSongs.Length; i++)
            {
                if (selectedSongs[i])
                    songIds.Add(project.Songs[i].Id);
            }

            return songIds.ToArray();
        }

        private void ShowExportResultToast(string format, bool success = true)
        {
            var msg = success ? SucessMessage : FailedMessage;
            Platform.ShowToast(app.Window, msg.Format(format));
        }

        private void RemoveUnusedChannelSettings(List<ChannelExportSettings> settings)
        {
            if (settings != null && settings.Count > 0)
            {
                var validSongIds = project.Songs.Select(s => s.Id).ToHashSet();
                settings.RemoveAll(c => !validSongIds.Contains(c.SongId));
            }
        }

        private void ExportWavMp3()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.WavMp3);
            var format = props.GetSelectedIndex(1);
            
            Action<string> ExportWavMp3Action = (filename) =>
            {
                if (filename != null)
                {
                    var songName = props.GetPropertyValue<string>(0);
                    var sampleRate = Convert.ToInt32(props.GetPropertyValue<string>(2), CultureInfo.InvariantCulture);
                    var bitrate = Convert.ToInt32(props.GetPropertyValue<string>(3), CultureInfo.InvariantCulture);
                    var loopMode = props.GetPropertyValue<string>(4);
                    var loopCount = loopMode != DurationOption ? props.GetPropertyValue<int>(5) : -1;
                    var duration = loopMode == DurationOption ? props.GetPropertyValue<int>(6) : -1;
                    var delay = props.GetPropertyValue<int>(7);
                    var separateFiles = props.GetPropertyValue<bool>(8);
                    var separateIntro = props.GetPropertyValue<bool>(9);
                    var stereo = props.GetPropertyValue<bool>(10) && (!separateFiles || project.OutputsStereoAudio);
                    var song = project.GetSong(songName);

                    var channelCount = project.GetActiveChannelCount();
                    var channelTypes = project.GetActiveChannelList();
                    var channelMask = 0L;
                    var pan = new float[channelCount]; 

                    var exportSettings = project.AudioExportConfig;

                    exportSettings.SongId        = song.Id;
                    exportSettings.Format        = props.GetPropertyValue<string>(1);
                    exportSettings.SampleRate    = props.GetPropertyValue<string>(2);
                    exportSettings.BitRate       = props.GetPropertyValue<string>(3);
                    exportSettings.LoopMode      = loopMode;
                    exportSettings.LoopCount     = props.GetPropertyValue<int>(5);
                    exportSettings.Duration      = props.GetPropertyValue<int>(6);
                    exportSettings.SeparateFiles = separateFiles;
                    exportSettings.SeparateIntro = separateIntro;
                    exportSettings.Stereo        = stereo;

                    for (int i = 0; i < channelCount; i++)
                    {
                        var enabled = props.GetPropertyValue<bool>(11, i, 0);
                        var panning = props.GetPropertyValue<int>(11, i, 2);

                        if (enabled) channelMask |= (1L << i);
                        pan[i] = panning / 100.0f;

                        var type  = channelTypes[i]; 
                        var saved = audioChannelSettings.Find(c => c.SongId == song.Id && c.ChannelType == type);
                        if (saved == null)
                        {
                            audioChannelSettings.Add(new ChannelExportSettings 
                            { 
                                SongId = song.Id, 
                                ChannelType = type, 
                                Enabled = enabled, 
                                Panning = panning 
                            });
                        } 
                    }

                    RemoveUnusedChannelSettings(audioChannelSettings);
                    exportSettings.Channels = audioChannelSettings;

                    AudioExportUtils.Save(song, filename, sampleRate, loopCount, duration, channelMask, separateFiles, separateIntro, stereo, pan, delay, Platform.IsMobile || project.UsesEPSMExpansion, false,
                         (samples, samplesChannels, fn) =>
                         {
                             switch (format)
                             {
                                 case AudioFormatType.Mp3:
                                     Mp3File.Save(samples, fn, sampleRate, bitrate, samplesChannels);
                                     break;
                                 case AudioFormatType.Wav:
                                     WaveFile.Save(samples, fn, sampleRate, samplesChannels);
                                     break;
                                 case AudioFormatType.Vorbis:
                                     VorbisFile.Save(samples, fn, sampleRate, bitrate, samplesChannels);
                                     break;
                             }
                         });

                    lastExportFilename = filename;
                }
            };


            if (Platform.IsMobile)
            {
                var songName = props.GetPropertyValue<string>(0);
                Platform.StartMobileSaveFileOperationAsync($"{songName}.{AudioFormatType.Extensions[format]}", (f) =>
                {
                    new Thread(() =>
                    {
                        app.BeginLogTask(true, "Exporting Audio", "Exporting long songs with lots of channels may take a while.");

                        ExportWavMp3Action(f);
                        
                        Platform.FinishMobileSaveFileOperationAsync(true, () =>
                        {
                            var aborted = Log.ShouldAbortOperation;
                            app.EndLogTask();
                            ShowExportResultToast(FormatAudioMessage, !aborted);
                        });
                    }).Start();
                });
            }
            else
            {
                var filename = (string)null;

                if (lastExportFilename != null)
                {
                    filename = lastExportFilename;
                }
                else
                {
                    filename = Platform.ShowSaveFileDialog(
                        $"Export {AudioFormatType.Names[format]} File",
                        $"{AudioFormatType.Names[format]} Audio File (*.{AudioFormatType.Extensions[format]})|*.{AudioFormatType.Extensions[format]}",
                        ref Settings.LastExportFolder);
                }

                if (filename != null)
                {
                    ExportWavMp3Action(filename);
                    ShowExportResultToast(FormatAudioMessage);
                }
            }
        }

        private bool LaunchVideoEncoding(string filename, bool preview, IVideoEncoder forcedEncoder = null)
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.Video);

            var videoMode = props.GetSelectedIndex(0);
            var resolutionIdx = props.GetSelectedIndex(2);
            var channelCount = project.GetActiveChannelCount();
            var channelTypes = project.GetActiveChannelList();

            var settings = new VideoExportSettings();
            settings.Filename = filename;
            settings.Project = project;
            settings.VideoMode = videoMode;
            settings.SongId = project.GetSong(props.GetPropertyValue<string>(1)).Id;
            settings.ResX = VideoResolution.ResolutionX[resolutionIdx];
            settings.ResY = VideoResolution.ResolutionY[resolutionIdx];
            settings.Downsample = Platform.IsMobile && preview ? 2 : 1;
            settings.HalfFrameRate = props.GetSelectedIndex(3) == 1;
            settings.AudioBitRate = Convert.ToInt32(props.GetPropertyValue<string>(4), CultureInfo.InvariantCulture);
            settings.VideoBitRate = Convert.ToInt32(props.GetPropertyValue<string>(5), CultureInfo.InvariantCulture);
            settings.LoopCount = props.GetPropertyValue<int>(6);
            settings.AudioDelay = props.GetPropertyValue<int>(7);
            settings.OscNumColumns = props.GetPropertyValue<int>(8);
            settings.OscWindow = props.GetPropertyValue<int>(9);
            settings.OscLineThickness = props.GetPropertyValue<int>(10);
            settings.OscColorMode = props.GetSelectedIndex(11);
            settings.PianoRollNoteWidth = Utils.ParseIntWithTrailingGarbage(props.GetPropertyValue<string>(12)) / 100.0f;
            settings.PianoRollZoom = (float)Math.Pow(2.0, props.GetSelectedIndex(13) - 3);
            settings.PianoRollNumRows = props.GetPropertyValue<int>(14);
            settings.PianoRollPerspective = Utils.ParseIntWithTrailingGarbage(props.GetPropertyValue<string>(15));
            settings.ShowRegisters = props.GetPropertyValue<bool>(16);
            settings.Stereo = preview ? project.OutputsStereoAudio : props.GetPropertyValue<bool>(17);
            settings.ChannelPan = new float[channelCount];
            settings.ChannelTranspose = new int[channelCount];
            settings.EmuTriggers = new bool[channelCount];
            settings.ChannelMask = 0;
            settings.PreviewMode = preview;
            settings.Encoder = forcedEncoder != null ? forcedEncoder : Platform.CreateVideoEncoder();

            for (int i = 0; i < channelCount; i++)
            {
                var enabled   = props.GetPropertyValue<bool>(18, i, 0);
                var panning   = props.GetPropertyValue<int>(18, i, 2);
                var transpose = props.GetPropertyValue<int>(18, i, 3) * 12;
                var trigger   = props.GetPropertyValue<string>(18, i, 4) == EmulationOption;

                if (enabled) settings.ChannelMask |= (1L << i);
                settings.ChannelPan[i] = preview ? 0.5f : panning / 100.0f;
                settings.ChannelTranspose[i] = transpose;
                settings.EmuTriggers[i] = trigger;

                if (!preview)
                {
                    var type  = channelTypes[i]; 
                    var saved = audioChannelSettings.Find(c => c.SongId == settings.SongId && c.ChannelType == type);
                    if (saved == null)
                    {
                        videoChannelSettings.Add(new ChannelExportSettings 
                        { 
                            SongId = settings.SongId,
                            ChannelType = type, 
                            Enabled = enabled,
                            Panning = panning,
                            Transpose = transpose,
                            Trigger = trigger ? 0 : 1
                        });
                    } 
                }
            }

            if (!preview)
            {
                var exportSettings = project.VideoExportConfig;

                exportSettings.Mode = props.GetPropertyValue<string>(0);
                exportSettings.SongId = settings.SongId;
                exportSettings.Resolution = props.GetPropertyValue<string>(2);
                exportSettings.FrameRate = props.GetPropertyValue<string>(3);
                exportSettings.AudioBitRate = props.GetPropertyValue<string>(4);
                exportSettings.VideoBitRate = props.GetPropertyValue<string>(5);
                exportSettings.LoopCount = settings.LoopCount;
                exportSettings.Delay = settings.AudioDelay;
                exportSettings.OscColumns = settings.OscNumColumns;
                exportSettings.OscWindow = settings.OscWindow;
                exportSettings.OscThickness = settings.OscLineThickness;
                exportSettings.OscColour = props.GetPropertyValue<string>(11);
                exportSettings.PianoRollWidth = props.GetPropertyValue<string>(12);
                exportSettings.PianoRollZoom = props.GetPropertyValue<string>(13);
                exportSettings.PianoRollRows = settings.PianoRollNumRows;
                exportSettings.PianoRollPerspective = props.GetPropertyValue<string>(15);
                exportSettings.OverlayRegisters = settings.ShowRegisters;
                exportSettings.Stereo = props.GetPropertyValue<bool>(17);

                RemoveUnusedChannelSettings(videoChannelSettings);
                exportSettings.Channels = videoChannelSettings;
            }

            if (videoMode == VideoMode.Oscilloscope)
            {
                return new VideoFileOscilloscope().Save(settings);
            }
            else
            {
                return new VideoFilePianoRoll().Save(settings);
            }
        }

        private void ExportVideo()
        {
            if (!canExportToVideo)
                return;

            var props = dialog.GetPropertyPage((int)ExportFormat.Video);

            Func<string, bool> ExportVideoAction = (filename) =>
            {
                if (filename != null)
                {
                    lastExportFilename = filename;
                    return LaunchVideoEncoding(filename, false);

                }
                else
                {
                    return false;
                }
            };

            if (Platform.IsMobile)
            {
                var songName = props.GetPropertyValue<string>(1);
                Platform.StartMobileSaveFileOperationAsync($"{songName}.mp4", (f) =>
                {
                    new Thread(() =>
                    {
                        app.BeginLogTask(true, ExportingVideoLabel, MobileExportVideoMessage);
                        
                        var success = ExportVideoAction(f);

                        Platform.FinishMobileSaveFileOperationAsync(success, () =>
                        {
                            app.EndLogTask();
                            ShowExportResultToast(FormatVideoMessage, success);
                        });

                    }).Start();
                });
            }
            else
            {
                var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog("Export Video File", "MP4 Video File (*.mp4)|*.mp4", ref Settings.LastExportFolder);
                ExportVideoAction(filename);
                ShowExportResultToast(FormatVideoMessage);
            }
        }

        private void ExportNsf()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.Nsf);
            var nsfe = props.GetSelectedIndex(3) > 0;
            var extension = nsfe ? "nsfe" : "nsf";

            Action<string> ExportNsfAction = (filename) =>
            {
                if (filename != null)
                {
                    var mode = MachineType.GetValueForName(props.GetPropertyValue<string>(4));
#if DEBUG
                    var kernel = FamiToneKernel.GetValueForName(props.GetPropertyValue<string>(7));
#else
                    var kernel = FamiToneKernel.FamiStudio;
#endif
                    var name      = props.GetPropertyValue<string>(0);
                    var artist    = props.GetPropertyValue<string>(1);
                    var copyright = props.GetPropertyValue<string>(2);
                    var bools     = props.GetPropertyValue<bool[]>(5);

                    var settings = project.NsfExportConfig;
                    settings.Name = name;
                    settings.Artist = artist;
                    settings.Copyright = copyright;
                    settings.Format = props.GetPropertyValue<string>(3);
                    settings.Mode = props.GetPropertyValue<string>(4);

                    RemoveUnusedChannelSettings(audioChannelSettings);
                    settings.SongList = project.Songs.Select((song, index) => new SongListExportSettings { SongId = song.Id, Enabled = bools[index] }).ToList();

                    new NsfFile().Save(project, kernel, filename,
                        GetSongIds(props.GetPropertyValue<bool[]>(5)),
                        name,
                        artist,
                        copyright,
                        mode,
                        nsfe);

                    lastExportFilename = filename;
                }
            };

            if (Platform.IsMobile)
            {
                Platform.StartMobileSaveFileOperationAsync($"{project.Name}.{extension}", (f) =>
                {
                    ExportNsfAction(f);
                    Platform.FinishMobileSaveFileOperationAsync(true, () => { ShowExportResultToast(FormatNsfMessage); });
                });
            }
            else
            {
                var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog("Export NSF File", $"Nintendo Sound Files (*.{extension})|*.{extension}", ref Settings.LastExportFolder);
                if (filename != null)
                {
                    ExportNsfAction(filename);
                    ShowExportResultToast(FormatNsfMessage);
                }
            }
        }

        private void ExportRom()
        {
            if (!canExportToRom)
                return;

            var props = dialog.GetPropertyPage((int)ExportFormat.Rom);
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(4));

            if (songIds.Length > RomFile.RomMaxSongs)
            {
                Platform.MessageBoxAsync(dialog.ParentWindow, $"Please select {RomFile.RomMaxSongs} songs or less.", "ROM Export", MessageBoxButtons.OK);
                return;
            }

            var name   = props.GetPropertyValue<string>(1);
            var artist = props.GetPropertyValue<string>(2);
            var bools  = props.GetPropertyValue<bool[]>(4);

            var settings = project.RomFdsExportConfig;
            settings.Type = props.GetPropertyValue<string>(0);
            settings.Name = name;
            settings.Artist = artist;
            settings.Mode = props.GetPropertyValue<string>(3);
            settings.SongList = project.Songs.Select((song, index) => new SongListExportSettings { SongId = song.Id, Enabled = bools[index] }).ToList();

            if (props.GetPropertyValue<string>(0) == FormatRomMessage)
            {
                Action<string> ExportRomAction = (filename) =>
                {
                    if (filename != null)
                    {
                        var rom = new RomFile();
                        rom.Save(
                            project, filename, songIds,
                            name,
                            artist,
                            props.GetPropertyValue<string>(3) == "PAL");

                        lastExportFilename = filename;
                    }
                };

                if (Platform.IsMobile)
                {
                    Platform.StartMobileSaveFileOperationAsync($"{project.Name}.nes", (f) =>
                    {
                        ExportRomAction(f);
                        Platform.FinishMobileSaveFileOperationAsync(true, () => { ShowExportResultToast(FormatRomMessage); });
                    });
                }
                else
                {
                    var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog("Export ROM File", "NES ROM (*.nes)|*.nes", ref Settings.LastExportFolder);
                    if (filename != null)
                    {
                        ExportRomAction(filename);
                        ShowExportResultToast(FormatRomMessage);
                    }
                }
            }
            else
            {
                Action<string> ExportFdsAction = (filename) =>
                {
                    if (filename != null)
                    {
                        var fds = new FdsFile();
                        fds.Save(
                            project, filename, songIds,
                            props.GetPropertyValue<string>(1),
                            props.GetPropertyValue<string>(2),
                            props.GetPropertyValue<string>(3) == "PAL");

                        lastExportFilename = filename;
                    }
                };

                if (Platform.IsMobile)
                {
                    Platform.StartMobileSaveFileOperationAsync($"{project.Name}.fds", (f) =>
                    {
                        ExportFdsAction(f);
                        Platform.FinishMobileSaveFileOperationAsync(true, () => { ShowExportResultToast(FormatFdsMessage); });
                    });
                }
                else
                {
                    var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog("Export Famicom Disk", "FDS Disk (*.fds)|*.fds", ref Settings.LastExportFolder);
                    if (filename != null)
                    {
                        ExportFdsAction(filename);
                        ShowExportResultToast(FormatFdsMessage);
                    }
                }
            }
        }

        private void ExportShare()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.Share);
            var share = Platform.IsAndroid && props.GetSelectedIndex(0) == 1;
            var filename = !string.IsNullOrEmpty(app.Project.Filename) ? Path.GetFileName(app.Project.Filename) : (project.Name != null && project.Name.Trim().Length > 0 ? $"{project.Name}.fms" : "Export.fms");

            if (share)
            {
                filename = Platform.GetShareFilename(filename);
                app.SaveProjectCopy(filename);
                Platform.StartShareFileAsync(filename, () => 
                {
                    ShowExportResultToast("Sharing Successful!");
                });
            }
            else
            {
                Platform.StartMobileSaveFileOperationAsync(filename, (f) =>
                {
                    app.SaveProjectCopy(f);
                    Platform.FinishMobileSaveFileOperationAsync(true, () => { ShowExportResultToast("Sharing Successful!"); });
                });
            }
        }

        private void SoundEngine_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 1)
            {
                props.SetPropertyEnabled(2, (bool)value);
                props.SetPropertyEnabled(3, (bool)value);
            }
            else if (propIdx == 4)
            {
                var dmcExportMode = props.GetSelectedIndex(4);
                props.SetPropertyEnabled(5, dmcExportMode != DpcmExportMode.Minimum);
                if (dmcExportMode == DpcmExportMode.Minimum)
                {
                    props.SetPropertyValue(5, false);
                }
            }
        }

        private void SoundEngine_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 8)
            {
                var famistudio = (ExportFormat)dialog.SelectedIndex == ExportFormat.FamiStudioMusic;
                Platform.MessageBoxAsync(dialog.ParentWindow, ResetMessage.Format(ExportFormatNames[famistudio ? 8 : 10]), ResetTitle, MessageBoxButtons.YesNo, (r) =>
                {
                    if (r == DialogResult.Yes)
                    {
                        if (famistudio)
                            project.ResetFamiStudioMusicCodeExportSettings();
                        else
                            project.ResetFamiTone2MusicCodeExportSettings();

                        var settings = famistudio ? project.FamiStudioMusicExportConfig : project.FamiTone2MusicExportConfig;
                        var types = Localization.ToStringArray(DpcmExportMode.LocalizedNames);

                        props.SetDropDownListIndex(0, Array.IndexOf(AssemblyFormat.Names, "CA65"));
                        props.SetPropertyValue(1, settings.Separate);
                        props.SetPropertyValue(2, settings.SongName);
                        props.SetPropertyValue(3, settings.DmcName);
                        props.SetDropDownListIndex(4, DpcmExportMode.Minimum);
                        props.SetPropertyValue(5, settings.UnusedMappings);
                        props.SetPropertyValue(6, settings.SongListInclude);

                        bool[] trues = new bool[project.Songs.Count];
                        Array.Fill(trues, true);

                        props.UpdateCheckBoxList(7, GetSongNames(), trues);
                    }
                });
            }
        }

        private void SfxEngine_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 4)
            {
                var famistudio = (ExportFormat)dialog.SelectedIndex == ExportFormat.FamiStudioSfx;
                Platform.MessageBoxAsync(dialog.ParentWindow, ResetMessage.Format(ExportFormatNames[famistudio ? 9 : 11]), ResetTitle, MessageBoxButtons.YesNo, (r) =>
                {
                    if (r == DialogResult.Yes)
                    {
                        if (famistudio)
                            project.ResetFamiStudioSfxCodeExportSettings();
                        else
                            project.ResetFamiTone2SfxCodeExportSettings();

                        var settings = famistudio ? project.FamiStudioSfxExportConfig : project.FamiTone2SfxExportConfig;

                        props.SetDropDownListIndex(0, Array.IndexOf(AssemblyFormat.Names, "CA65"));
                        props.SetDropDownListIndex(1, project.PalMode ? MachineType.PAL : MachineType.NTSC);
                        props.SetPropertyValue(2, settings.Include);

                        bool[] trues = new bool[project.Songs.Count];
                        Array.Fill(trues, true);

                        props.UpdateCheckBoxList(3, GetSongNames(), trues);
                    }
                });
            }
        }

        private void Midi_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 0 || propIdx == 4)
            {
                UpdateMidiInstrumentMapping();
            }
            else if (propIdx == 5)
            {     
                // Add the current grid settings so they are remembered if the song or mode is toggled.
                // They will only be saved later if a song is exported.
                var song   = project.Songs[props.GetSelectedIndex(0)];
                var mode   = props.GetSelectedIndex(4);
                var index  = Array.IndexOf(MidiFileReader.MidiInstrumentNames, props.GetPropertyValue<string>(5, rowIdx, 1));
                var typeId = mode == MidiExportInstrumentMode.Instrument ? project.Instruments[rowIdx].Id : song.Channels[rowIdx].Type;
                var found  = midiInstSettings.Find(s => s.SongId == song.Id && s.Mode == mode && s.TypeId == typeId);
                if (found != null)
                {
                    found.Index = index;
                }
                else
                {
                    midiInstSettings.Add(new MidiInstrumentExportSettings()
                    {
                        SongId = song.Id,
                        Mode   = mode,
                        TypeId = typeId,
                        Index  = index
                    });
                }
            }
        }

        private object[,] GetMidiInstrumentData(int mode, out string[] colNames)
        {
            var data  = (object[,])null;
            var props = dialog.GetPropertyPage((int)ExportFormat.Midi);
            var song  = project.Songs[props.GetSelectedIndex(0)];

            colNames = new[] { "", "MIDI Instrument" };

            if (mode == MidiExportInstrumentMode.Instrument)
            {
                data = new object[project.Instruments.Count, 2];
                for (int i = 0; i < project.Instruments.Count; i++)
                {
                    var inst   = project.Instruments[i];
                    var saved  = midiInstSettings.FirstOrDefault(m => m.SongId == song.Id && m.Mode == MidiExportInstrumentMode.Instrument && m.TypeId == inst.Id);
                    data[i, 0] = inst.NameWithExpansion;
                    data[i, 1] = saved != null ? MidiFileReader.MidiInstrumentNames[saved.Index] : MidiFileReader.MidiInstrumentNames[0];
                }

                colNames[0] = "FamiStudio Instrument";
            }
            else
            {
                data = new object[song.Channels.Length, 2];
                for (int i = 0; i < song.Channels.Length; i++)
                {
                    var saved  = midiInstSettings.FirstOrDefault(m => m.SongId == song.Id && m.Mode == MidiExportInstrumentMode.Channel && m.TypeId == song.Channels[i].Type);
                    data[i, 0] = song.Channels[i].Name;
                    data[i, 1] = saved != null ? MidiFileReader.MidiInstrumentNames[saved.Index] : MidiFileReader.MidiInstrumentNames[0];
                }

                colNames[0] = "NES Channel";
            }

            return data;
        }

        private void Midi_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 6 && click == ClickType.Button)
            {
                Platform.MessageBoxAsync(dialog.ParentWindow, ResetMessage.Format(ExportFormatNames[4]), ResetTitle, MessageBoxButtons.YesNo, (r) =>
                {
                    if (r == DialogResult.Yes)
                    {
                        project.ResetMidiExportSettings();
                        midiInstSettings.Clear();

                        var settings = project.MidiExportConfig;

                        props.SetDropDownListIndex(0, project.Songs.IndexOf(app.SelectedSong));
                        props.SetPropertyValue(1, settings.VolumeVelocity);
                        props.SetPropertyValue(2, settings.SlidesAsPitch);
                        props.SetPropertyValue(3, settings.PitchWheelRange);
                        props.SetDropDownListIndex(4, 0); // Instrument.
                        props.UpdateGrid(5, GetMidiInstrumentData(MidiExportInstrumentMode.Instrument, out _));
                    }
                });
            }
        }

        private void UpdateMidiInstrumentMapping()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.Midi);
            var mode  = props.GetSelectedIndex(4);
            var data  = GetMidiInstrumentData(mode, out var cols);

            props.UpdateGrid(5, data, cols);
        }

        private void ExportMidi()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.Midi);

            Action<string> ExportMidiAction = (filename) =>
            {
                if (filename != null)
                {
                    var songName = props.GetPropertyValue<string>(0);
                    var velocity = props.GetPropertyValue<bool>(1);
                    var slideNotes = props.GetPropertyValue<bool>(2);
                    var pitchRange = props.GetPropertyValue<int>(3);
                    var instrumentMode = props.GetSelectedIndex(4);
                    var song = project.GetSong(songName);
                    var instrumentMapping = new int[instrumentMode == MidiExportInstrumentMode.Channel ? song.Channels.Length : song.Project.Instruments.Count];

                    var settings = project.MidiExportConfig;

                    settings.SongId = project.Songs[props.GetSelectedIndex(0)].Id;
                    settings.VolumeVelocity = velocity;
                    settings.SlidesAsPitch = slideNotes;
                    settings.PitchWheelRange = pitchRange;
                    settings.Mode = props.GetPropertyValue<string>(4);

                    // Check for and remove any deleted songs, instruments, or channels before saving.
                    var validSongIds = project.Songs.Select(s => s.Id).ToHashSet();
                    var validInstIds = project.Instruments.Select(i => i.Id).ToHashSet();
                    var validChTypes = project.GetActiveChannelList().ToHashSet();

                    midiInstSettings.RemoveAll(s => 
                        !validSongIds.Contains(s.SongId) || 
                        (s.Mode == 0 && !validInstIds.Contains(s.TypeId)) || // Mode 0 = Instrument
                        (s.Mode == 1 && !validChTypes.Contains(s.TypeId))    // Mode 1 = Channel
                    );

                    settings.MidiInstruments = midiInstSettings;

                    for (int i = 0; i < instrumentMapping.Length; i++)
                        instrumentMapping[i] = Array.IndexOf(MidiFileReader.MidiInstrumentNames, props.GetPropertyValue<string>(5, i, 1));

                    new MidiFileWriter().Save(project, filename, song.Id, instrumentMode, instrumentMapping, velocity, slideNotes, pitchRange);

                    ShowExportResultToast(FormatMidiMessage);

                    lastExportFilename = filename;
                }
            };

            if (Platform.IsMobile)
            {
                var songName = props.GetPropertyValue<string>(0);
                Platform.StartMobileSaveFileOperationAsync($"{songName}.mid", (f) =>
                {
                    ExportMidiAction(f);
                    Platform.FinishMobileSaveFileOperationAsync(true, () => ShowExportResultToast(FormatMidiMessage, true));
                });
            }
            else
            {
                var filename = lastExportFilename != null ? lastExportFilename  : Platform.ShowSaveFileDialog("Export MIDI File", "MIDI Files (*.mid)|*.mid", ref Settings.LastExportFolder);
                ExportMidiAction(filename);
                ShowExportResultToast(FormatMidiMessage);
            }
        }

        private void TextPage_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 2)
            {
                Platform.MessageBoxAsync(dialog.ParentWindow, ResetMessage.Format(ExportFormatNames[6]), ResetTitle, MessageBoxButtons.YesNo, (r) =>
                {
                    if (r == DialogResult.Yes)
                    {
                        project.ResetFamiStudioTextExportSettings();

                        bool[] trues = new bool[project.Songs.Count];
                        Array.Fill(trues, true);

                        props.UpdateCheckBoxList(0, GetSongNames(), trues);
                        props.SetPropertyValue(1, project.FamiStudioTextExportConfig.DeleteUnusedData);
                    }
                });
            }
        }

        private void ExportText()
        {
            var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog("Export FamiStudio Text File", "FamiStudio Text Export (*.txt)|*.txt", ref Settings.LastExportFolder);
            if (filename != null)
            {
                var props = dialog.GetPropertyPage((int)ExportFormat.Text);
                var deleteUnusedData = props.GetPropertyValue<bool>(1);

                var bools    = props.GetPropertyValue<bool[]>(0);
                var settings = project.FamiStudioTextExportConfig;

                settings.SongList = project.Songs.Select((song, index) => new SongListExportSettings { SongId = song.Id, Enabled = bools[index] }).ToList();
                settings.DeleteUnusedData = deleteUnusedData;

                new FamistudioTextFile().Save(project, filename, GetSongIds(bools), deleteUnusedData);
                ShowExportResultToast(FormatFamiStudioTextMessage);
                lastExportFilename = filename;
            }
        }

        private void VGM_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            int songSelect = Platform.IsDesktop ? 0 : 1;
            if (propIdx == songSelect)
            {
                props.SetPropertyValue(songSelect+1, (string)value);
                props.SetPropertyEnabled(songSelect+8, project.GetSong((string)value).LoopPoint >= 0);
            }
        }

        private void VGM_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            // The props are in a different order on mobile, due to a warning message
            // being first. Offsetting by -1 to make it match desktop. Unless prop 0
            // is ever made a button, this shouldn't be a problem. - Steo
            if (Platform.IsMobile)
                propIdx--;

            if (propIdx == 9 && click == ClickType.Button)
            {
                Platform.MessageBoxAsync(dialog.ParentWindow, ResetMessage.Format(ExportFormatNames[5]), ResetTitle, MessageBoxButtons.YesNo, (r) =>
                {
                    if (r == DialogResult.Yes)
                    {
                        project.ResetVgmExportSettings();

                        var settings = project.VgmExportConfig;
                        var defName  = vgmSystems[project.PalMode ? 1 : 0] +
                            (project.UsesVrc7Expansion  ? $" + {ExpansionType.GetLocalizedName(ExpansionType.Vrc7)}" : "") +
                            (project.UsesFdsExpansion   ? $" + {ExpansionType.GetLocalizedName(ExpansionType.Fds)}"  : "") +
                            (project.UsesS5BExpansion   ? $" + {ExpansionType.GetLocalizedName(ExpansionType.S5B)}"  : "") +
                            (project.UsesEPSMExpansion  ? $" + {ExpansionType.GetLocalizedName(ExpansionType.EPSM)}" : "");

                        props.SetDropDownListIndex(0, project.Songs.IndexOf(app.SelectedSong));
                        props.SetPropertyValue(1, app.SelectedSong.Name);
                        props.SetPropertyValue(2, project.Name);
                        props.SetPropertyValue(3, defName);
                        props.SetPropertyValue(4, project.Author);
                        props.SetPropertyValue(5, DateTime.Now.ToString("yyyy\\/MM\\/dd"));
                        props.SetPropertyValue(6, "FamiStudio Export");
                        props.SetPropertyValue(7, project.Copyright);
                        props.SetPropertyValue(8, settings.SmoothLoop);
                    }
                });
                
            }
        }

        private void ExportVGM()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.VGM);

            // This is the best I can do here with this structure. - Steo
            var settings   = project.VgmExportConfig;
            var offset     = Platform.IsMobile ? 1 : 0;
            var song       = props.GetPropertyValue<string>(0 + offset);
            var trackTitle = props.GetPropertyValue<string>(1 + offset);
            var gameName   = props.GetPropertyValue<string>(2 + offset);
            var system     = props.GetPropertyValue<string>(3 + offset);
            var composer   = props.GetPropertyValue<string>(4 + offset);
            var date       = props.GetPropertyValue<string>(5 + offset);
            var vgmBy      = props.GetPropertyValue<string>(6 + offset);
            var notes      = props.GetPropertyValue<string>(7 + offset);
            var smoothLoop = props.GetPropertyValue<bool>(8 + offset);

            settings.SongId = project.Songs[props.GetSelectedIndex(0 + offset)].Id;
            settings.TrackTitle = trackTitle;
            settings.GameName = gameName;
            settings.System = system;
            settings.Composer = composer;
            settings.Date = date;
            settings.VgmBy = vgmBy;
            settings.Notes = notes;
            settings.SmoothLoop = smoothLoop;

            if (Platform.IsMobile)
            {
                Platform.StartMobileSaveFileOperationAsync($"{trackTitle}.vgm", (f) =>
                {
                    VgmFile.Save(project.GetSong(song), (f), trackTitle,
                        gameName,
                        system,
                        composer,
                        date,
                        vgmBy,
                        notes,
                        smoothLoop);
                    Platform.FinishMobileSaveFileOperationAsync(true, () => { ShowExportResultToast(FormatVgmMessage); });
                });
            }
            else
            {
                var filename = lastExportFilename ?? Platform.ShowSaveFileDialog($"Export {FormatVgmMessage}", $"{FormatVgmMessage} File (*.vgm)|*.vgm", ref Settings.LastExportFolder);
                if (filename != null)
                {
                    VgmFile.Save(project.GetSong(song), filename, trackTitle,
                        gameName,
                        system,
                        composer,
                        date,
                        vgmBy,
                        notes,
                        smoothLoop);
                    lastExportFilename = filename;
                    ShowExportResultToast(FormatVgmMessage);
                }
            }  
        }

        private void FamiTrackerPage_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 1)
            {
                Platform.MessageBoxAsync(dialog.ParentWindow, ResetMessage.Format(ExportFormatNames[7]), ResetTitle, MessageBoxButtons.YesNo, (r) =>
                {
                    if (r == DialogResult.Yes)
                    {
                        project.ResetFamiTrackerTextExportSettings();

                        bool[] trues = new bool[project.Songs.Count];
                        Array.Fill(trues, true);

                        props.UpdateCheckBoxList(0, GetSongNames(), trues);
                    }
                });
            }
        }
		
        private void ExportFamiTracker()
        {
            if (!canExportToFamiTracker)
                return;

            var props = dialog.GetPropertyPage((int)ExportFormat.FamiTracker);

            var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog("Export FamiTracker Text File", "FamiTracker Text Format (*.txt)|*.txt", ref Settings.LastExportFolder);
            if (filename != null)
            {
                var bools    = props.GetPropertyValue<bool[]>(0);
                var settings = project.FamiTrackerTextExportConfig;

                settings.SongList = project.Songs.Select((song, index) => new SongListExportSettings { SongId = song.Id, Enabled = bools[index] }).ToList();

                new FamitrackerTextFile().Save(project, filename, GetSongIds(bools));
                ShowExportResultToast(FormatFamiTrackerMessage);
                lastExportFilename = filename;
            }
        }

        private void ExportFamiTone2Music(bool famiStudio)
        {
            if ((famiStudio && !canExportToSoundEngine) || (!famiStudio && !canExportToFamiTone2))
                return;

            var props = dialog.GetPropertyPage(famiStudio ? (int)ExportFormat.FamiStudioMusic : (int)ExportFormat.FamiTone2Music);

            var separate = props.GetPropertyValue<bool>(1);
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(7));
            var kernel = famiStudio ? FamiToneKernel.FamiStudio : FamiToneKernel.FamiTone2;
            var exportFormat = AssemblyFormat.GetValueForName(props.GetPropertyValue<string>(0));
            var ext = (exportFormat == AssemblyFormat.CA65 || exportFormat == AssemblyFormat.SDAS) ? "s" : "asm";
            var songNamePattern = props.GetPropertyValue<string>(2);
            var dpcmNamePattern = props.GetPropertyValue<string>(3);
            var dpcmExportMode = props.GetSelectedIndex(4);
            var dpcmExportUnusedMappings = props.GetPropertyValue<bool>(5);
            var generateInclude = props.GetPropertyValue<bool>(6);
            var bools = props.GetPropertyValue<bool[]>(7);

            var settings = famiStudio ? project.FamiStudioMusicExportConfig : project.FamiTone2MusicExportConfig;

            settings.Separate        = separate;
            settings.Format          = props.GetPropertyValue<string>(0);
            settings.SongName        = songNamePattern;
            settings.DmcName         = dpcmNamePattern;
            settings.DmcExportMode   = props.GetPropertyValue<string>(4);
            settings.UnusedMappings  = dpcmExportUnusedMappings;
            settings.SongListInclude = generateInclude;

            settings.SongList = project.Songs.Select((song, index) => new SongListExportSettings { SongId = song.Id, Enabled = bools[index] }).ToList();

            if (separate)
            {
                var folder = lastExportFilename != null ? lastExportFilename : Platform.ShowBrowseFolderDialog("Select the export folder", ref Settings.LastExportFolder);

                if (folder != null)
                {
                    if (!project.EnsureSongAssemblyNamesAreUnique())
                    {
                        ShowExportResultToast(FormatAssemblyMessage, false);
                        return;
                    }

                    var success = true;

                    foreach (var songId in songIds)
                    {
                        var song = project.GetSong(songId);
                        var formattedSongName = songNamePattern.Replace("{project}", project.Name).Replace("{song}", song.Name);
                        var formattedDpcmName = dpcmNamePattern.Replace("{project}", project.Name).Replace("{song}", song.Name);
                        var songFilename = Path.Combine(folder, Utils.MakeNiceAsmName(formattedSongName) + "." + ext);
                        var dpcmFilename = Path.Combine(folder, Utils.MakeNiceAsmName(formattedDpcmName) + ".dmc");
                        var includeFilename = generateInclude ? Path.ChangeExtension(songFilename, null) + "_songlist.inc" : null;

                        Log.LogMessage(LogSeverity.Info, $"Exporting song '{song.Name}' as a separate assembly file.");

                        FamitoneMusicFile f = new FamitoneMusicFile(kernel, true);
                        success = success && f.Save(project, new int[] { songId }, exportFormat, -1, true, songFilename, dpcmFilename, dpcmExportMode, dpcmExportUnusedMappings, includeFilename, MachineType.Dual);
                    }

                    lastExportFilename = folder;
                    ShowExportResultToast(FormatAssemblyMessage, success);
                }
            }
            else
            {
                var engineName = famiStudio ? "FamiStudio" : "FamiTone2";
                var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog($"Export {engineName} Assembly Code", $"{engineName} Assembly File (*.{ext})|*.{ext}", ref Settings.LastExportFolder);
                if (filename != null)
                {
                    var includeFilename = generateInclude ? Path.ChangeExtension(filename, null) + "_songlist.inc" : null;

                    // LOCTODO : Still some strings to localize 
                    Log.LogMessage(LogSeverity.Info, $"Exporting all songs to a single assembly file.");

                    FamitoneMusicFile f = new FamitoneMusicFile(kernel, true);
                    var success = f.Save(project, songIds, exportFormat, -1, false, filename, Path.ChangeExtension(filename, ".dmc"), dpcmExportMode, dpcmExportUnusedMappings, includeFilename, MachineType.Dual);

                    lastExportFilename = filename;
                    ShowExportResultToast(FormatAssemblyMessage, success);
                }
            }
        }

        private void ExportFamiTone2Sfx(bool famiStudio)
        {
            var props = dialog.GetPropertyPage(famiStudio ? (int)ExportFormat.FamiStudioSfx : (int)ExportFormat.FamiTone2Sfx);
            var exportFormat = AssemblyFormat.GetValueForName(props.GetPropertyValue<string>(0));
            var ext = (exportFormat == AssemblyFormat.CA65 || exportFormat == AssemblyFormat.SDAS) ? "s" : "asm";
            var mode = MachineType.GetValueForName(props.GetPropertyValue<string>(1));
            var engineName = famiStudio ? "FamiStudio" : "FamiTone2";
            var generateInclude = props.GetPropertyValue<bool>(2);
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(3));
            var bools = props.GetPropertyValue<bool[]>(3);
            var settings = famiStudio ? project.FamiStudioSfxExportConfig : project.FamiTone2SfxExportConfig;

            settings.Format   = props.GetPropertyValue<string>(0);
            settings.Mode     = props.GetPropertyValue<string>(1);
            settings.Include  = generateInclude;
            settings.SongList = project.Songs.Select((song, index) => new SongListExportSettings { SongId = song.Id, Enabled = bools[index] }).ToList();

            var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog($"Export {engineName} Code", $"{engineName} Assembly File (*.{ext})|*.{ext}", ref Settings.LastExportFolder);
            if (filename != null)
            {
                var includeFilename = generateInclude ? Path.ChangeExtension(filename, null) + "_sfxlist.inc" : null;

                FamitoneSoundEffectFile f = new FamitoneSoundEffectFile();
                var result = f.Save(project, songIds, exportFormat, mode, famiStudio ? FamiToneKernel.FamiStudio : FamiToneKernel.FamiTone2, filename, includeFilename);
                ShowExportResultToast(FormatAssemblyMessage, result);
                lastExportFilename = filename;
            }
        }

        private uint ComputeProjectCrc(Project project)
        {
            // Only hashing fields that would have an impact on the generated UI.
            uint crc = CRC32.Compute(project.ExpansionAudioMask);
            crc = CRC32.Compute(project.ExpansionNumN163Channels, crc);

            foreach (var song in project.Songs)
            {
                crc = CRC32.Compute(song.Id,   crc);
                crc = CRC32.Compute(song.Name, crc);
            }

            foreach (var inst in project.Instruments)
            {
                crc = CRC32.Compute(inst.Id,   crc);
                crc = CRC32.Compute(inst.Name, crc);
            }

            return crc;
        }

        public bool HasAnyPreviousExport => lastProjectCrc != 0 && !string.IsNullOrEmpty(lastExportFilename);

        public bool IsProjectStillCompatible(Project project)
        {
            if (project != this.project)
                return false;

            return lastProjectCrc == ComputeProjectCrc(project);
        }
        
        public void Export(bool repeatLast)
        {
            if (Platform.IsDesktop)
                app.BeginLogTask(true);

            var selectedFormat = (ExportFormat)dialog.SelectedIndex;

            Exporting?.Invoke();

            if (!repeatLast)
                lastExportFilename = null;

            switch (selectedFormat)
            {
                case ExportFormat.WavMp3: ExportWavMp3(); break;
                case ExportFormat.Video: ExportVideo(); break;
                case ExportFormat.Nsf: ExportNsf(); break;
                case ExportFormat.Rom: ExportRom(); break;
                case ExportFormat.Midi: ExportMidi(); break;
                case ExportFormat.Text: ExportText(); break;
                case ExportFormat.FamiTracker: ExportFamiTracker(); break;
                case ExportFormat.FamiTone2Music: ExportFamiTone2Music(false); break;
                case ExportFormat.FamiStudioMusic: ExportFamiTone2Music(true); break;
                case ExportFormat.FamiTone2Sfx: ExportFamiTone2Sfx(false); break;
                case ExportFormat.FamiStudioSfx: ExportFamiTone2Sfx(true); break;
                case ExportFormat.VGM: ExportVGM(); break;
                case ExportFormat.Share: ExportShare(); break;
            }

            if (Platform.IsDesktop)
                app.EndLogTask();
        }

        public void ShowDialogAsync()
        {
            dialog.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    Export(false);
                }

                lastProjectCrc = ComputeProjectCrc(project);
            });
        }
    }
}
