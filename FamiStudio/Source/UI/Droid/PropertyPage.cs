using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FamiStudio
{
    public partial class PropertyPage
    {
        public int PropertyCount => 0;

        public void UpdateCheckBoxList(int idx, string[] values, bool[] selected)
        {
        }

        public void UpdateCheckBoxList(int idx, bool[] selected)
        {
        }

        public int AddColoredString(string value, System.Drawing.Color color)
        {
            return 0;
        }

        public int AddString(string label, string value, int maxLength = 0, string tooltip = null)
        {
            return 0;
        }

        public int AddMultilineString(string label, string value)
        {
            return 0;
        }

        public int AddButton(string label, string value, string tooltip = null)
        {
            return 0;
        }

        public int AddLabel(string label, string value, bool multiline = false, string tooltip = null)
        {
            return 0;
        }

        public int AddLinkLabel(string label, string value, string url, string tooltip = null)
        {
            return 0;
        }

        public int AddColorPicker(System.Drawing.Color color)
        {
            return 0;
        }

        public int AddIntegerRange(string label, int value, int min, int max, string tooltip = null)
        {
            return 0;
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

        public void AddCheckBox(string label, bool value, string tooltip = null)
        {
        }

        public int AddLabelCheckBox(string label, bool value, int margin = 0)
        {
            return 0;
        }

        public int AddDropDownList(string label, string[] values, string value, string tooltip = null)
        {
            return 0;
        }

        public int AddCheckBoxList(string label, string[] values, bool[] selected, string tooltip = null, int height = 200)
        {
            return 0;
        }

        public int AddSlider(string label, double value, double min, double max, double increment, int numDecimals, string format = "{0}", string tooltip = null)
        {
            return 0;
        }

        public void SetColumnEnabled(int propIdx, int colIdx, bool enabled)
        {
        }

        public void AddMultiColumnList(ColumnDesc[] columnDescs, object[,] data, int height = 300)
        {
        }

        public void UpdateMultiColumnList(int idx, object[,] data, string[] columnNames = null)
        {
        }

        public void UpdateMultiColumnList(int idx, int rowIdx, int colIdx, object value)
        {
        }

        public void SetPropertyEnabled(int idx, bool enabled)
        {
        }

        public void BeginAdvancedProperties()
        {
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
    }
}