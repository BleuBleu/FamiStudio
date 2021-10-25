using System.Diagnostics;
using Gtk;

namespace FamiStudio
{
    public class CellRendererButton : CellRendererText
    {
        public int LastMouseX { get; set; } = -100;
        public int LastMouseY { get; set; } = -100;

        public Gdk.Rectangle GetButtonRectangle(Gdk.Rectangle rc)
        {
            var buttonSize = rc.Height;
            var buttonRect = new Gdk.Rectangle(rc.Right - buttonSize, rc.Top, buttonSize, buttonSize);
            buttonRect.Inflate(-4, -4);
            return buttonRect;
        }

        protected override void Render(Gdk.Drawable window, Widget widget, Gdk.Rectangle backgroundArea, Gdk.Rectangle cellArea, Gdk.Rectangle exposeArea, CellRendererState flags)
        {
            base.Render(window, widget, backgroundArea, cellArea, exposeArea, flags);

            var buttonRect = GetButtonRectangle(backgroundArea);
            var highlight  = buttonRect.Contains(LastMouseX, LastMouseY); // flags.HasFlag(CellRendererState.Prelit) 

            Gdk.GC gcBlack = widget.Style.BlackGC;
            Gdk.GC gcBack  = widget.Style.BackgroundGC(highlight ? StateType.Selected : StateType.Normal);
            Gdk.GC gcText  = widget.Style.TextGC(highlight ? StateType.Selected : StateType.Normal);

            window.DrawRectangle(gcBack,  true,  buttonRect);
            window.DrawRectangle(gcBlack, false, buttonRect);

            Pango.Layout layout = new Pango.Layout(widget.PangoContext);
            layout.SetText("...");
            layout.Alignment = Pango.Alignment.Center;
            layout.Width = buttonRect.Width;
            window.DrawLayout(gcText, buttonRect.Left + buttonRect.Width / 2, buttonRect.Top, layout);
        }
    }
}
