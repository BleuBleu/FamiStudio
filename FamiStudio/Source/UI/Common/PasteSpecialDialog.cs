using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

#if FAMISTUDIO_WINDOWS
    using RenderTheme = FamiStudio.Direct2DTheme;
#else
    using RenderTheme = FamiStudio.GLTheme;
#endif

namespace FamiStudio
{
    class PasteSpecialDialog
    {
        private PropertyDialog dialog;
        private bool inPropertyChanged = false;
        private Dictionary<int, int> propToEffect = new Dictionary<int, int>();

        public unsafe PasteSpecialDialog(Channel channel, bool mix = false, bool notes = true, int effectsMask = Note.EffectAllMask)
        {
            dialog = new PropertyDialog(200);
            dialog.Properties.AddLabelCheckBox("Mix With Existing Notes", mix);
            dialog.Properties.AddLabelCheckBox("Paste Notes", notes);
            dialog.Properties.AddLabelCheckBox("Paste Effects", effectsMask == Note.EffectAllMask);

            for (int i = 0; i < Note.EffectCount; i++)
            {
                if (channel.SupportsEffect(i))
                {
                    propToEffect[dialog.Properties.PropertyCount] = i;
                    dialog.Properties.AddLabelCheckBox(Note.EffectNames[i], (effectsMask & (1 << i)) != 0, (int)(24 * RenderTheme.DialogScaling));
                }
            }

            dialog.Properties.AddIntegerRange("Repeat :", 1, 1, 32);
            dialog.Properties.Build();
            dialog.Properties.PropertyChanged += Properties_PropertyChanged;
            dialog.Name = "PasteSpecialDialog";
        }

        private void Properties_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (inPropertyChanged)
                return;

            inPropertyChanged = true; // Prevent recursion.

            if (propIdx == 2)
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

                props.SetPropertyValue(2, allEffects);
            }

            inPropertyChanged = false;
        }

        public DialogResult ShowDialog(FamiStudioForm parent)
        {
            return dialog.ShowDialog(parent);
        }

        public bool PasteMix        => dialog.Properties.GetPropertyValue<bool>(0);
        public bool PasteNotes      => dialog.Properties.GetPropertyValue<bool>(1);
        public int  PasteEffectMask
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

        public int PasteRepeat => dialog.Properties.GetPropertyValue<int>(dialog.Properties.PropertyCount - 1);
    }
}
