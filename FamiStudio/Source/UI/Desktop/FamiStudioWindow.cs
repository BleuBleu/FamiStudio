using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    public class FamiStudioWindow
    {
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
        public ContextMenu ContextMenu => controls.ContextMenu;
        public Control ActiveControl => activeControl;
        public Graphics Graphics => controls.Graphics;

        public Size Size => GetWindowSizeInternal();
        public int Width => GetWindowSizeInternal().Width;
        public int Height => GetWindowSizeInternal().Height;
        public string Text { set => glfwSetWindowTitle(window, value); }
        public bool IsLandscape => true;
        public bool IsAsyncDialogInProgress => controls.IsDialogActive;
        public bool MobilePianoVisible { get => false; set => value = false; }
        public IntPtr Handle => glfwGetNativeWindow(window);

        private Control activeControl = null;
        private Control captureControl = null;
        private Control hoverControl = null;
        private int captureButton = -1;
        private int lastButtonPress = -1;
        private Point contextMenuPoint = Point.Empty;
        private double lastTickTime = -1.0f;
        private float magnificationAccum = 0;
        private bool quit = false;
        private int lastCursorX = -1;
        private int lastCursorY = -1;
        private ModifierKeys modifiers = new ModifierKeys();

        // Double-click emulation.
        private int lastClickButton = -1;
        private double lastClickTime;
        private int lastClickX = -1;
        private int lastClickY = -1;

        private double delayedRightClickStartTime;
        private MouseEventArgs delayedRightClickArgs = null;
        private Control delayedRightClickControl = null;

        GLFWerrorfun errorCallback;
        GLFWwindowsizefun windowSizeCallback;
        GLFWwindowclosefun windowCloseCallback;
        GLFWwindowrefreshfun windowRefreshCallback;
        GLFWmousebuttonfun mouseButtonCallback;
        GLFWcursorposfun cursorPosCallback;
        GLFWcursorenterfun cursorEnterCallback;
        GLFWscrollfun scrollCallback;
        GLFWkeyfun keyCallback;
        GLFWcharfun charCallback;
        GLFWcharmodsfun charModsCallback;
        GLFWdropfun dropCallback;

        public FamiStudioWindow(FamiStudio app, IntPtr glfwWindow)
        {
            famistudio = app;
            window = glfwWindow;
            controls = new FamiStudioControls(this);
            activeControl = controls.PianoRoll;

            BindGLFWCallbacks();
            SetWindowIcon();
            SubclassWindow(true); 
            EnableWindowsDarkTheme();
            InitialFrameBufferClear();

            controls.InitializeGL();
        }
        
        private void BindGLFWCallbacks()
        {
            errorCallback = new GLFWerrorfun(ErrorCallback);
            windowSizeCallback = new GLFWwindowsizefun(WindowSizeCallback);
            windowCloseCallback = new GLFWwindowclosefun(WindowCloseCallback);
            windowRefreshCallback = new GLFWwindowrefreshfun(WindowRefreshCallback);
            mouseButtonCallback = new GLFWmousebuttonfun(MouseButtonCallback);
            cursorPosCallback = new GLFWcursorposfun(CursorPosCallback);
            cursorEnterCallback = new GLFWcursorenterfun(CursorEnterCallback);
            scrollCallback = new GLFWscrollfun(ScrollCallback);
            keyCallback = new GLFWkeyfun(KeyCallback);
            charCallback = new GLFWcharfun(CharCallback);
            charModsCallback = new GLFWcharmodsfun(CharModsCallback);
            dropCallback = new GLFWdropfun(DropCallback);

            glfwSetErrorCallback(errorCallback);
            glfwSetWindowSizeCallback(window, windowSizeCallback);
            glfwSetWindowCloseCallback(window, windowCloseCallback);
            glfwSetWindowRefreshCallback(window, windowRefreshCallback);
            glfwSetMouseButtonCallback(window, mouseButtonCallback);
            glfwSetCursorPosCallback(window, cursorPosCallback);
            glfwSetCursorEnterCallback(window, cursorEnterCallback);
            glfwSetScrollCallback(window, scrollCallback);
            glfwSetKeyCallback(window, keyCallback);
            glfwSetCharCallback(window, charCallback);
            glfwSetDropCallback(window, dropCallback);
        }

        private void UnbindGLFWCallbacks()
        {
            glfwSetErrorCallback(null);
            glfwSetWindowSizeCallback(window, null);
            glfwSetWindowCloseCallback(window, null);
            glfwSetWindowRefreshCallback(window, null);
            glfwSetMouseButtonCallback(window, null);
            glfwSetCursorPosCallback(window, null);
            glfwSetCursorEnterCallback(window, null);
            glfwSetScrollCallback(window, null);
            glfwSetKeyCallback(window, null);
            glfwSetCharCallback(window, null);
            glfwSetDropCallback(window, null);
        }

        public static unsafe FamiStudioWindow CreateWindow(FamiStudio fs)
        {
            glfwWindowHint(GLFW_CLIENT_API, GLFW_OPENGL_API);
            glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 1);
            glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 2);
            glfwWindowHint(GLFW_MAXIMIZED, 1);
            glfwWindowHint(GLFW_DOUBLEBUFFER, 1);
            glfwWindowHint(GLFW_COCOA_RETINA_FRAMEBUFFER, 1);

            var window = glfwCreateWindow(640, 480, "FamiStudio", IntPtr.Zero, IntPtr.Zero);
            if (window == IntPtr.Zero)
            {
                glfwTerminate();
                return null;
            }

            glfwMakeContextCurrent(window);
            glfwSwapInterval(1);
            glfwGetWindowContentScale(window, out var scaling, out _);

#if FAMISTUDIO_MACOS
            // TODO : Move this somewhere else. Must be called after window is created and before cursors.
            MacUtils.Initialize(glfwGetCocoaWindow(window));
#endif
            GL.StaticInitialize();
            Cursors.Initialize(scaling);
            DpiScaling.Initialize(scaling);

            return new FamiStudioWindow(fs, window);
        }

        private void InitialFrameBufferClear()
        {
            var size = GetWindowSizeInternal();
            GL.Disable(GL.DepthTest);
            GL.Viewport(0, 0, size.Width, size.Height);
            GL.ClearColor(
                Theme.DarkGreyLineColor1.R / 255.0f,
                Theme.DarkGreyLineColor1.G / 255.0f,
                Theme.DarkGreyLineColor1.B / 255.0f,
                1.0f);
            GL.Clear(GL.ColorBufferBit);
            glfwSwapBuffers(window);
        }

        private unsafe void SetWindowIcon()
        {
            var icon16 = TgaFile.LoadFromResource($"FamiStudio.Resources.FamiStudio_16.tga", true);
            var icon24 = TgaFile.LoadFromResource($"FamiStudio.Resources.FamiStudio_24.tga", true);
            var icon32 = TgaFile.LoadFromResource($"FamiStudio.Resources.FamiStudio_32.tga", true);
            var icon64 = TgaFile.LoadFromResource($"FamiStudio.Resources.FamiStudio_64.tga", true);
            var images = new GLFWimage[4];

            fixed (int* p16 = &icon16.Data[0], 
                        p24 = &icon24.Data[0], 
                        p32 = &icon32.Data[0], 
                        p64 = &icon64.Data[0])
            {
                images[0].width  = 16;
                images[0].height = 16;
                images[0].pixels = (IntPtr)p16;
                images[1].width  = 24;
                images[1].height = 24;
                images[1].pixels = (IntPtr)p24;
                images[2].width  = 32;
                images[2].height = 32;
                images[2].pixels = (IntPtr)p32;
                images[3].width  = 64;
                images[3].height = 64;
                images[3].pixels = (IntPtr)p64;

                fixed(GLFWimage* pi = &images[0])
                {
                    glfwSetWindowIcon(window, 4, (IntPtr)pi);
                }
            }
        }

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

        public void RunEventLoop(bool allowSleep = false)
        {
            RunIteration(allowSleep);
        }

        protected void RenderFrameAndSwapBuffer(bool force = false)
        {
            if (force)
                controls.MarkDirty();

            if (controls.AnyControlNeedsRedraw() && famistudio.Project != null)
            {
                controls.Redraw();
                glfwSwapBuffers(window);
            }
        }

        public void CaptureMouse(Control ctrl)
        {
            if (lastButtonPress >= 0)
            {
                Debug.Assert(captureControl == null);

                captureButton = lastButtonPress;
                captureControl = ctrl;
            }
        }

        public void ReleaseMouse()
        {
            if (captureControl != null)
            {
                captureControl = null;
            }
        }

        public Point PointToClient(Point p)
        {
            glfwGetWindowPos(window, out var px, out var py);
            return new Point(p.X - px, p.Y - py);
        }

        public Point PointToScreen(Point p)
        {
            glfwGetWindowPos(window, out var px, out var py);
            return new Point(p.X + px, p.Y + py);
        }

        public Point PointToClient(Control ctrl, Point p)
        {
            p = PointToClient(p);
            return new Point(p.X - ctrl.Left, p.Y - ctrl.Top);
        }

        public Point PointToScreen(Control ctrl, Point p)
        {
            p = new Point(p.X + ctrl.Left, p.Y + ctrl.Top);
            return PointToScreen(p);
        }

        // https://github.com/glfw/glfw/issues/1630
        private int FixKeyboardMods(int mods, int key, int action)
        {
            if (Platform.IsLinux)
            {
                if (key == GLFW_KEY_LEFT_SHIFT || key == GLFW_KEY_RIGHT_SHIFT)
                {
                    return (action == GLFW_RELEASE) ? mods & (~GLFW_MOD_SHIFT) : mods | GLFW_MOD_SHIFT;
                }
                if (key == GLFW_KEY_LEFT_CONTROL || key == GLFW_KEY_RIGHT_CONTROL)
                {
                    return (action == GLFW_RELEASE) ? mods & (~GLFW_MOD_CONTROL) : mods | GLFW_MOD_CONTROL;
                }
                if (key == GLFW_KEY_LEFT_ALT || key == GLFW_KEY_RIGHT_ALT)
                {
                    return (action == GLFW_RELEASE) ? mods & (~GLFW_MOD_ALT) : mods | GLFW_MOD_ALT;
                }
                if (key == GLFW_KEY_LEFT_SUPER || key == GLFW_KEY_RIGHT_SUPER)
                {
                    return (action == GLFW_RELEASE) ? mods & (~GLFW_MOD_SUPER) : mods | GLFW_MOD_SUPER;
                }
            }

            return mods;
        }

        private void ErrorCallback(int error, string description)
        {
            Debug.WriteLine($"*** GLFW Error code {error}, {description}.");
        }

        private void WindowSizeCallback(IntPtr window, int width, int height)
        {
            Debug.WriteLine($"*** SIZE {width}, {height}.");

            RefreshLayout();
        }

        private void WindowCloseCallback(IntPtr window)
        {
            if (IsAsyncDialogInProgress)
            {
                Platform.Beep();
                return;
            }

            if (famistudio.TryClosing())
            {
                Quit();
            }
        }

        public static void GLFWToWindow(double dx, double dy, out int x, out int y)
        {
            if (Platform.IsMacOS)
            {
                x = (int)Math.Round(dx * DpiScaling.Window);
                y = (int)Math.Round(dy * DpiScaling.Window);
            }
            else
            {
                x = (int)dx;
                y = (int)dy;
            }
        }

        private void WindowRefreshCallback(IntPtr window)
        {
            Debug.WriteLine($"WINDOW REFRESH!");
            MarkDirty();
        }

        private void MouseButtonCallback(IntPtr window, int button, int action, int mods)
        {
            Debug.WriteLine($"BUTTON! Button={button}, Action={action}, Mods={mods}");

            if (action == GLFW_PRESS)
            {
                if (captureControl != null)
                    return;

                var ctrl = controls.GetControlAtCoord(lastCursorX, lastCursorY, out int cx, out int cy);
                
                lastButtonPress = button; 

                // Double click emulation.
                var now = glfwGetTime();
                var delay = now - lastClickTime;

                var doubleClick = 
                    button == lastClickButton &&
                    delay <= Platform.DoubleClickTime &&
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
                    if (ctrl != ContextMenu)
                        controls.HideContextMenu();

                    if (doubleClick)
                    {
                        Debug.WriteLine($"DOUBLE CLICK!");

                        ctrl.MouseDoubleClick(new MouseEventArgs(MakeButtonFlags(button), cx, cy));
                    }
                    else
                    {
                        var ex = new MouseEventArgs(MakeButtonFlags(button), cx, cy);
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
                Control ctrl = null;

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

                if (button == captureButton)
                    ReleaseMouse();

                if (ctrl != null)
                {
                    if (button == GLFW_MOUSE_BUTTON_RIGHT)
                        ConditionalEmitDelayedRightClick(true, true, ctrl);

                    if (ctrl != ContextMenu)
                        controls.HideContextMenu();

                    ctrl.MouseUp(new MouseEventArgs(MakeButtonFlags(button), cx, cy));
                }
            }
        }

        private void CursorPosCallback(IntPtr window, double xpos, double ypos)
        {
            //Debug.WriteLine($"POS! X={xpos}, Y={ypos}");

            GLFWToWindow(xpos, ypos, out lastCursorX, out lastCursorY);

            int cx;
            int cy;
            Control ctrl = null;
            Control hover = null;

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

            var buttons = MakeButtonFlags();
            var e = new MouseEventArgs(buttons, cx, cy);

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

                const float Multiplier = Platform.IsWindows ? 30.0f : 2.0f;

                // MATTT : Test this on all platforms.
                var scrollX = -(float)xoffset * Settings.TrackPadMoveSensitity * Multiplier;
                var scrollY =  (float)yoffset * Settings.TrackPadMoveSensitity * Multiplier;

                if (Settings.ReverseTrackPadX) scrollX *= -1.0f;
                if (Settings.ReverseTrackPadY) scrollY *= -1.0f;

                Debug.WriteLine($"SCALED! X={scrollX}, Y={scrollY}");

                var buttons = MakeButtonFlags();

                if (scrollY != 0.0f)
                    ctrl.MouseWheel(new MouseEventArgs(buttons, cx, cy, 0, scrollY));
                if (scrollX != 0.0f)
                    ctrl.MouseHorizontalWheel(new MouseEventArgs(buttons, cx, cy, scrollX));
            }
        }
        
        private void SendKeyUpOrDown(Control ctrl, KeyEventArgs e, bool down)
        {
            if (down)
                ctrl.KeyDown(e);
            else
                ctrl.KeyUp(e);
        }

        private void KeyCallback(IntPtr window, int key, int scancode, int action, int mods)
        {
            mods = FixKeyboardMods(mods, key, action);

            Debug.WriteLine($"KEY! Key = {(Keys)key}, Scancode = {scancode}, Action = {action}, Mods = {mods}");

            modifiers.Set(mods);

            var down = action == GLFW_PRESS || action == GLFW_REPEAT;
            var e = new KeyEventArgs((Keys)key, mods, action == GLFW_REPEAT, scancode);
            
            if (controls.IsContextMenuActive)
            {
                SendKeyUpOrDown(controls.ContextMenu, e, down);
            }
            else if (controls.IsDialogActive)
            {
                SendKeyUpOrDown(controls.TopDialog, e, down);
            }
            else
            {
                if (down)
                    famistudio.KeyDown(e);
                else
                    famistudio.KeyUp(e);

                foreach (var ctrl in controls.Controls)
                    SendKeyUpOrDown(ctrl, e, down);
            }
        }

        private void CharCallback(IntPtr window, uint codepoint)
        {
            Debug.WriteLine($"CHAR! Key = {codepoint}, Char = {((char)codepoint).ToString()}");

            var e = new CharEventArgs((char)codepoint, modifiers.Modifiers);

            if (controls.IsContextMenuActive)
            {
                controls.ContextMenu.Char(e);
            }
            else if (controls.IsDialogActive)
            {
                controls.TopDialog.Char(e);
            }
            else
            {
                foreach (var ctrl in controls.Controls)
                    ctrl.Char(e);
            }
        }

        private void CharModsCallback(IntPtr window, uint codepoint, int mods)
        {
            Debug.WriteLine($"CHAR MODS! CodePoint = {codepoint}, Mods = {mods}");
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

        private Size GetWindowSizeInternal()
        {
            glfwGetWindowSize(window, out var tw, out var th);
            GLFWToWindow(tw, th, out var w, out var h);
            return new Size(w, h);
        }

        private Point GetClientCursorPosInternal()
        {
            glfwGetCursorPos(window, out var dx, out var dy);
            GLFWToWindow(dx, dy, out var x, out var y);
            return new Point(x, y);
        }

        private Point GetScreenCursorPosInternal()
        {
            return PointToScreen(GetClientCursorPosInternal());
        }

        private int MakeButtonFlags(int button)
        {
            return 1 << button;
        }

        private int MakeButtonFlags()
        {
            var flags = 0;
            if (glfwGetMouseButton(window, GLFW_MOUSE_BUTTON_LEFT)   != 0) flags |= MouseEventArgs.ButtonLeft;
            if (glfwGetMouseButton(window, GLFW_MOUSE_BUTTON_RIGHT)  != 0) flags |= MouseEventArgs.ButtonRight;
            if (glfwGetMouseButton(window, GLFW_MOUSE_BUTTON_MIDDLE) != 0) flags |= MouseEventArgs.ButtonMiddle;
            return flags;
        }

        protected void DelayRightClick(Control ctrl, MouseEventArgs e)
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

        protected void ConditionalEmitDelayedRightClick(bool checkTime = true, bool forceClear = false, Control checkCtrl = null)
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

        protected bool ShouldIgnoreMouseMoveBecauseOfDelayedRightClick(MouseEventArgs e, int x, int y, Control checkCtrl)
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

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hwnd, int nIndex, IntPtr newProc);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hwnd, int nIndex, WndProcDelegate newProc);
        
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLong(IntPtr hwnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr proc, IntPtr hwnd, int msg, int wparam, int lparam);

        [DllImport("user32.dll")]
        static extern IntPtr SetTimer(IntPtr hwnd, IntPtr evt, uint elapse, TimerProcDelegate func);
        
        [DllImport("user32.dll")]
        static extern IntPtr KillTimer(IntPtr hwnd, IntPtr evt);

        private const int GWL_WNDPROC = -4;
        private const int WM_ENTERSIZEMOVE = 0x231;
        private const int WM_EXITSIZEMOVE = 0x232;
        private const int WM_TIMER = 0x113;

        private delegate IntPtr WndProcDelegate(IntPtr hwnd, int msg, int wparam, int lparam);
        private delegate void TimerProcDelegate(IntPtr hwnd, int msg, int id, int time);

        private TimerProcDelegate timerProc;
        private WndProcDelegate newWndProc;
        private IntPtr  oldWndProc;
        private bool inTimerProc;

        private void SubclassWindow(bool enable)
        {
            if (Platform.IsWindows)
            {
                var hwnd = Handle;

                if (enable)
                {
                    Debug.Assert(newWndProc == null);
                    newWndProc = new WndProcDelegate(WndProc);
                    oldWndProc = GetWindowLong(hwnd, GWL_WNDPROC);
                    SetWindowLong(hwnd, GWL_WNDPROC, newWndProc);
                }
                else
                {
                    Debug.Assert(oldWndProc != IntPtr.Zero);
                    SetWindowLong(hwnd, GWL_WNDPROC, oldWndProc);
                }
            }
        }

        private void TimerProc(IntPtr hwnd, int msg, int id, int time)
        {
            // Prevent recursion.
            if (!inTimerProc)
            {
                inTimerProc = true;
                RunIteration();
                inTimerProc = false;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, int wparam, int lparam)
        {
            if (msg == WM_ENTERSIZEMOVE) // WM_ENTERSIZEMOVE 
            {
                Debug.Assert(timerProc == null);
                timerProc = new TimerProcDelegate(TimerProc);
                SetTimer(hwnd, (IntPtr)1, 4, timerProc);
            }
            else if (msg == WM_EXITSIZEMOVE) // WM_EXITSIZEMOVE
            {
                Debug.Assert(timerProc != null);
                timerProc = null;
                KillTimer(hwnd, (IntPtr)1);
            }

            return CallWindowProc(oldWndProc, hwnd, msg, wparam, lparam);
        }

        [DllImport("DwmApi")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

        protected void EnableWindowsDarkTheme()
        {
            if (Platform.IsWindows)
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

        public void Quit()
        {
            // MATTT : Retest this on macos.
            quit = true;

            // Need to disable all event processing here. When quitting we may have
            // a NULL project so the rendering and event processing code might be
            // unstable. 
            SubclassWindow(false);
            UnbindGLFWCallbacks();

            // HACK : RtMidi still has a thread running and it seems
            // to prevent the app from quitting. Will investigate more
            // later.

            //Process.GetCurrentProcess().Kill();
        }

        public void Refresh()
        {
            RenderFrameAndSwapBuffer(true);
        }

        public void RefreshLayout()
        {
            var size = GetWindowSizeInternal();
            controls.Resize(size.Width, size.Height);
            controls.MarkDirty();
        }

        public void MarkDirty()
        {
            controls.MarkDirty();
        }

        public ModifierKeys GetModifierKeys()
        {
            return modifiers;
        }

        public Point GetCursorPosition()
        {
            // Pretend the mouse is fixed when a context menu is active.
            return controls.IsContextMenuActive ? contextMenuPoint : GetScreenCursorPosInternal(); // MATTT : Is this screen or client.
        }

        public void RefreshCursor()
        {
            var pt = GetClientCursorPosInternal();
            RefreshCursor(controls.GetControlAtCoord(pt.X, pt.Y, out _, out _));
        }

        private void RefreshCursor(Control ctrl)
        {
            if (captureControl != null && captureControl != ctrl)
                return;

            glfwSetCursor(window, ctrl != null ? ctrl.Cursor : Cursors.Default);
        }

        public void SetActiveControl(Control ctrl, bool animate = true)
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

        public bool IsKeyDown(Keys k)
        {
            return glfwGetKey(window, (int)k) == GLFW_PRESS;
        }

#if FAMISTUDIO_MACOS
        private IntPtr selType = MacUtils.SelRegisterName("type");
        private IntPtr selMagnification = MacUtils.SelRegisterName("magnification");
        private IntPtr selNextEventMatchingMask = MacUtils.SelRegisterName("nextEventMatchingMask:untilDate:inMode:dequeue:");
        private IntPtr nsLoopMode;

        private void ProcessSpecialEvents()
        {
            if (!Settings.TrackPadControls)
                return;

            if (nsLoopMode == IntPtr.Zero)
                nsLoopMode = MacUtils.GetStringConstant(MacUtils.FoundationLibrary, "NSDefaultRunLoopMode");

            while (true)
            {
                const int NSEventTypeMagnify = 30;

                var e = MacUtils.SendIntPtr(MacUtils.NSApplication, selNextEventMatchingMask, 1 << NSEventTypeMagnify, IntPtr.Zero, nsLoopMode, true);

                if (e == IntPtr.Zero)
                    return;

                var type = MacUtils.SendInt(e, selType);

                if (type == NSEventTypeMagnify)
                {
                    var magnification = (float)MacUtils.SendFloat(e, selMagnification);

                    if (Math.Sign(magnification) != Math.Sign(magnificationAccum))
                        magnificationAccum = 0;

                    magnificationAccum += magnification;

                    float threshold = 1.0f / (float)Utils.Clamp(Settings.TrackPadZoomSensitity, 1, 16);

                    if (Math.Abs(magnificationAccum) > threshold)
                    {
                        var pt = GetClientCursorPosInternal();
                        var sz = GetWindowSizeInternal();

                        // We get notified for clicks in the title back and stuff.
                        if (pt.X < 0 || pt.Y < 0 || pt.X >= sz.Width || pt.Y >= sz.Height)
                            continue;

                        Debug.WriteLine($"PINCH ZOOM! {magnificationAccum} {pt.X} {pt.Y}");

                        var ctrl = controls.GetControlAtCoord(pt.X, pt.Y, out int x, out int y);

                        modifiers.SetForced(GLFW_MOD_CONTROL);
                        ctrl.MouseWheel(new MouseEventArgs(0, pt.X, pt.Y, 0, magnificationAccum));
                        modifiers.SetForced(0);

                        magnificationAccum = 0;
                    }
                }
            }
        }
#else
        void ProcessSpecialEvents()
        {
        }
#endif

        private void ProcessEvents()
        {
            glfwPollEvents();
        }

        private void RunIteration(bool allowSleep = true)
        {
            ProcessSpecialEvents();
            ProcessEvents();
            Tick();
            RenderFrameAndSwapBuffer();
            ConditionalEmitDelayedRightClick();

            if (allowSleep)
            {
                // Always sleep a bit, even if we rendered something. This handles cases where people
                // turn off vsync and we end up eating 100% CPU. 
                System.Threading.Thread.Sleep(4);
            }
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
