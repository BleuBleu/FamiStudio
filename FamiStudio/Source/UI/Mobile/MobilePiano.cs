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
    public class MobilePiano : RenderControl
    {
        const int NumOctaves = 8;
        const int NumNotes   = NumOctaves * 12;

        const int DefaultButtonSize    = 135;
        const int DefaultIconPos       = 12;
        const int DefaultWhiteKeySizeX = 120;
        const int DefaultBlackKeySizeX = 96;

        private int   whiteKeySizeX;
        private int   blackKeySizeX;
        private int   octaveSizeX;
        private int   iconPos;
        private int   virtualSizeX;
        private int   buttonSize;
        private float iconScaleFloat = 1.0f;

        enum CaptureOperation
        {
            None,
            MobilePan
        }

        private enum ButtonImageIndices
        {
            MobilePianoNext,
            MobilePianoPrev,
            MobilePianoDrag,
            Count
        };

        private readonly string[] ButtonImageNames = new string[]
        {
            "MobilePianoNext",
            "MobilePianoPrev",
            "MobilePianoDrag"
        };

        RenderBrush whiteKeyBrush;
        RenderBrush blackKeyBrush;
        RenderBitmapAtlas bmpButtonAtlas;
        //Button[] buttons = new Button[(int)ButtonType.Count];

        private float zoomX = 1.0f;
        private int scrollX = 0;

        // Mouse tracking.
        private int lastX;
        private int lastY;
        private int captureX;
        private int captureY;
        private float flingVelX;
        private CaptureOperation captureOperation = CaptureOperation.None;

        // Scaled layout variables.
        private int layoutSize;
        
        public int LayoutSize => layoutSize;

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            Debug.Assert((int)ButtonImageIndices.Count == ButtonImageNames.Length);

            var displayInfo = Xamarin.Essentials.DeviceDisplay.MainDisplayInfo;

            layoutSize = Math.Min((int)displayInfo.Width, (int)displayInfo.Height) / 4;
            buttonSize = layoutSize / 2;

            whiteKeyBrush  = g.CreateVerticalGradientBrush(0, layoutSize, Theme.LightGreyFillColor1, Theme.LightGreyFillColor2);
            blackKeyBrush  = g.CreateVerticalGradientBrush(0, layoutSize, Theme.DarkGreyFillColor1,  Theme.DarkGreyFillColor2);
            bmpButtonAtlas = g.CreateBitmapAtlasFromResources(ButtonImageNames);
        }
        
        private void UpdateRenderCoords()
        {
            var displayInfo = Xamarin.Essentials.DeviceDisplay.MainDisplayInfo;
            var scale = Math.Min((int)displayInfo.Width, (int)displayInfo.Height) / 1080.0f;

            whiteKeySizeX = ScaleCustom(DefaultWhiteKeySizeX, scale * zoomX);
            blackKeySizeX = ScaleCustom(DefaultBlackKeySizeX, scale * zoomX);
            octaveSizeX   = 7 * whiteKeySizeX;
            virtualSizeX  = octaveSizeX * NumOctaves;
        }

        protected override void OnRenderTerminated()
        {
            Utils.DisposeAndNullify(ref bmpButtonAtlas);
            //Utils.DisposeAndNullify(ref scrollBarBrush);

            Utils.DisposeAndNullify(ref whiteKeyBrush);
            Utils.DisposeAndNullify(ref blackKeyBrush);
        }

        protected override void OnResize(EventArgs e)
        {
            UpdateRenderCoords();
            ClampScroll();
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

        private bool IsBlackKey(int key)
        {
            return key == 1 || key == 3 || key == 6 || key == 8 || key == 10;
        }

        private Rectangle GetKeyRectangle(int octave, int key)
        {
            var blackKey = IsBlackKey(key);

            key = key <= 4 ? key / 2 : 3 + (key - 5) / 2;

            if (blackKey)
                return new Rectangle(octaveSizeX * octave + key * whiteKeySizeX + whiteKeySizeX - blackKeySizeX / 2 - scrollX, 0, blackKeySizeX, Height / 2);
            else
                return new Rectangle(octaveSizeX * octave + key * whiteKeySizeX - scrollX, 0, whiteKeySizeX, Height);
        }

        private Rectangle GetDragRectangle(int octave, int idx)
        {
            if (octave == NumOctaves - 1 && idx == 1)
                return Rectangle.Empty;

            var r0 = GetKeyRectangle(octave,       idx == 0 ? 3 : 10);
            var r1 = GetKeyRectangle(octave + idx, idx == 0 ? 6 : 1);

            return new Rectangle(r0.Right, 0, r1.Left - r0.Right, Height / 2);
        }

        protected override void OnRender(RenderGraphics g)
        {
            int actualWidth = Width - buttonSize;
            int maxVisibleOctave = 8; // NumOctaves - Utils.Clamp((int)Math.Floor(scrollX / (float)octaveSizeX), 0, NumOctaves);
            int minVisibleOctave = 0; // NumOctaves - Utils.Clamp((int)Math.Ceiling((scrollX + actualWidth) / (float)octaveSizeX), 0, NumOctaves);

            var cb = g.CreateCommandList();
            var cp = g.CreateCommandList();

            // Background (white keys)
            cb.FillRectangle(0, 0, Width, Height, whiteKeyBrush);

            // Black keys
            for (int i = minVisibleOctave; i < maxVisibleOctave; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    if (IsBlackKey(j))
                        cp.FillRectangle(GetKeyRectangle(i, j), blackKeyBrush);
                }
            }

            // Lines between white keys
            for (int i = minVisibleOctave; i < maxVisibleOctave; i++)
            {
                for (int j = 0; j < 12; j++)
                {
                    if (!IsBlackKey(j))
                    {
                        var x = GetKeyRectangle(i, j).X;
                        var y = j == 0 || j == 5 ? 0 : Height / 2;
                        cp.DrawLine(x, y, x, Height, ThemeResources.BlackBrush);
                    }
                }
            }

            // Drag images
            for (int i = minVisibleOctave; i < maxVisibleOctave; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    var r = GetDragRectangle(i, j);
                    if (!r.IsEmpty)
                    {
                        var size  = bmpButtonAtlas.GetElementSize((int)ButtonImageIndices.MobilePianoDrag);
                        var scale = Math.Min(r.Width, r.Height) / (float)Math.Min(size.Width, size.Height);
                        var posX  = r.X + r.Width  / 2 - (int)(size.Width  * scale / 2);
                        var posY  =       r.Height / 2 - (int)(size.Height * scale / 2);
                        cp.DrawBitmapAtlas(bmpButtonAtlas, (int)ButtonImageIndices.MobilePianoDrag, posX, posY, 0.5f, scale, Color.Black);
                    }
                }
            }

            //if ((editMode == EditionMode.Channel || editMode == EditionMode.DPCMMapping) && ThemeResources.FontSmall.Size < noteSizeY)
            //    r.cp.DrawText("C" + i, ThemeResources.FontSmall, r.g.WindowScaling, octaveBaseX - noteSizeY + 1, ThemeResources.BlackBrush, RenderTextFlags.Middle, whiteKeySizeX - r.g.WindowScaling * 2, noteSizeY - 1);
            //if ((i == playOctave && j == playNote) || (draggingNote && (i == dragOctave && j == dragNote)))
            //    r.cp.FillRectangle(GetKeyRectangle(i, j), blackKeyPressedBrush);

            g.DrawCommandList(cb);
            g.DrawCommandList(cp, new Rectangle(0, 0, actualWidth, Height));
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
            var actualWidth = Width - buttonSize;
            var minScrollX = 0;
            var maxScrollX = Math.Max(virtualSizeX - actualWidth, 0);

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
