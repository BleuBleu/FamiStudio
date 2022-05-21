using System.Drawing;
using System.Collections.Generic;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class CheckBox2 : RenderControl
    {
        private string text;
        private bool check;
        private int margin = DpiScaling.ScaleForDialog(4);
        private RenderBitmapAtlasRef bmpCheckOn;
        private RenderBitmapAtlasRef bmpCheckOff;

        public CheckBox2(bool chk, string txt = null)
        {
            text = txt;
            check = chk;
            height = DpiScaling.ScaleForDialog(24);
        }

        public bool Checked
        {
            get { return check; }
            set { check = value; MarkDirty(); }
        }

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            // MATTT : Different icon?
            bmpCheckOn  = g.GetBitmapAtlasRef("CheckBoxYes");
            bmpCheckOff = g.GetBitmapAtlasRef("CheckBoxNo");
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList(GLGraphicsBase.CommandListUsage.Dialog);
            var bmpSize = bmpCheckOn.ElementSize;

            var baseY = (height - bmpSize.Height) / 2;

            c.FillAndDrawRectangle(0, baseY, bmpSize.Width - 1, baseY + bmpSize.Height - 1, ThemeResources.WhiteBrush, ThemeResources.BlackBrush);
            c.DrawBitmapAtlas(check ? bmpCheckOn : bmpCheckOff, 0, baseY, 1, 1, Color.Black);

            if (!string.IsNullOrEmpty(text))
                c.DrawText(text, ThemeResources.FontMedium, bmpSize.Width + margin, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft, 0, height);
        }
    }
}
