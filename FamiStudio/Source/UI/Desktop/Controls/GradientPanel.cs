using System;
using System.Diagnostics;
using System.Drawing;

namespace FamiStudio
{
    public class GradientPanel : Container
    {
        private Color colorTop;
        private Color colorBottom;

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

        protected override void OnRender(Graphics g)
        {
            g.DefaultCommandList.FillAndDrawRectangleGradient(0, 0, width, height, colorTop, colorBottom, Color.Black, true, height);
            base.OnRender(g);
        }
    }
}
