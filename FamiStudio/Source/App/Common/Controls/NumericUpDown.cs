using System;
using System.Diagnostics;
using System.Globalization;

namespace FamiStudio
{
    public class NumericUpDown : TextBox
    {
        public delegate void ValueChangedDelegate(Control sender, float val);
        public event ValueChangedDelegate ValueChanged;

        private float val;
        private float min;
        private float max = 10.0f;
        private float inc = 1.0f;
        private TextureAtlasRef[] bmp;
        private float captureDuration;
        private int captureButton = -1;
        private int hoverButton = -1;

        protected int textBoxMargin = DpiScaling.ScaleForWindow(2);

        #region Localization
        protected LocalizedString EnterValueContext;

        #endregion

        public NumericUpDown(float value, float minVal, float maxVal, float increment) : base(value, minVal, maxVal, increment)
        {
            val = value;
            min = minVal;
            max = maxVal;
            inc = increment;
            
            //Debug.Assert(val % increment == 0);
            //Debug.Assert(min % increment == 0);
            //Debug.Assert(max % increment == 0);
            height = DpiScaling.ScaleForWindow(Platform.IsMobile ? 16 : 24);
            allowMobileEdit = false;
            supportsDoubleClick = true;
            supportsLongPress = true;
            SetTextBoxValue();
        }

        public float Value
        {
            get 
            {
                //Debug.Assert(val >= min && val <= max && (val % inc) == 0);
                Debug.Assert(val >= min && val <= max);
                return val;
            }
            set 
            {
                var newValue = (float)Math.Round(Utils.RoundDownFloat(value, inc), 2);
                if (SetAndMarkDirty(ref val, Utils.Clamp(newValue, min, max)))
                {
                    SetTextBoxValue();
                    ValueChanged?.Invoke(this, val);
                }
            }
        }

        public float Minimum
        {
            get { return min; }
            set { min = value; val = Utils.Clamp(val, min, max); SetTextBoxValue(); MarkDirty(); }
        }

        public float Maximum
        {
            get { return max; }
            set { max = value; val = Utils.Clamp(val, min, max); SetTextBoxValue(); MarkDirty(); }
        }

        protected void UpdateOuterMargins()
        {
            outerMarginLeft  = GetButtonRect(0).Width + textBoxMargin;
            outerMarginRight = outerMarginLeft;
        }

        protected override void OnAddedToContainer()
        {
            UpdateOuterMargins();

            var g = ParentWindow.Graphics;
            bmp = new[]
            {
                g.GetTextureAtlasRef("UpDownMinus"),
                g.GetTextureAtlasRef("UpDownPlus")
            };

            // "outerMargin" needs to be set before calling this.
            base.OnAddedToContainer();
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateOuterMargins();
            base.OnResize(e);
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
                    Value += captureButton == 0 ? -inc : inc;
                }
                // Then increment every 50ms (in steps of 10 after a while).
                else if (lastDuration > 0.5f && ((int)((lastDuration - 0.5f) * 20) != (int)((captureDuration - 0.5f) * 20)))
                {
                    Value += (captureButton == 0 ? -inc : inc) * (lastDuration >= 1.5f && (Value % (10 * inc)) == 0 ? 10 : 1);
                }
            }
            else if (!HasDialogFocus)
            {
                SetTickEnabled(false);
            }

            base.Tick(delta);
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            var idx = IsPointInButton(e.X, e.Y);
            if (idx >= 0)
            {
                GetValueFromTextBox();
                captureButton = idx;
                captureDuration = 0;
                Value += captureButton == 0 ? -inc : inc;
                CapturePointer();
            }
            else
            {
                base.OnPointerDown(e);
            }
        }

#if FAMISTUDIO_MOBILE
        protected override void OnTouchLongPress(PointerEventArgs e)
        {
            App.ShowContextMenuAsync(new[]
            {
                new ContextMenuOption("Type",      EnterValueContext,         () => { EnterValue(); }),
            });
        }

        protected void EnterValue()
        {
            Platform.EditTextAsync(label, Math.Round(Value).ToString(), (s) =>
            {
                Value = Utils.ParseFloatWithTrailingGarbage(s);

                if (container is Grid grid)
                    grid.UpdateControlValue(this, Value);
            });
        }
#endif

        private void GetValueFromTextBox()
        {
            ClampNumber();
            val = Utils.ParseFloatWithTrailingGarbage(text);
        }

        private void SetTextBoxValue()
        {
            var fmt = inc % 1 == 0 ? "F0" : inc % 0.1f == 0 ? "F1" : "F2";
            text = val.ToString(fmt);
            SelectAll();
            caretIndex = text.Length;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled && (e.Key == Keys.Up || e.Key == Keys.Down))
            {
                GetValueFromTextBox();
                var mult = e.Modifiers.IsShiftDown ? inc * 10 : inc * 1;
                var sign = e.Key == Keys.Up ? 1 : -1;
                Value += mult * sign;
            }
        }

        protected override void OnAcquiredDialogFocus()
        {
            SelectAll();
            caretIndex = text.Length;
            SetTickEnabled(true);
        }

        protected override void OnLostDialogFocus()
        {
            GetValueFromTextBox();
            SetTickEnabled(false);
        }

        protected override void OnMouseDoubleClick(PointerEventArgs e)
        {
            if (IsPointInButton(e.X, e.Y) >= 0)
            {
                // Double clicks get triggered if you click quickly, so
                // treat those as click so that the buttons remain responsive.
                OnPointerDown(e);
            }
            else
            {
                base.OnMouseDoubleClick(e);
            }
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            if (captureButton >= 0)
            {
                captureButton = -1;
                ReleasePointer();
                MarkDirty();
            }
            else if (e.Right)
            {
                App.ShowContextMenuAsync(new[]
                {
                    new ContextMenuOption("MenuCut",   CutName,   () => Cut()),
                    new ContextMenuOption("MenuCopy",  CopyName,  () => Copy()),
                    new ContextMenuOption("MenuPaste", PasteName, () => Paste()),
                });
            }
            else
            {
                base.OnPointerUp(e);
            }
        }

        protected override void OnPointerMove(PointerEventArgs e)
        {
            if (!e.IsTouchEvent)
            {
                SetAndMarkDirty(ref hoverButton, IsPointInButton(e.X, e.Y));
            }

            base.OnPointerMove(e);
        }

        protected override void OnPointerLeave(System.EventArgs e)
        {
            SetAndMarkDirty(ref hoverButton, -1);
            base.OnPointerLeave(e);
        }

        protected override void OnMouseWheel(PointerEventArgs e)
        {
            if (enabled && captureButton < 0 && e.ScrollY != 0 && e.X > GetButtonRect(0).Right && e.X < GetButtonRect(1).Left)
            {
                Value += e.ScrollY > 0 ? inc : -inc;
            }

            e.MarkHandled();
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();
            var color = enabled ? Theme.LightGreyColor1 : Theme.MediumGreyColor1;

            var rects = new[]
            {
                GetButtonRect(0),
                GetButtonRect(1)
            };

            if (Platform.IsMobile)
            {
                c.DrawText(val.ToString(CultureInfo.InvariantCulture), fonts.FontMedium, rects[0].Right, 0, color, TextFlags.MiddleCenter, rects[1].Left - rects[0].Right, height);
            }
            else
            {
                base.OnRender(g);
            }

            for (int i = 0; i < 2; i++)
            {
                var fillBrush = enabled && captureButton == i ? Theme.MediumGreyColor1 :
                                enabled && hoverButton   == i ? Theme.DarkGreyColor6 :
                                                                Theme.DarkGreyColor5;

                c.FillAndDrawRectangle(rects[i], fillBrush, color);

                c.PushTranslation(0, captureButton == i ? 1 : 0);
                c.DrawTextureAtlasCentered(bmp[i], rects[i], 1, color);
                c.PopTransform();
            }
        }
    }
}
