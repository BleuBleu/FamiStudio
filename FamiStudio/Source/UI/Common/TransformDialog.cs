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

        public delegate void EmptyDelegate();
        public event EmptyDelegate CleaningUp;

        private PropertyPage[] pages = new PropertyPage[(int)TransformOperation.Max];
        private MultiPropertyDialog dialog;
        private FamiStudio app;

        public unsafe TransformDialog(FamiStudio famistudio)
        {
            app = famistudio;
            dialog = new MultiPropertyDialog("Transform Songs", 550, 500);

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
                    page.AddCheckBox("Merge identical patterns:", true);                       // 0
                    page.AddCheckBox("Delete empty patterns:", true);                          // 1
                    page.AddCheckBox("Adjust maximum note lengths:", true);                    // 2
                    page.AddCheckBoxList(null, GetSongNames(), null);                          // 3
                    break;
                case TransformOperation.ProjectCleanup:
                    page.AddCheckBox("Delete unused instruments:", true);                      // 0
                    page.AddCheckBox("Merge identical instruments:", true);                    // 1
                    page.AddCheckBox("Unassign unused DPCM instrument keys:", true);           // 2
                    page.AddCheckBox("Delete unassigned samples:", true);                      // 3
                    page.AddCheckBox("Permanently apply all DPCM samples processing:", false); // 4
                    page.AddCheckBox("Delete unused arpeggios:", true);                        // 5
                    page.PropertyChanged += ProjectCleanup_PropertyChanged;
                    break;
            }

            page.Build();
            pages[(int)section] = page;

            return page;
        }

        private void ProjectCleanup_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            // Applying processing implies deleting source data.
            if (propIdx == 5)
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
            var songIds = GetSongIds(props.GetPropertyValue<bool[]>(3));

            var mergeIdenticalPatterns = props.GetPropertyValue<bool>(0);
            var deleteEmptyPatterns    = props.GetPropertyValue<bool>(1);
            var reduceNoteLengths      = props.GetPropertyValue<bool>(2);

            if (songIds.Length > 0 && (mergeIdenticalPatterns || deleteEmptyPatterns || reduceNoteLengths))
            {
                app.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                CleaningUp?.Invoke();

                foreach (var songId in songIds)
                {
                    app.Project.GetSong(songId).DeleteNotesPastMaxInstanceLength();
                }

                if (reduceNoteLengths)
                {
                    foreach (var songId in songIds)
                        app.Project.GetSong(songId).SetNoteDurationToMaximumLength();
                }

                if (mergeIdenticalPatterns)
                {
                    foreach (var songId in songIds)
                        app.Project.GetSong(songId).MergeIdenticalPatterns();
                }

                if (deleteEmptyPatterns)
                {
                    foreach (var songId in songIds)
                        app.Project.GetSong(songId).DeleteEmptyPatterns();
                }

                app.UndoRedoManager.EndTransaction();
            }
        }

        private void ProjectCleanup()
        {
            var props = dialog.GetPropertyPage((int)TransformOperation.ProjectCleanup);

            var deleteUnusedInstruments   = props.GetPropertyValue<bool>(0);
            var mergeIdenticalInstruments = props.GetPropertyValue<bool>(1);
            var unassignUnusedSamples     = props.GetPropertyValue<bool>(2);
            var deleteUnusedSamples       = props.GetPropertyValue<bool>(3);
            var applyAllSamplesProcessing = props.GetPropertyValue<bool>(4);
            var deleteUnusedArpeggios     = props.GetPropertyValue<bool>(5);

            if (mergeIdenticalInstruments || deleteUnusedInstruments || unassignUnusedSamples || deleteUnusedSamples || applyAllSamplesProcessing || deleteUnusedArpeggios)
            {
                app.UndoRedoManager.BeginTransaction(TransactionScope.Project);

                CleaningUp?.Invoke();

                if (deleteUnusedInstruments)
                {
                    app.Project.DeleteUnusedInstruments();
                }

                if (mergeIdenticalInstruments)
                {
                    app.Project.MergeIdenticalInstruments();
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
                
                if (deleteUnusedArpeggios)
                {
                    app.Project.DeleteUnusedArpeggios();
                }

                app.UndoRedoManager.EndTransaction();
            }
        }

        public void ShowDialog(FamiStudioForm parent, Action<DialogResult> callback)
        {
            dialog.ShowDialog(parent, (r) =>
            {
                if (r == DialogResult.OK)
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

                callback(r);
            });
        }
    }
}
