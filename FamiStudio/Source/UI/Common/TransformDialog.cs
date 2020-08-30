using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamiStudio
{
    class TransformDialog
    {
        enum TransformOperation
        {
            Cleanup,
            Max
        };

        readonly string[] ConfigSectionNames =
        {
            "Cleanup",
            ""
        };

        private PropertyPage[] pages = new PropertyPage[(int)TransformOperation.Max];
        private MultiPropertyDialog dialog;
        private FamiStudio app;

        public unsafe TransformDialog(Rectangle mainWinRect, FamiStudio famistudio)
        {
            int width = 450;
            int height = 400;
            int x = mainWinRect.Left + (mainWinRect.Width - width) / 2;
            int y = mainWinRect.Top + (mainWinRect.Height - height) / 2;

#if FAMISTUDIO_LINUX
            height += 30;
#endif

            app = famistudio;
            dialog = new MultiPropertyDialog(x, y, width, height);

            for (int i = 0; i < (int)TransformOperation.Max; i++)
            {
                var section = (TransformOperation)i;
                var page = dialog.AddPropertyPage(ConfigSectionNames[i], "Clean");
                CreatePropertyPage(page, section);
            }
        }


        private string[] GetSongNames()
        {
            var names = new string[app.Project.Songs.Count];
            for (var i = 0; i < app.Project.Songs.Count; i++)
                names[i] = app.Project.Songs[i].Name;
            return names;
        }

        private PropertyPage CreatePropertyPage(PropertyPage page, TransformOperation section)
        {
            switch (section)
            {
                case TransformOperation.Cleanup:
                    page.AddBoolean("Merge identical patterns:", true);    // 0
                    page.AddBoolean("Delete empty patterns:", true);       // 1
                    page.AddBoolean("Merge identical instruments:", true); // 2
                    page.AddBoolean("Delete unused instruments:", true);   // 3
                    page.AddBoolean("Delete unused samples:", true);       // 4
                    page.AddBoolean("Delete unused arpeggios:", true);     // 5
                    page.AddStringListMulti(null, GetSongNames(), null);   // 6
                    break;
            }

            page.Build();
            pages[(int)section] = page;

            return page;
        }

        private int[] GetSongIds(bool[] selectedSongs)
        {
            var songIds = new List<int>();

            for (int i = 0; i < selectedSongs.Length; i++)
            {
                if (selectedSongs[i])
                    songIds.Add(app.Project.Songs[i].Id);
            }

            return songIds.ToArray();
        }

        private void Cleanup()
        {
            var props = dialog.GetPropertyPage((int)TransformOperation.Cleanup);
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(6));

            var mergeIdenticalPatterns    = props.GetPropertyValue<bool>(0);
            var deleteEmptyPatterns       = props.GetPropertyValue<bool>(1);
            var mergeIdenticalInstruments = props.GetPropertyValue<bool>(2);
            var deleteUnusedInstruments   = props.GetPropertyValue<bool>(3);
            var deleteUnusedSamples       = props.GetPropertyValue<bool>(4);
            var deleteUnusedArpeggios     = props.GetPropertyValue<bool>(5);

            if (songIds.Length > 0 && (mergeIdenticalPatterns || deleteEmptyPatterns || mergeIdenticalInstruments || deleteUnusedInstruments || deleteUnusedSamples || deleteUnusedArpeggios))
            {
                app.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                foreach (var songId in songIds)
                {
                    app.Project.GetSong(songId).DeleteNotesPastMaxInstanceLength();
                }

                if (mergeIdenticalPatterns)
                {
                    foreach (var songId in songIds)
                    {
                        var song = app.Project.GetSong(songId);
                        song.MergeIdenticalPatterns();
                    }
                }

                if (deleteEmptyPatterns)
                {
                    foreach (var songId in songIds)
                    {
                        var song = app.Project.GetSong(songId);
                        song.DeleteEmptyPatterns();
                    }
                }

                if (mergeIdenticalInstruments)
                {
                    app.Project.MergeIdenticalInstruments();
                }

                if (deleteUnusedInstruments)
                {
                    app.Project.DeleteUnusedInstruments();
                }

                if (deleteUnusedSamples)
                {
                    app.Project.DeleteUnusedSamples();
                }

                if (deleteUnusedArpeggios)
                {
                    app.Project.DeleteUnusedArpeggios();
                }

                app.UndoRedoManager.EndTransaction();
            }
        }

        public DialogResult ShowDialog()
        {
            var dialogResult = dialog.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                var operation = (TransformOperation)dialog.SelectedIndex;

                switch (operation)
                {
                    case TransformOperation.Cleanup: Cleanup(); break;
                }
            }

            return dialogResult;
        }
    }
}
