using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Reflection;

namespace FamiStudio
{
    public partial class PropertyPage : UserControl
    {
        private static Bitmap[] warningIcons;

        class Property
        {
            public PropertyType type;
            public Label label;
            public Control control;
            public int leftMarging;
            public string sliderFormat;
            public PictureBox warningIcon;
            public bool visible = true;
        };

        private int layoutHeight;
        private Font font;
        private Bitmap colorBitmap;
        private List<Property> properties = new List<Property>();
        private ToolTip toolTip;

        public int LayoutHeight => layoutHeight;
        public int PropertyCount => properties.Count;

        [DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);

        public PropertyPage()
        {
            InitializeComponent();

            // Happens in design mode
            try
            {
                font = new Font(PlatformUtils.PrivateFontCollection.Families[0], 10.0f, FontStyle.Regular);
            }
            catch
            {
            }

            if (warningIcons ==  null)
            {
                string suffix = DpiScaling.Dialog > 1 ? "@2x" : "";
                
                warningIcons = new Bitmap[3];
                warningIcons[0] = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.WarningGood{suffix}.png"))   as Bitmap;
                warningIcons[1] = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.WarningYellow{suffix}.png")) as Bitmap;
                warningIcons[2] = Image.FromStream(Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.Warning{suffix}.png"))       as Bitmap;
            }
        }

        private void InitializeComponent()
        {
            toolTip = new ToolTip();
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Theme.DarkGreyFillColor1;
            Padding = new Padding(3);
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

        private unsafe Bitmap GetColorBitmap()
        {
            if (colorBitmap == null)
            {
                colorBitmap = new Bitmap(Theme.CustomColors.GetLength(0), Theme.CustomColors.GetLength(1));
                var data = colorBitmap.LockBits(new Rectangle(0, 0, colorBitmap.Width, colorBitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte* ptr = (byte*)data.Scan0.ToPointer();

                for (int j = 0; j < colorBitmap.Height; j++)
                {
                    for (int i = 0; i < colorBitmap.Width; i++)
                    {
                        var color = Theme.CustomColors[i, j];

                        ptr[i * 4 + 0] = color.B;
                        ptr[i * 4 + 1] = color.G;
                        ptr[i * 4 + 2] = color.R;
                        ptr[i * 4 + 3] = 255;
                    }

                    ptr += data.Stride;
                }

                colorBitmap.UnlockBits(data);
            }

            return colorBitmap;
        }

        private Label CreateLabel(string str, string tooltip = null, bool multiline = false)
        {
            Debug.Assert(!string.IsNullOrEmpty(str));

            var label = new Label();

            label.Text = str;
            label.Font = font;
            label.AutoSize = true;
            label.ForeColor = Theme.LightGreyFillColor2;
            label.BackColor = BackColor;
            if (multiline)
                label.MaximumSize = new Size(1000, 0);
            toolTip.SetToolTip(label, tooltip);

            return label;
        }

        private Label CreateLinkLabel(string str, string url, string tooltip = null)
        {
            var label = new LinkLabel();

            label.Text = str;
            label.Font = font;
            label.LinkColor = Theme.LightGreyFillColor1;
            label.Links.Add(0, str.Length, url);
            label.LinkClicked += Label_LinkClicked;
            label.TextAlign = ContentAlignment.BottomCenter;
            //label.AutoSize = true;
            label.ForeColor = Theme.LightGreyFillColor2;
            label.BackColor = BackColor;
            toolTip.SetToolTip(label, tooltip);

            return label;
        }

        private void Label_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var link = sender as LinkLabel;
            PlatformUtils.OpenUrl(link.Links[0].LinkData as string);
        }

        private TextBox CreateColoredTextBox(string txt, Color backColor)
        {
            var textBox = new TextBox();

            textBox.Text = txt;
            textBox.Font = font;
            textBox.TextChanged += TextBox_TextChanged;
            textBox.BackColor = backColor;

            return textBox;
        }

        private TextBox CreateTextBox(string txt, int maxLength, string tooltip = null)
        {
            var textBox = new TextBox();

            textBox.Text = txt;
            textBox.Font = font;
            textBox.MaxLength = maxLength;
            textBox.TextChanged += TextBox_TextChanged;
            toolTip.SetToolTip(textBox, tooltip);

            return textBox;
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            ForceTextBoxASCII(sender as TextBox);
        }

        private void ForceTextBoxASCII(TextBox textBox)
        {
            // All of our text storage is ASCII at the moment, so enforce it right away
            // to prevent issues later on.
            var oldText = textBox.Text;
            var newText = Utils.ForceASCII(oldText);

            if (oldText != newText)
            {
                var selStart = textBox.SelectionStart;
                var selLen = textBox.SelectionLength;

                textBox.Text = newText;
                textBox.SelectionStart = selStart;
                textBox.SelectionLength = selLen;
            }
        }

        private TextBox CreateMultilineTextBox(string txt)
        {
            var textBox = new TextBox();

            textBox.Font = new Font(PlatformUtils.PrivateFontCollection.Families[0], 8.0f, FontStyle.Regular);
            textBox.Text = txt;
            textBox.BackColor = Theme.DarkGreyFillColor1;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.ForeColor = Theme.LightGreyFillColor2;
            textBox.Location = new Point(5, 5);
            textBox.Multiline = true;
            textBox.ReadOnly = true;
            textBox.Height = DpiScaling.ScaleForDialog(300);
            textBox.ScrollBars = ScrollBars.Vertical;
            textBox.Select(0, 0);
            textBox.GotFocus += TextBox_GotFocus;

            return textBox;
        }

        private void TextBox_GotFocus(object sender, EventArgs e)
        {
            HideCaret((sender as TextBox).Handle);
        }

        private PictureBox CreateColorPickerPictureBox(Color color)
        {
            var pictureBox = new NoInterpolationPictureBox();
            var bmp = GetColorBitmap();

            pictureBox.Image = bmp;
            pictureBox.Height = (int)Math.Round(Width * (bmp.Height / (float)bmp.Width));
            pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox.BorderStyle = BorderStyle.FixedSingle;
            pictureBox.BackColor = color;
            pictureBox.MouseDown += PictureBox_MouseDown;
            pictureBox.MouseMove += PictureBox_MouseMove;
            pictureBox.MouseDoubleClick += PictureBox_MouseDoubleClick;

            return pictureBox;
        }

        private PictureBox CreatePictureBox(Bitmap bmp)
        {
            var pictureBox = new PictureBox();

            pictureBox.Image = bmp;
            pictureBox.Width  = DpiScaling.ScaleForDialog(bmp.Width);
            pictureBox.Height = DpiScaling.ScaleForDialog(bmp.Height);
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.BorderStyle = BorderStyle.None;

            return pictureBox;
        }

        private void ChangeColor(PictureBox pictureBox, int x, int y)
        {
            int i = Math.Min(Theme.CustomColors.GetLength(0) - 1, Math.Max(0, (int)(x / (float)pictureBox.Width  * Theme.CustomColors.GetLength(0))));
            int j = Math.Min(Theme.CustomColors.GetLength(1) - 1, Math.Max(0, (int)(y / (float)pictureBox.Height * Theme.CustomColors.GetLength(1))));

            foreach (var prop in properties)
            {
                if (prop.type == PropertyType.ColoredTextBox)
                {
                    prop.control.BackColor = Theme.CustomColors[i, j];
                }
            }

            pictureBox.BackColor = Theme.CustomColors[i, j];
        }

        private void PictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ChangeColor(sender as PictureBox, e.X, e.Y);

            PropertyWantsClose?.Invoke(GetPropertyIndexForControl(sender as Control));
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

        private NumericUpDown CreateNumericUpDown(int value, int min, int max, string tooltip = null)
        {
            var upDown = new NumericUpDown();

            upDown.Font = font;
            upDown.Minimum = min;
            upDown.Maximum = max;
            upDown.Text = value.ToString();
            upDown.ValueChanged += UpDown_ValueChanged;
            toolTip.SetToolTip(upDown, tooltip);

            return upDown;
        }

        private ProgressBar CreateProgressBar(float value)
        {
            var progress = new ProgressBar();

            progress.Font = font;
            progress.Minimum = 0;
            progress.Maximum = 1000;
            progress.Value = (int)Math.Round(value * 1000);

            return progress;
        }

        private RadioButton CreateRadioButton(string text, bool check)
        {
            var radio = new RadioButton();

            radio.Font = font;
            radio.ForeColor = Theme.LightGreyFillColor2;
            radio.Text = text;
            radio.AutoSize = false;
            radio.Checked = check;

            return radio;
        }

        private void UpDown_ValueChanged(object sender, EventArgs e)
        {
            int idx = GetPropertyIndexForControl(sender as Control);
            PropertyChanged?.Invoke(this, idx, -1, -1, GetPropertyValue(idx));
        }

        private DomainUpDown CreateDomainUpDown(int[] values, int value)
        {
            var upDown = new DomainUpDown();

            upDown.Items.AddRange(values);
            upDown.SelectedItem = value;
            upDown.Font = font;

            return upDown;
        }

        private CheckBox CreateCheckBox(bool value, string text = "", string tooltip = null)
        {
            var cb = new CheckBox();

            cb.Text = text;
            cb.Checked = value;
            cb.Font = font;
            cb.ForeColor = Theme.LightGreyFillColor2;
            cb.CheckedChanged += Cb_CheckedChanged;
            toolTip.SetToolTip(cb, tooltip);

            return cb;
        }

        private void Cb_CheckedChanged(object sender, EventArgs e)
        {
            int idx = GetPropertyIndexForControl(sender as Control);
            PropertyChanged?.Invoke(this, idx, -1, -1, GetPropertyValue(idx));
        }

        private ComboBox CreateDropDownList(string[] values, string value, string tooltip = null)
        {
            var cb = new ComboBox();

            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.Items.AddRange(values);
            cb.Text = value;
            cb.Font = font;
            cb.Enabled = values.Length > 0;
            cb.SelectedIndexChanged += Cb_SelectedIndexChanged;
            toolTip.SetToolTip(cb, tooltip);

            return cb;
        }

        private void Cb_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = GetPropertyIndexForControl(sender as Control);
            PropertyChanged?.Invoke(this, idx, -1, -1, GetPropertyValue(idx));
        }

        private PropertyPageListView CreateCheckedListBox(string[] values, bool[] selected, string tooltip = null, int height = 200)
        {
            var columns = new[]
            {
                new ColumnDesc("A", 0.0f, ColumnType.CheckBox),
                new ColumnDesc("B", 1.0f, ColumnType.Label)
            };

            var list = new PropertyPageListView(columns);
            var data = new object[values.Length, 2];

            for (int i = 0; i < values.Length; i++)
            {
                data[i, 0] = selected != null ? selected[i] : true;
                data[i, 1] = values[i];
            }

            list.UpdateData(data);

            list.Font = font;
            list.Height = DpiScaling.ScaleForDialog(height);
            list.HeaderStyle = ColumnHeaderStyle.None;
            list.ValueChanged += CheckedListBox_ValueChanged;
            toolTip.SetToolTip(list, tooltip);

            return list;
        }

        private void CheckedListBox_ValueChanged(object sender, int itemIndex, int columnIndex, object value)
        {
            var propIdx = GetPropertyIndexForControl(sender as Control);
            PropertyChanged?.Invoke(this, propIdx, itemIndex, columnIndex, value);
        }

        private Button CreateButton(string text, string tooltip)
        {
            var button = new Button();
            button.Text = text;
            button.Click += Button_Click;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = font;
            button.ForeColor = Theme.LightGreyFillColor2;
            button.Height = DpiScaling.ScaleForDialog(32);
            toolTip.SetToolTip(button, tooltip);
            return button;
        }

        private void Button_Click(object sender, EventArgs e)
        {
            var propIdx = GetPropertyIndexForControl(sender as Control);
            PropertyClicked?.Invoke(this, ClickType.Button, propIdx, -1, -1);
        }

        public void UpdateCheckBoxList(int idx, string[] values, bool[] selected)
        {
            var list = properties[idx].control as PropertyPageListView;

            Debug.Assert(values.Length == list.Items.Count);

            for (int i = 0; i < values.Length; i++)
            {
                list.SetData(i, 0, selected != null ? selected[i] : true);
                list.SetData(i, 1, values[i]);
            }

            list.Invalidate();
        }

        public void UpdateCheckBoxList(int idx, bool[] selected)
        {
            var list = properties[idx].control as PropertyPageListView;

            Debug.Assert(selected.Length == list.Items.Count);

            for (int i = 0; i < selected.Length; i++)
                list.SetData(i, 0, selected[i]);

            list.Invalidate();
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
            properties.Add(
                new Property()
                {
                    type = PropertyType.MultilineTextBox,
                    label = label != null ? CreateLabel(label) : null,
                    control = CreateMultilineTextBox(value)
                });
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
                    type = PropertyType.Label,
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
                    control = CreateColorPickerPictureBox(color)
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

        public int AddProgressBar(string label, float value)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.ProgressBar,
                    label = label != null ? CreateLabel(label) : null,
                    control = CreateProgressBar(value)
                });
            return properties.Count - 1;
        }

        public int AddRadioButton(string label, string text, bool check)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Radio,
                    label = label != null ? CreateLabel(label) : null,
                    control = CreateRadioButton(text, check)
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

        public void AddDomainRange(string label, int[] values, int value)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.DomainUpDown,
                    label = label != null ? CreateLabel(label) : null,
                    control = CreateDomainUpDown(values, value)
                });
        }

        public void UpdateDomainRange(int idx, int[] values, int value)
        {
            var upDown = (properties[idx].control as DomainUpDown);

            upDown.Items.Clear();
            upDown.Items.AddRange(values);
            upDown.Text = " "; // Workaround refresh bug.
            upDown.SelectedItem = value;
        }

        public void SetLabelText(int idx, string text)
        {
            (properties[idx].control as Label).Text = text;
        }

        public void SetDropDownListIndex(int idx, int selIdx)
        {
            (properties[idx].control as ComboBox).SelectedIndex = selIdx;
        }

        public void UpdateDropDownListItems(int idx, string[] values)
        {
            var cb = (properties[idx].control as ComboBox);
            var selectedIdx = cb.SelectedIndex;

            cb.Items.Clear();
            cb.Items.AddRange(values);

            if (selectedIdx < values.Length)
                cb.SelectedIndex = selectedIdx;
            else
                cb.SelectedIndex = 0;
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

        public int AddLabelCheckBox(string label, bool value, int margin = 0)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.CheckBox,
                    control = CreateCheckBox(value, label),
                    leftMarging = margin
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
            properties.Add(
                new Property()
                {
                    type = PropertyType.CheckBoxList,
                    label = label != null ? CreateLabel(label) : null,
                    control = CreateCheckedListBox(values, selected, tooltip, height)
                });
            return properties.Count - 1;
        }

        public int AddRadioButtonList(string label, string[] values, int selectedIndex, string tooltip = null)
        {
            Debug.Assert(false);
            return -1;
        }

        private Slider CreateSlider(double value, double min, double max, double increment, int numDecimals, bool showLabel, string tooltip = null)
        {
            var slider = new Slider(value, min, max, increment, numDecimals, showLabel);
            slider.FormatValueEvent += Slider_FormatValueEvent;
            slider.ValueChangedEvent += Slider_ValueChangedEvent;
            slider.Font = font;
            return slider;
        }

        private void Slider_ValueChangedEvent(Slider slider, double value)
        {
            var idx = GetPropertyIndexForControl(slider);
            PropertyChanged?.Invoke(this, idx, -1, -1, value);
        }

        private string Slider_FormatValueEvent(Slider slider, double value)
        {
            var idx = GetPropertyIndexForControl(slider);

            if (idx >= 0)
                return string.Format(properties[idx].sliderFormat, value);

            return null;
        }

        public int AddSlider(string label, double value, double min, double max, double increment, int numDecimals, string format = "{0}", string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Slider,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateSlider(value, min, max, increment, numDecimals, format != null, tooltip),
                    sliderFormat = format
                });
            return properties.Count - 1;
        }

        private PropertyPageListView CreateListView(ColumnDesc[] columnDescs, object[,] data, int height = 300)
        {
            var list = new PropertyPageListView(columnDescs);

            if (data != null)
                list.UpdateData(data);

            list.Font = font;
            list.Height = DpiScaling.ScaleForDialog(height);
            list.MouseDoubleClick += ListView_MouseDoubleClick;
            list.MouseDown += ListView_MouseDown;
            list.ButtonPressed += ListView_ButtonPressed;
            list.ValueChanged += ListView_ValueChanged;

            return list;
        }

        private void ListView_ButtonPressed(object sender, int itemIndex, int columnIndex)
        {
            var propIdx = GetPropertyIndexForControl(sender as Control);
            PropertyClicked?.Invoke(this, ClickType.Button, propIdx, itemIndex, columnIndex);
        }

        private void ListView_ValueChanged(object sender, int itemIndex, int columnIndex, object value)
        {
            var propIdx = GetPropertyIndexForControl(sender as Control);
            PropertyChanged?.Invoke(this, propIdx, itemIndex, columnIndex, value);
        }

        private void ListView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var listView = sender as PropertyPageListView;
                var hitTest = listView.HitTest(e.Location);

                if (hitTest.Item != null)
                {
                    var propIdx = GetPropertyIndexForControl(sender as Control);
                    PropertyClicked?.Invoke(this, ClickType.Right, propIdx, hitTest.Item.Index, hitTest.Item.SubItems.IndexOf(hitTest.SubItem));
                }
            }
        }

        private void ListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var listView = sender as PropertyPageListView;
            var hitTest = listView.HitTest(e.Location);

            if (hitTest.Item != null)
            {
                for (int i = 0; i < properties.Count; i++)
                {
                    if (properties[i].control == sender)
                    {
                        PropertyClicked?.Invoke(this, ClickType.Double, i, hitTest.Item.Index, hitTest.Item.SubItems.IndexOf(hitTest.SubItem));
                    }
                }
            }
        }

        public void SetColumnEnabled(int propIdx, int colIdx, bool enabled)
        {
            var listView = properties[propIdx].control as PropertyPageListView;
            listView.SetColumnEnabled(colIdx, enabled);
        }

        public void AddMultiColumnList(ColumnDesc[] columnDescs, object[,] data, int height = 300)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.MultiColumnList,
                    control = CreateListView(columnDescs, data, height)
                });
        }

        public void UpdateMultiColumnList(int idx, object[,] data, string[] columnNames = null)
        {
            var list = properties[idx].control as PropertyPageListView;

            list.UpdateData(data);

            if (columnNames != null)
                list.RenameColumns(columnNames);

            list.AutoResizeColumns();
        }

        public void UpdateMultiColumnList(int idx, int rowIdx, int colIdx, object value)
        {
            var list = properties[idx].control as PropertyPageListView;
            list.UpdateData(rowIdx, colIdx, value);
            //list.AutoResizeColumns();
        }

        public void SetPropertyEnabled(int idx, bool enabled)
        {
            var label = properties[idx].control as Label;

            if (label != null)
            {
                label.ForeColor = enabled ? Theme.LightGreyFillColor2 : Theme.MediumGreyFillColor1;
            }
            else
            {
                properties[idx].control.Enabled = enabled;
            }
        }

        public void SetPropertyVisible(int idx, bool visible)
        {
            properties[idx].visible = visible;
        }

        public void AppendText(int idx, string line)
        {
            var textBox = properties[idx].control as TextBox;
            textBox.AppendText(line + "\r\n");
            textBox.SelectionStart = textBox.Text.Length - 1;
            textBox.SelectionLength = 0;
            textBox.ScrollToCaret();
            textBox.Focus();
        }

        public void BeginAdvancedProperties()
        {
            advancedPropertyStart = properties.Count;
        }

        public void SetPropertyWarning(int idx, CommentType type, string comment)
        {
            var prop = properties[idx];

            if (prop.warningIcon == null)
                prop.warningIcon = CreatePictureBox(warningIcons[(int)type]);
            else
                prop.warningIcon.Image = warningIcons[(int)type];

            prop.warningIcon.Width   = DpiScaling.ScaleForDialog(16);
            prop.warningIcon.Height  = DpiScaling.ScaleForDialog(16);
            prop.warningIcon.Visible = !string.IsNullOrEmpty(comment);
            toolTip.SetToolTip(prop.warningIcon, comment);
        }

        public object GetPropertyValue(int idx)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.TextBox:
                case PropertyType.ColoredTextBox:
                case PropertyType.MultilineTextBox:
                    ForceTextBoxASCII(prop.control as TextBox);
                    return (prop.control as TextBox).Text;
                case PropertyType.NumericUpDown:
                    return (int)(prop.control as NumericUpDown).Value;
                case PropertyType.DomainUpDown:
                    return int.TryParse(prop.control.Text, out var val) ? val : 0;
                case PropertyType.Slider:
                    return (prop.control as Slider).Value;
                case PropertyType.Radio:
                    return (prop.control as RadioButton).Checked;
                case PropertyType.CheckBox:
                    return (prop.control as CheckBox).Checked;
                case PropertyType.ColorPicker:
                    return (prop.control as PictureBox).BackColor;
                case PropertyType.DropDownList:
                    return (prop.control as ComboBox).Text;
                case PropertyType.CheckBoxList:
                    {
                        var listView = prop.control as PropertyPageListView;
                        var selected = new bool[listView.Items.Count];
                        for (int i = 0; i < listView.Items.Count; i++)
                            selected[i] = (bool)listView.GetData(i, 0);
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
            Debug.Assert(prop.type == PropertyType.MultiColumnList);
            var list = prop.control as PropertyPageListView;
            return (T)list.GetData(rowIdx, colIdx);
        }

        public int GetSelectedIndex(int idx)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.DropDownList:
                    return (prop.control as ComboBox).SelectedIndex;
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
                case PropertyType.MultilineTextBox:
                    (prop.control as TextBox).Text = (string)value;
                    break;
                case PropertyType.ProgressBar:
                    (prop.control as ProgressBar).Value = (int)Math.Round((float)value * 1000);
                    break;
                case PropertyType.Slider:
                    (prop.control as Slider).Value = (double)value;
                    break;
            }
        }
        
        private int GetRadioButtonHeight(string text, int width)
        {
            var testLabel = CreateLabel(text, null, true);

            testLabel.MaximumSize = new Size(width - DpiScaling.ScaleForDialog(16), 0);
            Controls.Add(testLabel);
            var height = testLabel.Height + DpiScaling.ScaleForDialog(8);
            Controls.Remove(testLabel);

            return height;
        }

        public void Build(bool advanced = false)
        {
            SuspendLayout();

            int margin = DpiScaling.ScaleForDialog(5);
            int maxLabelWidth = 0;
            int defaultLabelHeight = 24;

            // Workaround scaling issue with checkboxes. 
            // Measure a label and well use this for checkbox.
            if (DpiScaling.Dialog > 1.0f)
            {
                Label testLabel = CreateLabel("888");
                Controls.Add(testLabel);
                defaultLabelHeight = Math.Max(defaultLabelHeight, testLabel.Height) + 2;
                Controls.Remove(testLabel);
            }

            var propertyCount = advanced || advancedPropertyStart < 0 ? properties.Count : advancedPropertyStart;

            for (int i = 0; i < propertyCount; i++)
            {
                var prop = properties[i];

                if (prop.visible && prop.label != null)
                {
                    // This is really ugly. We cant measure the labels unless they are added.
                    Controls.Add(prop.label);
                    maxLabelWidth = Math.Max(maxLabelWidth, prop.label.Width);
                    Controls.Remove(prop.label);
                }
            }

            int widthNoMargin = Width - (margin * 2);
            int totalHeight = margin;
            int warningWidth = showWarnings ? DpiScaling.ScaleForDialog(16) + margin : 0;

            for (int i = 0; i < propertyCount; i++)
            {
                var prop = properties[i];
                var height = 0;

                if (!prop.visible)
                    continue;

                // Hack for checkbox that dont scale with Hi-DPI. 
                if (DpiScaling.Dialog > 1.0f && prop.control is CheckBox)
                {
                    prop.control.Height = defaultLabelHeight;
                    if (prop.label != null)
                        prop.label.Height = defaultLabelHeight;
                }

                if (prop.label != null)
                {
                    prop.label.Left    = margin;
                    prop.label.Top     = totalHeight;
                    //prop.label.Width   = widthNoMargin / 2;

                    prop.control.Left  = maxLabelWidth + margin;
                    prop.control.Top   = totalHeight;
                    prop.control.Width = widthNoMargin - maxLabelWidth - warningWidth;

                    Controls.Add(prop.label);
                    Controls.Add(prop.control);

                    height = prop.label.Height;
                }
                else
                {
                    prop.control.Left  = margin + prop.leftMarging;
                    prop.control.Top   = totalHeight;
                    prop.control.Width = widthNoMargin;

                    // HACK : For some multiline controls.
                    if (prop.control is Label && prop.control.MaximumSize.Width != 0)
                        prop.control.MaximumSize = new Size(prop.control.Width, 0);
                    else if (prop.control is RadioButton)
                        prop.control.Height = GetRadioButtonHeight(prop.control.Text, prop.control.Width);

                    Controls.Add(prop.control);
                }

                height = Math.Max(prop.control.Height, height);

                if (prop.warningIcon != null)
                {
                    prop.warningIcon.Top  = totalHeight + (height - prop.warningIcon.Height) / 2;
                    prop.warningIcon.Left = widthNoMargin + margin + margin - warningWidth;
                    Controls.Add(prop.warningIcon);
                }

                totalHeight += height + margin;
            }

            Height = totalHeight;
            layoutHeight = totalHeight;
            ResumeLayout();
        }
    }
}
