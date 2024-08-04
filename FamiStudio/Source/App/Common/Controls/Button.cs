using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class Button : Control
    {
        public delegate string StringDelegate(Control sender);
        public delegate string ImageDelegate(Control sender, ref Color tint);
        public delegate bool BoolDelegate(Control sender);
        public delegate bool DimmedDelegate(Control sender, ref int dimming);

        // This is basically the same as "MouseDown" but can mark the event as handled automatically.
        // It is fired before the MouseDown. ProjectExplorer uses this to handle clicks and drags of 
        // instrument envelopes.
        public event ControlDelegate Click;
        public event ControlDelegate RightClick;

        // These are "dynamic" and will take precedence over the regular image/enabled/dimmed flags.
        public event ImageDelegate ImageEvent;
        public event BoolDelegate EnabledEvent;
        public event DimmedDelegate DimmedEvent;
        public event StringDelegate TextEvent;

        private string text;
        private string imageName;
        private float imageScale = 1.0f;
        private bool ellipsis;
        private bool border;
        private bool hover;
        private bool press;
        private bool dimmed;
        private bool transparent;
        private bool clickOnMouseUp;
        private bool whiteHighlight;
        private bool handleOnClick = true;
        private bool vibrateOnClick;
        private bool vibrateOnRightClick;
        private bool bottomText;
        private byte dimming = 64;
        private byte margin = 4;
        private Font font;
        private Size scaledImageSize;
        private TextureAtlasRef bmp;

        private Color fgColor = Theme.LightGreyColor1;
        private Color bgColor = Theme.DarkGreyColor5; 

        public Color ForegroundColor { get => fgColor; set => fgColor = value; }
        public Color BackgroundColor { get => bgColor; set => bgColor = value; }

        public bool ClickOnMouseUp      { get => clickOnMouseUp;      set => clickOnMouseUp      = value; }
        public bool MarkHandledOnClick  { get => handleOnClick;       set => handleOnClick       = value; }
        public bool VibrateOnClick      { get => vibrateOnClick;      set => vibrateOnClick      = value; }
        public bool VibrateOnRightClick { get => vibrateOnRightClick; set => vibrateOnRightClick = value; }

        public TextureAtlasRef Image => bmp;

        public Button(string img, string txt = null) 
        {
            supportsLongPress = false;
            supportsDoubleClick = false;
            ImageName = img;
            text = txt;
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

        public Font Font
        {
            get { return font; }
            set { font = value; MarkDirty(); }
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

        public int Dimming
        {
            get { return dimming; }
            set { SetAndMarkDirty(ref dimming, (byte)value); }
        }

        public bool WhiteHighlight 
        {
            get { return whiteHighlight; }
            set { SetAndMarkDirty(ref whiteHighlight, value); }
        }

        public bool BottomText 
        {
            get { return bottomText; }
            set { SetAndMarkDirty(ref bottomText, value); }
        }

        public int Margin 
        {
            get { return margin; }
            set { SetAndMarkDirty(ref margin, (byte)value); }
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(text) ? imageName : text;
        }

        public Rectangle ImageRect
        {
            get
            {
                // Only bothered to support this one use case.
                if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(imageName))
                {
                    return new Rectangle(0, 0, GetScaledMargin() * 2 + scaledImageSize.Width, height);
                }
                else
                {
                    return Rectangle.Empty; 
                }
            }
        }

        public void SetSupportsDoubleClick(bool on)
        {
            supportsDoubleClick = on;
        }

        public void AutosizeWidth()
        {
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(imageName))
            {
                Debug.Assert(!bottomText);
                width = GetScaledMargin() * 3 + scaledImageSize.Width + GetFontInternal().MeasureString(text, false);
            }
            else if (!string.IsNullOrEmpty(imageName))
            {
                width = scaledImageSize.Width;
            }
            else if (!string.IsNullOrEmpty(text))
            {
                width = GetFontInternal().MeasureString(text, false);
            }
        }

        private Font GetFontInternal()
        {
            return font == null ? fonts.FontMedium : font;
        }

        private int GetScaledMargin()
        {
            return DpiScaling.ScaleForWindow(margin);
        }

        protected override void OnAddedToContainer()
        {
            UpdateAtlasBitmap();
        }

        public void TriggerClick()
        {
            Click?.Invoke(this);
        }

        private void TriggerClickEvent(PointerEventArgs e, bool left)
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

        protected override void OnPointerDown(PointerEventArgs e)
        {
            var canRightClick = RightClick != null;

            if (Enabled && (e.Left || (e.Right && canRightClick)))
            {
                if (!e.IsTouchEvent && !clickOnMouseUp)
                    TriggerClickEvent(e, e.Left);
                press = true;
            }

            MarkDirty();
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            var canRightClick = RightClick != null;

            if (Enabled && (!e.IsTouchEvent && e.Left || (e.Right && canRightClick)))
            {
                if (clickOnMouseUp || e.IsTouchEvent)
                    TriggerClickEvent(e, e.Left);
            }

            SetAndMarkDirty(ref press, false);
        }

        protected override void OnTouchClick(PointerEventArgs e)
        {
            if (Enabled)
            {
                TriggerClickEvent(e, true);
            }
        }

        protected override void OnPointerEnter(EventArgs e)
        {
            SetAndMarkDirty(ref hover, true);
        }

        protected override void OnPointerLeave(EventArgs e)
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
                scaledImageSize = new Size(
                    DpiScaling.ScaleCustom(bmp.ElementSize.Width,  imageScale),
                    DpiScaling.ScaleCustom(bmp.ElementSize.Height, imageScale));
            }
        }

        public void AutoSizeToImage()
        {
            Debug.Assert(bmp != null);
            Resize(scaledImageSize);
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();

            var localFont = GetFontInternal();
            var localEnabled = Enabled;
            var localDimmed  = dimmed;
            var localDimming = (int)dimming;
            var localFgColor = fgColor;

            if (ImageEvent != null)
            {
                var wantedImageName = ImageEvent(this, ref localFgColor);
                if (wantedImageName != imageName)
                {
                    imageName = wantedImageName;
                    UpdateAtlasBitmap();
                }
            }

            if (DimmedEvent != null)
            {
                localDimmed = DimmedEvent.Invoke(this, ref localDimming);
            }

            // "Non-transparent" changes the BG on hover
            // "Transparent" changes the opacity on hover.
            if (transparent)
            {
                var maxOpacity = transparent && localDimmed ? localDimming : 255;
                var opacity = Math.Min(maxOpacity, transparent ? localEnabled ? hover ? 128 : 255 : 64 : 255);

                localFgColor = localFgColor.Transparent(opacity, true);
            }
            else if (!localEnabled)
            {
                localFgColor = localFgColor.Scaled(128);
            }

            // MATTT : On mobile, would be nice to have transparent buttons changed their opacities a bit.
            if (localEnabled && !transparent && (border || press || hover))
            {
                var localBgColor = bgColor;
                
                if (press)
                    localBgColor = localBgColor.Scaled(384, true);
                else if (hover)
                    localBgColor = localBgColor.Scaled(300, true);

                c.FillRectangle(ClientRectangle, localBgColor);
            }

            if (whiteHighlight)
            {
                c.DrawRectangle(ClientRectangle, Theme.WhiteColor, 3, true, true);
            }
            else if (border)
            {
                c.DrawRectangle(ClientRectangle, Theme.BlackColor);
            }

            var localText = TextEvent != null ? TextEvent.Invoke(this) : text;
            var hasText = !string.IsNullOrEmpty(localText);

            c.PushTranslation(0, press ? 1 : 0);

            if (!hasText && bmp != null)
            {
                c.DrawTextureAtlas(bmp, 
                    (width  - scaledImageSize.Width)  / 2, 
                    (height - scaledImageSize.Height) / 2, imageScale, localFgColor);
            }
            else if (hasText && bmp == null)
            {
                c.DrawText(localText, localFont, 0, 0, localFgColor, TextFlags.MiddleCenter | (ellipsis ? TextFlags.Ellipsis : 0), width, height);
            }
            else if (hasText && bmp != null)
            {
                var localMargin = GetScaledMargin();

                if (bottomText)
                {
                    c.DrawTextureAtlas(bmp, (width - scaledImageSize.Width) / 2, localMargin, imageScale, localFgColor);
                    c.DrawText(localText, localFont, 0, scaledImageSize.Height + localMargin, localFgColor, TextFlags.TopCenter | (ellipsis ? TextFlags.Ellipsis : TextFlags.Clip), width, height - scaledImageSize.Height - localMargin);
                }
                else
                {
                    c.DrawTextureAtlas(bmp, localMargin, (height - scaledImageSize.Height) / 2, imageScale, localFgColor);
                    c.DrawText(localText, localFont, scaledImageSize.Width + localMargin * 2, 0, localFgColor, TextFlags.MiddleLeft | (ellipsis ? TextFlags.Ellipsis : TextFlags.Clip), width - scaledImageSize.Width - localMargin * 2, height);
                }
            }

            c.PopTransform();

            // Debug
            //c.DrawRectangle(ClientRectangle, Color.Pink);
        }
    }
}
