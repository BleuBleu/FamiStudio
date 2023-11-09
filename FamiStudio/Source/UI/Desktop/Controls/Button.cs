using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class Button : Control
    {
        public delegate void ClickDelegate(Control sender);
        public event ClickDelegate Click;

        private string text;
        private string imageName;
        private TextureAtlasRef bmp;
        private int margin = DpiScaling.ScaleForWindow(4);
        private bool bold;
        private bool ellipsis;
        private bool border;
        private bool hover;
        private bool press;

        public Button(string img, string txt) 
        {
            Image = img;
            text  = txt;
        }

        public string Text
        {
            get { return text; }
            set { text = value; MarkDirty(); }
        }

        public string Image
        {
            get { return imageName; }
            set { imageName = value; UpdateAtlasBitmap(); MarkDirty(); }
        }

        public bool BoldFont
        {
            get { return bold; }
            set { bold = value; MarkDirty(); }
        }

        public bool Border
        {
            get { return border; }
            set { border = value; MarkDirty(); }
        }

        public bool Ellipsis
        {
            get { return ellipsis; }
            set { ellipsis = value; MarkDirty(); }
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

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();
            var bmpSize = bmp != null ? bmp.ElementSize : Size.Empty;
            var color = enabled ? Theme.LightGreyColor1 : Theme.MediumGreyColor1;

            if (enabled && (border || press || hover))
            {
                var fillBrush = press ? Theme.MediumGreyColor1 :
                                hover ? Theme.DarkGreyColor6 :
                                        Theme.DarkGreyColor5;

                c.FillRectangle(ClientRectangle, fillBrush);
            }

            if (border)
            {
                c.DrawRectangle(ClientRectangle, Theme.BlackColor);
            }

            var hasText = !string.IsNullOrEmpty(text);

            c.PushTranslation(0, press ? 1 : 0);

            if (!hasText && bmp != null)
            {
                c.DrawTextureAtlas(bmp, (width - bmpSize.Width) / 2, (height - bmpSize.Height) / 2, 1, 1, color);
            }
            else if (hasText && bmp == null)
            {
                c.DrawText(text, bold ? Fonts.FontMediumBold : Fonts.FontMedium, 0, 0, color, TextFlags.MiddleCenter | (ellipsis ? TextFlags.Ellipsis : 0), width, height);
            }
            else if (hasText && bmp != null)
            {
                c.DrawTextureAtlas(bmp, margin, (height - bmpSize.Height) / 2, 1, 1, color);
                c.DrawText(text, bold ? Fonts.FontMediumBold : Fonts.FontMedium, bmpSize.Width + margin * 2, 0, color, TextFlags.MiddleLeft | TextFlags.Clip, width - bmpSize.Width - margin * 2, height);
            }

            c.PopTransform();
        }
    }
}
