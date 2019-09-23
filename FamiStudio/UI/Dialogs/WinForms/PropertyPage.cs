using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

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
        Color
    };

    public partial class PropertyPage : UserControl
    {
        class Property
        {
            public PropertyType type;
            public Label label;
            public Control control;
        };

        int layoutHeight;
        Font font;
        Bitmap colorBitmap;
        List<Property> properties = new List<Property>();

        public delegate void PropertyChangedDelegate(PropertyPage props, int idx, object value);
        public event PropertyChangedDelegate PropertyChanged;
        public delegate void PropertyWantsCloseDelegate(int idx);
        public event PropertyWantsCloseDelegate PropertyWantsClose;

        public int LayoutHeight => layoutHeight;

        public PropertyPage()
        {
            InitializeComponent();

            // Happens in design mode
            try
            {
                font = new Font(PlatformDialogs.PrivateFontCollection.Families[0], 10.0f, FontStyle.Regular);
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

        private Label CreateLabel(string str)
        {
            var label = new Label();

            label.Text = str;
            label.Font = font;
            label.AutoSize = true;
            label.ForeColor = ThemeBase.LightGreyFillColor2;
            label.BackColor = BackColor;

            return label;
        }

        private TextBox CreateColoredTextBox(string txt, Color backColor)
        {
            var textBox = new TextBox();

            textBox.Text = txt;
            textBox.Font = font;
            textBox.BackColor = backColor;

            return textBox;
        }

        private TextBox CreateTextBox(string txt, int maxLength)
        {
            var textBox = new TextBox();

            textBox.Text = txt;
            textBox.Font = font;
            textBox.MaxLength = maxLength;

            return textBox;
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

        private NumericUpDown CreateNumericUpDown(int value, int min, int max)
        {
            var upDown = new NumericUpDown();

            upDown.Font = font;
            upDown.Minimum = min;
            upDown.Maximum = max;
            upDown.Text = value.ToString();
            upDown.ValueChanged += UpDown_ValueChanged;

            return upDown;
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

        private CheckBox CreateCheckBox(bool value)
        {
            var cb = new CheckBox();

            cb.Text = "";
            cb.Checked = value;
            cb.Font = font;
            cb.CheckedChanged += Cb_CheckedChanged;

            return cb;
        }

        private void Cb_CheckedChanged(object sender, EventArgs e)
        {
            int idx = GetPropertyIndex(sender as Control);
            PropertyChanged?.Invoke(this, idx, GetPropertyValue(idx));
        }

        private ComboBox CreateDropDownList(string[] values, string value)
        {
            var cb = new ComboBox();

            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.Items.AddRange(values);
            cb.Text = value;
            cb.Font = font;

            return cb;
        }

        private CheckedListBox CreateCheckedListBox(string[] values, bool[] selected)
        {
            var listBox = new PaddedCheckedListBox();

            for (int i = 0; i < values.Length; i++)
            {
                listBox.Items.Add(values[i], selected != null ? selected[i] : true);
            }

            listBox.IntegralHeight = false;
            listBox.Font = font;
            listBox.Height = 200;
            listBox.CheckOnClick = true;
            listBox.SelectionMode = SelectionMode.One;

            return listBox;
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

        public void AddString(string label, string value, int maxLength = 0)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.String,
                    label = CreateLabel(label),
                    control = CreateTextBox(value, maxLength)
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

        public void AddIntegerRange(string label, int value, int min, int max)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.IntegerRange,
                    label = CreateLabel(label),
                    control = CreateNumericUpDown(value, min, max)
                });
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

        public void AddBoolean(string label, bool value)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Boolean,
                    label = CreateLabel(label),
                    control = CreateCheckBox(value)
                });
        }

        public void AddStringList(string label, string[] values, string value)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.StringList,
                    label = CreateLabel(label),
                    control = CreateDropDownList(values, value)
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
            properties[idx].control.Enabled = enabled;
        }

        public object GetPropertyValue(int idx)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.String:
                case PropertyType.ColoredString:
                    return (prop.control as TextBox).Text;
                case PropertyType.IntegerRange:
                    return (int)(prop.control as NumericUpDown).Value;
                case PropertyType.DomainRange:
                    return (int)(prop.control as DomainUpDown).SelectedItem;
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
            }

            return null;
        }

        public T GetPropertyValue<T>(int idx)
        {
            return (T)GetPropertyValue(idx);
        }

        public void Build()
        {
            SuspendLayout();

            const int margin = 5;

            int maxLabelWidth = 0;

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
                    prop.control.Left  = margin;
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
