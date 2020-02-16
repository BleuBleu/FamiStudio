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

        public NsfImportDialog(string file, Rectangle mainWinRect)
        {
            int width  = 350;
            int height = 300;
            int x = mainWinRect.Left + (mainWinRect.Width  - width)  / 2;
            int y = mainWinRect.Top  + (mainWinRect.Height - height) / 2;

            filename = file;
            songNames = NsfFile.GetSongNames(filename);

            if (songNames != null && songNames.Length > 0)
            {
                dialog = new PropertyDialog(x, y, width, height);
                dialog.Properties.AddStringList("Song:", songNames, songNames[0]); // 0
                dialog.Properties.AddIntegerRange("Duration (s):", 120, 1, 600);   // 1
                dialog.Properties.AddIntegerRange("Pattern Length:", 256, 4, 256); // 2
                dialog.Properties.AddIntegerRange("Start frame:", 0, 0, 256);      // 3
                dialog.Properties.AddBoolean("Remove intro silence:", true);       // 4
                dialog.Properties.Build();
            }
        }

        public DialogResult ShowDialog()
        {
            if (dialog != null)
            {
                return dialog.ShowDialog();
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
    }
}
