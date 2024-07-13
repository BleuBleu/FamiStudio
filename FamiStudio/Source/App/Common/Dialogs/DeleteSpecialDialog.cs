using System;
using System.Collections.Generic;

namespace FamiStudio
{
    class DeleteSpecialDialog
    {
        private PropertyDialog dialog;
        private List<int> checkToEffect = new List<int>();

        #region Localization

        LocalizedString DeleteSpecialTitle;
        LocalizedString DeleteNotesLabel;
        LocalizedString DeleteNotesTooltip;
        LocalizedString EffectsToDeleteLabel;

        #endregion

        public unsafe DeleteSpecialDialog(FamiStudioWindow win, Channel channel, bool notes = true, int effectsMask = Note.EffectAllMask)
        {
            Localization.Localize(this);

            var effectList  = new List<string>();
            var checkedList = new List<bool>();

            for (int i = 0; i < Note.EffectCount; i++)
            {
                if (channel.ShouldDisplayEffect(i))
                {
                    checkToEffect.Add(i);
                    effectList.Add(EffectType.LocalizedNames[i]);
                    checkedList.Add((effectsMask & (1 << i)) != 0);
                }
            }


            dialog = new PropertyDialog(win, DeleteSpecialTitle, 260);
            dialog.Properties.AddLabelCheckBox(DeleteNotesLabel, notes, 0, DeleteNotesTooltip); // 0
            dialog.Properties.AddCheckBoxList(EffectsToDeleteLabel.Colon, effectList.ToArray(), checkedList.ToArray(), "Select the effects to delete.", 7, PropertyFlags.ForceFullWidth); // 1
            dialog.Properties.Build();
        }

        public void ShowDialogAsync(Action<DialogResult> callback)
        {
            dialog.ShowDialogAsync(callback);
        }

        public bool DeleteNotes => dialog.Properties.GetPropertyValue<bool>(0);
        public int  DeleteEffectMask
        {
            get
            {
                int mask = 0;
                var checks = dialog.Properties.GetPropertyValue<bool[]>(1);

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
