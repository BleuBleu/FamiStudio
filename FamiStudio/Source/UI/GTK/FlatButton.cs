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
        string buttonText;
        Pixbuf pixbuf;
#if FAMISTUDIO_LINUX
        static Pango.Context context;
        Pango.Layout layoutNormal;
        Pango.Layout layoutBold;
#endif

        public FlatButton(Pixbuf pb, string text = null)
        {
            pixbuf = pb;
            buttonText = text;

            Events |= Gdk.EventMask.ButtonPressMask | EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask | EventMask.ButtonReleaseMask;

            if (text == null)
            {
                WidthRequest  = GtkUtils.ScaleGtkWidget(40);
                HeightRequest = GtkUtils.ScaleGtkWidget(40);
            }

#if FAMISTUDIO_LINUX
            if (context == null)
            {
                context = CreatePangoContext();
            }

            var fontSize = Pango.Units.ToPixels(Style.FontDesc.Size);

            layoutNormal = new Pango.Layout(context);
            layoutNormal.Alignment = Pango.Alignment.Left;
            layoutNormal.SetText(text);
            layoutNormal.FontDescription = Pango.FontDescription.FromString($"Quicksand {fontSize}");

            layoutBold = new Pango.Layout(context);
            layoutBold.Alignment = Pango.Alignment.Left;
            layoutBold.SetText(text);
            layoutBold.FontDescription = Pango.FontDescription.FromString($"Quicksand Bold {fontSize}");
#endif
        }

        public bool Bold
        {
            get { return bold; }
            set { bold = value; QueueDraw(); }
        }

        public Pixbuf Pixbuf
        {
            get { return pixbuf; }
            set { pixbuf = value; QueueDraw(); }
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
            int width = Allocation.Width;
            int height = Allocation.Height;

#if FAMISTUDIO_MACOS
            var bgColor = Style.Backgrounds[(int)State];
            var fgColor = Style.Foregrounds[(int)State];

            int imgWidth  = (int)(pixbuf.Width  / DpiScaling.Dialog);
            int imgHeight = (int)(pixbuf.Height / DpiScaling.Dialog);

            int x = buttonText == null ? (width - imgWidth) / 2 : 5;
            int y = (height - imgHeight) / 2;
            int yp = State == Gtk.StateType.Active ? 1 : 0;

            // Have to use Cairo here since i am unable to draw unpixelated bitmaps with Gdk.
            var ctx = CairoHelper.Create(ev.Window);

            CairoHelper.SetSourceColor(ctx, bgColor);
            ctx.Paint();
            ctx.Translate(x, y + yp);
            ctx.Scale(1.0f / DpiScaling.Dialog, 1.0f / DpiScaling.Dialog);
            CairoHelper.SetSourcePixbuf(ctx, pixbuf, 0, 0);
            ctx.Paint();

            if (buttonText != null)
            {
                CairoHelper.SetSourceColor(ctx, fgColor);
                ctx.Scale(1.0f, 1.0f);
                ctx.SelectFontFace("Quicksand", FontSlant.Normal, bold ? FontWeight.Bold : FontWeight.Normal);
                ctx.SetFontSize(14 * DpiScaling.Dialog);
                ctx.MoveTo(20 * DpiScaling.Dialog, 13 * DpiScaling.Dialog);
                ctx.ShowText(buttonText);
            }

            ctx.Target.Dispose();
            ctx.Dispose();
#else
            using (Gdk.GC gc = new Gdk.GC((Drawable)base.GdkWindow))
            {
                int x = layoutNormal == null ? (width - pixbuf.Width) / 2 : 5;
                int y = (height - pixbuf.Height) / 2;
                int yp = State == Gtk.StateType.Active ? 1 : 0;

                GdkWindow.DrawRectangle(Style.BackgroundGC(State), true, 0, 0, width, height);

                if (layoutNormal != null)
                {
                    var layout = bold ? layoutBold : layoutNormal;

                    layout.Width = -1;
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
#endif
            return true;
        }

        protected override void OnSizeAllocated(Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated(allocation);
        }
    }
}
