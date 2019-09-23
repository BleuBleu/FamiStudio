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

            toolbar.Height *= scaling;
            tableLayout.RowStyles[0].Height *= scaling;
            projectExplorer.Width *= scaling;
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

        public void Run()
        {
            Application.Run(this);
        }
    }
}
