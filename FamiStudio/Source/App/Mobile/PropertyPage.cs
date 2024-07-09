using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public partial class PropertyPage
    {
        public void SetPropertyWarning(int idx, CommentType type, string comment)
        {
        }

        //private bool Grid_CellEnabled(Control sender, int rowIndex, int colIndex)
        //{
        //    var propIdx = GetPropertyIndexForControl(sender);
        //    return PropertyCellEnabled == null || PropertyCellEnabled.Invoke(this, propIdx, rowIndex, colIndex);
        //}

        //private void Grid_CellClicked(Control sender, bool left, int rowIndex, int colIndex)
        //{
        //    var propIdx = GetPropertyIndexForControl(sender);
        //    PropertyClicked?.Invoke(this, left ? ClickType.Left : ClickType.Right, propIdx, rowIndex, colIndex);
        //}

        //private void Grid_CellDoubleClicked(Control sender, int rowIndex, int colIndex)
        //{
        //    var propIdx = GetPropertyIndexForControl(sender);
        //    PropertyClicked?.Invoke(this, ClickType.Double, propIdx, rowIndex, colIndex);
        //}

        //private void Grid_ButtonPressed(Control sender, int rowIndex, int colIndex)
        //{
        //    var propIdx = GetPropertyIndexForControl(sender);
        //    PropertyClicked?.Invoke(this, ClickType.Button, propIdx, rowIndex, colIndex);
        //}

        //private void Grid_ValueChanged(Control sender, int rowIndex, int colIndex, object value)
        //{
        //    var propIdx = GetPropertyIndexForControl(sender);
        //    PropertyChanged?.Invoke(this, propIdx, rowIndex, colIndex, value);
        //}

        public void SetColumnEnabled(int propIdx, int colIdx, bool enabled)
        {
            // MATTT : Need to keep labels too to toggle them.
            var prop = properties[propIdx];
            Debug.Assert(prop.type == PropertyType.Grid);

            // MATTT : This is very dump code.
            for (int i = 0; i < prop.subControls.Length; i++)
            {
                var r = i / prop.columns.Length;
                var c = i % prop.columns.Length;

                if (c == colIdx)
                    prop.subControls[i].Enabled = enabled;
            }
        }

        public void SetRowColor(int propIdx, int rowIdx, Color color)
        {
            //var grid = properties[propIdx].control as Grid;
            //grid.SetRowColor(rowIdx, color);
        }

        public void OverrideCellSlider(int propIdx, int rowIdx, int colIdx, int min, int max, Func<object, string> fmt)
        {
            //var grid = properties[propIdx].control as Grid;
            //grid.OverrideCellSlider(rowIdx, colIdx, min, max, fmt);
        }

        public int AddGrid(string label, ColumnDesc[] columnDescs, object[,] data, int rows = 7, string tooltip = null, GridOptions options = GridOptions.None)
        {
            // We need initial data on mobile.
            Debug.Assert(data != null);

            var prop = new Property();
            prop.type = PropertyType.Grid;
            prop.label = label != null ? CreateLabel(label, tooltip) : null;
            prop.columns = columnDescs;

            var gridContainer = new Container();
            gridContainer.SetupClipRegion(false);

            prop.control = gridContainer;
            prop.subControls = new Control[data.Length];

            // Need to add to be able to add sub controls.
            container.AddControl(gridContainer);

            var numRows = 0;
            var noHeader = options.HasFlag(GridOptions.NoHeader);
            var mergeCheckboxAndLabel = false;

            if (noHeader)
            {
                Debug.Assert(columnDescs.Length == 2);
                numRows = data.GetLength(0);
            }
            else
            {
                // Special case, if the 2 first columns are checkbox + label, we combine then in something nicer.
                if (columnDescs[0].Type == ColumnType.CheckBox && columnDescs[1].Type == ColumnType.Label)
                {
                    numRows = data.GetLength(0) * (data.GetLength(1) - 1);
                    mergeCheckboxAndLabel = true;
                }
                else
                {
                    numRows = data.GetLength(0) * data.GetLength(1);
                }
            }

            var rowMargin = DpiScaling.ScaleForWindow(2);
            var rowHeight = DpiScaling.ScaleForWindow(16);
            var rowIdx = 0;
            var y = 0;

            for (int r = 0; r < data.GetLength(0); r++)
            {
                for (int c = 0; c < columnDescs.Length; c++)
                {
                    var ctrl = (Control)null;
                    var col = columnDescs[c];

                    if (mergeCheckboxAndLabel && c == 0)
                    {
                        var checkBox = new CheckBox((bool)data[r, c], (string)data[r, c + 1]);
                        checkBox.Move(0, y, 1000, rowHeight);
                        gridContainer.AddControl(checkBox);

                        prop.subControls[r * prop.columns.Length + 0] = checkBox;
                        prop.subControls[r * prop.columns.Length + 1] = checkBox;

                        c++;
                        rowIdx++;
                        y += checkBox.Height;
                        continue;
                    }

                    var x = 0;

                    if (!noHeader)
                    {
                        var colLabel = new Label(columnDescs[c].Name);
                        colLabel.Move(0, y, 300, rowHeight); // MATTT : Hardcoded 300, need to measure!
                        gridContainer.AddControl(colLabel);
                        x = colLabel.Right;
                    }

                    switch (col.Type)
                    {
                        case ColumnType.CheckBox:
                            var checkBox = new CheckBox((bool)data[r, c]);
                            ctrl = checkBox;
                            break;
                        case ColumnType.Label:
                            var text = new Label((string)data[r, c]);
                            ctrl = text;
                            break;
                        case ColumnType.Slider:
                            var seek = new Slider((int)data[r, c], col.MinValue, col.MaxValue, 1, false /*, col.Formatter*/); // MATTT : Format!
                            ctrl = seek;
                            break;
                        case ColumnType.NumericUpDown:
                            var upDown = new NumericUpDown((int)data[r, c], col.MinValue, col.MaxValue, 1);
                            ctrl = upDown;
                            break;
                        case ColumnType.DropDown:
                            var dropDown = new DropDown(col.DropDownValues, Array.IndexOf(col.DropDownValues, (string)data[r, c]));
                            ctrl = dropDown;
                            break;
                        case ColumnType.Button:
                            var button = new Button(null, "...");
                            button.Border = true;
                            ctrl = button;
                            break;
                        default:
                            Debug.Assert(false);
                            break;
                    }

                    ctrl.Move(x, y, 700, rowHeight);
                    gridContainer.AddControl(ctrl);
                    y += rowHeight + rowMargin;

                    // MATTT : There was a different layout depending on noheader?
                    //gridLayout.AddView(view, CreateGridLayoutParams(rowIdx, noHeader ? c : 1));

                    prop.subControls[r * prop.columns.Length + c] = ctrl;

                    if (!noHeader) rowIdx++;
                }

                if (noHeader) rowIdx++;
            }

            // Will be re-added during build.
            container.RemoveControl(gridContainer);
            gridContainer.Resize(1000, y);

            properties.Add(prop);

            return properties.Count - 1;
        }

        public void UpdateGrid(int idx, object[,] data, string[] columnNames = null)
        {
            //var list = properties[idx].control as Grid;

            //list.UpdateData(data);

            //if (columnNames != null)
            //    list.RenameColumns(columnNames);
        }

        public void UpdateGrid(int idx, int rowIdx, int colIdx, object value)
        {
            //var grid = properties[idx].control as Grid;
            //grid.UpdateData(rowIdx, colIdx, value);
        }

        public void UpdateCheckBoxList(int idx, string[] values, bool[] selected)
        {
            // MATTT
            //var grid = properties[idx].control as Grid;
            //Debug.Assert(values.Length == grid.ItemCount);

            //for (int i = 0; i < values.Length; i++)
            //{
            //    grid.UpdateData(i, 0, selected != null ? selected[i] : true);
            //    grid.UpdateData(i, 1, values[i]);
            //}
        }

        public void UpdateCheckBoxList(int idx, bool[] selected)
        {
            // MATTT
            //var grid = properties[idx].control as Grid;
            //Debug.Assert(selected.Length == grid.ItemCount);

            //for (int i = 0; i < selected.Length; i++)
            //    grid.UpdateData(i, 0, selected[i]);
        }

        public T GetPropertyValue<T>(int idx, int rowIdx, int colIdx)
        {
            //var prop = properties[idx];
            //Debug.Assert(prop.type == PropertyType.Grid);
            //var grid = prop.control as Grid;
            //return (T)grid.GetData(rowIdx, colIdx);

            return default(T); // MATTT
        }

        public void SetPropertyValue(int idx, int rowIdx, int colIdx, object value)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.Grid:
                    //(prop.control as Grid).SetData(rowIdx, colIdx, value);
                    // MATTT
                    break;
            }
        }

        private object GetCheckBoxListValue(int idx)
        {
            var prop = properties[idx];
            var selected = new bool[prop.subControls.Length]; 
            // MATTT
            //for (int i = 0; i < grid.ItemCount; i++)
            //    selected[i] = (bool)grid.GetData(i, 0);
            return selected;
        }

        private int GetRadioListSelectedIndex(int idx)
        {
            //var grid = prop.control as Grid;

            //for (int i = 0; i < grid.ItemCount; i++)
            //{
            //    if ((bool)grid.GetData(i, 0) == true)
            //        return i;
            //}

            //Debug.Assert(false);
            //return -1;
            
            return 0; // MATTT
        }

        public int AddCheckBoxList(string label, string[] values, bool[] selected, string tooltip = null, int numRows = 7)
        {
            var prop = new Property();
            prop.type = PropertyType.CheckBoxList;
            prop.label = CreateLabel(label);
            
            var listContainer = new Container();
            listContainer.SetupClipRegion(false);

            prop.control = listContainer;
            prop.subControls = new Control[values.Length];

            // Need to add to be able to add sub controls.
            container.AddControl(listContainer);

            var rowHeight = DpiScaling.ScaleForWindow(16);

            for (int i = 0; i < values.Length; i++)
            {
                var checkBox = CreateCheckBox(selected == null ? true : selected[i], values[i]);
                checkBox.Move(0, i * rowHeight, 1000, rowHeight); // MATTT
                listContainer.AddControl(checkBox);
            }

            listContainer.Resize(1000, values.Length * rowHeight); // MATTT
            container.RemoveControl(listContainer);
            properties.Add(prop);

            return properties.Count - 1;
        }

        public int AddRadioButtonList(string label, string[] values, int selectedIndex, string tooltip = null, int numRows = 7)
        {
            var prop = new Property();
            prop.type = PropertyType.CheckBoxList;
            prop.label = CreateLabel(label);

            var listContainer = new Container();
            listContainer.SetupClipRegion(false);

            prop.control = listContainer;
            prop.subControls = new Control[values.Length];

            // Need to add to be able to add sub controls.
            container.AddControl(listContainer);

            var rowHeight = DpiScaling.ScaleForWindow(16);

            for (int i = 0; i < values.Length; i++)
            {
                var checkBox = CreateRadioButton(values[i], i == selectedIndex, false);
                checkBox.Move(0, i * rowHeight, 1000, rowHeight); // MATTT
                listContainer.AddControl(checkBox);
            }

            listContainer.Resize(1000, values.Length * rowHeight); // MATTT
            container.RemoveControl(listContainer);
            properties.Add(prop);

            return properties.Count - 1;
        }

        public void Build()
        {
            container.RemoveAllControls();

            var margin = DpiScaling.ScaleForWindow(4);
            var x = margin;
            var y = margin;
            var actualLayoutWidth = layoutWidth - margin * 2;

            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];

                if (!prop.visible)
                {
                    continue;
                }

                if (i == advancedPropertyStart)
                {
                    var advLabel = new Label(AdvancedPropsLabel);
                    advLabel.Move(x, y, actualLayoutWidth, 1);
                    advLabel.Font = container.Fonts.FontMediumBold;
                    advLabel.AutoSizeHeight();
                    container.AddControl(advLabel);
                    y = advLabel.Bottom + margin;

                    var advTooltip = new Label(AdvancedPropsTooltip);
                    advTooltip.Font = container.Fonts.FontVerySmall;
                    advTooltip.Multiline = true;
                    advTooltip.Move(x, y, actualLayoutWidth, 1);
                    container.AddControl(advTooltip);
                    y = advTooltip.Bottom + margin;
                }

                var tooltip = prop.control.ToolTip;
                if (string.IsNullOrEmpty(tooltip) && prop.label != null && !string.IsNullOrEmpty(prop.label.ToolTip))
                {
                    tooltip = prop.label.ToolTip;
                }

                if (prop.label != null)
                {
                    var localLayoutWidth = actualLayoutWidth;
                    var checkBox = prop.control is CheckBox;
                    var topY = y;

                    container.AddControl(prop.label);
                    container.AddControl(prop.control);

                    if (checkBox)
                    {
                        // MATTT : Hardcoded 96. 
                        localLayoutWidth -= 96;
                    }

                    prop.label.Move(x, y, localLayoutWidth, 1);
                    prop.label.Font = container.Fonts.FontMediumBold;
                    prop.label.AutoSizeHeight();
                    y = prop.label.Bottom + margin;

                    if (!string.IsNullOrEmpty(tooltip))
                    {
                        if (prop.tooltipLabel == null)
                        {
                            prop.tooltipLabel = new Label(tooltip);
                            prop.tooltipLabel.Font = container.Fonts.FontVerySmall;
                            prop.tooltipLabel.Multiline = true;
                            prop.tooltipLabel.Enabled = prop.control.Enabled;
                            prop.tooltipLabel.Move(x, y, localLayoutWidth, 1);
                            container.AddControl(prop.tooltipLabel);
                            y = prop.tooltipLabel.Bottom + margin;
                        }
                    }

                    if (!checkBox)
                    {
                        prop.control.Move(x, y, localLayoutWidth, prop.control.Height);
                        y = prop.control.Bottom + margin;
                    }
                    else
                    {
                        // MATTT : Hardcoded 64. 
                        // MATTT : Allow checkboxes without text to be centered to get a larger touch area.
                        // MATTT : Make checkboxes scale on mobile. 64 is too small.
                        prop.control.Move(x + actualLayoutWidth - 64, topY, 64, y - topY);
                    }
                }
                else
                {
                    prop.control.Move(x, y, actualLayoutWidth, prop.control.Height);
                    container.AddControl(prop.control);
                    y = prop.control.Bottom + margin;
                }

                //if (prop.warningIcon != null)
                //{
                //    prop.warningIcon.Move(
                //        x + actualLayoutWidth - prop.warningIcon.Width,
                //        y + totalHeight + (height - prop.warningIcon.Height) / 2);
                //    container.AddControl(prop.warningIcon);
                //}
            }

            layoutHeight = y;
        }
    }
}