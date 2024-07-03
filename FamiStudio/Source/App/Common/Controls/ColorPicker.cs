using System.Diagnostics;

namespace FamiStudio
{
    public class ColorPicker : Control
    {
        public delegate void ColorChangedDelegate(Control sender, Color color);
        public delegate void DoubleClickDelegate(Control sender);

        public event ColorChangedDelegate ColorChanged;
        public event DoubleClickDelegate  DoubleClicked;

        private Color selectedColor;
        public Color SelectedColor => selectedColor;

        public ColorPicker(Color color)
        {
            selectedColor = color;
        }

        public void SetNiceSize(int width)
        {
            var numColorsX = Theme.CustomColors.GetLength(0);
            var numColorsY = Theme.CustomColors.GetLength(1);

            Resize(width, numColorsY * width / numColorsX);
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            if (e.Left)
            {
                ChangeColor(e.X, e.Y);
                e.MarkHandled();
            }
        }

        protected override void OnPointerMove(PointerEventArgs e)
        {
            if (e.Left)
            {
                ChangeColor(e.X, e.Y);
                e.MarkHandled();
            }
        }

        protected override void OnMouseDoubleClick(PointerEventArgs e)
        {
            if (e.Left)
            {
                ChangeColor(e.X, e.Y);
                DoubleClicked?.Invoke(this);
                e.MarkHandled();
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

        protected override void OnRender(Graphics g)
        {
            Debug.Assert(enabled); // TODO : Add support for disabled state.

            var c = g.GetCommandList();

            var numColorsX = Theme.CustomColors.GetLength(0);
            var numColorsY = Theme.CustomColors.GetLength(1);

            for (var i = 0; i < Theme.CustomColors.GetLength(0); i++)
            {
                for (var j = 0; j < Theme.CustomColors.GetLength(1); j++)
                {
                    var x0 = (int)((i + 0) * width  / (float)numColorsX);
                    var y0 = (int)((j + 0) * height / (float)numColorsY);
                    var x1 = (int)((i + 1) * width  / (float)numColorsX);
                    var y1 = (int)((j + 1) * height / (float)numColorsY);

                    c.FillRectangle(x0, y0, x1, y1, Theme.CustomColors[i, j]);
                }
            }

            c.DrawRectangle(0, 0, width - 1, height - 1, Theme.BlackColor);
        }
    }
}
