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
            FamiTracker,
            FamiTone2,
            Max
        };

        Project project;
        MultiPropertyDialog dialog;

        public unsafe ExportDialog(Rectangle mainWinRect, Project project)
        {
            int width  = 450;
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
                var page = dialog.AddPropertyPage(format.ToString(), "Export" + format.ToString());
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
                    page.AddStringList("Mode :", new[] { "NTSC", "PAL", "Dual" }, "NTSC"); // 3
                    page.AddStringListMulti(null, songNames, null); // 4
                    page.AddStringList("Engine :", Enum.GetNames(typeof(FamitoneMusicFile.FamiToneKernel)), Enum.GetNames(typeof(FamitoneMusicFile.FamiToneKernel))[0]); // 5
                    page.SetPropertyEnabled(3, false);
                    break;
                case ExportFormat.Rom:
                    page.AddString("Name :", project.Name.Substring(0, Math.Min(28, project.Name.Length)), 28); // 0
                    page.AddString("Artist :", project.Author.Substring(0, Math.Min(28, project.Author.Length)), 28); // 1
                    page.AddStringListMulti(null, songNames, null); // 2
                    break;
                case ExportFormat.FamiTracker:
                    page.AddStringListMulti(null, songNames, null); // 0
                    break;
                case ExportFormat.FamiTone2:
                    page.AddStringList("Format :", Enum.GetNames(typeof(FamitoneMusicFile.OutputFormat)), Enum.GetNames(typeof(FamitoneMusicFile.OutputFormat))[0]); // 0
                    page.AddBoolean("Separate Files :", false); // 1
                    page.AddString("Song Name Pattern :", "{project}_{song}"); // 2
                    page.AddString("DMC Name Pattern :", "{project}"); // 3
                    page.AddStringListMulti(null, songNames, null); // 4
                    page.SetPropertyEnabled(2, false);
                    page.SetPropertyEnabled(3, false);
                    break;
            }

            page.Build();
            page.PropertyChanged += Page_PropertyChanged;

            return page;
        }

        private void Page_PropertyChanged(PropertyPage props, int idx, object value)
        {
            if (props == dialog.GetPropertyPage((int)ExportFormat.FamiTone2) && idx == 1)
            {
                props.SetPropertyEnabled(2, (bool)value);
                props.SetPropertyEnabled(3, (bool)value);
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
                var props = dialog.GetPropertyPage((int)ExportFormat.Nsf);
                var kernel = (FamitoneMusicFile.FamiToneKernel)Enum.Parse(typeof(FamitoneMusicFile.FamiToneKernel), props.GetPropertyValue<string>(5));

                NsfFile.Save(project, kernel, filename,
                    GetSongIds(props.GetPropertyValue<bool[]>(4)),
                    props.GetPropertyValue<string>(0),
                    props.GetPropertyValue<string>(1),
                    props.GetPropertyValue<string>(2));
            }
        }

        private void ExportRom()
        {
            var filename = PlatformUtils.ShowSaveFileDialog("Export ROM File", "NES ROM (*.nes)|*.nes");
            if (filename != null)
            {
                var props = dialog.GetPropertyPage((int)ExportFormat.Rom);

                RomFile.Save(project, filename,
                    GetSongIds(props.GetPropertyValue<bool[]>(2)),
                    props.GetPropertyValue<string>(0),
                    props.GetPropertyValue<string>(1));
            }
        }

        private void ExportFamiTracker()
        {
            var filename = PlatformUtils.ShowSaveFileDialog("Export FamiTracker Text File", "FamiTracker Text Format (*.txt)|*.txt");
            if (filename != null)
            {
                var props = dialog.GetPropertyPage((int)ExportFormat.FamiTracker);
                FamitrackerFile.Save(project, filename, GetSongIds(props.GetPropertyValue<bool[]>(0)));
            }
        }
        
        private void ExportFamiTone2()
        {
            var props = dialog.GetPropertyPage((int)ExportFormat.FamiTone2);
            var formatString = props.GetPropertyValue<string>(0);
            var ext = formatString == "CA65" ? "s" : "asm";
            var separate = props.GetPropertyValue<bool>(1);
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(4));
            var exportFormat = (FamitoneMusicFile.OutputFormat)Enum.Parse(typeof(FamitoneMusicFile.OutputFormat), formatString);
            var songNamePattern = props.GetPropertyValue<string>(2);
            var dpcmNamePattern = props.GetPropertyValue<string>(3);

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

                        FamitoneMusicFile f = new FamitoneMusicFile(FamitoneMusicFile.FamiToneKernel.FamiTone2);
                        f.Save(project, new int[] { songId }, exportFormat, true, songFilename, dpcmFilename);
                    }
                }
            }
            else
            {
                var filename = PlatformUtils.ShowSaveFileDialog("Export FamiTone2 Code", $"FamiTone2 Assembly File (*.{ext})|*.{ext}");
                if (filename != null)
                {
                    FamitoneMusicFile f = new FamitoneMusicFile(FamitoneMusicFile.FamiToneKernel.FamiTone2);
                    f.Save(project, songIds, exportFormat, false, filename, Path.ChangeExtension(filename, ".dmc"));
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
                    case ExportFormat.FamiTracker: ExportFamiTracker(); break;
                    case ExportFormat.FamiTone2: ExportFamiTone2(); break;
                }
            }
        }
    }
}
