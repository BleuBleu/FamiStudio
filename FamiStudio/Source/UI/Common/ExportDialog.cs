using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace FamiStudio
{
#if !FAMISTUDIO_ANDROID // DROIDTODO!
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
            Text,
            FamiTracker,
            FamiStudioMusic,
            FamiStudioSfx,
            FamiTone2Music,
            FamiTone2Sfx,
            Max
        };

        string[] ExportFormatNames =
        {
            "WAV / MP3 / OGG",
            "Video (Piano Roll)",
            "Video (Oscilloscope)",
            "NSF",
            "ROM / FDS",
            "MIDI",
            "FamiStudio Text",
            "FamiTracker Text",
            "FamiStudio Music Code",
            "FamiStudio SFX Code",
            "FamiTone2 Music Code",
            "FamiTone2 SFX Code",
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
            "ExportText",
            "ExportFamiTracker",
            "ExportFamiStudioEngine",
            "ExportFamiStudioEngine",
            "ExportFamiTone2",
            "ExportFamiTone2"
        };

        private Project project;
        private MultiPropertyDialog dialog;
        private uint lastExportCrc;
        private string lastExportFilename;

        public delegate void EmptyDelegate();
        public event EmptyDelegate Exporting;

        public unsafe ExportDialog(Project project)
        {
            int width  = 600;
            int height = 550;

#if FAMISTUDIO_LINUX
            height += 100;
#elif FAMISTUDIO_MACOS
            height += 80;
#endif

            this.dialog = new MultiPropertyDialog(width, height, 200);
            this.project = project;

            for (int i = 0; i < (int)ExportFormat.Max; i++)
            {
                var format = (ExportFormat)i;
                var page = dialog.AddPropertyPage(ExportFormatNames[i], ExportIcons[i]);
                CreatePropertyPage(page, format);
            }
        }

        private string[] GetSongNames()
        {
            var names = new string[project.Songs.Count];
            for (var i = 0; i < project.Songs.Count; i++)
                names[i] = project.Songs[i].Name;
            return names;
        }

        private object[,] GetDefaultChannelsData()
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

        private bool AddCommonVideoProperties(PropertyPage page, string[] songNames)
        {
            if (!string.IsNullOrEmpty(Settings.FFmpegExecutablePath) && File.Exists(Settings.FFmpegExecutablePath))
            {
                page.AddDropDownList("Song :", songNames, songNames[0]); // 0
                page.AddDropDownList("Resolution :", VideoResolution.Names, VideoResolution.Names[0]); // 1
                page.AddDropDownList("Frame Rate :", new[] { "50/60 FPS", "25/30 FPS" }, "50/60 FPS"); // 2
                page.AddDropDownList("Audio Bit Rate (Kb/s) :", new[] { "64", "96", "112", "128", "160", "192", "224", "256" }, "128"); // 3
                page.AddDropDownList("Video Bit Rate (Kb/s):", new[] { "250", "500", "750", "1000", "1500", "2000", "3000", "4000", "5000", "8000", "10000" }, "8000"); // 4
                page.AddIntegerRange("Loop Count :", 1, 1, 8); // 5
                return true;
            }
            else
            {
                page.AddLabel(null, "Video export requires FFmpeg. Please go in the application settings and look for the 'FFmpeg' section.", true);
#if FAMISTUDIO_LINUX || FAMISTUDIO_MACOS
                // HACK : Last minute hack, too lazy to debug GTK layouting issues right now.
                page.AddLabel(null, " ");
#endif
                return false;
            }
        }

        private PropertyPage CreatePropertyPage(PropertyPage page, ExportFormat format)
        {
            var songNames = GetSongNames();

            switch (format)
            {
                case ExportFormat.WavMp3:
                    page.AddDropDownList("Song :", songNames, songNames[0]); // 0
                    page.AddDropDownList("Format :", AudioFormatType.Names, AudioFormatType.Names[0]); // 1
                    page.AddDropDownList("Sample Rate :", new[] { "11025", "22050", "44100", "48000" }, "44100"); // 2
                    page.AddDropDownList("Bit Rate :", new[] { "96", "112", "128", "160", "192", "224", "256" }, "128"); // 3
                    page.AddDropDownList("Mode :", new[] { "Loop N times", "Duration" }, "Loop N times"); // 4
                    page.AddIntegerRange("Loop count:", 1, 1, 10); // 5
                    page.AddIntegerRange("Duration (sec):", 120, 1, 1000); // 6
                    page.AddCheckBox("Separate channel files", false); // 7
                    page.AddCheckBox("Separate intro file", false); // 8
                    page.AddCheckBox("Stereo", false); // 9
                    page.AddMultiColumnList(new[] { new ColumnDesc("", 0.0f, ColumnType.CheckBox), new ColumnDesc("Channel", 0.4f), new ColumnDesc("Pan (% L/R)", 0.6f, ColumnType.Slider, "{0} %") }, GetDefaultChannelsData(), 200); // 10
                    page.SetPropertyEnabled(3, false);
                    page.SetPropertyEnabled(6, false);
                    page.SetColumnEnabled(10, 2, false);
                    page.PropertyChanged += WavMp3_PropertyChanged;
                    page.PropertyClicked += WavMp3_PropertyClicked;
                    break;
                case ExportFormat.VideoPianoRoll:
                    if (AddCommonVideoProperties(page, songNames)) // 0-5
                    {
                        page.AddDropDownList("Piano Roll Zoom :", new[] { "12.5%", "25%", "50%", "100%", "200%", "400%", "800%" }, project.UsesFamiTrackerTempo ? "100%" : "25%", "Higher zoom values scrolls faster and shows less far ahead."); // 6
                        page.AddCheckBox("Stereo", false); // 7
                        page.AddMultiColumnList(new[] { new ColumnDesc("", 0.0f, ColumnType.CheckBox), new ColumnDesc("Channel", 0.4f), new ColumnDesc("Pan (% L/R)", 0.6f, ColumnType.Slider, "{0} %") }, GetDefaultChannelsData(), 200); // 8
                        page.SetColumnEnabled(8, 2, false);
                        page.PropertyChanged += VideoPage_PropertyChanged;
                    }
                    break;
                case ExportFormat.VideoOscilloscope:
                    if (AddCommonVideoProperties(page, songNames)) // 0-5
                    {
                        page.AddIntegerRange("Oscilloscope Columns :", 1, 1, 5); // 6
                        page.AddIntegerRange("Oscilloscope Thickness :", 1, 1, 4); // 7
                        page.AddDropDownList("Oscilloscope Color :", OscilloscopeColorType.Names, OscilloscopeColorType.Names[OscilloscopeColorType.InstrumentsAndSamples]); // 8
                        page.AddCheckBox("Stereo", false); // 9
                        page.AddMultiColumnList(new[] { new ColumnDesc("", 0.0f, ColumnType.CheckBox), new ColumnDesc("Channel", 0.4f), new ColumnDesc("Pan (% L/R)", 0.6f, ColumnType.Slider, "{0} %") }, GetDefaultChannelsData(), 200); // 10
                        page.SetColumnEnabled(10, 2, false);
                        page.PropertyChanged += VideoPage_PropertyChanged;
                    }
                    break;
                case ExportFormat.Nsf:
                    page.AddString("Name :", project.Name, 31); // 0
                    page.AddString("Artist :", project.Author, 31); // 1
                    page.AddString("Copyright :", project.Copyright, 31); // 2
                    page.AddDropDownList("Mode :", MachineType.Names, MachineType.Names[project.PalMode ? MachineType.PAL : MachineType.NTSC]); // 3
                    page.AddCheckBoxList(null, songNames, null); // 4
#if DEBUG
                    page.AddDropDownList("Engine :", FamiToneKernel.Names, FamiToneKernel.Names[FamiToneKernel.FamiStudio]); // 5
#endif
                    page.SetPropertyEnabled(3, !project.UsesAnyExpansionAudio);
                    break;
                case ExportFormat.Rom:
                    page.AddDropDownList("Type :", new[] { "NES ROM", "FDS Disk" }, project.UsesFdsExpansion ? "FDS Disk" : "NES ROM"); // 0
                    page.AddString("Name :", project.Name.Substring(0, Math.Min(28, project.Name.Length)), 28); // 1
                    page.AddString("Artist :", project.Author.Substring(0, Math.Min(28, project.Author.Length)), 28); // 2
                    page.AddDropDownList("Mode :", new[] { "NTSC", "PAL" }, project.PalMode ? "PAL" : "NTSC"); // 3
                    page.AddCheckBoxList(null, songNames, null); // 4
                    if (project.UsesAnyExpansionAudio)
                        page.AddLabel(null, "ROM export does not support audio expansions. FDS disk export only supports the FDS expansion. Any incompatible expansion channel(s) will be ignored during the export.", true);
                    page.SetPropertyEnabled(0,  project.UsesFdsExpansion);
                    page.SetPropertyEnabled(3, !project.UsesAnyExpansionAudio);
                    break;
                case ExportFormat.Midi:
                    page.AddDropDownList("Song :", songNames, songNames[0]); // 0
                    page.AddCheckBox("Export volume as velocity :", true); // 1
                    page.AddCheckBox("Export slide notes as pitch wheel :", true); // 2
                    page.AddIntegerRange("Pitch wheel range :", 24, 1, 24); // 3
                    page.AddDropDownList("Instrument Mode :", MidiExportInstrumentMode.Names, MidiExportInstrumentMode.Names[0]); // 4
                    page.AddMultiColumnList(new[] { new ColumnDesc("", 0.4f), new ColumnDesc("", 0.6f, MidiFileReader.MidiInstrumentNames) }, null); // 5
                    page.PropertyChanged += Midi_PropertyChanged;
                    break;
                case ExportFormat.Text:
                    page.AddCheckBoxList(null, songNames, null); // 0
                    page.AddCheckBox("Delete unused data :", false); // 1
                    break;
                case ExportFormat.FamiTracker:
                    if (!project.UsesMultipleExpansionAudios)
                        page.AddCheckBoxList(null, songNames, null); // 0
                    else
                        page.AddLabel(null, "The original FamiTracker does not support multiple audio expansions. Limit yourself to a single expansion to enable export.", true);
                    break;
                case ExportFormat.FamiTone2Music:
                case ExportFormat.FamiStudioMusic:
                    page.AddDropDownList("Format :", AssemblyFormat.Names, AssemblyFormat.Names[0]); // 0
                    page.AddCheckBox("Separate Files :", false); // 1
                    page.AddString("Song Name Pattern :", "{project}_{song}"); // 2
                    page.AddString("DMC Name Pattern :", "{project}"); // 3
                    page.AddCheckBox("Generate song list include :", false); // 4
                    page.AddCheckBoxList(null, songNames, null); // 5
                    page.SetPropertyEnabled(2, false);
                    page.SetPropertyEnabled(3, false);
                    page.PropertyChanged += SoundEngine_PropertyChanged;
                    break;
                case ExportFormat.FamiTone2Sfx:
                case ExportFormat.FamiStudioSfx:
                    page.AddDropDownList("Format :", AssemblyFormat.Names, AssemblyFormat.Names[0]); // 0
                    page.AddDropDownList("Mode :", MachineType.Names, MachineType.Names[project.PalMode ? MachineType.PAL : MachineType.NTSC]); // 1
                    page.AddCheckBox("Generate SFX list include :", false); // 2
                    page.AddCheckBoxList(null, songNames, null); // 3
                    break;
            }

            page.Build();

            if (format == ExportFormat.Midi)
            {
                UpdateMidiInstrumentMapping();
            }

            return page;
        }

        private void VideoPage_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == props.PropertyCount - 2) // Stereo
            {
                props.SetColumnEnabled(props.PropertyCount - 1, 2, (bool)value);
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
            else if (propIdx == 7)
            {
                var separateChannels = (bool)value;

                props.SetPropertyEnabled(9, !separateChannels);
                if (separateChannels)
                    props.SetPropertyValue(9, false);

                props.SetColumnEnabled(10, 2, props.GetPropertyValue<bool>(9));
            }
            else if (propIdx == 9)
            {
                props.SetColumnEnabled(10, 2, (bool)value);
            }
        }

        private void WavMp3_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (propIdx == 9 && click == ClickType.Right && colIdx == 2)
            {
                props.UpdateMultiColumnList(propIdx, rowIdx, colIdx, 50);
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

            props.UpdateMultiColumnList(5, data, cols);
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

        private void ExportWavMp3()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.WavMp3);
            var format = props.GetSelectedIndex(1);
            var filename = (string)null;

            if (lastExportFilename != null)
            {
                filename = lastExportFilename;
            }
            else
            {
                filename = PlatformUtils.ShowSaveFileDialog(
                    $"Export {AudioFormatType.Names[format]} File",
                    $"{AudioFormatType.Names[format]} Audio File (*.{AudioFormatType.Extentions[format]})|*.{AudioFormatType.Extentions[format]}",
                    ref Settings.LastExportFolder);
            }

            if (filename != null)
            {
                var songName = props.GetPropertyValue<string>(0);
                var sampleRate = Convert.ToInt32(props.GetPropertyValue<string>(2), CultureInfo.InvariantCulture);
                var bitrate = Convert.ToInt32(props.GetPropertyValue<string>(3), CultureInfo.InvariantCulture);
                var loopCount = props.GetPropertyValue<string>(4) != "Duration" ? props.GetPropertyValue<int>(5) : -1;
                var duration  = props.GetPropertyValue<string>(4) == "Duration" ? props.GetPropertyValue<int>(6) : -1;
                var separateFiles = props.GetPropertyValue<bool>(7);
                var separateIntro = props.GetPropertyValue<bool>(8);
                var stereo = props.GetPropertyValue<bool>(9) && !separateFiles;
                var song = project.GetSong(songName);

                var channelCount = project.GetActiveChannelCount();
                var channelMask = 0;
                var pan = new float[channelCount];
                for (int i = 0; i < channelCount; i++)
                {
                    if (props.GetPropertyValue<bool>(10, i, 0))
                        channelMask |= (1 << i);

                    pan[i] = props.GetPropertyValue<int>(10, i, 2) / 100.0f;
                }

                AudioExportUtils.Save(song, filename, sampleRate, loopCount, duration, channelMask, separateFiles, separateIntro, stereo, pan,
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
        }

        private void ExportVideo(bool pianoRoll)
        {
            var filename = lastExportFilename != null ? lastExportFilename : PlatformUtils.ShowSaveFileDialog("Export Video File", "MP4 Video File (*.mp4)|*.mp4", ref Settings.LastExportFolder);

            if (filename != null)
            {
                var props = dialog.GetPropertyPage(pianoRoll ? (int)ExportFormat.VideoPianoRoll : (int)ExportFormat.VideoOscilloscope);

                var stereoPropIdx   = pianoRoll ? 7 : 9;
                var channelsPropIdx = pianoRoll ? 8 : 10;

                var songName = props.GetPropertyValue<string>(0);
                var resolutionIdx = props.GetSelectedIndex(1);
                var resolutionX = VideoResolution.ResolutionX[resolutionIdx];
                var resolutionY = VideoResolution.ResolutionY[resolutionIdx];
                var halfFrameRate = props.GetSelectedIndex(2) == 1;
                var audioBitRate = Convert.ToInt32(props.GetPropertyValue<string>(3), CultureInfo.InvariantCulture);
                var videoBitRate = Convert.ToInt32(props.GetPropertyValue<string>(4), CultureInfo.InvariantCulture);
                var loopCount = props.GetPropertyValue<int>(5);
                var stereo = props.GetPropertyValue<bool>(stereoPropIdx);
                var selectedChannels = props.GetPropertyValue<bool[]>(channelsPropIdx);
                var song = project.GetSong(songName);
                var channelCount = project.GetActiveChannelCount();
                var channelMask = 0;

                var pan = new float[channelCount];
                for (int i = 0; i < channelCount; i++)
                {
                    if (props.GetPropertyValue<bool>(channelsPropIdx, i, 0))
                        channelMask |= (1 << i);

                    pan[i] = props.GetPropertyValue<int>(channelsPropIdx, i, 2) / 100.0f;
                }

                if (pianoRoll)
                {
                    var pianoRollZoom = props.GetSelectedIndex(6) - 3;

                    new VideoFilePianoRoll().Save(project, song.Id, loopCount, Settings.FFmpegExecutablePath, filename, resolutionX, resolutionY, halfFrameRate, channelMask, audioBitRate, videoBitRate, pianoRollZoom, stereo, pan);
                }
                else
                {
                    var oscNumColumns    = props.GetPropertyValue<int>(6);
                    var oscLineThickness = props.GetPropertyValue<int>(7);
                    var oscColorMode     = props.GetSelectedIndex(8);

                    new VideoFileOscilloscope().Save(project, song.Id, loopCount, oscColorMode, oscNumColumns, oscLineThickness, Settings.FFmpegExecutablePath, filename, resolutionX, resolutionY, halfFrameRate, channelMask, audioBitRate, videoBitRate, stereo, pan);
                }

                lastExportFilename = filename;
            }
        }

        private void ExportNsf()
        {
            var filename = lastExportFilename != null ? lastExportFilename : PlatformUtils.ShowSaveFileDialog("Export NSF File", "Nintendo Sound Files (*.nsf)|*.nsf", ref Settings.LastExportFolder);
            if (filename != null)
            {
                var props  = dialog.GetPropertyPage((int)ExportFormat.Nsf);
                var mode   = MachineType.GetValueForName(props.GetPropertyValue<string>(3));
#if DEBUG
                var kernel = FamiToneKernel.GetValueForName(props.GetPropertyValue<string>(5));
#else
                var kernel = FamiToneKernel.FamiStudio;
#endif

                new NsfFile().Save(project, kernel, filename,
                    GetSongIds(props.GetPropertyValue<bool[]>(4)),
                    props.GetPropertyValue<string>(0),
                    props.GetPropertyValue<string>(1),
                    props.GetPropertyValue<string>(2),
                    mode);

                lastExportFilename = filename;
            }
        }

        private void ExportRom()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.Rom);
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(4));

            if (songIds.Length > RomFileBase.MaxSongs)
            {
                PlatformUtils.MessageBox($"Please select {RomFileBase.MaxSongs} songs or less.", "ROM Export", MessageBoxButtons.OK);
                return;
            }

            if (props.GetPropertyValue<string>(0) == "NES ROM")
            {
                var filename = lastExportFilename != null ? lastExportFilename : PlatformUtils.ShowSaveFileDialog("Export ROM File", "NES ROM (*.nes)|*.nes", ref Settings.LastExportFolder);
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
            }
            else
            {
                var filename = lastExportFilename != null ? null : PlatformUtils.ShowSaveFileDialog("Export Famicom Disk", "FDS Disk (*.fds)|*.fds", ref Settings.LastExportFolder);
                if (filename != null)
                {
                    var fds = new FdsFile();
                    fds.Save(
                        project, filename, songIds,
                        props.GetPropertyValue<string>(1),
                        props.GetPropertyValue<string>(2));

                    lastExportFilename = filename;
                }
            }
        }

        private void ExportMidi()
        {
            var filename = lastExportFilename != null ? lastExportFilename : PlatformUtils.ShowSaveFileDialog("Export MIDI File", "MIDI Files (*.mid)|*.mid", ref Settings.LastExportFolder);
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

                lastExportFilename = filename;
            }
        }

        private void ExportText()
        {
            var filename = lastExportFilename != null ? lastExportFilename : PlatformUtils.ShowSaveFileDialog("Export FamiStudio Text File", "FamiStudio Text Export (*.txt)|*.txt", ref Settings.LastExportFolder);
            if (filename != null)
            {
                var props = dialog.GetPropertyPage((int)ExportFormat.Text);
                var deleteUnusedData = props.GetPropertyValue<bool>(1);
                new FamistudioTextFile().Save(project, filename, GetSongIds(props.GetPropertyValue<bool[]>(0)), deleteUnusedData);
                lastExportFilename = filename;
            }
        }

        private void ExportFamiTracker()
        {
            if (!project.UsesMultipleExpansionAudios)
            {
                var filename = lastExportFilename != null ? lastExportFilename : PlatformUtils.ShowSaveFileDialog("Export FamiTracker Text File", "FamiTracker Text Format (*.txt)|*.txt", ref Settings.LastExportFolder);
                if (filename != null)
                {
                    var props = dialog.GetPropertyPage((int)ExportFormat.FamiTracker);
                    new FamitrackerTextFile().Save(project, filename, GetSongIds(props.GetPropertyValue<bool[]>(0)));
                    lastExportFilename = filename;
                }
            }
        }

        private void ExportFamiTone2Music(bool famiStudio)
        {
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
                var folder = lastExportFilename != null ? lastExportFilename : PlatformUtils.ShowBrowseFolderDialog("Select the export folder", ref Settings.LastExportFolder);

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
                }
            }
            else
            {
                var engineName = famiStudio ? "FamiStudio" : "FamiTone2";
                var filename = lastExportFilename != null ? lastExportFilename : PlatformUtils.ShowSaveFileDialog($"Export {engineName} Assembly Code", $"{engineName} Assembly File (*.{ext})|*.{ext}", ref Settings.LastExportFolder);
                if (filename != null)
                {
                    var includeFilename = generateInclude ? Path.ChangeExtension(filename, null) + "_songlist.inc" : null;

                    Log.LogMessage(LogSeverity.Info, $"Exporting all songs to a single assembly file.");

                    FamitoneMusicFile f = new FamitoneMusicFile(kernel, true);
                    f.Save(project, songIds, exportFormat, false, filename, Path.ChangeExtension(filename, ".dmc"), includeFilename, MachineType.Dual);

                    lastExportFilename = filename;
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

            var filename = lastExportFilename != null ? lastExportFilename : PlatformUtils.ShowSaveFileDialog($"Export {engineName} Code", $"{engineName} Assembly File (*.{ext})|*.{ext}", ref Settings.LastExportFolder);
            if (filename != null)
            {
                var includeFilename = generateInclude ? Path.ChangeExtension(filename, null) + "_sfxlist.inc" : null;

                FamitoneSoundEffectFile f = new FamitoneSoundEffectFile();
                f.Save(project, songIds, exportFormat, mode, famiStudio ? FamiToneKernel.FamiStudio : FamiToneKernel.FamiTone2, filename, includeFilename);
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

        public bool HasAnyPreviousExport => lastExportCrc != 0;

        public bool CanRepeatLastExport(Project project)
        {
            if (project != this.project)
                return false;

            return lastExportCrc == ComputeProjectCrc(project);
        }

        public void Export(FamiStudioForm parentForm, bool repeatLast)
        {
            var dlgLog = new LogProgressDialog(parentForm);
            using (var scopedLog = new ScopedLogOutput(dlgLog, LogSeverity.Info))
            {
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
                }

                if (dlgLog.HasMessages)
                {
                    Log.LogMessage(LogSeverity.Info, "Done!");
                    Log.ReportProgress(1.0f);
                }

                dlgLog.StayModalUntilClosed();
            }
        }

        public void ShowDialog(FamiStudioForm parentForm)
        {
            if (dialog.ShowDialog(parentForm) == DialogResult.OK)
            {
                dialog.Hide();
                Export(parentForm, false);
                lastExportCrc = lastExportFilename != null ? ComputeProjectCrc(project) : 0;
            }
        }
    }
#endif
}
