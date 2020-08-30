using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace FamiStudio
{
    public class FamiStudioForm : GameWindow
    {
        enum DeferredEventType
        {
            MouseMove,
            MouseDown,
            MouseDoubleClick,
            MouseUp,
            MouseWheel,
			MouseWheelHorizontal,
            PinchZoom,
            MouseEnter,
            MouseLeave,
            KeyDown,
            KeyUp
        }

        class DeferredEvent
        {
            public DeferredEvent(DeferredEventType t, GLControl c, EventArgs e)
            {
                type = t;
                ctrl = c;
                args = e;
            }
            public DeferredEventType type;
            public GLControl ctrl;
            public EventArgs args;
        };

        private IntPtr nsLoopMode = IntPtr.Zero;
        private IntPtr nsApplication = IntPtr.Zero;
        private IntPtr cursor = Cursors.Default;
        private IntPtr selType = IntPtr.Zero;
        private IntPtr selNextEventMatchingMask = IntPtr.Zero;
        private IntPtr selMagnification = IntPtr.Zero;
        private FamiStudio famistudio;
        private float magnificationAccum = 0;
        private bool forceCtrlDown = false;
        private bool[] keys = new bool[256];
        private bool processingDeferredEvents = false;
        private List<DeferredEvent> deferredEvents = new List<DeferredEvent>();
        private GLControl captureControl;
        private System.Windows.Forms.MouseButtons captureButtons;
        private FamiStudioControls controls;

        public FamiStudio FamiStudio => famistudio;
        public Toolbar ToolBar => controls.ToolBar;
        public Sequencer Sequencer => controls.Sequencer;
        public PianoRoll PianoRoll => controls.PianoRoll;
        public ProjectExplorer ProjectExplorer => controls.ProjectExplorer;

        public string Text { get => Title; set => Title = value; }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate bool WindowShouldZoomToFrameDelegate(IntPtr self, IntPtr cmd, IntPtr nsWindow, RectangleF toFrame);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void ResetCursorRectsDelegate(IntPtr self, IntPtr cmd);

        private readonly WindowShouldZoomToFrameDelegate WindowShouldZoomToFrameHandler;
        private readonly ResetCursorRectsDelegate ResetCursorRectsHandler;

        private bool WindowShouldZoomToFrame(IntPtr self, IntPtr cmd, IntPtr nsWindow, RectangleF toFrame)
        {
            return false;
        }

        private void ResetCursorRects(IntPtr sender, IntPtr cmd)
        {
            MacUtils.SetWindowCursor(WindowInfo.Handle, sender, cursor);
        }

        public FamiStudioForm(FamiStudio famistudio)
            : base(1280, 720, new GraphicsMode(new ColorFormat(8, 8, 8, 0), 0, 0), "FamiStudio")
        {
            this.VSync = VSyncMode.On;
            this.famistudio = famistudio;

            controls = new FamiStudioControls(this);

            MacUtils.Initialize(WindowInfo.Handle);

            // There are some severe maximize bugs in OpenTK. Simply disable the maximize and bypass all the frame zooming thing.
            WindowShouldZoomToFrameHandler = WindowShouldZoomToFrame;
            ResetCursorRectsHandler = ResetCursorRects;

            MacUtils.RemoveMaximizeButton(WindowInfo.Handle);
            MacUtils.ClassReplaceMethod(
                MacUtils.ClassLookup("OpenTK_GameWindow1"),
                MacUtils.SelRegisterName("windowShouldZoom:toFrame:"),
                Marshal.GetFunctionPointerForDelegate(WindowShouldZoomToFrameHandler),
                "b@:@{NSRect={NSPoint=ff}{NSSize=ff}}");
            MacUtils.ClassReplaceMethod(
                MacUtils.ClassLookup("OpenTK_NSView1"),
                MacUtils.SelRegisterName("resetCursorRects"),
                Marshal.GetFunctionPointerForDelegate(ResetCursorRectsHandler),
                "v@:");

            controls.Resize(Width, Height);
            controls.InitializeGL(this);

            GL.Disable(EnableCap.DepthTest);

            var nsAplicationType = typeof(OpenTK.NativeWindow).Assembly.GetType("OpenTK.Platform.MacOS.NSApplication");
            var handleField = nsAplicationType.GetField("Handle", BindingFlags.NonPublic | BindingFlags.Static);
            nsApplication = (IntPtr)handleField.GetValue(null);

            var cocoaNativeWindowType = typeof(OpenTK.NativeWindow).Assembly.GetType("OpenTK.Platform.MacOS.CocoaNativeWindow");
            var loopModeFile = cocoaNativeWindowType.GetField("NSDefaultRunLoopMode", BindingFlags.NonPublic | BindingFlags.Static);
            nsLoopMode = (IntPtr)loopModeFile.GetValue(null);

            selType = MacUtils.SelRegisterName("type");
            selMagnification = MacUtils.SelRegisterName("magnification");
            selNextEventMatchingMask = MacUtils.SelRegisterName("nextEventMatchingMask:untilDate:inMode:dequeue:");
        }

        public void CaptureMouse(GLControl ctrl)
        {
            captureButtons = MacUtils.GetMouseButtons();
            if (captureButtons != System.Windows.Forms.MouseButtons.None)
            {
                captureControl = ctrl;
                //Debug.WriteLine($"CAPTURE! captureControl = {captureControl} captureButtons = {captureButtons}");
            }
        }

        public void ReleaseMouse()
        {
            //Debug.WriteLine($"RELEASE! captureControl = {captureControl}");
            captureControl = null;
        }

        public System.Drawing.Point GetCursorPosition()
        {
            return PointToScreen(MacUtils.GetWindowMousePosition(WindowInfo.Handle));
        }

        public void RefreshCursor()
        {
            var client = MacUtils.GetWindowMousePosition(WindowInfo.Handle);
            var ctrl = controls.GetControlAtCoord(client.X, client.Y, out _, out _);

            RefreshCursor(ctrl);

            MacUtils.InvalidateCursor(WindowInfo.Handle);
        }

        public void RefreshCursor(GLControl ctrl)
        {
            if (captureControl != null && captureControl != ctrl)
                return;

            if (ctrl != null)
                cursor = ctrl.Cursor.Current;

            MacUtils.InvalidateCursor(WindowInfo.Handle);
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            Invalidate();
            OnRenderFrame(null);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            controls.Resize(Width, Height);
            Invalidate();
            OnRenderFrame(null);
        }

        public Point PointToClient(GLControl ctrl, Point p)
        {
            return base.PointToClient(new Point(p.X - ctrl.Left, p.Y - ctrl.Top));
        }

        public Point PointToScreen(GLControl ctrl, Point p)
        {
            return base.PointToScreen(new Point(ctrl.Left + p.X, ctrl.Top  + p.Y));
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            var ctrl = controls.GetControlAtCoord(e.X, e.Y, out int x, out int y);
            RefreshCursor(ctrl);
            deferredEvents.Add(new DeferredEvent(DeferredEventType.MouseMove, ctrl, OpenTkUtils.ToWinFormArgs(e, x, y)));
        }

        protected override void OnFocusedChanged(EventArgs e)
        {
            base.OnFocusedChanged(e);
        }

        DateTime    lastClickTime = DateTime.MinValue;
        Point       lastClickPos = Point.Empty;
        MouseButton lastMouseButton = (MouseButton)0;

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (captureControl != null) return;

            // Position is not reliable here. Super buggy.
            var pt = MacUtils.GetWindowMousePosition(WindowInfo.Handle);

            if (pt.X < 0 || pt.Y < 0 || pt.X >= ClientSize.Width || pt.Y >= ClientSize.Height)
                return;

            var ctrl = controls.GetControlAtCoord(pt.X, pt.Y, out int x, out int y);
            var time = DateTime.Now;

            // No double-click in OpenTK, need to detect ourselves...
            if (e.Button == lastMouseButton &&
                (time - lastClickTime).TotalMilliseconds < 500 && 
                Math.Abs(lastClickPos.X - pt.X) < 4 && 
                Math.Abs(lastClickPos.Y - pt.Y) < 4)
            {
                lastMouseButton = (MouseButton)0;
                lastClickTime = DateTime.MinValue;
                lastClickPos = Point.Empty;

                deferredEvents.Add(new DeferredEvent(DeferredEventType.MouseDoubleClick, ctrl, OpenTkUtils.ToWinFormArgs(e, x, y)));
            }
            else
            {
                lastMouseButton = e.Button;
                lastClickTime = time;
                lastClickPos = pt;

                deferredEvents.Add(new DeferredEvent(DeferredEventType.MouseDown, ctrl, OpenTkUtils.ToWinFormArgs(e, x, y)));
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            // Position is not reliable here. Super buggy.
            var pt = MacUtils.GetWindowMousePosition(WindowInfo.Handle);

            if (pt.X < 0 || pt.Y < 0 || pt.X >= ClientSize.Width || pt.Y >= ClientSize.Height)
                return;

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
                ctrl = controls.GetControlAtCoord(pt.X, pt.Y, out x, out y);
            }

            deferredEvents.Add(new DeferredEvent(DeferredEventType.MouseUp, ctrl, OpenTkUtils.ToWinFormArgs(e, x, y)));
        }

        float lastScrollX = float.NaN;
        float lastScrollY = float.NaN;

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            
            // Position is not reliable here. Super buggy.
            var pt = MacUtils.GetWindowMousePosition(WindowInfo.Handle);

            // We get notified for clicks in the title back and stuff.
            if (pt.X < 0 || pt.Y < 0 || pt.X >= ClientSize.Width || pt.Y >= ClientSize.Height)
                return;
            
            if (float.IsNaN(lastScrollX))
            {
                lastScrollX = e.Mouse.Scroll.X;
                lastScrollY = e.Mouse.Scroll.Y;
            }

            var ctrl = controls.GetControlAtCoord(pt.X, pt.Y, out int x, out int y);

            if (Settings.TrackPadControls)
            {
                var dx = (int)Math.Round((e.Mouse.Scroll.X - lastScrollX) * 10.0f * (Settings.ReverseTrackPad ? -1.0f : 1.0f));
                var dy = (int)Math.Round((e.Mouse.Scroll.Y - lastScrollY) * 10.0f * (Settings.ReverseTrackPad ? -1.0f : 1.0f));

                if (dy != 0)
                    deferredEvents.Add(new DeferredEvent(DeferredEventType.MouseWheel, ctrl, new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.None, 0, x, y, dy)));
                if (dx != 0)
                    deferredEvents.Add(new DeferredEvent(DeferredEventType.MouseWheelHorizontal, ctrl, new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.None, 0, x, y, dx)));
            }
            else
            {
                if (Math.Abs(e.DeltaPrecise) > 0.001f)
                    deferredEvents.Add(new DeferredEvent(DeferredEventType.MouseWheel, ctrl, OpenTkUtils.ToWinFormArgs(e, x, y)));
            }

            lastScrollX = e.Mouse.Scroll.X;
            lastScrollY = e.Mouse.Scroll.Y;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
        }

        public static bool IsKeyDown(System.Windows.Forms.Keys k)
        {
            return Keyboard.GetState().IsKeyDown(OpenTkUtils.FromWinFormKey(k));
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            var args = new System.Windows.Forms.KeyEventArgs(OpenTkUtils.ToWinFormKey(e.Key) | OpenTkUtils.GetModifierKeys());
            famistudio.KeyDown(args);
            foreach (var ctrl in controls.Controls)
                deferredEvents.Add(new DeferredEvent(DeferredEventType.KeyDown, ctrl, args));
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);

            var args = new System.Windows.Forms.KeyEventArgs(OpenTkUtils.ToWinFormKey(e.Key) | OpenTkUtils.GetModifierKeys());
            famistudio.KeyUp(args);
            foreach (var ctrl in controls.Controls)
                deferredEvents.Add(new DeferredEvent(DeferredEventType.KeyUp, ctrl, args));
        }

        protected override void OnRenderFrame(OpenTK.FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            // Tick here so that we also tick on move and resize.
            famistudio.Tick();

            if (controls.Redraw(Width, Height))
                SwapBuffers();
        }

        public void Refresh()
        {
            Invalidate();
            OnRenderFrame(null);
        }

        protected void ProcessDeferredEvents()
        {
            var hasMouseCaptureEvent = false;

            processingDeferredEvents = true;

            foreach (var evt in deferredEvents)
            {
                switch (evt.type)
                {
                    case DeferredEventType.MouseMove:
                        if (captureControl == null)
                            evt.ctrl?.MouseMove(evt.args as System.Windows.Forms.MouseEventArgs);
                        hasMouseCaptureEvent = true;
                        break;
                    case DeferredEventType.MouseDown:
                        evt.ctrl?.MouseDown(evt.args as System.Windows.Forms.MouseEventArgs);
                        break;
                    case DeferredEventType.MouseDoubleClick:
                        evt.ctrl?.MouseDoubleClick(evt.args as System.Windows.Forms.MouseEventArgs);
                        break;
                    case DeferredEventType.MouseUp:
                        if (captureControl == null)
                            evt.ctrl?.MouseUp(evt.args as System.Windows.Forms.MouseEventArgs);
                        hasMouseCaptureEvent = true;
                        break;
                    case DeferredEventType.MouseWheel:
                        evt.ctrl?.MouseWheel(evt.args as System.Windows.Forms.MouseEventArgs);
                        break;
					case DeferredEventType.MouseWheelHorizontal:
						evt.ctrl?.MouseHorizontalWheel(evt.args as System.Windows.Forms.MouseEventArgs);
						break;
                    case DeferredEventType.PinchZoom:
                        forceCtrlDown = true;
                        evt.ctrl?.MouseWheel(evt.args as System.Windows.Forms.MouseEventArgs);
                        forceCtrlDown = false;
                        break;
                    case DeferredEventType.MouseLeave:
                        evt.ctrl?.MouseLeave(evt.args as System.Windows.Forms.MouseEventArgs);
                        break;
                    case DeferredEventType.KeyDown:
                        evt.ctrl?.KeyDown(evt.args as System.Windows.Forms.KeyEventArgs);
                        break;
                    case DeferredEventType.KeyUp:
                        evt.ctrl?.KeyUp(evt.args as System.Windows.Forms.KeyEventArgs);
                        break;
                }
            }

            if (captureControl != null && hasMouseCaptureEvent)
            {
                var pt = MacUtils.GetWindowMousePosition(WindowInfo.Handle);
                var buttons = MacUtils.GetMouseButtons();

                //Debug.WriteLine($"MOVE! captureControl = {captureControl} buttons = {buttons} captureButtons = {captureButtons} pt = {pt}");

                if (buttons != captureButtons || buttons == System.Windows.Forms.MouseButtons.None)
                {
                    var args = new System.Windows.Forms.MouseEventArgs(captureButtons, 0, pt.X - captureControl.Left, pt.Y - captureControl.Top, 0);
                    captureControl.MouseUp(args);
                    captureControl = null;
                }
                else
                {
                    var args = new System.Windows.Forms.MouseEventArgs(buttons, 0, pt.X - captureControl.Left, pt.Y - captureControl.Top, 0);
                    captureControl.MouseMove(args); 
                }
            }

            processingDeferredEvents = false;
            deferredEvents.Clear();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (processingDeferredEvents)
            {
                e.Cancel = true;
                return;
            }

            if (!famistudio.TryClosing())
            {
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }

        private void ProcessSpecialEvents()
        {
            if (!Settings.TrackPadControls)
                return;

            while (true)
            {
                const int NSEventTypeMagnify = 30;

                var e = MacUtils.SendIntPtr(nsApplication, selNextEventMatchingMask, 1 << NSEventTypeMagnify, IntPtr.Zero, nsLoopMode, true);

                if (e == IntPtr.Zero)
                    return;

                var type = MacUtils.SendInt(e, selType);

                if (type == NSEventTypeMagnify)
                {
                    var magnification = (float)MacUtils.SendFloat(e, selMagnification);

                    if (Math.Sign(magnification) != Math.Sign(magnificationAccum))
                        magnificationAccum = 0;

                    magnificationAccum += magnification;

                    if (Math.Abs(magnificationAccum) > 0.25f)
                    {
                        var pt = MacUtils.GetWindowMousePosition(WindowInfo.Handle);

                        // We get notified for clicks in the title back and stuff.
                        if (pt.X < 0 || pt.Y < 0 || pt.X >= ClientSize.Width || pt.Y >= ClientSize.Height)
                            continue;

                        var ctrl = controls.GetControlAtCoord(pt.X, pt.Y, out int x, out int y);
                        deferredEvents.Add(new DeferredEvent(DeferredEventType.PinchZoom, ctrl, new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.None, 0, x, y, Math.Sign(magnificationAccum) * 120)));
                        magnificationAccum = 0;
                    }
                }
            }
        }

        public void Invalidate()
        {
            controls.Invalidate();
        }
        
        public void RefreshSequencerLayout()    
        {
            controls.Resize(Width, Height);
            controls.Invalidate();
        }

        public System.Windows.Forms.Keys GetModifierKeys()
        {
            return OpenTkUtils.GetModifierKeys() | (forceCtrlDown ? System.Windows.Forms.Keys.Control : System.Windows.Forms.Keys.None);
        }

        public new void Run() 
        {
            Visible = true; 
            OnResize(EventArgs.Empty);

            while (true)
            {
                var timeStart = DateTime.Now;

                // OpenTK and GTK can both co-exist, but the GTK event look (which
                // is used in modal dialogs) cannot run from inside the OpenTK
                // event loop. Doing this lead to obscure crashes. To avoid this,
                // we run the OpenTK event loop, but we catch most events and defer
                // them a bit and execute the handlers from outside of the event
                // loop (basically outside of ProcessEvent).

                ProcessSpecialEvents();
                ProcessEvents();
                ProcessDeferredEvents();

                if (Exists && !IsExiting)
                {
                    OnRenderFrame(null);
                }
                else
                {
                    return;
                }

                // Arbitrary sleep time.
                System.Threading.Thread.Sleep(4);
            }
        }
    }
}
