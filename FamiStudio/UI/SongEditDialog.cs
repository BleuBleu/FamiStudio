using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class SongEditDialog : Form
    {
        Song song;

        public unsafe SongEditDialog(Song song)
        {
            this.song = song;

            InitializeComponent();
            
            textBox1.Text = song.Name;
            textBox1.Font = new Font(Theme.PrivateFontCollection.Families[0], 10.0f, FontStyle.Regular);
            textBox1.BackColor = song.Color;
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.DeselectAll();

            upDownTempo.Font = textBox1.Font;
            upDownSpeed.Font = textBox1.Font;
            upDownPatternLen.Font = textBox1.Font;
            upDownSongLen.Font = textBox1.Font;
            upDownBarLen.Font = textBox1.Font;

            labelTempo.Font = textBox1.Font;
            labelSpeed.Font = textBox1.Font;
            labelPatternLen.Font = textBox1.Font;
            labelSongLen.Font = textBox1.Font;
            labelBarLen.Font = textBox1.Font;

            upDownTempo.Value = song.Tempo;
            upDownSpeed.Value = song.Speed;
            upDownPatternLen.Value = song.PatternLength;
            upDownSongLen.Value = song.Length;
            GenerateBarLengths();
            upDownBarLen.SelectedItem = song.BarLength;

            var bmp = new Bitmap(Theme.CustomColors.GetLength(0), Theme.CustomColors.GetLength(1));
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            byte* ptr = (byte*)data.Scan0.ToPointer();

            for (int j = 0; j < bmp.Height; j++)
            {
                for (int i = 0; i < bmp.Width; i++)
                {
                    var color = Theme.CustomColors[i, j];

                    ptr[i * 4 + 0] = color.B;
                    ptr[i * 4 + 1] = color.G;
                    ptr[i * 4 + 2] = color.R;
                    ptr[i * 4 + 3] = 255;
                }

                ptr += data.Stride;
            }

            bmp.UnlockBits(data);

            pictureBox1.Image = bmp;
            DialogResult = DialogResult.Cancel;
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

        public string NewName => textBox1.Text;
        public Color NewColor => textBox1.BackColor;
        public int NewTempo => (int)upDownTempo.Value;
        public int NewSpeed => (int)upDownSpeed.Value;
        public int NewPatternLength => (int)upDownPatternLen.Value;
        public int NewSongLength => (int)upDownSongLen.Value;
        public int NewBarLength => (int)upDownBarLen.SelectedItem;

        private void GenerateBarLengths()
        {
            upDownBarLen.Items.Clear();

            int patternLen = (int)upDownPatternLen.Value;

            for (int i = patternLen; i >= 2; i--)
            {
                if (patternLen % i == 0)
                {
                    upDownBarLen.Items.Add(i);
                }
            }
        }

        private void ChangeColor(int x, int y)
        {
            int i = Math.Min(Theme.CustomColors.GetLength(0) - 1, Math.Max(0, (int)(x / (float)pictureBox1.Width  * Theme.CustomColors.GetLength(0))));
            int j = Math.Min(Theme.CustomColors.GetLength(1) - 1, Math.Max(0, (int)(y / (float)pictureBox1.Height * Theme.CustomColors.GetLength(1))));

            textBox1.BackColor = Theme.CustomColors[i, j];
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ChangeColor(e.X, e.Y);
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ChangeColor(e.X, e.Y);
        }

        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ChangeColor(e.X, e.Y);

            DialogResult = DialogResult.OK;
            Close();
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

        private void upDownPatternLen_ValueChanged(object sender, EventArgs e)
        {
            GenerateBarLengths();

            upDownBarLen.Text = " "; // Workaround refresh bug.
            if (!upDownBarLen.Items.Contains(song.BarLength))
                upDownBarLen.SelectedIndex = upDownBarLen.Items.Count - 1;
            else
                upDownBarLen.SelectedItem = song.BarLength;
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
    }
}
