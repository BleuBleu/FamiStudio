using System;
using System.Drawing;
using System.Diagnostics;

namespace FamiStudio
{
    public class Button : Control
    {
        public delegate void ClickDelegate(Control sender);
        public event ClickDelegate Click;

        private string text;
        private string imageName;
        private BitmapAtlasRef bmp;
        private int margin = DpiScaling.ScaleForWindow(4);
        private bool bold;
        private bool border;
        private bool hover;
        private bool press;

        public Button(string img, string txt)
        {
            imageName = img;
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
            set { imageName = value; bmp = null; MarkDirty(); }
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

        public void AutosizeWidth(int imageSize)
        {
            // Only bothered to support this one use case.
            Debug.Assert(!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(imageName));
            width = margin * 3 + ScaleForWindow(imageSize) + ThemeResources.FontMedium.MeasureString(text, false);
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

        protected override void OnRender(Graphics g)
        {
            if (bmp == null && !string.IsNullOrEmpty(imageName))
            {
                bmp = g.GetBitmapAtlasRef(imageName);
                Debug.Assert(bmp != null);
            }

            var c = parentDialog.CommandList;
            var bmpSize = bmp != null ? bmp.ElementSize : Size.Empty;
            var brush = enabled ? ThemeResources.LightGreyFillBrush1 : ThemeResources.MediumGreyFillBrush1;

            if (enabled && (border || press || hover))
            {
                var fillBrush = press ? ThemeResources.MediumGreyFillBrush1 :
                                hover ? ThemeResources.DarkGreyFillBrush3 :
                                        ThemeResources.DarkGreyFillBrush2;

                c.FillRectangle(ClientRectangle, fillBrush);
            }

            if (border)
            {
                c.DrawRectangle(ClientRectangle, ThemeResources.BlackBrush);
            }

            var hasText = !string.IsNullOrEmpty(text);

            c.PushTranslation(0, press ? 1 : 0);

            if (!hasText && bmp != null)
            {
                c.DrawBitmapAtlas(bmp, (width - bmpSize.Width) / 2, (height - bmpSize.Height) / 2, 1, 1, brush.Color0);
            }
            else if (hasText && bmp == null)
            {
                c.DrawText(text, bold ? ThemeResources.FontMediumBold : ThemeResources.FontMedium, 0, 0, brush, TextFlags.MiddleCenter, width, height);
            }
            else if (hasText && bmp != null)
            {
                c.DrawBitmapAtlas(bmp, margin, (height - bmpSize.Height) / 2, 1, 1, brush.Color0);
                c.DrawText(text, bold ? ThemeResources.FontMediumBold : ThemeResources.FontMedium, bmpSize.Width + margin * 2, 0, brush, TextFlags.MiddleLeft | TextFlags.Clip, width - bmpSize.Width - margin * 2, height);
            }

            c.PopTransform();
        }
    }
}
