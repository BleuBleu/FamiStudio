using System;

namespace FamiStudio
{
    // MATTT : Delete if unused.
    public class HorizontalLine : Control
    {
        private Color color = Theme.LightGreyColor1;

        public HorizontalLine()
        {
        }

        public Color Color { get => color; set => SetAndMarkDirty(ref color, value); }

        protected override void OnRender(Graphics g)
        {
            g.DefaultCommandList.DrawLine(0, 0, width, 0, color);
        }
    }
}
