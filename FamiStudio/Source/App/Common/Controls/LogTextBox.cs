using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace FamiStudio
{
    public class LogTextBox : Control
    {
        private List<string> lines = new List<string>();
        private int numLines;
        private int scroll = 0;
        private bool draggingScrollbars;
        private int captureScrollBarPos;
        private int captureMouseY;
        private int maxScroll = 0;

        private int margin         = DpiScaling.ScaleForWindow(4);
        private int scrollBarWidth = DpiScaling.ScaleForWindow(10);
        private int lineHeight     = DpiScaling.ScaleForWindow(16);

        private LocalizedString CopyLogTextContext;

        public LogTextBox(int lineCount) 
        {
            Localization.Localize(this);

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

        protected override void OnPointerDown(PointerEventArgs e)
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
                    CapturePointer();
                    draggingScrollbars = true;
                    captureScrollBarPos = scrollBarPos;
                    captureMouseY = e.Y;
                }

                MarkDirty();
                return;
            }
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            if (draggingScrollbars)
            {
                draggingScrollbars = false;
                ReleasePointer();
                MarkDirty();
            }
            else if (e.Right)
            {
                App.ShowContextMenuAsync([new ContextMenuOption("MenuCopy", CopyLogTextContext, () => Platform.SetClipboardString(string.Join(Environment.NewLine, lines)))]);
            }
        }

        private void UpdateScrollParams()
        {
            maxScroll = Math.Max(0, lines.Count - numLines);
        }

        protected override void OnPointerMove(PointerEventArgs e)
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

        protected override void OnMouseWheel(PointerEventArgs e)
        {
            var sign = e.ScrollY < 0 ? 1 : -1;

            if (sign == 0)
                return;

            SetAndMarkDirty(ref scroll, Utils.Clamp(scroll + sign * 3, 0, maxScroll));
            e.MarkHandled();
        }

        protected override void OnRender(Graphics g)
        {
            Debug.Assert(enabled); // TODO : Add support for disabled state.

            var c = g.GetCommandList();
            var hasScrollBar = GetScrollBarParams(out var scrollBarPos, out var scrollBarSize);
            var actualScrollBarWidth = hasScrollBar ? scrollBarWidth : 0;

            c.FillAndDrawRectangle(0, 0, width - 1, height, Theme.DarkGreyColor1, Theme.LightGreyColor1);

            for (int i = 0, j = scroll; i < numLines && j < lines.Count; j++, i++)
            {
                c.DrawText(lines[j], Fonts.FontMedium, margin, i * lineHeight, Theme.LightGreyColor1, TextFlags.MiddleLeft | TextFlags.Clip, width - margin - actualScrollBarWidth, lineHeight);
            }

            if (hasScrollBar)
            {
                c.FillAndDrawRectangle(width - scrollBarWidth, 0, width - 1, height, Theme.DarkGreyColor4, Theme.LightGreyColor1);
                c.FillAndDrawRectangle(width - scrollBarWidth, scrollBarPos, width - 1, scrollBarPos + scrollBarSize, Theme.MediumGreyColor1, Theme.LightGreyColor1);
            }
        }
    }
}
