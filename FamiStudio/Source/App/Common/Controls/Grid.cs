using System;
using System.Globalization;
using System.Diagnostics;
using System.Linq;

namespace FamiStudio
{
    public class Grid : Control
    {
        public delegate void ValueChangedDelegate(Control sender, int rowIndex, int colIndex, object value);
        public delegate void ButtonPressedDelegate(Control sender, int rowIndex, int colIndex);
        public delegate void CellClickedDelegate(Control sender, bool left, int rowIndex, int colIndex);
        public delegate void CellDoubleClickedDelegate(Control sender, int rowIndex, int colIndex);
        public delegate void HeaderCellClickedDelegate(Control sender, int colIndex);
        public delegate void selectedRowUpdatedDelegate(Control sender, int rolIndex);

        public event ValueChangedDelegate ValueChanged;
        public event ButtonPressedDelegate ButtonPressed;
        public event CellClickedDelegate CellClicked;
        public event CellDoubleClickedDelegate CellDoubleClicked;
        public event HeaderCellClickedDelegate HeaderCellClicked;
        public event selectedRowUpdatedDelegate selectedRowUpdated;

        private class CellSliderData
        {
            public int MinValue;
            public int MaxValue;
            public Func<double, string> Formatter;
            public float? DefaultValue;
        }

        private int scroll;
        private int maxScroll;
        private int hoverRow = -1;
        private int hoverCol = -1;
        private int selectedRow = -1;
        private bool hoverButton;
        private int dropDownRow = -1;
        private int dropDownCol = -1;
        private int numRows;
        private int numItemRows;
        private int numHeaderRows;
        private object[,] data;
        private object[,] prevData;
        private int[] columnWidths;
        private int[] columnOffsets;
        private byte columnEnabledMask;
        private bool hasAnyDropDowns;
        private bool hasAnyCheckBoxes;
        private bool hasAnySliders;
        private bool fullRowSelect;
        private bool isClosingList;
        private Font font;
        private Font fontBold;
        private ColumnDesc[] columns;
        private Color[] rowColors;
        private Color foreColor = Theme.LightGreyColor1;
        private CellSliderData[,] cellSliderData;
        private bool[,] cellDisabled;
        private Point lastMousePosition;

        private bool draggingScrollbars;
        private bool draggingSlider;
        private int  captureScrollBarPos;
        private int  captureMouseY;
        private int  sliderCol;
        private int  sliderRow;

        private DropDown dropDownInactive;
        private DropDown dropDownActive;

        private TextureAtlasRef bmpCheckOn;
        private TextureAtlasRef bmpCheckOff;
        private TextureAtlasRef bmpRadioOn;
        private TextureAtlasRef bmpRadioOff;
        private TextureAtlasRef bmpUpDownPlus;
        private TextureAtlasRef bmpUpDownMinus;

        private int margin         = DpiScaling.ScaleForWindow(Platform.IsMobile ? 2 : 4);
        private int scrollBarWidth = DpiScaling.ScaleForWindow(10);
        private int rowHeight      = DpiScaling.ScaleForWindow(20);
        private int checkBoxWidth  = DpiScaling.ScaleForWindow(20);

        public int ItemCount => data.GetLength(0);
        public bool FullRowSelect { get => fullRowSelect; set => fullRowSelect = value; }

        #region Localization

        LocalizedString SelectAllLabel;
        LocalizedString SelectNoneLabel;
        LocalizedString EnterValueContext;
        LocalizedString ResetDefaultValueContext;
        LocalizedString ResetPreviousValueContext;

        #endregion

        public Grid(ColumnDesc[] columnDescs, int rows, bool hasHeader = true)
        {
            Localization.Localize(this);

            Debug.Assert(columnDescs.Length <= 8); // We use a byte for masks.

            columns = columnDescs;
            numRows = rows;
            numItemRows = hasHeader ? numRows - 1 : numRows;
            numHeaderRows = hasHeader ? 1 : 0;
            height = numRows * rowHeight;

            foreach (var col in columnDescs)
            {
                if (col.Type == ColumnType.DropDown)
                {
                    hasAnyDropDowns = true;
                }
                else if (col.Type == ColumnType.CheckBox)
                {
                    hasAnyCheckBoxes = true;
                }
                else if (col.Type == ColumnType.Slider)
                {
                    hasAnySliders = true;
                }
            }

            if (hasAnyDropDowns)
            {
                dropDownInactive = new DropDown(new[] { "" }, 0, true);
                dropDownActive = new DropDown(new[] { "" }, 0);
                dropDownInactive.SetRowHeight(rowHeight);
                dropDownActive.SetRowHeight(rowHeight);
                dropDownInactive.Visible = false;
                dropDownActive.Visible = false;
                dropDownInactive.IsGridChild = true;
                dropDownActive.IsGridChild = true;
                dropDownActive.ListClosing += DropDownActive_ListClosing;
                dropDownActive.SelectedIndexChanged += DropDownActive_SelectedIndexChanged;
            }

            columnEnabledMask = 0xff;
            
            supportsDoubleClick = true;
        }

        public void SetColumnEnabled(int col, bool enabled)
        {
            if (enabled)
                columnEnabledMask = (byte)(columnEnabledMask |  (1 << col));
            else
                columnEnabledMask = (byte)(columnEnabledMask & ~(1 << col));

            MarkDirty();
        }

        public void SetCellEnabled(int row, int col, bool enabled)
        {
            cellDisabled[row, col] = !enabled;
        }

        public bool IsColumnEnabled(int col)
        {
            return (columnEnabledMask & (1 << col)) != 0;
        }

        public bool IsCellEnabled(int row, int col)
        {
            return cellDisabled == null || !cellDisabled[row, col];
        }

        public bool IsCellOrColumnEnabled(int row, int col)
        {
            return IsColumnEnabled(col) && IsCellEnabled(row, col) && enabled;
        }

        public void SetRowColor(int row, Color color)
        {
            if (rowColors == null)
                rowColors = new Color[data.GetLength(0)];

            rowColors[row] = color;
            foreColor = Color.Black;
        }

        public void OverrideCellSlider(int row, int col, int min, int max, Func<double, string> fmt, float? defaultValue)
        {
            if (cellSliderData == null)
                cellSliderData = new CellSliderData[data.GetLength(0), data.GetLength(1)];

            cellSliderData[row, col] = new CellSliderData() { MinValue = min, MaxValue = max, Formatter = fmt, DefaultValue = defaultValue };
        }

        private void GetCellSliderData(int row, int col, out int min, out int max, out Func<double, string> fmt, out float? defaultValue)
        {
            if (cellSliderData != null && cellSliderData[row, col] != null)
            {
                min = cellSliderData[row, col].MinValue;
                max = cellSliderData[row, col].MaxValue;
                fmt = cellSliderData[row, col].Formatter;
                defaultValue = cellSliderData[row, col].DefaultValue;
            }
            else
            {
                min = columns[col].MinValue;    
                max = columns[col].MaxValue;
                fmt = columns[col].Formatter;
                defaultValue = columns[col].DefaultValue;
            }
        }

        private void DropDownActive_SelectedIndexChanged(Control sender, int index)
        {
            if (dropDownRow >= 0 && dropDownCol >= 0 && dropDownActive.Visible)
            {
                data[dropDownRow, dropDownCol] = dropDownActive.Text;
                ValueChanged?.Invoke(this, dropDownRow, dropDownCol, dropDownActive.Text);
                MarkDirty();
            }
        }

        private void DropDownActive_ListClosing(Control sender)
        {
            // Prevent recursion when clearing focus.
            if (isClosingList)
                return;

            Debug.Assert(dropDownActive.Visible);
            isClosingList = true;
            dropDownActive.Visible = false;
            dropDownRow = -1;
            dropDownCol = -1;
            GrabDialogFocus();
            MarkDirty();
            isClosingList = false;
        }

        public void UpdateData(int row, int col, object val)
        {
            Debug.Assert(val == null || val.GetType() != typeof(LocalizedString));
            data[row, col] = val;
            MarkDirty();
        }

        public void UpdateData(object[,] newData)
        {
#if DEBUG
            foreach (var o in newData)
                Debug.Assert(o == null || o.GetType() != typeof(LocalizedString));
#endif

            var sizeChanged = data == null ||
                data.GetLength(0) != newData.GetLength(0) ||
                data.GetLength(1) != newData.GetLength(1);

            data = newData;
            prevData = (object[,])newData.Clone();

            Debug.Assert(data.GetLength(1) == columns.Length);

            if (sizeChanged)
            {
                var newCellDisabled = new bool[data.GetLength(0), data.GetLength(1)];

                if (cellDisabled != null)
                {
                    for (var i = 0; i < data.GetLength(0); i++)
                    {
                        for (var j = 0; j < data.GetLength(1); j++)
                        {
                            if (i < cellDisabled.GetLength(0) && j < cellDisabled.GetLength(1))
                            {
                                newCellDisabled[i, j] = cellDisabled[i, j];
                            }
                        }
                    }
                }

                cellDisabled = newCellDisabled;
            }

            if (window != null)
                UpdateLayout();
            MarkDirty();
        }

        public void RenameColumns(string[] columnNames)
        {
            Debug.Assert(columnNames.Length == columns.Length);
            for (int i = 0; i < columnNames.Length; i++)
                columns[i].Name = columnNames[i];
            MarkDirty();
        }

        public object GetData(int row, int col)
        {
            return data[row, col];
        }

        public void SetData(int row, int col, object d)
        {
            data[row, col] = d;
            MarkDirty();
        }

        public void SetRadio(int row, int col)
        {
            Debug.Assert(columns[col].Type == ColumnType.Radio);
            for (int i = 0; i < data.GetLength(0); i++)
                data[i, col] = i == row;
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
                var colWidth = col.Type == ColumnType.CheckBox || col.Type == ColumnType.Radio || col.Type == ColumnType.Image ? checkBoxWidth : (int)Math.Round(col.Width * actualWidth);

                columnWidths[i] = colWidth;
                columnOffsets[i] = totalWidth;
                totalWidth += colWidth;
            }

            columnWidths[columns.Length - 1] = actualWidth - totalWidth;
            columnOffsets[columns.Length - 1] = totalWidth;
            columnOffsets[columns.Length] = width - 1;

            maxScroll = data != null ? Math.Max(0, data.GetLength(0) - numItemRows) : 0;
            scroll = Utils.Clamp(scroll, 0, maxScroll);
        }

        public void ResetScroll()
        {
            SetAndMarkDirty(ref scroll, 0);
        }

        public void UpdateSelectedRow(int index)
        {
            if (index >= scroll + numItemRows || index < scroll)
            {
                var newScroll = index < scroll ? index : index - numItemRows + 1;
                scroll = Math.Min(newScroll, ItemCount - numItemRows);
            }

            selectedRowUpdated?.Invoke(this, index);
            SetAndMarkDirty(ref selectedRow, index);
        }

        public void ResetSelectedRow()
        {
            SetAndMarkDirty(ref selectedRow, -1);
        }

        protected override void OnAddedToContainer()
        {
            var g = Graphics;

            bmpCheckOn     = g.GetTextureAtlasRef("CheckBoxYes");
            bmpCheckOff    = g.GetTextureAtlasRef("CheckBoxNo");
            bmpRadioOn     = g.GetTextureAtlasRef("RadioButtonOn");
            bmpRadioOff    = g.GetTextureAtlasRef("RadioButtonOff");
            bmpUpDownPlus  = g.GetTextureAtlasRef("UpDownPlus");
            bmpUpDownMinus = g.GetTextureAtlasRef("UpDownMinus");

            font     = Platform.IsMobile ? fonts.FontSmall     : fonts.FontMedium;
            fontBold = Platform.IsMobile ? fonts.FontSmallBold : fonts.FontMediumBold;

            UpdateLayout();

            if (hasAnyDropDowns)
            {
                ParentContainer.AddControl(dropDownInactive);
                ParentContainer.AddControl(dropDownActive);
            }
        }

        private bool PixelToCell(int x, int y, out int row, out int col)
        {
            row = -1;
            col = -1;

            var maxX = width - (GetScrollBarParams(out _, out _) ? scrollBarWidth : 0);

            if (x < 0 || x > maxX || y > height)
                return false;

            for (int i = 1; i < columnOffsets.Length; i++)
            {
                if (x <= columnOffsets[i])
                {
                    col = i - 1;
                    break;
                }
            }

            // Row -1 will mean header.
            row = y / rowHeight - numHeaderRows;

            if (row < 0)
            {
                return false;
            }

            row += scroll;

            Debug.Assert(col >= 0);
            if (row >= 0 && row < data.GetLength(0))
            {
                return IsCellOrColumnEnabled(row, col);
            }
            else
            {
                col = -1;
                return false;
            }
        }

        private void KeyboardNavigateUpDown(int newIndex)
        {
            var row = Math.Clamp(selectedRow + newIndex, 0, ItemCount - 1);
            UpdateSelectedRow(row);
        }

        protected override void OnPointerDown(PointerEventArgs e)
        {
            var valid = PixelToCell(e.X, e.Y, out var row, out var col);

            if (e.Left)
            {
                MarkDirty();

                if (e.Y > rowHeight * numHeaderRows)
                {
                    if (GetScrollBarParams(out var scrollBarPos, out var scrollBarSize) && e.X > width - scrollBarWidth)
                    {
                        var y = e.Y - rowHeight * numHeaderRows;

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
                            CapturePointer();
                            draggingScrollbars = true;
                            captureScrollBarPos = scrollBarPos;
                            captureMouseY = e.Y;
                        }

                        return;
                    }
                    else
                    {
                        if (valid)
                        { 
                            var colDesc = columns[col];

                            switch (colDesc.Type)
                            {
                                case ColumnType.Button:
                                {
                                    if (IsPointInButton(e.X, row, col))
                                        ButtonPressed?.Invoke(this, row, col);
                                    break;
                                }
                                case ColumnType.Radio:
                                {
                                    for (int i = 0; i < data.GetLength(0); i++)
                                        data[i, col] = i == row;
                                    ValueChanged?.Invoke(this, row, col, data[row, col]);
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
                                    CapturePointer();
                                    draggingSlider = true;
                                    sliderCol = col;
                                    sliderRow = row;
                                    GetCellSliderData(row, col, out var sliderMin, out var sliderMax, out _, out _);
                                    data[row, col] = (int)Math.Round(Utils.Lerp(sliderMin, sliderMax, Utils.Saturate((e.X - columnOffsets[col]) / (float) columnWidths[col])));
                                    ValueChanged?.Invoke(this, row, col, data[row, col]);
                                    break;
                                }
                                case ColumnType.DropDown:
                                {
                                    dropDownActive.Visible = true;
                                    dropDownActive.Move(left + columnOffsets[col], top + (row + numHeaderRows - scroll) * rowHeight, columnWidths[col], rowHeight);
                                    dropDownActive.SetItems(colDesc.DropDownValues);
                                    dropDownActive.SelectedIndex = Array.IndexOf(colDesc.DropDownValues, (string)data[row, col]);
                                    dropDownActive.SetListOpened(true);
                                    dropDownActive.GrabDialogFocus();
                                    dropDownRow = row;
                                    dropDownCol = col;
                                    break;
                                }
                                case ColumnType.NumericUpDown:
                                {
                                    var increment = 0;
                                    if (e.X <= (left + columnOffsets[col + 0] + rowHeight)) increment = -1;
                                    if (e.X >= (left + columnOffsets[col + 1] - rowHeight)) increment =  1;
                                    data[row, col] = Utils.Clamp((int)data[row, col] + increment, colDesc.MinValue, colDesc.MaxValue);
                                    ValueChanged?.Invoke(this, row, col, data[row, col]);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (valid)
            {
                if (e.Left || e.Right)
                    CellClicked?.Invoke(this, e.Left, row, col);
            }
            else if (e.Left && row < 0 && col >= 0)
            {
                HeaderCellClicked?.Invoke(this, col);
            }
            
            SetAndMarkDirty(ref selectedRow, row);
        }

        private bool IsPointInButton(int x, int row, int col)
        {
            if (row < 0 || col < 0 || !IsCellOrColumnEnabled(row, col))
                return false;
            var cellX = x - columnOffsets[col];
            var buttonX = cellX - columnWidths[col] + rowHeight;
            return buttonX >= 0 && buttonX < rowHeight;
        }

        protected void EnterSliderValue()
        {
            var col = Array.FindIndex(columns, c => c.Type == ColumnType.Slider);
            if (col == -1)
                return;

            GetCellSliderData(selectedRow, col, out var min, out var max, out var fmt, out _);
            
            var value = (int)data[selectedRow, col];
            var scale = Utils.ParseFloatWithLeadingAndTrailingGarbage(fmt(1));
            var dlg   = new ValueInputDialog(ParentWindow, new Point(WindowPosition.X, WindowPosition.Y), null, value, min, max, true, scale);
            
            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    data[selectedRow, col] = dlg.Value;
                    MarkDirty();
                }
            });
        }

        protected void ResetSliderDefaultValue()
        {
            var col = Array.FindIndex(columns, c => c.Type == ColumnType.Slider);
            if (col == -1)
                return;

            GetCellSliderData(selectedRow, col, out var min, out var max, out var fmt, out var defaultValue);

            if (defaultValue != null)
            {
                var val = (int)(defaultValue / Utils.ParseFloatWithLeadingAndTrailingGarbage(fmt(1)));
                data[selectedRow, col] = val;
                MarkDirty();
            }
        }

        protected void ResetSliderPreviousValue()
        {
            var col = Array.FindIndex(columns, c => c.Type == ColumnType.Slider);
            if (col == -1)
                return;

            data[selectedRow, col] = prevData[selectedRow, col];
        }

        protected override void OnPointerMove(PointerEventArgs e)
        {
            if (draggingSlider)
            {
                GetCellSliderData(sliderRow, sliderCol, out var sliderMin, out var sliderMax, out _, out _);
                var oldSliderVal = (int)data[sliderRow, sliderCol];
                var newSliderVal = ModifierKeys.IsControlDown
                    ? Math.Clamp(Math.Sign(e.X - lastMousePosition.X) + oldSliderVal, sliderMin, sliderMax)
                    : (int)Math.Round(Utils.Lerp(sliderMin, sliderMax, Utils.Saturate((e.X - columnOffsets[sliderCol]) / (float)columnWidths[sliderCol])));

                if (newSliderVal != oldSliderVal)
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
                UpdateHover(e);
            }

            lastMousePosition = e.Position;
        }

        protected override void OnLostDialogFocus()
        {
            ResetSelectedRow();
            base.OnLostDialogFocus();
        }

        protected override void OnPointerUp(PointerEventArgs e)
        {
            if (draggingScrollbars || draggingSlider)
            {
                draggingSlider = false;
                draggingScrollbars = false;
                ReleasePointer();
                MarkDirty();
            }
            else if (e.Right)
            {
                if (hasAnyCheckBoxes)
                {
                    App.ShowContextMenuAsync(new[]
                    {
                        new ContextMenuOption("SelectAll",  SelectAllLabel,  () => SelectAllCheckBoxes(true)),
                        new ContextMenuOption("SelectNone", SelectNoneLabel, () => SelectAllCheckBoxes(false))
                    });
                    e.MarkHandled();
                }
                else if (hasAnySliders)
                {
                    App.ShowContextMenuAsync(new[]
                    {
                        new ContextMenuOption("Type",      EnterValueContext,         () => EnterSliderValue()),
                        new ContextMenuOption("MenuReset", ResetPreviousValueContext, () => ResetSliderPreviousValue()),
                        new ContextMenuOption("MenuReset", ResetDefaultValueContext,  () => ResetSliderDefaultValue())
                    });
                    e.MarkHandled();
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (HasDialogFocus && !e.Handled)
            {
                if (e.Key == Keys.Left || e.Key == Keys.Right)
                {
                    // Slider adjustment.
                    if (hasAnySliders)
                    {
                        var col = Array.FindIndex(columns, c => c.Type == ColumnType.Slider);
                        if (col != -1)
                        {
                            var sign  = e.Key == Keys.Left ? -1 : 1;
                            var value = sign * (ModifierKeys.IsShiftDown ? 10 : 1);

                            IncrementSlider(selectedRow, col, value);
                        }
                    }

                    e.Handled = true;
                }
                else if (e.Key == Keys.Space)
                {
                    // Checkbox toggling.
                    if (hasAnyCheckBoxes)
                    {
                        var col = Array.FindIndex(columns, c => c.Type == ColumnType.CheckBox);
                        if (col != -1)
                        {
                            ToggleCheckbox(selectedRow, col);
                        }
                    }

                    e.Handled = true;
                }
                else
                {
                    // Vertical navigation.
                    var newIndex = e.Key switch
                    {
                        Keys.Up       => -1,
                        Keys.Down     =>  1,
                        Keys.PageUp   => -numItemRows,
                        Keys.PageDown =>  numItemRows,
                        Keys.Home     => -ItemCount,
                        Keys.End      =>  ItemCount,
                        _             =>  0,
                    };
                    if (newIndex != 0)
                    {
                        KeyboardNavigateUpDown(newIndex);
                        e.Handled = true;
                    }
                }
            }
        }

        private void ToggleCheckbox(int row, int col)
        {
            var newValue = !(bool)data[row, col];
            data[row, col] = newValue;

            ValueChanged?.Invoke(this, row, col, newValue);
            MarkDirty();
        }

        private void IncrementSlider(int row, int col, int offset)
        {
            GetCellSliderData(row, col, out var min, out var max, out _, out _);

            var newValue = (int)data[row, col] + offset;
            data[row, col] = Math.Clamp(newValue, min, max);

            ValueChanged?.Invoke(this, row, col, newValue);
            MarkDirty();
        }

        private void SelectAllCheckBoxes(bool check)
        {
            for (var i = 0; i < columns.Length; i++)
            {
                if (columns[i].Type == ColumnType.CheckBox)
                {
                    for (var j = 0; j < ItemCount; j++)
                    {
                        data[j, i] = check;
                    }
                }
            }

            MarkDirty();
        }

        protected override void OnPointerLeave(EventArgs e)
        {
            SetAndMarkDirty(ref hoverRow, -1);
            SetAndMarkDirty(ref hoverCol, -1);
            SetAndMarkDirty(ref hoverButton, false);
        }

        private void UpdateHover(PointerEventArgs e)
        {
            PixelToCell(e.X, e.Y, out var row, out var col);
            if (!e.IsTouchEvent)
            {
                SetAndMarkDirty(ref hoverRow, row);
                SetAndMarkDirty(ref hoverCol, col);
                SetAndMarkDirty(ref hoverButton, IsPointInButton(e.X, row, col));
            }
        }

        protected override void OnMouseWheel(PointerEventArgs e)
        {
            var sign = e.ScrollY < 0 ? 1 : -1;

            if (sign != 0)
            {
                if (GetScrollBarParams(out _, out _))
                    e.MarkHandled();

                SetAndMarkDirty(ref scroll, Utils.Clamp(scroll + sign * 3, 0, maxScroll));

                if (dropDownActive != null && dropDownActive.Visible)
                {
                    dropDownActive.SetListOpened(false);
                    dropDownActive.Visible = false;
                    GrabDialogFocus();
                    e.MarkHandled();
                }

                UpdateHover(e);
            }
        }

        protected override void OnMouseDoubleClick(PointerEventArgs e)
        {
            if (e.Left && PixelToCell(e.X, e.Y, out var row, out var col) && row < data.GetLength(0))
            {
                CellDoubleClicked?.Invoke(this, row, col);

                // Handle double-clicks as clicks for few types to make them more responsive.
                if ((e.Y > rowHeight * numHeaderRows))
                {
                    var colDesc = columns[col];
                    if (colDesc.Type == ColumnType.CheckBox ||
                        colDesc.Type == ColumnType.NumericUpDown)
                    {
                        OnPointerDown(e);
                    }
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

        protected override void OnResize(EventArgs e)
        {
            UpdateLayout();
            MarkDirty();
        }

        protected override void OnRender(Graphics g)
        {
            var c = g.DefaultCommandList;
            var hasScrollBar = GetScrollBarParams(out var scrollBarPos, out var scrollBarSize);
            var actualScrollBarWidth = hasScrollBar ? scrollBarWidth : 0;

            // BG
            c.FillRectangle(0, 0, width - 1, height, Theme.DarkGreyColor1);

            if (rowColors != null)
            {
                for (int i = 0, k = scroll; i < numItemRows && k < data.GetLength(0); i++, k++) // Rows
                {
                    var y = (i + numHeaderRows) * rowHeight;
                    c.FillRectangle(0, y, width - 1, y + rowHeight, rowColors[k]);
                }
            }

            // Header
            if (numHeaderRows != 0)
            {
                c.FillRectangle(0, 0, width, rowHeight, Theme.DarkGreyColor3);
                for (var j = 0; j < columns.Length; j++) 
                    c.DrawText(columns[j].Name, font, columnOffsets[j] + margin, 0, foreColor, TextFlags.MiddleLeft, 0, rowHeight);
            }

            var localForeColor = enabled ? foreColor : Theme.MediumGreyColor1;

            // Data
            if (data != null)
            {
                for (int i = 0, k = scroll; i < numItemRows && k < data.GetLength(0); i++, k++) // Rows
                {
                    var y = (i + numHeaderRows) * rowHeight;

                    for (var j = 0; j < data.GetLength(1); j++) // Colums
                    {
                        var col = columns[j];
                        var colWidth = columnWidths[j];
                        var cellEnabled = IsCellOrColumnEnabled(k, j);
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
                                c.DrawText((string)val, font, margin, 0, localForeColor, TextFlags.MiddleLeft, 0, rowHeight);
                                c.PushTranslation(buttonBaseX, 0);
                                c.FillAndDrawRectangle(0, 0, rowHeight - 1, rowHeight, hoverRow == k && hoverCol == j && hoverButton ? Theme.MediumGreyColor1 : Theme.DarkGreyColor3, localForeColor);
                                c.DrawText("...", fontBold, 0, 0, localForeColor, TextFlags.MiddleCenter, rowHeight, rowHeight);
                                c.PopTransform();
                                break;
                            }
                            case ColumnType.Slider:
                            {
                                if (cellEnabled)
                                {
                                    var f = float.Parse(val.ToString());
                                    GetCellSliderData(k, j, out var sliderMin, out var sliderMax, out var fmt, out _);
                                    c.FillRectangle(0, 0, (int)Math.Round((f - sliderMin) / (double)(sliderMax - sliderMin) * colWidth), rowHeight, Theme.DarkGreyColor6);
                                    c.DrawText(fmt(f), font, 0, 0, localForeColor, TextFlags.MiddleCenter, colWidth, rowHeight);
                                }
                                else
                                {
                                    c.DrawText("N/A", fontBold, 0, 0, Theme.MediumGreyColor1, TextFlags.MiddleCenter, colWidth, rowHeight);
                                }
                                break;
                            }
                            case ColumnType.Label:
                            {
                                c.DrawText((string)val, font, margin, 0, localForeColor, TextFlags.MiddleLeft | (col.Ellipsis ? TextFlags.Ellipsis : 0), colWidth - margin * 2, rowHeight);
                                break;
                            }
                            case ColumnType.Radio:
                            {
                                var radioBaseX = (colWidth  - bmpRadioOn.ElementSize.Width)  / 2;
                                var radioBaseY = (rowHeight - bmpRadioOn.ElementSize.Height) / 2;
                                c.PushTranslation(radioBaseX, radioBaseY);
                                c.DrawTextureAtlas((bool)val ? bmpRadioOn : bmpRadioOff, 0, 0, 1, localForeColor);
                                c.PopTransform();
                                break;
                            }
                            case ColumnType.CheckBox:
                            {
                                var checkBaseX = (colWidth  - bmpCheckOn.ElementSize.Width)  / 2;
                                var checkBaseY = (rowHeight - bmpCheckOn.ElementSize.Height) / 2;
                                c.PushTranslation(checkBaseX, checkBaseY);
                                c.DrawRectangle(0, 0, bmpCheckOn.ElementSize.Width - 1, bmpCheckOn.ElementSize.Height - 1, localForeColor);
                                c.DrawTextureAtlas((bool)val ? bmpCheckOn : bmpCheckOff, 0, 0, 1, localForeColor);
                                c.PopTransform();
                                break;
                            }
                            case ColumnType.Image:
                            {
                                var bmp = g.GetTextureAtlasRef((string)val);
                                c.DrawTextureAtlasCentered(bmp, 0, 0, checkBoxWidth, rowHeight, 1, localForeColor);
                                break;
                            }
                            case ColumnType.NumericUpDown:
                            {
                                if (cellEnabled)
                                {
                                    c.DrawTextureAtlasCentered(bmpUpDownMinus, 0, 0, rowHeight, rowHeight, 1, localForeColor);
                                    c.DrawTextureAtlasCentered(bmpUpDownPlus, colWidth - rowHeight, 0, rowHeight, rowHeight, 1, localForeColor);
                                    c.DrawText(val.ToString(), font, 0, 0, localForeColor, TextFlags.MiddleCenter, colWidth, rowHeight);
                                }
                                else
                                {
                                    c.DrawText("N/A", font, 0, 0, Theme.MediumGreyColor1, TextFlags.MiddleCenter, colWidth, rowHeight);
                                }
                                break;
                            }
                        }

                        c.PopTransform();
                    }
                }

                // Hovered cell
                if (enabled && hoverCol >= 0 && (hoverRow - scroll) >= 0 && (hoverRow - scroll) < numItemRows && hoverRow < data.GetLength(0))
                {
                    var hoverColor = Color.FromArgb(50, Color.White);

                    if (fullRowSelect)
                        c.FillRectangle(0, (numHeaderRows + hoverRow - scroll) * rowHeight, width, (numHeaderRows + hoverRow - scroll + 1) * rowHeight, hoverColor);
                    else
                        c.FillRectangle(columnOffsets[hoverCol], (numHeaderRows + hoverRow - scroll) * rowHeight, columnOffsets[hoverCol + 1], (numHeaderRows + hoverRow - scroll + 1) * rowHeight, hoverColor);
                }

                // Highlighted cell (for keyboard). Full Row Select
                if (enabled && selectedRow >= 0 && (selectedRow - scroll) >= 0 && (selectedRow - scroll) < numItemRows && selectedRow < data.GetLength(0))
                {
                    var highlightColor = Color.FromArgb(70, 0, 128, 255); // Blue
                    
                    c.FillRectangle(0, (numHeaderRows + selectedRow - scroll) * rowHeight, width, (numHeaderRows + selectedRow - scroll + 1) * rowHeight, highlightColor);
                }
            }

            // Border + Grid lines. Draw at end since on mobile these will be drawn as
            // polygons (thickness will be > 1) and they need to be on top.
            c.DrawLine(0, 0, width, 0, Theme.BlackColor);
            for (var i = numHeaderRows + 1; i < numRows; i++)
                c.DrawLine(0, i * rowHeight, width - actualScrollBarWidth, i * rowHeight, Theme.BlackColor);
            for (var j = 0; j < columnOffsets.Length - 1; j++)
                c.DrawLine(columnOffsets[j], 0, columnOffsets[j], height, Theme.BlackColor);
            if (numHeaderRows != 0)
                c.DrawLine(0, rowHeight, width - 1, rowHeight, foreColor);

            if (hasScrollBar)
            {
                c.PushTranslation(width - scrollBarWidth - 1, numHeaderRows * rowHeight);
                c.FillAndDrawRectangle(0, 0, scrollBarWidth, rowHeight * numItemRows, Theme.DarkGreyColor4, localForeColor);
                c.FillAndDrawRectangle(0, scrollBarPos, scrollBarWidth, scrollBarPos + scrollBarSize, Theme.MediumGreyColor1, localForeColor);
                c.PopTransform();
            }

            c.DrawRectangle(0, 0, width - 1, height, enabled ? foreColor : Theme.MediumGreyColor1);
        }
    }
}
