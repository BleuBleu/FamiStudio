using System;
using System.Collections.Generic;
using Android.Util;
using Android.Content.Res;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Google.Android.Material.Button;

using Debug = System.Diagnostics.Debug;
using Color = System.Drawing.Color;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using Orientation = Android.Widget.Orientation;
using Android.Views.InputMethods;
using Android.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public partial class PropertyPage : AndroidX.Fragment.App.Fragment, View.IOnTouchListener, EditText.IOnEditorActionListener
    {
        public int PropertyCount => 0;

        private delegate void OnTouchDelegate(Property prop);

        class Property
        {
            public PropertyType type;
            public LinearLayout layout;
            public TextView label;
            public TextView value;
            public TextView tooltip;
            public List<View> controls = new List<View>();
            public ImageView warning;
            public View view;
            public OnTouchDelegate onTouch;
            public string sliderFormat;
            public bool visible = true;
        };

        private int firstAdvancedProperty = -1;
        private string lastLabel;
        private Context context;
        private List<Property> properties = new List<Property>();
        private LinearLayout pageLayout;

        public PropertyPage()
        {
        }

        public PropertyPage(Android.Content.Context ctx)
        {
            context = ctx;
        }

        private EditText CreateEditText(string txt, int maxLength)
        {
            var editText = new EditText(new ContextThemeWrapper(context, Resource.Style.LightGrayTextMedium));

            editText.InputType = Android.Text.InputTypes.ClassText;
            editText.Text = txt;
            editText.SetTextColor(DroidUtils.GetColorFromResources(context, Resource.Color.LightGreyFillColor2));
            editText.Background.SetColorFilter(new BlendModeColorFilter(DroidUtils.GetColorFromResources(context, Resource.Color.LightGreyFillColor2), BlendMode.SrcAtop));
            editText.SetMaxLines(1);
            editText.SetOnEditorActionListener(this);
            editText.AfterTextChanged += EditText_AfterTextChanged;

            if (maxLength > 0)
                editText.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(maxLength) } );

            return editText;
        }

        private void EditText_AfterTextChanged(object sender, AfterTextChangedEventArgs e)
        {
            // MATTT : Force ASCII here!
            var editText = sender as EditText;
            var idx = GetPropertyIndexForView(editText);
            if (idx >= 0)
                PropertyChanged?.Invoke(this, idx, -1, -1, editText.Text);
        }

        private SwitchCompat CreateSwitch(bool value)
        {
            var layout = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            layout.Gravity = GravityFlags.Right | GravityFlags.CenterVertical;

            var toggle = new SwitchCompat(context);
            toggle.LayoutParameters = layout;
            toggle.Checked = value;
            toggle.CheckedChange += Toggle_CheckedChange;

            return toggle;
        }

        private void Toggle_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            var idx = GetPropertyIndexForView(sender as View);
            if (idx >= 0)
                PropertyChanged?.Invoke(this, idx, -1, -1, e.IsChecked);
        }

        private SeekBar CreateSeekBar(double value, double min, double max, double increment, int numDecimals, bool showLabel, string tooltip = null)
        {
            var seek = new SeekBar(new ContextThemeWrapper(context, Resource.Style.LightGraySeekBar));
            var padding = DroidUtils.DpToPixels(20);

            seek.SetPadding(padding, padding, padding, padding);
            seek.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            seek.Min = (int)min;
            seek.Max = (int)max;
            seek.Progress = (int)value;
            seek.ProgressChanged += Seek_ProgressChanged;

            return seek;
        }

        private void Seek_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            var idx = GetPropertyIndexForView(e.SeekBar);
            if (idx >= 0)
            {
                var prop = properties[idx];
                if (prop.value != null)
                    prop.value.Text = string.Format(prop.sliderFormat, e.Progress);
                PropertyChanged?.Invoke(this, idx, -1, -1, (double)e.Progress);
            }
        }

        private Spinner CreateSpinner(string[] values, string value, string tooltip = null)
        {
            var spinner = new Spinner(new ContextThemeWrapper(context, Resource.Style.LightGrayTextMedium));
            var adapter = new CustomFontArrayAdapter(spinner, context, Android.Resource.Layout.SimpleSpinnerItem, values);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinner.Adapter = adapter;
            spinner.Background.SetColorFilter(new BlendModeColorFilter(DroidUtils.GetColorFromResources(context, Resource.Color.LightGreyFillColor2), BlendMode.SrcAtop));
            spinner.ItemSelected += Spinner_ItemSelected;
            return spinner;
        }

        private void Spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var idx = GetPropertyIndexForView(sender as View);
            if (idx >= 0)
                PropertyChanged?.Invoke(this, idx, -1, -1, e.Position); // MATTT : This is wrong, send text of item here.
        }

        private HorizontalNumberPicker CreateNumberPicker(int value, int min, int max)
        {
            var picker = new HorizontalNumberPicker(context, value, min, max);
            picker.ValueChanged += Picker_ValueChanged;
            return picker;
        }

        private void Picker_ValueChanged(object sender, int val)
        {
            var idx = GetPropertyIndexForView(sender as View);
            if (idx >= 0)
                PropertyChanged?.Invoke(this, idx, -1, -1, val);
        }

        public void UpdateCheckBoxList(int idx, bool[] selected)
        {
            var prop = properties[idx];
            Debug.Assert(prop.controls.Count == selected.Length);
            for (int i = 0; i < selected.Length; i++)
                (prop.controls[i] as CheckBox).Checked = selected[i];
        }

        public int AddColoredTextBox(string value, System.Drawing.Color color)
        {
            return AddTextBox("Color", value);
        }

        private LinearLayout CreateLinearLayout(bool vertical, bool matchParentWidth, bool matchParentHeight, int margin)
        {
            var layout = new LinearLayout.LayoutParams(
                    matchParentWidth  ? ViewGroup.LayoutParams.MatchParent : ViewGroup.LayoutParams.WrapContent,
                    matchParentHeight ? ViewGroup.LayoutParams.MatchParent : ViewGroup.LayoutParams.WrapContent);
            margin = DroidUtils.DpToPixels(margin);
            layout.SetMargins(margin, margin, margin, margin);

            var lin = new LinearLayout(context);
            lin.LayoutParameters = layout;
            lin.Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal;
            return lin;
        }

        private TextView CreateTextView(string str, int style)
        {
            var text = new TextView(new ContextThemeWrapper(context, style));
            text.Text = str;
            return text;
        }

        private View CreateSpacer()
        {
            var spacer = new View(context);
            spacer.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 1);
            spacer.SetBackgroundColor(Android.Graphics.Color.LightGray); // MATTT color.
            return spacer;
        }

        private int GetPropertyIndexForView(View v)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                if (properties[i].layout == v ||
                    properties[i].controls.Contains(v))
                {
                    return i;
                }
            }

            return -1;
        }

        private int GetPropertyIndexForView(View v, out int controlIdx)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                if (properties[i].layout == v ||
                    properties[i].controls.Contains(v))
                {
                    controlIdx = properties[i].controls.IndexOf(v);
                    return i;
                }
            }

            controlIdx = -1;
            return -1;
        }

        private Property GetPropertyForView(View v)
        {
            if (v == null)
                return null;

            var idx = GetPropertyIndexForView(v);
            return idx >= 0 ? properties[idx] : null;
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            /*
            if (e.Action == MotionEventActions.Down && e.PointerCount == 1)
            {
                var prop = GetPropertyForView(v);
                if (prop != null && prop.onTouch != null)
                    prop.onTouch(prop);
            }
            return true;
            */
            return false;
        }

        public int AddTextBox(string label, string value, int maxLength = 0, string tooltip = null)
        {
            var prop = new Property();
            var editText = CreateEditText(value, maxLength);

            prop.type = PropertyType.TextBox;
            prop.label = CreateTextView(label, Resource.Style.LightGrayTextMedium);
            prop.controls.Add(editText);
            prop.tooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;
            prop.layout = CreateLinearLayout(true, true, false, 10);
            prop.layout.SetOnTouchListener(this);
            prop.layout.AddView(prop.label);
            if (prop.tooltip != null)
                prop.layout.AddView(prop.tooltip);
            prop.layout.AddView(editText);

            properties.Add(prop);

            return properties.Count - 1;
        }

        public bool OnEditorAction(TextView v, [GeneratedEnum] ImeAction actionId, KeyEvent e)
        {
            if (actionId == ImeAction.Done)
            {
                if (v is EditText textView)
                    textView.ClearFocus();
            }

            return false;
        }

        private MaterialButton CreateButton(string value)
        {
            var button = new MaterialButton(context);
            button.Text = value;
            button.Click += Button_Click;
            return button;
        }

        private void Button_Click(object sender, EventArgs e)
        {
            var idx = GetPropertyIndexForView(sender as MaterialButton);
            if (idx >= 0)
                PropertyClicked?.Invoke(this, ClickType.Button, idx, -1, -1);
        }

        public int AddButton(string label, string value, string tooltip = null)
        {
            var prop = new Property();
            var button = CreateButton(value);

            prop.type = PropertyType.Button;
            prop.label = CreateTextView(label, Resource.Style.LightGrayTextMedium);
            prop.tooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;
            prop.controls.Add(button);
            prop.layout = CreateLinearLayout(true, true, false, 10);
            prop.layout.AddView(prop.label);
            if (prop.tooltip != null)
                prop.layout.AddView(prop.tooltip);
            prop.layout.AddView(button);
            properties.Add(prop);

            return properties.Count - 1;
        }

        public int AddLabel(string label, string value, bool multiline = false, string tooltip = null)
        {
            Debug.Assert(false); // TODO : Store the label and apply it to the next control.
            return 0;
        }

        public int AddLinkLabel(string label, string value, string url, string tooltip = null)
        {
            Debug.Assert(false); // TODO : Store the label and apply it to the next control.
            return 0;
        }

        private ColorPickerView CreateColorPicker()
        {
            var picker = new ColorPickerView(context);
            return picker;
        }

        public int AddColorPicker(System.Drawing.Color color)
        {
            var prop = new Property();
            var picker = CreateColorPicker();

            prop.type = PropertyType.ColorPicker;
            prop.label = CreateTextView("Color", Resource.Style.LightGrayTextMedium);
            prop.controls.Add(picker);
            prop.layout = CreateLinearLayout(true, true, false, 10);
            prop.layout.AddView(prop.label);
            prop.layout.AddView(picker);

            properties.Add(prop);

            return 0;
        }

        public int AddNumericUpDown(string label, int value, int min, int max, string tooltip = null)
        {
            var prop = new Property();
            var picker = CreateNumberPicker(value, min, max);

            prop.type = PropertyType.NumericUpDown;
            prop.controls.Add(picker);
            prop.label = CreateTextView(label, Resource.Style.LightGrayTextMedium);
            prop.tooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;
            prop.layout = CreateLinearLayout(true, true, false, 10);
            prop.layout.AddView(prop.label);
            if (prop.tooltip != null)
                prop.layout.AddView(prop.tooltip);
            prop.layout.AddView(picker);
            properties.Add(prop);

            return properties.Count - 1;
        }

        public int AddProgressBar(string label, float value)
        {
            return 0;
        }

        public int AddRadioButton(string label, string text, bool check)
        {
            return 0;
        }

        public void UpdateIntegerRange(int idx, int min, int max)
        {
        }

        public void UpdateIntegerRange(int idx, int value, int min, int max)
        {
        }

        public void AddDomainRange(string label, int[] values, int value)
        {
        }

        public void UpdateDomainRange(int idx, int[] values, int value)
        {
        }

        public void SetLabelText(int idx, string text)
        {
        }

        public void SetDropDownListIndex(int idx, int selIdx)
        {
        }

        public void UpdateDropDownListItems(int idx, string[] values)
        {
        }

        public void SetPropertyVisible(int idx, bool visible)
        {
            Debug.Assert(pageLayout == null);
            properties[idx].visible = visible;
        }

        public int AddCheckBox(string label, bool value, string tooltip = null)
        {
            var prop = new Property();
            var sw = CreateSwitch(value);

            prop.type = PropertyType.CheckBox;
            prop.label = CreateTextView(label, Resource.Style.LightGrayTextMedium);
            prop.tooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;
            prop.controls.Add(sw);
            prop.layout = CreateLinearLayout(false, true, false, 10);
            properties.Add(prop);

            var inner = CreateLinearLayout(true, false, false, 0);

            prop.layout.AddView(inner);
            inner.AddView(prop.label);
            if (prop.tooltip != null)
                inner.AddView(prop.tooltip);
            prop.layout.AddView(sw);

            return properties.Count - 1;
        }

        public int AddLabelCheckBox(string label, bool value, int margin = 0)
        {
            return 0;
        }

        public int AddDropDownList(string label, string[] values, string value, string tooltip = null)
        {
            var prop = new Property();
            var spinner = CreateSpinner(values, value, tooltip); ;

            prop.type = PropertyType.DropDownList;
            prop.label = CreateTextView(label, Resource.Style.LightGrayTextMedium);
            prop.tooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;
            prop.controls.Add(spinner);
            prop.layout = CreateLinearLayout(true, true, false, 10);
            prop.layout.AddView(prop.label);
            if (prop.tooltip != null)
                prop.layout.AddView(prop.tooltip);
            prop.layout.AddView(spinner);
            properties.Add(prop);

            return properties.Count - 1;
        }

        private CheckBox CreateCheckBox(string text, bool chk)
        {
            var checkBox = new CheckBox(new ContextThemeWrapper(context, Resource.Style.LightGrayCheckBox));
            checkBox.Text = text;
            checkBox.Checked = chk;
            checkBox.CheckedChange += CheckBox_CheckedChange;
            return checkBox;
        }

        private void CheckBox_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            var idx = GetPropertyIndexForView(sender as View, out var controlIdx);
            if (idx >= 0 && controlIdx >= 0)
                PropertyChanged?.Invoke(this, idx, controlIdx, 0, e.IsChecked);
        }

        public int AddCheckBoxList(string label, string[] values, bool[] selected, string tooltip = null, int height = 200)
        {
            var prop = new Property();
            prop.type = PropertyType.CheckBoxList;
            prop.label = CreateTextView(label, Resource.Style.LightGrayTextMedium);
            prop.tooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;
            prop.layout = CreateLinearLayout(true, true, false, 10);
            prop.layout.AddView(prop.label);
            if (prop.tooltip != null)
                prop.layout.AddView(prop.tooltip);

            for (int i = 0; i < values.Length; i++)
            {
                var checkBox = CreateCheckBox(values[i], selected == null ? true : selected[i]);
                prop.controls.Add(checkBox);
                prop.layout.AddView(checkBox);
            }

            properties.Add(prop);

            return properties.Count - 1;
        }

        private LinearLayout.LayoutParams CreateLinearLayoutParams(int width, int height, GravityFlags gravity = GravityFlags.NoGravity, float weight = -1.0f)
        {
            var layout = new LinearLayout.LayoutParams(width, height);

            if (gravity != GravityFlags.NoGravity)
                layout.Gravity = gravity;
            if (weight >= 0.0)
                layout.Weight = weight;

            return layout;
        }

        public int AddSlider(string label, double value, double min, double max, double increment, int numDecimals, string format = "{0}", string tooltip = null)
        {
            var prop = new Property();
            var seekBar = CreateSeekBar(value, min, max, 0, 0, false);

            prop.type = PropertyType.Slider;
            prop.label = CreateTextView(label, Resource.Style.LightGrayTextMedium);
            prop.tooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;
            if (format != null)
                prop.value = CreateTextView(string.Format(format, value), Resource.Style.LightGrayTextSmall);
            prop.layout = CreateLinearLayout(true, true, false, 10);
            prop.controls.Add(seekBar);
            prop.sliderFormat = format;

            var inner = CreateLinearLayout(false, true, false, 0);

            seekBar.LayoutParameters = CreateLinearLayoutParams(0, ViewGroup.LayoutParams.WrapContent, GravityFlags.Left | GravityFlags.CenterVertical, 1.0f); ;
            inner.AddView(seekBar);

            if (prop.value != null)
            {
                prop.value.LayoutParameters = CreateLinearLayoutParams(DroidUtils.DpToPixels(72), ViewGroup.LayoutParams.WrapContent, GravityFlags.Right | GravityFlags.CenterVertical);
                inner.AddView(prop.value);
            }

            prop.layout.AddView(prop.label);
            if (prop.tooltip != null)
                prop.layout.AddView(prop.tooltip);
            prop.layout.AddView(inner);

            properties.Add(prop);

            return properties.Count - 1;
        }

        public void SetColumnEnabled(int propIdx, int colIdx, bool enabled)
        {
            Debug.Assert(false); // Will not be supported initially.
        }

        public void AddMultiColumnList(ColumnDesc[] columnDescs, object[,] data, int height = 300)
        {
            Debug.Assert(false); // Will not be supported initially.
        }

        public void UpdateMultiColumnList(int idx, object[,] data, string[] columnNames = null)
        {
            Debug.Assert(false); // Will not be supported initially.
        }

        public void UpdateMultiColumnList(int idx, int rowIdx, int colIdx, object value)
        {
            Debug.Assert(false); // Will not be supported initially.
        }

        public void SetPropertyEnabled(int idx, bool enabled)
        {
            Debug.Assert(false);
        }

        public void BeginAdvancedProperties()
        {
            firstAdvancedProperty = properties.Count;
        }

        public void SetPropertyWarning(int idx, CommentType type, string comment)
        {
        }

        public object GetPropertyValue(int idx)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.TextBox:
                case PropertyType.ColoredTextBox:
                case PropertyType.MultilineTextBox:
                    //ForceTextBoxASCII(prop.control as TextBox); MATTT
                    return (prop.controls[0] as EditText).Text;
                case PropertyType.NumericUpDown:
                    return (prop.controls[0] as HorizontalNumberPicker).Value;
                case PropertyType.Slider:
                    return (double)(prop.controls[0] as SeekBar).Progress;
                case PropertyType.Radio:
                    Debug.Assert(false); // MATTT
                    //return (prop.control as RadioButton).Checked;
                    break;
                case PropertyType.CheckBox:
                    return (prop.controls[0] as SwitchCompat).Checked;
                case PropertyType.ColorPicker:
                    return (prop.controls[0] as ColorPickerView).SelectedColor;
                case PropertyType.DropDownList:
                    return (prop.controls[0] as Spinner).SelectedItemPosition;
                case PropertyType.CheckBoxList:
                    {
                        var selected = new bool[prop.controls.Count];
                        for (int i = 0; i < prop.controls.Count; i++)
                            selected[i] = (prop.controls[i] as CheckBox).Checked;
                        return selected;
                    }
                case PropertyType.Button:
                    return (prop.controls[0] as MaterialButton).Text;
            }

            return null;
        }

        public T GetPropertyValue<T>(int idx)
        {
            return (T)GetPropertyValue(idx);
        }

        public T GetPropertyValue<T>(int idx, int rowIdx, int colIdx)
        {
            Debug.Assert(false);
            return default(T);
        }

        public int GetSelectedIndex(int idx)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.DropDownList:
                    return (prop.controls[0] as Spinner).SelectedItemPosition;
            }

            return -1;
        }

        public void SetPropertyValue(int idx, object value)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.CheckBox:
                    (prop.controls[0] as SwitchCompat).Checked = (bool)value;
                    break;
                case PropertyType.Button:
                    (prop.controls[0] as MaterialButton).Text = (string)value;
                    break;
                case PropertyType.MultilineTextBox:
                    (prop.controls[0] as EditText).Text = (string)value;
                    break;
                case PropertyType.ProgressBar:
                    //(prop.controls[0] as SeekBar).Progress = (int)Math.Round((float)value * 1000);
                    Debug.Assert(false);
                    break;
                case PropertyType.Slider:
                    (prop.controls[0] as SeekBar).Progress = (int)(double)value;
                    break;
            }
        }

        public void Build(bool advanced = false)
        {
        }

        private LinearLayout CreateAdvancedPropertiesBanner()
        {
            var text = CreateTextView("Advanced Properties", Resource.Style.LightGrayTextMedium);
            var tooltip = CreateTextView("These properties are meant for more advanced users", Resource.Style.LightGrayTextSmallTooltip);

            var outer = CreateLinearLayout(false, true, false, 10);
            var inner = CreateLinearLayout(true, false, false, 0);

            outer.AddView(inner);
            inner.AddView(text);
            inner.AddView(tooltip);

            return outer;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (pageLayout != null)
                return pageLayout;

            context = container.Context;

            var margin = DroidUtils.DpToPixels(2);
            var coordLayoutParameters = new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            coordLayoutParameters.SetMargins(margin, margin, margin, margin);

            pageLayout = new LinearLayout(container.Context);
            pageLayout.LayoutParameters = coordLayoutParameters;
            pageLayout.Orientation = Orientation.Vertical;

            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];

                if (i != 0)
                    pageLayout.AddView(CreateSpacer());
                if (i == firstAdvancedProperty)
                    pageLayout.AddView(CreateAdvancedPropertiesBanner());

                pageLayout.AddView(prop.layout);
            }

            return pageLayout;
        }

        public static Color[,] CustomColors = new Color[17, 6]
        {
        {
            Color.FromArgb(unchecked((int)0xffe57373)),
            Color.FromArgb(unchecked((int)0xffef5350)),
            Color.FromArgb(unchecked((int)0xfff44336)),
            Color.FromArgb(unchecked((int)0xffe53935)),
            Color.FromArgb(unchecked((int)0xffd32f2f)),
            Color.FromArgb(unchecked((int)0xffc62828)),
        },
        {
            Color.FromArgb(unchecked((int)0xfff06292)),
            Color.FromArgb(unchecked((int)0xffec407a)),
            Color.FromArgb(unchecked((int)0xffe91e63)),
            Color.FromArgb(unchecked((int)0xffd81b60)),
            Color.FromArgb(unchecked((int)0xffc2185b)),
            Color.FromArgb(unchecked((int)0xffad1457)),
        },
        {
            Color.FromArgb(unchecked((int)0xffba68c8)),
            Color.FromArgb(unchecked((int)0xffab47bc)),
            Color.FromArgb(unchecked((int)0xff9c27b0)),
            Color.FromArgb(unchecked((int)0xff8e24aa)),
            Color.FromArgb(unchecked((int)0xff7b1fa2)),
            Color.FromArgb(unchecked((int)0xff6a1b9a)),
        },
        {
            Color.FromArgb(unchecked((int)0xff9575cd)),
            Color.FromArgb(unchecked((int)0xff7e57c2)),
            Color.FromArgb(unchecked((int)0xff673ab7)),
            Color.FromArgb(unchecked((int)0xff5e35b1)),
            Color.FromArgb(unchecked((int)0xff512da8)),
            Color.FromArgb(unchecked((int)0xff4527a0)),
        },
        {
            Color.FromArgb(unchecked((int)0xff7986cb)),
            Color.FromArgb(unchecked((int)0xff5c6bc0)),
            Color.FromArgb(unchecked((int)0xff3f51b5)),
            Color.FromArgb(unchecked((int)0xff3949ab)),
            Color.FromArgb(unchecked((int)0xff303f9f)),
            Color.FromArgb(unchecked((int)0xff283593)),
        },
        {
            Color.FromArgb(unchecked((int)0xff64b5f6)),
            Color.FromArgb(unchecked((int)0xff42a5f5)),
            Color.FromArgb(unchecked((int)0xff2196f3)),
            Color.FromArgb(unchecked((int)0xff1e88e5)),
            Color.FromArgb(unchecked((int)0xff1976d2)),
            Color.FromArgb(unchecked((int)0xff1565c0)),
        },
        {
            Color.FromArgb(unchecked((int)0xff4fc3f7)),
            Color.FromArgb(unchecked((int)0xff29b6f6)),
            Color.FromArgb(unchecked((int)0xff03a9f4)),
            Color.FromArgb(unchecked((int)0xff039be5)),
            Color.FromArgb(unchecked((int)0xff0288d1)),
            Color.FromArgb(unchecked((int)0xff0277bd)),
        },
        {
            Color.FromArgb(unchecked((int)0xff4dd0e1)),
            Color.FromArgb(unchecked((int)0xff26c6da)),
            Color.FromArgb(unchecked((int)0xff00bcd4)),
            Color.FromArgb(unchecked((int)0xff00acc1)),
            Color.FromArgb(unchecked((int)0xff0097a7)),
            Color.FromArgb(unchecked((int)0xff00838f)),
        },
        {
            Color.FromArgb(unchecked((int)0xff4db6ac)),
            Color.FromArgb(unchecked((int)0xff26a69a)),
            Color.FromArgb(unchecked((int)0xff009688)),
            Color.FromArgb(unchecked((int)0xff00897b)),
            Color.FromArgb(unchecked((int)0xff00796b)),
            Color.FromArgb(unchecked((int)0xff00695c)),
        },
        {
            Color.FromArgb(unchecked((int)0xff81c784)),
            Color.FromArgb(unchecked((int)0xff66bb6a)),
            Color.FromArgb(unchecked((int)0xff4caf50)),
            Color.FromArgb(unchecked((int)0xff43a047)),
            Color.FromArgb(unchecked((int)0xff388e3c)),
            Color.FromArgb(unchecked((int)0xff2e7d32)),
        },
        {
            Color.FromArgb(unchecked((int)0xffaed581)),
            Color.FromArgb(unchecked((int)0xff9ccc65)),
            Color.FromArgb(unchecked((int)0xff8bc34a)),
            Color.FromArgb(unchecked((int)0xff7cb342)),
            Color.FromArgb(unchecked((int)0xff689f38)),
            Color.FromArgb(unchecked((int)0xff558b2f)),
        },
        {
            Color.FromArgb(unchecked((int)0xffdce775)),
            Color.FromArgb(unchecked((int)0xffd4e157)),
            Color.FromArgb(unchecked((int)0xffcddc39)),
            Color.FromArgb(unchecked((int)0xffc0ca33)),
            Color.FromArgb(unchecked((int)0xffafb42b)),
            Color.FromArgb(unchecked((int)0xff9e9d24)),
        },
        {
            Color.FromArgb(unchecked((int)0xfffff176)),
            Color.FromArgb(unchecked((int)0xffffee58)),
            Color.FromArgb(unchecked((int)0xffffeb3b)),
            Color.FromArgb(unchecked((int)0xfffdd835)),
            Color.FromArgb(unchecked((int)0xfffbc02d)),
            Color.FromArgb(unchecked((int)0xfff9a825)),
        },
        {
            Color.FromArgb(unchecked((int)0xffffd54f)),
            Color.FromArgb(unchecked((int)0xffffca28)),
            Color.FromArgb(unchecked((int)0xffffc107)),
            Color.FromArgb(unchecked((int)0xffffb300)),
            Color.FromArgb(unchecked((int)0xffffa000)),
            Color.FromArgb(unchecked((int)0xffff8f00)),
        },
        {
            Color.FromArgb(unchecked((int)0xffffb74d)),
            Color.FromArgb(unchecked((int)0xffffa726)),
            Color.FromArgb(unchecked((int)0xffff9800)),
            Color.FromArgb(unchecked((int)0xfffb8c00)),
            Color.FromArgb(unchecked((int)0xfff57c00)),
            Color.FromArgb(unchecked((int)0xffef6c00)),
        },
        {
            Color.FromArgb(unchecked((int)0xffff8a65)),
            Color.FromArgb(unchecked((int)0xffff7043)),
            Color.FromArgb(unchecked((int)0xffff5722)),
            Color.FromArgb(unchecked((int)0xfff4511e)),
            Color.FromArgb(unchecked((int)0xffe64a19)),
            Color.FromArgb(unchecked((int)0xffd84315)),
        },
        {
            Color.FromArgb(unchecked((int)0xffa1887f)),
            Color.FromArgb(unchecked((int)0xff8d6e63)),
            Color.FromArgb(unchecked((int)0xff795548)),
            Color.FromArgb(unchecked((int)0xff6d4c41)),
            Color.FromArgb(unchecked((int)0xff5d4037)),
            Color.FromArgb(unchecked((int)0xff4e342e)),
        }
        };

        private class PropertyTag : Java.Lang.Object
        {
            public Property prop;
            public PropertyTag(Property p)
            {
                prop = p;
            }
        };
    }

    public class CustomFontArrayAdapter : ArrayAdapter
    {
        Spinner spinner;

        public CustomFontArrayAdapter(Spinner spin, Context context, int textViewResourceId, string[] values) : base(context, textViewResourceId, values)
        {
            spinner = spin;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var baseView = base.GetView(position, convertView, parent);

            if (baseView is TextView tv)
                TextViewCompat.SetTextAppearance(tv, Resource.Style.LightGrayTextMedium);

            return baseView;

        }

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
            var baseView = base.GetDropDownView(position, convertView, parent);

            if (baseView is TextView tv)
            {
                var style = position == spinner.SelectedItemPosition ? Resource.Style.SpinnerItemSelected : Resource.Style.SpinnerItem;
                TextViewCompat.SetTextAppearance(tv, style);
                tv.SetBackgroundResource(Resource.Color.LightGreyFillColor2);
            }

            return baseView;
        }
    }

    // Loosely based off https://stackoverflow.com/questions/6796243/is-it-possible-to-make-a-horizontal-numberpicker
    public class HorizontalNumberPicker : LinearLayout
    {
        public delegate void ValueChangedDelegate(object sender, int val);
        public event ValueChangedDelegate ValueChanged;

        private MaterialButton buttonLess;
        private MaterialButton buttonMore;
        private TextView textView;

        private int value = 50;
        private int minimum = 0;
        private int maximum = 100;
        private int fastScrollDir = 0;

        public HorizontalNumberPicker(Context context, int val, int min, int max) : base(context)
        {
            var lp = new LinearLayout.LayoutParams(DroidUtils.DpToPixels(64), DroidUtils.DpToPixels(48));
            lp.Weight = 1;
            lp.Gravity = GravityFlags.CenterHorizontal | GravityFlags.CenterVertical;

            buttonLess = new MaterialButton(context);
            buttonLess.Text = "-";
            buttonLess.SetTextColor(Android.Graphics.Color.Black);
            buttonLess.BackgroundTintList = ColorStateList.ValueOf(DroidUtils.GetColorFromResources(context, Resource.Color.LightGreyFillColor2));
            buttonLess.LayoutParameters = lp;
            
            buttonMore = new MaterialButton(context);
            buttonMore.Text = "+";
            buttonMore.SetTextColor(Android.Graphics.Color.Black);
            buttonMore.BackgroundTintList = ColorStateList.ValueOf(DroidUtils.GetColorFromResources(context, Resource.Color.LightGreyFillColor2));
            buttonMore.LayoutParameters = lp;
            
            textView = new TextView(new ContextThemeWrapper(context, Resource.Style.LightGrayTextMedium));
            textView.Text = "";
            textView.LayoutParameters = lp;
            textView.Gravity = GravityFlags.Center;

            AddView(buttonLess);
            AddView(textView);
            AddView(buttonMore);

            buttonLess.Touch += ButtonLess_Touch;
            buttonMore.Touch += ButtonMore_Touch;

            minimum = min;
            maximum = max;
            value = val;

            UpdateValue(false);
        }

        private async void UpdateFastScroll(int dir)
        {
            fastScrollDir = dir;
            await Task.Delay(500);
            if (fastScrollDir == 0) return;

            do
            {
                value += fastScrollDir;
                UpdateValue();
                await Task.Delay(50);
            }
            while (fastScrollDir != 0);
        }

        private void UpdateValue(MotionEventActions action, int dir)
        {
            if (action == MotionEventActions.Down)
            {
                value += dir;
                UpdateValue();
                UpdateFastScroll(dir);
            }
            else if (action == MotionEventActions.Up || 
                     action == MotionEventActions.Cancel)
            {
                fastScrollDir = 0;
            }
        }

        private void ButtonLess_Touch(object sender, TouchEventArgs e)
        {
            UpdateValue(e.Event.Action, -1);
        }

        private void ButtonMore_Touch(object sender, TouchEventArgs e)
        {
            UpdateValue(e.Event.Action, 1);
        }

        private void UpdateValue(bool invokeEvent = true)
        {
            var newValue = Utils.Clamp(value, minimum, maximum);
            if (value != newValue)
            {
                value = newValue;
                if (invokeEvent)
                    ValueChanged?.Invoke(this, value);
            }
            textView.Text = value.ToString();
        }

        public int Minimum
        {
            get { return minimum; }
            set { minimum = value; UpdateValue(); }
        }

        public int Maximum
        {
            get { return maximum; }
            set { maximum = value; UpdateValue(); }
        }

        public int Value
        {
            get { return value; }
            set { this.value = value; UpdateValue(); }
        }
    }

    public class ColorPickerView : View
    {
        Paint borderPaint;
        Paint[,] fillPaints;

        // MATTT : Pass real selected color here.
        int selectedColorX = 3;
        int selectedColorY = 3;

        const int BorderWidth = 5;
        const float MaxHeightScreen = 0.4f;

        public System.Drawing.Color SelectedColor => PropertyPage.CustomColors[selectedColorX, selectedColorY];

        public ColorPickerView(Context context) : base(context)
        {
            Initialize(context);
        }

        public ColorPickerView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialize(context, attrs);
        }

        protected ColorPickerView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        private void Initialize(Context context, IAttributeSet attrs = null)
        {
            borderPaint = new Paint();
            borderPaint.SetStyle(Paint.Style.Stroke);
            borderPaint.StrokeWidth = BorderWidth;
            borderPaint.Color = Android.Graphics.Color.Black;

            fillPaints = new Paint[PropertyPage.CustomColors.GetLength(0), PropertyPage.CustomColors.GetLength(1)];

            for (int i = 0; i < fillPaints.GetLength(0); i++)
            {
                for (int j = 0; j < fillPaints.GetLength(1); j++)
                {
                    var fillPaint = new Paint();
                    fillPaint.SetStyle(Paint.Style.Fill);
                    fillPaint.Color = new Android.Graphics.Color(PropertyPage.CustomColors[i, j].ToArgb());
                    fillPaints[i, j] = fillPaint;
                }
            }
        }

        private void SelectColorAt(float x, float y)
        {
            var buttonSizeX = Width / (float)PropertyPage.CustomColors.GetLength(0);
            var buttonSizeY = Height / (float)PropertyPage.CustomColors.GetLength(1);

            var newSelectedColorX = (int)(x / buttonSizeX);
            var newSelectedColorY = (int)(y / buttonSizeY);

            // MATTT : Clamp here!!!

            if (newSelectedColorX != selectedColorX ||
                newSelectedColorY != selectedColorY)
            {
                selectedColorX = newSelectedColorX;
                selectedColorY = newSelectedColorY;
                Invalidate();
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (e.Action == MotionEventActions.Down)
            {
                // Needed to prevent the scrolling view to scroll.
                Parent.RequestDisallowInterceptTouchEvent(true);
                SelectColorAt(e.GetX(), e.GetY());
                return true;
            }
            else if (e.Action == MotionEventActions.Move)
            {
                SelectColorAt(e.GetX(), e.GetY());
                return true;
            }
            else if (e.Action == MotionEventActions.Up)
            {
                Parent.RequestDisallowInterceptTouchEvent(false);
                return true;
            }

            return false;
        }

        protected override void OnDraw(Canvas canvas)
        {
            var buttonSizeX = canvas.Width / (float)fillPaints.GetLength(0);
            var buttonSizeY = canvas.Height / (float)fillPaints.GetLength(1);

            // Colored squares.
            for (int i = 0; i < fillPaints.GetLength(0); i++)
            {
                for (int j = 0; j < fillPaints.GetLength(1); j++)
                {
                    var rf = new RectF(
                        (i + 0) * buttonSizeX,
                        (j + 0) * buttonSizeY,
                        (i + 1) * buttonSizeX,
                        (j + 1) * buttonSizeY);

                    canvas.DrawRect(rf, fillPaints[i, j]);
                }
            }

            // Selected color.
            var rb = new RectF(
                        (selectedColorX + 0) * buttonSizeX + BorderWidth / 2,
                        (selectedColorY + 0) * buttonSizeY + BorderWidth / 2,
                        (selectedColorX + 1) * buttonSizeX - BorderWidth / 2,
                        (selectedColorY + 1) * buttonSizeY - BorderWidth / 2);

            canvas.DrawRect(rb, borderPaint);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            var modeWidth = MeasureSpec.GetMode(widthMeasureSpec);
            var modeHeight = MeasureSpec.GetMode(heightMeasureSpec);

            var width = MeasureSpec.GetSize(widthMeasureSpec);
            var height = MeasureSpec.GetSize(heightMeasureSpec);

            //if (modeHeight != MeasureSpecMode.Exactly)
            {
                var ratio = PropertyPage.CustomColors.GetLength(1) / (float)PropertyPage.CustomColors.GetLength(0);
                height = (int)(width * ratio);

                // Dont allow it to be more than 50% of the height of the screen. This prevent
                // it from being HUGE in landscale mode.
                //if (modeWidth != MeasureSpecMode.Exactly)
                {
                    var percentHeight = height / (float)Context.Resources.DisplayMetrics.HeightPixels;
                    if (percentHeight > MaxHeightScreen)
                    {
                        height = (int)(Context.Resources.DisplayMetrics.HeightPixels * MaxHeightScreen);
                        width = (int)(height / ratio);
                    }
                }
            }

            SetMeasuredDimension(width, height);
        }
    }
}
