using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    // MATTT : Rename to "Window".
    public class FamiStudioForm
    {
        private static FamiStudioForm instance; // MATTT : Remove once we pass the form to the dialogs.

        private const double DelayedRightClickTime = 0.25;
        private const int    DelayedRightClickPixelTolerance = 2;

        private IntPtr window; // GLFW window.

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

        public Size Size
        {
            get
            {
                glfwGetWindowSize(window, out var w, out var h);
                return new Size(w, h);
            }
        }

        public int Width
        {
            get
            {
                glfwGetWindowSize(window, out var w, out _);
                return w;
            }
        }

        public int Height
        {
            get
            {
                glfwGetWindowSize(window, out _, out var h);
                return h;
            }
        }

        public string Text { set => glfwSetWindowTitle(window, value); }
        public bool IsLandscape => true;
        public bool IsAsyncDialogInProgress => controls.IsDialogActive;
        public bool MobilePianoVisible { get => false; set => value = false; }

        private GLControl activeControl = null;
        private GLControl captureControl = null;
        private GLControl hoverControl = null;
        private int captureButton = -1;
        private int lastButtonPress = -1;
        private Timer timer = new Timer();
        private Point contextMenuPoint = Point.Empty;
        private double lastTickTime = -1.0f;
        private bool quit = false;
        private int lastCursorX = -1;
        private int lastCursorY = -1;

        // Double-click emulation.
        private int lastClickButton = -1;
        private double lastClickTime;
        private int lastClickX = -1;
        private int lastClickY = -1;

        private double delayedRightClickStartTime;
        private MouseEventArgs2 delayedRightClickArgs = null;
        private GLControl delayedRightClickControl = null;

        //[StructLayout(LayoutKind.Sequential)]
        //public struct NativeMessage
        //{
        //    public IntPtr Handle;
        //    public uint Message;
        //    public IntPtr WParameter;
        //    public IntPtr LParameter;
        //    public uint Time;
        //    public Point Location;
        //}

        GLFWerrorfun errorCallback;
        GLFWwindowsizefun windowSizeCallback;
        GLFWwindowclosefun windowCloseCallback;
        GLFWwindowrefreshfun windowRefreshCallback;
        GLFWframebuffersizefun frameBufferSizeCallback; // TODO
        GLFWwindowcontentscalefun contentScaleCallback; // TODO
        GLFWmousebuttonfun mouseButtonCallback;
        GLFWcursorposfun cursorPosCallback;
        GLFWcursorenterfun cursorEnterCallback;
        GLFWscrollfun scrollCallback;
        GLFWkeyfun keyCallback;
        GLFWcharfun charCallback;
        GLFWcharmodsfun charModsCallback;
        GLFWdropfun dropCallback;
        GLFWmonitorfun monitorCallback; // TODO

        //[DllImport("USER32.dll")]
        //private static extern short GetKeyState(int key);

        //[DllImport("user32.dll")]
        //public static extern int PeekMessage(out NativeMessage message, IntPtr window, uint filterMin, uint filterMax, uint remove);

        public FamiStudioForm(FamiStudio app, IntPtr glfwWindow)
        {
            famistudio = app;
            window = glfwWindow;
            instance = this;
            controls = new FamiStudioControls(this);
            activeControl = controls.PianoRoll;

            //timer.Tick += timer_Tick;
            //timer.Interval = 4;

            //DragDrop  += FamiStudioForm_DragDrop;
            //DragEnter += FamiStudioForm_DragEnter;
            //Application.Idle += Application_Idle;

            //GL.Disable(EnableCap.DepthTest);
            //GL.Viewport(0, 0, Width, Height);
            //GL.ClearColor(
            //    Theme.DarkGreyFillColor2.R / 255.0f,
            //    Theme.DarkGreyFillColor2.G / 255.0f,
            //    Theme.DarkGreyFillColor2.B / 255.0f,
            //    1.0f);
            //GL.Clear(ClearBufferMask.ColorBufferBit);
            //GraphicsContext.CurrentContext.SwapBuffers();

            // MATTT : Unset those when closing.
            errorCallback = new GLFWerrorfun(ErrorCallback);
            windowSizeCallback = new GLFWwindowsizefun(WindowSizeCallback);
            windowCloseCallback = new GLFWwindowclosefun(WindowCloseCallback);
            windowRefreshCallback = new GLFWwindowrefreshfun(WindowRefreshCallback);
            //frameBufferSizeCallback = new GLFWframebuffersizefun(); // TODO!
            //contentScaleCallback = new GLFWwindowcontentscalefun(); // TODO!
            mouseButtonCallback = new GLFWmousebuttonfun(MouseButtonCallback);
            cursorPosCallback = new GLFWcursorposfun(CursorPosCallback);
            cursorEnterCallback = new GLFWcursorenterfun(CursorEnterCallback);
            scrollCallback = new GLFWscrollfun(ScrollCallback);
            keyCallback = new GLFWkeyfun(KeyCallback);
            charCallback = new GLFWcharfun(CharCallback);
            charModsCallback = new GLFWcharmodsfun(CharModsCallback);
            dropCallback = new GLFWdropfun(DropCallback);
            //monitorCallback = new GLFWmonitorfun(); // TODO!

            glfwSetErrorCallback(errorCallback);
            glfwSetWindowSizeCallback(window, windowSizeCallback);
            glfwSetWindowCloseCallback(window, windowCloseCallback);
            glfwSetWindowRefreshCallback(window, windowRefreshCallback);
            //glfwSetFramebufferSizeCallback(); // TODO!
            //glfwSetWindowContentScaleCallback(); // TODO!
            glfwSetMouseButtonCallback(window, mouseButtonCallback);
            glfwSetCursorPosCallback(window, cursorPosCallback);
            glfwSetCursorEnterCallback(window, cursorEnterCallback);
            glfwSetScrollCallback(window, scrollCallback);
            glfwSetKeyCallback(window, keyCallback);
            glfwSetCharCallback(window, charCallback);
            glfwSetDropCallback(window, dropCallback);
            //glfwSetMonitorCallback(); // TODO!

            EnableWindowsDarkTheme();

            controls.InitializeGL();
        }
        
        public static unsafe FamiStudioForm InitializeGLFWAndCreateWindow(FamiStudio fs)
        {
            if (glfwInit() == 0)
                return null;

            glfwWindowHint(GLFW_CLIENT_API, GLFW_OPENGL_API);
            glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 1);
            glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 2);
            glfwWindowHint(GLFW_MAXIMIZED, 1);

            var window = glfwCreateWindow(640, 480, "FamiStudio", IntPtr.Zero, IntPtr.Zero);
            if (window == IntPtr.Zero)
            {
                glfwTerminate();
                return null;
            }

            glfwMakeContextCurrent(window);
            glfwSwapInterval(1);
            glfwGetWindowContentScale(window, out var scaling, out _);

            GL.Initialize();
            Cursors.Initialize();
            DpiScaling.Initialize(scaling);

            return new FamiStudioForm(fs, window);
        }

        //bool IsApplicationIdle()
        //{
        //    NativeMessage msg;
        //    return PeekMessage(out msg, IntPtr.Zero, 0, 0, 0) == 0;
        //}

        //private void Application_Idle(object sender, EventArgs e)
        //{
        //    do
        //    {
        //        TickAndRender();
        //    }
        //    while (IsApplicationIdle());
        //}

        //private void timer_Tick(object sender, EventArgs e)
        //{
        //    TickAndRender();
        //}

        private void Tick()
        {
            // MATTT : Do the 0.25 clamp on mobile too!!
            var tickTime = glfwGetTime();

            if (lastTickTime < 0.0)
                lastTickTime = tickTime;

            var deltaTime = (float)Math.Min(0.25f, (float)(tickTime - lastTickTime));

            if (!IsAsyncDialogInProgress)
                famistudio.Tick(deltaTime);

            controls.Tick(deltaTime);

            lastTickTime = tickTime;
        }

        //private void TickAndRender()
        //{
        //    Tick();

        //    if (controls.AnyControlNeedsRedraw() && famistudio.Project != null)
        //    {
        //        // Here we hit a vsync if its enabled. 
        //        //RenderFrameAndSwapBuffers(); MATTT
        //    }

        //    // Always sleep, in case people turn off vsync. This avoid rendering 
        //    // is a super tight loop. We could check "VSyncEnabled" but its an extension
        //    // and I dont trust it. Let's take a break either way.
        //    System.Threading.Thread.Sleep(4);

        //    //ConditionalEmitDelayedRightClick(); MATTT
        //}

        // MATTT : Temporary
        public void RunEventLoop()
        {
            RunIteration();
        }

        protected void RenderFrame(bool force = false)
        {
            if (force)
                controls.MarkDirty();
            controls.Redraw();
        }

        public void CaptureMouse(GLControl ctrl)
        {
            //if (lastButtonPress != System.Windows.Forms.MouseButtons.None)
            //{
            //    Debug.Assert(captureControl == null);

            //    captureButton = lastButtonPress;
            //    captureControl = ctrl;
            //    Capture = true;
            //}
        }

        public void ReleaseMouse()
        {
            //if (captureControl != null)
            //{
            //    captureControl = null;
            //    Capture = false;
            //}
        }

        public Point PointToClient(GLControl ctrl, Point p)
        {
            //p = PointToClient(p);
            //return new Point(p.X - ctrl.Left, p.Y - ctrl.Top);
            return Point.Empty;
        }

        public Point PointToScreen(GLControl ctrl, Point p)
        {
            //p = new Point(p.X + ctrl.Left, p.Y + ctrl.Top);
            //return PointToScreen(p);
            return Point.Empty;
        }

        private void ErrorCallback(int error, string description)
        {
        }

        private void WindowSizeCallback(IntPtr window, int width, int height)
        {
            RefreshLayout();
        }

        private void WindowCloseCallback(IntPtr window)
        {
            // MATTT : This may pop some async message boxes in the future.
            if (famistudio.TryClosing())
                quit = true;

            // timer.Stop(); // MATTT : See how we want to handle this.
        }

        private void WindowRefreshCallback(IntPtr window)
        {
        }

        private void MouseButtonCallback(IntPtr window, int button, int action, int mods)
        {
            Debug.WriteLine($"BUTTON! Button={button}, Action={action}, Mods={mods}");

            if (action == GLFW_PRESS)
            {
                if (captureControl != null)
                    return;

                var ctrl = controls.GetControlAtCoord(lastCursorX, lastCursorY, out int cx, out int cy);
                
                lastButtonPress = button; // MATTT : Remove MouseButtons when we are done with WinFOrms.

                // Double click emulation.
                var now = glfwGetTime();
                var delay = now - lastClickTime;

                var doubleClick = 
                    button == lastClickButton &&
                    delay <= PlatformUtils.DoubleClickTime &&
                    Math.Abs(lastClickX - lastCursorX) < 4 &&
                    Math.Abs(lastClickY - lastCursorY) < 4;

                if (doubleClick)
                {
                    lastClickButton = -1;
                }
                else
                {
                    lastClickButton = button;
                    lastClickTime = now;
                    lastClickX = lastCursorX;
                    lastClickY = lastCursorY;
                }

                if (ctrl != null)
                {
                    SetActiveControl(ctrl);

                    // Ignore the first click.
                    if (controls.IsContextMenuActive && ctrl != ContextMenu)
                    {
                        controls.HideContextMenu();
                        return;
                    }

                    if (doubleClick)
                    {
                        ctrl.MouseDoubleClick(new MouseEventArgs2(MakeButtonFlags(button), cx, cy));
                    }
                    else
                    {
                        var ex = new MouseEventArgs2(MakeButtonFlags(button), cx, cy); // MATTT : Remove MouseButtons when we are done with WinFOrms.
                        ctrl.GrabDialogFocus();
                        ctrl.MouseDown(ex);
                        if (ex.IsRightClickDelayed)
                            DelayRightClick(ctrl, ex);
                    }
                }
            }
            else if (action == GLFW_RELEASE)
            {
                int cx;
                int cy;
                GLControl ctrl = null;

                if (captureControl != null)
                {
                    ctrl = captureControl;
                    cx = lastCursorX - ctrl.Left;
                    cy = lastCursorY - ctrl.Top;
                }
                else
                {
                    ctrl = controls.GetControlAtCoord(lastCursorX, lastCursorY, out cx, out cy);
                }

                if (MakeButtonFlags(button) == captureButton) // MATTT : WinForms shit.
                    ReleaseMouse();

                if (ctrl != null)
                {
                    if (button == GLFW_MOUSE_BUTTON_RIGHT)
                        ConditionalEmitDelayedRightClick(true, true, ctrl);

                    if (ctrl != ContextMenu)
                        controls.HideContextMenu();

                    ctrl.MouseUp(new MouseEventArgs2(MakeButtonFlags(button), cx, cy));
                }
            }
        }

        private void CursorPosCallback(IntPtr window, double xpos, double ypos)
        {
            Debug.WriteLine($"POS! X={xpos}, Y={ypos}");

            // MATTT : Do we get fractional coords with DPI scaling?
            lastCursorX = (int)xpos;
            lastCursorY = (int)ypos;

            int cx;
            int cy;
            GLControl ctrl = null;
            GLControl hover = null;

            if (captureControl != null)
            {
                ctrl = captureControl;
                cx = lastCursorX - ctrl.Left;
                cy = lastCursorY - ctrl.Top;
                hover = controls.GetControlAtCoord(lastCursorX, lastCursorY, out _, out _);
            }
            else
            {
                ctrl = controls.GetControlAtCoord(lastCursorX, lastCursorY, out cx, out cy);
                hover = ctrl;
            }

            var buttons = MakeButtonFlags(
                glfwGetMouseButton(window, GLFW_MOUSE_BUTTON_LEFT) != 0,
                glfwGetMouseButton(window, GLFW_MOUSE_BUTTON_RIGHT) != 0,
                glfwGetMouseButton(window, GLFW_MOUSE_BUTTON_MIDDLE) != 0);

            var e = new MouseEventArgs2(buttons, cx, cy);

            if (ShouldIgnoreMouseMoveBecauseOfDelayedRightClick(e, cx, cy, ctrl))
                return;

            ConditionalEmitDelayedRightClick(false, true, ctrl);

            // Dont forward move mouse when a context menu is active.
            if (ctrl != null && (!controls.IsContextMenuActive || ctrl == ContextMenu))
            {
                ctrl.MouseMove(e);
                RefreshCursor(ctrl);
            }

            if (hover != hoverControl)
            {
                if (hoverControl != null && (!controls.IsContextMenuActive || hoverControl == ContextMenu))
                    hoverControl.MouseLeave(EventArgs.Empty);
                hoverControl = hover;
            }
        }

        private void CursorEnterCallback(IntPtr window, int entered)
        {
            Debug.WriteLine($"ENTER! entered={entered}");

            if (entered == 0)
            {
                if (hoverControl != null && (!controls.IsContextMenuActive || hoverControl == ContextMenu))
                {
                    hoverControl.MouseLeave(EventArgs.Empty);
                    hoverControl = null;
                }
            }
        }

        private void ScrollCallback(IntPtr window, double xoffset, double yoffset)
        {
            Debug.WriteLine($"SCROLL! X={xoffset}, Y={yoffset}");

            var ctrl = controls.GetControlAtCoord(lastCursorX, lastCursorY, out int cx, out int cy);
            if (ctrl != null)
            {
                if (ctrl != ContextMenu)
                    controls.HideContextMenu();

                // MATTT : Apply trackpad sensitivity if trackpad controls
                // MATTT : HACKY MULTIPLIER.
                var scrollX = Utils.SignedCeil((float)xoffset * 5000.0f);
                var scrollY = Utils.SignedCeil((float)yoffset * 5000.0f);

                // MATTT : Read button states here too.
                if (scrollY != 0.0f)
                    ctrl.MouseWheel(new MouseEventArgs2(0, cx, cy, 0, scrollY));
                if (scrollX != 0.0f)
                    ctrl.MouseHorizontalWheel(new MouseEventArgs2(0, cx, cy, scrollX));
            }
        }

        private void KeyCallback(IntPtr window, int key, int scancode, int action, int mods)
        {
            Debug.WriteLine($"KEY! Key = {key}, Scancode = {scancode}, Action = {action}, Mods = {mods}");
        }

        private void CharCallback(IntPtr window, uint codepoint)
        {
            Debug.WriteLine($"CHAR! Key = {codepoint}");
        }

        private void CharModsCallback(IntPtr window, uint codepoint, int mods)
        {
        }

        private unsafe void DropCallback(IntPtr window, int count, IntPtr paths)
        {
            if (count > 0)
            {
                string filename;

                // There has to be a more generic way to do this? 
                if (IntPtr.Size == 4)
                    filename = Marshal.PtrToStringAnsi(new IntPtr(*(int*)paths.ToPointer()));
                else
                    filename = Marshal.PtrToStringAnsi(new IntPtr(*(long*)paths.ToPointer()));

                if (!string.IsNullOrEmpty(filename))
                    famistudio.OpenProject(filename);
            }
        }

        private void GetCursorPosInternal(out int x, out int y)
        {
            // MATTT : Do we get fractional coords with DPI scaling?
            glfwGetCursorPos(window, out var dx, out var dy);
            x = (int)dx;
            y = (int)dy;
        }

        private int MakeButtonFlags(int button)
        {
            switch (button)
            {
                case GLFW_MOUSE_BUTTON_LEFT:   return MouseEventArgs2.ButtonLeft;
                case GLFW_MOUSE_BUTTON_RIGHT:  return MouseEventArgs2.ButtonRight;
                case GLFW_MOUSE_BUTTON_MIDDLE: return MouseEventArgs2.ButtonMiddle;
            }

            return 0;
        }

        private int MakeButtonFlags(bool l, bool r, bool m)
        {
            var flags = 0;
            if (l) flags |= MouseEventArgs2.ButtonLeft;
            if (r) flags |= MouseEventArgs2.ButtonRight;
            if (m) flags |= MouseEventArgs2.ButtonMiddle;
            return flags;
        }

        protected void DelayRightClick(GLControl ctrl, MouseEventArgs2 e)
        {
            Debug.WriteLine($"DelayRightClick {ctrl}");

            delayedRightClickControl = ctrl;
            delayedRightClickStartTime = glfwGetTime();
            delayedRightClickArgs = e;
        }

        protected void ClearDelayedRightClick()
        {
            if (delayedRightClickControl != null)
                Debug.WriteLine($"ClearDelayedRightClick {delayedRightClickControl}");

            delayedRightClickArgs = null;
            delayedRightClickControl = null;
            delayedRightClickStartTime = 0.0;
        }

        protected void ConditionalEmitDelayedRightClick(bool checkTime = true, bool forceClear = false, GLControl checkCtrl = null)
        {
            var delta = glfwGetTime() - delayedRightClickStartTime;
            var clear = forceClear;

            if (delayedRightClickArgs != null && (delta > DelayedRightClickTime || !checkTime) && (checkCtrl == delayedRightClickControl || checkCtrl == null))
            {
                Debug.WriteLine($"ConditionalEmitDelayedRightClick delayedRightClickControl={delayedRightClickControl} checkTime={checkTime} deltaMs={delta} forceClear={forceClear}");
                delayedRightClickControl.MouseDownDelayed(delayedRightClickArgs);
                clear = true;
            }

            if (clear)
                ClearDelayedRightClick();
        }

        protected bool ShouldIgnoreMouseMoveBecauseOfDelayedRightClick(MouseEventArgs2 e, int x, int y, GLControl checkCtrl)
        {
            // Surprisingly is pretty common to move by 1 pixel between a right click
            // mouse down/up. Add a small tolerance.
            if (delayedRightClickArgs != null && checkCtrl == delayedRightClickControl && e.Right)
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

        /*
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            
            if (m.Msg == 0x0231) // WM_ENTERSIZEMOVE 
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
        */

        // MATTT
        public void Refresh()
        {

        }

        public void RefreshLayout()
        {
            glfwGetWindowSize(window, out var w, out var h);
            controls.Resize(w, h);
            controls.MarkDirty();
        }

        public void MarkDirty()
        {
            controls.MarkDirty();
        }

        public Keys GetModifierKeys()
        {
            //return ModifierKeys;
            return Keys.None;
        }

        public Point GetCursorPosition()
        {
            // Pretend the mouse is fixed when a context menu is active.
            return controls.IsContextMenuActive ? contextMenuPoint : Cursor.Position;
        }

        public void RefreshCursor()
        {
            //var pt = PointToClient(Cursor.Position);
            //RefreshCursor(controls.GetControlAtCoord(pt.X, pt.Y, out _, out _));
        }

        private void RefreshCursor(GLControl ctrl)
        {
            //if (captureControl != null && captureControl != ctrl)
            //    return;

            //Cursor = ctrl != null ? ctrl.Cursor.Current : Cursors.Default;
        }

        public void SetActiveControl(GLControl ctrl, bool animate = true)
        {
            //if (ctrl != null && ctrl != activeControl && (ctrl == PianoRoll || ctrl == Sequencer || ctrl == ProjectExplorer))
            //{
            //    activeControl.MarkDirty();
            //    activeControl = ctrl;
            //    activeControl.MarkDirty();
            //}
        }

        public void ShowContextMenu(int x, int y, ContextMenuOption[] options)
        {
            //contextMenuPoint = PointToScreen(new Point(x, y));
            //controls.ShowContextMenu(x, y, options);
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
            //return (GetKeyState((int)k) & 0x8000) != 0;
            return false;
        }

        //protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        //{
        //    if (keyData == Keys.Up    ||
        //        keyData == Keys.Down  ||
        //        keyData == Keys.Left  ||
        //        keyData == Keys.Right ||
        //        keyData == Keys.Tab   ||
        //        keyData == Keys.F10)
        //    {
        //        var e = new KeyEventArgs(keyData);

        //        if (controls.IsContextMenuActive)
        //        {
        //            controls.ContextMenu.KeyDown(e);
        //        }
        //        else if (controls.IsDialogActive)
        //        {
        //            controls.TopDialog.KeyDown(e);
        //        }
        //        else
        //        {
        //            famistudio.KeyDown(e, (int)keyData);
        //            foreach (var ctrl in controls.Controls)
        //                ctrl.KeyDown(e);
        //        }

        //        return true;
        //    }
        //    else
        //    {
        //        return base.ProcessCmdKey(ref msg, keyData);
        //    }
        //}

        [DllImport("DwmApi")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

        protected void EnableWindowsDarkTheme()
        {
            if (PlatformUtils.IsWindows)
            {
                IntPtr handle = glfwGetNativeWindow(window);

                // From https://stackoverflow.com/questions/57124243/winforms-dark-title-bar-on-windows-10
                try
                {
                    if (DwmSetWindowAttribute(handle, 19, new[] { 1 }, 4) != 0)
                        DwmSetWindowAttribute(handle, 20, new[] { 1 }, 4);
                }
                catch
                {
                    // Will likely fail on Win7/8.
                }
            }
        }

        private void RunIteration()
        {
            glfwPollEvents();

            Tick();

            if (controls.AnyControlNeedsRedraw() && famistudio.Project != null)
            {
                controls.Redraw();
                glfwSwapBuffers(window);
            }

            // Always sleep a bit, even if we rendered something. This handles cases
            // where people turn off vsync. 
            System.Threading.Thread.Sleep(4);

            ConditionalEmitDelayedRightClick();
        }

        public void Run()
        {
            while (!quit)
            {
                RunIteration();
            }
        }
    }
}
