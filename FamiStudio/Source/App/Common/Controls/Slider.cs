using System;
using System.Globalization;
using System.Diagnostics;

namespace FamiStudio
{
    public class Slider : Control
    {
        public delegate void ValueChangedDelegate(Control sender, double val);
        public event ValueChangedDelegate ValueChanged;

        private double min;
        private double max;
        private double val;
        private double increment;
        private string format;
        private bool label = true;
        private bool dragging;
        private bool hover;
        private int dragOffsetX;
        private TextureAtlasRef bmpThumb;

        private int thumbSize;
        private int labelMargin;
        private int labelSize;

        public Slider(double value, double minValue, double maxValue, double inc, bool showLabel, string fmt = "{0}")
        {
            min = minValue;
            max = maxValue;
            val = value;
            increment = inc;
            label = showLabel;
            format = fmt;
            height = DpiScaling.ScaleForWindow(24);
            labelSize   = label ? DpiScaling.ScaleForWindow(50) : 0;
            labelMargin = label ? DpiScaling.ScaleForWindow(4) : 0;
        }

        protected override void OnAddedToContainer()
        {
            var g = ParentWindow.Graphics;
            bmpThumb = g.GetTextureAtlasRef("SliderThumb");
            thumbSize = bmpThumb.ElementSize.Width;
        }

        public double Value
        {
            get { return val; }
            set { if (SetAndMarkDirty(ref val, Utils.Clamp(value, min, max))) ValueChanged?.Invoke(this, val); }
        }

        private Rectangle GetThumbRectangle()
        {
            var x = (int)Math.Round((val - min) / (max - min) * (width - thumbSize - labelSize - labelMargin));
            var y = (height - thumbSize) / 2;

            return new Rectangle(x, y, thumbSize, thumbSize);
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            if (enabled)
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
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            if (dragging)
            {
                dragging = false;
                Capture = false;
            }
        }

        protected override void OnPointerMove(PointerEventArgs e)
        {
            if (dragging)
            {
                var x = e.X - dragOffsetX;
                var ratio = x / (float)(width - thumbSize - labelSize - labelMargin);
                Value = Utils.Lerp(min, max, ratio);
            }
            else if (enabled)
            {
                SetAndMarkDirty(ref hover, GetThumbRectangle().Contains(e.X, e.Y));
            }
        }

        protected override void OnPointerLeave(EventArgs e)
        {
            SetAndMarkDirty(ref hover, false);
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();
            var thumbRect = GetThumbRectangle();

            c.DrawLine(thumbSize / 2, height / 2, width - thumbSize / 2 - labelSize - labelMargin, height / 2, Theme.DarkGreyColor1, DpiScaling.ScaleForWindow(3));
            c.DrawTextureAtlas(bmpThumb, thumbRect.Left, thumbRect.Top, 1, hover || dragging ? Theme.LightGreyColor2 : enabled ? Theme.LightGreyColor1 : Theme.MediumGreyColor1);

            if (label)
            {
                var str = string.Format(CultureInfo.InvariantCulture, format, val);
                c.DrawText(str, Fonts.FontMedium, width - labelSize, 0, enabled ? Theme.LightGreyColor1 : Theme.MediumGreyColor1, TextFlags.MiddleRight, labelSize, height);
            }
        }
    }
}
