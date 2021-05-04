using System;
using System.Diagnostics;
using Gdk;
using Cairo;

namespace FamiStudio
{
    public class CairoImage : Gtk.DrawingArea
    {
        Pixbuf pixbuf;

        public CairoImage(Pixbuf pb)
        {
            pixbuf = pb;
            WidthRequest  = GtkUtils.ScaleGtkWidget(pb.Width);
            HeightRequest = GtkUtils.ScaleGtkWidget(pb.Height);
        }

        public Pixbuf Pixbuf
        {
            get { return pixbuf; }
            set { pixbuf = value; QueueDraw(); }
        }

        protected override bool OnExposeEvent(Gdk.EventExpose ev)
        {
            int width  = Allocation.Width;
            int height = Allocation.Height;

            var bgColor = Style.Backgrounds[(int)State];

            int imgWidth  = (int)(pixbuf.Width  / GLTheme.DialogScaling);
            int imgHeight = (int)(pixbuf.Height / GLTheme.DialogScaling);

            int x = (width  - imgWidth)  / 2;
            int y = (height - imgHeight) / 2;

            // Have to use Cairo here since i am unable to draw unpixelated bitmaps with Gdk.
            var ctx = CairoHelper.Create(ev.Window);

            CairoHelper.SetSourceColor(ctx, bgColor);
            ctx.Paint();
            ctx.Translate(x, y);
            ctx.Scale(1.0f / GLTheme.DialogScaling, 1.0f / GLTheme.DialogScaling);
            CairoHelper.SetSourcePixbuf(ctx, pixbuf, 0, 0);
            ctx.Paint();

            ctx.Target.Dispose();
            ctx.Dispose();

            return true;
        }
    }
}
