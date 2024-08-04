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
            var grid = properties[propIdx].control as Grid;
            grid.SetColumnEnabled(colIdx, enabled);
        }

        public void SetRowColor(int propIdx, int rowIdx, Color color)
        {
            //var grid = properties[propIdx].control as Grid;
            //grid.SetRowColor(rowIdx, color);
        }

        public void OverrideCellSlider(int propIdx, int rowIdx, int colIdx, int min, int max, Func<object, string> fmt)
        {
            var grid = properties[propIdx].control as Grid;
            grid.OverrideCellSlider(rowIdx, colIdx, min, max, fmt);
        }

        private Grid CreateGrid(ColumnDesc[] columnDescs, object[,] data, string tooltip = null, GridOptions options = GridOptions.None)
        {
            var grid = new Grid(container, columnDescs, data, options);
            grid.ToolTip = tooltip;
            grid.ValueChanged += Grid_ValueChanged;
            grid.ButtonPressed += Grid_ButtonPressed;
            return grid;
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

        public int AddGrid(string label, ColumnDesc[] columnDescs, object[,] data, int rows = 7, string tooltip = null, GridOptions options = GridOptions.None, PropertyFlags flags = PropertyFlags.ForceFullWidth)
        {
            // We need initial data on mobile.
            Debug.Assert(data != null);

            var prop = new Property();
            prop.type = PropertyType.Grid;
            prop.label = label != null ? CreateLabel(label, tooltip) : null;
            prop.control = CreateGrid(columnDescs, data, tooltip, options);
            properties.Add(prop);

            return properties.Count - 1;
        }

        public void UpdateGrid(int idx, object[,] data, string[] columnNames = null)
        {
            var grid = properties[idx].control as Grid;
            var rebuild = data.Length != grid.Data.Length;

            grid.UpdateData(data);

            if (columnNames != null)
            {
                grid.RenameColumns(columnNames);
            }

            if (rebuild)
            {
                Build();
            }
        }

        public void UpdateGrid(int idx, int rowIdx, int colIdx, object value)
        {
            var grid = properties[idx].control as Grid;
            grid.UpdateData(rowIdx, colIdx, value);
        }

        public void UpdateCheckBoxList(int idx, string[] values, bool[] selected)
        {
            // This doesnt seem to be used?
            Debug.Assert(false);
        }

        public void UpdateCheckBoxList(int idx, bool[] selected)
        {
            // This doesnt seem to be used?
            Debug.Assert(false);
        }

        public T GetPropertyValue<T>(int idx, int rowIdx, int colIdx)
        {
            var grid = properties[idx].control as Grid;
            return (T)grid.GetData(rowIdx, colIdx);
        }
        
        public void SetPropertyEnabled(int idx, int rowIdx, int colIdx, bool enabled)
        {
            var grid = properties[idx].control as Grid;
            grid.SetCellEnabled(rowIdx, colIdx, enabled);
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
            var checkList = properties[idx].control as CheckBoxList;
            return checkList.Values;
        }

        private int GetRadioListSelectedIndex(int idx)
        {
            var prop = properties[idx];
            return (prop.control as RadioButtonList).SelectedIndex;
        }

        public void ClearRadioList(int idx)
        {
            var prop = properties[idx];
            (prop.control as RadioButtonList).SelectedIndex = -1;
        }

        public void UpdateRadioButtonList(int idx, string[] values, int selectedIndex)
        {
            var prop = properties[idx];
            prop.control = CreateRadioButtonList(values, selectedIndex);
            Build();
        }

        private CheckBoxList CreateCheckBoxList(string[] values, bool[] selected, string tooltip = null)
        {
            var checkList = new CheckBoxList(values, selected);
            checkList.ToolTip = tooltip;
            checkList.CheckedChanged += CheckList_CheckedChanged;
            return checkList;
        }

        private void CheckList_CheckedChanged(Control sender, int index, bool value)
        {
            var propIdx = GetPropertyIndexForControl(sender);
            PropertyChanged?.Invoke(this, propIdx, index, 0, value);
        }

        public int AddCheckBoxList(string label, string[] values, bool[] selected, string tooltip = null, int numRows = 7, PropertyFlags flags = PropertyFlags.ForceFullWidth)
        {
            var prop = new Property();
            prop.type = PropertyType.CheckBoxList;
            prop.label = label != null ? CreateLabel(label) : null;
            prop.control = CreateCheckBoxList(values, selected, tooltip);
            properties.Add(prop);

            return properties.Count - 1;
        }

        private RadioButtonList CreateRadioButtonList(string[] values, int selectedIndex, string tooltip = null)
        {
            var radioList = new RadioButtonList(values, selectedIndex);
            radioList.ToolTip = tooltip;
            radioList.RadioChanged += RadioList_RadioChanged;
            return radioList;
        }

        private void RadioList_RadioChanged(Control sender, int index)
        {
            var propIdx = GetPropertyIndexForControl(sender);
            PropertyChanged?.Invoke(this, propIdx, index, 0, index);
        }

        public int AddRadioButtonList(string label, string[] values, int selectedIndex, string tooltip = null, int numRows = 7, PropertyFlags flags = PropertyFlags.ForceFullWidth)
        {
            var prop = new Property();
            prop.type = PropertyType.RadioList;
            prop.label = CreateLabel(label);
            prop.control = CreateRadioButtonList(values, selectedIndex, tooltip);
            properties.Add(prop);

            return properties.Count - 1;
        }

        public void Build(int newLayoutWidth = -1)
        {
            container.RemoveAllControls();

            if (newLayoutWidth > 0)
            {
                layoutWidth = newLayoutWidth;
            }

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
                    var checkBox = prop.control is CheckBox c && string.IsNullOrEmpty(c.Text);
                    var topY = y;

                    container.AddControl(prop.label);
                    container.AddControl(prop.control);

                    if (checkBox)
                    {
                        localLayoutWidth -= prop.control.Height;
                    }

                    prop.label.Move(x, y, localLayoutWidth, 1);
                    prop.label.Font = container.Fonts.FontMediumBold;
                    prop.label.AutoSizeHeight();
                    y = prop.label.Bottom + margin;

                    if (!string.IsNullOrEmpty(tooltip))
                    {
                        prop.tooltipLabel = new Label(tooltip);
                        prop.tooltipLabel.Font = container.Fonts.FontVerySmall;
                        prop.tooltipLabel.Multiline = true;
                        prop.tooltipLabel.Enabled = prop.control.Enabled;
                        prop.tooltipLabel.Move(x, y, localLayoutWidth, 1);
                        container.AddControl(prop.tooltipLabel);
                        y = prop.tooltipLabel.Bottom + margin;
                    }

                    if (!checkBox)
                    {

                        if (prop.control is ColorPicker colorPicker)
                        {
                            var res = Platform.GetScreenResolution();
                            colorPicker.SetDesiredWidth(localLayoutWidth, Math.Min(res.Width, res.Height) / 2);
                            colorPicker.Move(x, y);
                        }
                        else
                        {
                            prop.control.Move(x, y, localLayoutWidth, prop.control.Height);
                        }
                        y = prop.control.Bottom + margin;
                    }
                    else
                    {
                        var checkBoxSize = prop.control.Height;
                        prop.control.Move(x + actualLayoutWidth - checkBoxSize, topY, checkBoxSize, y - topY);
                    }
                }
                else
                {
                    prop.control.Move(x, y, actualLayoutWidth, prop.control.Height);
                    container.AddControl(prop.control);
                    y = prop.control.Bottom + margin;
                }
            }

            layoutHeight = y;
            LayoutChanged?.Invoke(this);
        }
    }
}