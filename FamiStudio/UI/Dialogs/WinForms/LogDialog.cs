using FamiStudio.Properties;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class LogDialog : Form, ILogOutput
    {
        private LogSeverity minSeverity = LogSeverity.Warning;
        private List<string> messages = new List<string>();

        public LogDialog(LogSeverity minSeverity)
        {
            InitializeComponent();

            string suffix = Direct2DTheme.DialogScaling >= 2.0f ? "@2x" : "";

            Width  = (int)(Direct2DTheme.DialogScaling * Width);
            Height = (int)(Direct2DTheme.DialogScaling * Height);

            textBox.Font = new Font(PlatformUtils.PrivateFontCollection.Families[0], 8.0f, FontStyle.Regular);

            buttonYes.Image = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.Yes{suffix}.png"));
            buttonYes.Width  = (int)(buttonYes.Width * Direct2DTheme.DialogScaling);
            buttonYes.Height = (int)(buttonYes.Height * Direct2DTheme.DialogScaling);
            buttonYes.Left = Width - buttonYes.Width - 10;
            buttonYes.Top = Height - buttonYes.Height - 10;

            this.minSeverity = minSeverity;
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

        public bool LogEmpty => messages.Count == 0;

        protected override void OnShown(EventArgs e)
        {
            textBox.Text = string.Join("\r\n", messages);

            base.OnShown(e);
        }

        public void Log(string msg)
        {
            messages.Add(msg);   
        }

        public LogSeverity MinSeverity => minSeverity;
    }
}
