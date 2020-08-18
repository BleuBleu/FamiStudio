using FamiStudio.Properties;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class LogDialog : Form
    {
        public LogDialog(List<string> messages)
        {
            InitializeComponent();

            string suffix = Direct2DTheme.DialogScaling >= 2.0f ? "@2x" : "";

            Width  = (int)(Direct2DTheme.DialogScaling * Width);
            Height = (int)(Direct2DTheme.DialogScaling * Height);

            textBox.Font = new Font(PlatformUtils.PrivateFontCollection.Families[0], 10.0f, FontStyle.Regular);
            textBox.Text = string.Join("\r\n", messages);

            buttonYes.Image = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.Yes{suffix}.png"));
            buttonYes.Width  = (int)(buttonYes.Width * Direct2DTheme.DialogScaling);
            buttonYes.Height = (int)(buttonYes.Height * Direct2DTheme.DialogScaling);
            buttonYes.Left = Width - buttonYes.Width - 10;
            buttonYes.Top = Height - buttonYes.Height - 10;
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

        private void MultiPropertyDialog_KeyDown(object sender, KeyEventArgs e)
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
    }
}
