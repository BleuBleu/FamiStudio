using System.Drawing;
using System.Collections.Generic;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;
using System.Windows.Forms;
using System;

namespace FamiStudio
{
    public class DropDown2 : RenderControl
    {
        private const int MaxItemsInList = 10;

        public delegate void SelectedIndexChangedDelegate(RenderControl sender, int index);
        public event SelectedIndexChangedDelegate SelectedIndexChanged;

        private GLBitmapAtlasRef bmpArrow;
        private string[] items;
        private int selectedIndex = 0;
        private bool listOpened = false;
        private int listScroll = 0;
        private int listHoveredItemIndex = -1;

        private int maxListScroll = 0;
        private int margin = DpiScaling.ScaleForMainWindow(4);
        private int scrollBarWidth = DpiScaling.ScaleForMainWindow(10);
        private int defaultHeight = DpiScaling.ScaleForMainWindow(24);

        public DropDown2(string[] list, int index)
        {
            items = list;
            selectedIndex = index;
            maxListScroll = Math.Max(0, items.Length - MaxItemsInList);
            height = defaultHeight;
        }

        public override bool PriorityInput => listOpened;

        public string Text => items[selectedIndex];

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

        public void SetItems(string[] newItems)
        {
            items = newItems;
            if (selectedIndex >= items.Length)
                selectedIndex = 0;
            MarkDirty();
        }

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            bmpArrow = g.GetBitmapAtlasRef("DropDownArrow");
        }

        protected override void OnMouseDown(MouseEventArgsEx e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                SetListOpened(!listOpened);
            }
            //hovered = true;
            MarkDirty();
        }

        private void SetListOpened(bool open)
        {
            listOpened = open;
            height = defaultHeight + (listOpened ? Math.Min(items.Length, MaxItemsInList) * defaultHeight : 0);
        }

        //protected override void OnMouseUp(MouseEventArgs e)
        //{
        //    if (e.Button.HasFlag(MouseButtons.Left))
        //    {
        //        pressed = false;
        //        Click?.Invoke(this);
        //    }
        //    MarkDirty();
        //}

        //protected override void OnMouseMove(MouseEventArgs e)
        //{
        //    hovered = true;
        //    MarkDirty();
        //}

        //protected override void OnMouseLeave(EventArgs e)
        //{
        //    hovered = false;
        //    pressed = false;
        //    MarkDirty();
        //}

        private bool GetScrollBarParams(out int pos, out int size)
        {
            if (items.Length > MaxItemsInList)
            {
                var scrollAreaSize = MaxItemsInList * defaultHeight;
                var minScrollBarSizeY = scrollAreaSize / 4;
                var scrollY = listScroll * defaultHeight;
                var maxScrollY = maxListScroll * defaultHeight;

                size = Math.Max(minScrollBarSizeY, (int)Math.Round(scrollAreaSize * Math.Min(1.0f, scrollAreaSize / (float)(maxScrollY + scrollAreaSize))));
                pos  = (int)Math.Round((scrollAreaSize - size) * (scrollY / (float)maxScrollY));

                return true;
            }

            pos  = 0;
            size = 0;
            return false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var sign = e.Delta < 0 ? 1 : -1;

            if (sign == 0)
                return;

            if (listOpened)
            {
                var stepSize = Math.Min(4, items.Length / 20);
                listScroll = Utils.Clamp(listScroll + sign * stepSize, 0, maxListScroll);
                MarkDirty();
            }
            else
            {
                SelectedIndex = Utils.Clamp(selectedIndex + sign, 0, items.Length - 1);
            }
        }

        protected override void OnRender(RenderGraphics g)
        {
            var bmpSize = bmpArrow.ElementSize;
            var cb = g.CreateCommandList(GLGraphicsBase.CommandListUsage.Dialog);

            cb.FillAndDrawRectangle(0, 0, width - 1, defaultHeight - (listOpened ? 0 : 1), ThemeResources.DarkGreyLineBrush1, ThemeResources.LightGreyFillBrush1);
            cb.DrawBitmapAtlas(bmpArrow, width - bmpSize.Width - margin, (defaultHeight - bmpSize.Height) / 2, 1, 1, Theme.LightGreyFillColor1);

            if (selectedIndex >= 0)
            {
                cb.DrawText(items[selectedIndex], ThemeResources.FontMedium, margin, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft, 0, defaultHeight);
            }

            if (listOpened)
            {
                var cf = g.CreateCommandList(GLGraphicsBase.CommandListUsage.DialogForeground);
                var numItems = Math.Min(items.Length, MaxItemsInList);
                var hasScrollBar = GetScrollBarParams(out var scrollBarPos, out var scrollBarSize);
                var actualScrollBarWidth = hasScrollBar ? scrollBarWidth : 0;

                cf.PushTranslation(0, defaultHeight);
                cf.FillAndDrawRectangle(0, 0, width - 1, numItems * defaultHeight - 1, ThemeResources.DarkGreyLineBrush1, ThemeResources.LightGreyFillBrush1);

                for (int i = 0; i < numItems; i++)
                {
                    cf.DrawText(items[i + listScroll], ThemeResources.FontMedium, margin, i * defaultHeight, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft | RenderTextFlags.Clip, width - margin - actualScrollBarWidth, defaultHeight);
                }

                if (hasScrollBar)
                {
                    cf.FillAndDrawRectangle(width - scrollBarWidth, 0, width - 1, MaxItemsInList * defaultHeight - 1, ThemeResources.DarkGreyFillBrush1, ThemeResources.LightGreyFillBrush1);
                    cf.FillAndDrawRectangle(width - scrollBarWidth, scrollBarPos, width - 1, scrollBarPos + scrollBarSize - 1, ThemeResources.MediumGreyFillBrush1, ThemeResources.LightGreyFillBrush1);
                }

                cf.PopTransform();
            }
        }
    }
}
