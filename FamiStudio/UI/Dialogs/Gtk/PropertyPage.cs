using System;
using System.Collections.Generic;
using System.Diagnostics;
using Gdk;
using Gtk;
using Pango;

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

    public class PropertyPage : Gtk.Table
    {
        class Property
        {
            public PropertyType type;
            public Label label;
            public Widget control;
        };

        private object userData;
        private System.Drawing.Color color;
        private Pixbuf colorBitmap;
        private List<Property> properties = new List<Property>();

        public delegate void PropertyChangedDelegate(PropertyPage props, int idx, object value);
        public event PropertyChangedDelegate PropertyChanged;
        public delegate void PropertyWantsCloseDelegate(int idx);
        public event PropertyWantsCloseDelegate PropertyWantsClose;
        public new object UserData { get => userData; set => userData = value; }
        public int PropertyCount => properties.Count;

        public PropertyPage() : base(1, 1, false)
        {
            //AddColoredString("Hello!", System.Drawing.Color.FromArgb(0, 128, 0));
            //AddString("Allo oii oQi ", "Qoto");
            //AddIntegerRange("Number", 7, 0, 16);
            //AddDomainRange("Domain", new[] { 3, 7, 11, 16, 22 }, 11);
            //AddString("123Q", "44QQ4");
            //AddBoolean("YoQQ!", true);
            //AddStringListMulti(null, new[] {"Item1", "Item2", "Item3" }, new[] { false, true, true });
            //AddStringList("Combo", new[] { "Qal1", "Qal2", "Qal3" }, "Qal2");
            //AddColor(System.Drawing.Color.FromArgb(220, 100, 170));
            //Build();
        }

        private int GetPropertyIndex(Widget ctrl)
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
        private unsafe Gdk.Pixbuf GetColorBitmap()
        {
            if (colorBitmap == null)
            {
                var width  = ThemeBase.CustomColors.GetLength(0);
                var height = ThemeBase.CustomColors.GetLength(1);
                var data = new byte[width * height * 4];

                for (int j = 0, p = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        var color = ThemeBase.CustomColors[i, j];

                        data[p++] = color.R;
                        data[p++] = color.G;
                        data[p++] = color.B;
                        data[p++] = 255;
                    }
                }

                colorBitmap = new Pixbuf(data, true, 8, width, height, width * 4);
            }

            return colorBitmap;
        }

        private Label CreateLabel(string str)
        {
            var label = new Label();

            label.Text = str;
            label.SetAlignment(0.0f, 0.5f);

            return label;
        }

        private Entry CreateColoredTextBox(string txt, System.Drawing.Color backColor)
        {
            var textBox = new Entry();

            textBox.Text = txt;
            textBox.ModifyBase(StateType.Normal, ToGdkColor(backColor));
            textBox.WidthRequest = 50;

            return textBox;
        }

        private Entry CreateTextBox(string txt, int maxLength)
        {
            var textBox = new Entry();

            textBox.Text = txt;
            textBox.MaxLength = maxLength;
            textBox.WidthRequest = 50;

            return textBox;
        }

        private CheckButton CreateCheckBox(bool value, string text = "")
        {
            var cb = new CheckButton();
            cb.Active = value;
            cb.Label = text;
            cb.Toggled += Cb_Toggled;

            return cb;
        }

        private void Cb_Toggled(object sender, EventArgs e)
        {
            int idx = GetPropertyIndex(sender as Widget);
            PropertyChanged?.Invoke(this, idx, GetPropertyValue(idx));
        }

        private ColorSelector CreatePictureBox(System.Drawing.Color color) 
        {
            var pictureBox = new ColorSelector(GetColorBitmap(), ThemeBase.GetCustomColorIndex(color));

            pictureBox.Show();
            pictureBox.ButtonPressEvent += PictureBox_ButtonPressEvent;
            pictureBox.MotionNotifyEvent += PictureBox_MotionNotifyEvent;

            this.color = color;

            return pictureBox; 
        }

        private Gdk.Color ToGdkColor(System.Drawing.Color color)
        {
            return new Gdk.Color(color.R, color.G, color.B);
        }

        private void ChangeColor(ColorSelector image, int x, int y)
        {
            int colorSizeX = ThemeBase.CustomColors.GetLength(0);
            int colorSizeY = ThemeBase.CustomColors.GetLength(1);
            int imageWidth  = image.Allocation.Width;
            int imageHeight = image.Allocation.Height;

            int i = Math.Min(colorSizeX - 1, Math.Max(0, (int)(x / (float)imageWidth  * colorSizeX)));
            int j = Math.Min(colorSizeY - 1, Math.Max(0, (int)(y / (float)imageHeight * colorSizeY)));

#if FAMISTUDIO_LINUX
            image.SelectedColor = j * colorSizeX + i;
#else
            foreach (var prop in properties)
            {
                if (prop.type == PropertyType.ColoredString)
                {
                    prop.control.ModifyBase(StateType.Normal, ToGdkColor(ThemeBase.CustomColors[i, j]));
                }
            }
#endif

            color = ThemeBase.CustomColors[i, j];
        }

        private void PictureBox_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            var e = args.Event;

            ChangeColor(o as ColorSelector, (int)e.X, (int)e.Y);

            if (e.Type == EventType.TwoButtonPress ||
                e.Type == EventType.ThreeButtonPress)
            {
                PropertyWantsClose?.Invoke(GetPropertyIndex(o as Widget));
            }
        }

        private void PictureBox_MotionNotifyEvent(object o, MotionNotifyEventArgs args)
        {
            var e = args.Event;
            ChangeColor(o as ColorSelector, (int)e.X, (int)e.Y);
        }

        private SpinButton CreateNumericUpDown(int value, int min, int max)
        {
            var upDown = new SpinButton(min, max, 1);

            upDown.Value = value;
            upDown.ValueChanged += UpDown_ValueChanged1;

            return upDown;
        }

        private void UpDown_ValueChanged1(object sender, EventArgs e)
        {
            int idx = GetPropertyIndex(sender as Widget);
            PropertyChanged?.Invoke(this, idx, GetPropertyValue(idx));
        }

        private DomainSpinButton CreateDomainUpDown(int[] values, int value)
        {
            return new DomainSpinButton(values, value);
        }

        private ComboBox CreateDropDownList(string[] values, string value)
        {
            var cb = ComboBox.NewText();

            for (int i = 0; i < values.Length; i++)
            {
                cb.AppendText(values[i]);
                if (values[i] == value)
                    cb.Active = i;
            }

            cb.Sensitive = values.Length > 0;
            return cb;
        }

        private CheckBoxList CreateCheckedListBox(string[] values, bool[] selected)
        {
            return new CheckBoxList(values, selected);
        }

        public void AddColoredString(string value, System.Drawing.Color color)
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

        public void AddLabel(string label, string value)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Boolean,
                    label = CreateLabel(label),
                    control = CreateLabel(value)
                });
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

        public void AddLabelBoolean(string label, bool value)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Boolean,
                    control = CreateCheckBox(value, label)
                });
        }

        public void AddColor(System.Drawing.Color color)
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

        public void UpdateIntegerRange(int idx, int min, int max)
        {
            var spin = (properties[idx].control as SpinButton);
            spin.SetRange(min, max);
        }

        public void UpdateIntegerRange(int idx, int value, int min, int max)
        {
            var spin = (properties[idx].control as SpinButton);
            spin.SetRange(min, max);
            spin.Value = value;
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
            var spin = (properties[idx].control as DomainSpinButton);

            spin.UpdateValues(values, value);
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
            properties[idx].control.Sensitive = enabled;
        }

        public void SetLabelText(int idx, string text)
        {
            (properties[idx].control as Label).Text = text;
        }

        public object GetPropertyValue(int idx)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.String:
                case PropertyType.ColoredString:
                    return (prop.control as Entry).Text;
                case PropertyType.IntegerRange:
                    return (int)(prop.control as SpinButton).Value;
                case PropertyType.DomainRange:
                    return int.Parse((prop.control as DomainSpinButton).Text);
                case PropertyType.Boolean:
                    return (prop.control as CheckButton).Active;
                case PropertyType.Color:
                    return color;
                case PropertyType.StringList:
                    return (prop.control as ComboBox).ActiveText;
                case PropertyType.StringListMulti:
                    return (prop.control as CheckBoxList).GetSelected();
            }

            return null;
        }

        public T GetPropertyValue<T>(int idx)
        {
            return (T)GetPropertyValue(idx);
        }

        public void Build()
        {
            Resize((uint)properties.Count, 2);

            ColumnSpacing = 5;
            RowSpacing    = 5;
            
            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];

                if (prop.label != null)
                {
                    Attach(prop.label,   0, 1, (uint)i, (uint)(i + 1));
                    Attach(prop.control, 1, 2, (uint)i, (uint)(i + 1));

                    prop.label.Show();
                    prop.control.Show();
                }
                else
                {
                    // HACK: Cant be bothered to deal with GTK+2 aspect ratios.
                    if (prop.control is ColorSelector img)
                        img.DesiredWidth = Toplevel.WidthRequest - 10; // (10 = Border * 2)

                    Attach(prop.control, 0, 2, (uint)i, (uint)(i + 1));
                    prop.control.Show();
                }
            }
        }
    }
}
