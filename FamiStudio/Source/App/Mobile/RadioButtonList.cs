using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class RadioButtonList : Container
    {
        public delegate void RadioChangedDelegate(Control sender, int index);
        public event RadioChangedDelegate RadioChanged;

        private int selectedIndex;
        private int rowHeight = DpiScaling.ScaleForWindow(14);
        private List<RadioButton> radioButtons = new List<RadioButton>();

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                selectedIndex = value;
                for (var i = 0; i < radioButtons.Count; i++)
                {
                    radioButtons[i].Checked = i == selectedIndex;
                }
                MarkDirty();
            }
        }

        public RadioButtonList(string[] values, int idx)
        {
            clipRegion = false;
            height = rowHeight * values.Length;
            selectedIndex = idx;

            for (int i = 0; i < values.Length; i++)
            {
                var radio = new RadioButton(values[i], i == selectedIndex, false);
                radio.RadioChanged += Radio_RadioChanged;
                radio.Move(0, i * rowHeight, width, rowHeight);
                radioButtons.Add(radio);
            }
        }

        protected override void OnAddedToContainer()
        {
            foreach (var radio in radioButtons)
            {
                AddControl(radio);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            foreach (var radio in radioButtons)
            {
                radio.Resize(width, radio.Height);
            }
        }

        private void Radio_RadioChanged(Control sender, int index)
        {
            selectedIndex = index;
            RadioChanged?.Invoke(this, index);
        }
    }
}
