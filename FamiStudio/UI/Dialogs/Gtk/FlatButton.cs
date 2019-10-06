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

        public FlatButton(Pixbuf pb, string text = null)
        {
            pixbuf = pb;
            buttonText = text;

            Events |= Gdk.EventMask.ButtonPressMask | EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask | EventMask.ButtonReleaseMask;

            if (text == null)
            {
                WidthRequest  = 40;
                HeightRequest = 40;
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
            var bgColor = Style.Backgrounds[(int)State];
            var fgColor = Style.Foregrounds[(int)State];

            int width  = Allocation.Width;
            int height = Allocation.Height;

            int imgWidth  = (int)(pixbuf.Width  / GLTheme.DialogScaling);
            int imgHeight = (int)(pixbuf.Height / GLTheme.DialogScaling);

            int x = buttonText == null ? (width - imgWidth) / 2 : 5;
            int y = (height - imgHeight) / 2;
            int yp = State == Gtk.StateType.Active ? 1 : 0;

            // Have to use Cairo here since i am unable to draw unpixelated bitmaps with Gdk.
            var ctx = CairoHelper.Create(ev.Window);

            CairoHelper.SetSourceColor(ctx, bgColor);
            ctx.Paint();
            ctx.Translate(x, y + yp);
            ctx.Scale(1.0f / GLTheme.DialogScaling, 1.0f / GLTheme.DialogScaling);
            CairoHelper.SetSourcePixbuf(ctx, pixbuf, 0, 0);
            ctx.Paint();

            if (buttonText != null)
            {
                CairoHelper.SetSourceColor(ctx, fgColor);
                ctx.Scale(1.0f, 1.0f);
                ctx.SelectFontFace("Quicksand", FontSlant.Normal, bold ? FontWeight.Bold : FontWeight.Normal);
                ctx.SetFontSize(14 * GLTheme.DialogScaling);
                ctx.MoveTo(20 * GLTheme.DialogScaling, 13 * GLTheme.DialogScaling);
                ctx.ShowText(buttonText);
            }

            ctx.Target.Dispose();
            ctx.Dispose();

            return true;
        }

        protected override void OnSizeAllocated(Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated(allocation);
        }
    }
}
