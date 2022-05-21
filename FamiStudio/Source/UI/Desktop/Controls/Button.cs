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
        private int margin = DpiScaling.ScaleForDialog(4);
        private bool bold;
        private bool hovered;
        private bool pressed;

        public Button2(string img, string txt = null)
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

        protected override void OnMouseDown(MouseEventArgsEx e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                pressed = true;
            }
            hovered = true;
            MarkDirty();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                pressed = false;
                Click?.Invoke(this);
            }
            MarkDirty();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            hovered = true;
            MarkDirty();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            pressed = false;
            MarkDirty();
        }

        protected override void OnRender(RenderGraphics g)
        {
            if (bmp == null)
            {
                bmp = g.GetBitmapAtlasRef(imageName);
                Debug.Assert(bmp != null);
            }

            var c = g.CreateCommandList(GLGraphicsBase.CommandListUsage.Dialog);
            var bmpSize = bmp.ElementSize;

            if (hovered || pressed)
            {
                c.FillRectangle(ClientRectangle, pressed ? ThemeResources.MediumGreyFillBrush1 : ThemeResources.DarkGreyFillBrush2);
            }

            if (pressed)
            {
                c.PushTranslation(0, 1);
            }

            // Center image if no text, otherwise left align.
            if (string.IsNullOrEmpty(text))
            {
                c.DrawBitmapAtlas(bmp, (width - bmpSize.Width) / 2, (height - bmpSize.Height) / 2);
            }
            else
            {
                c.DrawBitmapAtlas(bmp, margin, (height - bmpSize.Height) / 2);
                c.DrawText(text, bold ? ThemeResources.FontMediumBold : ThemeResources.FontMedium, bmpSize.Width + margin * 2, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft | RenderTextFlags.Clip, width - bmpSize.Width - margin * 2, height);
            }

            if (pressed)
            {
                c.PopTransform();
            }
        }
    }
}
