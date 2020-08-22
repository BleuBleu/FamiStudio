using Gtk;
using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class DomainSpinButton : SpinButton
    {
        int[] values;

        public DomainSpinButton(int[] values, int value) : base(0, values.Length - 1, 1)
        {
            UpdateValues(values, value);
        }

        public void UpdateValues(int[] values, int value)
        {
            Debug.Assert(this.values == null || this.values.Length == values.Length);
            this.values = values;
            this.Value = Array.IndexOf(values, value);
        }

        protected override int OnInput(out double new_value)
        {
            new_value = Array.IndexOf(values, int.Parse(Text));
            return 1;
        }

        protected override int OnOutput()
        {
            int idx = Utils.Clamp((int)Value, 0, values.Length - 1);
            Text = values[idx].ToString();
            return 1;
        }
    }
}
