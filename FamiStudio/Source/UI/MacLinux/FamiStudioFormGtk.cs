using Gtk;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;

using Settings = FamiStudio.Settings;

namespace FamiStudio
{
    public class FamiStudioForm : GLWindow
    {
        private static FamiStudioForm instance;
        private FamiStudio famistudio;
        private FamiStudioControls controls;

        public string Text { get => Title; set => Title = value; }
        public FamiStudio FamiStudio => famistudio;

        public Toolbar ToolBar => controls.ToolBar;
        public Sequencer Sequencer => controls.Sequencer;
        public PianoRoll PianoRoll => controls.PianoRoll;
        public ProjectExplorer ProjectExplorer => controls.ProjectExplorer;
        public QuickAccessBar QuickAccessBar => controls.QuickAccessBar;
        public GLControl ActiveControl => null;
        public static FamiStudioForm Instance => instance;

        public bool IsLandscape => true;
        public bool IsAsyncDialogInProgress => false;
        public Size Size => Size.Empty;

        bool exposed = false;
        bool glInit  = false;
#if FAMISTUDIO_MACOS
        bool firstTickDone = false;
#endif

        private int  doubleClickTime = 250;
        private uint lastMouseButton = 999;
        private uint lastClickTime = 0;
        private Point lastClickPos = Point.Empty;
        private Point lastMousePos = Point.Empty;
        private bool forceCtrlDown = false;
        private GLControl captureControl = null;
        private System.Windows.Forms.MouseButtons captureButton   = System.Windows.Forms.MouseButtons.None;
        private System.Windows.Forms.MouseButtons lastButtonPress = System.Windows.Forms.MouseButtons.None;
        private BitArray keys = new BitArray(65536);
        private System.Windows.Forms.Keys modifiers = System.Windows.Forms.Keys.None;

        public FamiStudioForm(FamiStudio famistudio) : base(new GraphicsMode(new ColorFormat(8, 8, 8, 0), 0, 0), 1, 0, GraphicsContextFlags.Default)
        {
            this.famistudio = famistudio;
            this.Name = "FamiStudioForm";
            FamiStudioForm.instance = this;
            Icon = Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.FamiStudio_64.png");

            controls = new FamiStudioControls(this);

            WidthRequest  = GtkUtils.ScaleGtkWidget(640);
            HeightRequest = GtkUtils.ScaleGtkWidget(360);
            controls.Resize(GtkUtils.ScaleWindowCoord(WidthRequest), GtkUtils.ScaleWindowCoord(HeightRequest));

            Events |= 
                Gdk.EventMask.ButtonPressMask   |
                Gdk.EventMask.ButtonReleaseMask |
                Gdk.EventMask.KeyPressMask      |
                Gdk.EventMask.KeyReleaseMask    |
                Gdk.EventMask.ScrollMask        |
                Gdk.EventMask.PointerMotionMask |
                Gdk.EventMask.PointerMotionHintMask;

            ButtonPressEvent   += GlWindow_ButtonPressEvent;
            ButtonReleaseEvent += GlWindow_ButtonReleaseEvent;
            MotionNotifyEvent  += GlWindow_MotionNotifyEvent;
            FocusOutEvent      += Handle_FocusOutEvent;
#if FAMISTUDIO_LINUX
            ScrollEvent        += GlWindow_ScrollEvent;
#endif

            doubleClickTime = Gtk.Settings.GetForScreen(Gdk.Screen.Default).DoubleClickTime;

            Resize(GtkUtils.ScaleGtkWidget(1280), GtkUtils.ScaleGtkWidget(720));

#if FAMISTUDIO_LINUX
            Maximize();
#endif
        }
        
        protected override bool OnExposeEvent(Gdk.EventExpose evnt)
        {
            if (!exposed)
            {
#if FAMISTUDIO_MACOS
                MacOSInit();
#endif
                Cursors.Initialize();
                RefreshLayout();
                exposed = true;
            }
            return base.OnExposeEvent(evnt);
        }

#if FAMISTUDIO_MACOS
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GLibPollFunctionDelegate(IntPtr ufds, uint nfsd, int timeout);

        private GLibPollFunctionDelegate   OldPollFunctionPtr;
        private GLibPollFunctionDelegate   NewPollFunctionPtr;

        private IntPtr selType         = MacUtils.SelRegisterName("type");
        private IntPtr selCurrentEvent = MacUtils.SelRegisterName("currentEvent");
        private IntPtr selWindow       = MacUtils.SelRegisterName("window");
        private IntPtr selHasPreciseScrollingDeltas = MacUtils.SelRegisterName("hasPreciseScrollingDeltas");
        private IntPtr selscrollingDeltaX = MacUtils.SelRegisterName("scrollingDeltaX");
        private IntPtr selscrollingDeltaY = MacUtils.SelRegisterName("scrollingDeltaY");
        private IntPtr selDeltaX = MacUtils.SelRegisterName("deltaX");
        private IntPtr selDeltaY = MacUtils.SelRegisterName("deltaY");
        private IntPtr selLocationInWindow = MacUtils.SelRegisterName("locationInWindow");
        private IntPtr selMagnification = MacUtils.SelRegisterName("magnification");

        private IntPtr lastEvent;
        private float  magnificationAccum = 0;

        private const int NSEventTypeScrollWheel = 22;
        private const int NSEventTypeMagnify     = 30;

        [DllImport("libglib-2.0.0.dylib")]
        private extern static IntPtr g_main_context_get_poll_func(IntPtr context);

        [DllImport("libglib-2.0.0.dylib")]
        private extern static void g_main_context_set_poll_func(IntPtr context, IntPtr func);

        private void MacOSInit()
        {
            IntPtr windowHandle = MacUtils.NSWindowFromGdkWindow(GdkWindow.Handle);
            MacUtils.Initialize(windowHandle);

            // Hijack GDK main event loop so we can handle some more events.
            var pollFunc = g_main_context_get_poll_func(IntPtr.Zero);
            OldPollFunctionPtr = Marshal.GetDelegateForFunctionPointer<GLibPollFunctionDelegate>(pollFunc);
            NewPollFunctionPtr = GLibPollFunction;
            g_main_context_set_poll_func(IntPtr.Zero, Marshal.GetFunctionPointerForDelegate(NewPollFunctionPtr));
        }

        private void HandleScrollWheelEvent(IntPtr e)
        {
            float scrollX, scrollY;
            if (MacUtils.SendBool(e, selHasPreciseScrollingDeltas))
            {
                scrollX = (float)MacUtils.SendFloat(e, selscrollingDeltaX);
                scrollY = (float)MacUtils.SendFloat(e, selscrollingDeltaY);
            }
            else
            {
                scrollX = (float)MacUtils.SendFloat(e, selDeltaX);
                scrollY = (float)MacUtils.SendFloat(e, selDeltaY);
            }

            var pt = MacUtils.SendPoint(e, selLocationInWindow);
            var px = GtkUtils.ScaleWindowCoord((float)(pt.X));
            var py = GtkUtils.ScaleWindowCoord((int)(float)(Allocation.Height - pt.Y));

            var ctrl = controls.GetControlAtCoord(px, py, out int x, out int y);

            if (ctrl != null)
            {
                var trackpadControls = global::FamiStudio.Settings.TrackPadControls;
                var trackpadReverse  = global::FamiStudio.Settings.ReverseTrackPad;

                if (trackpadControls)
                    scrollY = -scrollY;

                var scale = trackpadControls ? (float)Utils.Clamp(global::FamiStudio.Settings.TrackPadMoveSensitity, 1, 16) : 1.0f;
                scale *= trackpadControls && trackpadReverse ? -1.0f : 1.0f;
                var dx = Utils.SignedCeil(scrollX * scale);
                var dy = Utils.SignedCeil(scrollY * scale);
                
                //Debug.WriteLine($"Mouse wheel {precise} {scrollX} {scrollY}");

                if (dy != 0)
                    ctrl.MouseWheel(new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.None, 1, x, y, dy));
                if (dx != 0)
                    ctrl.MouseHorizontalWheel(new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.None, 1, x, y, dx));
            }
        }

        private void HandleMagnifyEvent(IntPtr e)
        {
            var magnification = (float)MacUtils.SendFloat(e, selMagnification);

            if (Math.Sign(magnification) != Math.Sign(magnificationAccum))
                magnificationAccum = 0;

            magnificationAccum += magnification;

            //Debug.WriteLine($"Magnify {magnification}");

            var threshold = 2.0f / (float)Utils.Clamp(global::FamiStudio.Settings.TrackPadZoomSensitity, 1, 32);

            if (Math.Abs(magnificationAccum) > threshold)
            {
                var pt = MacUtils.SendPoint(e, selLocationInWindow);
                var px = GtkUtils.ScaleWindowCoord((float)(pt.X));
                var py = GtkUtils.ScaleWindowCoord((int)(float)(Allocation.Height - pt.Y));

                Debug.WriteLine($"{px} {py}");

                var ctrl = controls.GetControlAtCoord(px, py, out int x, out int y);

                if (ctrl != null)
                {
                    var args = new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.None, 1, x, y, Math.Sign(magnificationAccum) * 120);
                    forceCtrlDown = true;
                    ctrl.MouseWheel(args);
                    forceCtrlDown = false;
                }

                magnificationAccum = 0;
            }
        }

        private int GLibPollFunction(IntPtr ufds, uint nfsd, int timeout)
        {
            var r = OldPollFunctionPtr(ufds, nfsd, timeout);
            var currentEvent = MacUtils.SendIntPtr(MacUtils.NSApplication, selCurrentEvent);

            if (currentEvent != IntPtr.Zero && currentEvent != lastEvent)
            {
                var window = MacUtils.SendIntPtr(currentEvent, selWindow);

                if (window == MacUtils.NSWindow)
                {
                    var eventType = MacUtils.SendInt(currentEvent, selType);

                    switch (eventType)
                    {
                        case NSEventTypeScrollWheel:
                            HandleScrollWheelEvent(currentEvent);
                            break;
                        case NSEventTypeMagnify:
                            HandleMagnifyEvent(currentEvent);
                            break;
                    }
                }

                lastEvent = currentEvent;
            }

            return r;
        }
#endif

        void Handle_FocusOutEvent(object o, FocusOutEventArgs args)
        {
            keys.SetAll(false);
            modifiers = System.Windows.Forms.Keys.None;
        }

        protected override void Resized(int width, int height)
        {
            controls.Resize(GtkUtils.ScaleWindowCoord(width), GtkUtils.ScaleWindowCoord(height));
            MarkDirty();
            RenderFrame();
        }

        protected System.Windows.Forms.MouseEventArgs ToWinFormArgs(Gdk.EventScroll e, int x, int y, bool horizontal)
        {
            if (horizontal)
            {
                Debug.Assert(e.Direction == Gdk.ScrollDirection.Left || e.Direction == Gdk.ScrollDirection.Right);
                return new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.None, 1, x, y, e.Direction == Gdk.ScrollDirection.Right ? 120 : -120);
            }
            else
            {
                Debug.Assert(e.Direction == Gdk.ScrollDirection.Up || e.Direction == Gdk.ScrollDirection.Down);
                return new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.None, 1, x, y, e.Direction == Gdk.ScrollDirection.Up ? 120 : -120);
            }
        }

        void GlWindow_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            var scaledX = GtkUtils.ScaleWindowCoord(args.Event.X);
            var scaledY = GtkUtils.ScaleWindowCoord(args.Event.Y);

            var ctrl = controls.GetControlAtCoord(scaledX, scaledY, out int x, out int y);

            lastMousePos.X = scaledX;
            lastMousePos.Y = scaledY;

            if (args.Event.Type == Gdk.EventType.ButtonPress)
            {
                if (captureControl != null)
                    return;

                // GTK's double click is super weird, need to emulate the behavior
                // of Windows here. Basically it will report events in this manner:
                //  t=1 CLICK
                //  t=2 RELEASE
                //  t=3 CLICK <=== Extra Click/Release we dont get on windows.
                //  t=4 RELEASE
                //  t=4 DBL CLICK
                if (args.Event.Button == lastMouseButton &&
                   (args.Event.Time - lastClickTime) < doubleClickTime &&
                    Math.Abs(lastClickPos.X - scaledX) < 4 &&
                    Math.Abs(lastClickPos.Y - scaledY) < 4)
                {
                    lastMouseButton = 999;
                    lastClickTime   = 0;
                    lastClickPos    = Point.Empty;

                    if (ctrl != null)
                        ctrl.MouseDoubleClick(GtkUtils.ToWinFormArgs(args.Event, x, y));
                }
                else
                {
                    lastMouseButton = args.Event.Button;
                    lastClickTime   = args.Event.Time;
                    lastClickPos    = new Point(scaledX, scaledY);

                    var e = GtkUtils.ToWinFormArgs(args.Event, x, y);
                    lastButtonPress = e.Button;

                    if (ctrl != null)
                        ctrl.MouseDown(e);
                }
            }
        }

        void GlWindow_ButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
        {
            var scaledX = GtkUtils.ScaleWindowCoord(args.Event.X);
            var scaledY = GtkUtils.ScaleWindowCoord(args.Event.Y);

            int x;
            int y;
            GLControl ctrl = null;

            if (captureControl != null)
            {
                ctrl = captureControl;
                x = scaledX - ctrl.Left;
                y = scaledY - ctrl.Top;
            }
            else
            {
                ctrl = controls.GetControlAtCoord(scaledX, scaledY, out x, out y);
            }

            lastMousePos.X = scaledX;
            lastMousePos.Y = scaledY;

            var e = GtkUtils.ToWinFormArgs(args.Event, x, y);
            if (e.Button == captureButton)
                ReleaseMouse();

            if (ctrl != null)
                ctrl.MouseUp(e);
        }

        void GlWindow_ScrollEvent(object o, ScrollEventArgs args)
        {
            var scaledX = GtkUtils.ScaleWindowCoord(args.Event.X);
            var scaledY = GtkUtils.ScaleWindowCoord(args.Event.Y);

            var ctrl = controls.GetControlAtCoord(scaledX, scaledY, out int x, out int y);

            if (args.Event.Direction == Gdk.ScrollDirection.Up ||
                args.Event.Direction == Gdk.ScrollDirection.Down)
            {
                ctrl.MouseWheel(ToWinFormArgs(args.Event, x, y, false));
            }
            else if (args.Event.Direction == Gdk.ScrollDirection.Left ||
                     args.Event.Direction == Gdk.ScrollDirection.Right)
            {
                ctrl.MouseHorizontalWheel(ToWinFormArgs(args.Event, x, y, true));
            }
        }

        void GlWindow_MotionNotifyEvent(object o, MotionNotifyEventArgs args)
        {
            var scaledX = GtkUtils.ScaleWindowCoord(args.Event.X);
            var scaledY = GtkUtils.ScaleWindowCoord(args.Event.Y);

            int x;
            int y;
            GLControl ctrl = null;

            if (captureControl != null)
            {
                ctrl = captureControl;
                x = scaledX - ctrl.Left;
                y = scaledY - ctrl.Top;
            }
            else
            {
                ctrl = controls.GetControlAtCoord(scaledX, scaledY, out x, out y);
            }

            lastMousePos.X = scaledX;
            lastMousePos.Y = scaledY;

            if (ctrl != null)
            {
                ctrl.MouseMove(GtkUtils.ToWinFormArgs(args.Event, x, y));
                RefreshCursor(ctrl);
            }
        }

        private void SetKeyMap(System.Windows.Forms.Keys k, bool set)
        {
            var regularKey = k & ~System.Windows.Forms.Keys.Modifiers;
            if (regularKey > 0 && (int)regularKey < keys.Length)
            {
                keys[(int)regularKey] = set;
            }
            else
            {
                var mods = k & System.Windows.Forms.Keys.Modifiers;
                if (mods > 0)
                {
                    if (set)
                        modifiers |= mods;
                    else
                        modifiers &= ~mods;
                }
            }
        }

        protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
        {
            var winKey = GtkUtils.ToWinFormKey(evnt.Key);
            var winMod = GtkUtils.ToWinFormKey(evnt.State);

            //Debug.WriteLine($"{evnt.Key} {evnt.KeyValue} = {winKey}");

            //Debug.WriteLine(evnt.Key);
            Debug.WriteLine(PlatformUtils.KeyCodeToString((int)evnt.Key));

            SetKeyMap(winKey, true);

            var args = new System.Windows.Forms.KeyEventArgs(winKey | winMod);
            famistudio.KeyDown(args, (int)evnt.Key);
            foreach (var ctrl in controls.Controls)
                ctrl.KeyDown(args);

            return base.OnKeyPressEvent(evnt);
        }

        protected override bool OnKeyReleaseEvent(Gdk.EventKey evnt)
        {
            var winKey = GtkUtils.ToWinFormKey(evnt.Key);
            var winMod = GtkUtils.ToWinFormKey(evnt.State);

            SetKeyMap(winKey, false);

            var args = new System.Windows.Forms.KeyEventArgs(winKey | winMod);

            famistudio.KeyUp(args, (int)evnt.Key);
            foreach (var ctrl in controls.Controls)
                ctrl.KeyUp(args);

            return base.OnKeyReleaseEvent(evnt);
        }

        public void RefreshLayout()
        { 
            controls.Resize(GtkUtils.ScaleWindowCoord(Allocation.Width), GtkUtils.ScaleWindowCoord(Allocation.Height));
            controls.MarkDirty();
        }

        public void MarkDirty()
        {
            controls.MarkDirty();
        }

        public void SetActiveControl(GLControl ctrl, bool animate = true)
        {
        }

        public void ShowContextMenu(ContextMenuOption[] options)
        {
        }

        private void GetScaledWindowOrigin(out int ox, out int oy)
        {
            GdkWindow.GetOrigin(out ox, out oy);
            ox = GtkUtils.ScaleWindowCoord(ox);
            oy = GtkUtils.ScaleWindowCoord(oy);
        }

        public Point PointToClient(Point p)
        {
            GetScaledWindowOrigin(out var ox, out var oy);
            return new Point(p.X - ox, p.Y - oy);
        }

        public Point PointToScreen(Point p)
        {
            GetScaledWindowOrigin(out var ox, out var oy);
            return new Point(ox + p.X, oy + p.Y);
        }

        public Point PointToClient(GLControl ctrl, Point p)
        {
            GetScaledWindowOrigin(out var ox, out var oy);
            return new Point(p.X - ctrl.Left - ox, p.Y - ctrl.Top - oy);
        }

        public Point PointToScreen(GLControl ctrl, Point p)
        {
            GetScaledWindowOrigin(out var ox, out var oy);
            return new Point(ox + ctrl.Left + p.X, oy + ctrl.Top + p.Y);
        }

        protected override void GLInitialized()
        {
            GL.Disable(EnableCap.DepthTest);

            GL.Viewport(0, 0, Allocation.Width, Allocation.Height);
            GL.ClearColor(
                Theme.DarkGreyFillColor2.R / 255.0f,
                Theme.DarkGreyFillColor2.G / 255.0f,
                Theme.DarkGreyFillColor2.B / 255.0f,
                1.0f);

            // Clear+swap twice. Seems to clear up the garbage that may be in the back buffer.
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsContext.CurrentContext.SwapBuffers();
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsContext.CurrentContext.SwapBuffers();

            controls.InitializeGL();
            glInit = true;
            GLib.Idle.Add(new GLib.IdleHandler(OnIdleProcessMain));
        }

        protected bool OnIdleProcessMain()
        {
#if FAMISTUDIO_MACOS
            // HACK : On MacOS, in retina, sometimes the OS will force-resize the window
            // without notifying us. Just refresh the layout on the first frame to be safe.
            if (!firstTickDone)
            {
                RefreshLayout();
                firstTickDone = true;
            }
#endif

            RenderFrame();

            // Window becomes null when exiting the app.
            if (GdkWindow != null)
            {
                famistudio.Tick();
            }

            return true;
        }

        public void Refresh()
        {
            RenderFrame();
        }

        protected override void RenderFrame(bool force = false)
        {
            if (force)
                MarkDirty();

            if (glInit && GdkWindow != null && controls.Redraw())
            {
                GraphicsContext.CurrentContext.SwapBuffers();
            }
            else
            {
                System.Threading.Thread.Sleep(4);
            }
        }

        public void CaptureMouse(GLControl ctrl)
        {
            if (lastButtonPress != System.Windows.Forms.MouseButtons.None)
            {
                Debug.Assert(captureControl == null);

                captureButton  = lastButtonPress;
                captureControl = ctrl;
                Gdk.Pointer.Grab(GdkWindow, true, Gdk.EventMask.PointerMotionMask | Gdk.EventMask.ButtonReleaseMask, null, null, 0);
            }
        }

        public void ReleaseMouse()
        {
            if (captureControl != null)
            {
                captureControl = null;
                Gdk.Pointer.Ungrab(0);
            }
        }

        public Point GetCursorPosition()
        {
            return PointToScreen(lastMousePos);
        }

        public void RefreshCursor()
        {
            RefreshCursor(controls.GetControlAtCoord(lastMousePos.X, lastMousePos.Y, out _, out _));
        }

        private void RefreshCursor(GLControl ctrl)
        {
            if (captureControl != null && captureControl != ctrl)
                return;

            if (ctrl != null)
                GdkWindow.Cursor = ctrl.Cursor.Current;
        }

        public System.Windows.Forms.Keys GetModifierKeys()
        {
            return modifiers | (forceCtrlDown ? System.Windows.Forms.Keys.Control : System.Windows.Forms.Keys.None);
        }

        public static bool IsKeyDown(System.Windows.Forms.Keys k)
        {
            return (int)k < instance.keys.Length ? instance.keys[(int)k] : false;
        }

        public Rectangle Bounds
        {
            get
            {
                GetScaledWindowOrigin(out var ox, out var oy);
                return new Rectangle(ox, oy, ox + GtkUtils.ScaleWindowCoord(Allocation.Width), oy + GtkUtils.ScaleWindowCoord(Allocation.Height));
            }
        }

        public bool ShouldIgnoreMouseWheel(GLControl ctrl, System.Windows.Forms.MouseEventArgs e)
        {
            return false;
        }

        protected override bool OnDeleteEvent(Gdk.Event evnt)
        {
            if (!famistudio.TryClosing())
                return true;

            Application.Quit();

            return base.OnDeleteEvent(evnt);
        }

        public void Run()
        {
            Show();
            Application.Run();
        }
    }
}
