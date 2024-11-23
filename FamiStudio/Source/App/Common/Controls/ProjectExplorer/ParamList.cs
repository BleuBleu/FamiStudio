using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices.Marshalling;

namespace FamiStudio
{
    public class ParamList : ParamControl
    {
        private float bmpScale = Platform.IsMobile ? DpiScaling.Window * 0.25f : 1.0f;

        private TextureAtlasRef bmpLeft;
        private TextureAtlasRef bmpRight;

        private int buttonSizeX;
        private int buttonSizeY;
        private int hoverButtonIndex;
        private bool capture;
        private int captureButton;
        private float changeDelay;

        public ParamList(ParamInfo p) : base(p)
        {
            height = DpiScaling.ScaleForWindow(16);
            supportsDoubleClick = false;
        }

        public override bool CanReceiveLongPress => !capture;

        protected override void OnAddedToContainer()
        {
            bmpLeft = ParentWindow.Graphics.GetTextureAtlasRef("ButtonLeft");
            bmpRight = ParentWindow.Graphics.GetTextureAtlasRef("ButtonRight");
            buttonSizeX = DpiScaling.ScaleCustom(bmpLeft.ElementSize.Width, bmpScale);
            buttonSizeY = DpiScaling.ScaleCustom(bmpLeft.ElementSize.Height, bmpScale);
            height = buttonSizeY;
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

        protected override void OnPointerDown(PointerEventArgs e)
        {
            if (e.Left && IsParamEnabled())
            {
                var buttonIndex = GetButtonIndex(e.X);
                if (buttonIndex != 0)
                {
                    Debug.Assert(!capture);
                    InvokeValueChangeStart();
                    changeDelay = 0.35f;
                    capture = true;
                    captureButton = buttonIndex;
                    ChangeValue(buttonIndex);
                    SetTickEnabled(true);
                    CapturePointer();
                    e.MarkHandled();
                }
            }
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            if (e.Left && capture)
            {
                capture = false;
                SetTickEnabled(false);
                InvokeValueChangeEnd();
                e.MarkHandled();
            }
            else if (e.Right && GetButtonIndex(e.X) == 0)
            {
                ShowParamContextMenu();
                e.MarkHandled();
            }
        }

        protected override void OnPointerMove(PointerEventArgs e)
        {
            SetAndMarkDirty(ref hoverButtonIndex, enabled ? GetButtonIndex(e.X) : 0);
        }

        protected override void OnPointerLeave(EventArgs e)
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
                changeDelay -= delta;
                if (changeDelay <= 0)
                {
                    ChangeValue(captureButton);
                    changeDelay = 0.1f;
                }
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
            var valString = param.GetValueString();
            var opacity = paramEnabled ? 255 : 64;
            var opacityL = paramEnabled && val != valPrev ? (hoverButtonIndex == -1 ? 150 : 255) : 64;
            var opacityR = paramEnabled && val != valNext ? (hoverButtonIndex ==  1 ? 150 : 255) : 64;

            c.DrawTextureAtlas(bmpLeft, 0, buttonOffsetY, bmpScale, Color.Black.Transparent(opacityL));

            if (valString.StartsWith("img:"))
            {
                var img = c.Graphics.GetTextureAtlasRef(valString.Substring(4));
                c.DrawTextureAtlasCentered(img, buttonSizeX, 0, labelWidth, height, 1, Color.Black);
            }
            else
            {
                c.DrawText(valString, Fonts.FontMedium, buttonSizeX, 0, Color.Black.Transparent(opacity), TextFlags.MiddleCenter, labelWidth, height);
            }

            c.DrawTextureAtlas(bmpRight, buttonSizeX + labelWidth, buttonOffsetY, bmpScale, Color.Black.Transparent(opacityR));
        }
    }
}
