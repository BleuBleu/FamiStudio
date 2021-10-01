using System;
using System.Diagnostics;

using Color     = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush       = FamiStudio.GLBrush;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderFont        = FamiStudio.GLFont;

namespace FamiStudio
{
    public class Piano : RenderControl
    {
        const int DefaultButtonSize       = 135;
        const int DefaultIconPos          = 12;

        enum CaptureOperation
        {
            None,
            MobilePan
        }

        private enum ButtonImageIndices
        { 
            Forward,
            Backward,
            Count
        };

        private readonly string[] ButtonImageNames = new string[]
        {
            "Forward",
            "Backward"
        };

        RenderBrush whiteKeyBrush;
        RenderBrush blackKeyBrush;
        RenderBitmapAtlas bmpButtonAtlas;
        //Button[] buttons = new Button[(int)ButtonType.Count];

        // Popup-list scrollong.
        private int scrollX = 0;
        private int maxScrollX = 0;

        // Mouse tracking.
        private int lastX;
        private int lastY;
        private int captureX;
        private int captureY;
        private float flingVelX;
        private CaptureOperation captureOperation = CaptureOperation.None;

        // Scaled layout variables.
        private int buttonSize;
        private float iconScaleFloat = 1.0f;

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            Debug.Assert((int)ButtonImageIndices.Count == ButtonImageNames.Length);

            bmpButtonAtlas = g.CreateBitmapAtlasFromResources(ButtonImageNames);
            //scrollBarBrush = g.CreateSolidBrush(Color.FromArgb(64, Color.Black));

            //var scale = Math.Min(ParentFormSize.Width, ParentFormSize.Height) / 1080.0f;
/*
            buttonFont   = ThemeResources.GetBestMatchingFontByHeight(g, ScaleCustom(DefaultTextSize, scale), false);
            listFont     = ThemeResources.GetBestMatchingFontByHeight(g, ScaleCustom(DefaultListItemTextSize, scale), false);
            listFontBold = ThemeResources.GetBestMatchingFontByHeight(g, ScaleCustom(DefaultListItemTextSize, scale), true);

            buttonSize        = ScaleCustom(DefaultButtonSize, scale);
            buttonSizeNav     = ScaleCustom(DefaultNavButtonSize, scale);
            buttonIconPos1    = ScaleCustom(DefaultIconPos1, scale);
            buttonIconPos2    = ScaleCustom(DefaultIconPos2, scale);
            textPosTop        = ScaleCustom(DefaultTextPosTop, scale);
            listItemSize      = ScaleCustom(DefaultListItemSize, scale);
            listIconPos       = ScaleCustom(DefaultListIconPos, scale);
            scrollBarSizeX    = ScaleCustom(DefaultScrollBarSizeX, scale);
            iconScaleFloat    = ScaleCustomFloat(DefaultIconSize / (float)bmpButtonAtlas.GetElementSize(0).Width, scale);
*/
        }

        protected override void OnRenderTerminated()
        {
            //Utils.DisposeAndNullify(ref bmpButtonAtlas);
            //Utils.DisposeAndNullify(ref scrollBarBrush);
        }

        protected override void OnResize(EventArgs e)
        {
            //UpdateButtonLayout();
            base.OnResize(e);
        }

        private void TickFling(float delta)
        {
            if (flingVelX != 0.0f)
            {
                var deltaPixel = (int)Math.Round(flingVelX * delta);
                if (deltaPixel != 0 && DoScroll(-deltaPixel))
                    flingVelX *= (float)Math.Exp(delta * -4.5f);
                else
                    flingVelX = 0.0f;
            }
        }

        public void Tick(float delta)
        {
            TickFling(delta);
        }

        protected override void OnRender(RenderGraphics g)
        {
        }

        private void StartCaptureOperation(int x, int y, CaptureOperation op)
        {
            lastX = x;
            lastY = y;
            captureX = x;
            captureY = y;
            captureOperation = op;
        }

        private bool ClampScroll()
        {
            var scrolled = true;
            if (scrollX < 0) { scrollX = 0; scrolled = false; }
            if (scrollX > maxScrollX) { scrollX = maxScrollX; scrolled = false; }
            return scrolled;
        }

        private bool DoScroll(int deltaX)
        {
            scrollX += deltaX;
            MarkDirty();
            return ClampScroll();
        }

        private void UpdateCaptureOperation(int x, int y)
        {
            if (captureOperation == CaptureOperation.MobilePan)
                DoScroll(lastX - x);
        }

        private void EndCaptureOperation(int x, int y)
        {
            captureOperation = CaptureOperation.None;
            MarkDirty();
        }

        protected override void OnTouchUp(int x, int y)
        {
            EndCaptureOperation(x, y);
        }

        protected override void OnTouchDown(int x, int y)
        {
            flingVelX = 0;

/*
            if (popupRatio == 1.0f)
            {
                var rect = GetExpandedListRect();
                if (rect.Contains(x, y))
                    StartCaptureOperation(x, y, CaptureOperation.MobilePan);
            }
*/            
        }

        protected override void OnTouchFling(int x, int y, float velX, float velY)
        {
            EndCaptureOperation(x, y);
            flingVelX = velX;
        }

        protected override void OnTouchMove(int x, int y)
        {
            UpdateCaptureOperation(x, y);
            lastX = x;
            lastY = y;
        }
    }
}
