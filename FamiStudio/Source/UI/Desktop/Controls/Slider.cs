using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Globalization;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class Slider2 : RenderControl
    {
        private double min;
        private double max;
        private double val;
        private double increment;
        private string format;
        private bool label = true;
        private bool dragging;
        private bool hover;
        private int dragOffsetX;
        private RenderBitmapAtlasRef bmpThumb;

        private int thumbSize;
        private int labelMargin;
        private int labelSize;

        public Slider2(double value, double minValue, double maxValue, double inc, bool showLabel, string fmt = "{0}")
        {
            min = minValue;
            max = maxValue;
            val = value;
            increment = inc;
            label = showLabel;
            format = fmt;
            height = DpiScaling.ScaleForMainWindow(24);
            labelSize   = label ? DpiScaling.ScaleForMainWindow(50) : 0;
            labelMargin = label ? DpiScaling.ScaleForMainWindow(4) : 0;
        }

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            bmpThumb = g.GetBitmapAtlasRef("SliderThumb");
            thumbSize = bmpThumb.ElementSize.Width;
        }

        public double Value
        {
            get { return val; }
            set { SetAndMarkDirty(ref val, Utils.Clamp(value, min, max)); }
        }

        private Rectangle GetThumbRectangle()
        {
            var x = (int)Math.Round((val - min) / (max - min) * (width - thumbSize - labelSize - labelMargin));
            var y = (height - thumbSize) / 2;

            return new Rectangle(x, y, thumbSize, thumbSize);
        }

        protected override void OnMouseDown(MouseEventArgsEx e)
        {
            var thumbRect = GetThumbRectangle();
            if (thumbRect.Contains(e.X, e.Y))
            {
                Capture = true;
                dragging = true;
                dragOffsetX = e.X - thumbRect.X;
            }
            else if (e.X > thumbRect.Right)
            {
                Value += increment;
            }
            else if (e.X < thumbRect.Left)
            {
                Value -= increment;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (dragging)
            {
                dragging = false;
                Capture = false;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                var x = e.X - dragOffsetX;
                var ratio = x / (float)(width - thumbSize - labelSize - labelMargin);
                Value = Utils.Lerp(min, max, ratio);
            }
            else
            {
                SetAndMarkDirty(ref hover, GetThumbRectangle().Contains(e.X, e.Y));

            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            SetAndMarkDirty(ref hover, false);
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList(GLGraphicsBase.CommandListUsage.Dialog);
            var thumbRect = GetThumbRectangle();

            c.DrawLine(thumbSize / 2, height / 2, width - thumbSize / 2 - labelSize - labelMargin, height / 2, ThemeResources.DarkGreyLineBrush1, ScaleForMainWindow(3));
            c.DrawBitmapAtlas(bmpThumb, thumbRect.Left, thumbRect.Top, 1, 1, hover || dragging ? Theme.LightGreyFillColor2 : Theme.LightGreyFillColor1);

            if (label)
            {
                var str = string.Format(CultureInfo.InvariantCulture, format, val);
                c.DrawText(str, ThemeResources.FontMedium, width - labelSize, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleRight, labelSize, height);
            }
        }
    }
}
