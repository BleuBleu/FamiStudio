using System;
using System.Collections.Generic;

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

        #region Localization

        LocalizedString[] ConfigSectionNames = new LocalizedString[(int)TransformOperation.Max];

        // Title
        LocalizedString Title;

        // Song tooltips
        LocalizedString MergePatternsTooltip;
        LocalizedString DeleteEmptyPatternsTooltip;
        LocalizedString AdjustMaximumNoteLengthsTooltip;
        LocalizedString SongsTooltip;

        // Song labels
        LocalizedString MergePatternsLabel;
        LocalizedString DeleteEmptyPatternsLabel;
        LocalizedString AdjustMaximumNoteLengthsLabel;
        LocalizedString SongsLabel;

        // Project tooltips
        LocalizedString DeleteUnusedInstrumentsTooltip;
        LocalizedString MergeIdenticalInstrumentsTooltip;
        LocalizedString UnassignUnusedSamplesTooltip;
        LocalizedString DeleteUnassignedSamplesTooltip;
        LocalizedString PermanentlyApplyProcessingTooltip;
        LocalizedString DiscardN163FdsResampleWavTooltip;
        LocalizedString DeleteUnusedArpeggiosTooltip;

        // Project labels
        LocalizedString DeleteUnusedInstrumentsLabel;
        LocalizedString MergeIdenticalInstrumentsLabel;
        LocalizedString UnassignUnusedSamplesLabel;
        LocalizedString DeleteUnassignedSamplesLabel;
        LocalizedString PermanentlyApplyProcessingLabel;
        LocalizedString DiscardN163FdsResampleWavLabel;
        LocalizedString DeleteUnusedArpeggiosLabel;

        #endregion

        public delegate void EmptyDelegate();
        public event EmptyDelegate CleaningUp;

        private PropertyPage[] pages = new PropertyPage[(int)TransformOperation.Max];
        private MultiPropertyDialog dialog;
        private FamiStudio app;

        public unsafe TransformDialog(FamiStudioWindow win)
        {
            Localization.Localize(this);

            app = win.FamiStudio;
            dialog = new MultiPropertyDialog(win, Title, 550);

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
                    page.AddCheckBox(MergePatternsLabel.Colon, true, MergePatternsTooltip); // 0
                    page.AddCheckBox(DeleteEmptyPatternsLabel.Colon, true, DeleteEmptyPatternsTooltip); // 1
                    page.AddCheckBox(AdjustMaximumNoteLengthsLabel.Colon, true, AdjustMaximumNoteLengthsTooltip); // 2
                    page.AddCheckBoxList(Platform.IsMobile ? SongsLabel.Colon : null , GetSongNames(), null, SongsTooltip) ; // 3
                    break;
                case TransformOperation.ProjectCleanup:
                    page.AddCheckBox(DeleteUnusedInstrumentsLabel.Colon, true, DeleteUnusedInstrumentsTooltip); // 0
                    page.AddCheckBox(MergeIdenticalInstrumentsLabel.Colon, true, MergeIdenticalInstrumentsTooltip); // 1
                    page.AddCheckBox(UnassignUnusedSamplesLabel.Colon, true, UnassignUnusedSamplesTooltip); // 2
                    page.AddCheckBox(DeleteUnassignedSamplesLabel.Colon, true, DeleteUnassignedSamplesTooltip); // 3
                    page.AddCheckBox(PermanentlyApplyProcessingLabel.Colon, false, PermanentlyApplyProcessingTooltip); // 4
                    page.AddCheckBox(DiscardN163FdsResampleWavLabel.Colon, false, DiscardN163FdsResampleWavTooltip); // 5
                    page.AddCheckBox(DeleteUnusedArpeggiosLabel.Colon, true, DeleteUnusedArpeggiosTooltip); // 6
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
            var discardN163FdsResampling  = props.GetPropertyValue<bool>(5);
            var deleteUnusedArpeggios     = props.GetPropertyValue<bool>(6);

            if (mergeIdenticalInstruments || deleteUnusedInstruments || unassignUnusedSamples || deleteUnusedSamples || applyAllSamplesProcessing || discardN163FdsResampling || deleteUnusedArpeggios)
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

                if (discardN163FdsResampling)
                {
                    app.Project.DeleteAllN163FdsResampleWavData();
                }
                
                if (deleteUnusedArpeggios)
                {
                    app.Project.DeleteUnusedArpeggios();
                }

                // The selected instrument/arpeggio may get deleted.
                if (!app.Project.InstrumentExists(app.SelectedInstrument))
                    app.SelectedInstrument = app.Project.Instruments.Count > 0 ? app.Project.Instruments[0] : null;
                if (!app.Project.ArpeggioExists(app.SelectedArpeggio))
                    app.SelectedArpeggio = app.Project.Arpeggios.Count > 0 ? app.Project.Arpeggios[0] : null; ;

                app.UndoRedoManager.EndTransaction();
            }
        }

        public void ShowDialogAsync(Action<DialogResult> callback)
        {
            dialog.ShowDialogAsync((r) =>
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
