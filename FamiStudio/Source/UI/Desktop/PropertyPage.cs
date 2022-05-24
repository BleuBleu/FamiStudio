using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;

using RenderBitmapAtlasRef = FamiStudio.GLBitmapAtlasRef;
using RenderBrush          = FamiStudio.GLBrush;
using RenderGeometry       = FamiStudio.GLGeometry;
using RenderControl        = FamiStudio.GLControl;
using RenderGraphics       = FamiStudio.GLGraphics;
using RenderCommandList    = FamiStudio.GLCommandList;

namespace FamiStudio
{
    public partial class PropertyPage
    {
        class Property
        {
            public PropertyType type;
            public Label2 label;
            public RenderControl control;
            public ImageBox2 warningIcon;
            public bool visible = true;
        };

        private int baseX;
        private int baseY;
        private int layoutWidth;
        private int layoutHeight;
        private List<Property> properties = new List<Property>();
        private Dialog dialog;

        private readonly static string[] WarningIcons = 
        {
            "WarningGood",
            "WarningYellow",
            "Warning"
        };

        public int LayoutHeight  => layoutHeight;
        public int PropertyCount => properties.Count;

        public PropertyPage(Dialog dlg, int x, int y, int width)
        {
            baseX = x;
            baseY = y;
            layoutWidth = width;
            dialog = dlg;
        }

        public bool Visible
        {
            set
            {
                foreach (var prop in properties)
                {
                    if (prop.label != null)
                        prop.label.Visible = value;
                    if (prop.control != null)
                        prop.control.Visible = value;
                }
            }
        }

        private int GetPropertyIndexForControl(RenderControl ctrl)
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

        private Label2 CreateLabel(string str, string tooltip = null, bool multiline = false)
        {
            Debug.Assert(!string.IsNullOrEmpty(str));

            var label = new Label2(str, multiline);
            //toolTip.SetToolTip(label, SplitLongTooltip(tooltip));

            return label;
        }

        private LinkLabel2 CreateLinkLabel(string str, string url, string tooltip = null)
        {
            var label = new LinkLabel2(str, url);
            //toolTip.SetToolTip(label, SplitLongTooltip(tooltip));
            return label;
        }

        private void Label_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var link = sender as LinkLabel;
            PlatformUtils.OpenUrl(link.Links[0].LinkData as string);
        }

        private TextBox2 CreateColoredTextBox(string txt, Color backColor)
        {
            var textBox = new TextBox2(txt);
            textBox.BackColor = backColor;
            textBox.ForeColor = Color.Black;
            return textBox;
        }

        private TextBox2 CreateTextBox(string txt, int maxLength, string tooltip = null)
        {
            var textBox = new TextBox2(txt);

            //textBox.MaxLength = maxLength;
            //textBox.TextChanged += TextBox_TextChanged;
            //toolTip.SetToolTip(textBox, SplitLongTooltip(tooltip));

            return textBox;
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            ForceTextBoxASCII(sender as TextBox2);
        }

        private void ForceTextBoxASCII(TextBox2 textBox)
        {
            // All of our text storage is ASCII at the moment, so enforce it right away
            // to prevent issues later on.
            var oldText = textBox.Text;
            var newText = Utils.ForceASCII(oldText);

            if (oldText != newText)
            {
                // MATTT
                //var selStart = textBox.SelectionStart;
                //var selLen = textBox.SelectionLength;

                //textBox.Text = newText;
                //textBox.SelectionStart = selStart;
                //textBox.SelectionLength = selLen;
            }
        }

        private TextBox CreateMultilineTextBox(string txt)
        {
            var textBox = new TextBox();

            //textBox.Font = new Font(PlatformUtils.PrivateFontCollection.Families[0], 8.0f, FontStyle.Regular);
            //textBox.Text = txt;
            //textBox.BackColor = Theme.DarkGreyFillColor1;
            //textBox.BorderStyle = BorderStyle.FixedSingle;
            //textBox.ForeColor = Theme.LightGreyFillColor2;
            //textBox.Location = new Point(5, 5);
            //textBox.Multiline = true;
            //textBox.ReadOnly = true;
            //textBox.Height = DpiScaling.ScaleForMainWindow(300);
            //textBox.ScrollBars = ScrollBars.Vertical;
            //textBox.Select(0, 0);
            //textBox.GotFocus += TextBox_GotFocus;

            return textBox;
        }

        //private void TextBox_GotFocus(object sender, EventArgs e)
        //{
        //    HideCaret((sender as TextBox).Handle);
        //}

        private ColorPicker2 CreateColorPicker(Color color)
        {
            var colorPicker = new ColorPicker2(color);
            colorPicker.SetNiceSize(layoutWidth);
            colorPicker.ColorChanged += ColorPicker_ColorChanged;
            return colorPicker;
        }

        private void ColorPicker_ColorChanged(RenderControl sender, Color color)
        {
            foreach (var prop in properties)
            {
                if (prop.type == PropertyType.ColoredTextBox)
                {
                    (prop.control as TextBox2).BackColor = color;
                }
            }
        }

        private ImageBox2 CreateImageBox(string image)
        {
            var imageBox = new ImageBox2(image);
            return imageBox;
        }

        private void ChangeColor(PictureBox pictureBox, int x, int y)
        {
            int i = Math.Min(Theme.CustomColors.GetLength(0) - 1, Math.Max(0, (int)(x / (float)pictureBox.Width  * Theme.CustomColors.GetLength(0))));
            int j = Math.Min(Theme.CustomColors.GetLength(1) - 1, Math.Max(0, (int)(y / (float)pictureBox.Height * Theme.CustomColors.GetLength(1))));

            //foreach (var prop in properties)
            //{
            //    if (prop.type == PropertyType.ColoredTextBox)
            //    {
            //        prop.control.BackColor = Theme.CustomColors[i, j];
            //    }
            //}

            pictureBox.BackColor = Theme.CustomColors[i, j];
        }

        private void PictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Left)
            //    ChangeColor(sender as PictureBox, e.X, e.Y);

            //PropertyWantsClose?.Invoke(GetPropertyIndexForControl(sender as Control));
        }

        private void PictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ChangeColor(sender as PictureBox, e.X, e.Y);
        }

        private void PictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ChangeColor(sender as PictureBox, e.X, e.Y);
        }

        private NumericUpDown2 CreateNumericUpDown(int value, int min, int max, string tooltip = null)
        {
            var upDown = new NumericUpDown2(value, min, max);

            //upDown.ValueChanged += UpDown_ValueChanged;
            //toolTip.SetToolTip(upDown, SplitLongTooltip(tooltip));

            return upDown;
        }

        private ProgressBar2 CreateProgressBar()
        {
            var progress = new ProgressBar2();
            return progress;
        }

        private RadioButton2 CreateRadioButton(string text, bool check, bool multiline)
        {
            var radio = new RadioButton2(text, check, multiline);

            //radio.Font = font;
            //radio.ForeColor = Theme.LightGreyFillColor2;
            //radio.Text = text;
            //radio.AutoSize = false;
            //radio.Checked = check;
            //radio.Padding = new Padding(DpiScaling.ScaleForMainWindow(16), 0, 0, 0);

            return radio;
        }

        private void UpDown_ValueChanged(object sender, EventArgs e)
        {
            //int idx = GetPropertyIndexForControl(sender as Control);
            //PropertyChanged?.Invoke(this, idx, -1, -1, GetPropertyValue(idx));
        }

        private CheckBox2 CreateCheckBox(bool value, string text = "", string tooltip = null)
        {
            var cb = new CheckBox2(value, text);
            //cb.CheckedChanged += Cb_CheckedChanged;
            //toolTip.SetToolTip(cb, SplitLongTooltip(tooltip));
            return cb;
        }

        private void Cb_CheckedChanged(object sender, EventArgs e)
        {
            //int idx = GetPropertyIndexForControl(sender as Control);
            //PropertyChanged?.Invoke(this, idx, -1, -1, GetPropertyValue(idx));
        }

        private DropDown2 CreateDropDownList(string[] values, string value, string tooltip = null)
        {
            var dropDown = new DropDown2(values, Array.IndexOf(values, value));
            dropDown.SelectedIndexChanged += DropDown_SelectedIndexChanged;
            //cb.SelectedIndexChanged += Cb_SelectedIndexChanged;
            //toolTip.SetToolTip(cb, SplitLongTooltip(tooltip));

            return dropDown;
        }

        private void DropDown_SelectedIndexChanged(RenderControl sender, int index)
        {
            int idx = GetPropertyIndexForControl(sender);
            PropertyChanged?.Invoke(this, idx, -1, -1, GetPropertyValue(idx));
        }

        private void Cb_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private object CreateCheckedListBox(string[] values, bool[] selected, string tooltip = null, int height = 200)
        {
            //var columns = new[]
            //{
            //    new ColumnDesc("A", 0.0f, ColumnType.CheckBox),
            //    new ColumnDesc("B", 1.0f, ColumnType.Label)
            //};

            //var list = new PropertyPageListView(columns);
            //var data = new object[values.Length, 2];

            //for (int i = 0; i < values.Length; i++)
            //{
            //    data[i, 0] = selected != null ? selected[i] : true;
            //    data[i, 1] = values[i];
            //}

            //list.UpdateData(data);

            //list.Font = font;
            //list.Height = DpiScaling.ScaleForMainWindow(height);
            //list.HeaderStyle = ColumnHeaderStyle.None;
            //list.ValueChanged += CheckedListBox_ValueChanged;
            //toolTip.SetToolTip(list, SplitLongTooltip(tooltip));

            //return list;

            return null;
        }

        private void CheckedListBox_ValueChanged(object sender, int itemIndex, int columnIndex, object value)
        {
            //var propIdx = GetPropertyIndexForControl(sender as Control);
            //PropertyChanged?.Invoke(this, propIdx, itemIndex, columnIndex, value);
        }

        private Button2 CreateButton(string text, string tooltip)
        {
            var button = new Button2(null, text);
            button.Border = true;
            //button.Click += Button_Click;
            button.Resize(button.Width, DpiScaling.ScaleForMainWindow(32));
            //toolTip.SetToolTip(button, SplitLongTooltip(tooltip));
            return button;
        }

        private void Button_Click(object sender, EventArgs e)
        {
            //var propIdx = GetPropertyIndexForControl(sender as Control);
            //PropertyClicked?.Invoke(this, ClickType.Button, propIdx, -1, -1);
        }

        public void UpdateCheckBoxList(int idx, string[] values, bool[] selected)
        {
            //var list = properties[idx].control as PropertyPageListView;

            //Debug.Assert(values.Length == list.Items.Count);

            //for (int i = 0; i < values.Length; i++)
            //{
            //    list.SetData(i, 0, selected != null ? selected[i] : true);
            //    list.SetData(i, 1, values[i]);
            //}

            //list.Invalidate();
        }

        public void UpdateCheckBoxList(int idx, bool[] selected)
        {
            //var list = properties[idx].control as PropertyPageListView;

            //Debug.Assert(selected.Length == list.Items.Count);

            //for (int i = 0; i < selected.Length; i++)
            //    list.SetData(i, 0, selected[i]);

            //list.Invalidate();
        }

        public int AddColoredTextBox(string value, Color color)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.ColoredTextBox,
                    control = CreateColoredTextBox(value, color)
                });
            return properties.Count - 1;
        }

        public int AddTextBox(string label, string value, int maxLength = 0, string tooltip = null)
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

        public int AddMultilineTextBox(string label, string value)
        {
            //properties.Add(
            //    new Property()
            //    {
            //        type = PropertyType.MultilineTextBox,
            //        label = label != null ? CreateLabel(label) : null,
            //        control = CreateMultilineTextBox(value)
            //    });
            return properties.Count - 1;
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
                    control = CreateColorPicker(color)
                });
            return properties.Count - 1;
        }

        public int AddNumericUpDown(string label, int value, int min, int max, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.NumericUpDown,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateNumericUpDown(value, min, max, tooltip)
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
            //var upDown = (properties[idx].control as NumericUpDown);

            //upDown.Minimum = min;
            //upDown.Maximum = max;
        }

        public void UpdateIntegerRange(int idx, int value, int min, int max)
        {
            //var upDown = (properties[idx].control as NumericUpDown);

            //upDown.Minimum = min;
            //upDown.Maximum = max;
            //upDown.Value = value;
        }

        public void SetLabelText(int idx, string text)
        {
            //(properties[idx].control as Label).Text = text;
        }

        public void SetDropDownListIndex(int idx, int selIdx)
        {
            //(properties[idx].control as ComboBox).SelectedIndex = selIdx;
        }

        public void UpdateDropDownListItems(int idx, string[] values)
        {
            var dd = (properties[idx].control as DropDown2);
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

        public int AddCheckBoxList(string label, string[] values, bool[] selected, string tooltip = null, int height = 200)
        {
            //properties.Add(
            //    new Property()
            //    {
            //        type = PropertyType.CheckBoxList,
            //        label = label != null ? CreateLabel(label) : null,
            //        control = CreateCheckedListBox(values, selected, tooltip, height)
            //    });
            return properties.Count - 1;
        }

        public int AddRadioButtonList(string label, string[] values, int selectedIndex, string tooltip = null)
        {
            return -1;
        }

        private Slider2 CreateSlider(double value, double min, double max, double increment, int numDecimals, bool showLabel, string format = "{0}", string tooltip = null)
        {
            var slider = new Slider2(value, min, max, increment, showLabel, format);
            //slider.ValueChangedEvent += Slider_ValueChangedEvent;
            //toolTip.SetToolTip(slider, SplitLongTooltip(tooltip));
            return slider;
        }

        //private void Slider_ValueChangedEvent(Slider slider, double value)
        //{
        //    var idx = GetPropertyIndexForControl(slider);
        //    PropertyChanged?.Invoke(this, idx, -1, -1, value);
        //}

        //private string Slider_FormatValueEvent(Slider slider, double value)
        //{
        //    var idx = GetPropertyIndexForControl(slider);

        //    if (idx >= 0)
        //        return string.Format(properties[idx].sliderFormat, value);

        //    return null;
        //}

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

        private object CreateListView(ColumnDesc[] columnDescs, object[,] data, int height = 300)
        {
            //var list = new PropertyPageListView(columnDescs);

            //if (data != null)
            //    list.UpdateData(data);

            //list.Font = font;
            //list.Height = DpiScaling.ScaleForMainWindow(height);
            //list.MouseDoubleClick += ListView_MouseDoubleClick;
            //list.MouseDown += ListView_MouseDown;
            //list.ButtonPressed += ListView_ButtonPressed;
            //list.ValueChanged += ListView_ValueChanged;

            //return list;

            return null;
        }

        private void ListView_ButtonPressed(object sender, int itemIndex, int columnIndex)
        {
            //var propIdx = GetPropertyIndexForControl(sender as Control);
            //PropertyClicked?.Invoke(this, ClickType.Button, propIdx, itemIndex, columnIndex);
        }

        private void ListView_ValueChanged(object sender, int itemIndex, int columnIndex, object value)
        {
            //var propIdx = GetPropertyIndexForControl(sender as Control);
            //PropertyChanged?.Invoke(this, propIdx, itemIndex, columnIndex, value);
        }

        private void ListView_MouseDown(object sender, MouseEventArgs e)
        {
            //if (e.Button == MouseButtons.Right)
            //{
            //    var listView = sender as PropertyPageListView;
            //    var hitTest = listView.HitTest(e.Location);

            //    if (hitTest.Item != null)
            //    {
            //        var propIdx = GetPropertyIndexForControl(sender as Control);
            //        PropertyClicked?.Invoke(this, ClickType.Right, propIdx, hitTest.Item.Index, hitTest.Item.SubItems.IndexOf(hitTest.SubItem));
            //    }
            //}
        }

        private void ListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //var listView = sender as PropertyPageListView;
            //var hitTest = listView.HitTest(e.Location);

            //if (hitTest.Item != null)
            //{
            //    for (int i = 0; i < properties.Count; i++)
            //    {
            //        if (properties[i].control == sender)
            //        {
            //            PropertyClicked?.Invoke(this, ClickType.Double, i, hitTest.Item.Index, hitTest.Item.SubItems.IndexOf(hitTest.SubItem));
            //        }
            //    }
            //}
        }

        public void SetColumnEnabled(int propIdx, int colIdx, bool enabled)
        {
            //var listView = properties[propIdx].control as PropertyPageListView;
            //listView.SetColumnEnabled(colIdx, enabled);
        }

        public void AddMultiColumnList(ColumnDesc[] columnDescs, object[,] data, int height = 300)
        {
            //properties.Add(
            //    new Property()
            //    {
            //        type = PropertyType.MultiColumnList,
            //        control = CreateListView(columnDescs, data, height)
            //    });
        }

        public void UpdateMultiColumnList(int idx, object[,] data, string[] columnNames = null)
        {
            //var list = properties[idx].control as PropertyPageListView;

            //list.UpdateData(data);

            //if (columnNames != null)
            //    list.RenameColumns(columnNames);

            //list.AutoResizeColumns();
        }

        public void UpdateMultiColumnList(int idx, int rowIdx, int colIdx, object value)
        {
            //var list = properties[idx].control as PropertyPageListView;
            //list.UpdateData(rowIdx, colIdx, value);
        }

        public void SetPropertyEnabled(int idx, bool enabled)
        {
            //var label = properties[idx].control as Label;

            //if (label != null)
            //{
            //    label.ForeColor = enabled ? Theme.LightGreyFillColor2 : Theme.MediumGreyFillColor1;
            //}
            //else
            //{
            //    properties[idx].control.Enabled = enabled;
            //}
        }

        public void SetPropertyVisible(int idx, bool visible)
        {
            properties[idx].visible = visible;
        }

        public void AppendText(int idx, string line)
        {
            //var textBox = properties[idx].control as TextBox;
            //textBox.AppendText(line + "\r\n");
            //textBox.SelectionStart = textBox.Text.Length - 1;
            //textBox.SelectionLength = 0;
            //textBox.ScrollToCaret();
            //textBox.Focus();
        }

        public void BeginAdvancedProperties()
        {
            advancedPropertyStart = properties.Count;
        }

        private string SplitLongTooltip(string str)
        {
            const int MaxCharsPerLine = 64;

            if (str != null && str.Length > MaxCharsPerLine)
            {
                var strArray = str.ToCharArray(); 

                for (var i = MaxCharsPerLine - 1; i < str.Length; )
                {
                    if (strArray[i] == ' ')
                    {
                        strArray[i] = '\n';
                        i += MaxCharsPerLine;
                    }
                    else
                    {
                        i++;
                    }
                }

                return new string(strArray);
            }
            else
            {
                return str;
            }
        }

        public void SetPropertyWarning(int idx, CommentType type, string comment)
        {
            var prop = properties[idx];

            if (prop.warningIcon == null)
                prop.warningIcon = CreateImageBox(WarningIcons[(int)type]);
            else
                prop.warningIcon.Image = WarningIcons[(int)type];

            prop.warningIcon.Resize(DpiScaling.ScaleForMainWindow(16), DpiScaling.ScaleForMainWindow(16));
            prop.warningIcon.Visible = !string.IsNullOrEmpty(comment);
            //toolTip.SetToolTip(prop.warningIcon, SplitLongTooltip(comment));
        }

        public object GetPropertyValue(int idx)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.TextBox:
                case PropertyType.ColoredTextBox:
                case PropertyType.MultilineTextBox:
                    ForceTextBoxASCII(prop.control as TextBox2);
                    return (prop.control as TextBox2).Text;
                case PropertyType.NumericUpDown:
                    return (int)(prop.control as NumericUpDown2).Value;
                //case PropertyType.Slider:
                //    return (prop.control as Slider).Value;
                case PropertyType.Radio:
                    return (prop.control as RadioButton2).Checked;
                case PropertyType.CheckBox:
                    return (prop.control as CheckBox2).Checked;
                case PropertyType.ColorPicker:
                    return (prop.control as ColorPicker2).SelectedColor;
                case PropertyType.DropDownList:
                    return (prop.control as DropDown2).Text;
                case PropertyType.CheckBoxList:
                //    {
                //        var listView = prop.control as PropertyPageListView;
                //        var selected = new bool[listView.Items.Count];
                //        for (int i = 0; i < listView.Items.Count; i++)
                //            selected[i] = (bool)listView.GetData(i, 0);
                //        return selected;
                //    }
                case PropertyType.Button:
                    return (prop.control as Button2).Text;
            }

            return null;
        }

        public T GetPropertyValue<T>(int idx)
        {
            return (T)GetPropertyValue(idx);
        }

        public T GetPropertyValue<T>(int idx, int rowIdx, int colIdx)
        {
            //var prop = properties[idx];
            //Debug.Assert(prop.type == PropertyType.MultiColumnList);
            //var list = prop.control as PropertyPageListView;
            //return (T)list.GetData(rowIdx, colIdx);

            return default(T);
        }

        public int GetSelectedIndex(int idx)
        {
            //var prop = properties[idx];

            //switch (prop.type)
            //{
            //    case PropertyType.DropDownList:
            //        return (prop.control as ComboBox).SelectedIndex;
            //}

            return -1;
        }

        public void SetPropertyValue(int idx, object value)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.CheckBox:
                    (prop.control as CheckBox2).Checked = (bool)value;
                    break;
                case PropertyType.Button:
                    (prop.control as Button2).Text = (string)value;
                    break;
                case PropertyType.MultilineTextBox:
                    (prop.control as TextBox2).Text = (string)value;
                    break;
                case PropertyType.ProgressBar:
                    (prop.control as ProgressBar2).Progress = (float)value;
                    break;
                case PropertyType.Slider:
                    (prop.control as Slider2).Value = (double)value;
                    break;
            }
        }
        
        private int GetRadioButtonHeight(string text, int width)
        {
            var testLabel = CreateLabel(text, null, true);

            //testLabel.MaximumSize = new Size(width - DpiScaling.ScaleForMainWindow(16), 0);
            //Controls.Add(testLabel);
            //var height = testLabel.Height + DpiScaling.ScaleForMainWindow(8);
            //Controls.Remove(testLabel);
            //return height;

            return 0;
        }

        public void Build(bool advanced = false)
        {
            var margin = DpiScaling.ScaleForMainWindow(8);
            var maxLabelWidth = 0;
            var propertyCount = advanced || advancedPropertyStart < 0 ? properties.Count : advancedPropertyStart;

            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];

                dialog.RemoveControl(prop.label);
                dialog.RemoveControl(prop.control);
                dialog.RemoveControl(prop.warningIcon);
            }

            for (int i = 0; i < propertyCount; i++)
            {
                var prop = properties[i];

                if (prop.visible && prop.label != null)
                {
                    dialog.AddControl(prop.label); // Need to add to initialize rendering.
                    maxLabelWidth = Math.Max(maxLabelWidth, prop.label.MeasureWidth());
                }
            }

            int totalHeight = 0;
            int warningWidth = showWarnings ? DpiScaling.ScaleForMainWindow(16) + margin : 0;

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
                    prop.label.Move(baseX, baseY + totalHeight, maxLabelWidth, prop.label.Height);
                    prop.control.Move(baseX + maxLabelWidth + margin, baseY + totalHeight, layoutWidth - maxLabelWidth - warningWidth - margin, prop.control.Height);

                    dialog.AddControl(prop.label);
                    dialog.AddControl(prop.control);

                    height = prop.label.Height;
                }
                else
                {
                    prop.control.Move(baseX, baseY + totalHeight, layoutWidth, prop.control.Height);

                    dialog.AddControl(prop.control);
                }

                // Hack for some multi line labels. Ideally we would do that when the 
                // control is reszed or something.
                if (prop.type == PropertyType.Label ||
                    prop.type == PropertyType.Radio)
                {
                    (prop.control as Label2).ResizeForMultiline();
                }

                // MATTT : Focus management.
                //if (prop.type == PropertyType.ColoredTextBox)
                //{
                //    (prop.control as TextBox).SelectAll();
                //    prop.control.Focus();
                //}

                height = Math.Max(prop.control.Height, height);

                if (prop.warningIcon != null)
                {
                    prop.warningIcon.Move(
                        baseX + layoutWidth - prop.warningIcon.Width,
                        baseY + totalHeight + (height - prop.warningIcon.Height) / 2);
                    dialog.AddControl(prop.warningIcon);
                }

                totalHeight += height;
            }

            layoutHeight = totalHeight;
        }
    }
}
