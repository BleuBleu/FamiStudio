using System;
using System.Diagnostics;
using Gdk;
using Cairo;

namespace FamiStudio
{
    public class FlatButton : Gtk.DrawingArea
    {
        bool focus;
        bool bold;
        ImageSurface image;
        static Pango.Context context;
        Pango.Layout layoutNormal;
        Pango.Layout layoutBold;

        public FlatButton(Pixbuf pb, string text = null)
        {
            image = GdkPixbufToCairoImageSurface(pb);

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
            else
            {
                WidthRequest  = 40;
                HeightRequest = 40;
            }
        }

        unsafe ImageSurface GdkPixbufToCairoImageSurface(Pixbuf pb)
        {
            byte* p = (byte*)pb.Pixels.ToPointer();

            for (int y = 0; y < pb.Height; y++, p += pb.Rowstride)
            {
                for (int x = 0; x < pb.Width * 4; x += 4)
                {
                    byte tmp = p[x + 0];
                    p[x + 0] = p[x + 2];
                    p[x + 2] = tmp;
                }
            }

            return new ImageSurface(pb.Pixels, Format.ARGB32, pb.Width, pb.Height, pb.Rowstride);
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

            var bgColor = Style.Backgrounds[(int)State];

            int width  = Allocation.Width;
            int height = Allocation.Height;

            int imgWidth  = (int)(image.Width  / GLTheme.DialogScaling);
            int imgHeight = (int)(image.Height / GLTheme.DialogScaling);

            int x = layoutNormal == null ? (width - imgWidth) / 2 : 5;
            int y = (height - imgHeight) / 2;
            int yp = State == Gtk.StateType.Active ? 1 : 0;

            // Have to use Cairo here since i am unable to draw unpixelated bitmaps with Gdk.
            var ctx = CairoHelper.Create(ev.Window);
            ctx.SetSourceRGB(bgColor.Red / 65535.0f, bgColor.Green / 65535.0f, bgColor.Blue / 65535.0f);
            ctx.Paint();
            ctx.Translate(x, y + yp);
            ctx.Scale(1.0f / GLTheme.DialogScaling, 1.0f / GLTheme.DialogScaling);
            ctx.SetSource(image);
            ctx.Paint();
            ctx.Target.Dispose();
            ctx.Dispose();

            if (layoutNormal != null)
            {
                using (Gdk.GC gc = new Gdk.GC((Drawable)base.GdkWindow))
                {
                    var layout = bold ? layoutBold : layoutNormal;

                    layout.Width = Allocation.Width - image.Width - 10;
                    layout.GetSize(out _, out int layoutHeight);
                    layoutHeight = Pango.Units.ToPixels(layoutHeight);

                    GdkWindow.DrawLayout(Style.ForegroundGC(State), 10 + image.Width, (height - layoutHeight) / 2 + yp, layout);
                }
            }

            return true;
        }

        protected override void OnSizeAllocated(Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated(allocation);
        }
    }
}
