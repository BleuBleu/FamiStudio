using System;
using System.Drawing;
using System.Windows.Forms;

namespace FamiStudio
{
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
                dialog = new PropertyDialog(350);
                dialog.Properties.AddStringList("Song:", songNames, songNames[0]); // 0
                dialog.Properties.AddIntegerRange("Duration (s):", 120, 1, 600);   // 1
                dialog.Properties.AddIntegerRange("Pattern Length:", 256, 4, 256); // 2
                dialog.Properties.AddIntegerRange("Start frame:", 0, 0, 256);      // 3
                dialog.Properties.AddBoolean("Remove intro silence:", true);       // 4
                dialog.Properties.AddBoolean("Reverse DPCM bits:", false);         // 5
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
    }
}
