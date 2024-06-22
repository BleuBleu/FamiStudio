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
        LocalizedString MobileSelectAllLabel;
        LocalizedString SelectAllLabel;
        LocalizedString MobileSelectNoneLabel;
        LocalizedString SelectNoneLabel;

        #endregion

        public unsafe DeleteSpecialDialog(FamiStudioWindow win, Channel channel, bool notes = true, int effectsMask = Note.EffectAllMask)
        {
            Localization.Localize(this);

            dialog = new PropertyDialog(win, DeleteSpecialTitle, 260);
            dialog.Properties.AddLabelCheckBox(DeleteNotesLabel, notes, 0, DeleteNotesTooltip); // 0
            dialog.Properties.AddLabel(null, EffectsToDeleteLabel.Colon); // 1

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


            dialog.Properties.AddCheckBoxList(Platform.IsMobile ? EffectsToDeleteLabel : null, effectList.ToArray(), checkedList.ToArray(), "Select the effects to delete."); // 2
            dialog.Properties.AddButton(Platform.IsMobile ? MobileSelectAllLabel  : null, SelectAllLabel); // 3
            dialog.Properties.AddButton(Platform.IsMobile ? MobileSelectNoneLabel : null, SelectNoneLabel); // 4
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
