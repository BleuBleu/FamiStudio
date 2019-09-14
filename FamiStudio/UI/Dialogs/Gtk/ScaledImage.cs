using Gdk;
using Gtk;
using System;

namespace FamiStudio
{
    [System.ComponentModel.ToolboxItem(true)]
    public class ScaledImage : Gtk.DrawingArea
    {
        private int desiredWidth = 100;
        private Pixbuf imgOriginal;
        private Pixbuf imgScaled;

        public int DesiredWidth { get => desiredWidth; set => desiredWidth = value; }

        public ScaledImage(Pixbuf img)
        {
            imgOriginal = img;
            imgScaled = img;
            Events |= Gdk.EventMask.ButtonPressMask | EventMask.ButtonMotionMask;
        }

        protected override bool OnExposeEvent(Gdk.EventExpose ev)
        {
            base.OnExposeEvent(ev);

            using (Gdk.GC gc = new Gdk.GC((Drawable)base.GdkWindow))
            {
                GdkWindow.DrawPixbuf(gc, imgScaled, 0, 0, 0, 0, Allocation.Width, Allocation.Height, RgbDither.None, 0, 0);
            }

            return true;
        }

        protected override void OnSizeAllocated(Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated(allocation);
            imgScaled = imgOriginal.ScaleSimple(allocation.Width, allocation.Height, InterpType.Nearest);
        }

        protected override void OnSizeRequested(ref Gtk.Requisition requisition)
        {
            requisition.Width  = desiredWidth;
            requisition.Height = (int)(desiredWidth * (imgOriginal.Height / (float)imgOriginal.Width));
        }
    }
}
