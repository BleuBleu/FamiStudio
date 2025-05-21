using System;
using System.Globalization;
using System.Collections.Generic;

namespace FamiStudio
{
    class ValueInputDialog
    {
        private PropertyDialog dialog;
        private float minValue;
        private float maxValue;

        public unsafe ValueInputDialog(FamiStudioWindow win, Point pt, string paramName, float value, float min, float max, bool leftAlign, float inc = 1.0f)
        {
            minValue = min;
            maxValue = max;

            dialog = new PropertyDialog(win, paramName, pt, 200, leftAlign);

            // Our up/down widget on android doesnt have a text box to type at the moment.
            if (Platform.IsMobile)
            {
                dialog.Properties.AddTextBox(null, value.ToString(CultureInfo.InvariantCulture), 0, true); // 0
                dialog.Properties.PropertyWantsClose += Properties_PropertyWantsClose;
            }
            else
            {
                dialog.Properties.AddNumericUpDownFloat(null, value, min, max, inc, null); // 0
            }

            dialog.Properties.Build();
        }

        private void Properties_PropertyWantsClose(int idx)
        {
            dialog.Close(DialogResult.OK);
        }

        public void ShowDialogAsync(Action<DialogResult> callback)
        {
            dialog.ShowDialogAsync(callback);
        }

        private float GetValue()
        {
            var rawValue = Platform.IsMobile?
                Utils.ParseIntWithTrailingGarbage(dialog.Properties.GetPropertyValue<string>(0)) :
                dialog.Properties.GetPropertyValue<float>(0);

            return Utils.Clamp(rawValue, minValue, maxValue);
        }

        public float Value => GetValue();
    }
}
