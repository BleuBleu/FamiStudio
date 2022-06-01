using System;
using System.Collections.Generic;

namespace FamiStudio
{
    class PasteSpecialDialog
    {
        private PropertyDialog dialog;
        private List<int> checkToEffect = new List<int>();

        public unsafe PasteSpecialDialog(Channel channel, bool mix = false, bool notes = true, int effectsMask = Note.EffectAllMask)
        {
            dialog = new PropertyDialog("Paste Special", 260);
            dialog.Properties.AddLabelCheckBox("Mix With Existing Notes", mix, 0, "When enabled, will preserve the existing note/effects and only paste if there was nothing already there."); // 0
            dialog.Properties.AddLabelCheckBox("Paste Notes", notes, 0, "When enabled, will paste the musical notes."); // 1
            dialog.Properties.AddLabel(null, "Effects to paste:"); // 2

            var effectList  = new List<string>();
            var checkedList = new List<bool>();

            for (int i = 0; i < Note.EffectCount; i++)
            {
                if (channel.ShouldDisplayEffect(i))
                {
                    checkToEffect.Add(i);
                    effectList.Add(Note.EffectNames[i]);
                    checkedList.Add((effectsMask & (1 << i)) != 0);
                }
            }

            dialog.Properties.AddCheckBoxList(Platform.IsMobile ? "Effects to paste" : null, effectList.ToArray(), checkedList.ToArray(), "Select the effects to paste."); // 3
            dialog.Properties.AddButton(Platform.IsMobile ? "Select All Effects" : null, "Select All"); // 4
            dialog.Properties.AddButton(Platform.IsMobile ? "De-select All Effects" : null, "Select None"); // 5
            dialog.Properties.AddNumericUpDown("Repeat :", 1, 1, 32, "Number of times to repeat the paste"); // 6
            dialog.Properties.SetPropertyVisible(2, Platform.IsDesktop);
            dialog.Properties.Build();
            dialog.Properties.PropertyClicked += Properties_PropertyClicked;
        }

        private void Properties_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (click == ClickType.Button && (propIdx == 4 || propIdx == 5))
            {
                var keys = new bool[checkToEffect.Count];
                for (int i = 0; i < keys.Length; i++)
                    keys[i] = propIdx == 4;
                props.UpdateCheckBoxList(3, keys);
            }
        }

        public void ShowDialogAsync(FamiStudioWindow parent, Action<DialogResult> callback)
        {
             dialog.ShowDialogAsync(parent, callback);
        }

        public bool PasteMix        => dialog.Properties.GetPropertyValue<bool>(0);
        public bool PasteNotes      => dialog.Properties.GetPropertyValue<bool>(1);
        public int  PasteEffectMask
        {
            get
            {
                int mask = 0;
                var checks = dialog.Properties.GetPropertyValue<bool[]>(3);

                for (int i = 0; i < checkToEffect.Count; i++)
                {
                    if (checks[i])
                        mask |= (1 << checkToEffect[i]);
                }

                return mask;
            }
        }

        public int PasteRepeat => dialog.Properties.GetPropertyValue<int>(dialog.Properties.PropertyCount - 1);
    }
}
