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
            Split,
            Tempo,
            Max
        };

        readonly string[] ConfigSectionNames =
        {
            "Cleanup",
            "Split",
            "Tempo",
            ""
        };

        private PropertyPage[] pages = new PropertyPage[(int)TransformOperation.Max];
        private MultiPropertyDialog dialog;
        private FamiStudio app;

        public unsafe TransformDialog(Rectangle mainWinRect, FamiStudio famistudio)
        {
            int width = 450;
            int height = 300;
            int x = mainWinRect.Left + (mainWinRect.Width - width) / 2;
            int y = mainWinRect.Top + (mainWinRect.Height - height) / 2;

            app = famistudio;
            dialog = new MultiPropertyDialog(x, y, width, height);

            for (int i = 0; i < (int)TransformOperation.Max; i++)
            {
                var section = (TransformOperation)i;
                var page = dialog.AddPropertyPage(ConfigSectionNames[i], "ExportNsf"); // MATTT
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
                    page.AddStringListMulti(null, GetSongNames(), null);   // 5
                    break;
                case TransformOperation.Split:
                    page.AddIntegerRange("Split Patterns in :", 2, 2, 4);  // 0
                    page.AddStringListMulti(null, GetSongNames(), null);   // 5
                    break;
                case TransformOperation.Tempo:
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
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(5));

            var mergeIdenticalPatterns    = props.GetPropertyValue<bool>(0);
            var deleteEmptyPatterns       = props.GetPropertyValue<bool>(1);
            var mergeIdenticalInstruments = props.GetPropertyValue<bool>(2);
            var deleteUnusedInstruments   = props.GetPropertyValue<bool>(3);
            var deleteUnusedSamples       = props.GetPropertyValue<bool>(4);

            if (songIds.Length > 0 && (mergeIdenticalPatterns || deleteEmptyPatterns || mergeIdenticalInstruments || deleteUnusedInstruments || deleteUnusedSamples))
            {
                app.UndoRedoManager.BeginTransaction(TransactionScope.Project);

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
                        song.RemoveEmptyPatterns();
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

                app.UndoRedoManager.EndTransaction();
            }
        }

        private void Split()
        {
            var props = dialog.GetPropertyPage((int)TransformOperation.Split);
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(1));

            if (songIds.Length > 0)
            {
                var factor = props.GetPropertyValue<int>(0);

                app.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                foreach (var songId in songIds)
                {
                    var song = app.Project.GetSong(songId);
                    song.Split(factor);
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
                    case TransformOperation.Split:   Split();   break;
                }
            }

            return dialogResult;
        }
    }
}
