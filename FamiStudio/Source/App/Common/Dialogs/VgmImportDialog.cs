using System;

namespace FamiStudio
{
    class VgmImportDialog
    {
        private PropertyDialog dialog;
        private string filename;

        #region Localization

        LocalizedString VgmImportTitle;
        LocalizedString PatternLength;
        LocalizedString FramesToSkip;
        LocalizedString AdjustClock;
        LocalizedString ReverseDPCMBitsLabel;
        LocalizedString PreserveDPCMPaddingByte;
        LocalizedString Ym2149AsEPSM;

        #endregion

        public VgmImportDialog(FamiStudioWindow win, string file, Action<Project> action)
        {
            Localization.Localize(this);

            filename = file;
            dialog = new PropertyDialog(win, VgmImportTitle, 400);
            dialog.Properties.AddNumericUpDown(PatternLength.Colon, 256, 40, Pattern.MaxLength, 1);  // 0
            dialog.Properties.AddNumericUpDown(FramesToSkip.Colon, 0, 0, 100, 1);  // 1
            dialog.Properties.AddCheckBox(AdjustClock.Colon, true);        // 2
            dialog.Properties.AddCheckBox(ReverseDPCMBitsLabel.Colon, false);          // 3
            dialog.Properties.AddCheckBox(PreserveDPCMPaddingByte.Colon, false); // 4
            dialog.Properties.AddCheckBox(Ym2149AsEPSM.Colon, false); // 5
            dialog.Properties.Build();
        }

        public void ShowDialogAsync(FamiStudioWindow parent, Action<Project> action)
        {
            if (dialog != null)
            {
                // This is only ran in desktop and this isnt really async, so its ok.
                dialog.ShowDialogAsync((r) =>
                {
                    if (r == DialogResult.OK)
                    { 
                        var patternLen          = dialog.Properties.GetPropertyValue<int>(0);
                        var skipFrames          = dialog.Properties.GetPropertyValue<int>(1);
                        var adjustClock         = dialog.Properties.GetPropertyValue<bool>(2);
                        var reverseDpcmBits     = dialog.Properties.GetPropertyValue<bool>(3);
                        var preserveDpcmPadding = dialog.Properties.GetPropertyValue<bool>(4);
                        var ym2149AsEpsm        = dialog.Properties.GetPropertyValue<bool>(5);

                        var project = new VgmFile().Load(filename, patternLen, skipFrames, adjustClock, reverseDpcmBits, preserveDpcmPadding, ym2149AsEpsm);
                        action(project);
                    }
                    else
                    {
                        action(null);
                    }
                });
            }
            else
            {
                action(null);
            }
        }
    }
}
