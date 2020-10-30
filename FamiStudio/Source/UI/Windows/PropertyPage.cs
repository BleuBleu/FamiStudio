using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

#if FAMISTUDIO_WINDOWS
using RenderTheme = FamiStudio.Direct2DTheme;
#else
    using RenderTheme = FamiStudio.GLTheme;
#endif

namespace FamiStudio
{
    public enum PropertyType
    {
        String,
        ColoredString,
        IntegerRange,
        DomainRange,
        Boolean,
        StringList,
        StringListMulti,
        Color,
        Label,
        Button,
        MultilineString,
        ProgressBar
    };

    public partial class PropertyPage : UserControl
    {
        public delegate void ButtonPropertyClicked(PropertyPage props, int propertyIndex);

        class Property
        {
            public PropertyType type;
            public Label label;
            public Control control;
            public int leftMarging;
            public ButtonPropertyClicked click;
        };

        private int layoutHeight;
        private Font font;
        private Bitmap colorBitmap;
        private List<Property> properties = new List<Property>();
        private object userData;

        public delegate void PropertyChangedDelegate(PropertyPage props, int idx, object value);
        public event PropertyChangedDelegate PropertyChanged;
        public delegate void PropertyWantsCloseDelegate(int idx);
        public event PropertyWantsCloseDelegate PropertyWantsClose;

        public int LayoutHeight => layoutHeight;
        public int PropertyCount => properties.Count;
        public object UserData { get => userData; set => userData = value; }

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
        }

        private int GetPropertyIndex(Control ctrl)
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
                colorBitmap = new Bitmap(ThemeBase.CustomColors.GetLength(0), ThemeBase.CustomColors.GetLength(1));
                var data = colorBitmap.LockBits(new Rectangle(0, 0, colorBitmap.Width, colorBitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte* ptr = (byte*)data.Scan0.ToPointer();

                for (int j = 0; j < colorBitmap.Height; j++)
                {
                    for (int i = 0; i < colorBitmap.Width; i++)
                    {
                        var color = ThemeBase.CustomColors[i, j];

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

        private Label CreateLabel(string str, string tooltip = null)
        {
            var label = new Label();

            label.Text = str;
            label.Font = font;
            label.AutoSize = true;
            label.ForeColor = ThemeBase.LightGreyFillColor2;
            label.BackColor = BackColor;
            toolTip.SetToolTip(label, tooltip);

            return label;
        }

        private Label CreateLinkLabel(string str, string url, string tooltip = null)
        {
            var label = new LinkLabel();

            label.Text = str;
            label.Font = font;
            label.LinkColor = ThemeBase.LightGreyFillColor1;
            label.Links.Add(0, str.Length, url);
            label.LinkClicked += Label_LinkClicked;
            label.AutoSize = true;
            label.ForeColor = ThemeBase.LightGreyFillColor2;
            label.BackColor = BackColor;
            toolTip.SetToolTip(label, tooltip);

            return label;
        }

        private void Label_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var link = sender as LinkLabel;
            Utils.OpenUrl(link.Links[0].LinkData as string);
        }

        private TextBox CreateColoredTextBox(string txt, Color backColor)
        {
            var textBox = new TextBox();

            textBox.Text = txt;
            textBox.Font = font;
            textBox.BackColor = backColor;

            return textBox;
        }

        private TextBox CreateTextBox(string txt, int maxLength, string tooltip = null)
        {
            var textBox = new TextBox();

            textBox.Text = txt;
            textBox.Font = font;
            textBox.MaxLength = maxLength;
            toolTip.SetToolTip(textBox, tooltip);

            return textBox;
        }

        private TextBox CreateMultilineTextBox(string txt)
        {
            var textBox = new TextBox();

            textBox.Font = new Font(PlatformUtils.PrivateFontCollection.Families[0], 10.0f, FontStyle.Regular);
            textBox.Text = string.Join("\r\n", txt);
            textBox.BackColor = ThemeBase.DarkGreyFillColor1;
            textBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBox.ForeColor = ThemeBase.LightGreyFillColor2;
            textBox.Location = new System.Drawing.Point(5, 5);
            textBox.Multiline = true;
            textBox.ReadOnly = true;
            textBox.Height = 300;
            textBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            textBox.Select(0, 0);
            textBox.GotFocus += TextBox_GotFocus;

            return textBox;
        }

        private void TextBox_GotFocus(object sender, EventArgs e)
        {
            HideCaret((sender as TextBox).Handle);
        }

        private PictureBox CreatePictureBox(Color color)
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

        private void ChangeColor(PictureBox pictureBox, int x, int y)
        {
            int i = Math.Min(ThemeBase.CustomColors.GetLength(0) - 1, Math.Max(0, (int)(x / (float)pictureBox.Width  * ThemeBase.CustomColors.GetLength(0))));
            int j = Math.Min(ThemeBase.CustomColors.GetLength(1) - 1, Math.Max(0, (int)(y / (float)pictureBox.Height * ThemeBase.CustomColors.GetLength(1))));

            foreach (var prop in properties)
            {
                if (prop.type == PropertyType.ColoredString)
                {
                    prop.control.BackColor = ThemeBase.CustomColors[i, j];
                }
            }

            pictureBox.BackColor = ThemeBase.CustomColors[i, j];
        }

        private void PictureBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ChangeColor(sender as PictureBox, e.X, e.Y);

            PropertyWantsClose?.Invoke(GetPropertyIndex(sender as Control));
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

        private void UpDown_ValueChanged(object sender, EventArgs e)
        {
            int idx = GetPropertyIndex(sender as Control);
            PropertyChanged?.Invoke(this, idx, GetPropertyValue(idx));
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
            cb.ForeColor = ThemeBase.LightGreyFillColor2;
            cb.CheckedChanged += Cb_CheckedChanged;
            toolTip.SetToolTip(cb, tooltip);

            return cb;
        }

        private void Cb_CheckedChanged(object sender, EventArgs e)
        {
            int idx = GetPropertyIndex(sender as Control);
            PropertyChanged?.Invoke(this, idx, GetPropertyValue(idx));
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
            int idx = GetPropertyIndex(sender as Control);
            PropertyChanged?.Invoke(this, idx, GetPropertyValue(idx));
        }

        private CheckedListBox CreateCheckedListBox(string[] values, bool[] selected)
        {
            var listBox = new PaddedCheckedListBox();

            for (int i = 0; i < values.Length; i++)
                listBox.Items.Add(values[i], selected != null ? selected[i] : true);

            listBox.IntegralHeight = false;
            listBox.Font = font;
            listBox.Height = 200;
            listBox.CheckOnClick = true;
            listBox.SelectionMode = SelectionMode.One;

            return listBox;
        }

        private Button CreateButton(string text, string tooltip)
        {
            var button = new Button();
            button.Text = text;
            button.Click += TextBox_Click;
            button.FlatStyle = FlatStyle.Flat;
            button.Font = font;
            button.ForeColor = ThemeBase.LightGreyFillColor2;
            button.Height = 32;
            toolTip.SetToolTip(button, tooltip);
            return button;
        }

        private void TextBox_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                if (properties[i].control == sender)
                {
                    properties[i].click(this, i);
                }
            }
        }

        public void UpdateMultiStringList(int idx, string[] values, bool[] selected)
        {
            var listBox = (properties[idx].control as PaddedCheckedListBox);

            listBox.Items.Clear();
            for (int i = 0; i < values.Length; i++)
                listBox.Items.Add(values[i], selected != null ? selected[i] : true);
        }

        public void AddColoredString(string value, Color color)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.ColoredString,
                    control = CreateColoredTextBox(value, color)
                });
        }

        public void AddString(string label, string value, int maxLength = 0, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.String,
                    label = CreateLabel(label),
                    control = CreateTextBox(value, maxLength, tooltip)
                });
        }

        public void AddMultilineString(string label, string value)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.MultilineString,
                    label = CreateLabel(label),
                    control = CreateMultilineTextBox(value)
                });
        }

        public void AddButton(string label, string value, ButtonPropertyClicked clickDelegate, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Button,
                    label = CreateLabel(label),
                    control = CreateButton(value, tooltip),
                    click = clickDelegate
                });
        }

        public void AddLabel(string label, string value, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Label,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateLabel(value, tooltip)
                });
        }

        public void AddLinkLabel(string label, string value, string url, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Label,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateLinkLabel(value, url, tooltip)
                });
        }

        public void AddColor(Color color)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Color,
                    control = CreatePictureBox(color)
                });
        }

        public void AddIntegerRange(string label, int value, int min, int max, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.IntegerRange,
                    label = CreateLabel(label, tooltip),
                    control = CreateNumericUpDown(value, min, max, tooltip)
                });
        }

        public void AddProgressBar(string label, float value)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.ProgressBar,
                    label = CreateLabel(label),
                    control = CreateProgressBar(value)
                });
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
                    type = PropertyType.DomainRange,
                    label = CreateLabel(label),
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

        public void SetStringListIndex(int idx, int selIdx)
        {
            (properties[idx].control as ComboBox).SelectedIndex = selIdx;
        }

        public void AddBoolean(string label, bool value, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Boolean,
                    label = CreateLabel(label, tooltip),
                    control = CreateCheckBox(value, "", tooltip)
                });
        }

        public void AddLabelBoolean(string label, bool value, int margin = 0)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Boolean,
                    control = CreateCheckBox(value, label),
                    leftMarging = margin
                });
        }

        public void AddStringList(string label, string[] values, string value, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.StringList,
                    label = CreateLabel(label, tooltip),
                    control = CreateDropDownList(values, value, tooltip)
                });
        }

        public void AddStringListMulti(string label, string[] values, bool[] selected)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.StringListMulti,
                    label = label != null ? CreateLabel(label) : null,
                    control = CreateCheckedListBox(values, selected)
                });
        }

        public void SetPropertyEnabled(int idx, bool enabled)
        {
            var label = properties[idx].control as Label;

            if (label != null)
            {
                label.ForeColor = enabled ? ThemeBase.LightGreyFillColor2 : ThemeBase.MediumGreyFillColor1;
            }
            else
            {
                properties[idx].control.Enabled = enabled;
            }
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

        public object GetPropertyValue(int idx)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.String:
                case PropertyType.ColoredString:
                case PropertyType.MultilineString:
                    return (prop.control as TextBox).Text;
                case PropertyType.IntegerRange:
                    return (int)(prop.control as NumericUpDown).Value;
                case PropertyType.DomainRange:
                    return int.TryParse(prop.control.Text, out var val) ? val : 0;
                case PropertyType.Boolean:
                    return (prop.control as CheckBox).Checked;
                case PropertyType.Color:
                    return (prop.control as PictureBox).BackColor;
                case PropertyType.StringList:
                    return (prop.control as ComboBox).Text;
                case PropertyType.StringListMulti:
                    {
                        var listBox = prop.control as CheckedListBox;
                        var selected = new bool[listBox.Items.Count];
                        for (int i = 0; i < listBox.Items.Count; i++)
                            selected[i] = listBox.GetItemChecked(i);
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

        public void SetPropertyValue(int idx, object value)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.Boolean:
                    (prop.control as CheckBox).Checked = (bool)value;
                    break;
                case PropertyType.Button:
                    (prop.control as Button).Text = (string)value;
                    break;
                case PropertyType.MultilineString:
                    (prop.control as TextBox).Text = (string)value;
                    break;
                case PropertyType.ProgressBar:
                    (prop.control as ProgressBar).Value = (int)Math.Round((float)value * 1000);
                    break;
            }
        }
        
        public void Build()
        {
            SuspendLayout();

            int margin = (int)(5 * RenderTheme.DialogScaling);
            int maxLabelWidth = 0;
            int defaultLabelHeight = 24;

            // Workaround scaling issue with checkboxes. 
            // Measure a label and well use this for checkbox.
            if (RenderTheme.DialogScaling > 1.0f)
            {
                Label testLabel = CreateLabel("888");
                Controls.Add(testLabel);
                defaultLabelHeight = Math.Max(defaultLabelHeight, testLabel.Height) + 2;
                Controls.Remove(testLabel);
            }

            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];

                if (prop.label != null)
                {
                    // This is really ugly. We cant measure the labels unless they are added.
                    Controls.Add(prop.label);
                    maxLabelWidth = Math.Max(maxLabelWidth, prop.label.Width);
                    Controls.Remove(prop.label);
                }
            }

            int widthNoMargin = Width - (margin * 2);
            int totalHeight = margin;

            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];
                var height = 0;

                // Hack for checkbox that dont scale with Hi-DPI. 
                if (RenderTheme.DialogScaling > 1.0f && prop.control is CheckBox)
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
                    prop.control.Width = widthNoMargin - maxLabelWidth;

                    Controls.Add(prop.label);
                    Controls.Add(prop.control);

                    height = prop.label.Height;
                }
                else
                {
                    prop.control.Left  = margin + prop.leftMarging;
                    prop.control.Top   = totalHeight;
                    prop.control.Width = widthNoMargin;

                    Controls.Add(prop.control);
                }

                height = Math.Max(prop.control.Height, height);
                totalHeight += height + margin;
            }

            Height = totalHeight;
            layoutHeight = totalHeight;
            ResumeLayout();
        }
    }
}
