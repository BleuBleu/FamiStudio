namespace FamiStudio
{
    public class CheckBox : Control
    {
        public delegate void CheckedChangedDelegate(Control sender, bool check);
        public event CheckedChangedDelegate CheckedChanged;

        private string text;
        private bool check;
        private bool hover;
        private int margin = DpiScaling.ScaleForWindow(4);
        private TextureAtlasRef bmpCheckOn;
        private TextureAtlasRef bmpCheckOff;

        public CheckBox(bool chk, string txt = null)
        {
            text = txt;
            check = chk;
            height = DpiScaling.ScaleForWindow(24); // MATTT : Checkbox must scale on mobile. Also, accept click anywhere in its area, even on label.
        }

        public bool Checked
        {
            get { return check; }
            set { if (SetAndMarkDirty(ref check, value)) CheckedChanged?.Invoke(this, check); }
        }

        protected override void OnAddedToContainer()
        {
            bmpCheckOn  = window.Graphics.GetTextureAtlasRef("CheckBoxYes");
            bmpCheckOff = window.Graphics.GetTextureAtlasRef("CheckBoxNo");
        }

        public bool IsPointInCheckBox(int x, int y)
        {
            var bmpSize = bmpCheckOn.ElementSize;
            var baseY = (height - bmpSize.Height) / 2;

            return x >= 0 && y >= baseY && x < bmpSize.Width && y < baseY + bmpSize.Height;
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            if (enabled && IsPointInCheckBox(e.X, e.Y))
                Checked = !Checked;
        }

        protected override void OnMouseDoubleClick(PointerEventArgs e)
        {
            OnPointerDown(e);
        }

        protected override void OnPointerMove(PointerEventArgs e)
        {
            SetAndMarkDirty(ref hover, IsPointInCheckBox(e.X, e.Y));
       }

        protected override void OnPointerLeave(System.EventArgs e)
        {
            SetAndMarkDirty(ref hover, false);
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();
            var bmpSize = bmpCheckOn.ElementSize;
            var baseY = (height - bmpSize.Height) / 2;
            var color = enabled ? Theme.LightGreyColor1 : Theme.MediumGreyColor1;

            c.FillAndDrawRectangle(0, baseY, bmpSize.Width - 1, baseY + bmpSize.Height - 1, hover && enabled ? Theme.DarkGreyColor3 : Theme.DarkGreyColor1, color);
            c.DrawTextureAtlas(check ? bmpCheckOn : bmpCheckOff, 0, baseY, 1, color);

            if (!string.IsNullOrEmpty(text))
                c.DrawText(text, Fonts.FontMedium, bmpSize.Width + margin, 0, color, TextFlags.MiddleLeft, 0, height);
        }
    }
}
