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

        public virtual void ShowParamContextMenu()
        {
            App.ShowContextMenuAsync(new[]
            {
                new ContextMenuOption("MenuReset", ResetDefaultValueContext, () => { ResetParamDefaultValue(); })
            });
        }

        protected ParamControl(ParamInfo p)
        {
            param = p;
            supportsDoubleClick = false;
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
            if (Platform.IsMobile)
            {
                var scale = param.GetScaleValue();
                var value = param.GetValue();
                var scaledValue = Math.Round(value * scale, 2);

                Platform.EditTextAsync(param.Name, scaledValue.ToString(), (s) => 
                {
                    InvokeValueChangeStart();
                    param.SetValue(param.SnapAndClampValue((int)Math.Round(Utils.ParseFloatWithTrailingGarbage(s) / scale)));
                    InvokeValueChangeEnd();
                    MarkDirty();
                });
            }
            else
            {
                var dlg = new ValueInputDialog(ParentWindow, new Point(WindowPosition.X, WindowPosition.Y), param.Name, param.GetValue(), param.GetMinValue(), param.GetMaxValue(), true, param.GetScaleValue());
                dlg.ShowDialogAsync((r) =>
                {
                    if (r == DialogResult.OK)
                    {
                        InvokeValueChangeStart();
                        param.SetValue(param.SnapAndClampValue(dlg.Value));
                        InvokeValueChangeEnd();
                        MarkDirty();
                    }
                });
            }
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
