using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
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

        private IntPtr cursor = Cursors.Default;
        private FamiStudio famistudio;
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

#if FAMISTUDIO_MACOS
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
#endif

        public FamiStudioForm(FamiStudio famistudio)
            : base(1280, 720, new GraphicsMode(new ColorFormat(8, 8, 8, 0), 0, 0), "FamiStudio")
        {
            this.VSync = VSyncMode.On;
            this.famistudio = famistudio;

            controls = new FamiStudioControls(this);

#if FAMISTUDIO_LINUX
            // Doesnt work!
            //this.Icon = new Icon("/home/ubuntu/FamiStudio.ico", 32, 32);
#endif

#if FAMISTUDIO_MACOS
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
#endif

            controls.Resize(Width, Height);
            controls.InitializeGL(this);

            GL.Disable(EnableCap.DepthTest);
        }

        public void CaptureMouse(GLControl ctrl)
        {
#if FAMISTUDIO_MACOS
            captureButtons = MacUtils.GetMouseButtons();
#else
            captureButtons = GetStateButtons(Mouse.GetState());
#endif
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
#if FAMISTUDIO_MACOS
            return PointToScreen(MacUtils.GetWindowMousePosition(WindowInfo.Handle));
#else
            var mouseState = Mouse.GetCursorState();
            return new Point(mouseState.X, mouseState.Y);
#endif
        }

        public void RefreshCursor()
        {
#if FAMISTUDIO_MACOS
            var client = MacUtils.GetWindowMousePosition(WindowInfo.Handle);
#else
            var mouseState = Mouse.GetCursorState();
            var client = PointToClient(new Point(mouseState.X, mouseState.Y));
#endif
            var ctrl = controls.GetControlAtCoord(client.X, client.Y, out _, out _);

            RefreshCursor(ctrl);

#if FAMISTUDIO_MACOS
            MacUtils.InvalidateCursor(WindowInfo.Handle);
#endif
        }

        public void RefreshCursor(GLControl ctrl)
        {
            if (captureControl != null && captureControl != ctrl)
                return;

            if (ctrl != null)
                cursor = ctrl.Cursor.Current;

#if FAMISTUDIO_MACOS
            MacUtils.InvalidateCursor(WindowInfo.Handle);
#endif
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

#if FAMISTUDIO_MACOS
            // Position is not reliable here. Super buggy.
            var pt = MacUtils.GetWindowMousePosition(WindowInfo.Handle);
#else
            var pt = new Point(e.X, e.Y);
#endif

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
            //if (CheckFrozen(false)) return;
            base.OnMouseDown(e);

#if FAMISTUDIO_MACOS
            // Position is not reliable here. Super buggy.
            var pt = MacUtils.GetWindowMousePosition(WindowInfo.Handle);
#else
            var pt = new Point(e.X, e.Y);
#endif

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

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            //if (CheckFrozen(false)) return;
            base.OnMouseWheel(e);

#if FAMISTUDIO_MACOS
            // Position is not reliable here. Super buggy.
            var pt = MacUtils.GetWindowMousePosition(WindowInfo.Handle);
#else
            var pt = new Point(e.X, e.Y);
#endif

            // We get notified for clicks in the title back and stuff.
            if (pt.X < 0 || pt.Y < 0 || pt.X >= ClientSize.Width || pt.Y >= ClientSize.Height)
                return;

            var ctrl = controls.GetControlAtCoord(pt.X, pt.Y, out int x, out int y);

            deferredEvents.Add(new DeferredEvent(DeferredEventType.MouseWheel, ctrl, OpenTkUtils.ToWinFormArgs(e, x, y)));
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

        protected System.Windows.Forms.MouseButtons GetStateButtons(MouseState state)
        {
            System.Windows.Forms.MouseButtons buttons = System.Windows.Forms.MouseButtons.None;

            if (state.IsButtonDown(MouseButton.Left))
                buttons |= System.Windows.Forms.MouseButtons.Left;
            if (state.IsButtonDown(MouseButton.Right))
                buttons |= System.Windows.Forms.MouseButtons.Right;
            if (state.IsButtonDown(MouseButton.Middle))
                buttons |= System.Windows.Forms.MouseButtons.Middle;

            return buttons;
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
#if FAMISTUDIO_MACOS
                var pt = MacUtils.GetWindowMousePosition(WindowInfo.Handle);
                var buttons = MacUtils.GetMouseButtons();
#else
                var mouseState = Mouse.GetState();
                var buttons = GetStateButtons(mouseState);
                var pt = PointToClient(new Point(mouseState.X, mouseState.Y));
#endif
                var args = new System.Windows.Forms.MouseEventArgs(buttons, 0, pt.X - captureControl.Left, pt.Y - captureControl.Top, 0);

                //Debug.WriteLine($"MOVE! captureControl = {captureControl} buttons = {buttons} captureButtons = {captureButtons} pt = {pt}");

                if (buttons != captureButtons || buttons == System.Windows.Forms.MouseButtons.None)
                {
                    captureControl.MouseUp(args);
                    captureControl = null;
                }
                else
                {
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
            return OpenTkUtils.GetModifierKeys();
        }

        public new void Run() 
        {
            Visible = true; 
            OnResize(EventArgs.Empty);

#if FAMISTUDIO_LINUX
            PlatformUtils.InitializeGtk();
#endif

            while (true)
            {
                var timeStart = DateTime.Now;

                // OpenTK and GTK can both co-exist, but the GTK event look (which
                // is used in modal dialogs) cannot run from inside the OpenTK
                // event loop. Doing this lead to obscure crashes. To avoid this,
                // we run the OpenTK event loop, but we catch most events and defer
                // them a bit and execute the handlers from outside of the event
                // loop (basically outside of ProcessEvent).

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
