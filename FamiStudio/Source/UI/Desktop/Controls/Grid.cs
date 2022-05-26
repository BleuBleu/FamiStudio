using System;
using System.Drawing;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;
using System.Diagnostics;

namespace FamiStudio
{
    public class Grid2 : RenderControl
    {
        private int scroll;
        private int hoverRow = -1;
        private int hoverCol = -1;
        private int numRows;
        private bool header;
        private object[,] data;
        private int[] columnWidths;
        private int[] columnOffsets;
        private bool hasAnyDropDowns;
        private ColumnDesc[] columns;
        private DropDown2 dropDownInactive;
        private DropDown2 dropDownActive;

        private int margin         = DpiScaling.ScaleForMainWindow(4);
        private int scrollBarWidth = DpiScaling.ScaleForMainWindow(10);
        private int rowHeight      = DpiScaling.ScaleForMainWindow(20);
        private int checkBoxWidth  = DpiScaling.ScaleForMainWindow(24);

        public int ItemCount => data.GetLength(0);

        public Grid2(ColumnDesc[] columnDescs, int h, bool hasHeader = true)
        {
            columns = columnDescs;
            numRows = h / rowHeight;
            height = ScaleForMainWindow(numRows * rowHeight);
            header = hasHeader;

            foreach (var col in columnDescs)
            {
                if (col.Type == ColumnType.DropDown)
                {
                    hasAnyDropDowns = true;
                    break;
                }
            }

            if (hasAnyDropDowns)
            {
                dropDownInactive = new DropDown2(new[] { "" }, 0, true);
                dropDownActive   = new DropDown2(new[] { "" }, 0, true);
                dropDownInactive.Visible = false;
                dropDownActive.Visible   = false;
            }
        }

        public void UpdateData(object[,] newData)
        {
            data = newData;
            Debug.Assert(data.GetLength(1) == columns.Length);

            if (parentDialog != null)
                UpdateLayout();
        }

        public object GetData(int row, int col)
        {
            return data[row, col];
        }

        private void UpdateLayout()
        {
            var actualScrollBarWidth = data != null && data.GetLength(1) > numRows ? scrollBarWidth : 0;
            var actualWidth = width - actualScrollBarWidth;
            var totalWidth = 0;

            columnWidths = new int[columns.Length];
            columnOffsets = new int[columns.Length + 1];

            for (int i = 0; i < columns.Length - 1; i++)
            {
                var col = columns[i];
                var colWidth = col.Type == ColumnType.CheckBox ? checkBoxWidth : (int)Math.Round(col.Width * actualWidth); ;

                columnWidths[i] = colWidth;
                columnOffsets[i] = totalWidth;
                totalWidth += colWidth;
            }

            columnWidths[columns.Length - 1] = actualWidth - totalWidth;
            columnOffsets[columns.Length - 1] = totalWidth;
            columnOffsets[columns.Length]     = width - 1;
        }

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            UpdateLayout();

            if (hasAnyDropDowns)
            {
                parentDialog.AddControl(dropDownInactive);
                parentDialog.AddControl(dropDownActive);
            }
        }

        private bool PixelToCell(int x, int y, out int row, out int col)
        {
            row = -1;
            col = -1;

            if (x < 0 || x > width || y < 0 || y > height)
                return false;

            row = y / rowHeight;

            for (int i = 1; i < columnOffsets.Length; i++)
            {
                if (x <= columnOffsets[i])
                {
                    col = i - 1;
                    break;
                }
            }

            Debug.Assert(col >= 0);
            return true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            PixelToCell(e.X, e.Y, out var row, out var col);
            SetAndMarkDirty(ref hoverRow, row);
            SetAndMarkDirty(ref hoverCol, col);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            SetAndMarkDirty(ref hoverRow, -1);
            SetAndMarkDirty(ref hoverCol, -1);
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList(GLGraphicsBase.CommandListUsage.Dialog);

            // BG
            c.FillRectangle(0, 0, width, height, ThemeResources.DarkGreyLineBrush1);

            // Grid lines
            for (var i = 0; i <= data.GetLength(0) + (header ? 1 : 0); i++)
                c.DrawLine(0, i * rowHeight, width, i * rowHeight, ThemeResources.BlackBrush);
            for (var j = 0; j <= data.GetLength(1); j++)
                c.DrawLine(columnOffsets[j], 0, columnOffsets[j], height, ThemeResources.BlackBrush);

            var baseY = 0;

            // Header
            if (header)
            {
                c.FillRectangle(0, 0, width, rowHeight, ThemeResources.DarkGreyLineBrush3);
                for (var j = 0; j < data.GetLength(1); j++) 
                    c.DrawText(columns[j].Name, ThemeResources.FontMedium, columnOffsets[j] + margin, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft, 0, rowHeight);
                baseY = rowHeight;
            }

            // Data
            if (data != null)
            {
                for (var i = 0; i < data.GetLength(0); i++) // Rows
                {
                    var y = baseY + i * rowHeight;

                    for (var j = 0; j < data.GetLength(1); j++) // Colums
                    {
                        var col = columns[j];
                        var colWidth = columnWidths[j];
                        var x = columnOffsets[j];

                        c.PushTranslation(x, y);

                        switch (col.Type)
                        {
                            case ColumnType.DropDown:
                                dropDownInactive.Visible = true;
                                dropDownInactive.Move(left + x, top + y, columnWidths[j], rowHeight);
                                dropDownInactive.Render(g);
                                dropDownInactive.Visible = false;
                                break;
                            case ColumnType.Slider:
                                c.FillRectangle(0, 0, (int)Math.Round((int)data[i, j] / 100.0f * colWidth), rowHeight, ThemeResources.DarkGreyFillBrush3);
                                c.DrawText(string.Format(CultureInfo.InvariantCulture, col.StringFormat, (int)data[i, j]), ThemeResources.FontMedium, 0, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleCenter, colWidth, rowHeight);
                                break;
                            case ColumnType.Label:
                                c.DrawText((string)data[i, j], ThemeResources.FontMedium, margin, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft, 0, rowHeight);
                                break;
                        }

                        c.PopTransform();
                    }
                }
            }
        }
    }
}
