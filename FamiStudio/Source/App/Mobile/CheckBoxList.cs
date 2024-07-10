using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class CheckBoxList : Container
    {
        public delegate void CheckedChangedDelegate(Control sender, int index, bool value);
        public event CheckedChangedDelegate CheckedChanged;

        private int rowHeight = DpiScaling.ScaleForWindow(14);
        private List<CheckBox> checkList = new List<CheckBox>();

        public CheckBoxList(string[] values, bool[] selected)
        {
            clipRegion = false;
            height = rowHeight * values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                var checkBox = new CheckBox(selected == null ? true : selected[i], values[i]);
                checkBox.CheckedChanged += CheckBox_CheckedChanged;
                checkBox.Move(0, i * rowHeight, width, rowHeight);
                checkBox.UserData = i;
                checkList.Add(checkBox);
            }
        }

        protected override void OnAddedToContainer()
        {
            foreach (var check in checkList)
            {
                AddControl(check);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            foreach (var check in checkList)
            {
                check.Resize(width, check.Height);
            }
        }

        private void CheckBox_CheckedChanged(Control sender, bool check)
        {
            CheckedChanged?.Invoke(this, (int)this.UserData, check);
        }
    }
}
