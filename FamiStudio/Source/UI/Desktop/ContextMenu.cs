using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

using Color     = System.Drawing.Color;
using Point     = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

using RenderBitmapAtlas = FamiStudio.GLBitmapAtlas;
using RenderBrush       = FamiStudio.GLBrush;
using RenderFont        = FamiStudio.GLFont;
using RenderControl     = FamiStudio.GLControl;
using RenderGraphics    = FamiStudio.GLGraphics;
using RenderCommandList = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class ContextMenu : RenderControl
    {
        const int DefaultItemSizeY    = 22;
        const int DefaultIconPos      = 4;
        const int DefaultTextPosX     = 22;
        const int DefaultMenuMinSizeX = 100;

        int itemSizeY;
        int iconPos;
        int textPosX;
        int minSizeX;

        // TODO : This is super lame, find a way to build the atlas
        // on the fly as we get more and more images.
        readonly string[] ContextMenuIconsNames = new string[]
        {
            "MenuCheckOff",
            "MenuCheckOn",
            "MenuClearEnvelope",
            "MenuClearSelection",
            "MenuClearLoopPoint",
            "MenuClearRelease",
            "MenuCustomPatternSettings",
            "MenuDelete",
            "MenuDeleteSelection",
            "MenuEyedropper",
            "MenuForceDisplay",
            "MenuLoopPoint",
            "MenuMute", 
            "MenuProperties",
            "MenuRadio",
            "MenuRelease",
            "MenuSelectAll",
            "MenuSelectNote",
            "MenuSelectPattern",
            "MenuSolo", 
            "MenuStopNote",
            "MenuToggleAttack",
            "MenuToggleRelease",
            "MenuToggleSlide",
        };

        int hoveredItemIndex = -1;
        RenderBitmapAtlas contextMenuIconsAtlas;
        RenderBitmapAtlas expansionAtlas;
        ContextMenuOption[] menuOptions;

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            contextMenuIconsAtlas = g.CreateBitmapAtlasFromResources(ContextMenuIconsNames);
            expansionAtlas = g.CreateBitmapAtlasFromResources(ExpansionType.Icons);
        }

        protected override void OnRenderTerminated()
        {
            Utils.DisposeAndNullify(ref contextMenuIconsAtlas);
            Utils.DisposeAndNullify(ref expansionAtlas);
        }

        private void UpdateRenderCoords()
        {
            itemSizeY = ScaleForMainWindow(DefaultItemSizeY);
            iconPos   = ScaleForMainWindow(DefaultIconPos);
            textPosX  = ScaleForMainWindow(DefaultTextPosX);
            minSizeX  = ScaleForMainWindow(DefaultMenuMinSizeX);
        }

        public void Initialize(RenderGraphics g, ContextMenuOption[] options)
        {
            UpdateRenderCoords();

            menuOptions = options;

            // Measure size.
            var sizeX = 0;
            var sizeY = 0;

            // MATTT : right marging + shortcut.
            for (int i = 0; i < menuOptions.Length; i++)
            {
                ContextMenuOption option = menuOptions[i];

                sizeX = Math.Max(sizeX, (int)g.MeasureString(option.Text, ThemeResources.FontMedium));
                sizeY += itemSizeY;

                Debug.Assert(string.IsNullOrEmpty(option.Image) || GetAtlasAndIndex(option.Image, out _) >= 0);
            }

            width  = Math.Max(minSizeX, sizeX + textPosX) + iconPos; 
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

        // MATTT : Shortcut support too, display in toolbar!

        protected override void OnMouseDown(MouseEventArgsEx e)
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
            SetHoveredItemIndex(-1);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                App.HideContextMenu();
            }
            else if (hoveredItemIndex >= 0)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    App.HideContextMenu();
                    MarkDirty();
                    menuOptions[hoveredItemIndex].Callback();
                }
                else if (e.KeyCode == Keys.Up)
                {
                    SetHoveredItemIndex(Math.Max(0, hoveredItemIndex - 1));
                }
                else if (e.KeyCode == Keys.Down)
                {
                    SetHoveredItemIndex(Math.Min(menuOptions.Length - 1, hoveredItemIndex + 1));
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

        public void Tick(float delta)
        {
        }

        private int GetAtlasAndIndex(string image, out RenderBitmapAtlas atlas)
        {
            atlas = contextMenuIconsAtlas;
            var idx = Array.IndexOf(ContextMenuIconsNames, image);

            if (idx < 0)
            {
                atlas = expansionAtlas;
                idx = Array.IndexOf(ExpansionType.Icons, image);
            }

            return idx;
        }

        protected override void OnRender(RenderGraphics g)
        {
            Debug.Assert(menuOptions != null && menuOptions.Length > 0);

            var c = g.CreateCommandList();

            c.DrawRectangle(0, 0, Width - 1, Height - 1, ThemeResources.LightGreyFillBrush1);

            for (int i = 0, y = 0; i < menuOptions.Length; i++, y += itemSizeY)
            {
                ContextMenuOption option = menuOptions[i];

                c.PushTranslation(0, y);

                var hover = i == hoveredItemIndex;

                if (hover)
                    c.FillRectangle(0, 0, Width, itemSizeY, ThemeResources.MediumGreyFillBrush1);

                if (option.Separator) 
                    c.DrawLine(0, 0, Width, 0, ThemeResources.LightGreyFillBrush1);

                var imageName = option.Image;

                if (string.IsNullOrEmpty(imageName))
                {
                    var checkState = option.CheckState();
                    switch (checkState)
                    {
                        case ContextMenuCheckState.Checked:   imageName = "MenuCheckOn";  break;
                        case ContextMenuCheckState.Unchecked: imageName = "MenuCheckOff"; break;
                        case ContextMenuCheckState.Radio:     imageName = "MenuRadio";    break;
                    }
                }

                if (!string.IsNullOrEmpty(imageName))
                {
                    var iconIndex = GetAtlasAndIndex(imageName, out var atlas);
                    c.DrawBitmapAtlas(atlas, iconIndex, iconPos, iconPos, 1, 1, hover ? Theme.LightGreyFillColor2 : Theme.LightGreyFillColor1);
                }

                c.DrawText(option.Text, ThemeResources.FontMedium, textPosX, 0, hover ? ThemeResources.LightGreyFillBrush2 : ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft, Width, itemSizeY);
                c.PopTransform();
            }

            g.Clear(Theme.DarkGreyFillColor1);
            g.DrawCommandList(c);
        }
    }
}
