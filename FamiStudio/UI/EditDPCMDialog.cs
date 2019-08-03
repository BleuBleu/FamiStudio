using System;
using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class EditDPCMDialog : Form
    {
        public unsafe EditDPCMDialog(string name, int pitch, bool loop)
        {
            InitializeComponent();

            textBox1.Text = name;
            textBox1.Font = new Font(Theme.PrivateFontCollection.Families[0], 10.0f, FontStyle.Regular);
            textBox1.BackColor = Color.FromArgb(198, 205, 218);
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.DeselectAll();

            upDownPitch.Value = pitch;
            checkLoop.Checked = loop;

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
        public int NewPitch => (int)upDownPitch.Value;
        public bool NewLoop => checkLoop.Checked;

        private void EditDPCMDialog_KeyDown(object sender, KeyEventArgs e)
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
