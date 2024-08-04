namespace FamiStudio
{
    public class CheckBox : Control
    {
        public delegate void CheckedChangedDelegate(Control sender, bool check);
        public event CheckedChangedDelegate CheckedChanged;

        private string text;
        private bool check;
        private bool hover;
        private bool rightAlign;
        private float imageScale = Platform.IsDesktop ? 1.0f : DpiScaling.ScaleForWindowFloat(1.0f / 6.0f);
        private int scaledCheckboxSize;
        private int margin = DpiScaling.ScaleForWindow(4);
        private TextureAtlasRef bmpCheckOn;
        private TextureAtlasRef bmpCheckOff;

        public CheckBox(bool chk, string txt = null)
        {
            text = txt;
            check = chk;
            height = DpiScaling.ScaleForWindow(24);
            supportsDoubleClick = false;
        }

        public string Text => text;

        public bool RightAlign
        {
            get { return rightAlign; }
            set { SetAndMarkDirty(ref rightAlign, value); }
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
            scaledCheckboxSize = DpiScaling.ScaleCustom(bmpCheckOn.ElementSize.Height, imageScale);
        }

        public bool IsPointInCheckBox(int x, int y)
        {
            var baseX = rightAlign ? (width - scaledCheckboxSize) : 0;
            var baseY = (height - scaledCheckboxSize) / 2;

            return x >= baseX && y >= baseY && x < baseX + scaledCheckboxSize && y < baseY + scaledCheckboxSize;
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            if (enabled && IsPointInCheckBox(e.X, e.Y) && !e.IsTouchEvent)
                Checked = !Checked;
        }

        protected override void OnTouchClick(PointerEventArgs e)
        {
            // Allow click on label on mobile.
            if (enabled)
                Checked = !Checked;
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
            var baseX = rightAlign ? (width - scaledCheckboxSize) : 0;
            var baseY = (height - scaledCheckboxSize) / 2;
            var color = enabled ? Theme.LightGreyColor1 : Theme.MediumGreyColor1;

            c.FillAndDrawRectangle(baseX, baseY, baseX + scaledCheckboxSize - 1, baseY + scaledCheckboxSize - 1, hover && enabled ? Theme.DarkGreyColor3 : Theme.DarkGreyColor1, color);
            c.DrawTextureAtlas(check ? bmpCheckOn : bmpCheckOff, baseX, baseY, imageScale, color);

            if (!string.IsNullOrEmpty(text))
                c.DrawText(text, Fonts.FontMedium, scaledCheckboxSize + margin, 0, color, TextFlags.MiddleLeft, 0, height);
        }
    }
}
