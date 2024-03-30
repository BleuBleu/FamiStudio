using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class ContextMenu : Container
    {
        const int DefaultItemSizeY    = 22;
        const int DefaultIconPos      = 3;
        const int DefaultTextPosX     = 22;
        const int DefaultMenuMinSizeX = 100;

        int itemSizeY;
        int iconPos;
        int textPosX;
        int minSizeX;

        int hoveredItemIndex = -1;
        TextureAtlasRef[] bmpContextMenu;
        TextureAtlasRef bmpMenuCheckOn;
        TextureAtlasRef bmpMenuCheckOff;
        TextureAtlasRef bmpMenuRadio;
        ContextMenuOption[] menuOptions;

        public ContextMenu()
        {
            visible = false;
        }

        protected override void OnAddedToContainer()
        {
            var g = ParentWindow.Graphics;
            bmpMenuCheckOn  = g.GetTextureAtlasRef("MenuCheckOn");
            bmpMenuCheckOff = g.GetTextureAtlasRef("MenuCheckOff");
            bmpMenuRadio    = g.GetTextureAtlasRef("MenuRadio");
        }

        private void UpdateRenderCoords()
        {
            itemSizeY = DpiScaling.ScaleForWindow(DefaultItemSizeY);
            iconPos   = DpiScaling.ScaleForWindow(DefaultIconPos);
            textPosX  = DpiScaling.ScaleForWindow(DefaultTextPosX);
            minSizeX  = DpiScaling.ScaleForWindow(DefaultMenuMinSizeX);
        }

        public void Initialize(ContextMenuOption[] options)
        {
            UpdateRenderCoords();

            menuOptions = options;
            bmpContextMenu = new TextureAtlasRef[options.Length];

            // Measure size.
            var g = ParentWindow.Graphics;
            var sizeX = 0;
            var sizeY = 0;

            for (int i = 0; i < menuOptions.Length; i++)
            {
                ContextMenuOption option = menuOptions[i];

                sizeX = Math.Max(sizeX, (int)ParentWindow.Graphics.MeasureString(option.Text, Fonts.FontMedium));
                sizeY += itemSizeY;

                if (!string.IsNullOrEmpty(option.Image))
                {
                    bmpContextMenu[i] = g.GetTextureAtlasRef(option.Image);
                    Debug.Assert(bmpContextMenu != null);
                }
            }

            width  = Math.Max(minSizeX, sizeX + textPosX) + iconPos * 2; 
            height = sizeY;
        }

        protected int GetIndexAtCoord(int x, int y)
        {
            var idx = -1;

            if (x >= 0 && 
                y >= 0 &&
                x < Width &&
                y < Height)
            {
                idx = Math.Min(y / itemSizeY, menuOptions.Length - 1);;
            }

            return idx;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            var itemIndex = GetIndexAtCoord(e.X, e.Y);

            SetHoveredItemIndex(itemIndex);

            if (hoveredItemIndex >= 0)
            {
                App.HideContextMenu();
                MarkDirty();
                menuOptions[hoveredItemIndex].Callback();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var itemIndex = GetIndexAtCoord(e.X, e.Y);
            SetHoveredItemIndex(itemIndex);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (ParentWindow != null && visible)
            { 
                SetHoveredItemIndex(-1);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Keys.Escape)
            {
                App.HideContextMenu();
            }
            else 
            {
                if (hoveredItemIndex >= 0 && (e.Key == Keys.Enter || e.Key == Keys.KeypadEnter))
                {
                    App.HideContextMenu();
                    MarkDirty();
                    menuOptions[hoveredItemIndex].Callback();
                }
                else if (e.Key == Keys.Up)
                {
                    SetHoveredItemIndex(Math.Clamp(hoveredItemIndex - 1, 0, menuOptions.Length - 1));
                }
                else if (e.Key == Keys.Down)
                {
                    SetHoveredItemIndex(Math.Clamp(hoveredItemIndex + 1, 0, menuOptions.Length - 1));
                }
            }
        }

        protected void SetHoveredItemIndex(int idx)
        {
            if (idx != hoveredItemIndex)
            {
                hoveredItemIndex = idx;
                UpdateTooltip();
                MarkDirty();
            }
        }

        protected void UpdateTooltip()
        {
            App.SetToolTip(hoveredItemIndex >= 0 ? menuOptions[hoveredItemIndex].ToolTip : null);
        }

        public override void Tick(float delta)
        {
        }

        protected override void OnRender(Graphics g)
        {
            Debug.Assert(menuOptions != null && menuOptions.Length > 0);

            var c = g.DefaultCommandList;
            var prevWantedSeparator = false;

            c.DrawRectangle(0, 0, Width - 1, Height - 1, Theme.LightGreyColor1);

            for (int i = 0, y = 0; i < menuOptions.Length; i++, y += itemSizeY)
            {
                ContextMenuOption option = menuOptions[i];

                c.PushTranslation(0, y);

                var hover = i == hoveredItemIndex;

                if (hover)
                    c.FillRectangle(0, 0, Width, itemSizeY, Theme.MediumGreyColor1);

                if (i > 0 && (option.Separator.HasFlag(ContextMenuSeparator.Before) || prevWantedSeparator))
                {
                    c.DrawLine(0, 0, Width, 0, Theme.LightGreyColor1);
                    prevWantedSeparator = false;
                }

                var bmp = bmpContextMenu[i];

                if (bmp == null)
                {
                    var checkState = option.CheckState();
                    switch (checkState)
                    {
                        case ContextMenuCheckState.Checked:   bmp = bmpMenuCheckOn;  break;
                        case ContextMenuCheckState.Unchecked: bmp = bmpMenuCheckOff; break;
                        case ContextMenuCheckState.Radio:     bmp = bmpMenuRadio;    break;
                    }
                }

                if (bmp != null)
                {
                    c.DrawTextureAtlas(bmp, iconPos, iconPos, 1, 1, hover ? Theme.LightGreyColor2 : Theme.LightGreyColor1);
                }

                c.DrawText(option.Text, Fonts.FontMedium, textPosX, 0, hover ? Theme.LightGreyColor2 : Theme.LightGreyColor1, TextFlags.MiddleLeft, Width, itemSizeY);
                c.PopTransform();

                prevWantedSeparator = option.Separator.HasFlag(ContextMenuSeparator.After);
            }
        }
    }
}
