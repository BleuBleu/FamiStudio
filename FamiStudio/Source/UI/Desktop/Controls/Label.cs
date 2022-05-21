using System.Drawing;
using System.Collections.Generic;

using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush = FamiStudio.GLBrush;
using RenderGeometry = FamiStudio.GLGeometry;
using RenderControl = FamiStudio.GLControl;
using RenderGraphics = FamiStudio.GLGraphics;
using RenderCommandList = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class Label2 : RenderControl
    {
        private string text;

        public Label2()
        {
            height = DpiScaling.ScaleForDialog(24);
        }

        public string Text
        {
            get { return text; }
            set { text = value; MarkDirty(); }
        }

        public int MeasureWidth()
        {
            return ThemeResources.FontMedium.MeasureString(text, false);
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList(GLGraphicsBase.CommandListUsage.Dialog);
            c.DrawText(text, ThemeResources.FontMedium, 0, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft, 0, height);
        }
    }
}
