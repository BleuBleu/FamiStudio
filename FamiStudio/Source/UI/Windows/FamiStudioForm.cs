using Microsoft.Win32;
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
        private static bool mouseWheelRouting = false;

        [DllImport("USER32.dll")]
        private static extern short GetKeyState(int key);

        public FamiStudioForm(FamiStudio famistudio)
        {
            Cursors.Initialize();

            this.famistudio = famistudio;

            timer.Tick += timer_Tick;
            timer.Interval = 4;
            timer.Start();

            InitializeComponent();

            var scaling = Direct2DTheme.MainWindowScaling;

            toolbar.Height = (int)(toolbar.Height * scaling);
            tableLayout.RowStyles[0].Height = (int)(tableLayout.RowStyles[0].Height * scaling);
            projectExplorer.Width = (int)(projectExplorer.Width * scaling);

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("MouseWheelRouting");
                        if (o != null)
                            mouseWheelRouting = (int)(o) != 0;
                    }
                }
            }
            catch
            {
            }
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

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
        }

        public bool ShouldIgnoreMouseWheel(Control ctrl, MouseEventArgs e)
        {
            if (!mouseWheelRouting)
            {
                var pt = new System.Drawing.Point(e.X, e.Y);
                var outsideClientRectangle = !ctrl.ClientRectangle.Contains(pt);
                if (outsideClientRectangle)
                {
                    var controls = new Direct2DControl[]
                    {
                        toolbar,
                        sequencer,
                        pianoRoll,
                        projectExplorer
                    };

                    foreach (var ctrl2 in controls)
                    {
                        if (ctrl2 != ctrl)
                        {
                            var pt2 = ctrl2.PointToClient(ctrl.PointToScreen(pt));

                            if (ctrl2.ClientRectangle.Contains(pt2))
                            {
                                ctrl2.DoMouseWheel(new MouseEventArgs(e.Button, e.Clicks, pt2.X, pt2.Y, e.Delta));
                                break;
                            }
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            famistudio.KeyDown(e, (int)e.KeyCode);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            famistudio.KeyUp(e, (int)e.KeyCode);
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
                famistudio.KeyDown(new KeyEventArgs(keyData), (int)keyData);
                return true;
            }
            else
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        [DllImport("DwmApi")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // From https://stackoverflow.com/questions/57124243/winforms-dark-title-bar-on-windows-10
            try
            {
                if (DwmSetWindowAttribute(Handle, 19, new[] { 1 }, 4) != 0)
                    DwmSetWindowAttribute(Handle, 20, new[] { 1 }, 4);
            }
            catch
            {
                // Will likely fail on Win7/8.
            }
        }

        public void Run()
        {
            Application.Run(this);
        }

        public void RefreshLayout()
        {
            sequencer.Enabled = !PianoRoll.IsMaximized;

            if (PianoRoll.IsMaximized)
            {
                tableLayout.RowStyles[0].Height = 1;
            }
            else
            {
                tableLayout.RowStyles[0].Height = (int)(sequencer.ComputeDesiredSizeY() * Direct2DTheme.MainWindowScaling);
            }

            PianoRoll.Invalidate();
        }

        private void FamiStudioForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && CanFocus)
                e.Effect = DragDropEffects.Copy;
        }

        private void FamiStudioForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && CanFocus)
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                    FamiStudio.OpenProject(files[0]);
            }
        }
    }
}
