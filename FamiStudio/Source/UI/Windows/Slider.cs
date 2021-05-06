using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace FamiStudio
{
    public partial class Slider : UserControl
    {
        private int thumbWidth = 10;
        private int labelWidth = 80;

        private double minValue    = 0;
        private double maxValue    = 10;
        private double value       = 5;
        private double initValue   = 5;
        private double increment   = 10;
        private int    numDecimals = 0;

        private Pen   darkGrayPen    = new Pen(ThemeBase.MediumGreyFillColor1, 2);
        private Brush lightGrayBrush = new SolidBrush(ThemeBase.LightGreyFillColor2);
        private StringFormat format;

        public delegate string FormatValueDelegate(Slider slider, double value);
        public event FormatValueDelegate FormatValueEvent;
        public delegate void ValueChangedDelegate(Slider slider, double value);
        public event ValueChangedDelegate ValueChangedEvent;

        public double Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = Math.Round(Utils.Clamp(value, minValue, maxValue), numDecimals);
                Invalidate();
            }
        }

        public Slider(double val, double min, double max, double inc, int decimals)
        {
            InitializeComponent();
            DoubleBuffered = true;

            minValue = min;
            maxValue = max;
            numDecimals = decimals;
            value = Math.Round(Utils.Clamp(val, min, max), numDecimals);
            initValue = value;
            increment = inc;

            format = new StringFormat();
            format.LineAlignment = StringAlignment.Center;
            format.Alignment = StringAlignment.Center;
        }

        private Rectangle GetThumbRect()
        {
            var ratio = (value - minValue) / (float)(maxValue - minValue);
            var x = (int)Math.Round((Width - labelWidth - thumbWidth) * ratio) + thumbWidth / 2;

            return new Rectangle(x - thumbWidth / 2, 0, thumbWidth, Height);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                var rect = GetThumbRect();

                if (rect.Contains(e.X, e.Y))
                {
                    Capture = true;
                }
                else if (e.X > rect.Right)
                {
                    value = Utils.Clamp(value + increment, minValue, maxValue);
                }
                else if (e.X < rect.Left)
                {
                    value = Utils.Clamp(value - increment, minValue, maxValue);
                }

                value = Math.Round(value, numDecimals);

                ValueChangedEvent?.Invoke(this, value);
            }
            else if (e.Button == MouseButtons.Right)
            {
                //value = initValue;
            }

            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            Capture = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (Capture)
            {
                var ratio = Utils.Clamp((e.X - thumbWidth / 2) / (double)(Width - labelWidth - thumbWidth), 0.0, 1.0);
                value = Utils.Lerp(minValue, maxValue, ratio);
                value = Math.Round(value, numDecimals);
                ValueChangedEvent?.Invoke(this, value);
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.DrawLine(darkGrayPen, thumbWidth / 2, Height / 2, Width - labelWidth - thumbWidth / 2, Height / 2);
            e.Graphics.FillRectangle(lightGrayBrush, GetThumbRect());

            string str = null;

            if (FormatValueEvent != null)
                str = FormatValueEvent(this, value);

            if (str == null)
                str = value.ToString($"N{numDecimals}");

            e.Graphics.DrawString(str, Font, lightGrayBrush, new RectangleF(Width - labelWidth, 0, labelWidth, Height), format);
        }
    }
}
