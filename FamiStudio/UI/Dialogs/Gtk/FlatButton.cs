using System;
using System.Diagnostics;
using Gdk;

namespace FamiStudio
{
    public class FlatButton : Gtk.DrawingArea
    {
        bool focus;
        bool bold;
        Pixbuf pixbuf;
        static Pango.Context context;
        Pango.Layout layoutNormal;
        Pango.Layout layoutBold;

        public FlatButton(Pixbuf pb, string text = null)
        {
            pixbuf = pb;
            Events |= Gdk.EventMask.ButtonPressMask | EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask | EventMask.ButtonReleaseMask;

            if (text != null)
            {
                if (context == null)
                {
                    context = CreatePangoContext();
                }

                layoutNormal = new Pango.Layout(context);
                layoutNormal.Alignment = Pango.Alignment.Left;
                layoutNormal.SetText(text);
                layoutNormal.FontDescription = Pango.FontDescription.FromString("Quicksand 14");

                layoutBold = new Pango.Layout(context);
                layoutBold.Alignment = Pango.Alignment.Left;
                layoutBold.SetText(text);
                layoutBold.FontDescription = Pango.FontDescription.FromString("Quicksand Bold 14");
            }
        }

        public bool Bold
        {
            get { return bold; }
            set { bold = value; QueueDraw(); }
        }

        protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
        {
            if (evnt.Button == 1)
            {
                State = Gtk.StateType.Active;
                QueueDraw();
            }
            return base.OnButtonPressEvent(evnt);
        }

        protected override bool OnButtonReleaseEvent(EventButton evnt)
        {
            if (evnt.Button == 1)
            {
                State = focus ? Gtk.StateType.Prelight : Gtk.StateType.Normal;
                QueueDraw();
            }
            return base.OnButtonReleaseEvent(evnt);
        }

        protected override bool OnEnterNotifyEvent(EventCrossing evnt)
        {
            focus = true;
            State = Gtk.StateType.Prelight;
            QueueDraw();
            return base.OnEnterNotifyEvent(evnt);
        }

        protected override bool OnLeaveNotifyEvent(EventCrossing evnt)
        {
            focus = false;
            State = Gtk.StateType.Normal;
            QueueDraw();
            return base.OnLeaveNotifyEvent(evnt);
        }

        protected override bool OnExposeEvent(Gdk.EventExpose ev)
        {
            base.OnExposeEvent(ev);

            using (Gdk.GC gc = new Gdk.GC((Drawable)base.GdkWindow))
            {
                int width  = Allocation.Width;
                int height = Allocation.Height;

                int x = layoutNormal == null ? (width - pixbuf.Width) / 2 : 5;
                int y = (height - pixbuf.Height) / 2;
                int yp = State == Gtk.StateType.Active ? 1 : 0;

                GdkWindow.DrawRectangle(Style.BackgroundGC(State), true, 0, 0, width, height);

                if (layoutNormal != null)
                {
                    var layout = bold ? layoutBold : layoutNormal;

                    layout.Width = Allocation.Width - pixbuf.Width - 10;
                    layout.GetSize(out _, out int layoutHeight);
                    layoutHeight = Pango.Units.ToPixels(layoutHeight);

                    GdkWindow.DrawPixbuf(gc, pixbuf, 0, 0, x, y + yp, pixbuf.Width, pixbuf.Height, RgbDither.None, 0, 0);
                    GdkWindow.DrawLayout(Style.ForegroundGC(State), 10 + pixbuf.Width, (height - layoutHeight) / 2 + yp, layout);
                }
                else
                {
                    GdkWindow.DrawPixbuf(gc, pixbuf, 0, 0, x, y + yp, pixbuf.Width, pixbuf.Height, RgbDither.None, 0, 0);
                }
            }

            return true;
        }

        protected override void OnSizeAllocated(Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated(allocation);
        }

        protected override void OnSizeRequested(ref Gtk.Requisition requisition)
        {
            requisition.Width  = pixbuf.Width  + 8;
            requisition.Height = pixbuf.Height + 8;
        }
    }
}
