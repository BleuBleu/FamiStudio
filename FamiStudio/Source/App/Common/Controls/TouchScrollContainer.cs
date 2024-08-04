using Android.Widget;
using Java.Sql;
using System;

namespace FamiStudio
{
    public class TouchScrollContainer : Container
    {
        private bool panning;
        private bool canFling;
        private bool border;
        private int lastDialogY;
        private int virtualSizeY;
        private int captureCookie;
        private float flingVelY;
        private GraphicsLayer layer = GraphicsLayer.Default;
        private ScrollIndicatorVisibility scrollIndicator = ScrollIndicatorVisibility.ShowAlways;

        private int   scrollIndicatorSizeX = DpiScaling.ScaleForWindow(2);
        private Color scollIndicatorColor  = Color.FromArgb(64, Theme.LightGreyColor2);

        public enum ScrollIndicatorVisibility
        {
            Hide,
            ShowWhenScrolling,
            ShowAlways
        }

        public TouchScrollContainer()
        {
        }

        public int VirtualSizeY
        {
            get { return virtualSizeY; }
            set { if (SetAndMarkDirty(ref virtualSizeY, Math.Max(0, value))) ClampScroll(); }
        }

        public ScrollIndicatorVisibility ScrollIndicator
        {
            get { return scrollIndicator; }
            set { SetAndMarkDirtyEnum(ref scrollIndicator, value); }
        }
        public bool Border 
        {
            get { return border; }
            set { SetAndMarkDirty(ref border, value); }
        }

        public GraphicsLayer Layer
        {
            get { return layer; }
            set { SetAndMarkDirtyEnum(ref layer, value); }
        }

        public Color ScollIndicatorColor
        {
            get { return scollIndicatorColor; }
            set { SetAndMarkDirty(ref scollIndicatorColor, value); }
        }

        private bool DoScroll(int deltaY)
        {
            ScrollY += deltaY;
            return ClampScroll();
        }

        public void CancelFling()
        {
            SetAndMarkDirty(ref panning, false);
            SetAndMarkDirty(ref flingVelY, 0.0f);
        }

        public bool ClampScroll()
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
            flingVelY = 0.0f;

            if (!e.Handled && e.Left)
            {
                panning = true;
                canFling = false;
                lastDialogY = WindowToControl(control.ControlToWindow(e.Position)).Y;
                captureCookie = control.CapturePointer();
            }
        }

        public override void OnContainerPointerUpNotify(Control control, PointerEventArgs e)
        {
            if (e.Left && panning)
            {
                panning = false;
                canFling = true;
            }
        }

        public override void OnContainerPointerMoveNotify(Control control, PointerEventArgs e)
        {
            var dialogY = WindowToControl(control.ControlToWindow(e.Position)).Y;

            if (panning)
            {
                // This can happen if a control captures after the initial pointer-down. 
                // The slider is an example of this, since it has a bit of slop.
                if (!CheckPointerCaptureCookie(captureCookie))
                {
                    panning = false;
                    flingVelY = 0.0f;
                    return;
                }

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

                MarkDirty();
            }
            else
            {
                SetTickEnabled(false);
            }
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList(layer);

            c.FillRectangle(ClientRectangle, Theme.DarkGreyColor4);

            base.OnRender(g);

            if ((scrollIndicator == ScrollIndicatorVisibility.ShowAlways) ||
                (scrollIndicator == ScrollIndicatorVisibility.ShowWhenScrolling && (flingVelY != 0.0 || panning)))
            {
                if (virtualSizeY > height)
                {
                    var sy = (int)Math.Round(height * (height           / (float)virtualSizeY));
                    var py = (int)Math.Round(height * (containerScrollY / (float)virtualSizeY));
                    var rect = new Rectangle(width - scrollIndicatorSizeX, py, width, sy);

                    c.FillRectangle(rect, scollIndicatorColor);
                }
            }

            if (border)
            {
                c.DrawRectangle(ClientRectangle, Color.Black);
            }
        }
    }
}
