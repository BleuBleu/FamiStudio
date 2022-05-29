using System;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class DropDown2 : RenderControl
    {
        private const int MaxItemsInList = 10;

        public delegate void SelectedIndexChangedDelegate(RenderControl sender, int index);
        public delegate void ListClosingDelegate(RenderControl sender);

        public event SelectedIndexChangedDelegate SelectedIndexChanged;
        public event ListClosingDelegate ListClosing;

        private GLBitmapAtlasRef bmpArrow;
        private string[] items;
        private int selectedIndex = 0;
        private bool hover;
        private bool listOpened;
        private bool transparent;
        private int listHover = -1;
        private int listScroll = 0;
        private int numItemsInList = 0;
        private int largeStepSize = 1;
        private bool draggingScrollbars;
        private int captureScrollBarPos;
        private int captureMouseY;
        private int maxListScroll = 0;

        private int margin         = DpiScaling.ScaleForMainWindow(4);
        private int scrollBarWidth = DpiScaling.ScaleForMainWindow(10);
        private int rowHeight      = DpiScaling.ScaleForMainWindow(24);

        public DropDown2(string[] list, int index, bool trans = false)
        {
            items = list;
            selectedIndex = index;
            height = rowHeight;
            transparent = trans;
            UpdateScrollParams();
        }

        public string Text => selectedIndex >= 0 && selectedIndex < items.Length ? items[selectedIndex] : null;

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set 
            {
                if (value != selectedIndex)
                {
                    selectedIndex = value; 
                    SelectedIndexChanged?.Invoke(this, value); 
                    MarkDirty();
                }
            }
        }

        public void SetRowHeight(int h)
        {
            rowHeight = h;
        }

        public void SetItems(string[] newItems)
        {
            items = newItems;
            if (selectedIndex >= items.Length)
                selectedIndex = 0;
            MarkDirty();
            UpdateScrollParams();
        }

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            bmpArrow = g.GetBitmapAtlasRef("DropDownArrow");
        }

        protected override void OnMouseDown(MouseEventArgs2 e)
        {
            MarkDirty();

            if (enabled && e.Left)
            {
                if (listOpened && e.Y > rowHeight)
                {
                    if (GetScrollBarParams(out var scrollBarPos, out var scrollBarSize) && e.X > width - scrollBarWidth)
                    {
                        var y = e.Y - rowHeight;

                        if (y < scrollBarPos)
                        {
                            listScroll = Math.Max(0, listScroll - largeStepSize);
                        }
                        else if (y > (scrollBarPos + scrollBarSize))
                        {
                            listScroll = Math.Min(maxListScroll, listScroll + largeStepSize);
                        }
                        else
                        {
                            Capture = true;
                            draggingScrollbars = true;
                            captureScrollBarPos = scrollBarPos;
                            captureMouseY = e.Y;
                        }

                        return;
                    }

                    SelectedIndex = listScroll + (e.Y - rowHeight) / rowHeight;
                }

                SetListOpened(!listOpened);
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

        protected override void OnKeyDown(KeyEventArgs2 e)
        {
            if (e.Key == Keys2.Escape)
            {
                SetListOpened(false);
                ClearDialogFocus();
                e.Handled = true;
            }
        }

        public void SetListOpened(bool open)
        {
            if (open != listOpened)
            {
                if (listOpened)
                    ListClosing?.Invoke(this);
                listOpened = open;
            }

            height = rowHeight + (listOpened ? Math.Min(items.Length, MaxItemsInList) * rowHeight : 0);
            listScroll = Math.Min(selectedIndex, maxListScroll);
        }

        private void UpdateScrollParams()
        {
            largeStepSize = Math.Min(4, items.Length / 20); 
            maxListScroll = Math.Max(0, items.Length - MaxItemsInList);
            numItemsInList = Math.Min(items.Length, MaxItemsInList);
        }

        private void UpdateListHover(MouseEventArgs2 e)
        {
            if (listOpened && e.X < width - scrollBarWidth)
                SetAndMarkDirty(ref listHover, listScroll + (e.Y - rowHeight) / rowHeight);
        }

        protected override void OnMouseMove(MouseEventArgs2 e)
        {
            if (draggingScrollbars)
            {
                GetScrollBarParams(out var scrollBarPos, out var scrollBarSize);
                var newScrollBarPos = captureScrollBarPos + (e.Y - captureMouseY);
                var ratio = newScrollBarPos / (float)(numItemsInList * rowHeight - scrollBarSize);
                var newListScroll = Utils.Clamp((int)Math.Round(ratio * maxListScroll), 0, maxListScroll);
                SetAndMarkDirty(ref listScroll, newListScroll);
            }
            else
            {
                SetAndMarkDirty(ref hover, e.Y < rowHeight && !listOpened);
                UpdateListHover(e);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            SetAndMarkDirty(ref hover, false);
            SetAndMarkDirty(ref listHover, -1);
        }

        protected override void OnLostDialogFocus()
        {
            SetListOpened(false);
        }

        private bool GetScrollBarParams(out int pos, out int size)
        {
            if (items.Length > MaxItemsInList)
            {
                var scrollAreaSize = numItemsInList * rowHeight;
                var minScrollBarSizeY = scrollAreaSize / 4;
                var scrollY = listScroll * rowHeight;
                var maxScrollY = maxListScroll * rowHeight;

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

            if (!enabled || sign == 0)
                return;

            if (listOpened)
            {
                SetAndMarkDirty(ref listScroll, Utils.Clamp(listScroll + sign * largeStepSize, 0, maxListScroll));
                UpdateListHover(e);
            }
            else
            {
                SelectedIndex = Utils.Clamp(selectedIndex + sign, 0, items.Length - 1);
            }
        }

        protected override void OnRender(RenderGraphics g)
        {
            var bmpSize = bmpArrow.ElementSize;
            var cb = parentDialog.CommandList;
            var brush = enabled ? ThemeResources.LightGreyFillBrush1 : ThemeResources.MediumGreyFillBrush1;

            if (!transparent)
                cb.FillAndDrawRectangle(0, 0, width - 1, rowHeight - (listOpened ? 0 : 1), hover && enabled ? ThemeResources.DarkGreyLineBrush3 : ThemeResources.DarkGreyLineBrush1, brush);
            
            cb.DrawBitmapAtlas(bmpArrow, width - bmpSize.Width - margin, (rowHeight - bmpSize.Height) / 2, 1, 1, hover && enabled ? Theme.LightGreyFillColor2 : brush.Color0);

            if (selectedIndex >= 0)
                cb.DrawText(items[selectedIndex], ThemeResources.FontMedium, margin, 0, brush, RenderTextFlags.MiddleLeft, 0, rowHeight);

            if (listOpened)
            {
                var cf = parentDialog.CommandListForeground;
                var hasScrollBar = GetScrollBarParams(out var scrollBarPos, out var scrollBarSize);
                var actualScrollBarWidth = hasScrollBar ? scrollBarWidth : 0;

                cf.PushTranslation(0, rowHeight);
                cf.FillAndDrawRectangle(0, 0, width - 1, numItemsInList * rowHeight - 1, ThemeResources.DarkGreyLineBrush1, ThemeResources.LightGreyFillBrush1);

                for (int i = 0; i < numItemsInList; i++)
                {
                    var absItemIndex = i + listScroll;
                    if (absItemIndex == selectedIndex || absItemIndex == listHover)
                        cf.FillRectangle(0, i * rowHeight, width, (i + 1) * rowHeight, absItemIndex == selectedIndex ? ThemeResources.DarkGreyFillBrush1 : ThemeResources.DarkGreyLineBrush3);
                    cf.DrawText(items[absItemIndex], ThemeResources.FontMedium, margin, i * rowHeight, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft | RenderTextFlags.Clip, width - margin - actualScrollBarWidth, rowHeight);
                }

                if (hasScrollBar)
                {
                    cf.FillAndDrawRectangle(width - scrollBarWidth, 0, width - 1, MaxItemsInList * rowHeight - 1, ThemeResources.DarkGreyFillBrush1, ThemeResources.LightGreyFillBrush1);
                    cf.FillAndDrawRectangle(width - scrollBarWidth, scrollBarPos, width - 1, scrollBarPos + scrollBarSize - 1, ThemeResources.MediumGreyFillBrush1, ThemeResources.LightGreyFillBrush1);
                }

                cf.PopTransform();
            }
        }
    }
}
