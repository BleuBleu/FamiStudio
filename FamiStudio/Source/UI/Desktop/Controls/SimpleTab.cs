using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class SimpleTab : Control
    {
        public delegate void ClickDelegate(Control sender);
        public event ClickDelegate Click;

        private string text;
        private bool selected;
        private bool hover;
        private bool press;

        public SimpleTab(string txt, bool sel) 
        {
            text  = txt;
            selected = sel;
        }

        public bool Selected
        {
            get { return selected; }
            set { SetAndMarkDirty(ref selected, value); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (enabled && e.Left)
            {
                press = true;
            }
            hover = true;
            MarkDirty();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (enabled && e.Left)
            {
                press = false;
                Click?.Invoke(this);
            }
            MarkDirty();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            SetAndMarkDirty(ref hover, true);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            SetAndMarkDirty(ref hover, false);
            SetAndMarkDirty(ref press, false);
        }

        public int ScaleLineForWindow(int width)
        {
            return width == 1 ? 1 : DpiScaling.ScaleForWindow(width) | 1;
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.GetCommandList();
            var lineThickness = ScaleLineForWindow(selected ? 3 : 1);

            c.PushTranslation(0, press ? 1 : 0);
            c.DrawText(text, selected ? Fonts.FontMediumBold : Fonts.FontMedium, 0, 0, Color.Black, TextFlags.MiddleCenter, width, height);
            c.PopTransform();
            c.DrawLine(0, height - lineThickness / 2, width, height - lineThickness / 2, Color.Black, lineThickness);
        }
    }
}
