using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class RadioButtonList : Container
    {
        public delegate void RadioChangedDelegate(Control sender, int index);
        public event RadioChangedDelegate RadioChanged;

        private int rowHeight = DpiScaling.ScaleForWindow(14);
        private List<RadioButton> radioButtons = new List<RadioButton>();

        public RadioButtonList(string[] values, int selectedIndex)
        {
            clipRegion = false;
            height = rowHeight * values.Length;

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
            RadioChanged?.Invoke(this, index);
        }
    }
}
