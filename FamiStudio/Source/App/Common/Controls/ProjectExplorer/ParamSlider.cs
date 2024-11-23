using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace FamiStudio
{
    public class ParamSlider : ParamControl
    {
        private float bmpScale = Platform.IsMobile ? DpiScaling.Window * 0.25f : 1.0f;
        private int touchSlop = DpiScaling.ScaleForWindow(5);

        private TextureAtlasRef bmpMinus;
        private TextureAtlasRef bmpPlus;

        private Color fillColor = Color.FromArgb(64, Color.Black);

        private int buttonSize;
        private int hoverButtonIndex;
        private bool capture;
        private bool changing;
        private int captureButton;
        private Point capturePos;
        private Point captureWinPos;
        private double captureTime;
        private float exp = 1.0f;

        public ParamSlider(ParamInfo p) : base(p)
        {
            exp = 1.0f / (p.Logarithmic ? 4 : 1);
            height = DpiScaling.ScaleForWindow(16);
        }

        protected override void OnAddedToContainer()
        {
            bmpMinus = ParentWindow.Graphics.GetTextureAtlasRef("ButtonMinus");
            bmpPlus  = ParentWindow.Graphics.GetTextureAtlasRef("ButtonPlus");
            buttonSize = DpiScaling.ScaleCustom(bmpMinus.ElementSize.Width, bmpScale);
            height = buttonSize - (Platform.IsMobile ? DpiScaling.ScaleForWindow(2) : 0);
        }

        // -1 = left, 1 = right, 0 = outside
        private int GetButtonIndex(int x)
        {
            if (x < buttonSize)
                return -1;
            if (x > width - buttonSize)
                return 1;

            return 0;
        }

        private float GetSliderRatio(int x)
        {
            return Utils.Saturate((x - buttonSize) / (float)(width - buttonSize * 2));
        }

        public override bool CanReceiveLongPress => !changing;

        protected override void OnPointerDown(PointerEventArgs e)
        {
            if (IsParamEnabled())
            {
                if (e.Left)
                {
                    var buttonIndex = GetButtonIndex(e.X);
                    Debug.Assert(!capture);
                    captureTime = Platform.TimeSeconds();
                    capture = true;
                    captureButton = buttonIndex;
                    capturePos = e.Position;
                    captureWinPos = ControlToWindow(e.Position);

                    if (buttonIndex == 0)
                    {
                        if (e.IsTouchEvent)
                        {
                            changing = false;
                        }
                        else
                        {
                            changing = true;
                            ChangeValue(e.X);
                            CapturePointer();
                            InvokeValueChangeStart();
                        }
                    }
                    else
                    {
                        changing = true;
                        IncrementValue(buttonIndex, 0.0);
                        SetTickEnabled(true);
                        InvokeValueChangeStart();
                    }
                }
            }
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            if (e.Left && capture)
            {
                SetTickEnabled(false);
                if (changing)
                {
                    InvokeValueChangeEnd();
                }
                capture = false;
                changing = false;
            }
            else if (e.Right && GetButtonIndex(e.X) == 0)
            {
                ShowParamContextMenu();
            }
        }

        protected override void OnPointerMove(PointerEventArgs e)
        {
            // Add a bit of slop on mobile to prevent sliders to mess up vertical scrolling
            if (capture && !changing && e.IsTouchEvent && captureButton == 0)
            {
                // If we start scrolling somehow, stop the capture.
                var winPos = ControlToWindow(e.Position);

                if (Math.Abs(winPos.Y - captureWinPos.Y) > touchSlop)
                {
                    ReleasePointer();
                    capture  = false;
                    changing = false;
                    return;
                }
                if (Math.Abs(e.X - capturePos.X) > touchSlop)
                {
                    changing = true;
                    CapturePointer();
                    InvokeValueChangeStart();
                }
            }

            if (changing)
            {
                if (captureButton == 0)
                {
                    ChangeValue(e.X);
                }

                SetAndMarkDirty(ref hoverButtonIndex, captureButton);
                e.MarkHandled();
            }
            else
            {
                SetAndMarkDirty(ref hoverButtonIndex, enabled ? GetButtonIndex(e.X) : 0);
            }
        }

        protected override void OnPointerLeave(EventArgs e)
        {
            SetAndMarkDirty(ref hoverButtonIndex, 0);
        }

        public override void ShowParamContextMenu()
        {
            App.ShowContextMenuAsync(new[]
            {
                new ContextMenuOption("Type",      EnterValueContext,        () => { EnterParamValue(); }),
                new ContextMenuOption("MenuReset", ResetDefaultValueContext, () => { ResetParamDefaultValue(); })
            });
        }

        private void ChangeValue(int x)
        {
            var ctrl = ModifierKeys.IsControlDown;
            var oldVal = param.GetValue();
            var newVal = oldVal;

            if (ctrl)
            {
                var delta = (x - capturePos.X) / 4;
                if (delta != 0)
                {
                    newVal = Utils.Clamp(oldVal + delta * param.SnapValue, param.GetMinValue(), param.GetMaxValue());
                    capturePos.X = x;
                }
            }
            else
            {
                var ratio = GetSliderRatio(x);
                newVal = (int)Math.Round(Utils.Lerp(param.GetMinValue(), param.GetMaxValue(), MathF.Pow(ratio, 1.0f / exp)));
                capturePos.X = x;
            }

            newVal = param.SnapAndClampValue(newVal);
            param.SetValue(newVal);
            MarkDirty();
        }

        private void IncrementValue(int sign, double captureDuration)
        {
            var oldVal = param.GetValue();
            var incLarge = param.SnapValue * 10;
            var incSmall = param.SnapValue;
            var inc = captureDuration > 1.5 && (oldVal % incLarge) == 0 ? incLarge : incSmall;
            var newVal = param.SnapAndClampValue(oldVal + inc * sign);
            param.SetValue(newVal);
            MarkDirty();
        }

        public override void Tick(float delta)
        {
            Debug.Assert(capture);

            if (capture && captureButton != 0)
            {
                var captureDuration = Platform.TimeSeconds() - captureTime;
                if (captureDuration > 0.35)
                    IncrementValue(captureButton, captureDuration);
            }
        }

        private bool IsParamEnabled()
        {
            return enabled && (param.IsEnabled == null || param.IsEnabled());
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.DefaultCommandList;
            var paramEnabled = IsParamEnabled();
            var sliderWidth = width - buttonSize * 2;
            var buttonOffsetY = Utils.DivideAndRoundUp(height - buttonSize, 2);
            var val = param.GetValue();
            var valPrev = param.SnapAndClampValue(val - 1);
            var valNext = param.SnapAndClampValue(val + 1);
            var min = param.GetMinValue();
            var max = param.GetMaxValue();
            var ratio = (val - min) / (float)(max - min);
            var valWidth = max == min ? 0 : (int)Math.Round(MathF.Pow(ratio, exp) * sliderWidth);
            var opacity = paramEnabled ? 255 : 64;
            var opacityL = paramEnabled && val != valPrev ? (hoverButtonIndex == -1 ? 150 : 255) : 64;
            var opacityR = paramEnabled && val != valNext ? (hoverButtonIndex ==  1 ? 150 : 255) : 64;

            c.DrawTextureAtlas(bmpMinus, 0, buttonOffsetY, bmpScale, Color.Black.Transparent(opacityL));
            c.PushTranslation(buttonSize, 0);
            c.FillRectangle(1, 1, valWidth, height, fillColor);
            c.DrawRectangle(0, 0, sliderWidth, height, Color.Black.Transparent(opacity), 1);
            c.DrawText(param.GetValueString(), Fonts.FontMedium, 0, 0, Color.Black.Transparent(opacity), TextFlags.MiddleCenter, sliderWidth, height);
            c.PopTransform();
            c.DrawTextureAtlas(bmpPlus, buttonSize + sliderWidth, buttonOffsetY, bmpScale, Color.Black.Transparent(opacityR));
        }
    }
}

