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
        private CheckBox[] checkList;

        #region Localization

        LocalizedString SelectAllLabel;
        LocalizedString SelectNoneLabel;

        #endregion

        public CheckBoxList(string[] values, bool[] selected)
        {
            Localization.Localize(this);

            clipRegion = false;
            height = rowHeight * values.Length;
            checkList = new CheckBox[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                var checkBox = new CheckBox(selected == null ? true : selected[i], values[i]);
                checkBox.CheckedChanged += CheckBox_CheckedChanged;
                checkBox.Move(0, i * rowHeight, width, rowHeight);
                checkBox.UserData = i;
                checkList[i] = checkBox;
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

        public override void OnContainerPointerUpNotify(Control control, PointerEventArgs e)
        {
            if (e.Right && !e.Handled)
            {
                App.ShowContextMenuAsync(new[]
                {
                    new ContextMenuOption("SelectAll",  SelectAllLabel,  () => SelectAllCheckBoxes(true)),
                    new ContextMenuOption("SelectNone", SelectNoneLabel, () => SelectAllCheckBoxes(false))
                });
                e.MarkHandled();
            }
        }

        private void SelectAllCheckBoxes(bool check)
        {
            foreach (var chk in checkList)
            {
                chk.Checked = check;
            }
        }

        public bool[] Values
        {
            get
            {
                var values = new bool[checkList.Length];
                for (var i = 0; i < checkList.Length; i++)
                    values[i] = checkList[i].Checked;
                return values;
            }
        }

        private void CheckBox_CheckedChanged(Control sender, bool check)
        {
            CheckedChanged?.Invoke(this, (int)sender.UserData, check);
        }
    }
}
