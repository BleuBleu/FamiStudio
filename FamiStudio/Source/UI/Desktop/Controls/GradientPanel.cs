using System;
using System.Diagnostics;
using System.Drawing;

namespace FamiStudio
{
    public class GradientPanel : Container
    {
        public delegate void ClickDelegate(Control sender);
        public event ClickDelegate RightClick;

        private Color colorTop;
        private Color colorBottom;

        private float blinkTimer;

        public GradientPanel(Color color)
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
                colorBottom = Theme.Darken(value, 20); // MATT : We used to draw same color, but with alpha = 200
                MarkDirty();
            }
        }

        public void Blink()
        {
            blinkTimer = 2.0f;
            MarkDirty();
        }

        public override void Tick(float delta)
        {
            base.Tick(delta);

            if (blinkTimer != 0.0f)
            {
                blinkTimer = MathF.Max(0.0f, blinkTimer - delta);
                MarkDirty();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Right)
                RightClick?.Invoke(this);
            base.OnMouseUp(e);
        }

        protected override void OnRender(Graphics g)
        {
            g.DefaultCommandList.FillAndDrawRectangleGradient(0, 0, width, height, colorTop, colorBottom, Color.Black, true, height);
            base.OnRender(g);
        }
    }
}
