using Gtk;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Resources;

namespace FamiStudio
{
    public class FamiStudioForm : Window
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
        public static FamiStudioForm Instance => instance;

        bool glInit = false;
        GLWidget glWidget;

        private int  doubleClickTime = 250;
        private uint lastMouseButton = 999;
        private uint lastClickTime = 0;
        private Point lastClickPos = Point.Empty;
        private Point lastMousePos = Point.Empty;
        private GLControl captureControl = null;
        private System.Windows.Forms.MouseButtons captureButton   = System.Windows.Forms.MouseButtons.None;
        private System.Windows.Forms.MouseButtons lastButtonPress = System.Windows.Forms.MouseButtons.None;

        public FamiStudioForm(FamiStudio famistudio) : base(WindowType.Toplevel)
        {
            this.famistudio = famistudio;
            this.Name = "FamiStudioForm";
            FamiStudioForm.instance = this;
            Icon = Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.FamiStudio_64.png");

            controls = new FamiStudioControls(this);

            glWidget = new GLWidget(new GraphicsMode(new ColorFormat(8, 8, 8, 0), 0, 0), 1, 0, GraphicsContextFlags.Default);
            glWidget.WidthRequest = 1280;
            glWidget.HeightRequest = 720;
            glWidget.Initialized += GLWidgetInitialize;
            glWidget.Events |= 
                Gdk.EventMask.ButtonPressMask   |
                Gdk.EventMask.ButtonReleaseMask |
                Gdk.EventMask.KeyPressMask      |
                Gdk.EventMask.KeyReleaseMask    |
                Gdk.EventMask.ScrollMask        |
                Gdk.EventMask.PointerMotionMask | 
                Gdk.EventMask.PointerMotionHintMask;
            glWidget.Show();

            glWidget.ButtonPressEvent   += GlWidget_ButtonPressEvent;
            glWidget.ButtonReleaseEvent += GlWidget_ButtonReleaseEvent;
            glWidget.ScrollEvent        += GlWidget_ScrollEvent;
            glWidget.MotionNotifyEvent  += GlWidget_MotionNotifyEvent;
            glWidget.Resized            += GlWidget_Resized;

            doubleClickTime = Gtk.Settings.GetForScreen(Gdk.Screen.Default).DoubleClickTime;

            Add(glWidget);
        }

        void GlWidget_Resized(object sender, EventArgs e)
        {
            controls.Resize(glWidget.Allocation.Width, glWidget.Allocation.Height);
            Invalidate();
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

        void GlWidget_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            var ctrl = controls.GetControlAtCoord((int)args.Event.X, (int)args.Event.Y, out int x, out int y);

            lastMousePos.X = (int)args.Event.X;
            lastMousePos.Y = (int)args.Event.Y;

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
                    Math.Abs(lastClickPos.X - args.Event.X) < 4 &&
                    Math.Abs(lastClickPos.Y - args.Event.Y) < 4)
                {
                    lastMouseButton = 999;
                    lastClickTime   = 0;
                    lastClickPos    = Point.Empty;

                    ctrl.MouseDoubleClick(GtkUtils.ToWinFormArgs(args.Event, x, y));
                }
                else
                {
                    lastMouseButton = args.Event.Button;
                    lastClickTime   = args.Event.Time;
                    lastClickPos    = new Point((int)args.Event.X, (int)args.Event.Y);

                    var e = GtkUtils.ToWinFormArgs(args.Event, x, y);
                    lastButtonPress = e.Button;
                    ctrl.MouseDown(e);
                }
            }
        }

        void GlWidget_ButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
        {
            int x;
            int y;
            GLControl ctrl = null;

            if (captureControl != null)
            {
                ctrl = captureControl;
                x = (int)args.Event.X - ctrl.Left;
                y = (int)args.Event.Y - ctrl.Top;
            }
            else
            {
                ctrl = controls.GetControlAtCoord((int)args.Event.X, (int)args.Event.Y, out x, out y);
            }

            lastMousePos.X = (int)args.Event.X;
            lastMousePos.Y = (int)args.Event.Y;

            var e = GtkUtils.ToWinFormArgs(args.Event, x, y);
            if (e.Button == captureButton)
                ReleaseMouse();

            if (ctrl != null)
                ctrl.MouseUp(e);
        }

        void GlWidget_ScrollEvent(object o, ScrollEventArgs args)
        {
            var ctrl = controls.GetControlAtCoord((int)args.Event.X, (int)args.Event.Y, out int x, out int y);

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

        void GlWidget_MotionNotifyEvent(object o, MotionNotifyEventArgs args)
        {
            //Debug.WriteLine($"MOVE! {args.Event.X} {args.Event.Y}");

            int x;
            int y;
            GLControl ctrl = null;

            if (captureControl != null)
            {
                ctrl = captureControl;
                x = (int)args.Event.X - ctrl.Left;
                y = (int)args.Event.Y - ctrl.Top;
            }
            else
            {
                ctrl = controls.GetControlAtCoord((int)args.Event.X, (int)args.Event.Y, out x, out y);
            }

            lastMousePos.X = (int)args.Event.X;
            lastMousePos.Y = (int)args.Event.Y;

            if (ctrl != null)
            {
                ctrl.MouseMove(GtkUtils.ToWinFormArgs(args.Event, x, y));
                RefreshCursor(ctrl);
            }
        }

        protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
        {
            var args = new System.Windows.Forms.KeyEventArgs(GtkUtils.ToWinFormKey(evnt.Key) | GtkUtils.ToWinFormKey(evnt.State));
            famistudio.KeyDown(args);
            foreach (var ctrl in controls.Controls)
                ctrl.KeyDown(args);

            return base.OnKeyPressEvent(evnt);
        }

        protected override bool OnKeyReleaseEvent(Gdk.EventKey evnt)
        {
            var args = new System.Windows.Forms.KeyEventArgs(GtkUtils.ToWinFormKey(evnt.Key) | GtkUtils.ToWinFormKey(evnt.State)); 
            foreach (var ctrl in controls.Controls)
                ctrl.KeyUp(args);

            return base.OnKeyReleaseEvent(evnt);
        }

        public void RefreshSequencerLayout()
        { 
            controls.Resize(glWidget.Allocation.Width, glWidget.Allocation.Height);
            controls.Invalidate();
        }

        public void Invalidate()
        {
            controls.Invalidate();
        }

        public Point PointToClient(Point p)
        {
            glWidget.GdkWindow.GetOrigin(out var ox, out var oy);
            return new Point(p.X - ox, p.Y - oy);
        }

        public Point PointToScreen(Point p)
        {
            glWidget.GdkWindow.GetOrigin(out var ox, out var oy);
            return new Point(ox + p.X, oy + p.Y);
        }

        public Point PointToClient(GLControl ctrl, Point p)
        {
            glWidget.GdkWindow.GetOrigin(out var ox, out var oy);
            return new Point(p.X - ctrl.Left - ox, p.Y - ctrl.Top - oy);
        }

        public Point PointToScreen(GLControl ctrl, Point p)
        {
            glWidget.GdkWindow.GetOrigin(out var ox, out var oy);
            return new Point(ox + ctrl.Left + p.X, oy + ctrl.Top + p.Y);
        }

        protected virtual void GLWidgetInitialize(object sender, EventArgs e)
        {
            GL.Disable(EnableCap.DepthTest);

            GL.Viewport(0, 0, glWidget.Allocation.Width, glWidget.Allocation.Height);
            GL.ClearColor(
                ThemeBase.DarkGreyFillColor2.R / 255.0f,
                ThemeBase.DarkGreyFillColor2.G / 255.0f,
                ThemeBase.DarkGreyFillColor2.B / 255.0f,
                1.0f);

            // Clear+swap twice. Seems to clear up the garbage that may be in the back buffer.
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsContext.CurrentContext.SwapBuffers();
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GraphicsContext.CurrentContext.SwapBuffers();

            controls.InitializeGL(this);
            glInit = true;
            GLib.Idle.Add(new GLib.IdleHandler(OnIdleProcessMain));
        }

        protected bool OnIdleProcessMain()
        {
            RenderFrame();
            famistudio.Tick();
            return true;
        }

        public void Refresh()
        {
            RenderFrame();
        }

        protected void RenderFrame()
        {
            if (glInit && controls.Redraw(glWidget.Allocation.Width, glWidget.Allocation.Height))
            {
                GraphicsContext.CurrentContext.SwapBuffers();
            }
        }

        public void CaptureMouse(GLControl ctrl)
        {
            if (lastButtonPress != System.Windows.Forms.MouseButtons.None)
            {
                Debug.Assert(captureControl == null);

                captureButton  = lastButtonPress;
                captureControl = ctrl;
                Gdk.Pointer.Grab(glWidget.GdkWindow, true, Gdk.EventMask.PointerMotionMask | Gdk.EventMask.ButtonReleaseMask, null, null, 0);
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
            var mouseState = Mouse.GetCursorState();
            return new Point(mouseState.X, mouseState.Y);
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
                glWidget.GdkWindow.Cursor = ctrl.Cursor.Current;
        }

        public System.Windows.Forms.Keys GetModifierKeys()
        {
            return OpenTkUtils.GetModifierKeys();
        }

        public static bool IsKeyDown(System.Windows.Forms.Keys k)
        {
            return Keyboard.GetState().IsKeyDown(OpenTkUtils.FromWinFormKey(k));
        }

        public Rectangle Bounds
        {
            get
            {
                glWidget.GdkWindow.GetOrigin(out var ox, out var oy);
                return new Rectangle(ox, oy, ox + Allocation.Width, oy + Allocation.Height);
            }
        }

        protected override bool OnDeleteEvent(Gdk.Event evnt)
        {
            if (!famistudio.TryClosing())
                return false;

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
