using System;
using System.Collections.Generic;

using DialogResult = System.Windows.Forms.DialogResult;

namespace FamiStudio
{
    class DeleteSpecialDialog
    {
        private PropertyDialog dialog;
        private bool inPropertyChanged = false;
        private Dictionary<int, int> propToEffect = new Dictionary<int, int>();

        public unsafe DeleteSpecialDialog(Channel channel, bool notes = true, int effectsMask = Note.EffectAllMask)
        {
            dialog = new PropertyDialog(200);
            dialog.Properties.AddLabelCheckBox("Delete Notes", notes);
            dialog.Properties.AddLabelCheckBox("Delete Effects", effectsMask == Note.EffectAllMask);

            for (int i = 0; i < Note.EffectCount; i++)
            {
                if (channel.ShouldDisplayEffect(i))
                {
                    propToEffect[dialog.Properties.PropertyCount] = i;
                    dialog.Properties.AddLabelCheckBox(Note.EffectNames[i], (effectsMask & (1 << i)) != 0, DpiScaling.ScaleForDialog(24));
                }
            }

            dialog.Properties.Build();
            dialog.Properties.PropertyChanged += Properties_PropertyChanged;
        }

        private void Properties_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (inPropertyChanged)
                return;

            inPropertyChanged = true; // Prevent recursion.

            if (propIdx == 1)
            {
                bool allEffects = (bool)value;

                foreach (var kv in propToEffect)
                {
                    props.SetPropertyValue(kv.Key, allEffects);
                }
            }
            else if (propToEffect.ContainsKey(propIdx))
            {
                bool allEffects = true;

                foreach (var kv in propToEffect)
                {
                    if (!props.GetPropertyValue<bool>(kv.Key))
                    {
                        allEffects = false;
                        break;
                    }
                }

                props.SetPropertyValue(1, allEffects);
            }

            inPropertyChanged = false;
        }

        public void ShowDialog(FamiStudioForm parent, Action<DialogResult> callback)
        {
            dialog.ShowDialog(parent, callback);
        }

        public bool DeleteNotes => dialog.Properties.GetPropertyValue<bool>(0);
        public int  DeleteEffectMask
        {
            get
            {
                int mask = 0;

                foreach (var kv in propToEffect)
                {
                    if (dialog.Properties.GetPropertyValue<bool>(kv.Key))
                    {
                        int effect = kv.Value;
                        mask |= (1 << effect);
                    }
                }

                return mask;
            }
        }
    }
}
