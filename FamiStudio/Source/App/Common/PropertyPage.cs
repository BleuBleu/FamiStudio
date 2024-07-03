using Android.Widget;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Xml.Linq;

namespace FamiStudio
{
    public partial class PropertyPage
    {
        class Property
        {
            public PropertyType type;
            public Label label;
            public Label tooltipLabel; // Mobile only
            public Control control;
            public ImageBox warningIcon;
            public bool visible = true;
            public bool forceKeepSize;
        };

        private int layoutWidth;
        private int layoutHeight;
        private List<Property> properties = new List<Property>();
        private Container container;

        #region Localization

        LocalizedString NameLabel;
        LocalizedString ColorLabel;
        LocalizedString AdvancedPropsLabel;
        LocalizedString AdvancedPropsTooltip;

        #endregion

        private readonly static string[] WarningIcons = 
        {
            "WarningGood",
            "WarningYellow",
            "Warning"
        };

        public int LayoutHeight  => layoutHeight;
        public int PropertyCount => properties.Count;

        public PropertyPage(Container cont, int width)
        {
            Localization.Localize(this);
            layoutWidth = width;
            container = cont;
        }

        public void ConditionalSetTextBoxFocus()
        {
            if (properties.Count > 0)
            {
                var prop = properties[0];
                prop.control.ClearDialogFocus();

                if (prop.type == PropertyType.TextBox || 
                    prop.type == PropertyType.ColoredTextBox || 
                    prop.type == PropertyType.NumericUpDown)
                {
                    (prop.control as TextBox).SelectAll();
                    prop.control.GrabDialogFocus();
                }
            }
        }

        private int GetPropertyIndexForControl(Control ctrl)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                if (properties[i].control == ctrl)
                {
                    return i;
                }
            }

            return -1;
        }

        private Label CreateLabel(string str, string tooltip = null, bool multiline = false)
        {
            //Debug.Assert(!string.IsNullOrEmpty(str)); // MATTT
            if (string.IsNullOrEmpty(str))
            {
                str = "FIX ME!!!!!!!!!";
            }

            var label = new Label(str, multiline);
            label.ToolTip = tooltip;
            return label;
        }

        private LinkLabel CreateLinkLabel(string str, string url, string tooltip = null)
        {
            var label = new LinkLabel(str, url);
            label.ToolTip = tooltip;
            return label;
        }

        private TextBox CreateColoredTextBox(string txt, Color backColor)
        {
            var textBox = new TextBox(txt);
            textBox.BackColor = backColor;
            textBox.ForeColor = Theme.BlackColor;
            //textBox.DisabledColor = Theme.BlackColor;
            textBox.SelectionColor = Theme.Darken(backColor);
            return textBox;
        }

        private TextBox CreateTextBox(string txt, int maxLength, string tooltip = null)
        {
            var textBox = new TextBox(txt, maxLength);
            textBox.ToolTip = tooltip;

            return textBox;
        }

        private TextBox CreateFileTextBox(string txt, int maxLength, string tooltip = null)
        {
            var textBox = new FileTextBox(txt, maxLength);
            textBox.ToolTip = tooltip;
            textBox.Click += FileTextBox_Click;

            return textBox;
        }

        private void FileTextBox_Click(Control sender)
        {
            var propIdx = GetPropertyIndexForControl(sender);
            PropertyClicked?.Invoke(this, ClickType.Button, propIdx, -1, -1);
        }

        private LogTextBox CreateLogTextBox()
        {
            return new LogTextBox(15);
        }

        private ColorPicker CreateColorPicker(Color color)
        {
            var colorPicker = new ColorPicker(color);
            colorPicker.SetNiceSize(layoutWidth);
            colorPicker.ColorChanged += ColorPicker_ColorChanged;
            colorPicker.DoubleClicked += ColorPicker_ColorDoubleClicked;
            return colorPicker;
        }

        private void ColorPicker_ColorChanged(Control sender, Color color)
        {
            foreach (var prop in properties)
            {
                if (prop.type == PropertyType.ColoredTextBox)
                {
                    (prop.control as TextBox).BackColor = color;
                    (prop.control as TextBox).SelectionColor = Theme.Darken(color);
                }
            }
        }

        private void ColorPicker_ColorDoubleClicked(Control sender)
        {
            PropertyWantsClose?.Invoke(GetPropertyIndexForControl(sender));
        }

        private ImageBox CreateImageBox(string image)
        {
            return new ImageBox(image);
        }

        private NumericUpDown CreateNumericUpDown(int value, int min, int max, int inc, string tooltip = null)
        {
            var upDown = new NumericUpDown(value, min, max, inc);

            upDown.ValueChanged += UpDown_ValueChanged;
            upDown.ToolTip = tooltip;

            return upDown;
        }

        private ProgressBar CreateProgressBar()
        {
            var progress = new ProgressBar();
            return progress;
        }

        private RadioButton CreateRadioButton(string text, bool check, bool multiline)
        {
            var radio = new RadioButton(text, check, multiline);
            return radio;
        }

        private void UpDown_ValueChanged(Control sender, int val)
        {
            int idx = GetPropertyIndexForControl(sender);
            PropertyChanged?.Invoke(this, idx, -1, -1, val);
        }

        private CheckBox CreateCheckBox(bool value, string text = "", string tooltip = null)
        {
            var cb = new CheckBox(value, text);
            cb.CheckedChanged += Cb_CheckedChanged;
            cb.ToolTip = tooltip;
            return cb;
        }

        private void Cb_CheckedChanged(Control sender, bool check)
        {
            int idx = GetPropertyIndexForControl(sender);
            PropertyChanged?.Invoke(this, idx, -1, -1, check);
        }

        private DropDown CreateDropDownList(string[] values, string value, string tooltip = null)
        {
            var dropDown = new DropDown(values, Array.IndexOf(values, value));
            dropDown.SelectedIndexChanged += DropDown_SelectedIndexChanged;
            dropDown.ToolTip = tooltip;

            return dropDown;
        }

        private void DropDown_SelectedIndexChanged(Control sender, int index)
        {
            int idx = GetPropertyIndexForControl(sender);
            PropertyChanged?.Invoke(this, idx, -1, -1, GetPropertyValue(idx));
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
            grid.ValueChanged += Grid_ValueChanged;
            grid.ToolTip = tooltip;

            return grid;
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
            grid.ValueChanged += Grid_ValueChanged;
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

        private Button CreateButton(string text, string tooltip)
        {
            var button = new Button(null, text);
            button.Border = true;
            button.Click += Button_Click;
            button.Resize(button.Width, DpiScaling.ScaleForWindow(32));
            button.ToolTip = tooltip;
            button.Ellipsis = true;
            return button;
        }

        private void Button_Click(Control sender)
        {
            var propIdx = GetPropertyIndexForControl(sender);
            PropertyClicked?.Invoke(this, ClickType.Button, propIdx, -1, -1);
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

        public int AddColoredTextBox(string value, Color color)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.ColoredTextBox,
                    label = Platform.IsMobile ? CreateLabel(NameLabel) : null,
                    control = CreateColoredTextBox(value, color)
                });
            return properties.Count - 1;
        }

        public int AddTextBox(string label, string value, int maxLength = 0, bool numeric = false, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.TextBox,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateTextBox(value, maxLength, tooltip)
                });
            return properties.Count - 1;
        }

        public int AddFileTextBox(string label, string value, int maxLength = 0, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.TextBox,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateFileTextBox(value, maxLength, tooltip)
                });
            return properties.Count - 1;
        }

        public int AddLogTextBox(string label)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.LogTextBox,
                    label = label != null ? CreateLabel(label) : null,
                    control = CreateLogTextBox()
                });
            return properties.Count - 1;
        }

        public ImageBox CreateImageBox(Texture bmp, int resX, int resY)
        {
            var image = new ImageBox(bmp);
            image.Resize(resX, resY);
            return image;
        }

        public int AddButton(string label, string value, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Button,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateButton(value, tooltip)
                });
            return properties.Count - 1;
        }

        public int AddLabel(string label, string value, bool multiline = false, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Label,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateLabel(value, tooltip, multiline)
                });
            return properties.Count - 1;
        }

        public int AddLinkLabel(string label, string value, string url, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.LinkLabel,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateLinkLabel(value, url, tooltip)
                });
            return properties.Count - 1;
        }

        public int AddColorPicker(Color color)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.ColorPicker,
                    label = Platform.IsMobile ? CreateLabel(ColorLabel) : null,
                    control = CreateColorPicker(color)
                });
            return properties.Count - 1;
        }

        public int AddNumericUpDown(string label, int value, int min, int max, int increment, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.NumericUpDown,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateNumericUpDown(value, min, max, increment, tooltip)
                });
            return properties.Count - 1;
        }

        public int AddProgressBar(string label)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.ProgressBar,
                    label = label != null ? CreateLabel(label) : null,
                    control = CreateProgressBar()
                });
            return properties.Count - 1;
        }

        public int AddRadioButton(string label, string text, bool check, bool multiline = false)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Radio,
                    label = label != null ? CreateLabel(label) : null,
                    control = CreateRadioButton(text, check, multiline)
                });
            return properties.Count - 1;
        }

        public void UpdateIntegerRange(int idx, int min, int max)
        {
            var upDown = (properties[idx].control as NumericUpDown);

            upDown.Minimum = min;
            upDown.Maximum = max;
        }

        public void UpdateIntegerRange(int idx, int value, int min, int max)
        {
            var upDown = (properties[idx].control as NumericUpDown);

            upDown.Minimum = min;
            upDown.Maximum = max;
            upDown.Value = value;
        }

        public void SetLabelText(int idx, string text)
        {
            (properties[idx].control as Label).Text = text;
        }

        public void SetDropDownListIndex(int idx, int selIdx)
        {
            (properties[idx].control as DropDown).SelectedIndex = selIdx;
        }

        public void UpdateDropDownListItems(int idx, string[] values)
        {
            var dd = (properties[idx].control as DropDown);
            dd.SetItems(values);
        }

        public int AddCheckBox(string label, bool value, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.CheckBox,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateCheckBox(value, "", tooltip)
                });
            return properties.Count - 1;
        }

        public int AddLabelCheckBox(string label, bool value, int margin = 0, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.CheckBox,
                    control = CreateCheckBox(value, label, tooltip),
                });
            return properties.Count - 1;
        }

        public int AddDropDownList(string label, string[] values, string value, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.DropDownList,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateDropDownList(values, value, tooltip)
                });
            return properties.Count - 1;
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

        public void ClearRadioList(int idx)
        {
            Debug.Assert(false);

            //var prop = properties[idx];
            //Debug.Assert(prop.type == PropertyType.RadioList);

            //var group = prop.layout as RadioGroup;
            //if (group != null)
            //    group.ClearCheck();
        }

        public void UpdateRadioButtonList(int idx, string[] values, int selectedIndex)
        {
            Debug.Assert(false);

            //var prop = properties[idx];
            //var radioGroup = prop.layout as RadioGroup;

            //radioGroup.RemoveAllViews();

            //for (int i = 0; i < values.Length; i++)
            //{
            //    var radio = CreateRadioButton(values[i], Resource.Style.LightGrayCheckBox);
            //    radio.Checked = i == selectedIndex;
            //    radio.Id = i;
            //    prop.controls.Add(radio);
            //    prop.layout.AddView(radio);
            //}
        }

        public int AddImageBox(string label, string tooltip = null)
        {
            return -1;
        }

        private Slider CreateSlider(double value, double min, double max, double increment, int numDecimals, bool showLabel, string format = "{0}", string tooltip = null)
        {
            var slider = new Slider(value, min, max, increment, showLabel, format);
            slider.ValueChanged += Slider_ValueChanged;
            slider.ToolTip = tooltip;
            return slider;
        }

        private void Slider_ValueChanged(Control slider, double value)
        {
            var idx = GetPropertyIndexForControl(slider);
            PropertyChanged?.Invoke(this, idx, -1, -1, value);
        }

        public int AddSlider(string label, double value, double min, double max, double increment, int numDecimals, string format = "{0}", string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Slider,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateSlider(value, min, max, increment, numDecimals, format != null, format, tooltip),
                });
            return properties.Count - 1;
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

        public void SetPropertyEnabled(int idx, bool enabled)
        {
            var prop = properties[idx];

            // TODO : I only added support to disable these so far.
            Debug.Assert(                                
                prop.type == PropertyType.Label          ||
                prop.type == PropertyType.CheckBox       ||
                prop.type == PropertyType.DropDownList   ||
                prop.type == PropertyType.NumericUpDown  ||
                prop.type == PropertyType.TextBox        ||
                prop.type == PropertyType.Slider         ||
                prop.type == PropertyType.ColoredTextBox ||
                prop.type == PropertyType.Grid);

            if (prop.label != null)
                prop.label.Enabled = enabled;

            // MATTT : Also do it when creating the labels, this only works when disabling a property after Build().
            if (prop.tooltipLabel != null)
                prop.tooltipLabel.Enabled = enabled;

            prop.control.Enabled = enabled;
        }

        public void SetPropertyVisible(int idx, bool visible)
        {
            properties[idx].visible = visible;
        }

        public void AppendText(int idx, string line)
        {
            var textBox = properties[idx].control as LogTextBox;
            textBox.AddLine(line);
        }

        public void BeginAdvancedProperties()
        {
            advancedPropertyStart = properties.Count;
        }

        public void SetPropertyWarning(int idx, CommentType type, string comment)
        {
            // MATTT : Split in desktop/mobile files.
            if (Platform.IsDesktop)
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
        }

        public object GetPropertyValue(int idx)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.TextBox:
                case PropertyType.FileTextBox:
                case PropertyType.ColoredTextBox:
                case PropertyType.LogTextBox:
                    return (prop.control as TextBox).Text;
                case PropertyType.NumericUpDown:
                    return (int)(prop.control as NumericUpDown).Value;
                case PropertyType.Slider:
                    return (prop.control as Slider).Value;
                case PropertyType.Radio:
                    return (prop.control as RadioButton).Checked;
                case PropertyType.CheckBox:
                    return (prop.control as CheckBox).Checked;
                case PropertyType.ColorPicker:
                    return (prop.control as ColorPicker).SelectedColor;
                case PropertyType.DropDownList:
                    return (prop.control as DropDown).Text;
                case PropertyType.CheckBoxList:
                {
                    var grid = prop.control as Grid;
                    var selected = new bool[grid.ItemCount];
                    for (int i = 0; i < grid.ItemCount; i++)
                        selected[i] = (bool)grid.GetData(i, 0);
                    return selected;
                }
                case PropertyType.Button:
                    return (prop.control as Button).Text;
            }

            return null;
        }

        public T GetPropertyValue<T>(int idx)
        {
            return (T)GetPropertyValue(idx);
        }

        public T GetPropertyValue<T>(int idx, int rowIdx, int colIdx)
        {
            var prop = properties[idx];
            Debug.Assert(prop.type == PropertyType.Grid);
            var grid = prop.control as Grid;
            return (T)grid.GetData(rowIdx, colIdx);
        }

        public int GetSelectedIndex(int idx)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.DropDownList:
                    return (prop.control as DropDown).SelectedIndex;
                case PropertyType.RadioList:
                {
                    var grid = prop.control as Grid;

                    for (int i = 0; i < grid.ItemCount; i++)
                    {
                        if ((bool)grid.GetData(i, 0) == true)
                            return i;
                    }

                    Debug.Assert(false);
                    return -1;
                }
            }

            return -1;
        }

        public void SetPropertyValue(int idx, object value)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.CheckBox:
                    (prop.control as CheckBox).Checked = (bool)value;
                    break;
                case PropertyType.Button:
                    (prop.control as Button).Text = (string)value;
                    break;
                case PropertyType.LogTextBox:
                case PropertyType.FileTextBox:
                case PropertyType.TextBox:
                    (prop.control as TextBox).Text = (string)value;
                    break;
                case PropertyType.ProgressBar:
                    (prop.control as ProgressBar).Progress = (float)value;
                    break;
                case PropertyType.Slider:
                    (prop.control as Slider).Value = (double)value;
                    break;
                case PropertyType.NumericUpDown:
                    (prop.control as NumericUpDown).Value = (int)value;
                    break;
            }
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
        
        // MATTT : Move to a separate file.
        public void BuildMobile()
        {
            container.RemoveAllControls();

            var margin = DpiScaling.ScaleForWindow(4);
            var warningWidth = 0; // MATTT showWarnings ? DpiScaling.ScaleForWindow(16) + margin : 0;

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

                //if (i > 0)
                //{
                //    var line = new HorizontalLine();
                //    line.Move(0, y, layoutWidth, margin);
                //    container.AddControl(line);
                //    y += margin;
                //}

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
                    container.AddControl(prop.label);
                    container.AddControl(prop.control);

                    prop.label.Move(x, y, actualLayoutWidth, 1);
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
                            prop.tooltipLabel.Move(x, y, actualLayoutWidth, 1);
                            container.AddControl(prop.tooltipLabel);
                            y = prop.tooltipLabel.Bottom + margin;
                        }
                    }

                    prop.control.Move(x, y, actualLayoutWidth, prop.control.Height);
                    y = prop.control.Bottom + margin;
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

        public void Build(bool advanced = false)
        {
            // MATTT : Move to a separate file.
            if (Platform.IsMobile)
            {
                BuildMobile();
                return;
            }

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
