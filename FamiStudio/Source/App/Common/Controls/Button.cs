using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class Button : Control
    {
        public delegate string ImageDelegate(Control sender, ref Color tint);
        public delegate bool BoolDelegate(Control sender);

        // This is basically the same as "MouseDown" but can mark the event as handled automatically.
        // It is fired before the MouseDown. ProjectExplorer uses this to handle clicks and drags of 
        // instrument envelopes.
        public event ControlDelegate Click;
        public event ControlDelegate RightClick;

        // These are "dynamic" and will take precedence over the regular image/enabled/dimmed flags.
        public event ImageDelegate ImageEvent;
        public event BoolDelegate EnabledEvent;
        public event BoolDelegate DimmedEvent;

        private string text;
        private string imageName;
        private float imageScale = 1.0f;
        private TextureAtlasRef bmp;
        private int margin = DpiScaling.ScaleForWindow(4);
        private bool bold;
        private bool ellipsis;
        private bool border;
        private bool hover;
        private bool press;
        private bool transparent;
        private bool dimmed;
        private bool clickOnMouseUp;
        private bool whiteHighlight;
        private bool handleOnClick = true;
        private bool vibrateOnClick;
        private bool vibrateOnRightClick;

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

        public bool ClickOnMouseUp      { get => clickOnMouseUp;      set => clickOnMouseUp      = value; }
        public bool MarkHandledOnClick  { get => handleOnClick;       set => handleOnClick       = value; }
        public bool VibrateOnClick      { get => vibrateOnClick;      set => vibrateOnClick      = value; }
        public bool VibrateOnRightClick { get => vibrateOnRightClick; set => vibrateOnRightClick = value; }

        public TextureAtlasRef Image => bmp;

        public Button(string img, string txt = null) 
        {
            ImageName = img;
            text  = txt;
        }

        public override bool Enabled 
        {
            get { return EnabledEvent != null ? EnabledEvent(this) : enabled; }
            set { base.Enabled = value; }
        }

        public string Text
        {
            get { return text; }
            set { SetAndMarkDirty(ref text, value); }
        }

        public string ImageName
        {
            get { return imageName; }
            set { if (SetAndMarkDirty(ref imageName, value)) UpdateAtlasBitmap(); }
        }

        public float ImageScale 
        {
            get { return imageScale; } 
            set { SetAndMarkDirty(ref imageScale, value); }
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
            get { return DimmedEvent != null ? DimmedEvent(this) : dimmed; }
            set { SetAndMarkDirty(ref dimmed, value); }
        }

        public bool WhiteHighlight 
        {
            get { return whiteHighlight; }
            set { SetAndMarkDirty(ref whiteHighlight, value); }
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

        private void TriggerClickEvent(MouseEventArgs e, bool left)
        {
            if (left)
            {
                if (Platform.IsMobile && vibrateOnClick)
                    Platform.VibrateTick();
                Click?.Invoke(this);
            }
            else
            {
                if (Platform.IsMobile && vibrateOnRightClick)
                    Platform.VibrateClick();
                RightClick?.Invoke(this);
            }

            if (handleOnClick)
                e.MarkHandled();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            var canRightClick = RightClick != null;

            if (enabled && (e.Left || (e.Right && canRightClick)))
            {
                press = true;
                if (Platform.IsDesktop && !clickOnMouseUp)
                    TriggerClickEvent(e, e.Left);
            }

            hover = Platform.IsDesktop;
            MarkDirty();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            var canRightClick = RightClick != null;

            if (Platform.IsDesktop && enabled && (e.Left || (e.Right && canRightClick)))
            {
                if (clickOnMouseUp)
                    TriggerClickEvent(e, e.Left);
            }

            SetAndMarkDirty(ref press, false);
        }

        protected override void OnTouchClick(MouseEventArgs e)
        {
            if (enabled)
            {
                TriggerClickEvent(e, true);
            }
        }

        protected override void OnTouchLongPress(MouseEventArgs e)
        {
            if (enabled)
            {
                TriggerClickEvent(e, false);
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            OnMouseDown(e);
        }

        protected override void OnMouseEnter(EventArgs e)
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
            Resize(
                DpiScaling.ScaleCustom(bmp.ElementSize.Width,  imageScale),
                DpiScaling.ScaleCustom(bmp.ElementSize.Height, imageScale));
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();
            var bmpSize = bmp != null ? bmp.ElementSize : Size.Empty;

            var localEnabled = Enabled;
            var localDimmed  = Dimmed;
            var tint = localEnabled || transparent ? fgColorEnabled : fgColorDisabled;

            if (ImageEvent != null)
            {
                var wantedImageName = ImageEvent(this, ref tint);
                if (wantedImageName != imageName)
                {
                    imageName = wantedImageName;
                    UpdateAtlasBitmap();
                }
            }

            var maxOpacity = transparent && localDimmed ? 0.25f : 1.0f;
            var opacity = Math.Min(maxOpacity, transparent ? localEnabled ? hover ? 0.5f : 1.0f : 0.25f : 1.0f);

            // Debug
            //c.DrawRectangle(ClientRectangle, Color.Pink);

            // "Non-transparent" changes the BG on hover
            // "Transparent" changes the opacity on hover.
            if (localEnabled && !transparent && (border || press || hover))
            {
                var bgColor = press ? bgColorPressed : hover ? bgColorHover : this.bgColor;
                c.FillRectangle(ClientRectangle, bgColor);
            }

            if (whiteHighlight)
            {
                c.DrawRectangle(ClientRectangle, Theme.WhiteColor, 3, true, true);
            }
            else if (border)
            {
                c.DrawRectangle(ClientRectangle, Theme.BlackColor);
            }

            var hasText = !string.IsNullOrEmpty(text);

            c.PushTranslation(0, press ? 1 : 0);

            if (!hasText && bmp != null)
            {
                c.DrawTextureAtlas(bmp, 
                    (width  - DpiScaling.ScaleCustom(bmpSize.Width,  imageScale)) / 2, 
                    (height - DpiScaling.ScaleCustom(bmpSize.Height, imageScale)) / 2, imageScale, tint.Transparent(opacity));
            }
            else if (hasText && bmp == null)
            {
                c.DrawText(text, bold ? Fonts.FontMediumBold : Fonts.FontMedium, 0, 0, tint, TextFlags.MiddleCenter | (ellipsis ? TextFlags.Ellipsis : 0), width, height);
            }
            else if (hasText && bmp != null)
            {
                c.DrawTextureAtlas(bmp, margin, (height - DpiScaling.ScaleCustom(bmpSize.Height, imageScale)) / 2, imageScale, tint);
                c.DrawText(text, bold ? Fonts.FontMediumBold : Fonts.FontMedium, bmpSize.Width + margin * 2, 0, tint, TextFlags.MiddleLeft | TextFlags.Clip, width - bmpSize.Width - margin * 2, height);
            }

            c.PopTransform();
        }
    }
}
