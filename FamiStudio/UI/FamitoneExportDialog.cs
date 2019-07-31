using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class FamitoneExportDialog : Form
    {
        private Project project;

        public unsafe FamitoneExportDialog(Project project)
        {
            InitializeComponent();

            this.project = project;

            checkedListSongs.Font = new Font(Theme.PrivateFontCollection.Families[0], 10.0f, FontStyle.Regular);
            comboBoxFormat.Font = checkedListSongs.Font;
            labelFormat.Font = checkedListSongs.Font;
            labelSeparate.Font = checkedListSongs.Font;
            labelName.Font = checkedListSongs.Font;
            checkSeperate.Font = checkedListSongs.Font;
            comboBoxFormat.SelectedIndex = 0;

            textBoxName.Font = checkedListSongs.Font;
            textBoxName.SelectionStart = textBoxName.Text.Length;
            textBoxName.DeselectAll();

            foreach (var song in project.Songs)
            {
                checkedListSongs.Items.Add(song, true);
            }

            DialogResult = DialogResult.Cancel;
        }

        public FamitoneMusicFile.OutputFormat ExportFormat
        {
            get { return (FamitoneMusicFile.OutputFormat)comboBoxFormat.SelectedIndex; }
        }

        public string NamePattern
        {
            get { return textBoxName.Text; }
    }

        public bool SeparateFiles
        {
            get { return checkSeperate.Checked; }
        }

        public int[] SelectedSongIds
        {
            get
            {
                var selectedSongIds = new List<int>();
                foreach (var item in checkedListSongs.CheckedItems)
                    selectedSongIds.Add((item as Song).Id);
                return selectedSongIds.ToArray();
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

        private void buttonYes_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonNo_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void checkSeperate_CheckedChanged(object sender, EventArgs e)
        {
            textBoxName.Enabled = checkSeperate.Checked;
        }
    }
}
