using System;
using System.Diagnostics;

namespace FamiStudio
{
    public partial class PropertyPage
    {
        private readonly static string[] WarningIcons =
        {
            "WarningGood",
            "WarningYellow",
            "Warning"
        };

        public void SetPropertyWarning(int idx, CommentType type, string comment)
        {
            var prop = properties[idx];

            if (prop.warningIcon == null)
                prop.warningIcon = CreateImageBox(WarningIcons[(int)type]);
            else
                prop.warningIcon.AtlasImageName = WarningIcons[(int)type];

            prop.warningIcon.Resize(DpiScaling.ScaleForWindow(16), DpiScaling.ScaleForWindow(16));
            prop.warningIcon.Visible = !string.IsNullOrEmpty(comment);
            prop.warningIcon.ToolTip = comment;
        }

        private void CheckList_ValueChanged(Control sender, int rowIndex, int colIndex, object value)
        {
            var propIdx = GetPropertyIndexForControl(sender);
            PropertyChanged?.Invoke(this, propIdx, rowIndex, colIndex, value);
        }

        private Grid CreateCheckListBox(string[] values, bool[] selected, string tooltip = null, int numRows = 7)
        {
            var columns = new[]
            {
                new ColumnDesc("A", 0.0f, ColumnType.CheckBox),
                new ColumnDesc("B", 1.0f, ColumnType.Label)
            };

            var grid = new Grid(columns, numRows, false);
            var data = new object[values.Length, 2];

            for (int i = 0; i < values.Length; i++)
            {
                data[i, 0] = selected != null ? selected[i] : true;
                data[i, 1] = values[i];
            }

            grid.UpdateData(data);
            grid.ValueChanged += CheckList_ValueChanged;
            grid.ToolTip = tooltip;

            return grid;
        }

        private void RadioButtonList_ValueChanged(Control sender, int rowIndex, int colIndex, object value)
        {
            var propIdx = GetPropertyIndexForControl(sender);
            PropertyChanged?.Invoke(this, propIdx, rowIndex, colIndex, value);
        }

        private Grid CreateRadioButtonList(string[] values, int selectedIndex, string tooltip = null, int numRows = 7)
        {
            var columns = new[]
            {
                new ColumnDesc("A", 0.0f, ColumnType.Radio),
                new ColumnDesc("B", 1.0f, ColumnType.Label)
            };

            var grid = new Grid(columns, numRows, false);
            var data = new object[values.Length, 2];

            for (int i = 0; i < values.Length; i++)
            {
                data[i, 0] = i == selectedIndex ? true : false;
                data[i, 1] = values[i];
            }

            grid.FullRowSelect = true;
            grid.UpdateData(data);
            grid.ValueChanged += RadioButtonList_ValueChanged;
            grid.CellClicked += RadioButtonList_CellClicked;
            grid.ToolTip = tooltip;

            return grid;
        }

        private void RadioButtonList_CellClicked(Control sender, bool left, int rowIndex, int colIndex)
        {
            if (colIndex > 0)
            {
                var grid = sender as Grid;
                grid.SetRadio(rowIndex, 0);
            }
        }

        private Grid CreateGrid(ColumnDesc[] columnDescs, object[,] data, int numRows = 7, string tooltip = null, GridOptions options = GridOptions.None)
        {
            var grid = new Grid(columnDescs, numRows, !options.HasFlag(GridOptions.NoHeader));

            if (data != null)
                grid.UpdateData(data);

            grid.ToolTip = tooltip;
            grid.ValueChanged += Grid_ValueChanged;
            grid.ButtonPressed += Grid_ButtonPressed;
            grid.CellDoubleClicked += Grid_CellDoubleClicked;
            grid.CellClicked += Grid_CellClicked;
            grid.CellEnabled += Grid_CellEnabled;

            return grid;
        }

        private bool Grid_CellEnabled(Control sender, int rowIndex, int colIndex)
        {
            var propIdx = GetPropertyIndexForControl(sender);
            return PropertyCellEnabled == null || PropertyCellEnabled.Invoke(this, propIdx, rowIndex, colIndex);
        }

        private void Grid_CellClicked(Control sender, bool left, int rowIndex, int colIndex)
        {
            var propIdx = GetPropertyIndexForControl(sender);
            PropertyClicked?.Invoke(this, left ? ClickType.Left : ClickType.Right, propIdx, rowIndex, colIndex);
        }

        private void Grid_CellDoubleClicked(Control sender, int rowIndex, int colIndex)
        {
            var propIdx = GetPropertyIndexForControl(sender);
            PropertyClicked?.Invoke(this, ClickType.Double, propIdx, rowIndex, colIndex);
        }

        private void Grid_ButtonPressed(Control sender, int rowIndex, int colIndex)
        {
            var propIdx = GetPropertyIndexForControl(sender);
            PropertyClicked?.Invoke(this, ClickType.Button, propIdx, rowIndex, colIndex);
        }

        private void Grid_ValueChanged(Control sender, int rowIndex, int colIndex, object value)
        {
            var propIdx = GetPropertyIndexForControl(sender);
            PropertyChanged?.Invoke(this, propIdx, rowIndex, colIndex, value);
        }

        public void SetColumnEnabled(int propIdx, int colIdx, bool enabled)
        {
            var grid = properties[propIdx].control as Grid;
            grid.SetColumnEnabled(colIdx, enabled);
        }

        public void SetRowColor(int propIdx, int rowIdx, Color color)
        {
            var grid = properties[propIdx].control as Grid;
            grid.SetRowColor(rowIdx, color);
        }

        public void OverrideCellSlider(int propIdx, int rowIdx, int colIdx, int min, int max, Func<object, string> fmt)
        {
            var grid = properties[propIdx].control as Grid;
            grid.OverrideCellSlider(rowIdx, colIdx, min, max, fmt);
        }

        public void UpdateGrid(int idx, object[,] data, string[] columnNames = null)
        {
            var list = properties[idx].control as Grid;

            list.UpdateData(data);

            if (columnNames != null)
                list.RenameColumns(columnNames);
        }

        public void UpdateGrid(int idx, int rowIdx, int colIdx, object value)
        {
            var grid = properties[idx].control as Grid;
            grid.UpdateData(rowIdx, colIdx, value);
        }

        public void UpdateCheckBoxList(int idx, string[] values, bool[] selected)
        {
            var grid = properties[idx].control as Grid;
            Debug.Assert(values.Length == grid.ItemCount);

            for (int i = 0; i < values.Length; i++)
            {
                grid.UpdateData(i, 0, selected != null ? selected[i] : true);
                grid.UpdateData(i, 1, values[i]);
            }
        }

        public void UpdateCheckBoxList(int idx, bool[] selected)
        {
            var grid = properties[idx].control as Grid;
            Debug.Assert(selected.Length == grid.ItemCount);

            for (int i = 0; i < selected.Length; i++)
                grid.UpdateData(i, 0, selected[i]);
        }

        public T GetPropertyValue<T>(int idx, int rowIdx, int colIdx)
        {
            var prop = properties[idx];
            Debug.Assert(prop.type == PropertyType.Grid);
            var grid = prop.control as Grid;
            return (T)grid.GetData(rowIdx, colIdx);
        }

        public void SetPropertyValue(int idx, int rowIdx, int colIdx, object value)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.Grid:
                    (prop.control as Grid).SetData(rowIdx, colIdx, value);
                    break;
            }
        }

        private object GetCheckBoxListValue(int idx)
        {
            var prop = properties[idx];
            var grid = prop.control as Grid;
            var selected = new bool[grid.ItemCount];
            for (int i = 0; i < grid.ItemCount; i++)
                selected[i] = (bool)grid.GetData(i, 0);
            return selected;
        }

        private int GetRadioListSelectedIndex(int idx)
        {
            var prop = properties[idx];
            var grid = prop.control as Grid;

            for (int i = 0; i < grid.ItemCount; i++)
            {
                if ((bool)grid.GetData(i, 0) == true)
                    return i;
            }

            Debug.Assert(false);
            return -1;
        }

        public int AddCheckBoxList(string label, string[] values, bool[] selected, string tooltip = null, int numRows = 7)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.CheckBoxList,
                    label = label != null ? CreateLabel(label) : null,
                    control = CreateCheckListBox(values, selected, tooltip, numRows)
                });
            return properties.Count - 1;
        }

        public int AddRadioButtonList(string label, string[] values, int selectedIndex, string tooltip = null, int numRows = 7)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.RadioList,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateRadioButtonList(values, selectedIndex, tooltip, numRows)
                });
            return properties.Count - 1;
        }

        public int AddGrid(string label, ColumnDesc[] columnDescs, object[,] data, int numRows = 7, string tooltip = null, GridOptions options = GridOptions.None)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Grid,
                    control = CreateGrid(columnDescs, data, numRows, tooltip, options)
                });
            return properties.Count - 1;
        }

        public void Build(bool advanced = false)
        {
            var margin = DpiScaling.ScaleForWindow(8);
            var maxLabelWidth = 0;
            var propertyCount = advanced || advancedPropertyStart < 0 ? properties.Count : advancedPropertyStart;

            container.RemoveAllControls();

            for (int i = 0; i < propertyCount; i++)
            {
                var prop = properties[i];
                if (prop.visible && prop.label != null)
                {
                    // HACK : Control need to be added to measure string.
                    container.AddControl(prop.label);
                    maxLabelWidth = Math.Max(maxLabelWidth, prop.label.MeasureWidth());
                    container.RemoveControl(prop.label);
                }
            }

            var totalHeight = 0;
            var warningWidth = showWarnings ? DpiScaling.ScaleForWindow(16) + margin : 0;
            var actualLayoutWidth = layoutWidth;

            var x = 0;
            var y = 0;

            for (int i = 0; i < propertyCount; i++)
            {
                var prop = properties[i];
                var height = 0;

                if (!prop.visible)
                    continue;

                if (i > 0)
                    totalHeight += margin;

                // These should take the full width, so if there is a label, it will go above.
                // MATTT : Actually make this work.
                var putLabelAbove = prop.type == PropertyType.Grid         ||
                                    prop.type == PropertyType.CheckBoxList ||
                                    prop.type == PropertyType.RadioList    ||
                                    prop.type == PropertyType.LogTextBox   ||
                                    prop.type == PropertyType.ColorPicker  ||
                                    prop.type == PropertyType.ProgressBar  ||
                                    prop.type == PropertyType.ImageBox;

                if (prop.label != null)
                {
                    prop.label.Move(x, y + totalHeight, maxLabelWidth, prop.label.Height);
                    prop.control.Move(x + maxLabelWidth + margin, y + totalHeight, actualLayoutWidth - maxLabelWidth - warningWidth - margin, prop.control.Height);

                    container.AddControl(prop.label);
                    container.AddControl(prop.control);

                    height = prop.label.Height;
                }
                else
                {
                    prop.control.Move(x, y + totalHeight, actualLayoutWidth, prop.control.Height);

                    container.AddControl(prop.control);
                }

                height = Math.Max(prop.control.Height, height);

                if (prop.warningIcon != null)
                {
                    prop.warningIcon.Move(
                        x + actualLayoutWidth - prop.warningIcon.Width,
                        y + totalHeight + (height - prop.warningIcon.Height) / 2);
                    container.AddControl(prop.warningIcon);
                }

                totalHeight += height;
            }

            layoutHeight = totalHeight;

            ConditionalSetTextBoxFocus();
        }
    }
}