using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System;
using System.Diagnostics;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class LogTextBox2 : RenderControl
    {
        private List<string> lines = new List<string>();
        private int numLines;
        private int scroll = 0;
        private bool draggingScrollbars;
        private int captureScrollBarPos;
        private int captureMouseY;
        private int maxScroll = 0;

        private int margin         = DpiScaling.ScaleForMainWindow(4);
        private int scrollBarWidth = DpiScaling.ScaleForMainWindow(10);
        private int lineHeight     = DpiScaling.ScaleForMainWindow(16);

        public LogTextBox2(int lineCount)
        {
            numLines = lineCount;
            height = lineCount* lineHeight;
            UpdateScrollParams();
        }

        public void AddLine(string line)
        {
            lines.Add(line);
            MarkDirty();
            UpdateScrollParams();
            scroll = maxScroll;
        }

        protected override void OnMouseDown(MouseEventArgs2 e)
        {
            if (e.Left && 
                GetScrollBarParams(out var scrollBarPos, out var scrollBarSize) && 
                e.X > width - scrollBarWidth)
            {
                var y = e.Y - lineHeight;

                if (y < scrollBarPos)
                {
                    scroll = Math.Max(0, scroll - lineHeight * 3);
                }
                else if (y > (scrollBarPos + scrollBarSize))
                {
                    scroll = Math.Min(maxScroll, scroll + lineHeight * 3);
                }
                else
                {
                    Capture = true;
                    draggingScrollbars = true;
                    captureScrollBarPos = scrollBarPos;
                    captureMouseY = e.Y;
                }

                MarkDirty();
                return;
            }
        }

        protected override void OnMouseUp(MouseEventArgs2 e)
        {
            if (draggingScrollbars)
            {
                draggingScrollbars = false;
                Capture = false;
                MarkDirty();
            }
        }

        private void UpdateScrollParams()
        {
            maxScroll = Math.Max(0, lines.Count - numLines);
        }

        protected override void OnMouseMove(MouseEventArgs2 e)
        {
            if (draggingScrollbars)
            {
                GetScrollBarParams(out var scrollBarPos, out var scrollBarSize);
                var newScrollBarPos = captureScrollBarPos + (e.Y - captureMouseY);
                var ratio = newScrollBarPos / (float)(height - scrollBarSize);
                var newListScroll = Utils.Clamp((int)Math.Round(ratio * maxScroll), 0, maxScroll);
                SetAndMarkDirty(ref scroll, newListScroll);
            }
        }

        private bool GetScrollBarParams(out int pos, out int size)
        {
            if (lines.Count > numLines)
            {
                var scrollAreaSize = height;
                var minScrollBarSizeY = scrollAreaSize / 4;
                var scrollY = scroll * lineHeight;
                var maxScrollY = maxScroll * lineHeight;

                size = Math.Max(minScrollBarSizeY, (int)Math.Round(scrollAreaSize * Math.Min(1.0f, scrollAreaSize / (float)(maxScrollY + scrollAreaSize))));
                pos  = (int)Math.Round((scrollAreaSize - size) * (scrollY / (float)maxScrollY));

                return true;
            }

            pos  = 0;
            size = 0;
            return false;
        }

        protected override void OnMouseWheel(MouseEventArgs2 e)
        {
            var sign = e.ScrollY < 0 ? 1 : -1;

            if (sign == 0)
                return;

            SetAndMarkDirty(ref scroll, Utils.Clamp(scroll + sign * 3, 0, maxScroll));
        }

        protected override void OnRender(RenderGraphics g)
        {
            Debug.Assert(enabled); // TODO : Add support for disabled state.

            var c = parentDialog.CommandList;
            var hasScrollBar = GetScrollBarParams(out var scrollBarPos, out var scrollBarSize);
            var actualScrollBarWidth = hasScrollBar ? scrollBarWidth : 0;

            c.FillAndDrawRectangle(0, 0, width - 1, height, ThemeResources.DarkGreyLineBrush1, ThemeResources.LightGreyFillBrush1);

            for (int i = 0, j = scroll; i < numLines && j < lines.Count; j++, i++)
            {
                c.DrawText(lines[j], ThemeResources.FontMedium, margin, i * lineHeight, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft | RenderTextFlags.Clip, width - margin - actualScrollBarWidth, lineHeight);
            }

            if (hasScrollBar)
            {
                c.FillAndDrawRectangle(width - scrollBarWidth, 0, width - 1, height, ThemeResources.DarkGreyFillBrush1, ThemeResources.LightGreyFillBrush1);
                c.FillAndDrawRectangle(width - scrollBarWidth, scrollBarPos, width - 1, scrollBarPos + scrollBarSize, ThemeResources.MediumGreyFillBrush1, ThemeResources.LightGreyFillBrush1);
            }
        }
    }
}
