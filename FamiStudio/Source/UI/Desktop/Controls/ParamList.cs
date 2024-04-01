using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.Marshalling;

namespace FamiStudio
{
    public class ParamList : Control
    {
        // MATTT : What was that again?
        private float bmpScale = Platform.IsMobile ? DpiScaling.Window * 0.25f : 1.0f;

        private TextureAtlasRef bmpLeft;
        private TextureAtlasRef bmpRight;

        private int buttonSizeX;
        private int buttonSizeY;
        private int hoverButtonIndex;
        private bool capture;
        private int captureButton;
        private double captureTime;
        private ParamInfo param;

        public event ControlDelegate ValueChangeStart;
        public event ControlDelegate ValueChangeEnd;

        public ParamList(ParamInfo p)
        {
            param = p;
            height = DpiScaling.ScaleForWindow(16);
        }

        protected override void OnAddedToContainer()
        {
            bmpLeft = ParentWindow.Graphics.GetTextureAtlasRef("ButtonLeft");
            bmpRight = ParentWindow.Graphics.GetTextureAtlasRef("ButtonRight");
            buttonSizeX = DpiScaling.ScaleCustom(bmpLeft.ElementSize.Width, bmpScale);
            buttonSizeY = DpiScaling.ScaleCustom(bmpLeft.ElementSize.Height, bmpScale);
            height = DpiScaling.ScaleForWindow(buttonSizeY);
        }

        // -1 = left, 1 = right, 0 = outside
        private int GetButtonIndex(int x) 
        {
            if (x < buttonSizeX)
                return -1;
            if (x > width - buttonSizeX)
                return 1;
            
            return 0;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Left && IsParamEnabled())
            {
                var buttonIndex = GetButtonIndex(e.X);
                if (buttonIndex != 0)
                {
                    Debug.Assert(!capture);
                    ValueChangeStart?.Invoke(this);
                    captureTime = Platform.TimeSeconds();
                    capture = true;
                    captureButton = buttonIndex;
                    ChangeValue(buttonIndex);
                    SetTickEnabled(true);
                    Capture = true;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Left && capture)
            {
                capture = false;
                SetTickEnabled(false);
                ValueChangeEnd?.Invoke(this);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            SetAndMarkDirty(ref hoverButtonIndex, enabled ? GetButtonIndex(e.X) : 0);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            SetAndMarkDirty(ref hoverButtonIndex, 0);
        }

        private void ChangeValue(int delta)
        {
            var oldVal = param.GetValue();
            var newVal = param.SnapAndClampValue(oldVal + delta);
            param.SetValue(newVal);
            MarkDirty();
        }

        public override void Tick(float delta)
        {
            Debug.Assert(capture);

            if (capture)
            {
                var captureDuration = Platform.TimeSeconds() - captureTime;
                if (captureDuration > 0.35)
                    ChangeValue(captureButton);
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
            var labelWidth = width - buttonSizeX * 2;
            var buttonOffsetY = Utils.DivideAndRoundUp(height - buttonSizeY, 2);
            var val = param.GetValue();
            var valPrev = param.SnapAndClampValue(val - 1);
            var valNext = param.SnapAndClampValue(val + 1);
            var opacity = paramEnabled ? 1.0f : 0.25f;
            var opacityL = paramEnabled && val != valPrev ? (hoverButtonIndex == -1 ? 0.6f : 1.0f) : 0.25f;
            var opacityR = paramEnabled && val != valNext ? (hoverButtonIndex ==  1 ? 0.6f : 1.0f) : 0.25f;

            c.DrawTextureAtlas(bmpLeft, 0, buttonOffsetY, bmpScale, Color.Black.Transparent(opacityL));
            c.DrawText(param.GetValueString(), Fonts.FontMedium, buttonSizeX, 0, Color.Black.Transparent(opacity), TextFlags.MiddleCenter, labelWidth, height);
            c.DrawTextureAtlas(bmpRight, buttonSizeX + labelWidth, buttonOffsetY, bmpScale, Color.Black.Transparent(opacityR));
        }
    }
}
