using System;
using System.Diagnostics;
using System.Collections.Generic;
using Android.App;
using Android.Util;
using Android.Content.Res;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Text;
using Android.Widget;
using AndroidX.Core.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.CoordinatorLayout.Widget;
using Android.Views.InputMethods;
using Google.Android.Material.Button;
using Java.Util;

using Orientation = Android.Widget.Orientation;
using AndroidX.Core.Graphics;
using System.Globalization;

namespace FamiStudio
{
    public partial class PropertyPage : AndroidX.Fragment.App.Fragment, View.IOnTouchListener, EditText.IOnEditorActionListener
    {
        public int PropertyCount => properties.Count;

        private delegate void OnTouchDelegate(Property prop);

        class Property
        {
            public PropertyType type;
            public LinearLayout layout;
            public TextView label;
            public TextView value;
            public TextView tooltip;
            public List<View> controls = new List<View>();
            public ColumnDesc[] columns;
            public string sliderFormat;
            public double sliderMin;
            public bool visible = true;
        };

        private int firstAdvancedProperty = -1;
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

        public void SetScrolling(int dummy)
        {
        }

        private string SanitizeLabel(string label)
        {
            return label != null ? label.TrimEnd(new[] { ' ', ':' }) : null;
        }

        private EditText CreateEditText(string txt, int maxLength)
        {
            var editText = new EditText(new ContextThemeWrapper(context, Resource.Style.LightGrayTextMedium));

            editText.InputType = InputTypes.ClassText;
            editText.Text = txt;
            editText.SetTextColor(Application.Context.GetColorStateList(Resource.Color.light_grey));
            editText.Background.SetColorFilter(BlendModeColorFilterCompat.CreateBlendModeColorFilterCompat(DroidUtils.GetColorFromResources(context, Resource.Color.LightGreyColor1), BlendModeCompat.SrcAtop));
            editText.SetMaxLines(1);
            editText.SetOnEditorActionListener(this);
            editText.AfterTextChanged += EditText_AfterTextChanged;

            if (maxLength > 0)
                editText.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(maxLength) } );

            return editText;
        }

        private void EditText_AfterTextChanged(object sender, AfterTextChangedEventArgs e)
        {
            var editText = sender as EditText;

            var idx = GetPropertyIndexForView(editText);
            if (idx >= 0)
                PropertyChanged?.Invoke(this, idx, -1, -1, editText.Text);
        }

        private SwitchCompat CreateSwitch(bool value)
        {
            var toggle = new SwitchCompat(context);
            toggle.LayoutParameters = CreateLinearLayoutParams(DroidUtils.DpToPixels(64), ViewGroup.LayoutParams.WrapContent, GravityFlags.Right | GravityFlags.CenterVertical);
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

        private SeekBar CreateSeekBar(double value, double min, double max, double increment, int numDecimals, int pad = 20, string tooltip = null)
        {
            var seek = new SeekBar(new ContextThemeWrapper(context, Resource.Style.LightGraySeekBar));
            var padding = DroidUtils.DpToPixels(pad);

            seek.SetPadding(padding, padding, padding, padding);
            seek.Min = (int)(min * 1000);
            seek.Max = (int)(max * 1000);
            seek.Progress = (int)(value * 1000);
            seek.ProgressChanged += Seek_ProgressChanged;

            return seek;
        }

        private void Seek_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            var idx = GetPropertyIndexForView(e.SeekBar);
            if (idx >= 0)
            {
                var prop = properties[idx];
                var newValue = prop.sliderMin + e.Progress / 1000.0;
                if (prop.value != null)
                    prop.value.Text = string.Format(prop.sliderFormat, newValue);
                PropertyChanged?.Invoke(this, idx, -1, -1, newValue);
            }
        }

        private Spinner CreateSpinner(string[] values, string value, string tooltip = null)
        {
            var spinner = new Spinner(new ContextThemeWrapper(context, Resource.Style.LightGrayTextMedium));
            var adapter = new CustomFontArrayAdapter(spinner, context, Android.Resource.Layout.SimpleSpinnerItem, values);
            spinner.Adapter = adapter;
            spinner.Background.SetColorFilter(BlendModeColorFilterCompat.CreateBlendModeColorFilterCompat(DroidUtils.GetColorFromResources(context, Resource.Color.LightGreyColor1), BlendModeCompat.SrcAtop));
            spinner.SetSelection(adapter.GetPosition(value));
            spinner.ItemSelected += Spinner_ItemSelected;
            return spinner;
        }

        private void Spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var spinner = sender as Spinner;
            var idx = GetPropertyIndexForView(spinner);
            if (idx >= 0)
            {
                var item = (spinner.Adapter as ArrayAdapter).GetItem(e.Position);
                PropertyChanged?.Invoke(this, idx, -1, -1, item.ToString());
            }
        }

        private HorizontalNumberPicker CreateNumberPicker(int value, int min, int max, int inc)
        {
            var picker = new HorizontalNumberPicker(context, value, min, max, inc);
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

        public int AddColoredTextBox(string value, Color color)
        {
            return AddTextBox("Name", value);
        }

        private LinearLayout CreateLinearLayout(bool vertical, bool matchParentWidth, bool matchParentHeight, int margin, bool radioGroup = false, float weight = 0.0f)
        {
            var layout = new LinearLayout.LayoutParams(
                    matchParentWidth  ? ViewGroup.LayoutParams.MatchParent : ViewGroup.LayoutParams.WrapContent,
                    matchParentHeight ? ViewGroup.LayoutParams.MatchParent : ViewGroup.LayoutParams.WrapContent, weight);
            margin = DroidUtils.DpToPixels(margin);
            layout.SetMargins(margin, margin, margin, margin);

            var lin = radioGroup ? new RadioGroup(context) : new LinearLayout(context);
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

        private ProgressBar CreateProgressBar(float value)
        {
            var progress = new ProgressBar(context, null, Android.Resource.Attribute.ProgressBarStyleHorizontal);
            progress.Min = 0;
            progress.Max = 1000;
            progress.Progress = (int)Math.Round(value * 1000);
            return progress;
        }

        private RadioButton CreateRadioButton(string str, int style)
        {
            var radio = new RadioButton(new ContextThemeWrapper(context, style));
            radio.Text = str;
            return radio;
        }

        private View CreateSpacer()
        {
            var spacer = new View(context);
            spacer.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 1);
            spacer.SetBackgroundColor(DroidUtils.GetColorFromResources(context, Resource.Color.LightGreyColor1)); 
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
            prop.label = CreateTextView(SanitizeLabel(label), Resource.Style.LightGrayTextMedium);
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

        public int AddFileTextBox(string label, string value, int maxLength = 0, string tooltip = null)
        {
            return AddTextBox(label, value, maxLength, tooltip);
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
            prop.label = CreateTextView(SanitizeLabel(label), Resource.Style.LightGrayTextMedium);
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
            var prop = new Property();
            var textView = CreateTextView(value, Resource.Style.LightGrayTextMedium);

            prop.type = PropertyType.Label;
            prop.label = CreateTextView(SanitizeLabel(label), Resource.Style.LightGrayTextMedium);
            prop.controls.Add(textView);
            prop.tooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;
            prop.layout = CreateLinearLayout(true, true, false, 10);
            prop.layout.SetOnTouchListener(this);
            prop.layout.AddView(prop.label);
            if (prop.tooltip != null)
                prop.layout.AddView(prop.tooltip);
            prop.layout.AddView(textView);
            properties.Add(prop);

            return properties.Count - 1;
        }

        public int AddLinkLabel(string label, string value, string url, string tooltip = null)
        {
            return 0;
        }

        private ColorPickerView CreateColorPicker(Color color)
        {
            var picker = new ColorPickerView(context);
            picker.SelectedColor = color;
            return picker;
        }

        public int AddColorPicker(Color color)
        {
            var prop = new Property();
            var picker = CreateColorPicker(color);

            prop.type = PropertyType.ColorPicker;
            prop.label = CreateTextView("Color", Resource.Style.LightGrayTextMedium);
            prop.controls.Add(picker);
            prop.layout = CreateLinearLayout(true, true, false, 10);
            prop.layout.AddView(prop.label);
            prop.layout.AddView(picker);

            properties.Add(prop);

            return 0;
        }

        public int AddNumericUpDown(string label, int value, int min, int max, int inc, string tooltip = null)
        {
            var prop = new Property();
            var picker = CreateNumberPicker(value, min, max, inc);

            prop.type = PropertyType.NumericUpDown;
            prop.controls.Add(picker);
            prop.label = CreateTextView(SanitizeLabel(label), Resource.Style.LightGrayTextMedium);
            prop.tooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;
            prop.layout = CreateLinearLayout(true, true, false, 10);
            prop.layout.AddView(prop.label);
            if (prop.tooltip != null)
                prop.layout.AddView(prop.tooltip);
            prop.layout.AddView(picker);
            properties.Add(prop);

            return properties.Count - 1;
        }

        public int AddProgressBar(string label, float value, string tooltip = null)
        {
            var prop = new Property();
            var picker = CreateProgressBar(value);

            prop.type = PropertyType.ProgressBar;
            prop.controls.Add(picker);
            prop.label = CreateTextView(SanitizeLabel(label), Resource.Style.LightGrayTextMedium);
            prop.tooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;
            prop.layout = CreateLinearLayout(true, true, false, 10);
            prop.layout.AddView(prop.label);
            if (prop.tooltip != null)
                prop.layout.AddView(prop.tooltip);
            prop.layout.AddView(picker);
            properties.Add(prop);

            return properties.Count - 1;
        }

        public int AddRadioButton(string label, string text, bool check, bool multiline = false)
        {
            var first = properties.Count == 0 || properties[properties.Count - 1].type != PropertyType.Radio;
            var prop = new Property();

            // Create the group on the first radio.
            var group = (RadioGroup)null;

            if (first)
            {
                group = CreateLinearLayout(true, true, false, 10, true) as RadioGroup;

                prop.label = CreateTextView(SanitizeLabel(label), Resource.Style.LightGrayTextMedium);
                prop.layout = group;
                prop.layout.AddView(prop.label);
                if (prop.tooltip != null)
                    prop.layout.AddView(prop.tooltip);
            }
            else
            {
                for (int i = properties.Count - 1; i >= 0; i--)
                {
                    if (properties[i].type == PropertyType.Radio)
                    {
                        group = properties[i].layout as RadioGroup;
                        if (group != null)
                            break;
                    }
                }
            }

            // Create the radio, but add ourselves to the previous layout.
            var radio = CreateRadioButton(text, Resource.Style.LightGrayCheckBox);
            radio.CheckedChange += Radio_CheckedChange;
            prop.type = PropertyType.Radio;
            prop.controls.Add(radio);
            group.AddView(radio);
            properties.Add(prop);

            if (check)
                radio.Checked = true;

            return properties.Count - 1;
        }

        private void RadioGroup_CheckedChange(object sender, RadioGroup.CheckedChangeEventArgs e)
        {
            var radio = sender as RadioGroup;
            var idx = GetPropertyIndexForView(radio);
            if (idx >= 0)
                PropertyChanged?.Invoke(this, idx, -1, -1, e.CheckedId);
        }

        private void Radio_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            var radio = sender as RadioButton;
            var idx = GetPropertyIndexForView(radio);
            if (idx >= 0)
                PropertyChanged?.Invoke(this, idx, -1, -1, e.IsChecked);
        }

        public void UpdateIntegerRange(int idx, int min, int max)
        {
            var picker = properties[idx].controls[0] as HorizontalNumberPicker;
            picker.Minimum = min;
            picker.Maximum = max;
        }

        public void UpdateIntegerRange(int idx, int value, int min, int max)
        {
            var picker = properties[idx].controls[0] as HorizontalNumberPicker;
            picker.Minimum = min;
            picker.Maximum = max;
            picker.Value = value;
        }

        public void UpdateDomainRange(int idx, int[] values, int value)
        {
        }

        public void SetLabelText(int idx, string text)
        {
            var textView = properties[idx].controls[0] as TextView;
            textView.Text = text;
        }

        public void SetDropDownListIndex(int idx, int selIdx)
        {
            var spinner = properties[idx].controls[0] as Spinner;
            spinner.SetSelection(selIdx);
        }

        public void UpdateDropDownListItems(int idx, string[] values)
        {
            var spinner = properties[idx].controls[0] as Spinner;
            var selIdx  = spinner.SelectedItemPosition;
            var adapter = new CustomFontArrayAdapter(spinner, context, Android.Resource.Layout.SimpleSpinnerItem, values);

            spinner.Adapter = adapter;
            
            if (selIdx >= 0 && selIdx < values.Length)
                spinner.SetSelection(selIdx);
            else
                spinner.SetSelection(0);
        }

        public void SetPropertyVisible(int idx, bool visible)
        {
            Debug.Assert(pageLayout == null);

            var prop = properties[idx];

            prop.visible = visible;

            // Properties must be shows/hidden before calling Build(). We dont need this.
            //if (prop.label   != null) prop.label.Visibility   = visible ? ViewStates.Visible : ViewStates.Gone;
            //if (prop.layout  != null) prop.layout.Visibility  = visible ? ViewStates.Visible : ViewStates.Gone;
            //if (prop.tooltip != null) prop.tooltip.Visibility = visible ? ViewStates.Visible : ViewStates.Gone;

            //foreach (var ctrl in prop.controls)
            //    ctrl.Visibility = visible ? ViewStates.Visible : ViewStates.Gone;
        }

        public int AddCheckBox(string label, bool value, string tooltip = null)
        {
            var prop = new Property();
            var sw = CreateSwitch(value);

            prop.type = PropertyType.CheckBox;
            prop.label = CreateTextView(SanitizeLabel(label), Resource.Style.LightGrayTextMedium);
            prop.tooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;
            prop.controls.Add(sw);
            prop.layout = CreateLinearLayout(false, true, false, 10);
            properties.Add(prop);

            var inner = CreateLinearLayout(true, false, false, 0, false, 1.0f);

            inner.AddView(prop.label);
            if (prop.tooltip != null)
                inner.AddView(prop.tooltip);

            prop.layout.AddView(inner);
            prop.layout.AddView(sw);

            return properties.Count - 1;
        }

        public int AddLabelCheckBox(string label, bool value, int margin = 0, string tooltip = null)
        {
            return AddCheckBox(label, value, tooltip);
        }

        public int AddDropDownList(string label, string[] values, string value, string tooltip = null)
        {
            var prop = new Property();
            var spinner = CreateSpinner(values, value, tooltip);

            prop.type = PropertyType.DropDownList;
            prop.label = CreateTextView(SanitizeLabel(label), Resource.Style.LightGrayTextMedium);
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

        public int AddCheckBoxList(string label, string[] values, bool[] selected, string tooltip = null, int numRows = 7)
        {
            var prop = new Property();
            prop.type = PropertyType.CheckBoxList;
            prop.label = CreateTextView(SanitizeLabel(label), Resource.Style.LightGrayTextMedium);
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

        public int AddRadioButtonList(string label, string[] values, int selectedIndex, string tooltip = null, int numRows = 7)
        {
            var prop = new Property();
            prop.type = PropertyType.RadioList;
            prop.label = CreateTextView(SanitizeLabel(label), Resource.Style.LightGrayTextMedium);
            prop.tooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;

            var radioGroup = CreateLinearLayout(true, true, false, 10, true) as RadioGroup;
            radioGroup.CheckedChange += RadioGroup_CheckedChange;

            prop.layout = radioGroup;
            prop.layout.AddView(prop.label);
            if (prop.tooltip != null)
                prop.layout.AddView(prop.tooltip);

            for (int i = 0; i < values.Length; i++)
            {
                var radio = CreateRadioButton(values[i], Resource.Style.LightGrayCheckBox);
                radio.Checked = i == selectedIndex;
                radio.Id = i;
                prop.controls.Add(radio);
                prop.layout.AddView(radio);
            }

            properties.Add(prop);

            return properties.Count - 1;
        }

        public void UpdateRadioButtonList(int idx, string[] values, int selectedIndex)
        {
            var prop = properties[idx];
            var radioGroup = prop.layout as RadioGroup;

            radioGroup.RemoveAllViews();

            for (int i = 0; i < values.Length; i++)
            {
                var radio = CreateRadioButton(values[i], Resource.Style.LightGrayCheckBox);
                radio.Checked = i == selectedIndex;
                radio.Id = i;
                prop.controls.Add(radio);
                prop.layout.AddView(radio);
            }
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
            // On older androids, the seekbar misbehave when using negative values. 
            // Make everything relative to zero and remap.
            var prop = new Property();
            var seekBar = CreateSeekBar(value - min, 0, max - min, 0, 0);

            prop.type = PropertyType.Slider;
            prop.label = CreateTextView(SanitizeLabel(label), Resource.Style.LightGrayTextMedium);
            prop.tooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;
            if (format != null)
                prop.value = CreateTextView(string.Format(format, value), Resource.Style.LightGrayTextSmall);
            prop.layout = CreateLinearLayout(true, true, false, 10);
            prop.controls.Add(seekBar);
            prop.sliderFormat = format;
            prop.sliderMin = min;

            var inner = CreateLinearLayout(false, true, false, 0);

            seekBar.LayoutParameters = CreateLinearLayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent,GravityFlags.CenterVertical, 1.0f);
            inner.AddView(seekBar);

            if (prop.value != null)
            {
                prop.value.LayoutParameters = CreateLinearLayoutParams(DroidUtils.DpToPixels(72), ViewGroup.LayoutParams.WrapContent, GravityFlags.CenterVertical);
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
            var prop = properties[propIdx];
            Debug.Assert(prop.type == PropertyType.Grid);
            for (int i = 0; i < prop.controls.Count; i++)
            {
                var r = i / prop.columns.Length;
                var c = i % prop.columns.Length;

                if (c == colIdx)
                    prop.controls[i].Enabled = enabled;
            }
        }

        public void SetRowColor(int propIdx, int rowIdx, Color color)
        {
        }

        public void OverrideCellSlider(int propIdx, int rowIdx, int colIdx, int min, int max, Func<object, string> fmt)
        {
            var prop = properties[propIdx];
            Debug.Assert(prop.type == PropertyType.Grid);
            Debug.Assert(prop.columns[colIdx].Type == ColumnType.Slider);

            var ctrlIdx = rowIdx * prop.columns.Length + colIdx;
            //var seekBar = prop.controls[ctrlIdx] as SeekBar;
            var seekBar = prop.controls[ctrlIdx] as SeekBarWithLabelView;

            seekBar.Min = min;
            seekBar.Max = max;
            seekBar.Format = fmt;
        }

        public int AddGrid(string label, ColumnDesc[] columnDescs, object[,] data, int rows = 7, string tooltip = null, GridOptions options = GridOptions.None)
        {
            // We need initial data on mobile.
            if (data != null)
            {
                var prop = new Property();
                prop.type = PropertyType.Grid;
                prop.label = CreateTextView(SanitizeLabel(label), Resource.Style.LightGrayTextMedium);
                prop.tooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;
                prop.layout = CreateLinearLayout(true, true, false, 10);
                prop.layout.AddView(prop.label);
                prop.columns = columnDescs;

                if (prop.tooltip != null)
                    prop.layout.AddView(prop.tooltip);

                var gridLayout = new GridLayout(context);
                gridLayout.ColumnCount = data.GetLength(1);
                gridLayout.RowCount = data.GetLength(0);
                gridLayout.LayoutParameters = CreateLinearLayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);

                prop.layout.AddView(gridLayout);

                var maxWidth = columnDescs[0].Width;
                var maxColIdx = 0;
                for (int c = 1; c < columnDescs.Length; c++)
                {
                    if (columnDescs[c].Width > maxWidth)
                    {
                        maxWidth = columnDescs[c].Width;
                        maxColIdx = c;
                    }
                }

                for (int r = 0; r < data.GetLength(0); r++)
                {
                    for (int c = 0; c < columnDescs.Length; c++)
                    {
                        var view = (View)null;
                        var col = columnDescs[c];

                        switch (col.Type)
                        {
                            case ColumnType.CheckBox:
                                var checkBox = new CheckBox(new ContextThemeWrapper(context, Resource.Style.LightGrayCheckBox));
                                checkBox.Checked = data == null || (bool)data[r, c];
                                view = checkBox;
                                break;
                            case ColumnType.Label:
                                var text = new TextView(new ContextThemeWrapper(context, Resource.Style.LightGrayTextMedium));
                                text.Text = data == null ? "" : (string)data[r, c];
                                view = text;
                                break;
                            case ColumnType.Slider:
                                //var seek = new SeekBar(new ContextThemeWrapper(context, Resource.Style.LightGraySeekBar));
                                var seek = new SeekBarWithLabelView(context, col.MinValue, col.MaxValue, data == null ? 50 : (int)data[r, c]);
                                seek.Min = col.MinValue;
                                seek.Max = col.MaxValue;
                                seek.Progress = data == null ? 50 : (int)data[r, c];
                                view = seek;
                                break;
                            default:
                                Debug.Assert(false);
                                view = new View(context);
                                break;
                        }

                        var margin = DroidUtils.DpToPixels(2);
                        var gridLayoutParams = new GridLayout.LayoutParams(
                                GridLayout.InvokeSpec(r, 1),
                                GridLayout.InvokeSpec(c, 1, c == maxColIdx ? 1.0f : 0.0f));
                        gridLayoutParams.SetGravity(GravityFlags.CenterVertical);
                        gridLayoutParams.SetMargins(margin, margin, margin, margin);
                        gridLayout.AddView(view, gridLayoutParams);

                        prop.controls.Add(view);
                    }
                }

                properties.Add(prop);
            }
            else
            {
                properties.Add(new Property());
            }

            return properties.Count - 1;
        }

        public void UpdateGrid(int idx, object[,] data, string[] columnNames = null)
        {
            var prop = properties[idx];
            Debug.Assert(prop.type == PropertyType.Grid);
            Debug.Assert(prop.controls.Count == data.Length); // We dont support resizing on mobile atm.
            Debug.Assert(columnNames == null); // We dont even have columns on Mobile yet.

            for (int r = 0; r < data.GetLength(0); r++)
            {
                for (int c = 0; c < prop.columns.Length; c++)
                {
                    UpdateGrid(idx, r, c, data[r, c]);
                }
            }
        }

        public void UpdateGrid(int idx, int rowIdx, int colIdx, object value)
        {
            var prop = properties[idx];
            Debug.Assert(prop.type == PropertyType.Grid);
            var ctrlIdx = rowIdx * prop.columns.Length + colIdx;
            var view = prop.controls[ctrlIdx];
            
            switch (prop.columns[colIdx].Type)
            {
                case ColumnType.CheckBox:
                    (view as CheckBox).Checked = (bool)value;
                    break;
                case ColumnType.Label:
                    (view as TextView).Text = (string)value;
                    break;
                case ColumnType.Slider:
                    (view as SeekBar).Progress = (int)value;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }

        public void SetPropertyEnabled(int idx, bool enabled)
        {
            // MATTT : Make sure grid disables correctly!

            var prop = properties[idx];

            if (prop.label != null)
                prop.label.Enabled = enabled;
            if (prop.tooltip != null)
                prop.tooltip.Enabled = enabled;

            foreach (var ctrl in prop.controls)
                ctrl.Enabled = enabled;
        }

        public void BeginAdvancedProperties()
        {
            firstAdvancedProperty = properties.Count;
        }

        public void SetPropertyWarning(int idx, CommentType type, string comment)
        {
        }

        public void ClearRadioList(int idx)
        {
            var prop = properties[idx];
            Debug.Assert(prop.type == PropertyType.RadioList);

            var group = prop.layout as RadioGroup;
            if (group != null)
                group.ClearCheck();
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
                    return (prop.controls[0] as EditText).Text;
                case PropertyType.NumericUpDown:
                    return (prop.controls[0] as HorizontalNumberPicker).Value;
                case PropertyType.Slider:
                {
                    var seekbar = prop.controls[0] as SeekBar;
                    return seekbar.Progress / 1000.0 + prop.sliderMin;
                }
                case PropertyType.Radio:
                    return (prop.controls[0] as RadioButton).Checked;
                case PropertyType.CheckBox:
                    return (prop.controls[0] as SwitchCompat).Checked;
                case PropertyType.ColorPicker:
                    return (prop.controls[0] as ColorPickerView).SelectedColor;
                case PropertyType.DropDownList:
                {
                    var spinner = prop.controls[0] as Spinner;
                    var adapter = spinner.Adapter as ArrayAdapter;
                    return spinner.SelectedItemPosition >= 0 ? adapter.GetItem(spinner.SelectedItemPosition).ToString() : "";
                }
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
            var prop = properties[idx];
            Debug.Assert(prop.type == PropertyType.Grid);
            var ctrlIdx = rowIdx * prop.columns.Length + colIdx;
            var view = prop.controls[ctrlIdx];

            if (view is CheckBox)
            {
                return (T)(object)(view as CheckBox).Checked;
            }
            else if (view is TextView)
            {
                return (T)(object)(view as TextView).Text;
            }
            else if (view is SeekBar)
            {
                return (T)(object)(view as SeekBar).Progress;
            }
            else
            {
                Debug.Assert(false);
                return default(T);
            }
        }

        public int GetSelectedIndex(int idx)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.DropDownList:
                    return (prop.controls[0] as Spinner).SelectedItemPosition;
                case PropertyType.RadioList:
                    return (prop.layout as RadioGroup).CheckedRadioButtonId;
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
                case PropertyType.TextBox:
                case PropertyType.FileTextBox:
                case PropertyType.LogTextBox:
                    (prop.controls[0] as EditText).Text = (string)value;
                    break;
                case PropertyType.ProgressBar:
                    (prop.controls[0] as ProgressBar).Progress = (int)Math.Round((float)value * 1000);
                    break;
                case PropertyType.Slider:
                    (prop.controls[0] as SeekBar).Progress = (int)(((double)value - prop.sliderMin) * 1000.0);
                    break;
                case PropertyType.Label:
                    (prop.controls[0] as TextView).Text = (string)value;
                    break;
                case PropertyType.NumericUpDown:
                    (prop.controls[0] as HorizontalNumberPicker).Value = (int)value;
                    break;
            }
        }

        public void SetPropertyValue(int idx, int rowIdx, int colIdx, object value)
        {
            var prop = properties[idx];

            switch (prop.type)
            {
                case PropertyType.Grid:
                    UpdateGrid(idx, rowIdx, colIdx, value);
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
            pageLayout.SetBackgroundColor(DroidUtils.ToAndroidColor(Theme.DarkGreyColor4));

            var first = true;

            for (int i = 0; i < properties.Count; i++)
            {
                var prop = properties[i];

                if (prop.layout == null || prop.visible == false)
                    continue;

                if (!first)
                    pageLayout.AddView(CreateSpacer());
                if (i == firstAdvancedProperty)
                    pageLayout.AddView(CreateAdvancedPropertiesBanner());

                pageLayout.AddView(prop.layout);
                first = false;
            }

            return pageLayout;
        }

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
            SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
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
                tv.SetBackgroundResource(Resource.Color.LightGreyColor1);
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

        private Timer timer;
        private FastScrollTask timerTask;

        private int value = 50;
        private int minimum = 0;
        private int maximum = 100;
        private int increment = 1;
        private int fastScrollDir = 0;

        public HorizontalNumberPicker(Context context, int val, int min, int max, int inc) : base(context)
        {
            var lp = new LinearLayout.LayoutParams(DroidUtils.DpToPixels(64), DroidUtils.DpToPixels(48));
            lp.Weight = 1;
            lp.Gravity = GravityFlags.CenterHorizontal | GravityFlags.CenterVertical;

            buttonLess = new MaterialButton(context);
            buttonLess.Text = "-";
            buttonLess.SetTextColor(Android.Graphics.Color.Black);
            buttonLess.BackgroundTintList = ColorStateList.ValueOf(DroidUtils.GetColorFromResources(context, Resource.Color.LightGreyColor1));
            buttonLess.LayoutParameters = lp;

            buttonMore = new MaterialButton(context);
            buttonMore.Text = "+";
            buttonMore.SetTextColor(Android.Graphics.Color.Black);
            buttonMore.BackgroundTintList = ColorStateList.ValueOf(DroidUtils.GetColorFromResources(context, Resource.Color.LightGreyColor1));
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
            increment = inc;
            value = val;

            UpdateValue(false);
        }

        // There has to be a better place to put this...
        protected override void DrawableStateChanged()
        {
            base.DrawableStateChanged();

            buttonLess.Enabled = Enabled;
            textView.Enabled = Enabled;
            buttonMore.Enabled = Enabled;
        }

        private void StartFastScroll(int dir)
        {
            // Can be needed if we press both buttons at same time.
            CancelFastScroll();

            fastScrollDir = dir;
            timerTask = new FastScrollTask(this);
            timer = new Timer();
            timer.ScheduleAtFixedRate(timerTask, 500, 50);
        }

        private void CancelFastScroll()
        {
            fastScrollDir = 0;
            if (timer != null)
            {
                timer.Cancel();
                timer = null;
            }
            if (timerTask != null)
            {
                timerTask = null;
            }
        }

        private void DoFastScroll()
        {
            if (fastScrollDir == 0)
                return;

            value += fastScrollDir;
            UpdateValue();
        }

        private void UpdateValue(MotionEventActions action, int dir)
        {
            if (action == MotionEventActions.Down)
            {
                value += dir;
                UpdateValue();
                StartFastScroll(dir);
            }
            else if (action == MotionEventActions.Up || 
                     action == MotionEventActions.Cancel)
            {
                CancelFastScroll();
            }
        }

        private void ButtonLess_Touch(object sender, TouchEventArgs e)
        {
            UpdateValue(e.Event.Action, -increment);
        }

        private void ButtonMore_Touch(object sender, TouchEventArgs e)
        {
            UpdateValue(e.Event.Action, increment);
        }

        private void UpdateValue(bool invokeEvent = true)
        {
            value = Utils.Clamp(Utils.RoundDown(value, increment), minimum, maximum);
            if (invokeEvent)
                ValueChanged?.Invoke(this, value);
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

        private class FastScrollTask : TimerTask
        {
            HorizontalNumberPicker picker;
            
            public FastScrollTask(HorizontalNumberPicker p)
            {
                picker = p;
            }

            public override void Run()
            {
                (picker.Context as Android.App.Activity).RunOnUiThread(() => { picker.DoFastScroll(); });
            }
        }
    }

    public class SeekBarWithLabelView : View
    {
        private readonly int LightWidth = 3;

        private Paint borderPaint;
        private Paint bgPaint;
        private Paint barPaint;
        private Paint textPaint;

        private bool changing = false;
        private float touchDownX;
        private float touchSlop;

        private int min = 0;
        private int max = 100;
        private int progress = 0;
        private Func<object, string> format;

        public int Min { get => min; set => min = value; }
        public int Max { get => max; set => max = value; }
        public int Progress { get => progress; set => progress = value; }
        public Func<object, string> Format { get => format; set => format = value; }

        public SeekBarWithLabelView(Context context, int min, int max, int progress, Func<object, string> format = null) : base(context)
        {
            Initialize(context);
            this.min = min;
            this.max = max;
            this.progress = progress;
            this.Format = format == null ? (o) => Convert.ToString(o, CultureInfo.InvariantCulture) : format;
        }

        public SeekBarWithLabelView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialize(context, attrs);
        }

        protected SeekBarWithLabelView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        private void Initialize(Context context, IAttributeSet attrs = null)
        {
            borderPaint = new Paint();
            borderPaint.SetStyle(Paint.Style.Stroke);
            borderPaint.StrokeWidth = LightWidth;
            borderPaint.Color = Android.Graphics.Color.Black;

            textPaint = new Paint();
            textPaint.Color = new Android.Graphics.Color(Theme.LightGreyColor1.ToArgb());
            textPaint.TextSize = DroidUtils.DpToPixels(14);
            textPaint.TextAlign = Paint.Align.Center;

            bgPaint = new Paint();
            bgPaint.SetStyle(Paint.Style.Fill);
            bgPaint.Color = new Android.Graphics.Color(Theme.DarkGreyColor1.ToArgb());

            barPaint = new Paint();
            barPaint.SetStyle(Paint.Style.Fill);
            barPaint.Color = new Android.Graphics.Color(Theme.DarkGreyColor6.ToArgb());

            touchSlop = ViewConfiguration.Get(context).ScaledTouchSlop;
            Touch += SeekBarWithLabelView_Touch;
        }

        private void SeekBarWithLabelView_Touch(object sender, TouchEventArgs e)
        {
            if (e.Event.Action == MotionEventActions.Down)
            {
                touchDownX = e.Event.GetX();
            }
            else if (e.Event.Action == MotionEventActions.Up)
            {
                UpdateProgress(e);
                changing = false;
            }
            else if (e.Event.Action == MotionEventActions.Move)
            {
                if (changing)
                {
                    UpdateProgress(e);
                }
                else if (Math.Abs(touchDownX - e.Event.GetX()) > touchSlop)
                {
                    changing = true;
                    UpdateProgress(e);
                }
            }
            else if (e.Event.Action == MotionEventActions.Cancel)
            {
                changing = false;
            }

            Debug.WriteLine($"{e.Event.Action} {e.Event.GetX()}");
        }

        private void UpdateProgress(TouchEventArgs e)
        {
            var ratio = e.Event.GetX() / (float)MeasuredWidth;
            progress = Utils.Clamp((int)Math.Round(ratio * (max - min) + min), min, max);
            Invalidate();
        }

        protected override void OnDraw(Canvas canvas)
        {
            canvas.DrawRect(0, 0, canvas.Width - 1, canvas.Height - 1, bgPaint);
            canvas.DrawRect(0, 0, (progress - min) / (float)(max - min) * canvas.Width, canvas.Height, barPaint);
            canvas.DrawText(format(progress), (canvas.Width - 1) * 0.5f, (canvas.Height - (textPaint.Descent() + textPaint.Ascent())) * 0.5f, textPaint);
            canvas.DrawRect(0, 0, canvas.Width - 1, canvas.Height - 1, borderPaint);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            int dw = 0;
            int dh = DroidUtils.DpToPixels(24);

            SetMeasuredDimension(
                    View.ResolveSizeAndState(dw, widthMeasureSpec,  0),
                    View.ResolveSizeAndState(dh, heightMeasureSpec, 0));
        }
    }

    public class ColorPickerView : View
    {
        Paint borderPaint;
        Paint[,] fillPaints;

        int selectedColorX;
        int selectedColorY;

        const int   BorderWidth = 5;
        const float MaxHeightScreen = 0.4f;

        public Color SelectedColor
        {
            get
            {
                return Theme.CustomColors[selectedColorX, selectedColorY];
            }
            set
            {
                var idx = Theme.GetCustomColorIndex(value);
                var len = Theme.CustomColors.GetLength(0);
                selectedColorX = idx % len;
                selectedColorY = idx / len;
                Invalidate();
            } 
        }

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

            fillPaints = new Paint[Theme.CustomColors.GetLength(0), Theme.CustomColors.GetLength(1)];

            for (int i = 0; i < fillPaints.GetLength(0); i++)
            {
                for (int j = 0; j < fillPaints.GetLength(1); j++)
                {
                    var fillPaint = new Paint();
                    fillPaint.SetStyle(Paint.Style.Fill);
                    fillPaint.Color = new Android.Graphics.Color(Theme.CustomColors[i, j].ToArgb());
                    fillPaints[i, j] = fillPaint;
                }
            }
        }

        private void SelectColorAt(float x, float y)
        {
            var buttonSizeX = Width  / (float)Theme.CustomColors.GetLength(0);
            var buttonSizeY = Height / (float)Theme.CustomColors.GetLength(1);

            var newSelectedColorX = Utils.Clamp((int)(x / buttonSizeX), 0, Theme.CustomColors.GetLength(0) - 1);
            var newSelectedColorY = Utils.Clamp((int)(y / buttonSizeY), 0, Theme.CustomColors.GetLength(1) - 1);

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
            var width  = MeasureSpec.GetSize(widthMeasureSpec);
            var height = MeasureSpec.GetSize(heightMeasureSpec);

            var ratio = Theme.CustomColors.GetLength(1) / (float)Theme.CustomColors.GetLength(0);
            height = (int)(width * ratio);

            // Dont allow it to be more than 50% of the height of the screen. This prevent
            // it from being HUGE in landscale mode.
            var percentHeight = height / (float)Context.Resources.DisplayMetrics.HeightPixels;
            if (percentHeight > MaxHeightScreen)
            {
                height = (int)(Context.Resources.DisplayMetrics.HeightPixels * MaxHeightScreen);
                width  = (int)(height / ratio);
            }

            SetMeasuredDimension(width, height);
        }
    }
}
