using System;

namespace FamiStudio
{
    // MATTT : Add option for a little scroll bar indicator + use that in quick access bar.
    public class TouchScrollContainer : Container
    {
        private bool panning;
        private bool canFling;
        private int lastDialogY;
        private int virtualSizeY;
        private float flingVelY;

        public int VirtualSizeY 
        {
            get { return virtualSizeY; }
            set { if (SetAndMarkDirty(ref virtualSizeY, value)) ClampScroll(); }
        }

        public TouchScrollContainer()
        {
        }

        private bool DoScroll(int deltaY)
        {
            ScrollY += deltaY;
            return ClampScroll();
        }

        private bool ClampScroll()
        {
            var minScrollY = 0;
            var maxScrollY = Math.Max(0, virtualSizeY - height);

            var scrolled = true;
            if (ScrollY < minScrollY) { ScrollY = minScrollY; scrolled = false; }
            if (ScrollY > maxScrollY) { ScrollY = maxScrollY; scrolled = false; }
            return scrolled;
        }

        public override void OnContainerPointerDownNotify(Control control, PointerEventArgs e)
        {
            if (!e.Handled)
            {
                panning = true;
                canFling = false;
                lastDialogY = WindowToControl(control.ControlToWindow(e.Position)).Y;
                control.Capture = true;
            }
        }

        public override void OnContainerPointerUpNotify(Control control, PointerEventArgs e)
        {
            panning = false;
            canFling = true;
        }

        public override void OnContainerPointerMoveNotify(Control control, PointerEventArgs e)
        {
            var dialogY = WindowToControl(control.ControlToWindow(e.Position)).Y;

            if (panning)
            {
                DoScroll(lastDialogY - dialogY);
            }

            lastDialogY = dialogY;
        }

        public override void OnContainerTouchFlingNotify(Control control, PointerEventArgs e)
        {
            if (canFling)
            {
                panning = false;
                flingVelY = e.FlingVelocityY;
                SetTickEnabled(true);
            }
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            OnContainerPointerDownNotify(this, e);
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            OnContainerPointerUpNotify(this, e);
        }

        protected override void OnPointerMove(PointerEventArgs e)
        {
            OnContainerPointerMoveNotify(this, e);
        }

        protected override void OnTouchFling(PointerEventArgs e)
        {
            OnContainerTouchFlingNotify(this, e);
        }

        public override void Tick(float delta)
        {
            if (flingVelY != 0.0f)
            {
                var deltaPixel = (int)Math.Round(flingVelY * delta);
                if (deltaPixel != 0 && DoScroll(-deltaPixel))
                    flingVelY *= (float)Math.Exp(delta * -4.5f);
                else
                    flingVelY = 0.0f;
            }
            else
            {
                SetTickEnabled(false);
            }
        }

        protected override void OnRender(Graphics g)
        {
            g.DefaultCommandList.FillRectangle(ClientRectangle, Theme.DarkGreyColor4);
            base.OnRender(g);
        }
    }
}
