using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    public partial class FamiStudioWindow
    {
        private const double DelayedRightClickTime = 0.25;
        private const int    DelayedRightClickPixelTolerance = 2;

        private IntPtr window; // GLFW window.

        private FamiStudio famistudio;
        private FamiStudioContainer container;
        private Graphics graphics;
        private Fonts fonts;
        private bool dirty = true;

        public FamiStudio FamiStudio => famistudio;
        public Toolbar ToolBar => container.ToolBar;
        public Sequencer Sequencer => container.Sequencer;
        public PianoRoll PianoRoll => container.PianoRoll;
        public ProjectExplorer ProjectExplorer => container.ProjectExplorer;
        public QuickAccessBar QuickAccessBar => container.QuickAccessBar;
        public MobilePiano MobilePiano => container.MobilePiano;
        public ContextMenu ContextMenu => container.ContextMenu;
        public Control ActiveControl => activeControl;
        public Graphics Graphics => graphics;
        public Fonts Fonts => fonts;
        public static FamiStudioWindow Instance { get; private set; }

        public Size Size => GetWindowSizeInternal();
        public int Width => GetWindowSizeInternal().Width;
        public int Height => GetWindowSizeInternal().Height;
        public string Text { set => glfwSetWindowTitle(window, value); }
        public bool IsLandscape => true;
        public bool IsAsyncDialogInProgress => container.IsDialogActive;
        public bool IsContextMenuActive => container.IsContextMenuActive;
        public bool MobilePianoVisible { get => false; set => value = false; }
        public Point LastMousePosition => new Point(lastCursorX, lastCursorY);
        public Point LastContextMenuPosition => ScreenToWindow(contextMenuPoint);
        public IntPtr Handle => glfwGetNativeWindow(window);

        private Control activeControl = null;
        private Control captureControl = null;
        private Control hoverControl = null;
        private int captureCookie;
        private int captureButton = -1;
        private int lastButtonPress = -1;
        private Point contextMenuPoint = Point.Empty;
        private double lastTickTime = -1.0f;
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
        private PointerEventArgs delayedRightClickArgs = null;
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
            Instance = this;

            PlatformWindowInitialize();
            InitializeGL();
            SetWindowIcon();
            InitialFrameBufferClear();
            CreateGraphics();
            BindGLFWCallbacks();
            CreateContainer();
            RefreshLayout();
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

        private void InitializeGL()
        {
            glfwGetWindowContentScale(window, out var scaling, out _);

            GL.StaticInitialize(glfwGetProcAddress, false);
            Cursors.Initialize(scaling);
            DpiScaling.Initialize(scaling);
        }

        private void CreateGraphics()
        {
            graphics = new Graphics();
            fonts = new Fonts(graphics);
        }

        private void CreateContainer()
        {
            container = new FamiStudioContainer(this);
            activeControl = PianoRoll;
        }

        private void DestroyGraphics()
        {
            Utils.DisposeAndNullify(ref fonts);
            Utils.DisposeAndNullify(ref graphics);
        }

        public static unsafe FamiStudioWindow CreateWindow(FamiStudio fs)
        {
            glfwWindowHint(GLFW_CLIENT_API, GLFW_OPENGL_API);
            glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
            glfwWindowHint(GLFW_MAXIMIZED, 1);
            glfwWindowHint(GLFW_RESIZABLE, 1);
            glfwWindowHint(GLFW_DOUBLEBUFFER, 1);
            glfwWindowHint(GLFW_COCOA_RETINA_FRAMEBUFFER, 1);
            glfwWindowHint(GLFW_DEPTH_BITS, 16);
            glfwWindowHint(GLFW_STENCIL_BITS, 0);
            glfwWindowHintString(GLFW_WAYLAND_APP_ID, "famistudio");
        #if FAMISTUDIO_MACOS
            glfwWindowHint (GLFW_OPENGL_FORWARD_COMPAT, 1);
        #endif
        #if DEBUG
            glfwWindowHint(GLFW_OPENGL_DEBUG_CONTEXT, 1);
        #endif

            IntPtr window = IntPtr.Zero;

            // Try 3.3 core first, much more standard. Then try all the way down to 3.0, in compatiblity profile.
            for (var minor = 3; minor >= 0 && window == IntPtr.Zero; minor--)
            {
                glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, minor);
                glfwWindowHint(GLFW_OPENGL_PROFILE, minor == 3 ? GLFW_OPENGL_CORE_PROFILE : GLFW_OPENGL_ANY_PROFILE);

                window = glfwCreateWindow(1280, 720, "FamiStudio", IntPtr.Zero, IntPtr.Zero);
            }

            if (window == IntPtr.Zero)
            {
                glfwTerminate();
                return null;
            }

            glfwMakeContextCurrent(window);
            glfwSetWindowSizeLimits(window, 400, 300, GLFW_DONT_CARE, GLFW_DONT_CARE);
            glfwSwapInterval(1);

            return new FamiStudioWindow(fs, window);
        }

        private void DestroyWindow()
        {
            DestroyGraphics();
            glfwDestroyWindow(window);
        }

        private void InitialFrameBufferClear()
        {
            var size = GetWindowSizeInternal();
            GL.Disable(GL.DepthTest);
            GL.Viewport(0, 0, size.Width, size.Height);
            GL.ClearColor(
                Theme.DarkGreyColor1.R / 255.0f,
                Theme.DarkGreyColor1.G / 255.0f,
                Theme.DarkGreyColor1.B / 255.0f,
                1.0f);
            GL.Clear(GL.ColorBufferBit);
            glfwSwapBuffers(window);
        }

        private unsafe void SetWindowIcon()
        {
            var icon16 = TgaFile.LoadFromResource($"FamiStudio.Resources.Icons.FamiStudio_16.tga", true);
            var icon24 = TgaFile.LoadFromResource($"FamiStudio.Resources.Icons.FamiStudio_24.tga", true);
            var icon32 = TgaFile.LoadFromResource($"FamiStudio.Resources.Icons.FamiStudio_32.tga", true);
            var icon64 = TgaFile.LoadFromResource($"FamiStudio.Resources.Icons.FamiStudio_64.tga", true);
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
            var tickTime = glfwGetTime();

            if (lastTickTime < 0.0)
                lastTickTime = tickTime;

            var deltaTime = (float)Math.Min(0.25f, (float)(tickTime - lastTickTime));

            if (!IsAsyncDialogInProgress)
                famistudio.Tick(deltaTime);

            container.TickWithChildren(deltaTime);

            lastTickTime = tickTime;
        }

        public void RunEventLoop(bool allowSleep = false)
        {
            RunIteration(allowSleep);
        }

        protected void RenderFrameAndSwapBuffer(bool force = false)
        {
            if (!quit && (dirty || force))
            {
                Debug.Assert(!DpiScaling.ForceUnitScaling);

                var rect = new Rectangle(Point.Empty, Size);
                var clearColor = Theme.DarkGreyColor2;

                graphics.BeginDrawFrame(rect, true, clearColor);
                container.Render(graphics);
                graphics.EndDrawFrame();

                glfwSwapBuffers(window);

                dirty = false;
            }
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        public int CapturePointer(Control ctrl)
        {
            if (lastButtonPress >= 0)
            {
                Debug.Assert(captureControl == null);

                captureButton = lastButtonPress;
                captureControl = ctrl;
            }

            return ++captureCookie;
        }

        public void ReleasePointer()
        {
            if (captureControl != null)
            {
                captureControl = null;
            }
        }

        public bool CheckCaptureCookie(int cookie)
        {
            return captureCookie == cookie;
        }

        public Point ScreenToWindow(Point p)
        {
            glfwGetWindowPos(window, out var px, out var py);
            return new Point(p.X - px, p.Y - py);
        }

        public Point WindowToScreen(Point p)
        {
            glfwGetWindowPos(window, out var px, out var py);
            return new Point(p.X + px, p.Y + py);
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

        private void ConditionalUpdateLastCursorPosition()
        {
            // On MacOS, the app isnt notified of mouse movement when the window
            // isnt in focus. Which mean we cant rely on the last position, we have
            // to fetch it every time.
            if (Platform.IsMacOS)
            {
                var pt = GetClientCursorPosInternal();
                lastCursorX = pt.X;
                lastCursorY = pt.Y;
            }
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
            if (Platform.IsMacOS && DpiScaling.IsInitialized)
            {
                Debug.Assert(!DpiScaling.ForceUnitScaling);

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
            if (quit)
                return;
            
            Debug.WriteLine($"BUTTON! Button={button}, Action={action}, Mods={mods}");
            
            ConditionalUpdateLastCursorPosition();

            if (action == GLFW_PRESS)
            {
                if (captureControl != null)
                    return;

                var ctrl = container.GetControlAt(lastCursorX, lastCursorY, out int cx, out int cy);
                container.ConditionalHideContextMenu(ctrl);

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

                    if (doubleClick)
                    {
                        // We dont support anything other and double-left click.
                        if (button == GLFW_MOUSE_BUTTON_LEFT)
                        {
                            Debug.WriteLine($"DOUBLE CLICK!");

                            ctrl.SendMouseDoubleClick(new PointerEventArgs(MakeButtonFlags(button), cx, cy));
                        }
                    }
                    else
                    {
                        var ex = new PointerEventArgs(MakeButtonFlags(button), cx, cy);
                        
                        ctrl.GrabDialogFocus();
                        var dlgBefore = TopDialog;
                        ctrl.SendPointerDown(ex);
                        var dlgAfter = TopDialog;

                        if (ex.IsRightClickDelayed)
                        {
                            DelayRightClick(ctrl, ex);
                        }
                        else if (dlgAfter != dlgBefore)
                        {
                            // If a mouse down happens to pop a dialog, send a mouse up immediate. Clears
                            // the "press" status on button. The real fix would be to have a focus system 
                            // that works on more than just dialogs. LostFocus would then clear the status.
                            ctrl.SendPointerUp(ex);
                        }
                    }
                }
            }
            else if (action == GLFW_RELEASE)
            {
                int cx;
                int cy;
                Control ctrl;

                if (captureControl != null)
                {
                    ctrl = captureControl;
                    cx = lastCursorX - ctrl.WindowPosition.X;
                    cy = lastCursorY - ctrl.WindowPosition.Y;
                }
                else
                {
                    ctrl = container.GetControlAt(lastCursorX, lastCursorY, out cx, out cy);
                }

                if (button == captureButton)
                    ReleasePointer();

                if (ctrl != null)
                {
                    if (button == GLFW_MOUSE_BUTTON_RIGHT)
                        ConditionalEmitDelayedRightClick(true, true, ctrl);

                    container.ConditionalHideContextMenu(ctrl);
                    ctrl.SendPointerUp(new PointerEventArgs(MakeButtonFlags(button), cx, cy));
                }
            }
        }

        private void CursorPosCallback(IntPtr window, double xpos, double ypos)
        {
            if (quit)
                return;

            //Debug.WriteLine($"POS! X={xpos}, Y={ypos}");

            GLFWToWindow(xpos, ypos, out lastCursorX, out lastCursorY);

            int cx;
            int cy;
            Control ctrl = null;
            Control hover = null;

            if (captureControl != null)
            {
                ctrl = captureControl;
                cx = lastCursorX - ctrl.WindowPosition.X;
                cy = lastCursorY - ctrl.WindowPosition.Y;
                hover = container.GetControlAt(lastCursorX, lastCursorY, out _, out _);
            }
            else
            {
                ctrl = container.GetControlAt(lastCursorX, lastCursorY, out cx, out cy);
                hover = ctrl;
            }

            var buttons = MakeButtonFlags();
            var e = new PointerEventArgs(buttons, cx, cy);

            if (ShouldIgnoreMouseMoveBecauseOfDelayedRightClick(e, cx, cy, ctrl))
                return;

            ConditionalEmitDelayedRightClick(false, true, ctrl);

            // Dont forward move mouse when a context menu is active.
            if (ctrl != null && (!container.IsContextMenuActive || ctrl == ContextMenu))
            {
                ctrl.SendPointerMove(e);
                RefreshCursor(ctrl);
            }

            if (hover != hoverControl)
            {
                if (hoverControl != null && (!container.IsContextMenuActive || hoverControl == ContextMenu))
                    hoverControl.SendPointerLeave(EventArgs.Empty);

                hoverControl = hover;
                
                if (hoverControl != null && (!container.IsContextMenuActive || hoverControl == ContextMenu))
                    hoverControl.SendPointerEnter(EventArgs.Empty);
            }
        }

        private void CursorEnterCallback(IntPtr window, int entered)
        {
            if (quit)
                return;

            Debug.WriteLine($"ENTER! entered={entered}");

            if (entered == 0)
            {
                if (hoverControl != null && (!container.IsContextMenuActive || hoverControl == ContextMenu))
                {
                    hoverControl.SendPointerLeave(EventArgs.Empty);
                    hoverControl = null;
                }
            }
        }

        private void ClearHoverControl()
        {
            if (hoverControl != null)
            {
                hoverControl.SendPointerLeave(EventArgs.Empty);
                hoverControl = null;
            }
        }

        private void ScrollCallback(IntPtr window, double xoffset, double yoffset)
        {
            if (quit)
                return;

            Debug.WriteLine($"SCROLL! X={xoffset}, Y={yoffset}");
            
            ConditionalUpdateLastCursorPosition();

            var ctrl = container.GetControlAt(lastCursorX, lastCursorY, out int cx, out int cy);
            if (ctrl != null)
            {
                SetActiveControl(ctrl);
                container.ConditionalHideContextMenu(ctrl);

                const float Multiplier = Platform.IsWindows ? 10.0f : 2.0f;

                var scrollX = -(float)xoffset * Settings.TrackPadMoveSensitity * Multiplier;
                var scrollY =  (float)yoffset * Settings.TrackPadMoveSensitity * Multiplier;

                if (Settings.ReverseTrackPadX) scrollX *= -1.0f;
                if (Settings.ReverseTrackPadY) scrollY *= -1.0f;

                Debug.WriteLine($"SCALED! X={scrollX}, Y={scrollY}");

                var buttons = MakeButtonFlags();

                if (scrollY != 0.0f)
                    ctrl.SendMouseWheel(new PointerEventArgs(buttons, cx, cy, false, 0, scrollY));
                if (scrollX != 0.0f)
                    ctrl.SendMouseHorizontalWheel(new PointerEventArgs(buttons, cx, cy, false, scrollX));
            }
        }
        
        private void SendKeyUpOrDown(Control ctrl, KeyEventArgs e, bool down)
        {
            if (down)
                ctrl.SendKeyDown(e);
            else
                ctrl.SendKeyUp(e);
        }

        private void KeyCallback(IntPtr window, int key, int scancode, int action, int mods)
        {
            if (quit)
                return;

            mods = FixKeyboardMods(mods, key, action);
            modifiers.Set(mods);

            //Debug.WriteLine($"KEY! Key = {(Keys)key}, Scancode = {scancode} ({Platform.ScancodeToString(scancode)}), Action = {action}, Mods = {mods}");

            var controls = container.GetControlsForKeyboard(out var mainFamistudioControl);
            var down = action == GLFW_PRESS || action == GLFW_REPEAT;
            var e = new KeyEventArgs((Keys)key, modifiers, action == GLFW_REPEAT, scancode);

            if (mainFamistudioControl)
            {
                if (down)
                    famistudio.KeyDown(e);
                else
                    famistudio.KeyUp(e);
            }

            if (!quit)
            {
                foreach (var ctrl in controls)
                    SendKeyUpOrDown(ctrl, e, down);
            }
        }

        private void CharCallback(IntPtr window, uint codepoint)
        {
            if (quit)
                return;

            //Debug.WriteLine($"CHAR! Key = {codepoint}, Char = {((char)codepoint).ToString()}");

            var controls = container.GetControlsForKeyboard(out _);
            var e = new CharEventArgs((char)codepoint, modifiers);

            foreach (var ctrl in controls)
                ctrl.SendChar(e);
        }

        private void CharModsCallback(IntPtr window, uint codepoint, int mods)
        {
            //Debug.WriteLine($"CHAR MODS! CodePoint = {codepoint}, Mods = {mods}");
        }

        private unsafe void DropCallback(IntPtr window, int count, IntPtr paths)
        {
            if (count > 0 && !container.IsDialogActive)
            {
                IntPtr ptr;

                // There has to be a more generic way to do this? 
                if (IntPtr.Size == 4)
                    ptr = new IntPtr(*(int*)paths.ToPointer());
                else
                    ptr = new IntPtr(*(long*)paths.ToPointer());

                var filename = Utils.PtrToStringUTF8(ptr);

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
            return WindowToScreen(GetClientCursorPosInternal());
        }

        private int MakeButtonFlags(int button)
        {
            return 1 << button;
        }

        private int MakeButtonFlags()
        {
            var flags = 0;
            if (glfwGetMouseButton(window, GLFW_MOUSE_BUTTON_LEFT)   != 0) flags |= PointerEventArgs.ButtonLeft;
            if (glfwGetMouseButton(window, GLFW_MOUSE_BUTTON_RIGHT)  != 0) flags |= PointerEventArgs.ButtonRight;
            if (glfwGetMouseButton(window, GLFW_MOUSE_BUTTON_MIDDLE) != 0) flags |= PointerEventArgs.ButtonMiddle;
            return flags;
        }

        protected void DelayRightClick(Control ctrl, PointerEventArgs e)
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
                delayedRightClickControl.SendPointerDownDelayed(delayedRightClickArgs);
                clear = true;
            }

            if (clear)
                ClearDelayedRightClick();
        }

        protected bool ShouldIgnoreMouseMoveBecauseOfDelayedRightClick(PointerEventArgs e, int x, int y, Control checkCtrl)
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

        public void Quit()
        {
            quit = true;

            // Need to disable all event processing here. When quitting we may have
            // a NULL project so the rendering and event processing code might be
            // unstable. 
            PlatformWindowShudown();
            UnbindGLFWCallbacks();
        }

        public void Refresh()
        {
            RenderFrameAndSwapBuffer(true);
        }

        public void RefreshLayout()
        {
            var size = GetWindowSizeInternal();
            container.Resize(size.Width, size.Height, true, true);
            MarkDirty();
        }

        public ModifierKeys GetModifierKeys()
        {
            return modifiers;
        }

        public Point GetCursorPosition()
        {
            // Pretend the mouse is fixed when a context menu is active.
            return container.IsContextMenuActive ? contextMenuPoint : GetScreenCursorPosInternal();
        }

        public void RefreshCursor()
        {
            var pt = GetClientCursorPosInternal();
            RefreshCursor(container.GetControlAt(pt.X, pt.Y, out _, out _));
        }

        private void RefreshCursor(Control ctrl)
        {
            if (captureControl != null && captureControl != ctrl)
                return;

            // Look up the chain, look for any control that might have requested a non-default cursor.
            var cursor = Cursors.Default;

            do
            {
                if (ctrl == null)
                {
                    break;
                }
                cursor = ctrl.Cursor;
                ctrl = ctrl.ParentContainer;
            }
            while (cursor == Cursors.Default);

            glfwSetCursor(window, cursor);
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

        public void ShowContextMenuAsync(ContextMenuOption[] options)
        {
            contextMenuPoint = WindowToScreen(new Point(lastCursorX, lastCursorY));
            container.ShowContextMenu(lastCursorX, lastCursorY, options);
            RefreshCursor(container.ContextMenu);
            ClearHoverControl();
        }

        public void HideContextMenu()
        {
            container.HideContextMenu();
            ClearHoverControl();
        }

        public void InitDialog(Dialog dialog)
        {
            container.InitDialog(dialog);
        }

        public void PushDialog(Dialog dialog)
		{
            ClearHoverControl();
            container.PushDialog(dialog);
        }

        public void PopDialog(Dialog dialog)
        {
            ClearHoverControl();
            container.PopDialog(dialog);
        }

        public Dialog TopDialog => container.TopDialog;

        public void ShowToast(string text, bool longDuration = false, Action click = null)
        {
            container.ShowToast(text, longDuration, click);
        }

        public bool IsKeyDown(Keys k)
        {
            return glfwGetKey(window, (int)k) == GLFW_PRESS;
        }

        private void ProcessEvents()
        {
            glfwPollEvents();
        }

        private void RunIteration(bool allowSleep = true)
        {
            ProcessPlatformEvents();
            ProcessEvents();

            if (!quit)
            { 
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
        }

        public void Run()
        {
            while (!quit)
            {
                RunIteration();
            }

            DestroyWindow();
        }
    }
}
