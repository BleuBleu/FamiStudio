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

namespace FamiStudio
{
    public partial class PropertyPage : AndroidX.Fragment.App.Fragment, View.IOnTouchListener
    {
        public int PropertyCount => 0;

        private delegate void OnTouchDelegate(Property prop);

        class Property
        {
            public PropertyType type;
            public LinearLayout layout;
            public TextView textLabel;
            public TextView textTooltip;
            public TextView textValue;
            public View control;
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

        //private TextView CreateTextView(string str, string tooltip = null, bool multiline = false)
        //{
        //    var text = new TextView(context);
        //    text.Text = str;
        //    //text.SetTextSize(Android.Util.ComplexUnitType.Sp, 14); 
        //    text.Typeface = quicksand;
        //    return text;
        //}

        private EditText CreateEditText(string txt, int maxLength, string tooltip = null)
        {
            var edit = new EditText(context);
            edit.Text = txt; // MATTT maxLength
            //edit.SetTextSize(Android.Util.ComplexUnitType.Sp, 14);
            //edit.Typeface = quicksand;
            return edit;
        }

        private SwitchCompat CreateSwitch(bool value)
        {
            var layout = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            layout.Gravity = GravityFlags.Right | GravityFlags.CenterVertical;

            var toggle = new SwitchCompat(context);
            toggle.LayoutParameters = layout;
            toggle.Checked = value;

            return toggle;
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
            var prop = GetPropertyForView(e.SeekBar);
            if (prop != null && prop.textValue != null)
                prop.textValue.Text = string.Format(prop.sliderFormat, e.Progress);
        }

        private Spinner CreateSpinner(string[] values, string value, string tooltip = null)
        {
            var spinner = new Spinner(new ContextThemeWrapper(context, Resource.Style.LightGrayTextMedium));
            var adapter = new CustomFontArrayAdapter(spinner, context, Android.Resource.Layout.SimpleSpinnerItem, values);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            spinner.Adapter = adapter;
            spinner.Background.SetColorFilter(new BlendModeColorFilter(DroidUtils.GetColorFromResources(context, Resource.Color.LightGreyFillColor2), BlendMode.SrcAtop));
            return spinner;
        }

        private HorizontalNumberPicker CreateNumberPicker(int value, int min, int max)
        {
            // MATTT this sucks, maybe do something like this, but with a long press mode that goes fast.
            // https://stackoverflow.com/questions/6796243/is-it-possible-to-make-a-horizontal-numberpicker
            var picker = new HorizontalNumberPicker(context);

            //picker.MinValue = min;
            //picker.MaxValue = max;
            //picker.Value = value;

            return picker;
        }

        public void UpdateCheckBoxList(int idx, string[] values, bool[] selected)
        {
        }

        public void UpdateCheckBoxList(int idx, bool[] selected)
        {
        }

        public int AddColoredTextBox(string value, System.Drawing.Color color)
        {
            return AddTextBox(null, ""); // MATTT : We need a label here on android.
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
                    properties[i].control == v)
                {
                    return i;
                }
            }

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

        private void OnTextBoxDialogButton(object o, DialogClickEventArgs e)
        {
            if (e.Which == (int)DialogButtonType.Positive)
            {
                if (o is AlertDialog dlg)
                {
                    var editText = dlg.FindViewById<EditText>(123);
                    if (editText != null)
                    {
                        var prop = (editText.Tag as PropertyTag).prop;
                        prop.textValue.Text = editText.Text; // MATTT Remove diacritics here.
                    }
                }
            }
        }

        private void OnTextBoxTouch(Property prop)
        {
            var editText = new EditText(context);
            editText.Text = prop.textValue.Text; // MATTT Where do we really store the value?
            editText.Id = 123;
            editText.Tag = new PropertyTag(prop);

            var builder = new AlertDialog.Builder(context);
            var dlg = builder.SetPositiveButton("OK", OnTextBoxDialogButton).SetNegativeButton("Cancel", OnTextBoxDialogButton).SetView(editText).Create();
            dlg.Show();
        }

        public int AddTextBox(string label, string value, int maxLength = 0, string tooltip = null)
        {
            // MATTT : maxLength

            var prop = new Property();
            prop.type = PropertyType.TextBox;
            prop.textLabel = CreateTextView(label, Resource.Style.LightGrayTextMedium);
            prop.textValue = CreateTextView(value, Resource.Style.LightGrayTextSmall);
            prop.textTooltip = !string.IsNullOrEmpty(tooltip) ? CreateTextView(tooltip, Resource.Style.LightGrayTextSmallTooltip) : null;
            prop.layout = CreateLinearLayout(false, true, false, 10);
            prop.layout.SetOnTouchListener(this);
            prop.onTouch = OnTextBoxTouch;
            properties.Add(prop);

            var inner = CreateLinearLayout(true, false, false, 0);

            prop.layout.AddView(inner);
            inner.AddView(prop.textLabel);
            if (prop.textTooltip != null)
                inner.AddView(prop.textTooltip);
            inner.AddView(prop.textValue);

            return properties.Count - 1;
        }

        private MaterialButton CreateButton(string value)
        {
            var button = new MaterialButton(context);
            button.Text = value;
            return button;
        }

        public int AddButton(string label, string value, string tooltip = null)
        {
            var prop = new Property();
            prop.type = PropertyType.TextBox;
            prop.layout = CreateLinearLayout(false, true, false, 10);
            prop.control = CreateButton(value);
            properties.Add(prop);

            var inner = CreateLinearLayout(true, false, false, 0);

            prop.layout.AddView(inner);
            inner.AddView(prop.control);

            return properties.Count - 1;
        }

        public int AddLabel(string label, string value, bool multiline = false, string tooltip = null)
        {
            Debug.Assert(false); // TODO : Store the label and apply it to the next control.
            return 0;
        }

        public int AddLinkLabel(string label, string value, string url, string tooltip = null)
        {
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
            prop.type = PropertyType.TextBox;
            prop.textLabel = CreateTextView("Color", Resource.Style.LightGrayTextMedium);
            prop.textValue = null; // CreateTextView(value, 12);
            prop.control = CreateColorPicker();
            prop.layout = CreateLinearLayout(false, true, false, 10);
            properties.Add(prop);

            var inner = CreateLinearLayout(true, false, false, 0);

            prop.layout.AddView(inner);
            inner.AddView(prop.textLabel);
            inner.AddView(prop.control);

            return 0;
        }

        public int AddIntegerRange(string label, int value, int min, int max, string tooltip = null)
        {
            var prop = new Property();
            prop.type = PropertyType.CheckBox;
            prop.textLabel = CreateTextView(label, Resource.Style.LightGrayTextMedium);
            prop.layout = CreateLinearLayout(false, true, false, 10);
            prop.control = CreateNumberPicker(value, min, max);
            properties.Add(prop);

            var inner = CreateLinearLayout(true, false, false, 0);

            prop.layout.AddView(inner);
            inner.AddView(prop.textLabel);
            inner.AddView(prop.control);

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
            prop.type = PropertyType.CheckBox;
            prop.textLabel = CreateTextView(label, Resource.Style.LightGrayTextMedium);
            prop.textValue = CreateSwitch(value);
            prop.layout = CreateLinearLayout(false, true, false, 10);
            properties.Add(prop);

            var inner = CreateLinearLayout(true, false, false, 0);

            prop.layout.AddView(inner);
            inner.AddView(prop.textLabel);
            prop.layout.AddView(prop.textValue);

            return properties.Count - 1;
        }

        public int AddLabelCheckBox(string label, bool value, int margin = 0)
        {
            return 0;
        }

        public int AddDropDownList(string label, string[] values, string value, string tooltip = null)
        {
            var prop = new Property();
            prop.type = PropertyType.CheckBox;
            prop.textLabel = CreateTextView(label, Resource.Style.LightGrayTextMedium);
            prop.layout = CreateLinearLayout(false, true, false, 10);
            prop.control = CreateSpinner(values, value, tooltip);
            properties.Add(prop);

            var inner = CreateLinearLayout(true, false, false, 0);

            prop.layout.AddView(inner);
            inner.AddView(prop.textLabel);
            inner.AddView(prop.control);

            return properties.Count - 1;
        }

        public int AddCheckBoxList(string label, string[] values, bool[] selected, string tooltip = null, int height = 200)
        {
            var prop = new Property();
            prop.type = PropertyType.Slider;
            prop.textLabel = CreateTextView(label, Resource.Style.LightGrayTextMedium);
            prop.layout = CreateLinearLayout(false, true, false, 10);
            properties.Add(prop);

            var inner = CreateLinearLayout(true, true, false, 0);

            prop.layout.AddView(inner);
            inner.AddView(prop.textLabel);

            // MATTT : Have an array of controls?
            for (int i = 0; i < values.Length; i++)
            {
                // MATTT : Function to create checkbox.
                var checkBox = new CheckBox(new ContextThemeWrapper(context, Resource.Style.LightGrayCheckBox));
                checkBox.Text = values[i];
                //checkBox.Typeface = quicksandBold;
                //checkBox.SetTextSize(ComplexUnitType.Dip, 12);
                checkBox.Checked = selected == null ? true : selected[i];
                inner.AddView(checkBox);
            }

            return properties.Count - 1;
        }

        public int AddSlider(string label, double value, double min, double max, double increment, int numDecimals, string format = "{0}", string tooltip = null)
        {
            var prop = new Property();
            prop.type = PropertyType.Slider;
            prop.textLabel = CreateTextView(label, Resource.Style.LightGrayTextMedium);
            if (format != null)
                prop.textValue = CreateTextView(string.Format(format, value), Resource.Style.LightGrayTextSmall);
            prop.layout = CreateLinearLayout(false, true, false, 10);
            prop.control = CreateSeekBar(value, min, max, 0, 0, false);
            prop.sliderFormat = format;
            properties.Add(prop);

            var inner = CreateLinearLayout(true, true, false, 0);

            prop.layout.AddView(inner);
            inner.AddView(prop.textLabel);
            if (prop.textValue != null)
                inner.AddView(prop.textValue);
            inner.AddView(prop.control);

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
            return null;
        }

        public T GetPropertyValue<T>(int idx)
        {
            return default(T);
        }

        public T GetPropertyValue<T>(int idx, int rowIdx, int colIdx)
        {
            return default(T);
        }

        public int GetSelectedIndex(int idx)
        {
            return 0;
        }

        public void SetPropertyValue(int idx, object value)
        {
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

                /*

                if (prop.label != null)
                {
                    var labelLayout = new GridLayout.LayoutParams(GridLayout.InvokeSpec(i), GridLayout.InvokeSpec(0));
                    prop.label.LayoutParameters = labelLayout;
                    labelLayout.SetGravity(GravityFlags.Left | GravityFlags.Top);
                    var viewLayout = new GridLayout.LayoutParams(GridLayout.InvokeSpec(i), GridLayout.InvokeSpec(1));
                    viewLayout.SetGravity(GravityFlags.FillHorizontal | GravityFlags.Top);
                    prop.view.LayoutParameters = viewLayout;

                    gridLayout.AddView(prop.label);
                    gridLayout.AddView(prop.view);

                    prop.label.SetBackgroundColor(Android.Graphics.Color.Red);
                    prop.view.SetBackgroundColor(Android.Graphics.Color.Green);
                }
                else
                {
                    var viewLayout = new GridLayout.LayoutParams(GridLayout.InvokeSpec(i), GridLayout.InvokeSpec(1, 2));
                    viewLayout.SetGravity(GravityFlags.FillHorizontal | GravityFlags.Top);
                    prop.view.LayoutParameters = viewLayout;

                    gridLayout.AddView(prop.view);
                }
                */
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

        //private View GetViewInternal(int position, ViewGroup parent)
        //{
        //    var text = new TextView(new ContextThemeWrapper(parent.Context, style));
        //    text.Text = GetItem(position).ToString();
        //    //text.SetPadding(70, 20, 20, 20);
        //    return text;

        //    //TextView v = base.GetView(position, convertView, parent) as TextView;
        //    //v.Typeface = typeface;
        //    //return v;
        //}

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
        MaterialButton buttonLess;
        MaterialButton buttonMore;
        TextView textView;

        public HorizontalNumberPicker(Context context) : base(context)
        {
            var lp = new LinearLayout.LayoutParams(DroidUtils.DpToPixels(64), DroidUtils.DpToPixels(48));
            lp.Weight = 1;
            lp.Gravity = GravityFlags.CenterHorizontal | GravityFlags.CenterVertical;

            buttonLess = new MaterialButton(context); //new MaterialButton(new ContextThemeWrapper(context, Resource.Style.BlackTextLargeBold));
            buttonLess.Text = "-";
            buttonLess.SetTextColor(Android.Graphics.Color.Black);
            buttonLess.BackgroundTintList = ColorStateList.ValueOf(DroidUtils.GetColorFromResources(context, Resource.Color.LightGreyFillColor2));
            buttonLess.LayoutParameters = lp;
            
            buttonMore = new MaterialButton(context); //new MaterialButton(new ContextThemeWrapper(context, Resource.Style.BlackTextLargeBold));
            buttonMore.Text = "+";
            buttonMore.SetTextColor(Android.Graphics.Color.Black);
            buttonMore.BackgroundTintList = ColorStateList.ValueOf(DroidUtils.GetColorFromResources(context, Resource.Color.LightGreyFillColor2));
            buttonMore.LayoutParameters = lp;
            
            textView = new TextView(new ContextThemeWrapper(context, Resource.Style.LightGrayTextMedium));
            textView.Text = "123";
            textView.LayoutParameters = lp;
            textView.Gravity = GravityFlags.Center;

            AddView(buttonLess);
            AddView(textView);
            AddView(buttonMore);

            buttonLess.Click += ButtonLess_Click;
            buttonMore.Click += ButtonMore_Click;
        }

        private void ButtonLess_Click(object sender, EventArgs e)
        {
        }

        private void ButtonMore_Click(object sender, EventArgs e)
        {
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
        const float MaxHeightScreen = 0.5f;

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

            if (modeHeight != MeasureSpecMode.Exactly)
            {
                var ratio = PropertyPage.CustomColors.GetLength(1) / (float)PropertyPage.CustomColors.GetLength(0);
                height = (int)(width * ratio);

                // Dont allow it to be more than 50% of the height of the screen. This prevent
                // it from being HUGE in landscale mode.
                if (modeWidth != MeasureSpecMode.Exactly)
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
