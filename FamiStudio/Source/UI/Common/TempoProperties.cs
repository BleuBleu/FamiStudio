using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FamiStudio
{
    class TempoProperties
    {
        private int firstPropertyIndex;
        private PropertyPage props;
        private Song song;

        private int patternIdx = -1;
        private int minPatternIdx = -1;
        private int maxPatternIdx = -1;

        public TempoProperties(PropertyPage props, Song song, int patternIdx = -1, int minPatternIdx = -1, int maxPatternIdx = -1)
        {
            this.song = song;
            this.props = props;
            this.patternIdx = patternIdx;
            this.minPatternIdx = minPatternIdx;
            this.maxPatternIdx = maxPatternIdx;
            this.firstPropertyIndex = props.PropertyCount;

            if (song.UsesFamiTrackerTempo)
            {
                if (patternIdx < 0)
                {
                    props.AddIntegerRange("Tempo :", song.FamitrackerTempo, 32, 255, CommonTooltips.Tempo); // 0
                    props.AddIntegerRange("Speed :", song.FamitrackerSpeed, 1, 31, CommonTooltips.Speed); // 1
                }

                var notesPerBeat    = patternIdx < 0 ? song.BeatLength    : song.GetPatternBeatLength(patternIdx);
                var notesPerPattern = patternIdx < 0 ? song.PatternLength : song.GetPatternLength(patternIdx);
                var bpm = Song.ComputeFamiTrackerBPM(song.Project.PalMode, song.FamitrackerSpeed, song.FamitrackerTempo, notesPerBeat);

                props.AddIntegerRange("Notes per Beat :", notesPerBeat, 1, 256, CommonTooltips.NotesPerBar); // 2
                props.AddIntegerRange("Notes per Pattern :", notesPerPattern, 1, 256, CommonTooltips.NotesPerPattern); // 3
                props.AddLabel("BPM :", bpm.ToString("n1"), CommonTooltips.BPM); // 4
            }
            else
            {
                var noteLength      = (patternIdx < 0 ? song.NoteLength    : song.GetPatternNoteLength(patternIdx));
                var notesPerBeat    = (patternIdx < 0 ? song.BeatLength    : song.GetPatternBeatLength(patternIdx)) / noteLength;
                var notesPerPattern = (patternIdx < 0 ? song.PatternLength : song.GetPatternLength(patternIdx))     / noteLength;
                var bpm = Song.ComputeFamiStudioBPM(song.Project.PalMode, noteLength, notesPerBeat);

                props.AddIntegerRange("Frames per Note : ", noteLength, Song.MinNoteLength, Song.MaxNoteLength, CommonTooltips.FramesPerNote); // 0
                props.AddIntegerRange("Notes per Beat : ", notesPerBeat, 1, 256, CommonTooltips.NotesPerBar); // 1
                props.AddIntegerRange("Notes per Pattern : ", notesPerPattern, 1, Pattern.MaxLength / noteLength, CommonTooltips.NotesPerPattern); // 2
                props.AddLabel("BPM :", bpm.ToString("n1"), CommonTooltips.BPM); // 3

                //var tempos = FamiStudioTempoUtils.GetAvailableTempoList(song.Project.PalMode);
                //var tempoStrings = tempos.Select(t => t.bpm.ToString("n1")).ToArray();

                //// MATTT
                //propertyPage.BeginAdvancedProperties();
                //propertyPage.AddIntegerRange("Test1 : ", 5, 0, 10); // 7
                //propertyPage.AddString("Test2 : ", "Hello"); // 8
                //propertyPage.AddDropDownList("BPM : ", tempoStrings, tempoStrings[0]);
            }
        }

        public void EnableProperties(bool enabled)
        {
            for (var i = firstPropertyIndex; i < props.PropertyCount; i++)
                props.SetPropertyEnabled(i, enabled);
        }

        public void Apply(bool custom = false)
        {
            if (song.UsesFamiTrackerTempo)
            {
                if (patternIdx == -1)
                {
                    song.FamitrackerTempo = props.GetPropertyValue<int>(firstPropertyIndex + 0);
                    song.FamitrackerSpeed = props.GetPropertyValue<int>(firstPropertyIndex + 1);
                    song.SetBeatLength(props.GetPropertyValue<int>(firstPropertyIndex + 2));
                    song.SetDefaultPatternLength(props.GetPropertyValue<int>(firstPropertyIndex + 3));
                }
                else
                {
                    for (int i = minPatternIdx; i <= maxPatternIdx; i++)
                    {
                        var beatLength    = props.GetPropertyValue<int>(firstPropertyIndex + 0);
                        var patternLength = props.GetPropertyValue<int>(firstPropertyIndex + 1);

                        if (custom)
                            song.SetPatternCustomSettings(i, patternLength, beatLength);
                        else
                            song.ClearPatternCustomSettings(i);
                    }
                }
            }
            else
            {
                if (patternIdx == -1)
                {
                    var newNoteLength = props.GetPropertyValue<int>(firstPropertyIndex + 0);

                    if (newNoteLength != song.NoteLength)
                    {
                        var convertTempo = PlatformUtils.MessageBox($"You changed the note length, do you want FamiStudio to attempt convert the tempo by resizing notes?", "Tempo Change", MessageBoxButtons.YesNo) == DialogResult.Yes;
                        song.ResizeNotes(newNoteLength, convertTempo);
                    }

                    song.SetBeatLength(props.GetPropertyValue<int>(firstPropertyIndex + 1) * song.NoteLength);
                    song.SetDefaultPatternLength(props.GetPropertyValue<int>(firstPropertyIndex + 2) * song.NoteLength);
                }
                else
                {
                    var askedToConvertTempo = false;
                    var convertTempo = false;

                    for (int i = minPatternIdx; i <= maxPatternIdx; i++)
                    {
                        var noteLength    = song.NoteLength;
                        var patternLength = song.PatternLength;
                        var beatLength    = song.BeatLength;

                        if (custom)
                        {
                            noteLength    = props.GetPropertyValue<int>(firstPropertyIndex + 0);
                            beatLength    = props.GetPropertyValue<int>(firstPropertyIndex + 1) * noteLength;
                            patternLength = props.GetPropertyValue<int>(firstPropertyIndex + 2) * noteLength;
                        }

                        if (noteLength != song.GetPatternNoteLength(patternIdx))
                        {
                            if (!askedToConvertTempo)
                            {
                                convertTempo = PlatformUtils.MessageBox($"You changed the note length for this pattern, do you want FamiStudio to attempt convert the tempo by resizing notes?", "Tempo Change", MessageBoxButtons.YesNo) == DialogResult.Yes;
                                askedToConvertTempo = true;
                            }

                            if (convertTempo)
                                song.ResizePatternNotes(i, noteLength);
                        }

                        if (custom)
                            song.SetPatternCustomSettings(i, patternLength, beatLength, noteLength);
                        else
                            song.ClearPatternCustomSettings(i);
                    }
                }
            }
        }
    }
}
