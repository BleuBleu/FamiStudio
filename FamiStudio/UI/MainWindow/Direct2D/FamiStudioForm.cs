using System;
using System.Diagnostics;
using System.Net.Http;
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

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Up   ||
                keyData == Keys.Down ||
                keyData == Keys.Left ||
                keyData == Keys.Right)
            {
                famistudio.KeyDown(new KeyEventArgs(keyData));
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        public void Run()
        {
            Application.Run(this);
        }
    }
}
