using System;

namespace FamiStudio
{
    class NsfImportDialog
    {
        private PropertyDialog dialog;
        private string[] songNames;
        private int[]    songDurations;
        private string filename;

        #region Localization

        LocalizedString NsfImportTitle;
        LocalizedString SongLabel;
        LocalizedString DurationLabel;
        LocalizedString PatternLength;
        LocalizedString StartFrameLabel;
        LocalizedString RemoveIntroSilenceLabel;
        LocalizedString ReverseDPCMBitsLabel;
        LocalizedString PreserveDPCMPaddingByte;
        LocalizedString TuningLabel;

        #endregion

        public NsfImportDialog(FamiStudioWindow win, string file)
        {
            Localization.Localize(this);

            filename = file;
            songNames = NsfFile.GetSongNamesAndDurations(filename, out songDurations);

            if (songNames != null && songNames.Length > 0)
            {
                dialog = new PropertyDialog(win, NsfImportTitle, 400);
                dialog.Properties.AddDropDownList(SongLabel.Colon, songNames, songNames[0]);            // 0
                dialog.Properties.AddNumericUpDown(DurationLabel.Colon, 120, 1, 600, 1);                // 1
                dialog.Properties.AddNumericUpDown(PatternLength.Colon, 256, 4, Pattern.MaxLength, 1);  // 2
                dialog.Properties.AddNumericUpDown(StartFrameLabel.Colon, 0, 0, 256, 1);                // 3
                dialog.Properties.AddNumericUpDown(TuningLabel.Colon, 440, 300, 580, 1);                // 4
                dialog.Properties.AddCheckBox(RemoveIntroSilenceLabel.Colon, true);                     // 5
                dialog.Properties.AddCheckBox(ReverseDPCMBitsLabel.Colon, false);                       // 6
                dialog.Properties.AddCheckBox(PreserveDPCMPaddingByte.Colon, false);                    // 7
                dialog.Properties.PropertyChanged += Properties_PropertyChanged;
                dialog.Properties.Build();
                UpdateSongDuration(0);
            }
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
                        var songIndex           = Array.IndexOf(songNames, dialog.Properties.GetPropertyValue<string>(0)); ;
                        var duration            = dialog.Properties.GetPropertyValue<int>(1);
                        var patternLen          = dialog.Properties.GetPropertyValue<int>(2);
                        var startFrame          = dialog.Properties.GetPropertyValue<int>(3);
                        var tuning              = dialog.Properties.GetPropertyValue<int>(4);
                        var removeIntro         = dialog.Properties.GetPropertyValue<bool>(5);
                        var reverseDpcmBits     = dialog.Properties.GetPropertyValue<bool>(6);
                        var preserveDpcmPadding = dialog.Properties.GetPropertyValue<bool>(7);

                        var project = new NsfFile().Load(filename, songIndex, duration, patternLen, startFrame, removeIntro, reverseDpcmBits, preserveDpcmPadding, tuning);
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
