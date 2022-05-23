using System.Drawing;
using System.Collections.Generic;
using System.Globalization;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;
using System.Windows.Forms;

namespace FamiStudio
{
    public class NumericUpDown2 : RenderControl
    {
        private int val;
        private int min;
        private int max = 10;
        private RenderBitmapAtlasRef[] bmp;

        private float captureDuration;
        private int   captureButton = -1;

        private int hoverButton = -1;

        public NumericUpDown2(int value, int minVal, int maxVal)
        {
            val = value;
            min = minVal;
            max = maxVal;
            height = DpiScaling.ScaleForDialog(24);
        }

        public int Value
        {
            get { return val; }
            set
            {
                var newVal = Utils.Clamp(value, min, max);
                if (newVal != val)
                {
                    val = newVal;
                    MarkDirty();
                }
            }
        }

        protected override void OnRenderInitialized(RenderGraphics g)
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
            for (int i = 0; i < 2; i++)
            {
                var rect = GetButtonRect(i);
                if (rect.Contains(x, y))
                    return i;
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

        protected override void OnMouseDown(MouseEventArgsEx e)
        {
            var idx = IsPointInButton(e.X, e.Y);
            if (idx >= 0)
            {
                captureButton = idx;
                captureDuration = 0;
                Value += captureButton;
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
            var newHoverButton = IsPointInButton(e.X, e.Y);
            if (newHoverButton != hoverButton)
            {
                hoverButton = newHoverButton;
                MarkDirty();
            }
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            hoverButton = -1;
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList(GLGraphicsBase.CommandListUsage.Dialog);
            var rects = new []
            {
                GetButtonRect(0),
                GetButtonRect(1)
            };

            for (int i = 0; i < 2; i++)
            {
                var fillBrush = captureButton == i ? ThemeResources.MediumGreyFillBrush1 :
                                hoverButton   == i ? ThemeResources.DarkGreyFillBrush3 :
                                                     ThemeResources.DarkGreyFillBrush2;

                c.FillAndDrawRectangle(rects[i], fillBrush, ThemeResources.LightGreyFillBrush1);

                c.PushTranslation(0, captureButton == i ? 1 : 0);
                c.DrawBitmapAtlasCentered(bmp[i], rects[i], 1, 1, Theme.LightGreyFillColor1);
                c.PopTransform();
            }

            c.DrawText(val.ToString(CultureInfo.InvariantCulture), ThemeResources.FontMedium, rects[0].Right, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleCenter, rects[1].Left - rects[0].Right, height);
        }
    }
}
