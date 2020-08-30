using Gdk;
using Gtk;
using System;

namespace FamiStudio
{
    [System.ComponentModel.ToolboxItem(true)]
    public class ColorSelector : Gtk.DrawingArea
    {
        private int desiredWidth = 100;
        private Pixbuf imgOriginal;
        private Pixbuf imgScaled;
        private int selectedColor;

        public int DesiredWidth { get => desiredWidth; set => desiredWidth = value; }

        public ColorSelector(Pixbuf img, int color)
        {
            imgOriginal = img;
            imgScaled = img;
            selectedColor = color;
            Events |= Gdk.EventMask.ButtonPressMask | EventMask.ButtonMotionMask;
        }

        public int SelectedColor 
        {
            get { return selectedColor; }
            set { selectedColor = value; QueueDraw(); }
        }

        protected override bool OnExposeEvent(Gdk.EventExpose ev)
        {
            base.OnExposeEvent(ev);

            using (Gdk.GC gc = new Gdk.GC((Drawable)base.GdkWindow))
            {
                GdkWindow.DrawPixbuf(gc, imgScaled, 0, 0, 0, 0, Allocation.Width, Allocation.Height, RgbDither.None, 0, 0);

#if FAMISTUDIO_LINUX
                int y = selectedColor / imgOriginal.Width;
                int x = selectedColor % imgOriginal.Width;
                var sx = imgScaled.Width / (float)imgOriginal.Width;
                var sy = imgScaled.Height / (float)imgOriginal.Height;

                GdkWindow.DrawRectangle(gc, false, new Rectangle((int)(x * sx), (int)(y * sy), (int)sx - 1, (int)sy - 1));
#endif
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
