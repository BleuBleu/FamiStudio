using System;
using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
#if !FAMISTUDIO_ANDROID // DROIDTODO!
    class NsfImportDialog
    {
        private PropertyDialog dialog;
        private string[] songNames;
        private string filename;

        public NsfImportDialog(string file)
        {
            filename = file;
            songNames = NsfFile.GetSongNames(filename);

            if (songNames != null && songNames.Length > 0)
            {
                dialog = new PropertyDialog("NSF Import", 350);
                dialog.Properties.AddDropDownList("Song:", songNames, songNames[0]); // 0
                dialog.Properties.AddNumericUpDown("Duration (s):", 120, 1, 600);    // 1
                dialog.Properties.AddNumericUpDown("Pattern Length:", 256, 4, 256);  // 2
                dialog.Properties.AddNumericUpDown("Start frame:", 0, 0, 256);       // 3
                dialog.Properties.AddCheckBox("Remove intro silence:", true);        // 4
                dialog.Properties.AddCheckBox("Reverse DPCM bits:", false);          // 5
                dialog.Properties.AddCheckBox("Preserve DPCM padding byte:", false); // 6
                dialog.Properties.Build();
            }
        }

        public DialogResult ShowDialog(FamiStudioForm parent)
        {
            if (dialog != null)
            {
                return dialog.ShowDialog(parent);
            }
            else
            {
                return DialogResult.Cancel;
            }
        }

        public int SongIndex => Array.IndexOf(songNames, dialog.Properties.GetPropertyValue<string>(0));
        public int Duration => dialog.Properties.GetPropertyValue<int>(1);
        public int PatternLength => dialog.Properties.GetPropertyValue<int>(2);
        public int StartFrame => dialog.Properties.GetPropertyValue<int>(3);
        public bool RemoveIntroSilence => dialog.Properties.GetPropertyValue<bool>(4);
        public bool ReverseDpcmBits => dialog.Properties.GetPropertyValue<bool>(5);
        public bool PreserveDpcmPadding => dialog.Properties.GetPropertyValue<bool>(6);
    }
#endif
}
