using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class Button : Control
    {
        public event ControlDelegate Click;

        private string text;
        private string imageName;
        private TextureAtlasRef bmp;
        private int margin = DpiScaling.ScaleForWindow(4);
        private bool bold;
        private bool ellipsis;
        private bool border;
        private bool hover;
        private bool press;
        private bool transparent;
        private bool dimmed;

        private Color fgColorEnabled  = Theme.LightGreyColor1;
        private Color fgColorDisabled = Theme.MediumGreyColor1;
        private Color bgColor         = Theme.DarkGreyColor5;
        private Color bgColorPressed  = Theme.MediumGreyColor1;
        private Color bgColorHover    = Theme.DarkGreyColor6;
        
        public Color ForegroundColorEnabled  { get => fgColorEnabled;  set => fgColorEnabled  = value; }
        public Color ForegroundColorDisabled { get => fgColorDisabled; set => fgColorDisabled = value; }
        public Color BackgroundColor         { get => bgColor;         set => bgColor         = value; }
        public Color BackgroundColorPressed  { get => bgColorPressed;  set => bgColorPressed  = value; }
        public Color BackgroundColorHover    { get => bgColorHover;    set => bgColorHover    = value; }

        public Button(string img, string txt) 
        {
            Image = img;
            text  = txt;
        }

        public string Text
        {
            get { return text; }
            set { SetAndMarkDirty(ref text, value); }
        }

        public string Image
        {
            get { return imageName; }
            set { imageName = value; MarkDirty(); UpdateAtlasBitmap(); }
        }

        public bool BoldFont
        {
            get { return bold; }
            set { SetAndMarkDirty(ref bold, value); }
        }

        public bool Border
        {
            get { return border; }
            set { SetAndMarkDirty(ref border, value); }
        }

        public bool Ellipsis
        {
            get { return ellipsis; }
            set { SetAndMarkDirty(ref ellipsis, value); }
        }

        public bool Transparent
        {
            get { return transparent; }
            set { SetAndMarkDirty(ref transparent, value); }
        }

        public bool Dimmed
        {
            get { return dimmed; }
            set { SetAndMarkDirty(ref dimmed, value); }
        }

        public void AutosizeWidth()
        {
            // Only bothered to support this one use case.
            Debug.Assert(!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(imageName));
            width = margin * 3 + bmp.ElementSize.Width + ParentWindow.Fonts.FontMedium.MeasureString(text, false);
        }

        protected override void OnAddedToContainer()
        {
            UpdateAtlasBitmap();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (enabled && e.Left)
            {
                press = true;
            }
            hover = true;
            MarkDirty();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (enabled && e.Left)
            {
                press = false;
                Click?.Invoke(this);
            }
            MarkDirty();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            SetAndMarkDirty(ref hover, true);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            SetAndMarkDirty(ref hover, false);
            SetAndMarkDirty(ref press, false);
        }

        private void UpdateAtlasBitmap()
        {
            if (HasParent && !string.IsNullOrEmpty(imageName))
            {
                bmp = ParentWindow.Graphics.GetTextureAtlasRef(imageName);
                Debug.Assert(bmp != null);
            }
        }

        public void AutoSizeToImage()
        {
            Debug.Assert(bmp != null);
            Resize(bmp.ElementSize.Width, bmp.ElementSize.Height);
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();
            var bmpSize = bmp != null ? bmp.ElementSize : Size.Empty;
            var maxOpacity = transparent && dimmed ? 0.25f : 1.0f;
            var fgColor = enabled ? fgColorEnabled : fgColorDisabled;
            var opacity = Math.Min(maxOpacity, transparent ? enabled ? hover ? 0.5f : 1.0f : 0.25f : 1.0f);

            // Debug
            //c.FillRectangle(ClientRectangle, Color.Pink);

            // "Non-transparent" changes the BG on hover
            // "Transparent" changes the opacity on hover.
            if (enabled && !transparent && (border || press || hover))
            {
                var bgColor = press ? bgColorPressed : hover ? bgColorHover : this.bgColor;
                c.FillRectangle(ClientRectangle, bgColor);
            }

            if (border)
            {
                c.DrawRectangle(ClientRectangle, Theme.BlackColor);
            }

            var hasText = !string.IsNullOrEmpty(text);

            c.PushTranslation(0, press ? 1 : 0);

            if (!hasText && bmp != null)
            {
                c.DrawTextureAtlas(bmp, (width - bmpSize.Width) / 2, (height - bmpSize.Height) / 2, 1, fgColor.Transparent(opacity));
            }
            else if (hasText && bmp == null)
            {
                c.DrawText(text, bold ? Fonts.FontMediumBold : Fonts.FontMedium, 0, 0, fgColor, TextFlags.MiddleCenter | (ellipsis ? TextFlags.Ellipsis : 0), width, height);
            }
            else if (hasText && bmp != null)
            {
                c.DrawTextureAtlas(bmp, margin, (height - bmpSize.Height) / 2, 1, fgColor);
                c.DrawText(text, bold ? Fonts.FontMediumBold : Fonts.FontMedium, bmpSize.Width + margin * 2, 0, fgColor, TextFlags.MiddleLeft | TextFlags.Clip, width - bmpSize.Width - margin * 2, height);
            }

            c.PopTransform();
        }
    }
}
