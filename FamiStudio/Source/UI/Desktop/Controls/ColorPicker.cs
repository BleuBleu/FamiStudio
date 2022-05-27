using System.Drawing;
using System.Collections.Generic;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush = FamiStudio.GLBrush;
using RenderGeometry = FamiStudio.GLGeometry;
using RenderControl = FamiStudio.GLControl;
using RenderGraphics = FamiStudio.GLGraphics;
using RenderCommandList = FamiStudio.GLCommandList;
using System.Windows.Forms;
using System;

namespace FamiStudio
{
    public class ColorPicker2 : RenderControl
    {
        public delegate void ColorChangedDelegate(RenderControl sender, Color color);
        public delegate void DoubleClickDelegate(RenderControl sender);

        public event ColorChangedDelegate ColorChanged;
        public event DoubleClickDelegate  DoubleClicked;

        private Color selectedColor;
        public Color SelectedColor => selectedColor;

        public ColorPicker2(Color color)
        {
            selectedColor = color;
        }

        public void SetNiceSize(int width)
        {
            var numColorsX = Theme.CustomColors.GetLength(0);
            var numColorsY = Theme.CustomColors.GetLength(1);

            Resize(width, numColorsY * width / numColorsX);
        }

        protected override void OnMouseDown(MouseEventArgsEx e)
        {
            ChangeColor(e.X, e.Y);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
                ChangeColor(e.X, e.Y);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                ChangeColor(e.X, e.Y);
                DoubleClicked?.Invoke(this);
            }
        }

        private void ChangeColor(int x, int y)
        {
            var colorRectSizeX = width  / (float)Theme.CustomColors.GetLength(0);
            var colorRectSizeY = height / (float)Theme.CustomColors.GetLength(1);

            var colorIndexX = Utils.Clamp((int)(x / colorRectSizeX), 0, Theme.CustomColors.GetLength(0) - 1);
            var colorIndexY = Utils.Clamp((int)(y / colorRectSizeY), 0, Theme.CustomColors.GetLength(1) - 1);

            var newColor = Theme.CustomColors[colorIndexX, colorIndexY];
            if (newColor != selectedColor)
            {
                selectedColor = newColor;
                ColorChanged?.Invoke(this, selectedColor);
            }
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = parentDialog.CommandList;

            var numColorsX = Theme.CustomColors.GetLength(0);
            var numColorsY = Theme.CustomColors.GetLength(1);

            for (var i = 0; i < Theme.CustomColors.GetLength(0); i++)
            {
                for (var j = 0; j < Theme.CustomColors.GetLength(1); j++)
                {
                    var brush = ThemeResources.CustomColorBrushes[Theme.CustomColors[i, j]];

                    var x0 = (int)((i + 0) * width  / (float)numColorsX);
                    var y0 = (int)((j + 0) * height / (float)numColorsY);
                    var x1 = (int)((i + 1) * width  / (float)numColorsX);
                    var y1 = (int)((j + 1) * height / (float)numColorsY);

                    c.FillRectangle(x0, y0, x1, y1, brush);
                }
            }

            c.DrawRectangle(0, 0, width - 1, height - 1, ThemeResources.BlackBrush);
        }
    }
}
