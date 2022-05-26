using System;
using System.Globalization;
using System.Windows.Forms;
using System.Diagnostics;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public class Grid2 : RenderControl
    {
        public delegate void ValueChangedDelegate(RenderControl sender, int rowIndex, int colIndex, object value);
        public delegate void ButtonPressedDelegate(RenderControl sender, int rowIndex, int colIndex);

        public event ValueChangedDelegate ValueChanged;
        public event ButtonPressedDelegate ButtonPressed;

        private int scroll;
        private int maxScroll;
        private int hoverRow = -1;
        private int hoverCol = -1;
        private bool hoverButton;
        private int dropDownRow = -1;
        private int dropDownCol = -1;
        private int numRows;
        private int numItemRows;
        private int numHeaderRows;
        private object[,] data;
        private int[] columnWidths;
        private int[] columnOffsets;
        private bool hasAnyDropDowns;
        private ColumnDesc[] columns;

        private bool draggingScrollbars;
        private bool draggingSlider;
        private int  captureScrollBarPos;
        private int  captureMouseY;
        private int  sliderCol;
        private int  sliderRow;

        private DropDown2 dropDownInactive;
        private DropDown2 dropDownActive;

        private RenderBitmapAtlasRef bmpCheckOn;
        private RenderBitmapAtlasRef bmpCheckOff;

        private int margin         = DpiScaling.ScaleForMainWindow(4);
        private int scrollBarWidth = DpiScaling.ScaleForMainWindow(10);
        private int rowHeight      = DpiScaling.ScaleForMainWindow(20);
        private int checkBoxWidth  = DpiScaling.ScaleForMainWindow(20);

        public int ItemCount => data.GetLength(0);

        // MATTT : Go back to number of rows.
        public Grid2(ColumnDesc[] columnDescs, int rows, bool hasHeader = true)
        {
            columns = columnDescs;
            numRows = rows;
            numItemRows = hasHeader ? numRows - 1 : numRows;
            numHeaderRows = hasHeader ? 1 : 0;
            height = ScaleForMainWindow(numRows * rowHeight);

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
                dropDownActive = new DropDown2(new[] { "" }, 0);
                dropDownInactive.SetRowHeight(rowHeight);
                dropDownActive.SetRowHeight(rowHeight);
                dropDownInactive.Visible = false;
                dropDownActive.Visible = false;
                dropDownActive.ListClosing += DropDownActive_ListClosing;
                dropDownActive.SelectedIndexChanged += DropDownActive_SelectedIndexChanged;
            }
        }

        private void DropDownActive_SelectedIndexChanged(RenderControl sender, int index)
        {
            if (dropDownRow >= 0 && dropDownCol >= 0 && dropDownActive.Visible)
            {
                data[dropDownRow, dropDownCol] = dropDownActive.Text;
                ValueChanged?.Invoke(this, sliderRow, sliderCol, dropDownActive.Text);
                MarkDirty();
            }
        }

        private void DropDownActive_ListClosing(RenderControl sender)
        {
            Debug.Assert(dropDownActive.Visible);
            dropDownActive.Visible = false;
            dropDownRow = -1;
            dropDownCol = -1;
            MarkDirty();
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
            var actualScrollBarWidth = data != null && data.GetLength(0) > numItemRows ? scrollBarWidth : 0;
            var actualWidth = width - actualScrollBarWidth;
            var totalWidth = 0;

            columnWidths = new int[columns.Length];
            columnOffsets = new int[columns.Length + 1];

            for (int i = 0; i < columns.Length - 1; i++)
            {
                var col = columns[i];
                var colWidth = col.Type == ColumnType.CheckBox ? checkBoxWidth : (int)Math.Round(col.Width * actualWidth);

                columnWidths[i] = colWidth;
                columnOffsets[i] = totalWidth;
                totalWidth += colWidth;
            }

            columnWidths[columns.Length - 1] = actualWidth - totalWidth;
            columnOffsets[columns.Length - 1] = totalWidth;
            columnOffsets[columns.Length] = width - 1;

            maxScroll = data != null ? Math.Max(0, data.GetLength(0) - numItemRows) : 0;
        }

        protected override void OnRenderInitialized(RenderGraphics g)
        {
            UpdateLayout();

            if (hasAnyDropDowns)
            {
                parentDialog.AddControl(dropDownInactive);
                parentDialog.AddControl(dropDownActive);
            }

            bmpCheckOn  = g.GetBitmapAtlasRef("CheckBoxYes");
            bmpCheckOff = g.GetBitmapAtlasRef("CheckBoxNo");
        }

        private bool PixelToCell(int x, int y, out int row, out int col)
        {
            row = -1;
            col = -1;

            var minY = numHeaderRows * rowHeight;
            var maxX = width - (GetScrollBarParams(out _, out _) ? scrollBarWidth : 0);

            if (x < 0 || x > maxX || y < minY || y > height)
                return false;

            row = y / rowHeight - numHeaderRows + scroll;

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

        protected override void OnMouseDown(MouseEventArgsEx e)
        {
            if (e.Button.HasFlag(MouseButtons.Left))
            {
                MarkDirty();

                if (e.Y > rowHeight * numHeaderRows)
                {
                    if (GetScrollBarParams(out var scrollBarPos, out var scrollBarSize) && e.X > width - scrollBarWidth)
                    {
                        var y = e.Y - rowHeight;

                        if (y < scrollBarPos)
                        {
                            scroll = Math.Max(0, scroll - 3);
                        }
                        else if (y > (scrollBarPos + scrollBarSize))
                        {
                            scroll = Math.Min(maxScroll, scroll + 3);
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
                    else
                    {
                        PixelToCell(e.X, e.Y, out var row, out var col);
                        
                        var colDesc = columns[col];

                        switch (colDesc.Type)
                        {
                            case ColumnType.Button:
                            {
                                if (IsPointInButton(e.X, row, col))
                                    ButtonPressed?.Invoke(this, row, col);
                                break;
                            }
                            case ColumnType.CheckBox:
                            {
                                data[row, col] = !(bool)data[row, col];
                                ValueChanged?.Invoke(this, row, col, data[row, col]);
                                break;
                            }
                            case ColumnType.Slider:
                            {
                                Capture = true;
                                draggingSlider = true;
                                sliderCol = col;
                                sliderRow = row;
                                data[row, col] = (int)Math.Round(Utils.Lerp(0, 100, Utils.Saturate((e.X - columnOffsets[col]) / (float)columnWidths[col])));
                                ValueChanged?.Invoke(this, row, col, data[row, col]);
                                break;
                            }
                            case ColumnType.DropDown:
                            { 
                                dropDownActive.Visible = true;
                                dropDownActive.Move(left + columnOffsets[col], top + (row + numHeaderRows - scroll) * rowHeight, columnWidths[col], rowHeight);
                                dropDownActive.SetItems(colDesc.DropDownValues);
                                dropDownActive.SelectedIndex = Array.IndexOf(colDesc.DropDownValues,(string)data[row, col]);
                                dropDownActive.SetListOpened(true);
                                dropDownActive.GrabDialogFocus();
                                dropDownRow = row;
                                dropDownCol = col;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private bool IsPointInButton(int x, int row, int col)
        {
            if (row < 0 || col < 0)
                return false;
            var cellX = x - columnOffsets[col];
            var buttonX = cellX - columnWidths[col] + rowHeight;
            return buttonX >= 0 && buttonX < rowHeight;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (draggingSlider)
            {
                var newSliderVal = (int)Math.Round(Utils.Lerp(0, 100, Utils.Saturate((e.X - columnOffsets[sliderCol]) / (float)columnWidths[sliderCol])));
                if (newSliderVal != (int)data[sliderRow, sliderCol])
                {
                    data[sliderRow, sliderCol] = newSliderVal;
                    ValueChanged?.Invoke(this, sliderRow, sliderCol, newSliderVal);
                    MarkDirty();
                }
            }
            else if (draggingScrollbars)
            {
                GetScrollBarParams(out var scrollBarPos, out var scrollBarSize);
                var newScrollBarPos = captureScrollBarPos + (e.Y - captureMouseY);
                var ratio = newScrollBarPos / (float)(numItemRows * rowHeight - scrollBarSize);
                var newScroll = Utils.Clamp((int)Math.Round(ratio * maxScroll), 0, maxScroll);
                SetAndMarkDirty(ref scroll, newScroll);
            }
            else
            {
                PixelToCell(e.X, e.Y, out var row, out var col);
                SetAndMarkDirty(ref hoverRow, row);
                SetAndMarkDirty(ref hoverCol, col);
                SetAndMarkDirty(ref hoverButton, IsPointInButton(e.X, row, col));
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (draggingScrollbars || draggingSlider)
            {
                draggingSlider = false;
                draggingScrollbars = false;
                Capture = false;
                MarkDirty();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            SetAndMarkDirty(ref hoverRow, -1);
            SetAndMarkDirty(ref hoverCol, -1);
            SetAndMarkDirty(ref hoverButton, false);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var sign = e.Delta < 0 ? 1 : -1;

            if (sign != 0)
            {
                SetAndMarkDirty(ref scroll, Utils.Clamp(scroll + sign * 3, 0, maxScroll));

                if (dropDownActive != null && dropDownActive.Visible)
                {
                    dropDownActive.SetListOpened(false);
                    dropDownActive.Visible = false;
                    GrabDialogFocus();
                }
            }
        }

        private bool GetScrollBarParams(out int pos, out int size)
        {
            if (data != null && data.GetLength(0) > numItemRows)
            {
                var scrollAreaSize = numItemRows * rowHeight;
                var minScrollBarSizeY = scrollAreaSize / 4;
                var scrollY = scroll * rowHeight;
                var maxScrollY = maxScroll * rowHeight;

                size = Math.Max(minScrollBarSizeY, (int)Math.Round(scrollAreaSize * Math.Min(1.0f, scrollAreaSize / (float)(maxScrollY + scrollAreaSize))));
                pos = (int)Math.Round((scrollAreaSize - size) * (scrollY / (float)maxScrollY));

                return true;
            }

            pos = 0;
            size = 0;
            return false;
        }

        protected override void OnRender(RenderGraphics g)
        {
            var c = g.CreateCommandList(GLGraphicsBase.CommandListUsage.Dialog);
            var hasScrollBar = GetScrollBarParams(out var scrollBarPos, out var scrollBarSize);
            var actualScrollBarWidth = hasScrollBar ? scrollBarWidth : 0;

            // BG
            c.FillRectangle(0, 0, width, height, ThemeResources.DarkGreyLineBrush1);

            // Grid lines
            c.DrawLine(0, 0, width, 0, ThemeResources.BlackBrush);
            for (var i = numHeaderRows + 1; i < numRows; i++)
                c.DrawLine(0, i * rowHeight, width - actualScrollBarWidth, i * rowHeight, ThemeResources.BlackBrush);
            for (var j = 0; j < columnOffsets.Length - 1; j++)
                c.DrawLine(columnOffsets[j], 0, columnOffsets[j], height, ThemeResources.BlackBrush);
            if (numHeaderRows != 0)
                c.DrawLine(0, rowHeight, width - 1, rowHeight, ThemeResources.LightGreyFillBrush1);
            c.DrawRectangle(0, 0, width - 1, height - 1, ThemeResources.LightGreyFillBrush1);

            var baseY = 0;

            // Header
            if (numHeaderRows != 0)
            {
                c.FillRectangle(0, 0, width, rowHeight, ThemeResources.DarkGreyLineBrush3);
                for (var j = 0; j < data.GetLength(1); j++) 
                    c.DrawText(columns[j].Name, ThemeResources.FontMedium, columnOffsets[j] + margin, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft, 0, rowHeight);
                baseY = rowHeight;
            }

            // Data
            if (data != null)
            {
                // Hovered cell
                if (hoverCol >= 0 && (hoverRow - scroll) >= 0 && hoverRow < data.GetLength(0))
                    c.FillRectangle(columnOffsets[hoverCol], (numHeaderRows + hoverRow - scroll) * rowHeight, columnOffsets[hoverCol + 1], (numHeaderRows + hoverRow - scroll + 1) * rowHeight, ThemeResources.DarkGreyLineBrush3);

                for (int i = 0, k = scroll; i < numItemRows && k < data.GetLength(0); i++, k++) // Rows
                {
                    var y = baseY + i * rowHeight;

                    for (var j = 0; j < data.GetLength(1); j++) // Colums
                    {
                        var col = columns[j];
                        var colWidth = columnWidths[j];
                        var x = columnOffsets[j];
                        var val = data[k, j];

                        if (k == dropDownRow && j == dropDownCol)
                            continue;

                        c.PushTranslation(x, y);

                        switch (col.Type)
                        {
                            case ColumnType.DropDown:
                            {
                                dropDownInactive.Visible = true;
                                dropDownInactive.SetItems(new[] { (string)val });
                                dropDownInactive.Move(left + x, top + y, columnWidths[j], rowHeight);
                                dropDownInactive.Render(g);
                                dropDownInactive.Visible = false;
                                break;
                            }
                            case ColumnType.Button:
                            {
                                var buttonBaseX = colWidth - rowHeight;
                                c.DrawText((string)val, ThemeResources.FontMedium, margin, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft, 0, rowHeight);
                                c.PushTranslation(buttonBaseX, 0);
                                c.FillAndDrawRectangle(0, 0, rowHeight - 1, rowHeight - 1, hoverRow == k && hoverCol == j && hoverButton ? ThemeResources.LightRedFillBrush : ThemeResources.DarkRedFillBrush, ThemeResources.LightGreyFillBrush1);
                                c.DrawText("...", ThemeResources.FontMedium, 0, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleCenter, rowHeight, rowHeight);
                                c.PopTransform();
                                break;
                            }
                            case ColumnType.Slider:
                            {
                                c.FillRectangle(0, 0, (int)Math.Round((int)val / 100.0f * colWidth), rowHeight, ThemeResources.DarkGreyFillBrush3);
                                c.DrawText(string.Format(CultureInfo.InvariantCulture, col.StringFormat, (int)val), ThemeResources.FontMedium, 0, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleCenter, colWidth, rowHeight);
                                break;
                            }
                            case ColumnType.Label:
                            {
                                c.DrawText((string)val, ThemeResources.FontMedium, margin, 0, ThemeResources.LightGreyFillBrush1, RenderTextFlags.MiddleLeft, 0, rowHeight);
                                break;
                            }
                            case ColumnType.CheckBox:
                            {
                                var checkBaseX = (colWidth  - bmpCheckOn.ElementSize.Width)  / 2;
                                var checkBaseY = (rowHeight - bmpCheckOn.ElementSize.Height) / 2;
                                c.PushTranslation(checkBaseX, checkBaseY);
                                c.DrawRectangle(0, 0, bmpCheckOn.ElementSize.Width - 1, bmpCheckOn.ElementSize.Height - 1, ThemeResources.LightGreyFillBrush1);
                                c.DrawBitmapAtlas((bool)val ? bmpCheckOn : bmpCheckOff, 0, 0, 1, 1, Theme.LightGreyFillColor1);
                                c.PopTransform();
                                break;
                            }
                        }

                        c.PopTransform();
                    }
                }

                if (hasScrollBar)
                {
                    c.FillAndDrawRectangle(width - scrollBarWidth, rowHeight, width - 1, rowHeight + numItemRows * rowHeight - 1, ThemeResources.DarkGreyFillBrush1, ThemeResources.LightGreyFillBrush1);
                    c.FillAndDrawRectangle(width - scrollBarWidth, rowHeight + scrollBarPos, width - 1, rowHeight + scrollBarPos + scrollBarSize - 1, ThemeResources.MediumGreyFillBrush1, ThemeResources.LightGreyFillBrush1);
                }
            }
        }
    }
}
