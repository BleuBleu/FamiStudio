using Gtk;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
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

        private GLControl captureControl = null;
        private System.Windows.Forms.MouseButtons captureButton   = System.Windows.Forms.MouseButtons.None;
        private System.Windows.Forms.MouseButtons lastButtonPress = System.Windows.Forms.MouseButtons.None;

        public FamiStudioForm(FamiStudio famistudio) : base(WindowType.Toplevel)
        {
            this.famistudio = famistudio;
            this.Name = "FamiStudioForm";
            FamiStudioForm.instance = this;

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

            //glWidget.ConfigureEvent     += GlWidget_ConfigureEvent;
            glWidget.ButtonPressEvent   += GlWidget_ButtonPressEvent;
            glWidget.ButtonReleaseEvent += GlWidget_ButtonReleaseEvent;
            glWidget.ScrollEvent        += GlWidget_ScrollEvent;
            glWidget.MotionNotifyEvent  += GlWidget_MotionNotifyEvent;
            glWidget.Resized += GlWidget_Resized;

            Add(glWidget);
        }

        void GlWidget_Resized(object sender, EventArgs e)
        {
            controls.Resize(glWidget.Allocation.Width, glWidget.Allocation.Height);
            Invalidate();
            RenderFrame();
        }

        protected System.Windows.Forms.MouseEventArgs ToWinFormArgs(Gdk.EventButton e, int x, int y)
        {
            System.Windows.Forms.MouseButtons buttons = System.Windows.Forms.MouseButtons.None;

            if (e.Button == 1)
                buttons = System.Windows.Forms.MouseButtons.Left;
            else if (e.Button == 2)
                buttons = System.Windows.Forms.MouseButtons.Middle;
            else if (e.Button == 3)
                buttons = System.Windows.Forms.MouseButtons.Right;

            return new System.Windows.Forms.MouseEventArgs(buttons, 1, x, y, 0);
        }

        protected System.Windows.Forms.MouseEventArgs ToWinFormArgs(Gdk.EventMotion e, int x, int y)
        {
            System.Windows.Forms.MouseButtons buttons = System.Windows.Forms.MouseButtons.None;

            if ((e.State & Gdk.ModifierType.Button1Mask) != 0)
                buttons |= System.Windows.Forms.MouseButtons.Left;
            else if ((e.State & Gdk.ModifierType.Button2Mask) != 0)
                buttons |= System.Windows.Forms.MouseButtons.Middle;
            else if ((e.State & Gdk.ModifierType.Button3Mask) != 0)
                buttons |= System.Windows.Forms.MouseButtons.Right;

            return new System.Windows.Forms.MouseEventArgs(buttons, 1, x, y, 0);
        }

        protected System.Windows.Forms.MouseEventArgs ToWinFormArgs(Gdk.EventScroll e, int x, int y)
        {
            return new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.None, 1, x, y, e.Direction == Gdk.ScrollDirection.Up ? 120 : -120);
        }

        void GlWidget_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            var ctrl = controls.GetControlAtCoord((int)args.Event.X, (int)args.Event.Y, out int x, out int y);

            if (args.Event.Type == Gdk.EventType.ButtonPress)
            {
                if (captureControl != null)
                    return;

                var e = ToWinFormArgs(args.Event, x, y);
                lastButtonPress = e.Button;
                ctrl.MouseDown(e);
            }
            else if (args.Event.Type == Gdk.EventType.TwoButtonPress)
            {
                ctrl.MouseDoubleClick(ToWinFormArgs(args.Event, x, y));
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

            var e = ToWinFormArgs(args.Event, x, y);
            if (ctrl != null)
                ctrl.MouseUp(e);

            if (e.Button == captureButton)
                ReleaseMouse();
        }

        void GlWidget_ScrollEvent(object o, ScrollEventArgs args)
        {
            var ctrl = controls.GetControlAtCoord((int)args.Event.X, (int)args.Event.Y, out int x, out int y);
            ctrl.MouseWheel(ToWinFormArgs(args.Event, x, y));
        }

        void GlWidget_MotionNotifyEvent(object o, MotionNotifyEventArgs args)
        {
            Debug.WriteLine($"MOVE! {args.Event.X} {args.Event.Y}");

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

            if (ctrl != null)
                ctrl.MouseMove(ToWinFormArgs(args.Event, x, y));
        }

        public void RefreshSequencerLayout()
        { 
            controls.Resize(glWidget.Allocation.Width, glWidget.Allocation.Width);
            controls.Invalidate();
        }

        public void Invalidate()
        {
            controls.Invalidate();
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
            if (!glInit)
                return false;
            else
            {
                RenderFrame();
                return true;
            }
        }

        protected void RenderFrame()
        {
            if (!glInit)
                return;

            // Tick here so that we also tick on move and resize.
            famistudio.Tick(); // MATTT: In event loop?

            int width  = glWidget.Allocation.Width;
            int height = glWidget.Allocation.Height;

            if (controls.Redraw(width, height))
            {
                GraphicsContext.CurrentContext.SwapBuffers();
            }
        }

        public void CaptureMouse(GLControl ctrl)
        {
            if (lastButtonPress != System.Windows.Forms.MouseButtons.None)
            {
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
            return Point.Empty;
        }

        public void RefreshCursor()
        {
        }

        public System.Windows.Forms.Keys GetModifierKeys()
        {
            return System.Windows.Forms.Keys.None;
        }

        public static bool IsKeyDown(System.Windows.Forms.Keys k)
        {
            return false;
        }

        public Rectangle Bounds
        {
            get
            {
                glWidget.GdkWindow.GetOrigin(out var ox, out var oy);
                return new Rectangle(ox, oy, ox + Allocation.Width, oy + Allocation.Height);
            }
        }

    public void Run()
        {
            Show();

            while (true)
            {
                Application.RunIteration();
                //RenderFrame();
            }
        }
    }
}
