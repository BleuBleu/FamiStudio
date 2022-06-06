using System.Diagnostics;

namespace FamiStudio
{
    public class LinkLabel : Control
    {
        private string text;
        private string url;
        private bool hover;
        private int lineOffset = DpiScaling.ScaleForWindow(4);

        public LinkLabel(Dialog dlg, string txt, string link) : base(dlg)
        {
            text = txt;
            url = link;
            height = DpiScaling.ScaleForWindow(24);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            // MATTT : Need hand cursor here!
            var insideText = e.X >= 0 && e.X < MeasureString();
            Cursor = insideText ? Cursors.CopyCursor : Cursors.Default;
            SetAndMarkDirty(ref hover, insideText);
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            Cursor = Cursors.Default;
            SetAndMarkDirty(ref hover, false);
        }

        private int MeasureString()
        {
            return ThemeResources.FontMedium.MeasureString(text, false);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Left)
                Platform.OpenUrl(url);
        }

        protected override void OnRender(Graphics g)
        {
            Debug.Assert(enabled); // TODO : Add support for disabled state.

            var c = parentDialog.CommandList;
            var sx = MeasureString();
            var brush = hover ? ThemeResources.LightGreyFillBrush2 : ThemeResources.LightGreyFillBrush1;

            c.DrawText(text, ThemeResources.FontMedium, 0, 0, brush, TextFlags.MiddleLeft, 0, height);
            c.DrawLine(0, height  - lineOffset, sx, height - lineOffset, brush);
        }
    }
}
