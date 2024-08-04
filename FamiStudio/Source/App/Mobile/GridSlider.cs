using System;
using System.Globalization;
using System.Diagnostics;

namespace FamiStudio
{
    // MATTT : Add the same logic we had to prevent changing the slider while scrolling.
    // We only started the capture/dragging when the movement in X exceeded a certain 
    // threshold. Otherwise we would let the input go to the parent container.
    public class GridSlider : Control
    {
        public delegate void ValueChangedDelegate(Control sender, double val);
        public event ValueChangedDelegate ValueChanged;

        private int touchSlop = DpiScaling.ScaleForWindow(4);

        private int min;
        private int max;
        private int val;
        private int captureX;
        private Func<object, string> format;
        private bool changing;
        private bool dragging;

        public GridSlider(int value, int minValue, int maxValue, Func<object, string> fmt = null)
        {
            min = minValue;
            max = maxValue;
            val = value;
            format = fmt == null ? (o) => o.ToString() : fmt;
            height = DpiScaling.ScaleForWindow(14);
            supportsLongPress = true;
        }

        public int Min { get => min; set => SetAndMarkDirty(ref min, value); }
        public int Max { get => max; set => SetAndMarkDirty(ref max, value); }
        public Func<object, string> Format { get => format; set { format = value; MarkDirty(); } }

        public int Value
        {
            get { return val; }
            set { if (SetAndMarkDirty(ref val, Utils.Clamp(value, min, max))) ValueChanged?.Invoke(this, val); }
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            if (enabled)
            {
                if (e.IsTouchEvent)
                {
                    changing = false;
                }
                else
                {
                    Capture = true;
                    changing = true;
                    e.MarkHandled();
                }

                dragging = true;
                captureX = e.X;
            }
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            if (dragging)
            {
                dragging = false;
                changing = false;
                Capture = false;
                e.MarkHandled();
            }
        }

        protected override void OnPointerMove(PointerEventArgs e)
        {
            // Add a bit of slop on mobile to prevent sliders to mess up vertical scrolling
            if (dragging && !changing && e.IsTouchEvent && Math.Abs(e.X - captureX) > touchSlop)
            {
                changing = true;
                Capture = true;
            }

            if (changing)
            {
                Value = (int)Math.Round(Utils.Lerp(min, max, Utils.Saturate(e.X / (float)(width))));
                e.MarkHandled();
            }
        }

        protected override void OnTouchClick(PointerEventArgs e)
        {
            Value = (int)Math.Round(Utils.Lerp(min, max, Utils.Saturate(e.X / (float)(width))));
            e.MarkHandled();
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();

            var foreColor = enabled ? Theme.LightGreyColor1 : Theme.MediumGreyColor1;

            c.FillRectangle(0, 0, width, height, Theme.DarkGreyColor1);
            
            if (enabled)
            {
                c.FillRectangle(0, 0, (int)Math.Round((val - min) / (double)(max - min) * width), height, Theme.DarkGreyColor6);
            }

            c.DrawRectangle(0, 0, width, height, foreColor);
            c.DrawText(format(val), fonts.FontMedium, 0, 0, foreColor, TextFlags.MiddleCenter, width, height);
        }
    }
}
