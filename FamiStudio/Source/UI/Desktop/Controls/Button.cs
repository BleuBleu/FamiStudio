using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System;
using System.Diagnostics;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class Button2 : RenderControl
    {
        public delegate void ClickDelegate(RenderControl sender);
        public event ClickDelegate Click;

        private string text;
        private string imageName;
        private RenderBitmapAtlasRef bmp;
        private int margin = DpiScaling.ScaleForMainWindow(4);
        private bool bold;
        private bool border;
        private bool hover;
        private bool press;

        public Button2(string img, string txt)
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

        protected override void OnMouseDown(MouseEventArgsEx e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                press = true;
            }
            hover = true;
            MarkDirty();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
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

        protected override void OnRender(RenderGraphics g)
        {
            if (bmp == null && !string.IsNullOrEmpty(imageName))
            {
                bmp = g.GetBitmapAtlasRef(imageName);
                Debug.Assert(bmp != null);
            }

            var c = g.CreateCommandList(GLGraphicsBase.CommandListUsage.Dialog);
            var bmpSize = bmp != null ? bmp.ElementSize : Size.Empty;

            if (border || press || hover)
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
                c.DrawBitmapAtlas(bmp, (width - bmpSize.Width) / 2, (height - bmpSize.Height) / 2);
            }
            else if (hasText && bmp == null)
            {
                c.DrawText(text, bold ? ThemeResources.FontMediumBold : ThemeResources.FontMedium, 0, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleCenter, width, height);
            }
            else if (hasText && bmp != null)
            {
                c.DrawBitmapAtlas(bmp, margin, (height - bmpSize.Height) / 2);
                c.DrawText(text, bold ? ThemeResources.FontMediumBold : ThemeResources.FontMedium, bmpSize.Width + margin * 2, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft | RenderTextFlags.Clip, width - bmpSize.Width - margin * 2, height);
            }

            c.PopTransform();
        }
    }
}
