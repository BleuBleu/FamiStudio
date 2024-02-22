using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public partial class PropertyPage
    {
        public delegate void PropertyChangedDelegate(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value);
        public event PropertyChangedDelegate PropertyChanged;
        public delegate void PropertyWantsCloseDelegate(int idx);
        public event PropertyWantsCloseDelegate PropertyWantsClose;
        public delegate void PropertyClickedDelegate(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx);
        public event PropertyClickedDelegate PropertyClicked;
        public delegate bool PropertyCellEnabledDelegate(PropertyPage props, int propIdx, int rowIdx, int colIdx); 
        public event PropertyCellEnabledDelegate PropertyCellEnabled;

        private object userData;
        private int advancedPropertyStart = -1;
        private bool showWarnings = false;

        public object PropertiesUserData { get => userData; set => userData = value; }
        public bool HasAdvancedProperties { get => advancedPropertyStart > 0; }
        public bool ShowWarnings { get => showWarnings; set => showWarnings = value; }
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
        NoHeader = 1
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

        //public Type GetPropertyType()
        //{
        //    switch (this.Type)
        //    {
        //        case ColumnType.Slider:
        //            return typeof(int);
        //        case ColumnType.CheckBox: 
        //            return typeof(bool);
        //        default: 
        //            return typeof(string);
        //    }
        //}
    };
}
