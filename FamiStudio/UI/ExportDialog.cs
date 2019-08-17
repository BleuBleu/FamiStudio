using FamiStudio.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Resources;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class ExportDialog : Form
    {
        enum ExportFormat
        {
            Wav,
            Nsf,
            FamiTracker,
            FamiTone2,
            Max
        };

        ExportFormat selectedFormat = ExportFormat.Wav;
        Button[] formatButtons = new Button[(int)ExportFormat.Max];
        PropertyPage[] formatProps = new PropertyPage[(int)ExportFormat.Max];

        Font font;
        Font fontBold;

        Project project;

        public unsafe ExportDialog(Project project)
        {
            InitializeComponent();

            this.project = project;
            this.font = new Font(Theme.PrivateFontCollection.Families[0], 10.0f, FontStyle.Regular);
            this.fontBold = new Font(Theme.PrivateFontCollection.Families[0], 10.0f, FontStyle.Bold);

            for (int i = 0; i < (int)ExportFormat.Max; i++)
            {
                var format = (ExportFormat)i;

                formatButtons[i] = CreateButton(format);
                formatProps[i] = CreatePropertyPage(format);
            }
        }

        private void Btn_Click(object sender, EventArgs e)
        {
            int idx = Array.IndexOf(formatButtons, sender as Button);

            for (int i = 0; i < (int)ExportFormat.Max; i++)
            {
                formatButtons[i].Font = i == idx ? fontBold : font;
                formatProps[i].Visible = i == idx;
            }

            selectedFormat = (ExportFormat)idx;
        }

        private string[] GetSongNames()
        {
            var names = new string[project.Songs.Count];
            for (var i = 0; i < project.Songs.Count; i++)
                names[i] = project.Songs[i].Name;
            return names;
        }

        private Button CreateButton(ExportFormat format)
        {
            var btn = new NoFocusButton();

            btn.BackColor = BackColor;
            btn.ForeColor = Direct2DGraphics.ToDrawingColor4(Theme.LightGreyFillColor2);
            btn.ImageAlign = ContentAlignment.MiddleLeft;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Direct2DGraphics.ToDrawingColor4(Theme.DarkGreyFillColor2);
            btn.FlatAppearance.MouseDownBackColor = Direct2DGraphics.ToDrawingColor4(Theme.DarkGreyFillColor2);
            btn.Image = (Bitmap)Resources.ResourceManager.GetObject("Export" + format.ToString(), Resources.Culture);
            btn.Top = ((int)format) * 32;
            btn.Left = 0;
            btn.Width = panelTabs.Width;
            btn.Height = 32;
            btn.Font = format == 0 ? fontBold : font;
            btn.Text = format.ToString();
            btn.TextImageRelation = TextImageRelation.ImageBeforeText;
            btn.Click += Btn_Click;

            panelTabs.Controls.Add(btn);

            return btn;
        }

        private PropertyPage CreatePropertyPage(ExportFormat format)
        {
            PropertyPage page = new PropertyPage();

            page.Dock = DockStyle.Fill;
            panelProps.Controls.Add(page);

            var songName = GetSongNames();

            switch (format)
            {
                case ExportFormat.Wav:
                    page.AddStringList("Song :", songName, songName[0]); // 0
                    page.AddStringList("Sample Rate :", new[] { "11025", "22050", "44100", "48000" }, "44100"); // 1
                    break;
                case ExportFormat.Nsf:
                    page.AddString("Name :", project.Name, 31); // 0
                    page.AddString("Artist :", project.Author, 31); // 1
                    page.AddString("Copyright :", project.Copyright, 31); // 2
                    page.AddStringList("Mode :", new[] { "NTSC", "PAL", "Dual" }, "NTSC"); // 3
                    page.AddStringListMulti(null, songName, null); // 4
                    page.SetPropertyEnabled(3, false);
                    break;
                case ExportFormat.FamiTracker:
                    page.AddStringListMulti(null, songName, null); // 0
                    break;
                case ExportFormat.FamiTone2:
                    page.AddStringList("Format :", new[] { "NESASM", "CA65", "ASM6" }, "NESASM"); // 0
                    page.AddBoolean("Separate Files :", false); // 1
                    page.AddString("Song Name Pattern :", "{project}_{song}"); // 2
                    page.AddString("DMC Name Pattern :", "{project}"); // 3
                    page.AddStringListMulti(null, songName, null); // 4
                    page.SetPropertyEnabled(2, false);
                    page.SetPropertyEnabled(3, false);
                    break;
            }

            page.Build();
            page.PropertyChanged += Page_PropertyChanged;
            page.Visible = format == 0;

            return page;
        }

        private void Page_PropertyChanged(PropertyPage props, int idx, object value)
        {
            if (props == formatProps[(int)ExportFormat.FamiTone2] && idx == 1)
            {
                props.SetPropertyEnabled(2, (bool)value);
                props.SetPropertyEnabled(3, (bool)value);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var p = base.CreateParams;
                p.ExStyle |= 0x2000000; // WS_EX_COMPOSITED
                return p;
            }
        }

        private void RenameColorDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
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
            var sfd = new SaveFileDialog()
            {
                Filter = "Wave Audio File (*.wav)|*.wav",
                Title = "Export Wave File"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var props = formatProps[(int)ExportFormat.Wav];
                var songName = props.GetPropertyValue<string>(0);
                var sampleRate = Convert.ToInt32(props.GetPropertyValue<string>(1));

                WaveFile.Save(project.GetSong(songName), sfd.FileName, sampleRate);
            }
        }

        private void ExportNsf()
        {
            var sfd = new SaveFileDialog()
            {
                Filter = "Nintendo Sound Files (*.nsf)|*.nsf",
                Title = "Export NSF File"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var props = formatProps[(int)ExportFormat.Nsf];

                NsfFile.Save(project, sfd.FileName,
                    GetSongIds(props.GetPropertyValue<bool[]>(4)),
                    props.GetPropertyValue<string>(0),
                    props.GetPropertyValue<string>(1),
                    props.GetPropertyValue<string>(2));
            }
        }

        private void ExportFamiTracker()
        {
            var sfd = new SaveFileDialog()
            {
                Filter = "FamiTracker Text Format (*.txt)|*.txt",
                Title = "Export FamiTracker Text File"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var props = formatProps[(int)ExportFormat.FamiTracker];
                FamitrackerFile.Save(project, sfd.FileName, GetSongIds(props.GetPropertyValue<bool[]>(0)));
            }
        }
        
        private void ExportFamiTone2()
        {
            var props = formatProps[(int)ExportFormat.FamiTone2];
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

                        FamitoneMusicFile f = new FamitoneMusicFile();
                        f.Save(project, new int[] { songId }, exportFormat, true, songFilename, dpcmFilename);
                    }
                }
            }
            else
            {
                var sfd = new SaveFileDialog()
                {
                    Filter = $"FamiTone2 Assembly File (*.{ext})|*.{ext}",
                    Title = "Export FamiTone2 Code"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    FamitoneMusicFile f = new FamitoneMusicFile();
                    f.Save(project, songIds, exportFormat, false, sfd.FileName, Path.ChangeExtension(sfd.FileName, ".dmc"));
                }
            }
        }

        private void buttonYes_Click(object sender, EventArgs e)
        {
            switch (selectedFormat)
            {
                case ExportFormat.Wav: ExportWav(); break;
                case ExportFormat.Nsf: ExportNsf(); break;
                case ExportFormat.FamiTracker: ExportFamiTracker(); break;
                case ExportFormat.FamiTone2: ExportFamiTone2(); break;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonNo_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
