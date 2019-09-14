using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class Channel
    {
        public const int Square1 = 0;
        public const int Square2 = 1;
        public const int Triangle = 2;
        public const int Noise = 3;
        public const int DPCM = 4;
        public const int Count = 5;

        private Song song;
        private Pattern[] patternInstances = new Pattern[Song.MaxLength];
        private List<Pattern> patterns = new List<Pattern>();
        private int type;

        static string[] channelNames =
        {
            "Square 1",
            "Square 2",
            "Triangle",
            "Noise",
            "DPCM",
            ""
        };

        public Channel(Song song, int type, int songLength)
        {
            this.song = song;
            this.type = type;
        }

        public int Type
        {
            get { return type; }
        }

        public string Name
        {
            get { return channelNames[(int)type]; }
        }

        public Pattern[] PatternInstances
        {
            get { return patternInstances; }
        }

        public List<Pattern> Patterns
        {
            get { return patterns; }
        }

        public Pattern GetPattern(string name)
        {
            return patterns.Find(p => p.Name == name);
        }

        public Pattern GetPattern(int id)
        {
            return patterns.Find(p => p.Id == id);
        }

        public void Split(int factor)
        {
            if ((song.PatternLength % factor) == 0)
            {
                // TODO: This might generate identical patterns, need to cleanup.
                var splitPatterns = new Dictionary<Pattern, Pattern[]>();
                var newPatterns = new List<Pattern>();

                foreach (var pattern in patterns)
                {
                    var splits = pattern.Split(factor);
                    splitPatterns[pattern] = splits;
                    newPatterns.AddRange(splits);
                }

                var newSongLength = song.Length * factor;
                var newPatternsInstances = new Pattern[Song.MaxLength];
                
                for (int i = 0; i < song.Length; i++)
                {
                    var oldPattern = patternInstances[i];
                    if (oldPattern != null)
                    {
                        for (int j = 0; j < factor; j++)
                        {
                            newPatternsInstances[i * factor + j] = splitPatterns[oldPattern][j];
                        }
                    }
                }

                patternInstances = newPatternsInstances;
                patterns = newPatterns;
            }
        }

        public Pattern CreatePattern(string name = null)
        {
            if (name == null)
            {
                name = GenerateUniquePatternName();
            }
            else if (!IsPatternNameUnique(name))
            {
                Debug.Assert(false);
                return null;
            }

            var pat = new Pattern(song.Project.GenerateUniqueId(), song, type, name);
            patterns.Add(pat);
            return pat;
        }

        public void ColorizePatterns()
        {
            foreach (var pat in patterns)
            {
                pat.Color = ThemeBase.RandomCustomColor();
            }
        }

        public void RemoveEmptyPatterns()
        {
            for (int i = 0; i < patternInstances.Length; i++)
            {
                if (patternInstances[i] != null && !patternInstances[i].HasAnyNotes)
                {
                    patternInstances[i] = null;
                }
            }

            CleanupUnusedPatterns();
        }

        public bool RenamePattern(Pattern pattern, string name)
        {
            if (pattern.Name == name)
                return true;

            if (patterns.Find(p => p.Name == name) == null)
            {
                pattern.Name = name;
                return true;
            }

            return false;
        }

        public bool IsPatternNameUnique(string name)
        {
            return patterns.Find(p => p.Name == name) == null;
        }

        public string GenerateUniquePatternName(string baseName = null)
        {
            for (int i = 1; ; i++)
            {
                string name = (baseName != null ? baseName : "Pattern ") + i;
                if (IsPatternNameUnique(name))
                {
                    return name;
                }
            }
        }

        public bool GetMinMaxNote(out Note min, out Note max)
        {
            bool valid = false;

            min = new Note() { Value = 255 };
            max = new Note() { Value = 0 };

            for (int i = 0; i < song.Length; i++)
            {
                var pattern = PatternInstances[i];
                if (pattern != null)
                {
                    for (int j = 0; j < song.PatternLength; j++)
                    {
                        var n = pattern.Notes[j];
                        if (n.IsValid && !n.IsStop)
                        {
                            if (n.Value < min.Value) min = n;
                            if (n.Value > max.Value) max = n;
                            valid = true;
                        }
                    }
                }
            }

            return valid;
        }

        public void CleanupUnusedPatterns()
        {
            HashSet<Pattern> usedPatterns = new HashSet<Pattern>();
            for (int i = 0; i < song.Length; i++)
            {
                var inst = patternInstances[i];
                if (inst != null)
                {
                    usedPatterns.Add(inst);
                }
            }

            patterns.Clear();
            patterns.AddRange(usedPatterns);
        }

        public byte GetLastValidVolume(int startPatternIdx)
        {
            var lastVolume = (byte)Note.VolumeInvalid;

            for (int p = startPatternIdx; p >= 0 && lastVolume == Note.VolumeInvalid; p--)
            {
                var pattern = patternInstances[p];
                if (pattern != null)
                {
                    lastVolume = pattern.LastVolumeValue;
                    if (lastVolume != Note.VolumeInvalid)
                    {
                        return lastVolume;
                    }
                }
            }

            return Note.VolumeInvalid;
        }

        public byte GetLastValidNote(ref int patternIdx, out int lastNoteTime, out Instrument instrument)
        {
            var lastTime = int.MinValue;
            var lastNote = new Note() { Value = Note.NoteInvalid };

            // Find previous valid note.
            for (int p = patternIdx; p >= 0 && lastTime == int.MinValue; p--)
            {
                var pattern = patternInstances[p];
                if (pattern != null)
                {
                    var val = pattern.LastValidNoteValue;
                    if (val != Note.NoteInvalid)
                    { 
                        lastNoteTime = pattern.LastValidNoteTime;
                        instrument   = pattern.LastValidNoteInstrument;

                        return val;
                    }
                }
            }

            lastNoteTime = -1;
            patternIdx   = int.MinValue;
            instrument   = null;

            return Note.NoteInvalid;
        }

        public bool FindPreviousValidNote(int noteValue, ref int patternIdx, ref int noteIdx)
        {
            var pattern = patternInstances[patternIdx];
            if (pattern != null)
            {
                while (noteIdx >= 0 && !pattern.Notes[noteIdx].IsValid) noteIdx--;

                if (noteIdx >= 0)
                {
                    return pattern.Notes[noteIdx].Value == noteValue;
                }
            }

            patternIdx--;
            while (patternIdx >= 0)
            {
                pattern = patternInstances[patternIdx];
                if (pattern != null)
                {
                    var val = pattern.LastValidNoteValue;
                    if (val != Note.NoteInvalid && val == noteValue)
                    {
                        noteIdx = pattern.LastValidNoteTime;
                        return true;
                    }
                }

                noteIdx = song.PatternLength - 1;
                patternIdx--;
            }

            return false;
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            if (buffer.IsWriting)
                CleanupUnusedPatterns();

            int patternCount = patterns.Count;

            buffer.Serialize(ref song);
            buffer.Serialize(ref patternCount);
            buffer.InitializeList(ref patterns, patternCount);

            foreach (var pattern in patterns)
                pattern.SerializeState(buffer);

            for (int i = 0; i < PatternInstances.Length; i++)
                buffer.Serialize(ref patternInstances[i], this);
        }
    }
}
