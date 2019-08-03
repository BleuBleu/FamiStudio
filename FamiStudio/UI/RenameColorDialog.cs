using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class RenameColorDialog : Form
    {
        public unsafe RenameColorDialog(string name, Color selectedColor, bool allowNameEdit = true)
        {
            InitializeComponent();

            textBox1.Text = allowNameEdit ? name : "";
            textBox1.Font = new Font(Theme.PrivateFontCollection.Families[0], 10.0f, FontStyle.Regular);
            textBox1.BackColor = selectedColor;
            textBox1.Enabled = allowNameEdit;
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.DeselectAll();

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
