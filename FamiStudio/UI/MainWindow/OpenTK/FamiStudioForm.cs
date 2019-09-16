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

        const int ControlToolbar = 0;
        const int ControlSequener = 1;
        const int ControlPianoRoll = 2;
        const int ControlProjectExplorer = 3;

        private IntPtr cursor = Cursors.Default;
        private FamiStudio famistudio;
        private GLGraphics gfx = new GLGraphics();
        private GLControl[] controls = new GLControl[4];
        private bool[] keys = new bool[256];
        private bool processingDeferredEvents = false;
        private List<DeferredEvent> deferredEvents = new List<DeferredEvent>();
        private GLControl captureControl;
        private System.Windows.Forms.MouseButtons captureButtons;

        private Toolbar toolbar;
        private Sequencer sequencer;
        private PianoRoll pianoRoll;
        private ProjectExplorer projectExplorer;

        public FamiStudio FamiStudio => famistudio;
        public Toolbar ToolBar => toolbar;
        public Sequencer Sequencer => sequencer;
        public PianoRoll PianoRoll => pianoRoll;
        public ProjectExplorer ProjectExplorer => projectExplorer;

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

#if FAMISTUDIO_MACOS
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

            toolbar = new Toolbar();
            sequencer = new Sequencer();
            pianoRoll = new PianoRoll();
            projectExplorer = new ProjectExplorer();

            controls[ControlToolbar] = toolbar;
            controls[ControlSequener] = sequencer;
            controls[ControlPianoRoll] = pianoRoll;
            controls[ControlProjectExplorer] = projectExplorer;

            foreach (var ctrl in controls)
            {
                ctrl.ParentForm = this;
                ctrl.RenderInitialized(gfx);
            }

            GL.Disable(EnableCap.DepthTest);
        }

        public void CaptureMouse(GLControl ctrl)
        {
            captureControl = ctrl;
            captureButtons = MacUtils.GetMouseButtons();
        }

        public void ReleaseMouse()
        {
            captureControl = null;
        }

        public System.Drawing.Point GetCursorPosition()
        {
#if FAMISTUDIO_MACOS
            return PointToScreen(MacUtils.GetWindowMousePosition(WindowInfo.Handle));
#endif
        }

        public void RefreshCursor()
        {
#if FAMISTUDIO_MACOS
            var client = MacUtils.GetWindowMousePosition(WindowInfo.Handle);
#else
            var client = PointToClient(Mouse.GetCursorState());
#endif
            var ctrl = GetControlAtCoord(client.X, client.Y, out _, out _);

            if (ctrl != null)
                cursor = ctrl.Cursor.Current;

            MacUtils.InvalidateCursor(WindowInfo.Handle);
        }

        public void RefreshCursor(GLControl ctrl)
        {
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

            const int toolBarHeight = 40;
            const int projectExplorerWidth = 260;
            const int sequencerHeight = 298;

            toolbar.Move(0, 0, Width, toolBarHeight);
            projectExplorer.Move(Width - projectExplorerWidth, toolBarHeight, projectExplorerWidth, Height - toolBarHeight);
            sequencer.Move(0, toolBarHeight, Width - projectExplorerWidth, sequencerHeight);
            pianoRoll.Move(0, toolBarHeight + sequencerHeight, Width - projectExplorerWidth, Height - toolBarHeight - sequencerHeight);

            Invalidate();
            OnRenderFrame(null);
        }

        public Point PointToClient(GLControl ctrl, Point p)
        {
            foreach (var c in controls)
            {
                if (c == ctrl)
                {
                    return base.PointToClient(new Point(p.X - ctrl.Left, p.Y - ctrl.Top));
                }
            }

            return Point.Empty;
        }

        public Point PointToScreen(GLControl ctrl, Point p)
        {
            foreach (var c in controls)
            {
                if (c == ctrl)
                {
                    return base.PointToScreen(new Point(ctrl.Left + p.X, ctrl.Top  + p.Y));
                }
            }

            return Point.Empty;
        }

        protected GLControl GetControlAtCoord(int formX, int formY, out int ctrlX, out int ctrlY)
        {
            foreach (var ctrl in controls)
            {
                ctrlX = formX - ctrl.Left;
                ctrlY = formY - ctrl.Top;

                if (ctrlX >= 0 &&
                    ctrlY >= 0 &&
                    ctrlX <  ctrl.Width &&
                    ctrlY <  ctrl.Height)
                {
                    return ctrl;
                }
            }

            ctrlX = 0;
            ctrlY = 0;
            return null;
        }

        protected System.Windows.Forms.MouseEventArgs ToWinFormArgs(MouseMoveEventArgs e, int x, int y)
        {
#if FAMISTUDIO_MACOS
            // The OpenTK mouse state isnt reliable and often has buttons getting "stuck"
            // especially after things like resizing the window. Bypass it.
            var buttons = MacUtils.GetMouseButtons();
#else
            System.Windows.Forms.MouseButtons buttons = System.Windows.Forms.MouseButtons.None;

            if (e.Mouse.LeftButton == ButtonState.Pressed)
                buttons = System.Windows.Forms.MouseButtons.Left;
            else if (e.Mouse.MiddleButton == ButtonState.Pressed)
                buttons = System.Windows.Forms.MouseButtons.Middle;
            else if (e.Mouse.RightButton == ButtonState.Pressed)
                buttons = System.Windows.Forms.MouseButtons.Right;
#endif

            return new System.Windows.Forms.MouseEventArgs(buttons, 1, x, y, 0);
        }

        protected System.Windows.Forms.MouseEventArgs ToWinFormArgs(MouseButtonEventArgs e, int x, int y)
        {
            System.Windows.Forms.MouseButtons buttons = System.Windows.Forms.MouseButtons.None;

            switch (e.Button)
            {
                case MouseButton.Left:   buttons = System.Windows.Forms.MouseButtons.Left;   break;
                case MouseButton.Middle: buttons = System.Windows.Forms.MouseButtons.Middle; break;
                case MouseButton.Right:  buttons = System.Windows.Forms.MouseButtons.Right;  break;
            }

            return new System.Windows.Forms.MouseEventArgs(buttons, 1, x, y, 0);
        }

        protected System.Windows.Forms.MouseEventArgs ToWinFormArgs(MouseWheelEventArgs e, int x, int y)
        {
            return new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.None, 0, x, y, e.Delta * 120);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            if (captureControl != null) return;
            var ctrl = GetControlAtCoord(e.X, e.Y, out int x, out int y);
            RefreshCursor(ctrl);
            deferredEvents.Add(new DeferredEvent(DeferredEventType.MouseMove, ctrl, ToWinFormArgs(e, x, y)));
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

            var ctrl = GetControlAtCoord(pt.X, pt.Y, out int x, out int y);
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

                deferredEvents.Add(new DeferredEvent(DeferredEventType.MouseDoubleClick, ctrl, ToWinFormArgs(e, x, y)));
            }
            else
            {
                lastMouseButton = e.Button;
                lastClickTime = time;
                lastClickPos = pt;

                deferredEvents.Add(new DeferredEvent(DeferredEventType.MouseDown, ctrl, ToWinFormArgs(e, x, y)));
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            //if (CheckFrozen(false)) return;
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

            var ctrl = GetControlAtCoord(pt.X, pt.Y, out int x, out int y);

            deferredEvents.Add(new DeferredEvent(DeferredEventType.MouseUp, ctrl, ToWinFormArgs(e, x, y)));
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

            var ctrl = GetControlAtCoord(pt.X, pt.Y, out int x, out int y);

            deferredEvents.Add(new DeferredEvent(DeferredEventType.MouseWheel, ctrl, ToWinFormArgs(e, x, y)));
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
        }

        protected System.Windows.Forms.Keys ToWinFormKey(Key k)
        {
            if (k >= Key.A && k <= Key.Z)
                return System.Windows.Forms.Keys.A + (k - Key.A);
            else if (k == Key.ControlRight || k == Key.ControlLeft)
                return System.Windows.Forms.Keys.Control;
            else if (k == Key.AltRight || k == Key.AltLeft)
                return System.Windows.Forms.Keys.Alt;
            else if (k == Key.ShiftRight || k == Key.ShiftLeft)
                return System.Windows.Forms.Keys.Shift;
            else if (k == Key.Space)
                return System.Windows.Forms.Keys.Space;
            else if (k == Key.Enter)
                return System.Windows.Forms.Keys.Enter;
            else if (k == Key.Home)
                return System.Windows.Forms.Keys.Home;
            else if (k == Key.Delete)
                return System.Windows.Forms.Keys.Delete;

            Debug.WriteLine($"Unknown key pressed {k}");

            return System.Windows.Forms.Keys.None;
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            var args = new System.Windows.Forms.KeyEventArgs(ToWinFormKey(e.Key));
            famistudio.KeyDown(args);
            foreach (var ctrl in controls)
                deferredEvents.Add(new DeferredEvent(DeferredEventType.KeyDown, ctrl, args));
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);

            var args = new System.Windows.Forms.KeyEventArgs(ToWinFormKey(e.Key));
            famistudio.KeyDown(args);
            foreach (var ctrl in controls)
                deferredEvents.Add(new DeferredEvent(DeferredEventType.KeyUp, ctrl, args));
        }

        public System.Windows.Forms.Keys GetModifierKeys()
        {
            System.Windows.Forms.Keys modifiers = System.Windows.Forms.Keys.None;

            if (Keyboard.GetState().IsKeyDown(Key.ControlRight) || Keyboard.GetState().IsKeyDown(Key.ControlLeft))
                modifiers |= System.Windows.Forms.Keys.Control;
            if (Keyboard.GetState().IsKeyDown(Key.ShiftRight) || Keyboard.GetState().IsKeyDown(Key.ShiftLeft))
                modifiers |= System.Windows.Forms.Keys.Shift;
            if (Keyboard.GetState().IsKeyDown(Key.AltRight) || Keyboard.GetState().IsKeyDown(Key.AltLeft))
                modifiers |= System.Windows.Forms.Keys.Alt;

            return modifiers;
        }

        protected override void OnRenderFrame(OpenTK.FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            // Tick here so that we also tick on move and resize.
            famistudio.Tick();

            bool anyNeedsRedraw = false;
            foreach (var control in controls)
            {
                anyNeedsRedraw |= control.NeedsRedraw;
            }

            if (anyNeedsRedraw)
            {
                foreach (var control in controls)
                {
                    gfx.BeginDraw(control, Height);
                    control.Render(gfx);
                    control.Validate();
                    gfx.EndDraw();
                }

                SwapBuffers();
            }
        }

        protected void ProcessDeferredEvents()
        {
            processingDeferredEvents = true;

            foreach (var evt in deferredEvents)
            {
                switch (evt.type)
                {
                    case DeferredEventType.MouseMove:
                        evt.ctrl?.MouseMove(evt.args as System.Windows.Forms.MouseEventArgs);
                        break;
                    case DeferredEventType.MouseDown:
                        evt.ctrl?.MouseDown(evt.args as System.Windows.Forms.MouseEventArgs);
                        break;
                    case DeferredEventType.MouseDoubleClick:
                        evt.ctrl?.MouseDoubleClick(evt.args as System.Windows.Forms.MouseEventArgs);
                        break;
                    case DeferredEventType.MouseUp:
                        evt.ctrl?.MouseUp(evt.args as System.Windows.Forms.MouseEventArgs);
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

            if (captureControl != null)
            {
#if FAMISTUDIO_MACOS
                var pt = MacUtils.GetWindowMousePosition(WindowInfo.Handle);
                var buttons = MacUtils.GetMouseButtons();
                var args = new System.Windows.Forms.MouseEventArgs(buttons, 0, pt.X - captureControl.Left, pt.Y - captureControl.Top, 0);

                if (buttons != captureButtons)
                {
                    captureControl.MouseUp(args);
                }
                else
                {
                    captureControl.MouseMove(args);
                }
#else
                // TODO: Implement for Linux.
                Debug.Assert(false);
#endif
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
            foreach (var ctrl in controls)
                ctrl.Invalidate();
        }

        public new void Run() 
        {
#if FAMISTUDIO_MACOS
            MacUtils.Initialize(WindowInfo.Handle);
#endif

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
