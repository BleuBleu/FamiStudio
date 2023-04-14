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
        LocalizedString SongLabel;
        LocalizedString DurationLabel;
        LocalizedString PatternLength;
        LocalizedString StartFrameLabel;
        LocalizedString RemoveIntroSilenceLabel;
        LocalizedString ReverseDPCMBitsLabel;
        LocalizedString PreserveDPCMPaddingByte;

        #endregion

        public VgmImportDialog(FamiStudioWindow win, string file, Action<Project> action)
        {
            /*Localization.Localize(this);

            filename = file;
            songNames[0] = "Vgm";

            if (songNames != null && songNames.Length > 0)
                {
                dialog = new PropertyDialog(win, "VgmImportTitle", 400);
                dialog.Properties.AddNumericUpDown(PatternLength.Colon, 256, 4, Pattern.MaxLength, 1);  // 0
                dialog.Properties.PropertyChanged += Properties_PropertyChanged;
                dialog.Properties.Build();
                UpdateSongDuration(0);
            }*/
            var project = new VgmFile().Load(file, 160);
            action(project);
        }

        private void UpdateSongDuration(int idx)
        {
            dialog.Properties.SetPropertyValue(1, songDurations[idx] <= 0 ? 120 : songDurations[idx]);
        }

        private void Properties_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (propIdx == 0)
            {
                var idx = dialog.Properties.GetSelectedIndex(propIdx);
                UpdateSongDuration(idx);
            }
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
