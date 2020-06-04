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
            FamiTone2,
            Max
        };

        string[] ExportFormatNames =
        {
            "Wave",
            "NSF",
            "ROM",
            "FamiStudio Text",
            "FamiTracker Text",
            "FamiTone2",
            ""
        };

        Project project;
        MultiPropertyDialog dialog;

        public unsafe ExportDialog(Rectangle mainWinRect, Project project)
        {
            int width  = 500;
            int height = 450;
            int x = mainWinRect.Left + (mainWinRect.Width  - width)  / 2;
            int y = mainWinRect.Top  + (mainWinRect.Height - height) / 2;

#if FAMISTUDIO_LINUX
            height += 30;
#endif

            this.dialog = new MultiPropertyDialog(x, y, width, height);
            this.project = project;
      
            for (int i = 0; i < (int)ExportFormat.Max; i++)
            {
                var format = (ExportFormat)i;
                var page = dialog.AddPropertyPage(ExportFormatNames[i], "Export" + format.ToString());
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

        private PropertyPage CreatePropertyPage(PropertyPage page, ExportFormat format)
        {
            var songNames = GetSongNames();

            switch (format)
            {
                case ExportFormat.Wav:
                    page.AddStringList("Song :", songNames, songNames[0]); // 0
                    page.AddStringList("Sample Rate :", new[] { "11025", "22050", "44100", "48000" }, "44100"); // 1
                    break;
                case ExportFormat.Nsf:
                    page.AddString("Name :", project.Name, 31); // 0
                    page.AddString("Artist :", project.Author, 31); // 1
                    page.AddString("Copyright :", project.Copyright, 31); // 2
                    page.AddStringList("Mode :", Enum.GetNames(typeof(MachineType)), Enum.GetNames(typeof(MachineType))[0]); // 3
                    page.AddStringListMulti(null, songNames, null); // 4
#if DEBUG
                    page.AddStringList("Engine :", Enum.GetNames(typeof(FamitoneMusicFile.FamiToneKernel)), Enum.GetNames(typeof(FamitoneMusicFile.FamiToneKernel))[0]); // 5
#endif
                    page.SetPropertyEnabled(3, !project.UsesExpansionAudio);
                    break;
                case ExportFormat.Rom:
                    page.AddString("Name :", project.Name.Substring(0, Math.Min(28, project.Name.Length)), 28); // 0
                    page.AddString("Artist :", project.Author.Substring(0, Math.Min(28, project.Author.Length)), 28); // 1
                    page.AddStringListMulti(null, songNames, null); // 2
                    break;
                case ExportFormat.Text:
                    page.AddStringListMulti(null, songNames, null); // 0
                    break;
                case ExportFormat.FamiTracker:
                    page.AddStringListMulti(null, songNames, null); // 0
                    break;
                case ExportFormat.FamiTone2:
                    page.AddStringList("Type :", new[] { "Music", "Sound Effects" }, "Music" ); // 0
                    page.AddStringList("Variant :", Enum.GetNames(typeof(FamitoneMusicFile.FamiToneKernel)), Enum.GetNames(typeof(FamitoneMusicFile.FamiToneKernel))[0]); // 1
                    page.AddStringList("Format :", Enum.GetNames(typeof(AssemblyFormat)), Enum.GetNames(typeof(AssemblyFormat))[0]); // 2
                    page.AddBoolean("Separate Files :", false); // 3
                    page.AddString("Song Name Pattern :", "{project}_{song}"); // 4
                    page.AddString("DMC Name Pattern :", "{project}"); // 5
                    page.AddStringListMulti(null, songNames, null); // 6
                    page.SetPropertyEnabled(4, false);
                    page.SetPropertyEnabled(5, false);
                    break;
            }

            page.Build();
            page.PropertyChanged += Page_PropertyChanged;

            return page;
        }

        private void Page_PropertyChanged(PropertyPage props, int idx, object value)
        {
            if (props == dialog.GetPropertyPage((int)ExportFormat.FamiTone2) && (idx == 0 || idx == 3))
            {
                var music    = props.GetPropertyValue<string>(0) == "Music";
                var separate = props.GetPropertyValue<bool>(3);

                props.SetPropertyEnabled(1, music);
                props.SetPropertyEnabled(3, music);
                props.SetPropertyEnabled(4, music && separate);
                props.SetPropertyEnabled(5, music && separate);
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

                WaveFile.Save(project.GetSong(songName), filename, sampleRate);
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
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(2));

            if (songIds.Length > RomFile.MaxSongs)
            {
                PlatformUtils.MessageBox("Please select 8 songs or less.", "ROM Export", MessageBoxButtons.OK);
                return;
            }

            var filename = PlatformUtils.ShowSaveFileDialog("Export ROM File", "NES ROM (*.nes)|*.nes");
            if (filename != null)
            {
                RomFile.Save(project, filename,
                    songIds,
                    props.GetPropertyValue<string>(0),
                    props.GetPropertyValue<string>(1));
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
        
        private void ExportFamiTone2()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.FamiTone2);
            var music = props.GetPropertyValue<string>(0) == "Music";
            var kernelString = props.GetPropertyValue<string>(1);
            var formatString = props.GetPropertyValue<string>(2);
            var ext = formatString == "CA65" ? "s" : "asm";
            var separate = props.GetPropertyValue<bool>(3);
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(6));
            var kernel = (FamitoneMusicFile.FamiToneKernel)Enum.Parse(typeof(FamitoneMusicFile.FamiToneKernel), kernelString);
            var exportFormat = (AssemblyFormat)Enum.Parse(typeof(AssemblyFormat), formatString);
            var songNamePattern = props.GetPropertyValue<string>(4);
            var dpcmNamePattern = props.GetPropertyValue<string>(5);

            if (music)
            {
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
            else
            {
                var filename = PlatformUtils.ShowSaveFileDialog("Export FamiTone2 Code", $"FamiTone2 Assembly File (*.{ext})|*.{ext}");
                if (filename != null)
                {
                    FamitoneSoundEffectFile f = new FamitoneSoundEffectFile();
                    f.Save(project, songIds, exportFormat, filename);
                }
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
                    case ExportFormat.FamiTone2: ExportFamiTone2(); break;
                }
            }
        }
    }
}
