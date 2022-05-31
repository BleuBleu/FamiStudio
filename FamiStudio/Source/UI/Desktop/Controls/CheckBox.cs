namespace FamiStudio
{
    public class CheckBox : Control
    {
        public delegate void CheckedChangedDelegate(Control sender, bool check);
        public event CheckedChangedDelegate CheckedChanged;

        private string text;
        private bool check;
        private bool hover;
        private int margin = DpiScaling.ScaleForMainWindow(4);
        private BitmapAtlasRef bmpCheckOn;
        private BitmapAtlasRef bmpCheckOff;

        public CheckBox(bool chk, string txt = null)
        {
            text = txt;
            check = chk;
            height = DpiScaling.ScaleForMainWindow(24);
        }

        public bool Checked
        {
            get { return check; }
            set { if (SetAndMarkDirty(ref check, value)) CheckedChanged?.Invoke(this, check); }
        }

        protected override void OnRenderInitialized(Graphics g)
        {
            bmpCheckOn  = g.GetBitmapAtlasRef("CheckBoxYes");
            bmpCheckOff = g.GetBitmapAtlasRef("CheckBoxNo");
        }

        public bool IsPointInCheckBox(int x, int y)
        {
            var bmpSize = bmpCheckOn.ElementSize;
            var baseY = (height - bmpSize.Height) / 2;

            return x >= 0 && y >= baseY && x < bmpSize.Width && y < baseY + bmpSize.Height;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (enabled && IsPointInCheckBox(e.X, e.Y))
                Checked = !Checked;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            SetAndMarkDirty(ref hover, IsPointInCheckBox(e.X, e.Y));
       }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            SetAndMarkDirty(ref hover, false);
        }

        protected override void OnRender(Graphics g)
        {
            var c = parentDialog.CommandList;
            var bmpSize = bmpCheckOn.ElementSize;
            var baseY = (height - bmpSize.Height) / 2;
            var brush = enabled ? ThemeResources.LightGreyFillBrush1 : ThemeResources.MediumGreyFillBrush1;

            c.FillAndDrawRectangle(0, baseY, bmpSize.Width - 1, baseY + bmpSize.Height - 1, hover && enabled ? ThemeResources.DarkGreyLineBrush3 : ThemeResources.DarkGreyLineBrush1, brush);
            c.DrawBitmapAtlas(check ? bmpCheckOn : bmpCheckOff, 0, baseY, 1, 1, brush.Color0);

            if (!string.IsNullOrEmpty(text))
                c.DrawText(text, ThemeResources.FontMedium, bmpSize.Width + margin, 0, brush, TextFlags.MiddleLeft, 0, height);
        }
    }
}
