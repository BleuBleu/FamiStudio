using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace FamiStudio
{
    public partial class FamiStudioForm : Form
    {
        private FamiStudio famistudio;

        public FamiStudio FamiStudio => famistudio;
        public Toolbar ToolBar => toolbar;
        public Sequencer Sequencer => sequencer;
        public PianoRoll PianoRoll => pianoRoll;
        public ProjectExplorer ProjectExplorer => projectExplorer;

        private Timer timer = new Timer();

        [DllImport("USER32.dll")]
        private static extern short GetKeyState(int key);

        public FamiStudioForm(FamiStudio famistudio)
        {
            this.famistudio = famistudio;

            timer.Tick += timer_Tick;
            timer.Interval = 4;
            timer.Start();

            InitializeComponent();

            var scaling = Direct2DTheme.MainWindowScaling;

            toolbar.Height = (int)(toolbar.Height * scaling);
            tableLayout.RowStyles[0].Height = (int)(tableLayout.RowStyles[0].Height * scaling);
            projectExplorer.Width = (int)(projectExplorer.Width * scaling);
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            famistudio.Tick();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!famistudio.TryClosing())
            {
                e.Cancel = true;
                return;
            }

            base.OnFormClosing(e);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            famistudio.KeyDown(e);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            famistudio.KeyUp(e);
        }

        public static bool IsKeyDown(Keys k)
        {
            return (GetKeyState((int)k) & 0x8000) != 0;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Up    ||
                keyData == Keys.Down  ||
                keyData == Keys.Left  ||
                keyData == Keys.Right ||
                keyData == Keys.Tab)
            {
                famistudio.KeyDown(new KeyEventArgs(keyData));
                return true;
            }
            else
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        public void Run()
        {
            Application.Run(this);
        }

        public void RefreshSequencerLayout()
        {
            tableLayout.RowStyles[0].Height = (int)(sequencer.ComputeDesiredSizeY() * Direct2DTheme.MainWindowScaling);
            PianoRoll.Invalidate();
        }

        private void FamiStudioForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void FamiStudioForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                    FamiStudio.OpenProject(files[0]);
            }
        }
    }
}
