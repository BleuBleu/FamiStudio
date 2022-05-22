using System.Drawing;
using System.Collections.Generic;

using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush = FamiStudio.GLBrush;
using RenderGeometry = FamiStudio.GLGeometry;
using RenderControl = FamiStudio.GLControl;
using RenderGraphics = FamiStudio.GLGraphics;
using RenderCommandList = FamiStudio.GLCommandList;
using System.Globalization;

namespace FamiStudio
{
    public class NumericUpDown2 : RenderControl
    {
        private int value;
        private int min;
        private int max = 10;

        public NumericUpDown2(int val, int minVal, int maxVal)
        {
            value = val;
            min = minVal;
            max = maxVal;
            height = DpiScaling.ScaleForDialog(24);
        }

        //public string Text
        //{
        //    get { return text; }
        //    set { text = value; MarkDirty(); }
        //}

        private Rectangle GetButtonRect(bool left)
        {
            return left ? new Rectangle(0, 0, width / 4, height) :
                          new Rectangle(width * 3 / 4, 0, width / 4, height);
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList(GLGraphicsBase.CommandListUsage.Dialog);

            var rc1 = GetButtonRect(true);
            var rc2 = GetButtonRect(false);

            c.FillAndDrawRectangle(rc1, ThemeResources.DarkGreyFillBrush2, ThemeResources.LightGreyFillBrush1);
            c.FillAndDrawRectangle(rc2, ThemeResources.DarkGreyFillBrush2, ThemeResources.LightGreyFillBrush1);

            // -/+ icons.

            c.DrawText(value.ToString(CultureInfo.InvariantCulture), ThemeResources.FontMedium, rc1.Right, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleCenter, rc2.Left - rc1.Right, height);
        }
    }
}
