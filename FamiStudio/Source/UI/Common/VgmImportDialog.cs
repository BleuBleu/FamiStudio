using System;

namespace FamiStudio
{
    class VgmImportDialog
    {
        private PropertyDialog dialog;
        private string[] songNames;
        private int[]    songDurations;
        private string filename;

        #region Localization

        LocalizedString VgmImportTitle;
        LocalizedString PatternLength;

        #endregion

        public VgmImportDialog(FamiStudioWindow win, string file, Action<Project> action)
        {
            Localization.Localize(this);

            filename = file;
            dialog = new PropertyDialog(win, VgmImportTitle, 400);
            dialog.Properties.AddNumericUpDown(PatternLength.Colon, 256, 40, Pattern.MaxLength, 1);  // 0
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

                        var project = new VgmFile().Load(filename, patternLen);
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
