using System;
using System.Collections.Generic;

namespace FamiStudio
{
    class DeleteSpecialDialog
    {
        private PropertyDialog dialog;
        private List<int> checkToEffect = new List<int>();

        public unsafe DeleteSpecialDialog(Channel channel, bool notes = true, int effectsMask = Note.EffectAllMask)
        {
            dialog = new PropertyDialog("Delete Special", 260);
            dialog.Properties.AddLabelCheckBox("Delete Notes", notes, 0, "When enabled, will delete the musical notes."); // 0
            dialog.Properties.AddLabel(null, "Effects to paste:"); // 1

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


            dialog.Properties.AddCheckBoxList(Platform.IsMobile ? "Effects to delete" : null, effectList.ToArray(), checkedList.ToArray(), "Select the effects to delete."); // 2
            dialog.Properties.AddButton(Platform.IsMobile ? "Select All Effects" : null, "Select All"); // 3
            dialog.Properties.AddButton(Platform.IsMobile ? "De-select All Effects" : null, "Select None"); // 4
            dialog.Properties.SetPropertyVisible(1, Platform.IsDesktop);
            dialog.Properties.Build();
            dialog.Properties.PropertyClicked += Properties_PropertyClicked;
        }

        private void Properties_PropertyClicked(PropertyPage props, ClickType click, int propIdx, int rowIdx, int colIdx)
        {
            if (click == ClickType.Button && (propIdx == 3 || propIdx == 4))
            {
                var keys = new bool[checkToEffect.Count];
                for (int i = 0; i < keys.Length; i++)
                    keys[i] = propIdx == 3;
                props.UpdateCheckBoxList(2, keys);
            }
        }

        public void ShowDialogAsync(FamiStudioWindow parent, Action<DialogResult> callback)
        {
            dialog.ShowDialogAsync(parent, callback);
        }

        public bool DeleteNotes => dialog.Properties.GetPropertyValue<bool>(0);
        public int  DeleteEffectMask
        {
            get
            {
                int mask = 0;
                var checks = dialog.Properties.GetPropertyValue<bool[]>(2);

                for (int i = 0; i < checkToEffect.Count; i++)
                {
                    if (checks[i])
                        mask |= (1 << checkToEffect[i]);
                }

                return mask;
            }
        }
    }
}
