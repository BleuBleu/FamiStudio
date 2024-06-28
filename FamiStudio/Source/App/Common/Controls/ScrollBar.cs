using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace FamiStudio
{
    public class ScrollBar : Control
    {
        const int DefaultScrollBarThickness1 = 10;
        const int DefaultScrollBarThickness2 = 16;
        const int DefaultScrollStepSize = 60;

        private int scrollValue;
        private int virtualSize;
        private int scrollbarThickness;
        private int scrollStepSize = DpiScaling.ScaleForWindow(DefaultScrollStepSize);
        private Color lineColor = Theme.LightGreyColor1;

        private bool scrolling;
        private bool horizontal;
        private int captureScroll;
        private int captureMouse;

        public Color LineColor { get => lineColor; set => lineColor = value; }
        public int VirtualSize { get => virtualSize; set => SetAndMarkDirty(ref virtualSize, value); }
        public int ScrollBarThickness => scrollbarThickness;
        public override bool SupportsDoubleClick => false;

        public delegate void ScrolledDelegate(ScrollBar sender, int pos);
        public event ScrolledDelegate Scrolled;

        public ScrollBar()
        {
            UpdateThickness();
        }

        protected override void OnAddedToContainer()
        {
            UpdateThickness();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            var coord = horizontal ? e.X : e.Y;
            GetScrollBarParams(out var scrollBarPos1, out var scrollBarPos2);
            if (coord < scrollBarPos1)
            {
                SetScroll(scrollValue - height);
            }
            else if (coord > scrollBarPos2)
            {
                SetScroll(scrollValue + height);
            }
            else
            {
                captureMouse = coord;
                captureScroll = scrollValue;
                UpdateScroll(coord);
                scrolling = true;
                Capture = true;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (scrolling)
            {
                scrolling = false;
                Capture = false;
                UpdateScroll(horizontal ? e.X : e.Y);
                MarkDirty();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (scrolling)
            {
                UpdateScroll(horizontal ? e.X : e.Y);
            }
        }

        private void UpdateScroll(int coord)
        {
            SetScroll(captureScroll + ((coord - captureMouse) * virtualSize / (horizontal ? width : height)));
        }

        public void SetScroll(int newScroll, bool fireEvent = true)
        {
            newScroll = Utils.Clamp(newScroll, 0, Math.Max(0, virtualSize - (horizontal ? width : height)));
            if (SetAndMarkDirty(ref scrollValue, newScroll) && fireEvent)
            {
                Scrolled?.Invoke(this, newScroll);
            }
        }

        private void UpdateThickness()
        {
            scrollbarThickness = DpiScaling.ScaleForWindow(Settings.ScrollBars >= 2 ? DefaultScrollBarThickness2 : DefaultScrollBarThickness1);
        }

        private void GetScrollBarParams(out int pos1, out int pos2)
        {
            float p1;
            float p2;
            if (horizontal)
            {
                p1 = width * (scrollValue / (float)virtualSize);
                p2 = p1 + width * (width / (float)virtualSize);
            }
            else
            {
                p1 = height * (scrollValue / (float)virtualSize);
                p2 = p1 + height * (height / (float)virtualSize);
            }
            pos1 = (int)Math.Round(p1);
            pos2 = (int)Math.Round(p2);
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.DefaultCommandList;

            c.FillRectangle(0, 0, width, height, Theme.DarkGreyColor4);

            base.OnRender(g);

            GetScrollBarParams(out var scrollBarPos1, out var scrollBarPos2);

            if (horizontal)
            {
                c.FillAndDrawRectangle(0, 0, width, scrollbarThickness - 1, Theme.DarkGreyColor4, lineColor);
                c.FillAndDrawRectangle(scrollBarPos1, 0, scrollBarPos2, scrollbarThickness - 1, Theme.MediumGreyColor1, lineColor);
            }
            else
            {
                c.FillAndDrawRectangle(0, 0, scrollbarThickness - 1, height, Theme.DarkGreyColor4, lineColor);
                c.FillAndDrawRectangle(0, scrollBarPos1, scrollbarThickness - 1, scrollBarPos2, Theme.MediumGreyColor1, lineColor);
            }
        }
    }
}
