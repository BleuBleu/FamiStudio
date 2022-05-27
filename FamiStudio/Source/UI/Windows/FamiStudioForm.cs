using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OpenTK.Graphics;
using System.Drawing;
using OpenTK.Graphics.OpenGL;

using System.Diagnostics;
using System.Threading.Tasks;

namespace FamiStudio
{
    // MATTT : Rename to "Window".
    public partial class FamiStudioForm : GLForm
    {
        private static FamiStudioForm instance;

        private const int DelayedRightClickTimeMs = 250;
        private const int DelayedRightClickPixelTolerance = 2;

        private FamiStudio famistudio;
        private FamiStudioControls controls;

        public FamiStudio FamiStudio => famistudio;
        public Toolbar ToolBar => controls.ToolBar;
        public Sequencer Sequencer => controls.Sequencer;
        public PianoRoll PianoRoll => controls.PianoRoll;
        public ProjectExplorer ProjectExplorer => controls.ProjectExplorer;
        public QuickAccessBar QuickAccessBar => controls.QuickAccessBar;
        public MobilePiano MobilePiano => controls.MobilePiano;
        public new ContextMenu ContextMenu => controls.ContextMenu;
        public static FamiStudioForm Instance => instance;
        public new GLControl ActiveControl => activeControl;
        public GLGraphics Graphics => controls.Graphics;

        public bool IsLandscape => true;
        public bool IsAsyncDialogInProgress => controls.IsDialogActive;
        public bool MobilePianoVisible { get => false; set => value = false; }

        private GLControl activeControl = null;
        private GLControl captureControl = null;
        private GLControl hoverControl = null;
        private MouseButtons captureButton   = MouseButtons.None;
        private MouseButtons lastButtonPress = MouseButtons.None;
        private Timer timer = new Timer();
        private Point contextMenuPoint = Point.Empty;
        private DateTime lastTickTime = DateTime.Now;

        private DateTime       delayedRightClickStartTime = DateTime.MinValue;
        private MouseEventArgs delayedRightClickArgs      = null;
        private GLControl      delayedRightClickControl   = null;

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParameter;
            public IntPtr LParameter;
            public uint Time;
            public Point Location;
        }

        [DllImport("USER32.dll")]
        private static extern short GetKeyState(int key);

        [DllImport("user32.dll")]
        public static extern int PeekMessage(out NativeMessage message, IntPtr window, uint filterMin, uint filterMax, uint remove);

        public FamiStudioForm(FamiStudio famistudio)
        {
            this.famistudio = famistudio;

            Cursors.Initialize();

            instance = this;
            controls = new FamiStudioControls(this);
            activeControl = controls.PianoRoll;

            timer.Tick += timer_Tick;
            timer.Interval = 4;

            InitForm();

            DragDrop  += FamiStudioForm_DragDrop;
            DragEnter += FamiStudioForm_DragEnter;
            Application.Idle += Application_Idle;
        }

        bool IsApplicationIdle()
        {
            NativeMessage msg;
            return PeekMessage(out msg, IntPtr.Zero, 0, 0, 0) == 0;
        }

        private void Application_Idle(object sender, EventArgs e)
        {
            do
            {
                TickAndRender();
            }
            while (IsApplicationIdle());
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
            TickAndRender();
        }

        private void Tick()
        {
            // MATTT : Do the 0.25 clamp on mobile too!!
            var tickTime = DateTime.Now;
            var deltaTime = (float)Math.Min(0.25f, (float)(tickTime - lastTickTime).TotalSeconds);

            if (!IsAsyncDialogInProgress)
                famistudio.Tick(deltaTime);
            
            controls.Tick(deltaTime);

            lastTickTime = tickTime;
        }

        private void TickAndRender()
        {
            Tick();

            if (controls.AnyControlNeedsRedraw() && famistudio.Project != null)
            {
                // Here we hit a vsync if its enabled. 
                RenderFrameAndSwapBuffers();
            }

            // Always sleep, in case people turn off vsync. This avoid rendering 
            // is a super tight loop. We could check "VSyncEnabled" but its an extension
            // and I dont trust it. Let's take a break either way.
            System.Threading.Thread.Sleep(4);

            ConditionalEmitDelayedRightClick();
        }

        protected override void GraphicsContextInitialized()
        {
            GL.Disable(EnableCap.DepthTest);
            GL.Viewport(0, 0, Width, Height);
            GL.ClearColor(
                Theme.DarkGreyFillColor2.R / 255.0f,
                Theme.DarkGreyFillColor2.G / 255.0f,
                Theme.DarkGreyFillColor2.B / 255.0f,
                1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsContext.CurrentContext.SwapBuffers();

            controls.InitializeGL();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!famistudio.TryClosing())
            {
                e.Cancel = true;
                return;
            }

            timer.Stop();

            base.OnFormClosing(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            var ctrl = controls.GetControlAtCoord(e.X, e.Y, out int x, out int y);
            if (ctrl != null)
            {
                if (ctrl != ContextMenu)
                    controls.HideContextMenu();
                ctrl.MouseWheel(new MouseEventArgs(e.Button, e.Clicks, x, y, e.Delta));
            }
        }

        protected override void RenderFrame(bool force = false)
        {
            if (force)
                controls.MarkDirty();
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

        protected void DelayRightClick(GLControl ctrl, MouseEventArgsEx e)
        {
            Debug.WriteLine($"DelayRightClick {ctrl}");

            delayedRightClickControl = ctrl;
            delayedRightClickStartTime = DateTime.Now;
            delayedRightClickArgs = e;
        }

        protected void ClearDelayedRightClick()
        {
            if (delayedRightClickControl != null)
                Debug.WriteLine($"ClearDelayedRightClick {delayedRightClickControl}");

            delayedRightClickArgs = null;
            delayedRightClickControl = null;
            delayedRightClickStartTime = DateTime.MinValue;
        }

        protected void ConditionalEmitDelayedRightClick(bool checkTime = true, bool forceClear = false, GLControl checkCtrl = null)
        {
            var deltaMs = (DateTime.Now - delayedRightClickStartTime).TotalMilliseconds;
            var clear = forceClear;

            if (delayedRightClickArgs != null && (deltaMs > DelayedRightClickTimeMs || !checkTime) && (checkCtrl == delayedRightClickControl || checkCtrl == null))
            {
                Debug.WriteLine($"ConditionalEmitDelayedRightClick delayedRightClickControl={delayedRightClickControl} checkTime={checkTime} deltaMs={deltaMs} forceClear={forceClear}");
                delayedRightClickControl.MouseDownDelayed(delayedRightClickArgs);
                clear = true;
            }

            if (clear)
                ClearDelayedRightClick();
        }

        protected bool ShouldIgnoreMouseMoveBecauseOfDelayedRightClick(MouseEventArgs e, int x, int y, GLControl checkCtrl)
        {
            // Surprisingly is pretty common to move by 1 pixel between a right click
            // mouse down/up. Add a small tolerance.
            if (delayedRightClickArgs != null && checkCtrl == delayedRightClickControl && e.Button.HasFlag(MouseButtons.Right))
            {
                Debug.WriteLine($"ShouldIgnoreMouseMoveBecauseOfDelayedRightClick dx={Math.Abs(x - delayedRightClickArgs.X)} dy={Math.Abs(y - delayedRightClickArgs.Y)}");

                if (Math.Abs(x - delayedRightClickArgs.X) <= DelayedRightClickPixelTolerance &&
                    Math.Abs(y - delayedRightClickArgs.Y) <= DelayedRightClickPixelTolerance)
                {
                    return true;
                }
            }

            return false;            
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Debug.WriteLine($"OnMouseDown {e.X} {e.Y}");

            if (captureControl != null)
                return;

            var ctrl = controls.GetControlAtCoord(e.X, e.Y, out int x, out int y);
            lastButtonPress = e.Button;
            if (ctrl != null)
            {
                SetActiveControl(ctrl);
                
                // Ignore the first click.
                if (controls.IsContextMenuActive && ctrl != ContextMenu)
                {
                    controls.HideContextMenu();
                    return;
                }

                var ex = new MouseEventArgsEx(e.Button, e.Clicks, x, y, e.Delta);
                ctrl.GrabDialogFocus();
                ctrl.MouseDown(ex);
                if (ex.IsRightClickDelayed)
                    DelayRightClick(ctrl, ex);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            Debug.WriteLine($"OnMouseUp {e.X} {e.Y}");

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
            {
                if (e.Button.HasFlag(MouseButtons.Right))
                    ConditionalEmitDelayedRightClick(true, true, ctrl);

                if (ctrl != ContextMenu)
                    controls.HideContextMenu();

                ctrl.MouseUp(new MouseEventArgs(e.Button, e.Clicks, x, y, e.Delta));
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);

            var ctrl = controls.GetControlAtCoord(e.X, e.Y, out int x, out int y);
            lastButtonPress = e.Button;
            if (ctrl != null)
            {
                if (ctrl != ContextMenu)
                    controls.HideContextMenu();

                ctrl.MouseDoubleClick(new MouseEventArgs(e.Button, e.Clicks, x, y, e.Delta));
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int x;
            int y;
            GLControl ctrl = null;
            GLControl hover = null;
            Debug.WriteLine($"OnMouseMove {e.X} {e.Y}");

            if (captureControl != null)
            {
                ctrl = captureControl;
                x = e.X - ctrl.Left;
                y = e.Y - ctrl.Top;
                hover = controls.GetControlAtCoord(e.X, e.Y, out _, out _);
            }
            else
            {
                ctrl = controls.GetControlAtCoord(e.X, e.Y, out x, out y);
                hover = ctrl;
            }

            if (ShouldIgnoreMouseMoveBecauseOfDelayedRightClick(e, x, y, ctrl))
                return;

            ConditionalEmitDelayedRightClick(false, true, ctrl);

            // Dont forward move mouse when a context menu is active.
            if (ctrl != null && (!controls.IsContextMenuActive || ctrl == ContextMenu))
            {
                ctrl.MouseMove(new MouseEventArgs(e.Button, e.Clicks, x, y, e.Delta));
                RefreshCursor(ctrl);
            }

            if (hover != hoverControl)
            {
                if (hoverControl != null && (!controls.IsContextMenuActive || hoverControl == ContextMenu))
                    hoverControl.MouseLeave(EventArgs.Empty);
                hoverControl = hover;
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (hoverControl != null && (!controls.IsContextMenuActive || hoverControl == ContextMenu))
            {
                hoverControl.MouseLeave(EventArgs.Empty);
                hoverControl = null;
            }

            base.OnMouseLeave(e);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x020e) // WM_MOUSEHWHEEL
            {
                var e = PlatformUtils.ConvertHorizontalMouseWheelMessage(this, m);
                var ctrl = controls.GetControlAtCoord(e.X, e.Y, out int x, out int y);

                if (ctrl != null)
                {
                    if (ctrl != ContextMenu)
                        controls.HideContextMenu();
                    ctrl.MouseHorizontalWheel(e);
                }
            }
            else if (m.Msg == 0x0231) // WM_ENTERSIZEMOVE 
            {
                // We dont receive any messages during resize/move, so we rely on a timer.
                timer.Start();
            }
            else if (m.Msg == 0x0232) // WM_EXITSIZEMOVE
            {
                timer.Stop();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (controls.IsContextMenuActive)
            {
                controls.ContextMenu.KeyDown(e);
            }
            else if (controls.IsDialogActive)
            {
                controls.TopDialog.KeyDown(e);
            }
            else
            {
                famistudio.KeyDown(e, (int)e.KeyCode);
                foreach (var ctrl in controls.Controls)
                    ctrl.KeyDown(e);
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (controls.IsContextMenuActive)
            {
                controls.ContextMenu.KeyUp(e);
            }
            else if (controls.IsDialogActive)
            {
                controls.TopDialog.KeyUp(e);
            }
            else
            {
                famistudio.KeyUp(e, (int)e.KeyCode);
                foreach (var ctrl in controls.Controls)
                    ctrl.KeyUp(e);
            }

            base.OnKeyUp(e);
        }

        public void RefreshLayout()
        {
            controls.Resize(ClientRectangle.Width, ClientRectangle.Height);
            controls.MarkDirty();
        }

        public void MarkDirty()
        {
            controls.MarkDirty();
        }

        public Keys GetModifierKeys()
        {
            return ModifierKeys;
        }

        public Point GetCursorPosition()
        {
            // Pretend the mouse is fixed when a context menu is active.
            return controls.IsContextMenuActive ? contextMenuPoint : Cursor.Position;
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

            Cursor = ctrl != null ? ctrl.Cursor.Current : Cursors.Default;
        }

        public void SetActiveControl(GLControl ctrl, bool animate = true)
        {
            if (ctrl != null && ctrl != activeControl && (ctrl == PianoRoll || ctrl == Sequencer || ctrl == ProjectExplorer))
            {
                activeControl.MarkDirty();
                activeControl = ctrl;
                activeControl.MarkDirty();
            }
        }

        public void ShowContextMenu(int x, int y, ContextMenuOption[] options)
        {
            contextMenuPoint = PointToScreen(new Point(x, y));
            controls.ShowContextMenu(x, y, options);
        }

        public void HideContextMenu()
        {
            controls.HideContextMenu();
        }

        public void InitDialog(Dialog dialog)
        {
            controls.InitDialog(dialog);
        }

        public void PushDialog(Dialog dialog)
        {
            controls.PushDialog(dialog);
        }

        public void PopDialog(Dialog dialog)
        {
            controls.PopDialog(dialog);
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
                keyData == Keys.Tab   ||
                keyData == Keys.F10)
            {
                var e = new KeyEventArgs(keyData);

                if (controls.IsContextMenuActive)
                {
                    controls.ContextMenu.KeyDown(e);
                }
                else
                {
                    famistudio.KeyDown(e, (int)keyData);
                    foreach (var ctrl in controls.Controls)
                        ctrl.KeyDown(e);
                }

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

            //timerTask = Task.Factory.StartNew(TimerThread, TaskCreationOptions.LongRunning);
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
