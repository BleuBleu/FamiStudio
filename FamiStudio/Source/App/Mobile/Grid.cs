using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FamiStudio
{
    public class Grid : Container
    {
        public delegate void ValueChangedDelegate(Control sender, int rowIndex, int colIndex, object value);
        public delegate void ButtonPressedDelegate(Control sender, int rowIndex, int colIndex);

        public event ValueChangedDelegate ValueChanged;
        public event ButtonPressedDelegate ButtonPressed;

        private bool init;

        private int margin = DpiScaling.ScaleForWindow(4);

        private ColumnDesc[] columns;
        private Control[,] gridControls;
        private CellSliderData[,] cellSliderData;
        private bool[,] gridDisabled;
        private object[,] data;
        private GridOptions options;

        public object[,] Data => data;

        private class CellSliderData
        {
            public int MinValue;
            public int MaxValue;
            public Func<double, string> Formatter;
            public float? DefaultValue;
        }

        public Grid(Container parent, ColumnDesc[] cols, object[,] d, GridOptions opts = GridOptions.None)
        {
            clipRegion = false;
            columns = cols;
            options = opts;
            data = d;
            gridControls = new Control[data.GetLength(0), data.GetLength(1)];
            gridDisabled = new bool[data.GetLength(0), data.GetLength(1)];
        }

        private void ConditionalRecreateAllControls()
        {
            if (HasParent)
            {
                RecreateAllControls();
            }
        }

        private void RecreateAllControls()
        {
            RemoveAllControls();
            gridControls = new Control[data.GetLength(0), data.GetLength(1)];

            var numRows = 0;
            var twoColumnLayout = options.HasFlag(GridOptions.MobileTwoColumnLayout);
            var mergeCheckboxAndLabel = false;
            var firstColumnIsLabel = false;

            if (twoColumnLayout)
            {
                Debug.Assert(columns.Length == 2);
                numRows = data.GetLength(0);
            }
            else
            {
                // Special case, if the 2 first columns are checkbox + label, we combine then in something nicer.
                // TODO : Should those be GridOptions.xxx flags?
                if (columns[0].Type == ColumnType.CheckBox && columns[1].Type == ColumnType.Label)
                {
                    numRows = data.GetLength(0) * (data.GetLength(1) - 1);
                    mergeCheckboxAndLabel = true;
                }
                else if (columns[0].Type == ColumnType.Label)
                {
                    firstColumnIsLabel = true;
                }
                else
                {
                    numRows = data.GetLength(0) * data.GetLength(1);
                }
            }

            var maxColumnNameWidth = 0;

            for (int c = 0; c < data.GetLength(1); c++)
            {
                maxColumnNameWidth = Math.Max(maxColumnNameWidth, fonts.FontMedium.MeasureString(columns[c].Name, false));
            }

            var columnNameEllispsis = false;
            if (maxColumnNameWidth > (width * 2 / 5))
            {
                maxColumnNameWidth = (width * 2 / 5);
                columnNameEllispsis = true;
            }

            var rowMargin = DpiScaling.ScaleForWindow(2);
            var rowHeight = DpiScaling.ScaleForWindow(16);
            var y = 0;

            for (int r = 0; r < data.GetLength(0); r++)
            {
                for (int c = 0; c < columns.Length; c++)
                {
                    var ctrl = (Control)null;
                    var col = columns[c];
                    var localRowHeight = rowHeight;

                    if (c == 0 && mergeCheckboxAndLabel)
                    {
                        var checkBox = new CheckBox((bool)data[r, c], (string)data[r, c + 1]);
                        checkBox.Move(0, y, width, rowHeight);
                        checkBox.CheckedChanged += CheckBox_CheckedChanged;
                        checkBox.Enabled = !gridDisabled[r, 0] && enabled;
                        AddControl(checkBox);

                        gridControls[r, 0] = checkBox;
                        gridControls[r, 1] = checkBox;

                        c++;
                        y += checkBox.Height;
                        continue;
                    }

                    var x = 0;
                    var noLabel = twoColumnLayout || (firstColumnIsLabel && c == 0);

                    if (!noLabel)
                    {
                        var colLabel = new Label(columns[c].Name);
                        colLabel.Move(0, y, maxColumnNameWidth, rowHeight);
                        colLabel.Ellipsis = columnNameEllispsis;
                        AddControl(colLabel);
                        x = colLabel.Right + margin;
                    }
                    else if (twoColumnLayout && c == 1)
                    {
                        x = (int)(columns[0].Width * width);
                    }

                    switch (col.Type)
                    {
                        case ColumnType.CheckBox:
                        {
                            var checkBox = new CheckBox((bool)data[r, c]);
                            checkBox.CheckedChanged += CheckBox_CheckedChanged;
                            ctrl = checkBox;
                            break;
                        }
                        case ColumnType.Label:
                        {
                            var text = new Label((string)data[r, c]);
                            text.Ellipsis = twoColumnLayout;
                            ctrl = text;
                            break;
                        }
                        case ColumnType.Slider:
                        {
                            var min = col.MinValue;
                            var max = col.MaxValue;
                            var fmt = col.Formatter;

                            if (cellSliderData != null && cellSliderData[r, c] != null)
                            {
                                min = cellSliderData[r, c].MinValue;
                                max = cellSliderData[r, c].MaxValue;
                                fmt = cellSliderData[r, c].Formatter;
                            }

                            var slider = new Slider((int)data[r, c], min, max, fmt);
                            slider.ValueChanged += Slider_ValueChanged;
                            localRowHeight = slider.Height;
                            ctrl = slider;
                            break;
                        }
                        case ColumnType.NumericUpDown:
                        {
                            var upDown = new NumericUpDown((int)data[r, c], col.MinValue, col.MaxValue, 1);
                            upDown.ValueChanged += UpDown_ValueChanged;
                            ctrl = upDown;
                            break;
                        }
                        case ColumnType.DropDown:
                        {
                            var dropDown = new DropDown(col.DropDownValues, Array.IndexOf(col.DropDownValues, (string)data[r, c]));
                            dropDown.SelectedIndexChanged += DropDown_SelectedIndexChanged;
                            ctrl = dropDown;
                            break;
                        }
                        case ColumnType.Button:
                        {
                            var button = new Button(null, (string)data[r, c]);
                            button.Border = true;
                            button.Click += Button_Click;
                            ctrl = button;
                            break;
                        }
                        default:
                        {
                            Debug.Assert(false);
                            break;
                        }
                    }

                    ctrl.Move(x, y, false);

                    if (twoColumnLayout)
                    {
                        ctrl.Resize((int)(col.Width * width), localRowHeight);
                    }
                    else
                    {
                        ctrl.Resize(width - x, localRowHeight);
                    }

                    AddControl(ctrl);

                    if (c == 1 || !twoColumnLayout)
                    {
                        y += localRowHeight + rowMargin;
                    }

                    ctrl.Enabled = !gridDisabled[r, c] && enabled;
                    gridControls[r, c] = ctrl;
                }
            }

            Resize(width, y, false);
            MarkDirty();
        }

        private bool GetGridCoordForControl(Control ctrl, out int row, out int col)
        {
            for (int r = 0; r < gridControls.GetLength(0); r++)
            {
                for (int c = 0; c < gridControls.GetLength(1); c++)
                {
                    if (ctrl == gridControls[r, c])
                    {
                        row = r;
                        col = c;
                        return true;
                    }
                }
            }

            row = -1;
            col = -1;
            return false;
        }

        private void DropDown_SelectedIndexChanged(Control sender, int index)
        {
            if (GetGridCoordForControl(sender, out int row, out int col))
            {
                var text = columns[col].DropDownValues[index];
                data[row, col] = text;
                ValueChanged?.Invoke(this, row, col, text);
            }
        }

        private void UpDown_ValueChanged(Control sender, int val)
        {
            if (GetGridCoordForControl(sender, out int row, out int col))
            {
                data[row, col] = val;
                ValueChanged?.Invoke(this, row, col, val);
            }
        }

        private void Slider_ValueChanged(Control sender, double val)
        {
            if (GetGridCoordForControl(sender, out int row, out int col))
            {
                var rounded = (int)Math.Round(val);
                data[row, col] = rounded;
                ValueChanged?.Invoke(this, row, col, rounded);
            }
        }

        private void CheckBox_CheckedChanged(Control sender, bool check)
        {
            if (GetGridCoordForControl(sender, out int row, out int col))
            {
                data[row, col] = check;
                ValueChanged?.Invoke(this, row, col, (object)check);
            }
        }

        private void Button_Click(Control sender)
        {
            if (GetGridCoordForControl(sender, out int row, out int col))
            {
                ButtonPressed?.Invoke(this, row, col);
            }
        }

        public void SetData(int row, int col, object d)
        {
            data[row, col] = d;

            var ctrl = gridControls[row, col];

            switch (columns[col].Type)
            {
                case ColumnType.CheckBox:
                {
                    (ctrl as CheckBox).Checked = (bool)d;
                    break;
                }
                case ColumnType.Label:
                {
                    (ctrl as Label).Text = (string)d;
                    break;
                }
                case ColumnType.Slider:
                {
                    (ctrl as Slider).Value = (double)d;
                    break;
                }
                case ColumnType.NumericUpDown:
                {
                    (ctrl as NumericUpDown).Value = (int)d;
                    break;
                }
                case ColumnType.DropDown:
                {
                    (ctrl as DropDown).SelectedIndex = Array.IndexOf(columns[col].DropDownValues, (string)d);
                    break;
                }
            }

            MarkDirty();
        }

        public override bool Enabled 
        {
            get { return base.Enabled; }
            set
            {
                base.Enabled = value;

                if (gridControls != null)
                {
                    for (int r = 0; r < gridControls.GetLength(0); r++)
                    {
                        for (int c = 0; c < gridControls.GetLength(1); c++)
                        {
                            var ctrl = gridControls[r, c];
                            if (ctrl != null)
                            {
                                if (value)
                                {
                                    ctrl.Enabled = !gridDisabled[r, c];
                                }
                                else
                                {
                                    ctrl.Enabled = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OverrideCellSlider(int row, int col, int min, int max, Func<double, string> fmt, float? defaultValue)
        {
            if (cellSliderData == null)
                cellSliderData = new CellSliderData[data.GetLength(0), data.GetLength(1)];

            cellSliderData[row, col] = new CellSliderData() { MinValue = min, MaxValue = max, Formatter = fmt, DefaultValue = defaultValue };

            var slider = gridControls[row, col] as Slider;

            if (slider != null)
            {
                slider.Min = min;
                slider.Max = max;
                slider.Format = fmt;
                slider.DefaultValue = (float)defaultValue;
            }
        }

        public void UpdateControlValue(Control sender, object val)
        {
            if (GetGridCoordForControl(sender, out var row, out var col))
                SetData(row, col, val);
        }

        public string GetControlLabel(Control sender)
        {
            var col = columns
                .Select((column, index) => new { column, index })
                .FirstOrDefault(x => x.column.Type == ColumnType.Label)?.index ?? -1;

            if (GetGridCoordForControl(sender, out var row, out _) && col != -1)
                return (string)data[row, col];

            return string.Empty;
        }

        protected override void OnResize(EventArgs e)
        {
            ConditionalRecreateAllControls();
        }

        protected override void OnAddedToContainer()
        {
            if (!init)
                RecreateAllControls(); // Prevents EditTextAsync from recreating the controls and not working.

            init = true;
        }

        public void UpdateData(object[,] newData)
        {
            var sizeChanged =
                data.GetLength(0) != newData.GetLength(0) ||
                data.GetLength(1) != newData.GetLength(1);

            data = newData;

            if (sizeChanged)
            {
                var newGridDisabled = new bool[data.GetLength(0), data.GetLength(1)];

                for (var i = 0; i < data.GetLength(0); i++)
                {
                    for (var j = 0; j < data.GetLength(1); j++)
                    {
                        if (i < gridDisabled.GetLength(0) && j < gridDisabled.GetLength(1))
                        {
                            newGridDisabled[i, j] = gridDisabled[i, j];
                        }
                    }
                }

                gridDisabled = newGridDisabled;
            }

            Debug.Assert(data.GetLength(1) == columns.Length);
            ConditionalRecreateAllControls();
        }

        public void UpdateData(int row, int col, object val)
        {
            Debug.Assert(val == null || val.GetType() != typeof(LocalizedString));
            data[row, col] = val;
            ConditionalRecreateAllControls();
        }

        public object GetData(int row, int col)
        {
            return data[row, col];
        }

        public void RenameColumns(string[] columnNames)
        {
            Debug.Assert(columnNames.Length == columns.Length);
            for (int i = 0; i < columnNames.Length; i++)
                columns[i].Name = columnNames[i];
            ConditionalRecreateAllControls();
        }

        public void SetCellEnabled(int row, int col, bool enabled)
        {
            gridDisabled[row, col] = !enabled;
            
            if (gridControls[row, col] != null)
            {
                gridControls[row, col].Enabled = enabled;
            }
        }

        public void SetColumnEnabled(int colIdx, bool enabled)
        {
            for (int i = 0; i < gridControls.GetLength(0); i++)
            {
                gridDisabled[i, colIdx] = !enabled;

                if (gridControls[i, colIdx] != null)
                {
                    gridControls[i, colIdx].Enabled = enabled;
                }
            }
        }
    }
}