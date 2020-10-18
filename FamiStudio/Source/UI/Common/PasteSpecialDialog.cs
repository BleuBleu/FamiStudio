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

        public unsafe PasteSpecialDialog(Channel channel, Rectangle mainWinRect, bool mix = false, bool notes = true, int effectsMask = Note.EffectAllMask)
        {
            int width  = 200;
            int height = 300;
            int x = mainWinRect.Left + (mainWinRect.Width  - width)  / 2;
            int y = mainWinRect.Top  + (mainWinRect.Height - height) / 2;

            dialog = new PropertyDialog(x, y, width, height);
            dialog.Properties.AddLabelBoolean("Mix With Existing Notes", mix);
            dialog.Properties.AddLabelBoolean("Paste Notes", notes);
            dialog.Properties.AddLabelBoolean("Paste Effects", effectsMask == Note.EffectAllMask);

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

            if (idx == 2)
            {
                bool allEffects = (bool)value;

                for (int i = 3; i < dialog.Properties.PropertyCount; i++)
                {
                    props.SetPropertyValue(i, allEffects);
                }
            }
            else if (idx >= 3)
            {
                bool allEffects = true;

                for (int i = 3; i < dialog.Properties.PropertyCount; i++)
                {
                    if (!props.GetPropertyValue<bool>(i))
                    {
                        allEffects = false;
                        break;
                    }
                }

                props.SetPropertyValue(2, allEffects);
            }

            inPropertyChanged = false;
        }

        public DialogResult ShowDialog()
        {
            return dialog.ShowDialog();
        }

        public bool PasteMix        => dialog.Properties.GetPropertyValue<bool>(0);
        public bool PasteNotes      => dialog.Properties.GetPropertyValue<bool>(1);
        public int  PasteEffectMask
        {
            get
            {
                int mask = 0;

                for (int i = 3; i < dialog.Properties.PropertyCount; i++)
                {
                    if (dialog.Properties.GetPropertyValue<bool>(i))
                    {
                        int effect = propToEffect[i];
                        mask |= (1 << effect);
                    }
                }

                return mask;
            }
        }
    }
}
