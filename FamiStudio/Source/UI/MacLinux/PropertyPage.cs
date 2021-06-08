using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Gdk;
using Gtk;
using Pango;

#if FAMISTUDIO_LINUX
    using WarningImage = Gtk.Image;
#else
    using WarningImage = FamiStudio.CairoImage;
#endif

namespace FamiStudio
{
    public partial class PropertyPage : Gtk.Table
    {
        private static Pixbuf[] warningIcons;

        class Property
        {
            public PropertyType type;
            public Label label;
            public Widget control;
            public int leftMargin;
            public string sliderFormat;
            public WarningImage warningIcon;
            public ColumnDesc[] columns;
            public string multilineLabelText; // HACK for multiline labels.
        };

        private System.Drawing.Color color;
        private Pixbuf colorBitmap;
        private List<Property> properties = new List<Property>();

        // For some grid operators.
        private TreePath dragPath;
        private TreeViewColumn dragColumn;
        private int dragPropertyIndex = -1;
        private int dragRowIndex = -1;
        private int dragColIndex = -1;

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

            if (warningIcons == null)
            {
                var suffix = GLTheme.DialogScaling >= 2.0f ? "@2x" : "";

                warningIcons = new Pixbuf[3];
                warningIcons[0] = Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.WarningGood{suffix}.png");
                warningIcons[1] = Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.WarningYellow{suffix}.png");
                warningIcons[2] = Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.Warning{suffix}.png");
            }
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

        private Label CreateLabel(string str, string tooltip = null, bool multiline = false)
        {
            Debug.Assert(!string.IsNullOrEmpty(str));

            var label = new Label();

            label.Text = str;
            label.SetAlignment(0.0f, 0.5f);
            label.TooltipText = tooltip;

            if (multiline)
            {
                label.Wrap = true;
                label.SizeAllocated += Label_SizeAllocated;
                label.Text = "a";
            }

            return label;
        }

        void Label_SizeAllocated(object o, SizeAllocatedArgs args)
        {
            var idx = GetPropertyIndex(o as Widget);
            var prop = properties[idx];

            if (prop.multilineLabelText != null)
            {
                var lbl = o as Label;
                lbl.WidthRequest = args.Allocation.Width - 1;
                lbl.Text = prop.multilineLabelText;
                prop.multilineLabelText = null;
            }
        }

        private LinkButton CreateLinkLabel(string str, string url, string tooltip = null)
        {
            var label = new LinkButton(url, str);

            label.SetAlignment(0.0f, 0.5f);
            label.TooltipText = tooltip;

            return label;
        }

        private Entry CreateColoredTextBox(string txt, System.Drawing.Color backColor)
        {
            var textBox = new Entry();

            textBox.Text = txt;
            textBox.ModifyBase(StateType.Normal, ToGdkColor(backColor));
            textBox.WidthRequest = GtkUtils.ScaleGtkWidget(50);

            return textBox;
        }

        private Entry CreateTextBox(string txt, int maxLength, string tooltip = null)
        {
            var textBox = new Entry();

            textBox.Text = txt;
            textBox.MaxLength = maxLength;
            textBox.WidthRequest = GtkUtils.ScaleGtkWidget(50);
            textBox.TooltipText = tooltip;

            return textBox;
        }

        private ScrolledWindow CreateMultilineTextBox(string txt)
        {
            var textView = new TextView();
            textView.Buffer.Text = txt;
            textView.Editable = false;
            textView.CursorVisible = false;
#if FAMISTUDIO_MACOS
            textView.ModifyFont(FontDescription.FromString($"Quicksand 10"));
#else
            textView.ModifyFont(FontDescription.FromString($"Quicksand 8"));
#endif
            textView.WrapMode = Gtk.WrapMode.WordChar;
            textView.Show();

            var scroll = new ScrolledWindow(null, null);
            scroll.HscrollbarPolicy = PolicyType.Never;
            scroll.VscrollbarPolicy = PolicyType.Automatic;
            scroll.HeightRequest = GtkUtils.ScaleGtkWidget(400);
            scroll.ShadowType = ShadowType.EtchedIn;
            scroll.Show();
            scroll.Add(textView);

            return scroll;
        }

        private CheckButton CreateCheckBox(bool value, string text = null, string tooltip = null)
        {
            var cb = new CheckButton();
            cb.CanFocus = false;
            cb.Active = value;
            cb.Label = text;
            cb.TooltipText = tooltip;
            cb.Toggled += Cb_Toggled;

            return cb;
        }

        private void Cb_Toggled(object sender, EventArgs e)
        {
            int idx = GetPropertyIndex(sender as Widget);
            PropertyChanged?.Invoke(this, idx, -1, -1, GetPropertyValue(idx));
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

        private SpinButton CreateNumericUpDown(int value, int min, int max, string tooltip = null)
        {
            var upDown = new SpinButton(min, max, 1);

            upDown.Value = value;
            upDown.TooltipText = tooltip;
            upDown.ValueChanged += UpDown_ValueChanged1;

            return upDown;
        }

        private ProgressBar CreateProgressBar(float value)
        {
            var progress = new ProgressBar();
            return progress;
        }

        private RadioButton CreateRadioButton(string text, bool check)
        {
            RadioButton group = null;
            foreach (var p in properties)
            {
                if (p.type == PropertyType.Radio)
                {
                    group = p.control as RadioButton;
                    break;
                }
            }

            var radio = new RadioButton(group, "a");

            // HACK : The only radio button we create are multiline.
            var label = radio.Child as Label;
            label.Wrap = true;
            radio.SizeAllocated += RadioLabel_SizeAllocated;
            radio.Active = check;

            return radio;
        }

        void RadioLabel_SizeAllocated(object o, SizeAllocatedArgs args)
        {
            var radio = o as RadioButton;
            var idx = GetPropertyIndex(radio);
            var prop = properties[idx];

            if (prop.multilineLabelText != null)
            {
                var lbl = radio.Child as Label;
                lbl.Text = prop.multilineLabelText;
                lbl.WidthRequest = args.Allocation.Width - GtkUtils.ScaleGtkWidget(32);
                prop.multilineLabelText = null;
            }
        }

        private void UpDown_ValueChanged1(object sender, EventArgs e)
        {
            int idx = GetPropertyIndex(sender as Widget);
            PropertyChanged?.Invoke(this, idx, -1, -1, GetPropertyValue(idx));
        }

        private DomainSpinButton CreateDomainUpDown(int[] values, int value)
        {
            return new DomainSpinButton(values, value);
        }

        private ComboBox CreateDropDownList(string[] values, string value, string tooltip = null)
        {
            var cb = ComboBox.NewText();

            for (int i = 0; i < values.Length; i++)
            {
                cb.AppendText(values[i]);
                if (values[i] == value)
                    cb.Active = i;
            }

            cb.Sensitive = values.Length > 0;
            cb.TooltipText = tooltip;
            cb.WidthRequest = GtkUtils.ScaleGtkWidget(125);
            cb.Changed += Cb_Changed;

            return cb;
        }

        private void Cb_Changed(object sender, EventArgs e)
        {
            int idx = GetPropertyIndex(sender as Widget);
            PropertyChanged?.Invoke(this, idx, -1, -1, GetPropertyValue(idx));
        }

        private CheckBoxList CreateCheckedListBox(string[] values, bool[] selected)
        {
            return new CheckBoxList(values, selected);
        }

        public void UpdateCheckBoxList(int idx, string[] values, bool[] selected)
        {
            var listBox = (properties[idx].control as CheckBoxList);
            listBox.Update(values, selected);
        }

        public void UpdateCheckBoxList(int idx, bool[] selected)
        {
            var listBox = (properties[idx].control as CheckBoxList);
            listBox.Update(selected);
        }

        private Button CreateButton(string text, string tooltip)
        {
            var button = new Button();
            button.Label = text;
            button.TooltipText = tooltip;
            button.Clicked += Button_Clicked;         
            return button;
        }

        public int AddColoredString(string value, System.Drawing.Color color)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.ColoredString,
                    control = CreateColoredTextBox(value, color)
                });
            return properties.Count - 1;
        }

        public int AddString(string label, string value, int maxLength = 0)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.String,
                    label = label != null ? CreateLabel(label) : null,
                    control = CreateTextBox(value, maxLength)
                });
            return properties.Count - 1;
        }

        public int AddMultilineString(string label, string value)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.MultilineString,
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

        void Button_Clicked(object sender, EventArgs e)
        {
            var propIdx = GetPropertyIndex(sender as Widget);
            PropertyClicked?.Invoke(this, ClickType.Button, propIdx, -1, -1);
        }

        public int AddLabel(string label, string value, bool multiline = false, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Label,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateLabel(value, tooltip, multiline),
                    multilineLabelText = multiline ? value : null
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

        public int AddCheckBox(string label, bool value, string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.CheckBox,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateCheckBox(value)
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
                    leftMargin = margin
                });
            return properties.Count - 1;
        }

        public int AddColorPicker(System.Drawing.Color color)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.ColorPicker,
                    control = CreatePictureBox(color)
                });
            return properties.Count - 1;
        }

        public int AddIntegerRange(string label, int value, int min, int max, string tooltip = null)
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
                    control = CreateRadioButton(text, check),
                    multilineLabelText = text
                });
            return properties.Count - 1;
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

        public int AddDomainRange(string label, int[] values, int value)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.DomainUpDown,
                    label = label != null ? CreateLabel(label) : null,
                    control = CreateDomainUpDown(values, value)
                });
            return properties.Count - 1;
        }

        public void UpdateDomainRange(int idx, int[] values, int value)
        {
            var spin = (properties[idx].control as DomainSpinButton);

            spin.UpdateValues(values, value);
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

        public void SetDropDownListIndex(int idx, int selectedIndex)
        {
            var combo = (properties[idx].control as ComboBox);
            combo.Active = selectedIndex;
        }

        public void UpdateDropDownListItems(int idx, string[] values)
        {
            var combo = (properties[idx].control as ComboBox);
            var selectedIdx = combo.Active;

            combo.Model = new ListStore(typeof(string));
            for (int i = 0; i < values.Length; i++)
                combo.AppendText(values[i]);

            if (selectedIdx < values.Length)
                combo.Active = selectedIdx;
            else
                combo.Active = 0;
        }

        public int AddCheckBoxList(string label, string[] values, bool[] selected)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.CheckBoxList,
                    label = label != null ? CreateLabel(label) : null,
                    control = CreateCheckedListBox(values, selected)
                });
            return properties.Count - 1;
        }

        private ListStore CreateListStoreFromData(ColumnDesc[] columns, object[,] data)
        {
            var types = new Type[data.GetLength(1)];

            for (int i = 0; i < types.Length; i++)
                types[i] = columns[i].GetPropertyType();

            var listStore = new ListStore(types);

            for (int j = 0; j < data.GetLength(0); j++)
            {
                var values = new object[data.GetLength(1)];

                for (int i = 0; i < data.GetLength(1); i++)
                    values[i] = data[j, i];

                listStore.AppendValues(values);
            }

            return listStore;
        }

        private ScrolledWindow CreateTreeView(ColumnDesc[] columnDescs, object[,] data, int height)
        {
            var treeView = new TreeView();

            for (int i = 0; i < columnDescs.Length; i++)
            {
                var desc = columnDescs[i];
                var renderer = (CellRenderer)null;
                var attr = "text";

                switch (desc.Type)
                {
                    case ColumnType.Label:
                    {
                        var textRenderer = new CellRendererText();
                        renderer = textRenderer;
                        break;
                    }
                    case ColumnType.CheckBox:
                    {
                        var toggleRenderer = new CellRendererToggle();
                        toggleRenderer.Toggled += ToggleRenderer_Toggled;
                        renderer = toggleRenderer;
                        attr = "active";
                        break;
                    }
                    case ColumnType.DropDown:
                    {
                        var comboRenderer = new CellRendererCombo();
                        var listStore = new ListStore(typeof(string));
                        for (int j = 0; j < desc.DropDownValues.Length; j++)
                            listStore.AppendValues(desc.DropDownValues[j]);
                        comboRenderer.Editable = true;
                        comboRenderer.HasEntry = false;
                        comboRenderer.Model = listStore;
                        comboRenderer.Edited += ComboRenderer_Edited;
                        comboRenderer.TextColumn = 0;
                        renderer = comboRenderer;
                        break;
                    }
                    case ColumnType.Slider:
                    {
                        // TODO : Right now, we dont apply the formatting. To support
                        // this we will need to add an extra string column to our model,
                        // update it with the formatting and tell the renderer to use
                        // that volum as the "text" attribute. Or create our own cell
                        // renderer that supports formatting.
                        var sliderRenderer = new CellRendererProgress();
                        attr = "value";
                        renderer = sliderRenderer;
                        break;
                    }
                    case ColumnType.Button:
                    {
                        var buttonRenderer = new CellRendererButton();
                        renderer = buttonRenderer;
                        break;
                    }
                }

                renderer.Data[0] = treeView;

                var column = new TreeViewColumn(columnDescs[i].Name, renderer, attr, i);
                column.SortColumnId = -1; // Disable sorting
                treeView.AppendColumn(column);
            }

            if (data == null)
                data = new string[0, 0];

            treeView.Model = CreateListStoreFromData(columnDescs, data);
            treeView.EnableGridLines = TreeViewGridLines.Both;
            treeView.ButtonPressEvent += TreeView_ButtonPressEvent;
            treeView.ButtonReleaseEvent += TreeView_ButtonReleaseEvent;
            treeView.Selection.Mode = SelectionMode.None;
            treeView.Show();

            treeView.Events |= EventMask.PointerMotionMask;
            treeView.MotionNotifyEvent += TreeView_MotionNotifyEvent;

            var scroll = new ScrolledWindow(null, null);
            scroll.HscrollbarPolicy = PolicyType.Never;
            scroll.VscrollbarPolicy = PolicyType.Automatic;
            scroll.HeightRequest = GtkUtils.ScaleGtkWidget(height);
            scroll.ShadowType = ShadowType.EtchedIn;
            scroll.Add(treeView);

            return scroll;
        }

        int GetColumnIndex(TreeView treeView, CellRenderer renderer)
        { 
            for (int i = 0; i < treeView.Columns.Length; i++)
            {
                if (treeView.Columns[i].CellRenderers[0] == renderer)
                    return i;
            }

            Debug.Assert(false);
            return -1;
        }

        void ToggleRenderer_Toggled(object o, ToggledArgs args)
        {
            var renderer = o as CellRenderer;
            var treeView = renderer.Data[0] as TreeView;
            var colIdx = GetColumnIndex(treeView, renderer);

            treeView.Model.GetIter(out var iter, new TreePath(args.Path));
            var newVal = !(bool)treeView.Model.GetValue(iter, colIdx);
            treeView.Model.SetValue(iter, colIdx, newVal);
        }

        void ComboRenderer_Edited(object o, EditedArgs args)
        {
            var renderer = o as CellRenderer;
            var treeView = renderer.Data[0] as TreeView;
            var colIdx = GetColumnIndex(treeView, renderer);

            treeView.Model.GetIter(out var iter, new TreePath(args.Path));
            treeView.Model.SetValue(iter, colIdx, args.NewText);
        }

        [GLib.ConnectBefore]
        void TreeView_MotionNotifyEvent(object o, MotionNotifyEventArgs args)
        {
            var treeView = o as TreeView;

            if (dragPath != null)
            {
                var area = treeView.GetCellArea(dragPath, dragColumn);
                var percent = (int)Utils.Clamp(Math.Round((args.Event.X - area.Left) / (float)area.Width * 100.0f), 0.0f, 100.0f);

                if (treeView.Model.GetIter(out var iter, dragPath))
                    treeView.Model.SetValue(iter, dragColIndex, percent);
            }
            else if (treeView.GetPathAtPos((int)args.Event.X, (int)args.Event.Y, out var path, out var col, out var ix, out var iy))
            {
                var columnIndex = Array.IndexOf(treeView.Columns, col);

                if (treeView.Columns[columnIndex].CellRenderers[0] is CellRendererButton button)
                {
                    button.LastMouseX = (int)args.Event.X;
                    button.LastMouseY = (int)args.Event.Y;
                    treeView.QueueDraw();
                }
            }
        }

        [GLib.ConnectBefore]
        void TreeView_ButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];

                if (prop.control is ScrolledWindow scroll)
                {
                    if (scroll.Child is TreeView treeView)
                    {
                        if (treeView.GetPathAtPos((int)args.Event.X, (int)args.Event.Y, out var path, out var col, out var ix, out var iy))
                        {
                            var columnIndex = Array.IndexOf(treeView.Columns, col);
                            var columnDesc = prop.columns[columnIndex];

                            if (columnDesc.Type == ColumnType.Slider && args.Event.Button == 1)
                            {
                                var area = treeView.GetCellArea(path, col);

                                dragPath = path;
                                dragColumn = col;
                                dragPropertyIndex = i;
                                dragRowIndex = path.Indices[0];
                                dragColIndex = columnIndex;

                                var percent = (int)Utils.Clamp(Math.Round((args.Event.X - area.Left) / (float)area.Width * 100.0f), 0.0f, 100.0f);

                                if (treeView.Model.GetIter(out var iter, path))
                                    treeView.Model.SetValue(iter, columnIndex, percent);
                            }
                            else if (columnDesc.Type == ColumnType.Button)
                            {
                                var cellArea = treeView.GetBackgroundArea(path, col);
                                var button = treeView.Columns[columnIndex].CellRenderers[0] as CellRendererButton;
                                var buttonRect = button.GetButtonRectangle(cellArea);

                                if (buttonRect.Contains((int)args.Event.X, (int)args.Event.Y))
                                {
                                    PropertyClicked?.Invoke(this, ClickType.Button, i, path.Indices[0], columnIndex);
                                }
                            }
                            else
                            {
                                if (args.Event.Type == EventType.TwoButtonPress)
                                {
                                    PropertyClicked?.Invoke(this, ClickType.Double, i, path.Indices[0], columnIndex);
                                }
                                else if (args.Event.Button == 3)
                                {
                                    PropertyClicked?.Invoke(this, ClickType.Right, i, path.Indices[0], columnIndex);
                                }
                            }
                        }
                    }
                }
            }
        }

        [GLib.ConnectBefore]
        void TreeView_ButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
        {
            // MATTT : Trigger change event here!
            if (dragPath != null)
            {
                dragPath = null;
                dragColumn = null;
                dragPropertyIndex = -1;
                dragRowIndex = -1;
                dragColIndex = -1;
            }
        }

        public int AddMultiColumnList(ColumnDesc[] columnDescs, object[,] data, int height = 300)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.CheckBoxList,
                    control = CreateTreeView(columnDescs, data, height),
                    columns = columnDescs
                });
            return properties.Count - 1;
        }

        public void UpdateMultiColumnList(int idx, string[,] data, string[] columnNames = null)
        {
            var scroll = properties[idx].control as ScrolledWindow;
            var treeView = scroll.Child as TreeView;

            var ls = treeView.Model as ListStore;
            var count = ls.IterNChildren();

            if (count == data.GetLength(0))
            {
                if (ls.GetIterFirst(out var it))
                {
                    var j = 0;

                    do
                    {
                        var values = new string[data.GetLength(1)];

                        for (int i = 0; i < data.GetLength(1); i++)
                            values[i] = data[j, i];

                        ls.SetValues(it, values);
                        j++;
                    }
                    while (ls.IterNext(ref it));
                }
            }
            else
            {
                treeView.Model = CreateListStoreFromData(properties[idx].columns, data);
            }

            if (columnNames != null)
            {
                Debug.Assert(treeView.Columns.Length == columnNames.Length);
                for (int i = 0; i < treeView.Columns.Length; i++)
                {
                    treeView.Columns[i].Title = columnNames[i];
                }
            }
        }

        private HScale CreateSlider(double value, double min, double max, double increment, int numDecimals, string tooltip = null)
        {
            var scale = new HScale(min, max, increment);
            scale.DrawValue = true;
            scale.ValuePos = PositionType.Right;
            scale.Value = value;
            scale.FormatValue += Scale_FormatValue;
            scale.ChangeValue += Scale_ChangeValue;
            return scale;
        }

        void Scale_ChangeValue(object o, ChangeValueArgs args)
        {
            int idx = GetPropertyIndex(o as Widget);
            PropertyChanged?.Invoke(this, idx, -1, -1, GetPropertyValue(idx));
        }

        void Scale_FormatValue(object o, FormatValueArgs args)
        {
            var idx = GetPropertyIndex(o as Widget);

            if (idx >= 0 && properties[idx].sliderFormat != null)
                args.RetVal = string.Format(properties[idx].sliderFormat, args.Value);
        }

        public int AddSlider(string label, double value, double min, double max, double increment, int numDecimals, string format = "{0}", string tooltip = null)
        {
            properties.Add(
                new Property()
                {
                    type = PropertyType.Slider,
                    label = label != null ? CreateLabel(label, tooltip) : null,
                    control = CreateSlider(value, min, max, increment, numDecimals, tooltip),
                    sliderFormat = format
                });
            return properties.Count - 1;
        }

        public void SetPropertyEnabled(int idx, bool enabled)
        {
            properties[idx].control.Sensitive = enabled;
        }

        public void SetLabelText(int idx, string text)
        {
            (properties[idx].control as Label).Text = text;
        }

        public void NotifyClosing()
        {
#if !FAMISTUDIO_WINDOWS
            foreach (var prop in properties)
            { 
                if (prop.control is SpinButton)
                    (prop.control as SpinButton).Update();
            }
#endif
        }

        public void AppendText(int idx, string line)
        {
            var scroll = properties[idx].control as ScrolledWindow;
            var txt = scroll.Child as TextView;
            txt.Buffer.Insert(txt.Buffer.EndIter, line + "\n");
        }

        public void BeginAdvancedProperties()
        {
            advancedPropertyStart = properties.Count;
        }

        private WarningImage CreateImage(Pixbuf pixbuf)
        {
            return new WarningImage(pixbuf);
        }

        public void SetPropertyWarning(int idx, CommentType type, string comment)
        {
            var prop = properties[idx];

            if (prop.warningIcon == null)
                prop.warningIcon = CreateImage(warningIcons[(int)type]);
            else
                prop.warningIcon.Pixbuf = warningIcons[(int)type];

            prop.warningIcon.Visible = !string.IsNullOrEmpty(comment);
            prop.warningIcon.TooltipText = comment;
        }

        public object GetPropertyValue(int idx)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.String:
                case PropertyType.ColoredString:
                    return (prop.control as Entry).Text;
                case PropertyType.NumericUpDown:
                    return (int)(prop.control as SpinButton).Value;
                case PropertyType.DomainUpDown:
                    return int.Parse((prop.control as DomainSpinButton).Text);
                case PropertyType.Slider:
                    return (prop.control as HScale).Value;
                case PropertyType.Radio:
                    return (prop.control as RadioButton).Active;
                case PropertyType.CheckBox:
                    return (prop.control as CheckButton).Active;
                case PropertyType.ColorPicker:
                    return color;
                case PropertyType.DropDownList:
                    return (prop.control as ComboBox).ActiveText;
                case PropertyType.CheckBoxList:
                    return (prop.control as CheckBoxList).GetSelected();
                case PropertyType.Button:
                    return (prop.control as Button).Label;
            }

            return null;
        }

        public int GetSelectedIndex(int idx)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.DropDownList:
                    return (prop.control as ComboBox).Active;
            }

            return -1;
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
                case PropertyType.CheckBox:
                    (prop.control as CheckButton).Active = (bool)value;
                    break;
                case PropertyType.Button:
                    (prop.control as Button).Label = (string)value;
                    break;
                case PropertyType.ProgressBar:
                    (prop.control as ProgressBar).Fraction = (float)value;
                    break;
                case PropertyType.Slider:
                    (prop.control as HScale).Value = (double)value;
                    break;
            }
        }

        public void Build(bool advanced = false)
        {
            var propertyCount = advanced || advancedPropertyStart < 0 ? properties.Count : advancedPropertyStart;

            Resize((uint)propertyCount, showWarnings ? 3u : 2u);

            ColumnSpacing = (uint)GtkUtils.ScaleGtkWidget(5);
            RowSpacing    = (uint)GtkUtils.ScaleGtkWidget(5);

            for (int i = 0; i < propertyCount; i++)
            {
                var prop = properties[i];

                if (prop.label != null)
                {
                    Attach(prop.label,   0, 1, (uint)i, (uint)(i + 1), AttachOptions.Fill, AttachOptions.Expand | AttachOptions.Fill, 0, 0);
                    Attach(prop.control, 1, 2, (uint)i, (uint)(i + 1));

                    prop.label.Show();
                    prop.control.Show();
                }
                else
                {
                    var attachOption = AttachOptions.Expand | AttachOptions.Fill;

                    // HACK: Cant be bothered to deal with GTK+2 aspect ratios.
                    if (prop.control is ColorSelector img)
                        img.DesiredWidth = Toplevel.WidthRequest - GtkUtils.ScaleGtkWidget(10); // (10 = Border * 2)
                    else if (prop.control is Label lbl && lbl.Wrap)
                        attachOption = AttachOptions.Shrink | AttachOptions.Fill;

                    Attach(prop.control, 0, NColumns, (uint)i, (uint)(i + 1), attachOption, AttachOptions.Expand | AttachOptions.Fill, (uint)prop.leftMargin, 0);
                    prop.control.Show();
                }

                if (prop.warningIcon != null)
                {
                    Attach(prop.warningIcon, 2, 3, (uint)i, (uint)(i + 1), AttachOptions.Shrink, AttachOptions.Expand | AttachOptions.Fill, 0, 0);
                }
            }

            for (int i = propertyCount; i < properties.Count; i++)
            {
                var prop = properties[i];

                if (prop.label != null)
                    Remove(prop.label);
                Remove(prop.control);
                if (prop.warningIcon != null)
                    Remove(prop.warningIcon);
            }
        }
    }
}
