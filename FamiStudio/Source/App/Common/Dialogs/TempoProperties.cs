using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FamiStudio
{
    class TempoProperties
    {
        private PropertyPage props;
        private Song song;

        private int patternIdx    = -1;
        private int minPatternIdx = -1;
        private int maxPatternIdx = -1;

        private int firstPropIdx             = -1;
        private int famitrackerTempoPropIdx  = -1;
        private int famitrackerSpeedPropIdx  = -1;
        private int notesPerBeatPropIdx      = -1;
        private int notesPerPatternPropIdx   = -1;
        private int bpmLabelPropIdx          = -1;
        private int famistudioBpmPropIdx     = -1;
        private int framesPerNotePropIdx     = -1;
        private int groovePropIdx            = -1;
        private int groovePadPropIdx         = -1;

        private int originalNoteLength;

        private TempoInfo[] tempoList;
        private string[]    tempoStrings;
        private string[]    grooveStrings;

        #region Localization

        // Tooltips
        private LocalizedString TempoTooltip;
        private LocalizedString SpeedTooltip;
        private LocalizedString BPMTooltip;
        private LocalizedString FramesPerNoteTooltip;
        private LocalizedString NotesPerPatternTooltip;
        private LocalizedString NotesPerBeatTooltip;
        private LocalizedString GrooveTooltip;
        private LocalizedString GroovePaddingTooltip;

        // Labels
        private LocalizedString TempoLabel;
        private LocalizedString SpeedLabel;
        private LocalizedString BPMLabel;
        private LocalizedString NotesPerPatternLabel;
        private LocalizedString NotesPerBeatLabel;
        private LocalizedString FramesPerNoteLabel;
        private LocalizedString GrooveLabel;
        private LocalizedString GroovePaddingLabel;

        // Warnings
        private LocalizedString GrooveIdealWarning;
        private LocalizedString GrooveAlignedWarning;
        private LocalizedString GrooveUnalignedWarning;
        private LocalizedString NotesPerBeatBad;
        private LocalizedString NotesPerBeatGood;
        private LocalizedString FamiTrackerSpeed1Warning;
        private LocalizedString FamiTrackerTempo150BadWarning;
        private LocalizedString FamiTrackerTempo150GoodWarning;
        private LocalizedString FamiTrackerPatternLongerWarning;
        private LocalizedString FamiTrackerPatternLengthWarning;

        // Tempo conversion dialog
        private LocalizedString TempoConversionTitle;
        private LocalizedString ConvertMobileLabel;
        private LocalizedString ConvertResizeNotes;
        private LocalizedString ConvertLeaveNotes;
        private LocalizedString DuplicateLabel;

        // Notifications
        private LocalizedString DuplicateNotificationSong;
        private LocalizedString DuplicateNotificationCustom;

        #endregion

        public TempoProperties(PropertyPage props, Song song, int patternIdx = -1, int minPatternIdx = -1, int maxPatternIdx = -1)
        {
            Localization.Localize(this);

            this.song = song;
            this.props = props;
            this.patternIdx = patternIdx;
            this.minPatternIdx = minPatternIdx;
            this.maxPatternIdx = maxPatternIdx;

            props.PropertyChanged += Props_PropertyChanged;
        }

        public void AddProperties()
        {
            firstPropIdx = props.PropertyCount;

            if (song.UsesFamiTrackerTempo)
            {
                if (patternIdx < 0)
                {
                    famitrackerTempoPropIdx = props.AddNumericUpDown(TempoLabel.Colon, song.FamitrackerTempo, 32, 255, 1, 150, TempoTooltip); // 0
                    famitrackerSpeedPropIdx = props.AddNumericUpDown(SpeedLabel.Colon, song.FamitrackerSpeed, 1, 31, 1, 6, SpeedTooltip); // 1
                }
                
                var notesPerBeat    = patternIdx < 0 ? song.BeatLength    : song.GetPatternBeatLength(patternIdx);
                var notesPerPattern = patternIdx < 0 ? song.PatternLength : song.GetPatternLength(patternIdx);
                var bpm = Song.ComputeFamiTrackerBPM(song.Project.PalMode, song.FamitrackerSpeed, song.FamitrackerTempo, notesPerBeat);

                notesPerPatternPropIdx = props.AddNumericUpDown(NotesPerPatternLabel.Colon, notesPerPattern, 1, Pattern.MaxLength, 1, 16, NotesPerPatternTooltip); // 3
                notesPerBeatPropIdx = props.AddNumericUpDown(NotesPerBeatLabel.Colon, notesPerBeat, 1, 256, 1, 4, NotesPerBeatTooltip); // 2
                bpmLabelPropIdx = props.AddLabel(BPMLabel.Colon, bpm.ToString("n1"), false, BPMTooltip); // 4

                props.ShowWarnings = true;

                UpdateWarnings();
            }
            else
            {                                                              
                var noteLength      = (patternIdx < 0 ? song.NoteLength    : song.GetPatternNoteLength(patternIdx));
                var notesPerBeat    = (patternIdx < 0 ? song.BeatLength    : song.GetPatternBeatLength(patternIdx));
                var notesPerPattern = (patternIdx < 0 ? song.PatternLength : song.GetPatternLength(patternIdx));
                var groove          = (patternIdx < 0 ? song.Groove        : song.GetPatternGroove(patternIdx));

                tempoList = FamiStudioTempoUtils.GetAvailableTempos(song.Project.PalMode, notesPerBeat / noteLength);
                var tempoIndex = FamiStudioTempoUtils.FindTempoFromGroove(tempoList, groove);
                Debug.Assert(tempoIndex >= 0);
                tempoStrings = tempoList.Select(t => t.bpm.ToString("n1") + (t.groove.Length == 1 ? " *": "")).ToArray();

                var grooveList = FamiStudioTempoUtils.GetAvailableGrooves(tempoList[tempoIndex].groove);
                var grooveIndex = Array.FindIndex(grooveList, g => Utils.CompareArrays(g, groove) == 0);
                Debug.Assert(grooveIndex >= 0);
                grooveStrings = grooveList.Select(g => string.Join("-", g)).ToArray();

                famistudioBpmPropIdx   = props.AddDropDownList(BPMLabel.Colon, tempoStrings, tempoStrings[tempoIndex], BPMTooltip); // 0
                notesPerPatternPropIdx = props.AddNumericUpDown(NotesPerPatternLabel.Colon, notesPerPattern / noteLength, 1, Pattern.MaxLength / noteLength, 1, 16, NotesPerPatternTooltip); // 2
                notesPerBeatPropIdx    = props.AddNumericUpDown(NotesPerBeatLabel.Colon, notesPerBeat / noteLength, 1, 256, 1, 4, NotesPerBeatTooltip); // 1
                framesPerNotePropIdx   = props.AddLabel(FramesPerNoteLabel.Colon, noteLength.ToString(), false, FramesPerNoteTooltip); // 3

                props.ShowWarnings = true;
                props.BeginAdvancedProperties();
                groovePropIdx    = props.AddDropDownList(GrooveLabel.Colon, grooveStrings, grooveStrings[grooveIndex], GrooveTooltip); // 4
                groovePadPropIdx = props.AddDropDownList(GroovePaddingLabel.Colon, GroovePaddingType.Names, GroovePaddingType.Names[song.GroovePaddingMode], GroovePaddingTooltip); // 5

                originalNoteLength = noteLength;

                UpdateWarnings();
            }
        }

        private void Props_PropertyChanged(PropertyPage props, int propIdx, int rowIdx, int colIdx, object value)
        {
            if (song.UsesFamiTrackerTempo)
            {
                var tempo = song.FamitrackerTempo;
                var speed = song.FamitrackerSpeed;

                if (propIdx == famitrackerTempoPropIdx ||
                    propIdx == famitrackerSpeedPropIdx)
                {
                    tempo = props.GetPropertyValue<int>(famitrackerTempoPropIdx);
                    speed = props.GetPropertyValue<int>(famitrackerSpeedPropIdx);
                }

                var beatLength = props.GetPropertyValue<int>(notesPerBeatPropIdx);

                props.SetLabelText(bpmLabelPropIdx, Song.ComputeFamiTrackerBPM(song.Project.PalMode, speed, tempo, beatLength).ToString("n1"));
            }
            else
            {
                var notesPerBeat = props.GetPropertyValue<int>(notesPerBeatPropIdx);

                // Changing the number of notes in a beat will affect the list of available BPMs.
                if (propIdx == notesPerBeatPropIdx)
                {
                    tempoList = FamiStudioTempoUtils.GetAvailableTempos(song.Project.PalMode, notesPerBeat);
                    tempoStrings = tempoList.Select(t => t.bpm.ToString("n1") + (t.groove.Length == 1 ? " *" : "")).ToArray();
                    props.UpdateDropDownListItems(famistudioBpmPropIdx, tempoStrings);
                }

                // Changing the BPM affects the grooves and note length.
                if (propIdx == famistudioBpmPropIdx ||
                    propIdx == notesPerBeatPropIdx)
                {
                    var tempoIndex    = Array.IndexOf(tempoStrings, props.GetPropertyValue<string>(famistudioBpmPropIdx));
                    var tempoInfo     = tempoList[tempoIndex];
                    var framesPerNote = Utils.Min(tempoInfo.groove);

                    props.UpdateIntegerRange(notesPerPatternPropIdx, 1, Pattern.MaxLength / framesPerNote);

                    var grooveList = FamiStudioTempoUtils.GetAvailableGrooves(tempoInfo.groove);
                    grooveStrings = grooveList.Select(g => string.Join("-", g)).ToArray();

                    props.UpdateDropDownListItems(groovePropIdx, grooveStrings);
                    props.SetLabelText(framesPerNotePropIdx, framesPerNote.ToString());
                }
            }

            UpdateWarnings();
        }

        private void UpdateWarnings()
        {
            var numFramesPerPattern = 0;

            if (song.UsesFamiStudioTempo)
            {
                var tempoIndex = Array.IndexOf(tempoStrings, props.GetPropertyValue<string>(famistudioBpmPropIdx));
                var tempoInfo = tempoList[tempoIndex];
                var notesPerBeat = props.GetPropertyValue<int>(notesPerBeatPropIdx);
                var notesPerPattern = props.GetPropertyValue<int>(notesPerPatternPropIdx);

                if (tempoInfo.groove.Length == 1)
                {
                    props.SetPropertyWarning(famistudioBpmPropIdx, CommentType.Good, GrooveIdealWarning);
                }
                else if ((tempoInfo.groove.Length % notesPerBeat) == 0 ||
                         (notesPerBeat % tempoInfo.groove.Length) == 0)
                {
                    props.SetPropertyWarning(famistudioBpmPropIdx, CommentType.Warning, GrooveAlignedWarning);
                }
                else
                {
                    props.SetPropertyWarning(famistudioBpmPropIdx, CommentType.Error, GrooveUnalignedWarning);
                }

                if (notesPerBeat != 4)
                {
                    props.SetPropertyWarning(notesPerBeatPropIdx, CommentType.Error, NotesPerBeatBad);
                }
                else
                {
                    props.SetPropertyWarning(notesPerBeatPropIdx, CommentType.Good, NotesPerBeatGood);
                }

                var groovePadMode = GroovePaddingType.GetValueForName(props.GetPropertyValue<string>(groovePadPropIdx));
                numFramesPerPattern  = FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(notesPerPattern * Utils.Min(tempoInfo.groove), tempoInfo.groove, groovePadMode);
            }
            else if (famitrackerSpeedPropIdx >= 0)
            {
                var speed = props.GetPropertyValue<int>(famitrackerSpeedPropIdx);
                var tempo = props.GetPropertyValue<int>(famitrackerTempoPropIdx);

                if (speed == 1)
                {
                    props.SetPropertyWarning(famitrackerSpeedPropIdx, CommentType.Warning, FamiTrackerSpeed1Warning);
                }
                else
                {
                    props.SetPropertyWarning(famitrackerSpeedPropIdx, CommentType.Good, "");
                }

                if (tempo != 150)
                {
                    props.SetPropertyWarning(famitrackerTempoPropIdx, CommentType.Warning, FamiTrackerTempo150BadWarning);
                }
                else
                {
                    props.SetPropertyWarning(famitrackerTempoPropIdx, CommentType.Good, FamiTrackerTempo150GoodWarning);
                }
            }

            if (patternIdx >= 0 && numFramesPerPattern > song.PatternLength)
            {
                props.SetPropertyWarning(notesPerPatternPropIdx, CommentType.Warning, FamiTrackerPatternLongerWarning);
            }
            else if (numFramesPerPattern >= 256)
            {
                props.SetPropertyWarning(notesPerPatternPropIdx, CommentType.Warning, FamiTrackerPatternLengthWarning);
            }
            else
            {
                props.SetPropertyWarning(notesPerPatternPropIdx, CommentType.Good, "");
            }
        }

        public void EnableProperties(bool enabled)
        {
            for (var i = firstPropIdx; i < props.PropertyCount; i++)
                props.SetPropertyEnabled(i, enabled);
        }

        private void ShowConvertTempoDialogAsync(FamiStudioWindow win, bool conversionNeeded, Action<bool, bool> callback)
        {
            if (conversionNeeded)
            {
                var messageDlg = new PropertyDialog(win, TempoConversionTitle, 400, true, false);
                messageDlg.Properties.AddRadioButton(ConvertMobileLabel.Colon, ConvertResizeNotes, true, true, PropertyFlags.MultiLineLabel); // 0
                messageDlg.Properties.AddRadioButton(null, ConvertLeaveNotes, false, true); // 1
                messageDlg.Properties.AddLabelCheckBox(DuplicateLabel, true); // 2
                messageDlg.Properties.Build();
                messageDlg.ShowDialogAsync((r) =>
                {
                    callback(
                        messageDlg.Properties.GetPropertyValue<bool>(0),
                        messageDlg.Properties.GetPropertyValue<bool>(2));
                });
            }
            else
            {
                callback(false, false);
            }
        }

        private void FinishApply(Action callback)
        {
            song.DeleteNotesPastMaxInstanceLength();
            song.InvalidateCumulativePatternCache();
            song.Project.ValidateIntegrity();
            callback();
        }

        public void ApplyAsync(FamiStudioWindow win, bool custom, Action callback)
        {
            if (song.UsesFamiTrackerTempo)
            {
                if (patternIdx == -1)
                {
                    if (famitrackerTempoPropIdx >= 0)
                    {
                        song.FamitrackerTempo = props.GetPropertyValue<int>(famitrackerTempoPropIdx);
                        song.FamitrackerSpeed = props.GetPropertyValue<int>(famitrackerSpeedPropIdx);
                    }

                    song.SetBeatLength(props.GetPropertyValue<int>(notesPerBeatPropIdx));
                    song.SetDefaultPatternLength(props.GetPropertyValue<int>(notesPerPatternPropIdx));
                }
                else
                {
                    for (int i = minPatternIdx; i <= maxPatternIdx; i++)
                    {
                        var beatLength    = props.GetPropertyValue<int>(notesPerBeatPropIdx);
                        var patternLength = props.GetPropertyValue<int>(notesPerPatternPropIdx);

                        if (custom)
                        {
                            song.SetPatternCustomSettings(i, patternLength, beatLength);
                        }
                        else
                        {
                            song.ClearPatternCustomSettings(i);
                        }
                    }
                }

                FinishApply(callback);
            }
            else
            {
                var tempoIndex    = Array.IndexOf(tempoStrings, props.GetPropertyValue<string>(famistudioBpmPropIdx));
                var tempoInfo     = tempoList[tempoIndex];

                var beatLength    = props.GetPropertyValue<int>(notesPerBeatPropIdx);
                var patternLength = props.GetPropertyValue<int>(notesPerPatternPropIdx);
                var noteLength    = Utils.Min(tempoInfo.groove);

                var grooveIndex   = Array.IndexOf(grooveStrings, props.GetPropertyValue<string>(groovePropIdx));
                var groovePadMode = GroovePaddingType.GetValueForName(props.GetPropertyValue<string>(groovePadPropIdx));
                var grooveList    = FamiStudioTempoUtils.GetAvailableGrooves(tempoInfo.groove);
                var groove        = grooveList[grooveIndex];

                props.UpdateIntegerRange(notesPerPatternPropIdx, 1, Pattern.MaxLength / noteLength);
                props.SetLabelText(framesPerNotePropIdx, noteLength.ToString());

                if (patternIdx == -1)
                {
                    ShowConvertTempoDialogAsync(win, noteLength != originalNoteLength, (convert, duplicate) =>
                    {
                        if (duplicate && song.DuplicatePatternsForNoteLengthChange(0, song.Length - 1, true))
                        {
                            FamiStudio.StaticInstance.DisplayNotification(DuplicateNotificationSong, false);
                        }

                        song.ChangeFamiStudioTempoGroove(groove, convert);
                        song.SetBeatLength(beatLength * song.NoteLength);
                        song.SetDefaultPatternLength(patternLength * song.NoteLength);
                        song.SetGroovePaddingMode(groovePadMode);

                        FinishApply(callback);
                    });
                }
                else
                {
                    var actualNoteLength    = song.NoteLength;
                    var actualPatternLength = song.PatternLength;
                    var actualBeatLength    = song.BeatLength;

                    if (custom)
                    {
                        actualNoteLength = noteLength;
                        actualBeatLength = beatLength * noteLength;
                        actualPatternLength = patternLength * noteLength;
                    }

                    var patternsToResize = new List<int>();
                    for (int i = minPatternIdx; i <= maxPatternIdx; i++)
                    {
                        if (actualNoteLength != song.GetPatternNoteLength(patternIdx))
                        {
                            patternsToResize.Add(i);
                        }
                    }

                    ShowConvertTempoDialogAsync(win, patternsToResize.Count > 0, (convert, duplicate) =>
                    {
                        if (convert)
                        {
                            if (duplicate && song.DuplicatePatternsForNoteLengthChange(minPatternIdx, maxPatternIdx, false))
                            {
                                FamiStudio.StaticInstance.DisplayNotification(DuplicateNotificationCustom, false);
                            }

                            var processedPatterns = new HashSet<Pattern>();
                            foreach (var p in patternsToResize)
                            {
                                song.ResizePatternNotes(p, actualNoteLength, processedPatterns);
                            }
                        }

                        for (int i = minPatternIdx; i <= maxPatternIdx; i++)
                        {
                            if (custom)
                            {
                                song.SetPatternCustomSettings(i, actualPatternLength, actualBeatLength, groove, groovePadMode);
                            }
                            else
                            {
                                song.ClearPatternCustomSettings(i);
                            }
                        }

                        FinishApply(callback);
                    });
                }
            }
        }
    }
}
