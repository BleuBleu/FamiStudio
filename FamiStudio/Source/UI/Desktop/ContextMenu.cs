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
        const int DefaultItemSizeY    = 21;
        const int DefaultIconPos      = 4;
        const int DefaultTextPosX     = 20;
        const int DefaultMenuMinSizeX = 100;

        int itemSizeY;
        int iconPos;
        int textPosX;
        int minSizeX;

        // MATTT
        enum ContextMenuIconsIndices
        {
            Drag,
            MouseLeft,
            MouseRight,
            MouseWheel,
            Warning,
            Count
        };

        readonly string[] ContextMenuIconsNames = new string[]
        {
            "Drag",
            "MouseLeft",
            "MouseRight",
            "MouseWheel",
            "Warning"
        };

        RenderBitmapAtlas contextMenuIconsAtlas;
        ContextMenuOption[] menuOptions;

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            Debug.Assert((int)ContextMenuIconsIndices.Count == ContextMenuIconsNames.Length);
            contextMenuIconsAtlas = g.CreateBitmapAtlasFromResources(ContextMenuIconsNames);
        }

        protected override void OnRenderTerminated()
        {
            Utils.DisposeAndNullify(ref contextMenuIconsAtlas);
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
            }

            width  = Math.Max(minSizeX, sizeX + textPosX); 
            height = sizeY;
        }

        public void Tick(float delta)
        {
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

                if (option.Separator) 
                    c.DrawLine(0, 0, Width, 0, ThemeResources.LightGreyFillBrush1);

                var iconIndex = Array.IndexOf(ContextMenuIconsNames, option.Image);
                var iconSize = contextMenuIconsAtlas.GetElementSize(iconIndex);

                c.DrawBitmapAtlas(contextMenuIconsAtlas, iconIndex, iconPos, iconPos);
                c.DrawText(option.Text, ThemeResources.FontMedium, textPosX, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft, Width, itemSizeY);
                c.PopTransform();
            }

            g.Clear(Theme.DarkGreyFillColor1);
            g.DrawCommandList(c);
        }
    }
}
