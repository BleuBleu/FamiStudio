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
            SongCleanup,
            ProjectCleanup,
            Max
        };

        readonly string[] ConfigSectionNames =
        {
            "Song Cleanup",
            "Project Cleanup",
            ""
        };

        private PropertyPage[] pages = new PropertyPage[(int)TransformOperation.Max];
        private MultiPropertyDialog dialog;
        private FamiStudio app;

        public unsafe TransformDialog(FamiStudio famistudio)
        {
            app = famistudio;
            dialog = new MultiPropertyDialog(600, 430);

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
                case TransformOperation.SongCleanup:
                    page.AddBoolean("Merge identical patterns:", true);                       // 0
                    page.AddBoolean("Delete empty patterns:", true);                          // 1
                    page.AddStringListMulti(null, GetSongNames(), null);                      // 2
                    break;
                case TransformOperation.ProjectCleanup:
                    page.AddBoolean("Merge identical instruments:", true);                    // 0
                    page.AddBoolean("Delete unused instruments:", true);                      // 1
                    page.AddBoolean("Unassign unused DPCM instrument keys:", true);           // 2
                    page.AddBoolean("Delete unassigned samples:", true);                      // 3
                    page.AddBoolean("Delete DPCM samples WAV source data:", false);           // 4
                    page.AddBoolean("Permanently apply all DPCM samples processing:", false); // 5
                    page.AddBoolean("Delete unused arpeggios:", true);                        // 6
                    page.PropertyChanged += ProjectCleanup_PropertyChanged;
                    break;
            }

            page.Build();
            pages[(int)section] = page;

            return page;
        }

        private void ProjectCleanup_PropertyChanged(PropertyPage props, int idx, object value)
        {
            // Applying processing implies deleting source data.
            if (idx == 5)
            {
                var applyProcessing = (bool)value;

                if (applyProcessing)
                    props.SetPropertyValue(4, true);

                props.SetPropertyEnabled(4, !applyProcessing);
            }
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

        private void SongCleanup()
        {
            var props = dialog.GetPropertyPage((int)TransformOperation.SongCleanup);
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(2));

            var mergeIdenticalPatterns    = props.GetPropertyValue<bool>(0);
            var deleteEmptyPatterns       = props.GetPropertyValue<bool>(1);

            if (songIds.Length > 0 && (mergeIdenticalPatterns || deleteEmptyPatterns))
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

                app.UndoRedoManager.EndTransaction();
            }
        }

        private void ProjectCleanup()
        {
            var props = dialog.GetPropertyPage((int)TransformOperation.ProjectCleanup);

            var mergeIdenticalInstruments = props.GetPropertyValue<bool>(0);
            var deleteUnusedInstruments   = props.GetPropertyValue<bool>(1);
            var unassignUnusedSamples     = props.GetPropertyValue<bool>(2);
            var deleteUnusedSamples       = props.GetPropertyValue<bool>(3);
            var deleteWavSourceData       = props.GetPropertyValue<bool>(4);
            var applyAllSamplesProcessing = props.GetPropertyValue<bool>(5);
            var deleteUnusedArpeggios     = props.GetPropertyValue<bool>(6);

            if (mergeIdenticalInstruments || deleteUnusedInstruments || unassignUnusedSamples || deleteUnusedSamples || deleteWavSourceData || deleteUnusedArpeggios)
            {
                app.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                if (mergeIdenticalInstruments)
                {
                    app.Project.MergeIdenticalInstruments();
                }

                if (deleteUnusedInstruments)
                {
                    app.Project.DeleteUnusedInstruments();
                }

                if (unassignUnusedSamples)
                {
                    app.Project.UnmapUnusedSamples();
                }

                if (deleteUnusedSamples)
                {
                    app.Project.DeleteUnmappedSamples();
                }

                if (applyAllSamplesProcessing)
                {
                    app.Project.PermanentlyApplyAllSamplesProcessing();
                }
                else if (deleteWavSourceData)
                {
                    app.Project.DeleteSampleWavSourceData();
                }

                if (deleteUnusedArpeggios)
                {
                    app.Project.DeleteUnusedArpeggios();
                }

                app.UndoRedoManager.EndTransaction();
            }
        }

        public DialogResult ShowDialog(FamiStudioForm parent)
        {
            var dialogResult = dialog.ShowDialog(parent);

            if (dialogResult == DialogResult.OK)
            {
                var operation = (TransformOperation)dialog.SelectedIndex;

                switch (operation)
                {
                    case TransformOperation.SongCleanup:
                        SongCleanup();
                        break;
                    case TransformOperation.ProjectCleanup:
                        ProjectCleanup();
                        break;
                }
            }

            return dialogResult;
        }
    }
}
