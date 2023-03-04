using System;

namespace FamiStudio
{
    class NsfImportDialog
    {
        private PropertyDialog dialog;
        private string[] songNames;
        private string filename;

        public NsfImportDialog(FamiStudioWindow win, string file)
        {
            filename = file;
            songNames = NsfFile.GetSongNames(filename);

            if (songNames != null && songNames.Length > 0)
            {
                dialog = new PropertyDialog(win, "NSF Import", 350);
                dialog.Properties.AddDropDownList("Song:", songNames, songNames[0]); // 0
                dialog.Properties.AddNumericUpDown("Duration (s):", 120, 1, 600);    // 1
                dialog.Properties.AddNumericUpDown("Pattern Length:", 256, 4, Pattern.MaxLength);  // 2
                dialog.Properties.AddNumericUpDown("Start frame:", 0, 0, 256);       // 3
                dialog.Properties.AddCheckBox("Remove intro silence:", true);        // 4
                dialog.Properties.AddCheckBox("Reverse DPCM bits:", false);          // 5
                dialog.Properties.AddCheckBox("Preserve DPCM padding byte:", false); // 6
                dialog.Properties.Build();
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
                        var removeIntro         = dialog.Properties.GetPropertyValue<bool>(4); 
                        var reverseDpcmBits     = dialog.Properties.GetPropertyValue<bool>(5); 
                        var preserveDpcmPadding = dialog.Properties.GetPropertyValue<bool>(6);

                        var project = new NsfFile().Load(filename, songIndex, duration, patternLen, startFrame, removeIntro, reverseDpcmBits, preserveDpcmPadding);
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
