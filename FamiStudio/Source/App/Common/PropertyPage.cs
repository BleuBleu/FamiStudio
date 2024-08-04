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
            public Control control;
            public ImageBox warningIcon;
            public bool visible = true;
            public bool forceKeepSize;
            public Label tooltipLabel; // Mobile only
            public PropertyFlags flags;
        };

        private int layoutWidth;
        private int layoutHeight;
        private List<Property> properties = new List<Property>();
        private Container container;

        public delegate void PropertyChangedDelegate(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value);
        public event PropertyChangedDelegate PropertyChanged;
        public delegate void PropertyWantsCloseDelegate(int idx);
        public event PropertyWantsCloseDelegate PropertyWantsClose;
        public delegate void PropertyClickedDelegate(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx);
        public event PropertyClickedDelegate PropertyClicked;
        public delegate void LayoutChangedDelegate(PropertyPage props);
        public event LayoutChangedDelegate LayoutChanged;

        private object userData;
        private int advancedPropertyStart = -1;
        private bool showWarnings = false;

        public object PropertiesUserData { get => userData; set => userData = value; }
        public bool HasAdvancedProperties { get => advancedPropertyStart > 0; }
        public bool ShowWarnings { get => showWarnings; set => showWarnings = value; }

        #region Localization

        LocalizedString NameLabel;
        LocalizedString ColorLabel;
        LocalizedString AdvancedPropsLabel;
        LocalizedString AdvancedPropsTooltip;

        #endregion

        public int LayoutWidth   => layoutWidth;
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
            Debug.Assert(!string.IsNullOrEmpty(str));
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

        private TextBox CreateTextBox(string prompt, string txt, int maxLength, string tooltip = null)
        {
            var textBox = new TextBox(txt, maxLength);
            textBox.ToolTip = tooltip;
            textBox.Prompt = prompt;

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
            colorPicker.SetDesiredWidth(layoutWidth);
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
            cb.RightAlign = Platform.IsMobile && string.IsNullOrEmpty(text);
            return cb;
        }

        private void Cb_CheckedChanged(Control sender, bool check)
        {
            int idx = GetPropertyIndexForControl(sender);
            PropertyChanged?.Invoke(this, idx, -1, -1, check);
        }

        private DropDown CreateDropDownList(string prompt, string[] values, string value, string tooltip = null)
        {
            var dropDown = new DropDown(values, Array.IndexOf(values, value));
            dropDown.SelectedIndexChanged += DropDown_SelectedIndexChanged;
            dropDown.ToolTip = tooltip;
            dropDown.Prompt = prompt;

            return dropDown;
        }

        private void DropDown_SelectedIndexChanged(Control sender, int index)
        {
            int idx = GetPropertyIndexForControl(sender);
            PropertyChanged?.Invoke(this, idx, -1, -1, GetPropertyValue(idx));
        }

        private Button CreateButton(string text, string tooltip)
        {
            var button = new Button(null, text);
            button.Border = true;
            button.Click += Button_Click;
            button.Resize(button.Width, DpiScaling.ScaleForWindow(Platform.IsMobile ? 20 : 32));
            button.ToolTip = tooltip;
            button.Ellipsis = true;
            return button;
        }

        private void Button_Click(Control sender)
        {
            var propIdx = GetPropertyIndexForControl(sender);
            PropertyClicked?.Invoke(this, ClickType.Button, propIdx, -1, -1);
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
                    control = CreateTextBox(label, value, maxLength, tooltip)
                });
            return properties.Count - 1;
        }

        public int AddFileTextBox(string label, string value, int maxLength = 0, string tooltip = null, PropertyFlags flags = PropertyFlags.None)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.TextBox,
                    label = label != null ? CreateLabel(label, tooltip, flags.HasFlag(PropertyFlags.MultiLineLabel)) : null,
                    control = CreateFileTextBox(value, maxLength, tooltip),
                    flags = flags
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
                    control = CreateLogTextBox(),
                    flags = PropertyFlags.ForceFullWidth
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
                    control = CreateColorPicker(color),
                    flags = PropertyFlags.ForceFullWidth
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

        public int AddRadioButton(string label, string text, bool check, bool multiline = false, PropertyFlags flags = PropertyFlags.ForceFullWidth)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Radio,
                    label = label != null ? CreateLabel(label, null, flags.HasFlag(PropertyFlags.MultiLineLabel)) : null,
                    control = CreateRadioButton(text, check, multiline),
                    flags = flags
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

        public int AddDropDownList(string label, string[] values, string value, string tooltip = null, PropertyFlags flags = PropertyFlags.None)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.DropDownList,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateDropDownList(label, values, value, tooltip),
                    flags = flags
                });
            return properties.Count - 1;
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
                prop.type == PropertyType.Grid           ||
                prop.type == PropertyType.Button);

            if (prop.label != null)
                prop.label.Enabled = enabled;

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
                    return GetCheckBoxListValue(idx);
                case PropertyType.Button:
                    return (prop.control as Button).Text;
            }

            return null;
        }

        public T GetPropertyValue<T>(int idx)
        {
            return (T)GetPropertyValue(idx);
        }

        public int GetSelectedIndex(int idx)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.DropDownList:
                    return (prop.control as DropDown).SelectedIndex;
                case PropertyType.RadioList:
                    return GetRadioListSelectedIndex(idx);
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
    }


    public enum PropertyType
    {
        TextBox,
        FileTextBox,
        ColoredTextBox,
        NumericUpDown,
        Slider,
        CheckBox,
        DropDownList,
        CheckBoxList,
        Grid,
        ColorPicker,
        Label,
        LinkLabel,
        Button,
        LogTextBox,
        ProgressBar,
        Radio,
        RadioList,
        ImageBox
    };

    public enum CommentType
    {
        Good,
        Warning,
        Error
    };

    public enum ColumnType
    {
        CheckBox,
        Radio,
        Label,
        Button,
        DropDown,
        Slider,
        Image,
        NumericUpDown
    };

    public enum ClickType
    {
        Left,
        Right,
        Double,
        Button
    };

    [Flags]
    public enum GridOptions
    {
        None = 0,
        NoHeader = 1,
        MobileTwoColumnLayout = 2
    }

    public class ColumnDesc
    {
        public string Name;
        public bool Enabled = true;
        public bool Ellipsis;
        public float Width = 0.0f;
        public ColumnType Type = ColumnType.Label;
        public string[] DropDownValues;
        public Func<object, string> Formatter = DefaultFormat;
        public int MinValue;
        public int MaxValue;

        private static string DefaultFormat(object o) => o.ToString();

        public ColumnDesc(string name, float width, ColumnType type = ColumnType.Label)
        {
            Debug.Assert(type != ColumnType.CheckBox || width == 0.0f);

            Name = name;
            Type = type;
            Width = type == ColumnType.CheckBox || type == ColumnType.Image ? 0.0f : width;
        }

        public ColumnDesc(string name, float width, string[] values)
        {
            Name = name;
            Type = ColumnType.DropDown;
            DropDownValues = values;
            Width = width;
        }

        public ColumnDesc(string name, float width, int min, int max)
        {
            Name = name;
            Type = ColumnType.NumericUpDown;
            Width = width;
            MinValue = min;
            MaxValue = max;
        }

        public ColumnDesc(string name, float width, int min, int max, Func<object, string> fmt)
        {
            Name = name;
            Type = ColumnType.Slider;
            Formatter = fmt;
            Width = width;
            MinValue = min;
            MaxValue = max;
        }
    }

    [Flags]
    public enum PropertyFlags
    {
        None = 0,
        ForceFullWidth = 1, // (Desktop only) Forces the control to be full-width by putting the label above.
        MultiLineLabel = 2, // (Desktop only) Will allow multiline on the label, implies "ForceFullWidth".
    }
}
