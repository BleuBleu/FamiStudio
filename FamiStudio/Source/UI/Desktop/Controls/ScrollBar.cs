using System;
using System.Collections.Generic;
using System.Diagnostics;

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

        private bool scrolling;
        private bool horizontal;
        private int captureScroll;
        private int captureMouse;

        public int VirtualSize { get => virtualSize; set => SetAndMarkDirty(ref virtualSize, value); }
        public int ScrollBarThickness => scrollbarThickness;

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
            GetScrollBarParams(out var scrollBarPos, out var scrollBarSize);
            if (coord < scrollBarPos)
            {
                SetScroll(scrollValue - height);
            }
            else if (coord > (scrollBarPos + scrollBarSize))
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

        private void SetScroll(int newScroll)
        {
            newScroll = Utils.Clamp(newScroll, 0, virtualSize - (horizontal ? width : height));
            SetAndMarkDirty(ref scrollValue, newScroll);
        }

        private void UpdateThickness()
        {
            scrollbarThickness = DpiScaling.ScaleForWindow(Settings.ScrollBars >= 2 ? DefaultScrollBarThickness2 : DefaultScrollBarThickness1);
        }

        private void GetScrollBarParams(out int pos, out int size)
        {
            if (horizontal)
            {
                size = (int)Math.Round(width * (width / (float)virtualSize));
                pos  = (int)Math.Round(width * (scrollValue / (float)virtualSize));
            }
            else
            {
                size = (int)Math.Round(height * (height / (float)virtualSize));
                pos  = (int)Math.Round(height * (scrollValue / (float)virtualSize));
            }
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.DefaultCommandList;

            c.FillRectangle(0, 0, width, height, Theme.DarkGreyColor4);

            base.OnRender(g);

            GetScrollBarParams(out var scrollBarPos, out var scrollBarSize);

            if (horizontal)
            {
                c.FillAndDrawRectangle(0, 0, width - 1, scrollbarThickness, Theme.DarkGreyColor4, Theme.LightGreyColor1);
                c.FillAndDrawRectangle(scrollBarPos - 1, 0, scrollBarPos + scrollBarSize, scrollbarThickness, Theme.MediumGreyColor1, Theme.LightGreyColor1);
            }
            else
            {
                c.FillAndDrawRectangle(0, 0, scrollbarThickness, height - 1, Theme.DarkGreyColor4, Theme.LightGreyColor1);
                c.FillAndDrawRectangle(0, scrollBarPos - 1, scrollbarThickness, scrollBarPos + scrollBarSize, Theme.MediumGreyColor1, Theme.LightGreyColor1);
            }
        }
    }
}
