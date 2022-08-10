using System.Diagnostics;
using System.Globalization;

namespace FamiStudio
{
    public class NumericUpDown : TextBox
    {
        public delegate void ValueChangedDelegate(Control sender, int val);
        public event ValueChangedDelegate ValueChanged;

        private int val;
        private int min;
        private int max = 10;
        private BitmapAtlasRef[] bmp;
        private float captureDuration;
        private int   captureButton = -1;
        private int   hoverButton = -1;

        protected int textBoxMargin = DpiScaling.ScaleForWindow(2);

        public NumericUpDown(Dialog dlg, int value, int minVal, int maxVal) : base(dlg, value, minVal, maxVal)
        {
            val = value;
            min = minVal;
            max = maxVal;
            height = DpiScaling.ScaleForWindow(24);
            SetTextBoxValue();
        }

        public int Value
        {
            get 
            {
                Debug.Assert(val >= min && val <= max);
                return val; 
            }
            set 
            {
                if (SetAndMarkDirty(ref val, Utils.Clamp(value, min, max)))
                {
                    SetTextBoxValue();
                    ValueChanged?.Invoke(this, val);
                }
            }
        }

        public int Minimum
        {
            get { return min; }
            set { min = value; val = Utils.Clamp(val, min, max); SetTextBoxValue(); MarkDirty(); }
        }

        public int Maximum
        {
            get { return max; }
            set { max = value; val = Utils.Clamp(val, min, max); SetTextBoxValue(); MarkDirty(); }
        }

        protected override void OnAddedToDialog()
        {
            outerMargin = GetButtonRect(0).Width + textBoxMargin;
            base.OnAddedToDialog();
        }

        protected override void OnRenderInitialized(Graphics g)
        {
            bmp = new[]
            {
                g.GetBitmapAtlasRef("UpDownMinus"),
                g.GetBitmapAtlasRef("UpDownPlus")
            };

            base.OnRenderInitialized(g);
        }

        private Rectangle GetButtonRect(int idx)
        {
            return idx == 0 ? new Rectangle(0, 0, width / 4, height - 1) :
                              new Rectangle(width - width / 4 - 1, 0, width / 4, height - 1);
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
                else if (lastDuration > 0.5f && ((int)((lastDuration - 0.5f) * 20) != (int)((captureDuration - 0.5f) * 20)))
                {
                    Value += (captureButton == 0 ? -1 : 1) * (lastDuration >= 1.5f && (Value % 10) == 0 ? 10 : 1);
                }
            }

            base.Tick(delta);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            var idx = IsPointInButton(e.X, e.Y);
            if (idx >= 0)
            {
                GetValueFromTextBox();
                captureButton = idx;
                captureDuration = 0;
                Value += captureButton == 0 ? -1 : 1;
                Capture = true;
            }
            else
            {
                base.OnMouseDown(e);
            }
        }

        private void GetValueFromTextBox()
        {
            ClampNumber();
            val = Utils.ParseIntWithTrailingGarbage(text);
        }

        private void SetTextBoxValue()
        {
            text = val.ToString(CultureInfo.InvariantCulture);
            SelectAll();
            caretIndex = text.Length;
        }

        protected override void OnAcquiredDialogFocus()
        {
            SelectAll();
            caretIndex = text.Length;
        }

        protected override void OnLostDialogFocus()
        {
            GetValueFromTextBox();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (IsPointInButton(e.X, e.Y) >= 0)
            {
                // Double clicks get triggered if you click quickly, so
                // treat those as click so that the buttons remain responsive.
                OnMouseDown(e);
            }
            else
            {
                base.OnMouseDoubleClick(e);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (captureButton >= 0)
            {
                captureButton = -1;
                Capture = false;
            }
            else
            {
                base.OnMouseUp(e);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            SetAndMarkDirty(ref hoverButton, IsPointInButton(e.X, e.Y));
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            SetAndMarkDirty(ref hoverButton, -1);
            base.OnMouseLeave(e);
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
            base.OnRender(g);

            var c = parentDialog.CommandList;
            var color = enabled ? Theme.LightGreyColor1 : Theme.MediumGreyColor1;

            var rects = new []
            {
                GetButtonRect(0),
                GetButtonRect(1)
            };

            for (int i = 0; i < 2; i++)
            {
                var fillBrush = enabled && captureButton == i ? Theme.MediumGreyColor1 :
                                enabled && hoverButton   == i ? Theme.DarkGreyColor6 :
                                                                Theme.DarkGreyColor5;

                c.FillAndDrawRectangle(rects[i], fillBrush, color);

                c.PushTranslation(0, captureButton == i ? 1 : 0);
                c.DrawBitmapAtlasCentered(bmp[i], rects[i], 1, 1, color);
                c.PopTransform();
            }
        }
    }
}
