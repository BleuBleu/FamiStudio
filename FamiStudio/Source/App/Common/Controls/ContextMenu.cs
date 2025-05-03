using System;
using System.Diagnostics;

namespace FamiStudio
{
    // TODO : This should simply be a list of buttons.
    public class ContextMenu : Container
    {
        private int margin    = DpiScaling.ScaleForWindow(3);
        private int minSizeX  = DpiScaling.ScaleForWindow(Platform.IsDesktop ? 100 : 1);

        private int iconSizeX;
        private int itemSizeY;
        private int hoveredItemIndex = -1;
        private TextureAtlasRef[] bmpContextMenu;
        private TextureAtlasRef bmpMenuCheckOn;
        private TextureAtlasRef bmpMenuCheckOff;
        private TextureAtlasRef bmpMenuRadio;
        private ContextMenuOption[] menuOptions;

        private const ContextMenuSeparator flagBefore = Platform.IsMobile ? ContextMenuSeparator.MobileBefore : ContextMenuSeparator.Before;
        private const ContextMenuSeparator flagAfter  = Platform.IsMobile ? ContextMenuSeparator.MobileAfter  : ContextMenuSeparator.After;

        public ContextMenu()
        {
            visible = false;
            clipRegion = Platform.IsDesktop;
        }

        protected override void OnAddedToContainer()
        {
            var g = ParentWindow.Graphics;

            bmpMenuCheckOn  = g.GetTextureAtlasRef("MenuCheckOn");
            bmpMenuCheckOff = g.GetTextureAtlasRef("MenuCheckOff");
            bmpMenuRadio    = g.GetTextureAtlasRef("MenuRadio");
            
            UpdateLayout();
        }

        public void Initialize(ContextMenuOption[] options)
        {
            menuOptions = options;

            if (HasParent)
            {
                UpdateLayout();
            }
        }

        private void UpdateLayout()
        {
            if (menuOptions != null)
            {
                // Measure size.
                var g = ParentWindow.Graphics;
                var textSizeX = 0;
                iconSizeX = 0;

                bmpContextMenu = new TextureAtlasRef[menuOptions.Length];

                for (var i = 0; i < menuOptions.Length; i++)
                {
                    ContextMenuOption option = menuOptions[i];

                    textSizeX = Math.Max(textSizeX, (int)ParentWindow.Graphics.MeasureString(option.Text, Fonts.FontMedium));

                    if (!string.IsNullOrEmpty(option.Image))
                    {
                        var img = g.GetTextureAtlasRef(option.Image);
                        Debug.Assert(img != null);
                        bmpContextMenu[i] = img;
                        iconSizeX = Math.Max(img.ElementSize.Width, iconSizeX);
                    }
                    else if (option.CheckState != null)
                    {
                        iconSizeX = Math.Max(bmpMenuCheckOn.ElementSize.Width, iconSizeX);
                    }
                }


                if (iconSizeX > 0)
                {
                    iconSizeX += margin;
                    textSizeX += margin; // We do this since images tend to have borders in the image.
                    itemSizeY = iconSizeX + margin * 2; // We assume square icons.
                }
                else
                {
                    itemSizeY = DpiScaling.ScaleForWindow(22);
                }

                width = Math.Max(minSizeX, margin + iconSizeX + textSizeX + margin);
                height = menuOptions.Length * itemSizeY;
            }
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

        private void ActivateItem(int index)
        {
            SetHoveredItemIndex(-1); // Make sure previous index isn't highlighted on next menu open.
            App.HideContextMenu();
            MarkDirty();
            Platform.VibrateTick();
            menuOptions[index].Callback();
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            var itemIndex = GetIndexAtCoord(e.X, e.Y);

            SetHoveredItemIndex(itemIndex);

            if (hoveredItemIndex >= 0 && !e.IsTouchEvent)
            {
                ActivateItem(hoveredItemIndex);
            }
        }

        protected override void OnPointerMove(PointerEventArgs e)
        {
            var itemIndex = GetIndexAtCoord(e.X, e.Y);
            SetHoveredItemIndex(itemIndex);
        }

        protected override void OnPointerLeave(EventArgs e)
        {
            if (ParentWindow != null && visible)
            { 
                SetHoveredItemIndex(-1);
            }
        }

        protected override void OnTouchClick(PointerEventArgs e)
        {
            var itemIndex = GetIndexAtCoord(e.X, e.Y);
            if (itemIndex >= 0)
            {
                ActivateItem(itemIndex);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Keys.Escape)
            {
                SetHoveredItemIndex(-1);
                App.HideContextMenu();
            }
            else 
            {
                if (hoveredItemIndex >= 0 && (e.Key == Keys.Enter || e.Key == Keys.KeypadEnter))
                {
                    ActivateItem(hoveredItemIndex);
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
            if (idx != hoveredItemIndex && Platform.IsDesktop)
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

        protected override void OnRender(Graphics g)
        {
            Debug.Assert(menuOptions != null && menuOptions.Length > 0);

            var c = g.TopMostCommandList;
            var prevWantedSeparator = false;

            c.FillAndDrawRectangle(0, 0, Width - 1, Height - 1, Theme.DarkGreyColor2, Platform.IsMobile ? Theme.BlackColor : Theme.LightGreyColor1);

            for (int i = 0, y = 0; i < menuOptions.Length; i++, y += itemSizeY)
            {
                ContextMenuOption option = menuOptions[i];

                c.PushTranslation(0, y);

                var hover = i == hoveredItemIndex;

                if (hover)
                    c.FillRectangle(0, 0, Width, itemSizeY, Theme.MediumGreyColor1);

                if (i > 0 && (option.Separator.HasFlag(flagBefore) || prevWantedSeparator))
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
                    c.DrawTextureAtlasCentered(bmp, margin, 0, bmp.ElementSize.Width, itemSizeY, 1, hover ? Theme.LightGreyColor2 : Theme.LightGreyColor1);
                }

                c.DrawText(option.Text, Fonts.FontMedium, margin + iconSizeX, 0, hover ? Theme.LightGreyColor2 : Theme.LightGreyColor1, TextFlags.MiddleLeft, Width, itemSizeY);
                c.PopTransform();

                prevWantedSeparator = option.Separator.HasFlag(flagAfter);
            }
        }
    }
}
