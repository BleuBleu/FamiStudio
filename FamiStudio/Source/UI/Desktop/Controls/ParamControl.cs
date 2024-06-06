using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace FamiStudio
{
    public abstract class ParamControl : Control
    {
        protected ParamInfo param;

        protected LocalizedString EnterValueContext;
        protected LocalizedString ResetDefaultValueContext;

        public event ControlDelegate ValueChangeStart;
        public event ControlDelegate ValueChangeEnd;

        protected ParamControl(ParamInfo p)
        {
            param = p;
            Localization.Localize(this);
        }

        protected void InvokeValueChangeStart()
        {
            ValueChangeStart?.Invoke(this);
        }

        protected void InvokeValueChangeEnd()
        {
            ValueChangeEnd?.Invoke(this);
        }

        protected void EnterParamValue()
        {
            var dlg = new ValueInputDialog(ParentWindow, new Point(WindowPosition.X, WindowPosition.Y), param.Name, param.GetValue(), param.GetMinValue(), param.GetMaxValue(), true);
            dlg.ShowDialogAsync((r) =>
            {
                if (r == DialogResult.OK)
                {
                    InvokeValueChangeStart();
                    param.SetValue(dlg.Value);
                    InvokeValueChangeEnd();
                    MarkDirty();
                }
            });
        }

        protected void ResetParamDefaultValue()
        {
            InvokeValueChangeStart();
            param.SetValue(param.DefaultValue);
            InvokeValueChangeEnd();
            MarkDirty();
        }
    }
}
