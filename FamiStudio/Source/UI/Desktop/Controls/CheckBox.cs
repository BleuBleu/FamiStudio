using System.Drawing;
using System.Collections.Generic;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;
using System.Windows.Forms;

namespace FamiStudio
{
    public class CheckBox2 : RenderControl
    {
        private string text;
        private bool check;
        private bool hover;
        private int margin = DpiScaling.ScaleForMainWindow(4);
        private RenderBitmapAtlasRef bmpCheckOn;
        private RenderBitmapAtlasRef bmpCheckOff;

        public CheckBox2(bool chk, string txt = null)
        {
            text = txt;
            check = chk;
            height = DpiScaling.ScaleForMainWindow(24);
        }

        public bool Checked
        {
            get { return check; }
            set { check = value; MarkDirty(); }
        }

        protected override void OnRenderInitialized(RenderGraphics g)
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

        protected override void OnMouseDown(MouseEventArgsEx e)
        {
            if (IsPointInCheckBox(e.X, e.Y))
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

        protected override void OnRender(RenderGraphics g)
        {
            var c = parentDialog.CommandList;
            var bmpSize = bmpCheckOn.ElementSize;
            var baseY = (height - bmpSize.Height) / 2;

            c.FillAndDrawRectangle(0, baseY, bmpSize.Width - 1, baseY + bmpSize.Height - 1, hover ? ThemeResources.DarkGreyLineBrush3 : ThemeResources.DarkGreyLineBrush1, ThemeResources.LightGreyFillBrush1);
            c.DrawBitmapAtlas(check ? bmpCheckOn : bmpCheckOff, 0, baseY, 1, 1, Theme.LightGreyFillColor1);

            if (!string.IsNullOrEmpty(text))
                c.DrawText(text, ThemeResources.FontMedium, bmpSize.Width + margin, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft, 0, height);
        }
    }
}
