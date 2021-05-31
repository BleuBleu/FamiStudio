using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace FamiStudio
{
    public class Direct2DControl : UserControl
    {
        protected Direct2DGraphics d2dGraphics;

        public Direct2DControl()
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!DesignMode && d2dGraphics != null)
            {
                d2dGraphics.Dispose();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            InitRenderGraphics();
        }

        private void InitRenderGraphics()
        {
            if (!DesignMode)
            {
                DoubleBuffered = false;
                ResizeRedraw = true;
                d2dGraphics = new Direct2DGraphics(this);
                OnRenderInitialized(d2dGraphics);
            }
        }

        private void TerminateRenderGraphics()
        {
            if (!DesignMode)
            {
                OnRenderTerminated();
                d2dGraphics.Dispose();
                d2dGraphics = null;
            }
        }

        protected virtual void OnRenderInitialized(Direct2DGraphics g)
        {
        }

        protected virtual void OnRenderTerminated()
        {
        }

        protected virtual void OnRender(Direct2DGraphics g)
        {
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (d2dGraphics != null)
            {
                d2dGraphics.Resize(Math.Max(1, ClientSize.Width), Math.Max(1, ClientSize.Height));
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (DesignMode)
            {
                e.Graphics.Clear(System.Drawing.Color.Black);
                e.Graphics.DrawString("Direct2D cannot be used at design time.", this.Font, System.Drawing.Brushes.White, 10, 10);
            }
            else
            {
                d2dGraphics.BeginDraw();
                OnRender(d2dGraphics);

                try
                {
                    d2dGraphics.EndDraw();
                }
                catch (SharpDX.SharpDXException ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);

                    if (ex.HResult == -2003238900) // D2DERR_RECREATE_TARGET
                    {
                        TerminateRenderGraphics();
                        InitRenderGraphics();
                        Invalidate();
                    }
                }
            }
        }

        public virtual void DoMouseWheel(MouseEventArgs e)
        {
        }

        public new FamiStudioForm ParentForm { get => base.ParentForm as FamiStudioForm; }
        public FamiStudio App { get => (base.ParentForm as FamiStudioForm)?.FamiStudio; }
    }
}
