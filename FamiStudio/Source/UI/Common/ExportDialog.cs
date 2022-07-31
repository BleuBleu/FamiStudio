using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace FamiStudio
{
    public class ExportDialog
    {
        enum ExportFormat
        {
            WavMp3,
            VideoPianoRoll,
            VideoOscilloscope,
            Nsf,
            Rom,
            Midi,
            CommandLog,
            Text,
            FamiTracker,
            FamiStudioMusic,
            FamiStudioSfx,
            FamiTone2Music,
            FamiTone2Sfx,
            Share,
            Max
        };

        string[] ExportFormatNames =
        {
            Platform.IsDesktop ? "WAV / MP3 / OGG" : "WAV / MP3",
            "Video (Piano Roll)",
            "Video (Oscilloscope)",
            "NSF",
            "ROM / FDS",
            "MIDI",
            "VGM / Command Log",
            "FamiStudio Text",
            "FamiTracker Text",
            "FamiStudio Music Code",
            "FamiStudio SFX Code",
            "FamiTone2 Music Code",
            "FamiTone2 SFX Code",
            "Share",
            ""
        };

        string[] ExportIcons =
        {
            "ExportWav",
            "ExportVideo",
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

        public unsafe ExportDialog(FamiStudioWindow win)
        {
            dialog = new MultiPropertyDialog(win, "Export Songs", 600, 200);
            dialog.SetVerb("Export");
            //dialog.DestroyControlsOnClose = false;
            app = win.FamiStudio;
            project = app.Project;

            for (int i = 0; i < (int)ExportFormat.Max; i++)
            {
                var format = (ExportFormat)i;
                var page = dialog.AddPropertyPage(ExportFormatNames[i], ExportIcons[i]);
                CreatePropertyPage(page, format);
            }

            // Hide a few formats we don't care about on mobile.
            dialog.SetPageVisible((int)ExportFormat.Midi,            Platform.IsDesktop);
            dialog.SetPageVisible((int)ExportFormat.Text,            Platform.IsDesktop);
            dialog.SetPageVisible((int)ExportFormat.FamiTracker,     Platform.IsDesktop);
            dialog.SetPageVisible((int)ExportFormat.FamiStudioMusic, Platform.IsDesktop);
            dialog.SetPageVisible((int)ExportFormat.FamiStudioSfx,   Platform.IsDesktop);
            dialog.SetPageVisible((int)ExportFormat.FamiTone2Music,  Platform.IsDesktop);
            dialog.SetPageVisible((int)ExportFormat.FamiTone2Sfx,    Platform.IsDesktop);
            dialog.SetPageVisible((int)ExportFormat.Share,           Platform.IsMobile);

            if (Platform.IsDesktop)
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
                channelNames[i] = ChannelType.GetNameWithExpansion(channelTypes[i]);
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

        private object[,] GetDefaultChannelsGridData()
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

            var channelTypes = project.GetActiveChannelList();
            var data = new object[channelTypes.Length, 3];
            for (int i = 0; i < channelTypes.Length; i++)
            {
                data[i, 0] = !anyChannelActive || channelActives[i];
                data[i, 1] = ChannelType.GetNameWithExpansion(channelTypes[i]);
                data[i, 2] = 50;
            }

            return data;
        }
        
        // WAV/MP3 tooltips.
        const string SingleSongTooltip    = "Select the song to export.";
        const string WavFormatTooltip     = "Audio format to export to. WAV files are uncompressed and sound better, but are much larger.";
        const string SampleRateTooltip    = "The sample rate of the audio to export. Leave to 44100 to get the exact same sound as inside the app.";
        const string AudioBitRateTooltip  = "Audio bit rate for compressed formats (MP3/OGG/AAC). Lower bit rates results in smaller files, at the cost of audio quality.";
        const string LoopModeTooltip      = "Loop the song a certain number of times, or export a specific duration (in seconds).";
        const string LoopCountTooltip     = "Number of times to loop the song.";
        const string DurationTooltip      = "Duration to export, in seconds.";
        const string DelayTooltip         = "Optional audio delay effect that will include an echo of the audio at the specified delay.\n\nHighly recommanded to use with Stereo and heavy L/R panning (ex: set channels entirely on one side) as the echo effect will be on the opposite side.\n\nA value of 15 ms. is a good place to start, set to 0 to turn off.";
        const string SeperateFilesTooltip = "If enabled, each channel will be exported to a seperate file. Useful for offline mixing.";
        const string SeperateIntroTooltip = "If enabled, the intro (the part before the loop point), will be exported to a seperate file. Useful if making games.";
        const string StereoTooltip        = "If enabled, will export stereo audio and enable custom panning for each channel in the grid below.";
        const string ChannelGridTooltip   = "Select the channels to export. If stereo is enabled, you will be able to set the panning for each channel.";
        const string ChannelListTooltip   = "Select the channels to export.";

        // Video tooltips.
        const string VideoResTooltip      = "Resolution of the exported video. Higher resolution tend to take longer to render.";
        const string FpsTooltip           = "Frame rate of the video to export. The first frame rate is for PAL, while the second is for NTSC.";
        const string VideoBitRateTooltip  = "Video bit rate. Lower bit rates results in smaller files, at the cost of video quality.";
        const string OscWindowTooltip     = "Number of frames to display in the oscilloscope.";
        const string OscColumnsTooltip    = "Number of columns in the grid of oscilloscopes.";
        const string OscThicknessTooltip  = "Thickness of the oscilloscope lines.";
        const string PianoRollZoomTootip  = "Higher zoom values scrolls faster and shows less far ahead.";

        private bool AddCommonVideoProperties(PropertyPage page, string[] songNames)
        {
            // TODO : Make this part of the VideoEncoder.
            canExportToVideo = (!Platform.IsDesktop || (!string.IsNullOrEmpty(Settings.FFmpegExecutablePath) && File.Exists(Settings.FFmpegExecutablePath)));

            if (canExportToVideo)
            {
                page.AddDropDownList("Song :", songNames, app.SelectedSong.Name, SingleSongTooltip); // 0
                page.AddDropDownList("Resolution :", VideoResolution.Names, VideoResolution.Names[0], VideoResTooltip); // 1
                page.AddDropDownList("Frame Rate :", new[] { "50/60 FPS", "25/30 FPS" }, "50/60 FPS", FpsTooltip); // 2
                page.AddDropDownList("Audio Bit Rate (Kb/s) :", new[] { "64", "96", "112", "128", "160", "192", "224", "256", "320" }, "192", AudioBitRateTooltip); // 3
                page.AddDropDownList("Video Bit Rate (Kb/s):", new[] { "250", "500", "750", "1000", "1500", "2000", "3000", "4000", "5000", "8000", "10000" }, "8000", VideoBitRateTooltip); // 4
                page.AddNumericUpDown("Loop Count :", 1, 1, 8, LoopCountTooltip); // 5
                page.AddNumericUpDown("Audio Delay (ms) :", 0, 0, 500, DelayTooltip); // 6
                page.AddNumericUpDown("Oscilloscope Window :", 2, 1, 4, OscWindowTooltip); // 7
                page.SetPropertyVisible(6, Platform.IsDesktop); // No delay on mobile, sound bad without stereo.
                return true;
            }
            else
            {
                page.AddLabel(null, "Video export requires FFmpeg. Please go in the application settings and look for the 'FFmpeg' section.", true);
                return false;
            }
        }

        private PropertyPage CreatePropertyPage(PropertyPage page, ExportFormat format)
        {
            var songNames = GetSongNames();

            switch (format)
            {
                case ExportFormat.WavMp3:
                    page.AddDropDownList("Song :", songNames, app.SelectedSong.Name, SingleSongTooltip); // 0
                    page.AddDropDownList("Format :", AudioFormatType.Names, AudioFormatType.Names[0], WavFormatTooltip); // 1
                    page.AddDropDownList("Sample Rate :", new[] { "11025", "22050", "44100", "48000" }, "44100", SampleRateTooltip); // 2
                    page.AddDropDownList("Bit Rate :", new[] { "96", "112", "128", "160", "192", "224", "256" }, "192", AudioBitRateTooltip); // 3
                    page.AddDropDownList("Mode :", new[] { "Loop N times", "Duration" }, "Loop N times", LoopModeTooltip); // 4
                    page.AddNumericUpDown("Loop count:", 1, 1, 10, LoopCountTooltip); // 5
                    page.AddNumericUpDown("Duration (sec):", 120, 1, 1000, DurationTooltip); // 6
                    page.AddNumericUpDown("Delay (ms) :", 0, 0, 500, DelayTooltip); // 7
                    page.AddCheckBox("Separate channel files", false, SeperateFilesTooltip); // 8
                    page.AddCheckBox("Separate intro file", false, SeperateIntroTooltip); // 9
                    page.AddCheckBox("Stereo", project.OutputsStereoAudio, StereoTooltip); // 10
                    if (Platform.IsDesktop)
                        page.AddGrid(new[] { new ColumnDesc("", 0.0f, ColumnType.CheckBox), new ColumnDesc("Channel", 0.4f), new ColumnDesc("Pan (% L/R)", 0.6f, ColumnType.Slider, "{0} %") }, GetDefaultChannelsGridData(), 7, ChannelGridTooltip); // 11
                    else
                        page.AddCheckBoxList("Channels", GetChannelNames(), GetDefaultActiveChannels(), ChannelListTooltip); // 11
                    page.SetPropertyEnabled( 3, false);
                    page.SetPropertyEnabled( 6, false);
                    page.SetPropertyVisible( 7, Platform.IsDesktop); // No delay on mobile, sound bad without stereo.
                    page.SetPropertyVisible( 8, Platform.IsDesktop); // No separate files on mobile.
                    page.SetPropertyVisible( 9, Platform.IsDesktop); // No separate into on mobile.
                    page.SetPropertyVisible(10, Platform.IsDesktop); // No stereo on mobile.
                    page.SetPropertyEnabled(10, !project.OutputsStereoAudio); // Force stereo for EPSM.
                    page.SetColumnEnabled(11, 2, project.OutputsStereoAudio);
                    page.PropertyChanged += WavMp3_PropertyChanged;
                    page.PropertyClicked += WavMp3_PropertyClicked;
                    break;
                case ExportFormat.VideoPianoRoll:
                    if (AddCommonVideoProperties(page, songNames)) // 0-7
                    {
                        page.AddDropDownList("Piano Roll Zoom :", new[] { "12.5%", "25%", "50%", "100%", "200%", "400%", "800%" }, project.UsesFamiTrackerTempo ? "100%" : "25%", PianoRollZoomTootip); // 8
                        page.AddCheckBox("Stereo", project.OutputsStereoAudio, StereoTooltip); // 9
                        if (Platform.IsDesktop)
                            page.AddGrid(new[] { new ColumnDesc("", 0.0f, ColumnType.CheckBox), new ColumnDesc("Channel", 0.4f), new ColumnDesc("Pan (% L/R)", 0.6f, ColumnType.Slider, "{0} %") }, GetDefaultChannelsGridData(), 10, ChannelGridTooltip); // 10
                        else
                            page.AddCheckBoxList("Channels", GetChannelNames(), GetDefaultActiveChannels(), ChannelListTooltip); // 10
                        page.SetPropertyVisible(9, Platform.IsDesktop); // Stereo on mobile.
                        page.SetPropertyEnabled(9, !project.OutputsStereoAudio); // Force stereo for EPSM.
                        page.SetColumnEnabled(10, 2, project.OutputsStereoAudio);
                        page.PropertyChanged += VideoPage_PropertyChanged;
                    }
                    break;
                case ExportFormat.VideoOscilloscope:
                    if (AddCommonVideoProperties(page, songNames)) // 0-7
                    {
                        page.AddNumericUpDown("Oscilloscope Columns :", 1, 1, 5, OscColumnsTooltip); // 8
                        page.AddNumericUpDown("Oscilloscope Thickness :", 1, 1, 4, OscThicknessTooltip); // 9
                        page.AddDropDownList("Oscilloscope Color :", OscilloscopeColorType.Names, OscilloscopeColorType.Names[OscilloscopeColorType.InstrumentsAndSamples]); // 10
                        page.AddCheckBox("Stereo", project.OutputsStereoAudio); // 11
                        if (Platform.IsDesktop)
                            page.AddGrid(new[] { new ColumnDesc("", 0.0f, ColumnType.CheckBox), new ColumnDesc("Channel", 0.4f), new ColumnDesc("Pan (% L/R)", 0.6f, ColumnType.Slider, "{0} %") }, GetDefaultChannelsGridData(), 7, ChannelGridTooltip); // 12
                        else
                            page.AddCheckBoxList("Channels", GetChannelNames(), GetDefaultActiveChannels(), ChannelListTooltip); // 12
                        page.SetPropertyVisible(11, Platform.IsDesktop); // Stereo on mobile.
                        page.SetPropertyEnabled(11, !project.OutputsStereoAudio); // Force stereo for EPSM.
                        page.SetColumnEnabled(12, 2, project.OutputsStereoAudio);
                        page.PropertyChanged += VideoPage_PropertyChanged;
                    }
                    break;
                case ExportFormat.Nsf:
                    page.AddTextBox("Name :", project.Name, 31); // 0
                    page.AddTextBox("Artist :", project.Author, 31); // 1
                    page.AddTextBox("Copyright :", project.Copyright, 31); // 2
                    page.AddDropDownList("Format :", new[] { "NSF", "NSFe" }, "NSF"); // 3
                    page.AddDropDownList("Mode :", MachineType.Names, MachineType.Names[project.PalMode ? MachineType.PAL : MachineType.NTSC]); // 4
                    page.AddCheckBoxList(Platform.IsDesktop ? null : "Songs", songNames, null, null, 12); // 5
#if DEBUG
                    page.AddDropDownList("Engine :", FamiToneKernel.Names, FamiToneKernel.Names[FamiToneKernel.FamiStudio]); // 6
#endif
                    page.SetPropertyEnabled(4, !project.UsesAnyExpansionAudio);
                    break;
                case ExportFormat.Rom:
                    if (!project.UsesMultipleExpansionAudios)
                    {
                        page.AddDropDownList("Type :", new[] { "NES ROM", "FDS Disk" }, project.UsesFdsExpansion ? "FDS Disk" : "NES ROM"); // 0
                        page.AddTextBox("Name :", project.Name.Substring(0, Math.Min(28, project.Name.Length)), 28); // 1
                        page.AddTextBox("Artist :", project.Author.Substring(0, Math.Min(28, project.Author.Length)), 28); // 2
                        page.AddDropDownList("Mode :", new[] { "NTSC", "PAL" }, project.PalMode ? "PAL" : "NTSC"); // 3
                        page.AddCheckBoxList(Platform.IsDesktop ? null : "Songs", songNames, null, null, 12); // 4
                        page.SetPropertyEnabled(0, project.UsesFdsExpansion);
                        page.SetPropertyEnabled(3, !project.UsesAnyExpansionAudio);
                    }
                    else
                    {
                        page.AddLabel(null, "ROM export does not support multiple audio expansions. Limit yourself to a single expansion to enable export.", true);
                        canExportToRom = false;
                    }
                    break;
                case ExportFormat.Midi:
                    page.AddDropDownList("Song :", songNames, app.SelectedSong.Name); // 0
                    page.AddCheckBox("Export volume as velocity :", true); // 1
                    page.AddCheckBox("Export slide notes as pitch wheel :", true); // 2
                    page.AddNumericUpDown("Pitch wheel range :", 24, 1, 24); // 3
                    page.AddDropDownList("Instrument Mode :", MidiExportInstrumentMode.Names, MidiExportInstrumentMode.Names[0]); // 4
                    page.AddGrid(new[] { new ColumnDesc("", 0.4f), new ColumnDesc("", 0.6f, MidiFileReader.MidiInstrumentNames) }, null, 14); // 5
                    page.PropertyChanged += Midi_PropertyChanged;
                    break;
                case ExportFormat.Text:
                    page.AddCheckBoxList(null, songNames, null, null, 12); // 0
                    page.AddCheckBox("Delete unused data :", false); // 1
                    break;
                case ExportFormat.FamiTracker:
                    if (!project.UsesMultipleExpansionAudios)
                    {
                        page.AddCheckBoxList(null, songNames, null, null, 12); // 0
                        canExportToFamiTracker = true;
                    }
                    else
                    {
                        page.AddLabel(null, "The original FamiTracker does not support multiple audio expansions. Limit yourself to a single expansion to enable export.", true);
                        canExportToFamiTracker = false;
                    }
                    break;
                case ExportFormat.FamiTone2Music:
                case ExportFormat.FamiStudioMusic:
                    if (format == ExportFormat.FamiTone2Music && project.UsesAnyExpansionAudio)
                    {
                        page.AddLabel(null, "FamiTone2 does not support audio expansions.", true);
                        canExportToFamiTone2 = false;
                    }
                    else if (format == ExportFormat.FamiStudioMusic && project.UsesMultipleExpansionAudios)
                    {
                        page.AddLabel(null, "The FamiStudio Sound Engine only supports a single expansion at a time. Limit yourself to a single expansion to enable export.", true);
                        canExportToSoundEngine = false;
                    }
                    else
                    {
                        page.AddDropDownList("Format :", AssemblyFormat.Names, AssemblyFormat.Names[0]); // 0
                        page.AddCheckBox("Separate Files :", false); // 1
                        page.AddTextBox("Song Name Pattern :", "{project}_{song}"); // 2
                        page.AddTextBox("DMC Name Pattern :", "{project}"); // 3
                        page.AddCheckBox("Generate song list include :", false); // 4
                        page.AddCheckBoxList(null, songNames, null, null, 12); // 5
                        page.SetPropertyEnabled(2, false);
                        page.SetPropertyEnabled(3, false);
                        page.PropertyChanged += SoundEngine_PropertyChanged;
                    }
                    break;
                case ExportFormat.FamiTone2Sfx:
                case ExportFormat.FamiStudioSfx:
                    page.AddDropDownList("Format :", AssemblyFormat.Names, AssemblyFormat.Names[0]); // 0
                    page.AddDropDownList("Mode :", MachineType.Names, MachineType.Names[project.PalMode ? MachineType.PAL : MachineType.NTSC]); // 1
                    page.AddCheckBox("Generate SFX list include :", false); // 2
                    page.AddCheckBoxList(null, songNames, null, null, 12); // 3
                    break;
                case ExportFormat.CommandLog:
                    page.AddDropDownList("Song :", songNames, app.SelectedSong.Name); // 0
                    page.AddDropDownList("Filetype :", new[] { "Command Log", "VGM"}, "VGM"); // 1
                    break;
                case ExportFormat.Share:
                    page.AddRadioButtonList("Sharing mode", new[] { "Copy to Storage", "Share" }, 0, "Copy the FamiStudio project to your phone's storage, or share it to another application.");
                    break;
            }

            page.Build();

            return page;
        }

        private void VideoPage_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == props.PropertyCount - 2) // Stereo
            {
                props.SetColumnEnabled(props.PropertyCount - 1, 2, (bool)value);
            }
        }

        private void WavMp3_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 1)
            {
                props.SetPropertyEnabled(3, (string)value != "WAV");
            }
            else if (propIdx == 4)
            {
                props.SetPropertyEnabled(5, (string)value != "Duration");
                props.SetPropertyEnabled(6, (string)value == "Duration");
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
        }

        private void WavMp3_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 10 && click == ClickType.Right && colIdx == 2)
            {
                props.UpdateGrid(propIdx, rowIdx, colIdx, 50);
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
            Platform.ShowToast(dialog.ParentWindow, $"{format} Export {(success ? "Successful" : "Failed")}!");
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
                    var loopCount = props.GetPropertyValue<string>(4) != "Duration" ? props.GetPropertyValue<int>(5) : -1;
                    var duration = props.GetPropertyValue<string>(4) == "Duration" ? props.GetPropertyValue<int>(6) : -1;
                    var delay = props.GetPropertyValue<int>(7);
                    var separateFiles = props.GetPropertyValue<bool>(8);
                    var separateIntro = props.GetPropertyValue<bool>(9);
                    var stereo = props.GetPropertyValue<bool>(10) && (!separateFiles || project.OutputsStereoAudio);
                    var song = project.GetSong(songName);

                    var channelCount = project.GetActiveChannelCount();
                    var channelMask = 0L;
                    var pan = (float[])null;

                    if (Platform.IsDesktop)
                    {
                        pan = new float[channelCount]; 

                        for (int i = 0; i < channelCount; i++)
                        {
                            if (props.GetPropertyValue<bool>(11, i, 0))
                                channelMask |= (1L << i);

                            pan[i] = props.GetPropertyValue<int>(11, i, 2) / 100.0f;
                        }
                    }
                    else
                    {
                        var selectedChannels = props.GetPropertyValue<bool[]>(11);
                        for (int i = 0; i < channelCount; i++)
                        {
                            if (selectedChannels[i])
                                channelMask |= (1L << i);
                        }
                    }

                    AudioExportUtils.Save(song, filename, sampleRate, loopCount, duration, channelMask, separateFiles, separateIntro, stereo, pan, delay, Platform.IsMobile || project.UsesEPSMExpansion,
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
                Platform.StartMobileSaveFileOperationAsync(AudioFormatType.MimeTypes[format], $"{songName}", (f) =>
                {
                    new Thread(() =>
                    {
                        app.BeginLogTask(true, "Exporting Audio", "Exporting long songs with lots of channels may take a while.");

                        ExportWavMp3Action(f);
                        
                        Platform.FinishMobileSaveFileOperationAsync(true, () =>
                        {
                            var aborted = Log.ShouldAbortOperation;
                            app.EndLogTask();
                            ShowExportResultToast("Audio", !aborted);
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
                    filename = Platform.ShowSaveFileDialog(dialog.ParentWindow,
                        $"Export {AudioFormatType.Names[format]} File",
                        $"{AudioFormatType.Names[format]} Audio File (*.{AudioFormatType.Extensions[format]})|*.{AudioFormatType.Extensions[format]}",
                        ref Settings.LastExportFolder);
                }

                ExportWavMp3Action(filename);
                ShowExportResultToast("Audio");
            }
        }

        private void ExportVideo(bool pianoRoll)
        {
            if (!canExportToVideo)
                return;

            var props = dialog.GetPropertyPage(pianoRoll ? (int)ExportFormat.VideoPianoRoll : (int)ExportFormat.VideoOscilloscope);

            Func<string, bool> ExportVideoAction = (filename) =>
            {
                if (filename != null)
                {
                    var stereoPropIdx   = pianoRoll ? 9 : 11;
                    var channelsPropIdx = pianoRoll ? 10 : 12;

                    var songName = props.GetPropertyValue<string>(0);
                    var resolutionIdx = props.GetSelectedIndex(1);
                    var resolutionX = VideoResolution.ResolutionX[resolutionIdx];
                    var resolutionY = VideoResolution.ResolutionY[resolutionIdx];
                    var halfFrameRate = props.GetSelectedIndex(2) == 1;
                    var audioBitRate = Convert.ToInt32(props.GetPropertyValue<string>(3), CultureInfo.InvariantCulture);
                    var videoBitRate = Convert.ToInt32(props.GetPropertyValue<string>(4), CultureInfo.InvariantCulture);
                    var loopCount = props.GetPropertyValue<int>(5);
                    var delay = props.GetPropertyValue<int>(6);
                    var oscWindow = props.GetPropertyValue<int>(7);
                    var stereo = props.GetPropertyValue<bool>(stereoPropIdx);
                    var song = project.GetSong(songName);
                    var channelCount = project.GetActiveChannelCount();
                    var channelMask = 0L;
                    var pan = (float[])null;

                    if (Platform.IsDesktop)
                    {
                        pan = new float[channelCount];

                        for (int i = 0; i < channelCount; i++)
                        {
                            if (props.GetPropertyValue<bool>(channelsPropIdx, i, 0))
                                channelMask |= (1L << i);

                            pan[i] = props.GetPropertyValue<int>(channelsPropIdx, i, 2) / 100.0f;
                        }
                    }
                    else
                    {
                        var selectedChannels = props.GetPropertyValue<bool[]>(channelsPropIdx);
                        for (int i = 0; i < channelCount; i++)
                        {
                            if (selectedChannels[i])
                                channelMask |= (1L << i);
                        }
                    }

                    lastExportFilename = filename;

                    if (pianoRoll)
                    {
                        var pianoRollZoom = (float)Math.Pow(2.0, props.GetSelectedIndex(7) - 3);

                        return new VideoFilePianoRoll().Save(project, song.Id, loopCount, oscWindow, filename, resolutionX, resolutionY, halfFrameRate, channelMask, delay, audioBitRate, videoBitRate, pianoRollZoom, stereo, pan);
                    }
                    else
                    {
                        var oscNumColumns    = props.GetPropertyValue<int>(8);
                        var oscLineThickness = props.GetPropertyValue<int>(9);
                        var oscColorMode     = props.GetSelectedIndex(10);

                        return new VideoFileOscilloscope().Save(project, song.Id, loopCount, oscColorMode, oscNumColumns, oscLineThickness, oscWindow, filename, resolutionX, resolutionY, halfFrameRate, channelMask, delay, audioBitRate, videoBitRate, stereo, pan);
                    }
                }
                else
                {
                    return false;
                }
            };

            if (Platform.IsMobile)
            {
                var songName = props.GetPropertyValue<string>(0);
                Platform.StartMobileSaveFileOperationAsync("video/mp4", $"{songName}", (f) =>
                {
                    new Thread(() =>
                    {
                        app.BeginLogTask(true, "Exporting Video", 
                            "Exporting videos may take a very long time, especially at high resolutions. " +
                            "Make sure FamiStudio remains open, clicking BACK or closing this window will abort the operation. " +
                            "FamiStudio is currently preventing the screen from going to sleep.\n\n" +
                            "Also please note that for reasons outside of our control, the video encoding quality on mobile " +
                            "is inferior to the desktop version of FamiStudio.");
                        
                        var success = ExportVideoAction(f);

                        Platform.FinishMobileSaveFileOperationAsync(success, () =>
                        {
                            app.EndLogTask();
                            ShowExportResultToast("Video", success);
                        });

                    }).Start();
                });
            }
            else
            {
                var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog(dialog.ParentWindow, "Export Video File", "MP4 Video File (*.mp4)|*.mp4", ref Settings.LastExportFolder);
                ExportVideoAction(filename);
                ShowExportResultToast("Video");
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
                    var kernel = FamiToneKernel.GetValueForName(props.GetPropertyValue<string>(6));
#else
                    var kernel = FamiToneKernel.FamiStudio;
#endif

                    new NsfFile().Save(project, kernel, filename,
                        GetSongIds(props.GetPropertyValue<bool[]>(5)),
                        props.GetPropertyValue<string>(0),
                        props.GetPropertyValue<string>(1),
                        props.GetPropertyValue<string>(2),
                        mode,
                        nsfe);

                    lastExportFilename = filename;
                }
            };

            if (Platform.IsMobile)
            {
                Platform.StartMobileSaveFileOperationAsync("*/*", $"{project.Name}.{extension}", (f) =>
                {
                    ExportNsfAction(f);
                    Platform.FinishMobileSaveFileOperationAsync(true, () => { ShowExportResultToast("NSF"); });
                });
            }
            else
            {
                var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog(dialog.ParentWindow, "Export NSF File", $"Nintendo Sound Files (*.{extension})|*.{extension}", ref Settings.LastExportFolder);
                ExportNsfAction(filename);
                ShowExportResultToast("NSF");
            }
        }

        private void ExportRom()
        {
            if (!canExportToRom)
                return;

            var props = dialog.GetPropertyPage((int)ExportFormat.Rom);
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(4));

            if (songIds.Length > RomFileBase.MaxSongs)
            {
                Platform.MessageBoxAsync(dialog.ParentWindow, $"Please select {RomFileBase.MaxSongs} songs or less.", "ROM Export", MessageBoxButtons.OK);
                return;
            }

            if (props.GetPropertyValue<string>(0) == "NES ROM")
            {
                Action<string> ExportRomAction = (filename) =>
                {
                    if (filename != null)
                    {
                        var rom = new RomFile();
                        rom.Save(
                            project, filename, songIds,
                            props.GetPropertyValue<string>(1),
                            props.GetPropertyValue<string>(2),
                            props.GetPropertyValue<string>(3) == "PAL");

                        lastExportFilename = filename;
                    }
                };

                if (Platform.IsMobile)
                {
                    Platform.StartMobileSaveFileOperationAsync("*/*", $"{project.Name}.nes", (f) =>
                    {
                        ExportRomAction(f);
                        Platform.FinishMobileSaveFileOperationAsync(true, () => { ShowExportResultToast("NES ROM"); });
                    });
                }
                else
                {
                    var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog(dialog.ParentWindow, "Export ROM File", "NES ROM (*.nes)|*.nes", ref Settings.LastExportFolder);
                    ExportRomAction(filename);
                    ShowExportResultToast("NES ROM");
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
                            props.GetPropertyValue<string>(2));

                        lastExportFilename = filename;
                    }
                };

                if (Platform.IsMobile)
                {
                    Platform.StartMobileSaveFileOperationAsync("*/*", $"{project.Name}.fds", (f) =>
                    {
                        ExportFdsAction(f);
                        Platform.FinishMobileSaveFileOperationAsync(true, () => { ShowExportResultToast("FDS Disk"); });
                    });
                }
                else
                {
                    var filename = lastExportFilename != null ? null : Platform.ShowSaveFileDialog(dialog.ParentWindow, "Export Famicom Disk", "FDS Disk (*.fds)|*.fds", ref Settings.LastExportFolder);
                    ExportFdsAction(filename);
                    ShowExportResultToast("FDS Disk");
                }
            }
        }

        private void ExportShare()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.Share);
            var share = props.GetSelectedIndex(0) == 1;

            var filename = !string.IsNullOrEmpty(app.Project.Filename) ? Path.GetFileName(app.Project.Filename) : $"{project.Name}.fms";

            if (share)
            {
                filename = Platform.GetShareFilename(filename);
                app.SaveProjectCopy(filename);
                Platform.StartShareFileAsync(filename, () => 
                {
                    Platform.ShowToast(dialog.ParentWindow, "Sharing Successful!");
                });
            }
            else
            {
                Platform.StartMobileSaveFileOperationAsync("*/*", filename, (f) =>
                {
                    app.SaveProjectCopy(f);
                    Platform.FinishMobileSaveFileOperationAsync(true, () => { Platform.ShowToast(dialog.ParentWindow, "Sharing Successful!"); });
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
        }

        private void Midi_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 4)
                UpdateMidiInstrumentMapping();
        }

        private void UpdateMidiInstrumentMapping()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.Midi);
            var mode  = props.GetSelectedIndex(4);
            var data  = (object[,])null;
            var cols = new[] { "", "MIDI Instrument" };

            if (mode == MidiExportInstrumentMode.Instrument)
            {
                data = new object[project.Instruments.Count, 2];
                for (int i = 0; i < project.Instruments.Count; i++)
                {
                    var inst = project.Instruments[i];
                    data[i, 0] = inst.NameWithExpansion;
                    data[i, 1] = MidiFileReader.MidiInstrumentNames[0];
                }

                cols[0] = "FamiStudio Instrument";
            }
            else
            {
                var firstSong = project.Songs[0];

                data = new object[firstSong.Channels.Length, 2];
                for (int i = 0; i < firstSong.Channels.Length; i++)
                {
                    data[i, 0] = firstSong.Channels[i].Name;
                    data[i, 1] = MidiFileReader.MidiInstrumentNames[0];
                }

                cols[0] = "NES Channel";
            }

            props.UpdateGrid(5, data, cols);
        }

        private void ExportMidi()
        {
            var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog(dialog.ParentWindow, "Export MIDI File", "MIDI Files (*.mid)|*.mid", ref Settings.LastExportFolder);
            if (filename != null)
            {
                var props = dialog.GetPropertyPage((int)ExportFormat.Midi);
                var songName = props.GetPropertyValue<string>(0);
                var velocity = props.GetPropertyValue<bool>(1);
                var slideNotes = props.GetPropertyValue<bool>(2);
                var pitchRange = props.GetPropertyValue<int>(3);
                var instrumentMode = props.GetSelectedIndex(4);
                var song = project.GetSong(songName);
                var instrumentMapping = new int[instrumentMode == MidiExportInstrumentMode.Channel ? song.Channels.Length : song.Project.Instruments.Count];

                for (int i = 0; i < instrumentMapping.Length; i++)
                    instrumentMapping[i] = Array.IndexOf(MidiFileReader.MidiInstrumentNames, props.GetPropertyValue<string>(5, i, 1));

                new MidiFileWriter().Save(project, filename, song.Id, instrumentMode, instrumentMapping, velocity, slideNotes, pitchRange);

                ShowExportResultToast("MIDI");

                lastExportFilename = filename;
            }
        }

        private void ExportText()
        {
            var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog(dialog.ParentWindow, "Export FamiStudio Text File", "FamiStudio Text Export (*.txt)|*.txt", ref Settings.LastExportFolder);
            if (filename != null)
            {
                var props = dialog.GetPropertyPage((int)ExportFormat.Text);
                var deleteUnusedData = props.GetPropertyValue<bool>(1);
                new FamistudioTextFile().Save(project, filename, GetSongIds(props.GetPropertyValue<bool[]>(0)), deleteUnusedData);
                ShowExportResultToast("FamiStudio Text");
                lastExportFilename = filename;
            }
        }

        private void ExportCommandLog()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.CommandLog);
            var songName = props.GetPropertyValue<string>(0);
            var ext = "asm";
            var exportText = "Command Log";
            var filetype = props.GetPropertyValue<string>(1) == "VGM" ? 1 : 0;
            if(filetype == 1)
            {
                ext = "vgm";
                exportText = "VGM";
            }
            var song = project.GetSong(songName);
            var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog(dialog.ParentWindow, $"Export {exportText}", $"{exportText} File (*.{ext})|*.{ext}", ref Settings.LastExportFolder);
            ShowExportResultToast(exportText);
            if (filename != null)
            {
                VgmExport.Save(song, filename, filetype);
                lastExportFilename = filename;
            }
        }
		
        private void ExportFamiTracker()
        {
            if (!canExportToFamiTracker)
                return;

            var props = dialog.GetPropertyPage((int)ExportFormat.FamiTracker);

            var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog(dialog.ParentWindow, "Export FamiTracker Text File", "FamiTracker Text Format (*.txt)|*.txt", ref Settings.LastExportFolder);
            if (filename != null)
            {
                new FamitrackerTextFile().Save(project, filename, GetSongIds(props.GetPropertyValue<bool[]>(0)));
                ShowExportResultToast("FamiTracker");
                lastExportFilename = filename;
            }
        }

        private void ExportFamiTone2Music(bool famiStudio)
        {
            if ((famiStudio && !canExportToSoundEngine) || (!famiStudio && !canExportToFamiTone2))
                return;

            var props = dialog.GetPropertyPage(famiStudio ? (int)ExportFormat.FamiStudioMusic : (int)ExportFormat.FamiTone2Music);

            var separate = props.GetPropertyValue<bool>(1);
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(5));
            var kernel = famiStudio ? FamiToneKernel.FamiStudio : FamiToneKernel.FamiTone2;
            var exportFormat = AssemblyFormat.GetValueForName(props.GetPropertyValue<string>(0));
            var ext = exportFormat == AssemblyFormat.CA65 ? "s" : "asm";
            var songNamePattern = props.GetPropertyValue<string>(2);
            var dpcmNamePattern = props.GetPropertyValue<string>(3);
            var generateInclude = props.GetPropertyValue<bool>(4);

            if (separate)
            {
                var folder = lastExportFilename != null ? lastExportFilename : Platform.ShowBrowseFolderDialog(dialog.ParentWindow, "Select the export folder", ref Settings.LastExportFolder);

                if (folder != null)
                {
                    foreach (var songId in songIds)
                    {
                        var song = project.GetSong(songId);
                        var formattedSongName = songNamePattern.Replace("{project}", project.Name).Replace("{song}", song.Name);
                        var formattedDpcmName = dpcmNamePattern.Replace("{project}", project.Name).Replace("{song}", song.Name);
                        var songFilename = Path.Combine(folder, Utils.MakeNiceAsmName(formattedSongName) + "." + ext);
                        var dpcmFilename = Path.Combine(folder, Utils.MakeNiceAsmName(formattedDpcmName) + ".dmc");
                        var includeFilename = generateInclude ? Path.ChangeExtension(songFilename, null) + "_songlist.inc" : null;

                        Log.LogMessage(LogSeverity.Info, $"Exporting song '{song.Name}' as separate assembly files.");

                        FamitoneMusicFile f = new FamitoneMusicFile(kernel, true);
                        f.Save(project, new int[] { songId }, exportFormat, true, songFilename, dpcmFilename, includeFilename, MachineType.Dual);
                    }

                    lastExportFilename = folder;
                    ShowExportResultToast("Assembly");
                }
            }
            else
            {
                var engineName = famiStudio ? "FamiStudio" : "FamiTone2";
                var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog(dialog.ParentWindow, $"Export {engineName} Assembly Code", $"{engineName} Assembly File (*.{ext})|*.{ext}", ref Settings.LastExportFolder);
                if (filename != null)
                {
                    var includeFilename = generateInclude ? Path.ChangeExtension(filename, null) + "_songlist.inc" : null;

                    Log.LogMessage(LogSeverity.Info, $"Exporting all songs to a single assembly file.");

                    FamitoneMusicFile f = new FamitoneMusicFile(kernel, true);
                    f.Save(project, songIds, exportFormat, false, filename, Path.ChangeExtension(filename, ".dmc"), includeFilename, MachineType.Dual);

                    lastExportFilename = filename;
                    ShowExportResultToast("Assembly");
                }
            }
        }

        private void ExportFamiTone2Sfx(bool famiStudio)
        {
            var props = dialog.GetPropertyPage(famiStudio ? (int)ExportFormat.FamiStudioSfx : (int)ExportFormat.FamiTone2Sfx);
            var exportFormat = AssemblyFormat.GetValueForName(props.GetPropertyValue<string>(0));
            var ext = exportFormat == AssemblyFormat.CA65 ? "s" : "asm";
            var mode = MachineType.GetValueForName(props.GetPropertyValue<string>(1));
            var engineName = famiStudio ? "FamiStudio" : "FamiTone2";
            var generateInclude = props.GetPropertyValue<bool>(2);
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(3));

            var filename = lastExportFilename != null ? lastExportFilename : Platform.ShowSaveFileDialog(dialog.ParentWindow, $"Export {engineName} Code", $"{engineName} Assembly File (*.{ext})|*.{ext}", ref Settings.LastExportFolder);
            if (filename != null)
            {
                var includeFilename = generateInclude ? Path.ChangeExtension(filename, null) + "_sfxlist.inc" : null;

                FamitoneSoundEffectFile f = new FamitoneSoundEffectFile();
                f.Save(project, songIds, exportFormat, mode, famiStudio ? FamiToneKernel.FamiStudio : FamiToneKernel.FamiTone2, filename, includeFilename);
                ShowExportResultToast("SFX Assembly");
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
        
        //public void DestroyControls()
        //{
        //    dialog.DestroyControls();
        //}

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
                case ExportFormat.VideoPianoRoll: ExportVideo(true); break;
                case ExportFormat.VideoOscilloscope: ExportVideo(false); break;
                case ExportFormat.Nsf: ExportNsf(); break;
                case ExportFormat.Rom: ExportRom(); break;
                case ExportFormat.Midi: ExportMidi(); break;
                case ExportFormat.Text: ExportText(); break;
                case ExportFormat.FamiTracker: ExportFamiTracker(); break;
                case ExportFormat.FamiTone2Music: ExportFamiTone2Music(false); break;
                case ExportFormat.FamiStudioMusic: ExportFamiTone2Music(true); break;
                case ExportFormat.FamiTone2Sfx: ExportFamiTone2Sfx(false); break;
                case ExportFormat.FamiStudioSfx: ExportFamiTone2Sfx(true); break;
                case ExportFormat.CommandLog: ExportCommandLog(); break;
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
