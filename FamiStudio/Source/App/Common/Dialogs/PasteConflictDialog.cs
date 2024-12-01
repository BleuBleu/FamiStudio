using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    class PasteConflictDialog
    {
        private PropertyDialog dialog;

        private int instMissingIndex = -1;
        private int instNameConflictIndex = -1;

        private int arpMissingIndex = -1;
        private int arpNameConflictIndex = -1;

        private int sampleMissingIndex = -1;
        private int sampleNameConflictIndex = -1;

        private int patternMissingIndex = -1;
        private int patternNameConflictIndex = -1;

        #region Localization

        LocalizedString PasteConflictTitle;
        LocalizedString MissingLabel;
        LocalizedString CreateMissingOption;
        LocalizedString NoNotCreateMissingOption;
        LocalizedString ExistingLabel;
        LocalizedString MatchByNameOption;
        LocalizedString CreateNewOneOption;
        LocalizedString InstrumentName;
        LocalizedString ArpeggioName;
        LocalizedString DPCMSampleName;
        LocalizedString PatternName;

        #endregion

        public unsafe PasteConflictDialog(FamiStudioWindow win, ClipboardContentFlags instFlags, ClipboardContentFlags arpFlags, ClipboardContentFlags sampleFlags, ClipboardContentFlags patternFlags = ClipboardContentFlags.None)
        {
            Localization.Localize(this);

            dialog = new PropertyDialog(win, PasteConflictTitle, 460);

            // Create missing
            if (instFlags.HasFlag(ClipboardContentFlags.ContainsMissing))
            {
                dialog.Properties.AddLabel(null, MissingLabel.Format(InstrumentName), true); // 0
                instMissingIndex = dialog.Properties.AddDropDownList(null, [CreateMissingOption.Format(InstrumentName), NoNotCreateMissingOption.Format(InstrumentName)], CreateMissingOption.Format(InstrumentName), null, PropertyFlags.ForceFullWidth);
            }

            if (arpFlags.HasFlag(ClipboardContentFlags.ContainsMissing))
            {
                dialog.Properties.AddLabel(null, MissingLabel.Format(ArpeggioName), true); // 0
                arpMissingIndex = dialog.Properties.AddDropDownList(null, [CreateMissingOption.Format(ArpeggioName), NoNotCreateMissingOption.Format(ArpeggioName)], CreateMissingOption.Format(ArpeggioName), null, PropertyFlags.ForceFullWidth);
            }

            if (sampleFlags.HasFlag(ClipboardContentFlags.ContainsMissing))
            {
                dialog.Properties.AddLabel(null, MissingLabel.Format(DPCMSampleName), true); // 0
                sampleMissingIndex = dialog.Properties.AddDropDownList(null, [CreateMissingOption.Format(DPCMSampleName), NoNotCreateMissingOption.Format(DPCMSampleName)], CreateMissingOption.Format(DPCMSampleName), null, PropertyFlags.ForceFullWidth);
            }

            // This will never be set, we always want to include missing patterns.
            Debug.Assert(!patternFlags.HasFlag(ClipboardContentFlags.ContainsMissing));

            // Name conflicts.
            if (instFlags.HasFlag(ClipboardContentFlags.NameConflict))
            {
                dialog.Properties.AddLabel(null, ExistingLabel.Format(InstrumentName), true); // 0
                instNameConflictIndex = dialog.Properties.AddDropDownList(null, [MatchByNameOption.Format(InstrumentName), CreateNewOneOption.Format(InstrumentName)], MatchByNameOption.Format(InstrumentName), null, PropertyFlags.ForceFullWidth);
            }

            if (arpFlags.HasFlag(ClipboardContentFlags.NameConflict))
            {
                dialog.Properties.AddLabel(null, ExistingLabel.Format(ArpeggioName), true); // 0
                arpNameConflictIndex = dialog.Properties.AddDropDownList(null, [MatchByNameOption.Format(ArpeggioName), CreateNewOneOption.Format(ArpeggioName)], MatchByNameOption.Format(ArpeggioName), null, PropertyFlags.ForceFullWidth);
            }

            if (sampleFlags.HasFlag(ClipboardContentFlags.NameConflict))
            {
                dialog.Properties.AddLabel(null, ExistingLabel.Format(DPCMSampleName), true); // 0
                sampleNameConflictIndex = dialog.Properties.AddDropDownList(null, [MatchByNameOption.Format(DPCMSampleName), CreateNewOneOption.Format(DPCMSampleName)], MatchByNameOption.Format(DPCMSampleName), null, PropertyFlags.ForceFullWidth);
            }

            if (patternFlags.HasFlag(ClipboardContentFlags.NameConflict))
            {
                dialog.Properties.AddLabel(null, ExistingLabel.Format(PatternName), true); // 0
                patternNameConflictIndex = dialog.Properties.AddDropDownList(null, [MatchByNameOption.Format(PatternName), CreateNewOneOption.Format(PatternName)], MatchByNameOption.Format(PatternName), null, PropertyFlags.ForceFullWidth);
            }

            dialog.Properties.Build();
        }

        public void ShowDialogAsync(Action<DialogResult> callback)
        {
            dialog.ShowDialogAsync(callback);
        }

        public ClipboardImportFlags InstrumentFlags
        {
            get
            {
                var flags = ClipboardImportFlags.CreateMissing | ClipboardImportFlags.MatchByName;
                if (instMissingIndex >= 0 && dialog.Properties.GetSelectedIndex(instMissingIndex) == 1)
                {
                    flags &= (~ClipboardImportFlags.CreateMissing);
                }
                if (instNameConflictIndex >= 0 && dialog.Properties.GetSelectedIndex(instNameConflictIndex) == 1)
                {
                    flags &= (~ClipboardImportFlags.MatchByName);
                }
                return flags;
            }
        }

        public ClipboardImportFlags ArpeggioFlags
        {
            get
            {
                var flags = ClipboardImportFlags.CreateMissing | ClipboardImportFlags.MatchByName;
                if (arpMissingIndex >= 0 && dialog.Properties.GetSelectedIndex(arpMissingIndex) == 1)
                {
                    flags &= (~ClipboardImportFlags.CreateMissing);
                }
                if (arpNameConflictIndex >= 0 && dialog.Properties.GetSelectedIndex(arpNameConflictIndex) == 1)
                {
                    flags &= (~ClipboardImportFlags.MatchByName);
                }
                return flags;
            }
        }

        public ClipboardImportFlags DPCMSampleFlags
        {
            get
            {
                var flags = ClipboardImportFlags.CreateMissing | ClipboardImportFlags.MatchByName;
                if (sampleMissingIndex >= 0 && dialog.Properties.GetSelectedIndex(sampleMissingIndex) == 1)
                {
                    flags &= (~ClipboardImportFlags.CreateMissing);
                }
                if (sampleNameConflictIndex >= 0 && dialog.Properties.GetSelectedIndex(sampleNameConflictIndex) == 1)
                {
                    flags &= (~ClipboardImportFlags.MatchByName);
                }
                return flags;
            }
        }

        public ClipboardImportFlags PatternFlags
        {
            get
            {
                var flags = ClipboardImportFlags.CreateMissing | ClipboardImportFlags.MatchByName;
                if (patternNameConflictIndex >= 0 && dialog.Properties.GetSelectedIndex(patternNameConflictIndex) == 1)
                {
                    flags &= (~ClipboardImportFlags.MatchByName);
                }
                return flags;
            }
        }
    }
}
