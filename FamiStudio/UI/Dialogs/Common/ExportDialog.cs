using FamiStudio.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FamiStudio
{
    public class ExportDialog
    {
        enum ExportFormat
        {
            Wav,
            Nsf,
            Rom,
            Text,
            FamiTracker,
            FamiTone2Music,
            FamiTone2Sfx,
            Max
        };

        string[] ExportFormatNames =
        {
            "Wave",
            "NSF",
            "ROM / FDS",
            "FamiStudio Text",
            "FamiTracker Text",
            "FamiStudio Music ASM",
            "FamiStudio SFX ASM",
            ""
        };

        string[] ExportIcons =
        {
            "ExportWav",
            "ExportNsf",
            "ExportRom",
            "ExportText",
            "ExportFamiTracker",
            "ExportFamiTone2",
            "ExportFamiTone2"
        };

        Project project;
        MultiPropertyDialog dialog;

        public unsafe ExportDialog(Rectangle mainWinRect, Project project)
        {
            int width  = 550;
            int height = 450;
            int x = mainWinRect.Left + (mainWinRect.Width  - width)  / 2;
            int y = mainWinRect.Top  + (mainWinRect.Height - height) / 2;

#if FAMISTUDIO_LINUX
            height += 30;
#endif

            this.dialog = new MultiPropertyDialog(x, y, width, height, 200);
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
                channelNames[i] = Channel.ChannelNames[channelTypes[i]];
                if (i >= Channel.ExpansionAudioStart)
                    channelNames[i] += $" ({project.ExpansionAudioShortName})";
            }
            return channelNames;
        }

        private PropertyPage CreatePropertyPage(PropertyPage page, ExportFormat format)
        {
            var songNames = GetSongNames();

            switch (format)
            {
                case ExportFormat.Wav:
                    page.AddStringList("Song :", songNames, songNames[0]); // 0
                    page.AddStringList("Sample Rate :", new[] { "11025", "22050", "44100", "48000" }, "44100"); // 1
                    page.AddStringList("Mode :", new[] { "Loop N times", "Duration" }, "Loop N times"); // 2
                    page.AddIntegerRange("Loop count:", 1, 1, 10); // 3
                    page.AddIntegerRange("Duration (sec):", 120, 1, 1000); // 4
                    page.AddStringListMulti("Channels :", GetChannelNames(), null); // 5
                    page.SetPropertyEnabled(4, false);
                    break;
                case ExportFormat.Nsf:
                    page.AddString("Name :", project.Name, 31); // 0
                    page.AddString("Artist :", project.Author, 31); // 1
                    page.AddString("Copyright :", project.Copyright, 31); // 2
                    page.AddStringList("Mode :", Enum.GetNames(typeof(MachineType)), Enum.GetNames(typeof(MachineType))[project.PalMode ? 1 : 0]); // 3
                    page.AddStringListMulti(null, songNames, null); // 4
#if DEBUG
                    page.AddStringList("Engine :", Enum.GetNames(typeof(FamitoneMusicFile.FamiToneKernel)), Enum.GetNames(typeof(FamitoneMusicFile.FamiToneKernel))[0]); // 5
#endif
                    page.SetPropertyEnabled(3, !project.UsesExpansionAudio);
                    break;
                case ExportFormat.Rom:
                    page.AddStringList("Type :", new[] { "NES ROM", "FDS Disk" }, project.ExpansionAudio == Project.ExpansionFds ? "FDS Disk" : "NES ROM"); // 0
                    page.AddString("Name :", project.Name.Substring(0, Math.Min(28, project.Name.Length)), 28); // 1
                    page.AddString("Artist :", project.Author.Substring(0, Math.Min(28, project.Author.Length)), 28); // 2
                    page.AddStringList("Mode :", new[] { "NTSC", "PAL" }, project.PalMode ? "PAL" : "NTSC"); // 3
                    page.AddStringListMulti(null, songNames, null); // 2
                    page.SetPropertyEnabled(0,  project.ExpansionAudio == Project.ExpansionFds);
                    page.SetPropertyEnabled(3, !project.UsesExpansionAudio);
                    break;
                case ExportFormat.Text:
                    page.AddStringListMulti(null, songNames, null); // 0
                    break;
                case ExportFormat.FamiTracker:
                    page.AddStringListMulti(null, songNames, null); // 0
                    break;
                case ExportFormat.FamiTone2Music:
                    page.AddStringList("Variant :", Enum.GetNames(typeof(FamitoneMusicFile.FamiToneKernel)), Enum.GetNames(typeof(FamitoneMusicFile.FamiToneKernel))[0]); // 0
                    page.AddStringList("Format :", Enum.GetNames(typeof(AssemblyFormat)), Enum.GetNames(typeof(AssemblyFormat))[0]); // 1
                    page.AddBoolean("Separate Files :", false); // 2
                    page.AddString("Song Name Pattern :", "{project}_{song}"); // 3
                    page.AddString("DMC Name Pattern :", "{project}"); // 4
                    page.AddStringListMulti(null, songNames, null); // 5
                    page.SetPropertyEnabled(3, false);
                    page.SetPropertyEnabled(4, false);
                    break;
                case ExportFormat.FamiTone2Sfx:
                    page.AddStringList("Format :", Enum.GetNames(typeof(AssemblyFormat)), Enum.GetNames(typeof(AssemblyFormat))[0]); // 0
                    page.AddStringList("Mode :", Enum.GetNames(typeof(MachineType)), Enum.GetNames(typeof(MachineType))[project.PalMode ? 1 : 0]); // 1
                    page.AddStringListMulti(null, songNames, null); // 2
                    break;
            }

            page.Build();
            page.PropertyChanged += Page_PropertyChanged;

            return page;
        }

        private void Page_PropertyChanged(PropertyPage props, int idx, object value)
        {
            if (props == dialog.GetPropertyPage((int)ExportFormat.FamiTone2Music) && idx == 2)
            {
                props.SetPropertyEnabled(3, (bool)value);
                props.SetPropertyEnabled(4, (bool)value);
            }
            else if (props == dialog.GetPropertyPage((int)ExportFormat.Wav) && idx == 2)
            {
                props.SetPropertyEnabled(3, (string)value != "Duration");
                props.SetPropertyEnabled(4, (string)value == "Duration");
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

        private void ExportWav()
        {
            var filename = PlatformUtils.ShowSaveFileDialog("Export Wave File", "Wave Audio File (*.wav)|*.wav");
            if (filename != null)
            {
                var props = dialog.GetPropertyPage((int)ExportFormat.Wav);
                var songName = props.GetPropertyValue<string>(0);
                var sampleRate = Convert.ToInt32(props.GetPropertyValue<string>(1));
                var loopCount = props.GetPropertyValue<string>(2) != "Duration" ? props.GetPropertyValue<int>(3) : -1;
                var duration  = props.GetPropertyValue<string>(2) == "Duration" ? props.GetPropertyValue<int>(4) : -1;
                var selectedChannels = props.GetPropertyValue<bool[]>(5);

                var channelMask = 0;
                for (int i = 0; i < selectedChannels.Length; i++)
                {
                    if (selectedChannels[i])
                        channelMask |= (1 << i);
                }

                WaveFile.Save(project.GetSong(songName), filename, sampleRate, loopCount, duration, channelMask);
            }
        }

        private void ExportNsf()
        {
            var filename = PlatformUtils.ShowSaveFileDialog("Export NSF File", "Nintendo Sound Files (*.nsf)|*.nsf");
            if (filename != null)
            {
                var props  = dialog.GetPropertyPage((int)ExportFormat.Nsf);
                var mode   = (MachineType)Enum.Parse(typeof(MachineType), props.GetPropertyValue<string>(3));
#if DEBUG
                var kernel = (FamitoneMusicFile.FamiToneKernel)Enum.Parse(typeof(FamitoneMusicFile.FamiToneKernel), props.GetPropertyValue<string>(5));
#else
                var kernel = FamitoneMusicFile.FamiToneKernel.FamiStudio;
#endif

                new NsfFile().Save(project, kernel, filename,
                    GetSongIds(props.GetPropertyValue<bool[]>(4)),
                    props.GetPropertyValue<string>(0),
                    props.GetPropertyValue<string>(1),
                    props.GetPropertyValue<string>(2),
                    mode);
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
                var filename = PlatformUtils.ShowSaveFileDialog("Export ROM File", "NES ROM (*.nes)|*.nes");
                if (filename != null)
                {
                    var rom = new RomFile();
                    rom.Save(
                        project, filename, songIds,
                        props.GetPropertyValue<string>(1),
                        props.GetPropertyValue<string>(2),
                        props.GetPropertyValue<string>(3) == "PAL");
                }
            }
            else
            {
                var filename = PlatformUtils.ShowSaveFileDialog("Export Famicom Disk", "FDS Disk (*.fds)|*.fds");
                if (filename != null)
                {
                    var fds = new FdsFile();
                    fds.Save(
                        project, filename, songIds,
                        props.GetPropertyValue<string>(1),
                        props.GetPropertyValue<string>(2));
                }
            }
        }

        private void ExportText()
        {
            var filename = PlatformUtils.ShowSaveFileDialog("Export FamiStudio Text File", "FamiStudio Text Export (*.txt)|*.txt");
            if (filename != null)
            {
                var props = dialog.GetPropertyPage((int)ExportFormat.Text);
                new FamistudioTextFile().Save(project, filename, GetSongIds(props.GetPropertyValue<bool[]>(0)));
            }
        }

        private void ExportFamiTracker()
        {
            var filename = PlatformUtils.ShowSaveFileDialog("Export FamiTracker Text File", "FamiTracker Text Format (*.txt)|*.txt");
            if (filename != null)
            {
                var props = dialog.GetPropertyPage((int)ExportFormat.FamiTracker);
                new FamitrackerTextFile().Save(project, filename, GetSongIds(props.GetPropertyValue<bool[]>(0)));
            }
        }

        private void ExportFamiTone2Music()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.FamiTone2Music);
            var kernelString = props.GetPropertyValue<string>(0);
            var separate = props.GetPropertyValue<bool>(2);
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(5));
            var kernel = (FamitoneMusicFile.FamiToneKernel)Enum.Parse(typeof(FamitoneMusicFile.FamiToneKernel), kernelString);
            var exportFormat = (AssemblyFormat)Enum.Parse(typeof(AssemblyFormat), props.GetPropertyValue<string>(1));
            var ext = exportFormat == AssemblyFormat.CA65 ? "s" : "asm";
            var songNamePattern = props.GetPropertyValue<string>(3);
            var dpcmNamePattern = props.GetPropertyValue<string>(4);

            if (separate)
            {
                var folderBrowserDialog = new FolderBrowserDialog();
                folderBrowserDialog.Description = "Select the export folder";

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (var songId in songIds)
                    {
                        var song = project.GetSong(songId);
                        var formattedSongName = songNamePattern.Replace("{project}", project.Name).Replace("{song}", song.Name);
                        var formattedDpcmName = dpcmNamePattern.Replace("{project}", project.Name).Replace("{song}", song.Name);
                        var songFilename = Path.Combine(folderBrowserDialog.SelectedPath, Utils.MakeNiceAsmName(formattedSongName) + "." + ext);
                        var dpcmFilename = Path.Combine(folderBrowserDialog.SelectedPath, Utils.MakeNiceAsmName(formattedDpcmName) + ".dmc");

                        FamitoneMusicFile f = new FamitoneMusicFile(kernel);
                        f.Save(project, new int[] { songId }, exportFormat, true, songFilename, dpcmFilename, MachineType.Dual);
                    }
                }
            }
            else
            {
                var filename = PlatformUtils.ShowSaveFileDialog("Export FamiTone2 Code", $"FamiTone2 Assembly File (*.{ext})|*.{ext}");
                if (filename != null)
                {
                    FamitoneMusicFile f = new FamitoneMusicFile(kernel);
                    f.Save(project, songIds, exportFormat, false, filename, Path.ChangeExtension(filename, ".dmc"), MachineType.Dual);
                }
            }
        }

        private void ExportFamiTone2Sfx()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.FamiTone2Sfx);
            var exportFormat = (AssemblyFormat)Enum.Parse(typeof(AssemblyFormat), props.GetPropertyValue<string>(0));
            var ext = exportFormat == AssemblyFormat.CA65 ? "s" : "asm";
            var mode = (MachineType)Enum.Parse(typeof(MachineType), props.GetPropertyValue<string>(1));
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(2));

            var filename = PlatformUtils.ShowSaveFileDialog("Export FamiTone2 Code", $"FamiTone2 Assembly File (*.{ext})|*.{ext}");
            if (filename != null)
            {
                FamitoneSoundEffectFile f = new FamitoneSoundEffectFile();
                f.Save(project, songIds, exportFormat, mode, filename);
            }
        }

        public void ShowDialog()
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var selectedFormat = (ExportFormat)dialog.SelectedIndex;

                switch (selectedFormat)
                {
                    case ExportFormat.Wav: ExportWav(); break;
                    case ExportFormat.Nsf: ExportNsf(); break;
                    case ExportFormat.Rom: ExportRom(); break;
                    case ExportFormat.Text: ExportText(); break;
                    case ExportFormat.FamiTracker: ExportFamiTracker(); break;
                    case ExportFormat.FamiTone2Music: ExportFamiTone2Music(); break;
                    case ExportFormat.FamiTone2Sfx: ExportFamiTone2Sfx(); break;
                }
            }
        }
    }
}
