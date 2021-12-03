using System;
using System.Windows.Forms;
using OpenTK.Graphics;
using OpenTK.Platform;

namespace FamiStudio
{
    public class GLForm : Form
    {
        protected GLGraphics glGraphics;

        private IWindowInfo windowInfo;
        private IGraphicsContext graphicsContext;
        private bool vsyncEnabled = true;

        public bool VSyncEnabled => vsyncEnabled;

        public GLForm()
        {
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_VREDRAW = 0x1;
                const int CS_HREDRAW = 0x2;
                const int CS_OWNDC = 0x20;

                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_VREDRAW | CS_HREDRAW | CS_OWNDC;
                return cp;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && graphicsContext != null)
            {
                GraphicsContextTerminated();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (windowInfo != null)
            {
                graphicsContext.Update(windowInfo);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (!DesignMode)
            {
                DoubleBuffered = false;
                ResizeRedraw = true;

                ColorFormat colorBufferColorFormat = new ColorFormat(24);
                GraphicsMode graphicsMode = new GraphicsMode(colorBufferColorFormat, 0, 0, 0, ColorFormat.Empty, 2, false);

                windowInfo = Utilities.CreateWindowsWindowInfo(Handle);

                graphicsContext = new GraphicsContext(graphicsMode, windowInfo, 1, 2, GraphicsContextFlags.Default);
                graphicsContext.MakeCurrent(windowInfo);
                graphicsContext.LoadAll();

                // This is an extension, so might fail.
                try
                {
                    OpenTK.Platform.Windows.Wgl.Ext.SwapInterval(1);
                    vsyncEnabled = OpenTK.Platform.Windows.Wgl.Ext.GetSwapInterval() > 0;
                }
                catch
                {
                }

                GraphicsContextInitialized();
            }
        }

        protected virtual void GraphicsContextInitialized()
        {
        }

        protected virtual void GraphicsContextTerminated()
        {
        }

        protected virtual void RenderFrame(bool force = false)
        {
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (DesignMode)
            {
                e.Graphics.Clear(System.Drawing.Color.Black);
                e.Graphics.DrawString("OpenGL cannot be used at design time.", this.Font, System.Drawing.Brushes.White, 10, 10);
            }
            else if (graphicsContext != null)
            {
                RenderFrameAndSwapBuffers(true);
            }
        }

        public void RenderFrameAndSwapBuffers(bool force = false)
        {
            RenderFrame(force);
            graphicsContext.SwapBuffers();
        }
    }
}
