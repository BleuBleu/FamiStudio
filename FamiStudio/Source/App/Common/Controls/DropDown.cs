using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class DropDown : Control
    {
        private const int MaxItemsInList = 10;

        public delegate void SelectedIndexChangedDelegate(Control sender, int index);
        public delegate void ListClosingDelegate(Control sender);

        public event SelectedIndexChangedDelegate SelectedIndexChanged;
        public event ListClosingDelegate ListClosing;

        private TextureAtlasRef bmpArrow;
        private string[] items;
        private int selectedIndex = 0;
        private bool hover;
        private bool listOpened;
        private bool listJustOpened;
        private bool isGridChild; // Emergency hack for 4.0.3
        private bool transparent;
        private int listHover = -1;
        private int listScroll = 0;
        private int numItemsInList = 0;
        private int largeStepSize = 1;
        private bool draggingScrollbars;
        private int captureScrollBarPos;
        private int captureMouseY;
        private int maxListScroll = 0;

        private int margin         = DpiScaling.ScaleForWindow(4);
        private int scrollBarWidth = DpiScaling.ScaleForWindow(10);
        private int rowHeight      = DpiScaling.ScaleForWindow(24);

        public DropDown(string[] list, int index, bool trans = false)
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

        public bool IsGridChild
        {
            get { return isGridChild; }
            set { isGridChild = value; }
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

        protected override void OnAddedToContainer()
        {
            bmpArrow = Graphics.GetTextureAtlasRef("DropDownArrow");
        }

        protected override void OnPointerDown(PointerEventArgs e)
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
                }
            }
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            if (listJustOpened && isGridChild)
            {
                listJustOpened = false;
                return;
            }

            if (draggingScrollbars)
            {
                draggingScrollbars = false;
                Capture = false;
                MarkDirty();
            }
            else if (enabled && e.Left)
            {
                if (listOpened && e.Y > rowHeight)
                {
                    if (GetScrollBarParams(out var scrollBarPos, out var scrollBarSize) && e.X > width - scrollBarWidth)
                    {
                        var y = e.Y - rowHeight;

                        if (y < scrollBarPos)
                        {
                            SetAndMarkDirty(ref listScroll, Math.Max(0, listScroll - largeStepSize));
                        }
                        else if (y > (scrollBarPos + scrollBarSize))
                        {
                            SetAndMarkDirty(ref listScroll, Math.Min(maxListScroll, listScroll + largeStepSize));
                        }
                        return;
                    }

                    SelectedIndex = listScroll + (e.Y - rowHeight) / rowHeight;
                }

                SetListOpened(!listOpened);
            }
        }

        protected override void OnMouseDoubleClick(PointerEventArgs e)
        {
            OnPointerDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Keys.Escape)
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
                listJustOpened = open;

                if (open)
                {
                    GrabDialogFocus();
                    height = rowHeight * numItemsInList;
                }
                else
                {
                    ClearDialogFocus();
                    height = rowHeight;
                }

                MarkDirty();
            }

            height = rowHeight + (listOpened ? Math.Min(items.Length, MaxItemsInList) * rowHeight : 0);
            listScroll = Math.Min(selectedIndex, maxListScroll);
        }

        private void UpdateScrollParams()
        {
            largeStepSize = Utils.Clamp(items.Length / 20, 1, 4); 
            maxListScroll = Math.Max(0, items.Length - MaxItemsInList);
            numItemsInList = Math.Min(items.Length, MaxItemsInList);
        }

        private void UpdateListHover(PointerEventArgs e)
        {
            if (listOpened && e.X < width - scrollBarWidth)
                SetAndMarkDirty(ref listHover, listScroll + (e.Y - rowHeight) / rowHeight);
        }

        protected override void OnPointerMove(PointerEventArgs e)
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

        protected override void OnPointerLeave(EventArgs e)
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

        protected override void OnMouseWheel(PointerEventArgs e)
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

            e.MarkHandled();
        }

        protected override void OnRender(Graphics g)
        {
            var bmpSize = bmpArrow.ElementSize;
            var c = g.DefaultCommandList;
            var color = enabled ? Theme.LightGreyColor1 : Theme.MediumGreyColor1;

            if (!transparent)
                c.FillAndDrawRectangle(0, 0, width - 1, rowHeight - (listOpened ? 0 : 1), hover && enabled ? Theme.DarkGreyColor3 : Theme.DarkGreyColor1, color);
            
            c.DrawTextureAtlas(bmpArrow, width - bmpSize.Width - margin, (rowHeight - bmpSize.Height) / 2, 1, hover && enabled ? Theme.LightGreyColor2 : color);

            if (selectedIndex >= 0)
                c.DrawText(items[selectedIndex], Fonts.FontMedium, margin, 0, color, TextFlags.MiddleLeft, 0, rowHeight);

            if (listOpened)
            {
                var o = g.OverlayCommandList;
                var hasScrollBar = GetScrollBarParams(out var scrollBarPos, out var scrollBarSize);
                var actualScrollBarWidth = hasScrollBar ? scrollBarWidth : 0;

                o.PushTranslation(0, rowHeight);
                o.FillAndDrawRectangle(0, 0, width - 1, numItemsInList * rowHeight - 1, Theme.DarkGreyColor1, Theme.LightGreyColor1);

                for (int i = 0; i < numItemsInList; i++)
                {
                    var absItemIndex = i + listScroll;
                    if (absItemIndex == selectedIndex || absItemIndex == listHover)
                        o.FillRectangle(0, i * rowHeight, width, (i + 1) * rowHeight, absItemIndex == selectedIndex ? Theme.DarkGreyColor4 : Theme.DarkGreyColor3);
                    o.DrawText(items[absItemIndex], Fonts.FontMedium, margin, i * rowHeight, Theme.LightGreyColor1, TextFlags.MiddleLeft | TextFlags.Clip, width - margin - actualScrollBarWidth, rowHeight);
                }

                if (hasScrollBar)
                {
                    o.FillAndDrawRectangle(width - scrollBarWidth, 0, width - 1, MaxItemsInList * rowHeight - 1, Theme.DarkGreyColor4, Theme.LightGreyColor1);
                    o.FillAndDrawRectangle(width - scrollBarWidth, scrollBarPos, width - 1, scrollBarPos + scrollBarSize - 1, Theme.MediumGreyColor1, Theme.LightGreyColor1);
                }

                o.PopTransform();
            }
        }
    }
}
