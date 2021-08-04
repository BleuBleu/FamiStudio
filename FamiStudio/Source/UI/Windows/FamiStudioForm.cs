using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OpenTK.Graphics;
using Microsoft.Win32;
using System.Drawing;
using OpenTK.Graphics.OpenGL;

using RenderTheme = FamiStudio.GLTheme;
using System.Diagnostics;

// MATTT : Test horizontal mouse wheel.

namespace FamiStudio
{
    public partial class FamiStudioForm : GLForm
    {
        private FamiStudio famistudio;
        private FamiStudioControls controls;

        public FamiStudio FamiStudio => famistudio;
        public Toolbar ToolBar => controls.ToolBar;
        public Sequencer Sequencer => controls.Sequencer;
        public PianoRoll PianoRoll => controls.PianoRoll;
        public ProjectExplorer ProjectExplorer => controls.ProjectExplorer;

        private GLControl captureControl = null;
        private MouseButtons captureButton   = MouseButtons.None;
        private MouseButtons lastButtonPress = MouseButtons.None;
        private Timer timer = new Timer();

        [DllImport("USER32.dll")]
        private static extern short GetKeyState(int key);

        public FamiStudioForm(FamiStudio famistudio)
        {
            this.famistudio = famistudio;

            Cursors.Initialize();

            controls = new FamiStudioControls(this);

            timer.Tick += timer_Tick;
            timer.Interval = 4;
            timer.Start();

            InitForm();

            MouseWheel += FamiStudioForm_MouseWheel;
            DragDrop   += FamiStudioForm_DragDrop;
            DragEnter  += FamiStudioForm_DragEnter;
        }

        private void InitForm()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FamiStudioForm));

            AutoScaleMode = AutoScaleMode.None;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(33, 37, 41);
            ClientSize = new Size(1264, 681);
            Icon = new Icon(typeof(FamiStudioForm).Assembly.GetManifestResourceStream("FamiStudio.Resources.FamiStudio.ico"));
            KeyPreview = true; 
            Name = "FamiStudioForm";
            Text = "FamiStudio";
            AllowDrop = true;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            famistudio.Tick();
        }

        protected override void GraphicsContextInitialized()
        {
            GL.Disable(EnableCap.DepthTest);
            GL.Viewport(0, 0, Width, Height);
            GL.ClearColor(
                ThemeBase.DarkGreyFillColor2.R / 255.0f,
                ThemeBase.DarkGreyFillColor2.G / 255.0f,
                ThemeBase.DarkGreyFillColor2.B / 255.0f,
                1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsContext.CurrentContext.SwapBuffers();

            controls.InitializeGL(this);
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

        protected override void RenderFrame(bool force = false)
        {
            if (force)
                controls.Invalidate();

            controls.Redraw();
        }

        public void CaptureMouse(GLControl ctrl)
        {
            if (lastButtonPress != System.Windows.Forms.MouseButtons.None)
            {
                Debug.Assert(captureControl == null);

                captureButton = lastButtonPress;
                captureControl = ctrl;
                Capture = true;
            }
        }

        public void ReleaseMouse()
        {
            if (captureControl != null)
            {
                captureControl = null;
                Capture = false;
            }
        }

        public Point PointToClient(GLControl ctrl, Point p)
        {
            p = PointToClient(p);
            return new Point(p.X - ctrl.Left, p.Y - ctrl.Top);
        }

        public Point PointToScreen(GLControl ctrl, Point p)
        {
            p = new Point(p.X + ctrl.Left, p.Y + ctrl.Top);
            return PointToScreen(p);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RefreshLayout();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (captureControl != null)
                return;

            var ctrl = controls.GetControlAtCoord(e.X, e.Y, out int x, out int y);
            lastButtonPress = e.Button;
            if (ctrl != null)
                ctrl.MouseDown(new MouseEventArgs(e.Button, e.Clicks, x, y, e.Delta));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            int x;
            int y;
            GLControl ctrl = null;

            if (captureControl != null)
            {
                ctrl = captureControl;
                x = e.X - ctrl.Left;
                y = e.Y - ctrl.Top;
            }
            else
            {
                ctrl = controls.GetControlAtCoord(e.X, e.Y, out x, out y);
            }

            if (e.Button == captureButton)
                ReleaseMouse();

            if (ctrl != null)
                ctrl.MouseUp(new MouseEventArgs(e.Button, e.Clicks, x, y, e.Delta));
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            if (captureControl != null)
                return;

            var ctrl = controls.GetControlAtCoord(e.X, e.Y, out int x, out int y);
            lastButtonPress = e.Button;
            if (ctrl != null)
                ctrl.MouseDoubleClick(new MouseEventArgs(e.Button, e.Clicks, x, y, e.Delta));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int x;
            int y;
            GLControl ctrl = null;

            if (captureControl != null)
            {
                ctrl = captureControl;
                x = e.X - ctrl.Left;
                y = e.Y - ctrl.Top;
            }
            else
            {
                ctrl = controls.GetControlAtCoord(e.X, e.Y, out x, out y);
            }

            if (ctrl != null)
            {
                ctrl.MouseMove(new MouseEventArgs(e.Button, e.Clicks, x, y, e.Delta));
                RefreshCursor(ctrl);
            }
        }

        private void FamiStudioForm_MouseWheel(object sender, MouseEventArgs e)
        {
            var ctrl = controls.GetControlAtCoord(e.X, e.Y, out int x, out int y);
            if (ctrl != null)
                ctrl.MouseWheel(new MouseEventArgs(e.Button, e.Clicks, x, y, e.Delta));
        }

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x020e) // WM_MOUSEHWHEEL
            {
                var e = PlatformUtils.ConvertHorizontalMouseWheelMessage(this, m);
                var ctrl = controls.GetControlAtCoord(e.X, e.Y, out int x, out int y);

                if (ctrl != null)
                    ctrl.MouseHorizontalWheel(e);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            famistudio.KeyDown(e, (int)e.KeyCode);
            foreach (var ctrl in controls.Controls)
                ctrl.KeyDown(e);

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            famistudio.KeyUp(e, (int)e.KeyCode);
            foreach (var ctrl in controls.Controls)
                ctrl.KeyUp(e);

            base.OnKeyUp(e);
        }

        public void RefreshLayout()
        {
            controls.Resize(ClientRectangle.Width, ClientRectangle.Height);
            controls.Invalidate();
        }

        public Keys GetModifierKeys()
        {
            return ModifierKeys;
        }

        public Point GetCursorPosition()
        {
            return Cursor.Position;
        }

        public void RefreshCursor()
        {
            var pt = PointToClient(Cursor.Position);
            RefreshCursor(controls.GetControlAtCoord(pt.X, pt.Y, out _, out _));
        }

        private void RefreshCursor(GLControl ctrl)
        {
            if (captureControl != null && captureControl != ctrl)
                return;

            if (ctrl != null)
                Cursor = ctrl.Cursor.Current;
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
