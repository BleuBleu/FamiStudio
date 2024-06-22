using System;
using System.Collections.Generic;

namespace FamiStudio
{
    class PasteSpecialDialog
    {
        private PropertyDialog dialog;
        private List<int> checkToEffect = new List<int>();

        #region Localization

        LocalizedString PasteSpecialTitle;
        LocalizedString MixWithExistingLabel;
        LocalizedString MixWithExistingTooltip;
        LocalizedString PasteNotesLabel;
        LocalizedString PasteNotesTooltip;
        LocalizedString EffectsToPasteLabel;
        LocalizedString MobileSelectAllLabel;
        LocalizedString SelectAllLabel;
        LocalizedString MobileSelectNoneLabel;
        LocalizedString SelectNoneLabel;
        LocalizedString RepeatLabel;
        LocalizedString RepeatTooltip;

        #endregion

        public unsafe PasteSpecialDialog(FamiStudioWindow win, Channel channel, bool mix = false, bool notes = true, int effectsMask = Note.EffectAllMask)
        {
            Localization.Localize(this);

            dialog = new PropertyDialog(win, PasteSpecialTitle, 260);
            dialog.Properties.AddLabelCheckBox(MixWithExistingLabel.Colon, mix, 0, MixWithExistingTooltip); // 0
            dialog.Properties.AddLabelCheckBox(PasteNotesLabel.Colon, notes, 0, PasteNotesTooltip); // 1
            dialog.Properties.AddLabel(null, EffectsToPasteLabel.Colon); // 2

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

            dialog.Properties.AddCheckBoxList(Platform.IsMobile ? EffectsToPasteLabel : null, effectList.ToArray(), checkedList.ToArray(), "Select the effects to paste."); // 3
            dialog.Properties.AddButton(Platform.IsMobile ? MobileSelectAllLabel  : null, SelectAllLabel); // 4
            dialog.Properties.AddButton(Platform.IsMobile ? MobileSelectNoneLabel : null, SelectNoneLabel); // 5
            dialog.Properties.AddNumericUpDown(RepeatLabel.Colon, 1, 1, 32, 1, RepeatTooltip); // 6
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

        public void ShowDialogAsync(Action<DialogResult> callback)
        {
             dialog.ShowDialogAsync(callback);
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
