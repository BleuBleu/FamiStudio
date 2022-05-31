using System.Drawing;
using System.Globalization;

namespace FamiStudio
{
    public class NumericUpDown : Control
    {
        public delegate void ValueChangedDelegate(Control sender, int val);
        public event ValueChangedDelegate ValueChanged;

        private int val;
        private int min;
        private int max = 10;
        private BitmapAtlasRef[] bmp;

        private float captureDuration;
        private int   captureButton = -1;

        private int hoverButton = -1;

        public NumericUpDown(int value, int minVal, int maxVal)
        {
            val = value;
            min = minVal;
            max = maxVal;
            height = DpiScaling.ScaleForMainWindow(24);
        }

        public int Value
        {
            get { return val; }
            set { if (SetAndMarkDirty(ref val, Utils.Clamp(value, min, max))) ValueChanged?.Invoke(this, val); }
        }

        public int Minimum
        {
            get { return min; }
            set { min = value; val = Utils.Clamp(value, min, max); MarkDirty(); }
        }

        public int Maximum
        {
            get { return max; }
            set { max = value; val = Utils.Clamp(value, min, max); MarkDirty(); }
        }

        protected override void OnRenderInitialized(Graphics g)
        {
            bmp = new[]
            {
                g.GetBitmapAtlasRef("UpDownMinus"),
                g.GetBitmapAtlasRef("UpDownPlus")
            };
        }

        private Rectangle GetButtonRect(int idx)
        {
            return idx == 0 ? new Rectangle(0, 0, width / 4, height) :
                              new Rectangle(width * 3 / 4, 0, width / 4, height);
        }

        private int IsPointInButton(int x, int y)
        {
            if (enabled)
            {
                for (int i = 0; i < 2; i++)
                {
                    var rect = GetButtonRect(i);
                    if (rect.Contains(x, y))
                        return i;
                }
            }

            return -1;
        }

        public override void Tick(float delta)
        {
            if (captureButton >= 0)
            {
                var lastDuration = captureDuration;
                captureDuration += delta;

                // Transition to auto increment after 250ms.
                if (lastDuration < 0.5f && captureDuration >= 0.5f)
                {
                    Value += captureButton == 0 ? -1 : 1;
                }
                // Then increment every 50ms (in steps of 10 after a while).
                else if (lastDuration >= 0.25f && ((int)((lastDuration - 0.5f) * 20) != (int)((captureDuration - 0.5f) * 20)))
                {
                    Value += (captureButton == 0 ? -1 : 1) * (lastDuration >= 1.5f && (Value % 10) == 0 ? 10 : 1);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            var idx = IsPointInButton(e.X, e.Y);
            if (idx >= 0)
            {
                captureButton = idx;
                captureDuration = 0;
                Value += captureButton == 0 ? -1 : 1;
                Capture = true;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (captureButton >= 0)
            {
                captureButton = -1;
                Capture = false;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            SetAndMarkDirty(ref hoverButton, IsPointInButton(e.X, e.Y));
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            SetAndMarkDirty(ref hoverButton, -1);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (enabled && captureButton < 0 && e.ScrollY != 0 && e.X > GetButtonRect(0).Right && e.X < GetButtonRect(1).Left)
            {
                Value += e.ScrollY > 0 ? 1 : -1;
            }
        }

        protected override void OnRender(Graphics g)
        {
            var c = parentDialog.CommandList;
            var brush = enabled ? ThemeResources.LightGreyFillBrush1 : ThemeResources.MediumGreyFillBrush1;

            var rects = new []
            {
                GetButtonRect(0),
                GetButtonRect(1)
            };

            for (int i = 0; i < 2; i++)
            {
                var fillBrush = enabled && captureButton == i ? ThemeResources.MediumGreyFillBrush1 :
                                enabled && hoverButton   == i ? ThemeResources.DarkGreyFillBrush3 :
                                                                ThemeResources.DarkGreyFillBrush2;

                c.FillAndDrawRectangle(rects[i], fillBrush, brush);

                c.PushTranslation(0, captureButton == i ? 1 : 0);
                c.DrawBitmapAtlasCentered(bmp[i], rects[i], 1, 1, brush.Color0);
                c.PopTransform();
            }

            c.DrawText(val.ToString(CultureInfo.InvariantCulture), ThemeResources.FontMedium, rects[0].Right, 0, brush, TextFlags.MiddleCenter, rects[1].Left - rects[0].Right, height);
        }
    }
}
