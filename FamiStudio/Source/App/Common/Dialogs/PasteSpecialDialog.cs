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
        LocalizedString RepeatLabel;
        LocalizedString RepeatTooltip;

        #endregion

        public unsafe PasteSpecialDialog(FamiStudioWindow win, Channel channel, bool mix = false, bool notes = true, int effectsMask = Note.EffectAllMask)
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

            dialog = new PropertyDialog(win, PasteSpecialTitle, 260);
            dialog.Properties.AddLabelCheckBox(MixWithExistingLabel.Colon, mix, 0, MixWithExistingTooltip); // 0
            dialog.Properties.AddLabelCheckBox(PasteNotesLabel.Colon, notes, 0, PasteNotesTooltip); // 1
            dialog.Properties.AddCheckBoxList(EffectsToPasteLabel.Colon, effectList.ToArray(), checkedList.ToArray(), "Select the effects to paste."); // 2
            dialog.Properties.AddNumericUpDown(RepeatLabel.Colon, 1, 1, 32, 1, 1, RepeatTooltip); // 5
            dialog.Properties.Build();
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
                var checks = dialog.Properties.GetPropertyValue<bool[]>(2);

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
