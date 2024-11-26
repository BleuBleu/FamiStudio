using System;

namespace FamiStudio
{
    public class Slider : Control
    {
        public delegate void ValueChangedDelegate(Control sender, double val);
        public event ValueChangedDelegate ValueChanged;

        private int touchSlop = DpiScaling.ScaleForWindow(5);

        private double min;
        private double max;
        private double val;
        private int captureX;
        private Func<double, string> format;
        private bool changing;
        private bool dragging;

        public Slider(double value, double minValue, double maxValue, Func<double, string> fmt = null)
        {
            min = minValue;
            max = maxValue;
            val = value;
            format = fmt == null ? (o) => o.ToString() : fmt;
            height = DpiScaling.ScaleForWindow(Platform.IsMobile ? 14 : 24);
            supportsLongPress = true;
        }

        public double Min { get => min; set => SetAndMarkDirty(ref min, value); }
        public double Max { get => max; set => SetAndMarkDirty(ref max, value); }
        public Func<double, string> Format { get => format; set { format = value; MarkDirty(); } }

        public double Value
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
                    changing = true;
                    Value = Utils.Lerp(min, max, Utils.Saturate(e.X / (float)(width)));
                    CapturePointer();
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
                ReleasePointer();
                e.MarkHandled();
            }
        }

        protected override void OnPointerMove(PointerEventArgs e)
        {
            // Add a bit of slop on mobile to prevent sliders to mess up vertical scrolling
            if (dragging && !changing && e.IsTouchEvent && Math.Abs(e.X - captureX) > touchSlop)
            {
                changing = true;
                CapturePointer();
            }

            if (changing)
            {
                Value = Utils.Lerp(min, max, Utils.Saturate(e.X / (float)(width)));
                e.MarkHandled();
            }
        }

        protected override void OnTouchClick(PointerEventArgs e)
        {
            Value = Utils.Lerp(min, max, Utils.Saturate(e.X / (float)(width)));
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
