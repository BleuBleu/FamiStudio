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
            //var grid = properties[propIdx].control as Grid;
            //grid.OverrideCellSlider(rowIdx, colIdx, min, max, fmt);
        }

        private Grid CreateGrid(ColumnDesc[] columnDescs, object[,] data, string tooltip = null, GridOptions options = GridOptions.None)
        {
            var grid = new Grid(container, columnDescs, data, options);
            grid.ToolTip = tooltip;
            // MATTT : Hook up events.
            return grid;
        }

        public int AddGrid(string label, ColumnDesc[] columnDescs, object[,] data, int rows = 7, string tooltip = null, GridOptions options = GridOptions.None, PropertyFlags flags = PropertyFlags.ForceFullWidth)
        {
            // We need initial data on mobile.
            Debug.Assert(data != null);

            // MATTT : Grid gets built mostly in constructor, but the layout happens in Build(), what do we do?
            var prop = new Property();
            prop.type = PropertyType.Grid;
            prop.label = label != null ? CreateLabel(label, tooltip) : null;
            prop.control = CreateGrid(columnDescs, data, tooltip, options);
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
            var checkList = properties[idx].control as CheckBoxList;
            return checkList.Values;
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

        private CheckBoxList CreateCheckBoxList(string[] values, bool[] selected, string tooltip = null)
        {
            var checkList = new CheckBoxList(values, selected);
            checkList.ToolTip = tooltip;
            // MATTT : Hook events.
            return checkList;
        }

        public int AddCheckBoxList(string label, string[] values, bool[] selected, string tooltip = null, int numRows = 7, PropertyFlags flags = PropertyFlags.ForceFullWidth)
        {
            var prop = new Property();
            prop.type = PropertyType.CheckBoxList;
            prop.label = CreateLabel(label);
            prop.control = CreateCheckBoxList(values, selected, tooltip);
            properties.Add(prop);

            return properties.Count - 1;
        }

        private RadioButtonList CreateRadioButtonList(string[] values, int selectedIndex, string tooltip = null)
        {
            var radioList = new RadioButtonList(values, selectedIndex);
            radioList.ToolTip = tooltip;
            // MATTT : Hook events.
            return radioList;
        }

        public int AddRadioButtonList(string label, string[] values, int selectedIndex, string tooltip = null, int numRows = 7, PropertyFlags flags = PropertyFlags.ForceFullWidth)
        {
            var prop = new Property();
            prop.type = PropertyType.CheckBoxList;
            prop.label = CreateLabel(label);
            prop.control = CreateRadioButtonList(values, selectedIndex, tooltip);
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
            }

            layoutHeight = y;
        }
    }
}