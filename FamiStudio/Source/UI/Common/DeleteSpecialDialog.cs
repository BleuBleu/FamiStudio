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
    class DeleteSpecialDialog
    {
        private PropertyDialog dialog;
        private bool inPropertyChanged = false;
        private Dictionary<int, int> propToEffect = new Dictionary<int, int>();

        public unsafe DeleteSpecialDialog(Channel channel, Rectangle mainWinRect, bool notes = true, int effectsMask = Note.EffectAllMask)
        {
            int width  = 200;
            int height = 300;
            int x = mainWinRect.Left + (mainWinRect.Width  - width)  / 2;
            int y = mainWinRect.Top  + (mainWinRect.Height - height) / 2;

            dialog = new PropertyDialog(x, y, width, height);
            dialog.Properties.AddLabelBoolean("Delete Notes", notes);
            dialog.Properties.AddLabelBoolean("Delete Effects", effectsMask == Note.EffectAllMask);

            for (int i = 0; i < Note.EffectCount; i++)
            {
                if (channel.SupportsEffect(i))
                {
                    propToEffect[dialog.Properties.PropertyCount] = i;
                    dialog.Properties.AddLabelBoolean(Note.EffectNames[i], (effectsMask & (1 << i)) != 0, (int)(24 * RenderTheme.DialogScaling));
                }
            }

            dialog.Properties.Build();
            dialog.Properties.PropertyChanged += Properties_PropertyChanged;
        }

        private void Properties_PropertyChanged(PropertyPage props, int idx, object value)
        {
            if (inPropertyChanged)
                return;

            inPropertyChanged = true; // Prevent recursion.

            if (idx == 1)
            {
                bool allEffects = (bool)value;

                foreach (var kv in propToEffect)
                {
                    props.SetPropertyValue(kv.Key, allEffects);
                }
            }
            else if (propToEffect.ContainsKey(idx))
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

        public DialogResult ShowDialog()
        {
            return dialog.ShowDialog();
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
