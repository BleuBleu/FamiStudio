using System;
using System.Diagnostics;
using System.Drawing;

namespace FamiStudio
{
    public class PanelContainer : Container
    {
        private Color colorTop;
        private Color colorBottom;

        private float blinkTimer;

        public PanelContainer(Color color)
        {
            Color = color;
            clipRegion = false;
        }

        public Color Color
        {
            get { return colorTop; }
            set 
            {
                colorTop = value;
                colorBottom = Color.FromArgb(200, value);
                MarkDirty();
            }
        }

        public void Blink()
        {
            blinkTimer = 2.0f;
            SetTickEnabled(true);
            MarkDirty();
        }

        public override void Tick(float delta)
        {
            if (blinkTimer != 0.0f)
            {
                blinkTimer = MathF.Max(0.0f, blinkTimer - delta);
                if (blinkTimer == 0.0f)
                    SetTickEnabled(false);
                MarkDirty();
            }
        }

        protected override void OnRender(Graphics g)
        {
            var actualColorBottom = colorBottom;

            if (blinkTimer != 0.0f)
            {
                actualColorBottom = Theme.Darken(colorTop, (int)(MathF.Sin(blinkTimer * MathF.PI * 8.0f) * 16 + 16));
                actualColorBottom = Color.FromArgb(200, actualColorBottom);
            }

            g.DefaultCommandList.FillAndDrawRectangleGradient(0, 0, width, height, colorTop, actualColorBottom, Color.Black, true, height);
            base.OnRender(g);
        }
    }
}
