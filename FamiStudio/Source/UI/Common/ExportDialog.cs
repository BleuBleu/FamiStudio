using FamiStudio.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

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
            "WAV / MP3",
            "Video",
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

        Project project;
        MultiPropertyDialog dialog;
        uint lastExportCrc;
        string lastExportFilename;
        int[] midiInstrumentMapping;

        public unsafe ExportDialog(Project project)
        {
            int width  = 600;
            int height = 550;

#if FAMISTUDIO_LINUX 
            height += 80;
#elif FAMISTUDIO_MACOS
            height += 40;
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

        private string[] GetChannelNames()
        {
            var channelTypes = project.GetActiveChannelList();
            var channelNames = new string[channelTypes.Length];
            for (int i = 0; i < channelTypes.Length; i++)
            {
                channelNames[i] = ChannelType.Names[channelTypes[i]];
                if (i >= ChannelType.ExpansionAudioStart)
                    channelNames[i] += $" ({project.ExpansionAudioShortName})";
            }
            return channelNames;
        }

        private bool[] GetDefaultSelectedChannels()
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

            // All true if its an empty project.
            if (!anyChannelActive)
            {
                for (int i = 0; i < channelActives.Length; i++)
                    channelActives[i] = true;
            }

            return channelActives;
        }

        private PropertyPage CreatePropertyPage(PropertyPage page, ExportFormat format)
        {
            var songNames = GetSongNames();

            switch (format)
            {
                case ExportFormat.WavMp3:
                    page.AddDropDownList("Song :", songNames, songNames[0]); // 0
                    page.AddDropDownList("Format :", new[] { "WAV", "MP3" }, "WAV"); // 1
                    page.AddDropDownList("Sample Rate :", new[] { "11025", "22050", "44100", "48000" }, "44100"); // 2
                    page.AddDropDownList("Bit Rate :", new[] { "96", "112", "128", "160", "192", "224", "256" }, "128"); // 3
                    page.AddDropDownList("Mode :", new[] { "Loop N times", "Duration" }, "Loop N times"); // 4
                    page.AddIntegerRange("Loop count:", 1, 1, 10); // 5
                    page.AddIntegerRange("Duration (sec):", 120, 1, 1000); // 6
                    page.AddCheckBox("Separate channel files", false); // 7
                    page.AddCheckBox("Separate intro file", false); // 8
                    page.AddCheckBoxList("Channels :", GetChannelNames(), GetDefaultSelectedChannels()); // 9
                    page.SetPropertyEnabled(3, false);
                    page.SetPropertyEnabled(6, false);
                    page.PropertyChanged += WavMp3_PropertyChanged;
                    break;
                case ExportFormat.Video:
                    page.AddButton("Path To FFmpeg:", Settings.FFmpegExecutablePath, FFmpegPathButtonClicked, "Path to FFmpeg executable. On Windows this is ffmpeg.exe. To download and install ffpmeg, check the link below."); // 0
#if FAMISTUDIO_MACOS
                    // GTK LinkButtons dont work on MacOS, use a button (https://github.com/quodlibet/quodlibet/issues/2306)
                    page.AddButton(" ", "Download FFmpeg here", FFmpegDownloadButtonClicked); // 1
#else
                    page.AddLinkLabel(" ", "Download FFmpeg here", "https://famistudio.org/doc/ffmpeg/"); // 1
#endif
                    page.AddDropDownList("Song :", songNames, songNames[0]); // 2
                    page.AddDropDownList("Resolution :", VideoResolution.Names, VideoResolution.Names[0]); // 3
                    page.AddDropDownList("Frame Rate :", new[] { "50/60 FPS", "25/30 FPS" }, "50/60 FPS"); // 4
                    page.AddDropDownList("Audio Bit Rate (Kb/s) :", new[] { "64", "96", "112", "128", "160", "192", "224", "256" }, "128"); // 5
                    page.AddDropDownList("Video Bit Rate (Kb/s):", new[] { "250", "500", "1000", "2000", "4000", "8000", "10000", "12000", "14000", "16000", "18000", "20000" }, "12000"); // 6
                    page.AddDropDownList("Piano Roll Zoom :", new[] { "12.5%", "25%", "50%", "100%", "200%", "400%", "800%" }, project.UsesFamiTrackerTempo ? "100%" : "25%", "Higher zoom values scrolls faster and shows less far ahead."); // 7
                    page.AddIntegerRange("Loop Count :", 1, 1, 8); // 8
                    page.AddCheckBoxList("Channels :", GetChannelNames(), GetDefaultSelectedChannels()); // 9
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
                    page.SetPropertyEnabled(3, !project.UsesExpansionAudio);
                    break;
                case ExportFormat.Rom:
                    page.AddDropDownList("Type :", new[] { "NES ROM", "FDS Disk" }, project.ExpansionAudio == ExpansionType.Fds ? "FDS Disk" : "NES ROM"); // 0
                    page.AddString("Name :", project.Name.Substring(0, Math.Min(28, project.Name.Length)), 28); // 1
                    page.AddString("Artist :", project.Author.Substring(0, Math.Min(28, project.Author.Length)), 28); // 2
                    page.AddDropDownList("Mode :", new[] { "NTSC", "PAL" }, project.PalMode ? "PAL" : "NTSC"); // 3
                    page.AddCheckBoxList(null, songNames, null); // 2
                    page.SetPropertyEnabled(0,  project.ExpansionAudio == ExpansionType.Fds);
                    page.SetPropertyEnabled(3, !project.UsesExpansionAudio);
                    break;
                case ExportFormat.Midi:
                    page.AddDropDownList("Song :", songNames, songNames[0]); // 0
                    page.AddCheckBox("Export volume as velocity :", true); // 1
                    page.AddCheckBox("Export slide notes as pitch wheel :", true); // 2
                    page.AddIntegerRange("Pitch wheel range :", 24, 1, 24); // 3
                    page.AddDropDownList("Instrument Mode :", MidiExportInstrumentMode.Names, MidiExportInstrumentMode.Names[0]); // 4
                    page.AddLabel(null, "Double-click on a row to change."); // 5
                    page.AddMultiColumnList(new[] { "", "" }, null, MidiInstrumentDoubleClick, null); // 6
                    page.PropertyChanged += Midi_PropertyChanged;
                    break;
                case ExportFormat.Text:
                    page.AddCheckBoxList(null, songNames, null); // 0
                    page.AddCheckBox("Delete unused data :", false); // 1
                    break;
                case ExportFormat.FamiTracker:
                    page.AddCheckBoxList(null, songNames, null); // 0
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

        private void SoundEngine_PropertyChanged(PropertyPage props, int idx, object value)
        {
            if (idx == 1)
            {
                props.SetPropertyEnabled(2, (bool)value);
                props.SetPropertyEnabled(3, (bool)value);
            }
        }

        private void WavMp3_PropertyChanged(PropertyPage props, int idx, object value)
        {
            if (idx == 1)
            {
                props.SetPropertyEnabled(3, (string)value == "MP3");
            }
            else if (idx == 4)
            {
                props.SetPropertyEnabled(5, (string)value != "Duration");
                props.SetPropertyEnabled(6, (string)value == "Duration");
            }
        }

        private void Midi_PropertyChanged(PropertyPage props, int idx, object value)
        {
            if (idx == 4)
            {
                midiInstrumentMapping = null;
                UpdateMidiInstrumentMapping();
            }
        }

        private void UpdateMidiInstrumentMapping()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.Midi);
            var mode  = props.GetSelectedIndex(4);
            var data  = (string[,])null;
            var cols = new[] { "", "MIDI Instrument" };

            if (mode == MidiExportInstrumentMode.Instrument)
            {
                if (midiInstrumentMapping == null)
                    midiInstrumentMapping = new int[project.Instruments.Count];

                data = new string[project.Instruments.Count, 2];
                for (int i = 0; i < project.Instruments.Count; i++)
                {
                    data[i, 0] = project.Instruments[i].Name;
                    data[i, 1] = MidiFileReader.MidiInstrumentNames[midiInstrumentMapping[i]];
                }

                cols[0] = "FamiStudio Instrument";
            }
            else
            {
                var firstSong = project.Songs[0];

                if (midiInstrumentMapping == null)
                    midiInstrumentMapping = new int[firstSong.Channels.Length];

                data = new string[firstSong.Channels.Length, 2];
                for (int i = 0; i < firstSong.Channels.Length; i++)
                {
                    data[i, 0] = firstSong.Channels[i].Name;
                    data[i, 1] = MidiFileReader.MidiInstrumentNames[midiInstrumentMapping[i]];
                }

                cols[0] = "NES Channel";
            }

            props.UpdateMultiColumnList(6, data, cols);
        }

        private void MidiInstrumentDoubleClick(PropertyPage props, int propertyIndex, int itemIndex, int columnIndex)
        {
            Debug.Assert(midiInstrumentMapping != null);

            var dlg = new PropertyDialog(400, true, true, dialog);
            dlg.Properties.AddDropDownList("MIDI Instrument:", MidiFileReader.MidiInstrumentNames, MidiFileReader.MidiInstrumentNames[midiInstrumentMapping[itemIndex]]); // 0
            dlg.Properties.Build();

            if (dlg.ShowDialog(null) == DialogResult.OK)
            {
                midiInstrumentMapping[itemIndex] = dlg.Properties.GetSelectedIndex(0);
                UpdateMidiInstrumentMapping();
            }
        }

        private void FFmpegPathButtonClicked(PropertyPage props, int propertyIndex)
        {
            var dummy = "";
#if FAMISTUDIO_WINDOWS
            var ffmpegExeFilter = "FFmpeg Executable (ffmpeg.exe)|ffmpeg.exe";
#else
            var ffmpegExeFilter = "FFmpeg Executable (ffmpeg)|*.*";
#endif

#if FAMISTUDIO_MACOS
            // dialog.TemporarelyHide(); MATTT
#endif
            string filename = PlatformUtils.ShowOpenFileDialog("Please select FFmpeg executable", ffmpegExeFilter, ref dummy, dialog);
#if FAMISTUDIO_MACOS
            // dialog.TemporarelyShow(); MATTT
#endif

            if (filename != null)
            {
                props.SetPropertyValue(propertyIndex, filename);

                // Update settings right away.
                Settings.FFmpegExecutablePath = filename;
                Settings.Save();
            }
        }

        private void FFmpegDownloadButtonClicked(PropertyPage props, int propertyIndex)
        {
            Utils.OpenUrl("https://famistudio.org/doc/ffmpeg/");
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
            var format = props.GetPropertyValue<string>(1);

            var filename = "";

            if (format == "MP3")
                filename = lastExportFilename != null ? lastExportFilename : PlatformUtils.ShowSaveFileDialog("Export MP3 File", "MP3 Audio File (*.mp3)|*.mp3", ref Settings.LastExportFolder);
            else
                filename = lastExportFilename != null ? lastExportFilename : PlatformUtils.ShowSaveFileDialog("Export Wave File", "Wave Audio File (*.wav)|*.wav", ref Settings.LastExportFolder);

            if (filename != null)
            {
                var songName = props.GetPropertyValue<string>(0);
                var sampleRate = Convert.ToInt32(props.GetPropertyValue<string>(2), CultureInfo.InvariantCulture);
                var bitRate = Convert.ToInt32(props.GetPropertyValue<string>(3), CultureInfo.InvariantCulture);
                var loopCount = props.GetPropertyValue<string>(4) != "Duration" ? props.GetPropertyValue<int>(5) : -1;
                var duration  = props.GetPropertyValue<string>(4) == "Duration" ? props.GetPropertyValue<int>(6) : -1;
                var separateFiles = props.GetPropertyValue<bool>(7);
                var separateIntro = props.GetPropertyValue<bool>(8);
                var selectedChannels = props.GetPropertyValue<bool[]>(9);
                var song = project.GetSong(songName);

                var channelMask = 0;
                for (int i = 0; i < selectedChannels.Length; i++)
                {
                    if (selectedChannels[i])
                        channelMask |= (1 << i);
                }

                WavMp3ExportUtils.Save(song, filename, sampleRate, loopCount, duration, channelMask, separateFiles, separateIntro,
                     (samples, fn) => 
                     {
                         if (format == "MP3")
                             Mp3File.Save(samples, fn, sampleRate, bitRate);
                         else
                             WaveFile.Save(samples, fn, sampleRate);
                     });

                lastExportFilename = filename;
            }
        }

        private void ExportVideo()
        {
            var filename = lastExportFilename != null ? lastExportFilename : PlatformUtils.ShowSaveFileDialog("Export Video File", "MP4 Video File (*.mp4)|*.mp4", ref Settings.LastExportFolder);

            if (filename != null)
            {
                var zoomValues = new[] { "12.5%", "25%", "50%", "100%", "200%", "400%", "800%" };
                var frameRates = new[] { "50/60 FPS", "25/30 FPS" };

                var props = dialog.GetPropertyPage((int)ExportFormat.Video);
                var ffmpeg = props.GetPropertyValue<string>(0);
                var songName = props.GetPropertyValue<string>(2);
                var resolutionIdx = VideoResolution.GetIndexForName(props.GetPropertyValue<string>(3));
                var resolutionX = VideoResolution.ResolutionX[resolutionIdx];
                var resolutionY = VideoResolution.ResolutionY[resolutionIdx];
                var halfFrameRate = Array.IndexOf(frameRates, props.GetPropertyValue<string>(4)) == 1;
                var audioBitRate = Convert.ToInt32(props.GetPropertyValue<string>(5), CultureInfo.InvariantCulture);
                var videoBitRate = Convert.ToInt32(props.GetPropertyValue<string>(6), CultureInfo.InvariantCulture);
                var pianoRollZoom = Array.IndexOf(zoomValues, props.GetPropertyValue<string>(7)) - 3;
                var loopCount = props.GetPropertyValue<int>(8);
                var selectedChannels = props.GetPropertyValue<bool[]>(9);
                var song = project.GetSong(songName);

                var channelMask = 0;
                for (int i = 0; i < selectedChannels.Length; i++)
                {
                    if (selectedChannels[i])
                        channelMask |= (1 << i);
                }

                new VideoFile().Save(project, song.Id, loopCount, ffmpeg, filename, resolutionX, resolutionY, halfFrameRate, channelMask, audioBitRate, videoBitRate, pianoRollZoom);

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

                new MidiFileWriter().Save(project, filename, project.GetSong(songName).Id, instrumentMode, midiInstrumentMapping, velocity, slideNotes, pitchRange); 

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
            var filename = lastExportFilename != null ? lastExportFilename : PlatformUtils.ShowSaveFileDialog("Export FamiTracker Text File", "FamiTracker Text Format (*.txt)|*.txt", ref Settings.LastExportFolder);
            if (filename != null)
            {
                var props = dialog.GetPropertyPage((int)ExportFormat.FamiTracker);
                new FamitrackerTextFile().Save(project, filename, GetSongIds(props.GetPropertyValue<bool[]>(0)));
                filename = lastExportFilename;
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
            uint crc = CRC32.Compute(project.ExpansionAudio);

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
}
