using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class Channel
    {
        // Channel types.
        public const int Square1 = 0;
        public const int Square2 = 1;
        public const int Triangle = 2;
        public const int Noise = 3;
        public const int DPCM = 4;
        public const int ExpansionAudioStart = 5;
        public const int VRC6Square1 = 5;
        public const int VRC6Square2 = 6;
        public const int VRC6Saw = 7;
        public const int Count = 8;

        private Song song;
        private Pattern[] patternInstances = new Pattern[Song.MaxLength];
        private List<Pattern> patterns = new List<Pattern>();
        private int type;

        public int Type => type;
        public string Name => ChannelNames[(int)type];
        public Song Song => song;
        public Pattern[] PatternInstances => patternInstances;
        public List<Pattern> Patterns => patterns;

        public static string[] ChannelNames =
        {
            "Square 1",
            "Square 2",
            "Triangle",
            "Noise",
            "DPCM",
            "Square 1", // VRC6
            "Square 2", // VRC6
            "Saw" // VRC6
        };

        public Channel()
        {
            // For serialization
        }

        public Channel(Song song, int type, int songLength)
        {
            this.song = song;
            this.type = type;
        }

        public Pattern GetPattern(string name)
        {
            return patterns.Find(p => p.Name == name);
        }

        public Pattern GetPattern(int id)
        {
            return patterns.Find(p => p.Id == id);
        }

        public bool SupportsInstrument(Instrument instrument)
        {
            if (instrument == null)
                return type == DPCM;

            if (type == DPCM)
                return true;

            if (instrument.ExpansionType == Project.ExpansionNone && type < ExpansionAudioStart)
                return true;

            if (instrument.ExpansionType == Project.ExpansionVRC6 && type >= VRC6Square1 && type <= VRC6Saw)
                return true;

            return false;
        }

        public bool SupportsReleaseNotes => type != DPCM;
        public bool SupportsSlideNotes => type != Noise && type != DPCM;
        public bool SupportsVibrato => type != Noise && type != DPCM;

        public bool SupportsEffect(int effect)
        {
            switch (effect)
            {
                case Note.EffectVolume:       return type != DPCM;
                case Note.EffectVibratoSpeed: return SupportsVibrato;
                case Note.EffectVibratoDepth: return SupportsVibrato;
            }

            return true;
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

            min = new Note(255);
            max = new Note(0);

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

        public byte GetLastValidEffectValue(int startPatternIdx, int effect)
        {
            for (int p = startPatternIdx; p >= 0; p--)
            {
                var pattern = patternInstances[p];
                if (pattern != null)
                {
                    if (pattern.HasLastEffectValue(effect))
                        return pattern.GetLastEffectValue(effect);
                }
            }

            return (byte)Note.GetEffectDefaultValue(song, effect);
        }

        public bool GetLastValidNote(ref int patternIdx, out int noteIdx, out bool released)
        {
            noteIdx = -1;
            released = false;

            // Find previous valid note.
            for (; patternIdx >= 0; patternIdx--)
            {
                var pattern = patternInstances[patternIdx];
                if (pattern != null)
                {
                    var note = new Note(Note.NoteInvalid);

                    if (pattern.LastValidNoteTime >= 0)
                    {
                        note = pattern.LastValidNote;
                        noteIdx = pattern.LastValidNoteTime;
                        Debug.Assert(pattern.LastValidNote.IsValid);
                    }

                    released = note.IsStop ? false : released || pattern.LastValidNoteReleased;

                    if (note.IsValid)
                        return true;
                }
            }

            return false;
        }

        public bool ComputeSlideNoteParams(int patternIdx, int noteIdx, ushort[] noteTable, out int pitchDelta, out int stepSize, out int noteDuration)
        {
            var note = patternInstances[patternIdx].Notes[noteIdx];

            Debug.Assert(note.IsMusical);

            // Find the next note to calculate the slope.
            noteDuration = FindNextNoteForSlide(patternIdx, noteIdx, 256); // 256 is kind of arbitrary. 
            stepSize = 0;
            pitchDelta = 0;

            if (noteTable == null)
            {
                return note.Value != note.SlideNoteTarget;
            }
            else
            {
                pitchDelta = (int)noteTable[note.Value] - (int)noteTable[note.SlideNoteTarget];

                if (pitchDelta != 0)
                {
                    pitchDelta <<= 1; // We have 1 bit of fraction to better handle various slopes.

                    var frameCount = noteDuration * song.Speed + 1;
                    var floatStep  = Math.Abs(pitchDelta) / (float)frameCount;

                    stepSize = Utils.Clamp((int)Math.Ceiling(floatStep) * -Math.Sign(pitchDelta), sbyte.MinValue, sbyte.MaxValue);

                    return true;
                }

                return false;
            }
        }

        public int FindNextNoteForSlide(int patternIdx, int noteIdx, int maxNotes)
        {
            var noteCount = 0;

            for (int n = noteIdx + 1; n < song.PatternLength && noteCount < maxNotes; n++, noteCount++)
            {
                var tmpNote = patternInstances[patternIdx].Notes[n];
                if (tmpNote.IsMusical || tmpNote.IsStop)
                    return (patternIdx * song.PatternLength + n) - (patternIdx * song.PatternLength + noteIdx);
            }

            for (int p = patternIdx + 1; p < song.Length && noteCount < maxNotes; p++)
            {
                var pattern = patternInstances[p];
                if (pattern != null && pattern.FirstValidNoteTime >= 0)
                    return (p * song.PatternLength + pattern.FirstValidNoteTime) - (patternIdx * song.PatternLength + noteIdx);
                else
                    noteCount += song.PatternLength;
            }

            // This mean we hit the end of the song.
            if (noteCount < maxNotes)
                return (song.Length * song.PatternLength) - (patternIdx * song.PatternLength + noteIdx);

            return maxNotes;
        }

        public bool FindPreviousMatchingNote(int noteValue, ref int patternIdx, ref int noteIdx)
        {
            int p = patternIdx;
            int n = noteIdx;

            var pattern = patternInstances[p];
            if (pattern != null)
            {
                while (n >= 0 && !pattern.Notes[n].IsValid) n--;

                if (n >= 0)
                {
                    if (pattern.Notes[n].Value == noteValue)
                    {
                        patternIdx = p;
                        noteIdx = n;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            p--;
            while (p >= 0)
            {
                pattern = patternInstances[p];
                if (pattern != null && pattern.LastValidNoteTime >= 0)
                {
                    if (pattern.LastValidNote.IsValid && 
                        pattern.LastValidNote.Value == noteValue)
                    {
                        n = pattern.LastValidNoteTime;
                        patternIdx = p;
                        noteIdx = n;
                        return true;
                    }
                }

                n = song.PatternLength - 1;
                p--;
            }

            return false;
        }

        public void ClearPatternsInstancesPastSongLength()
        {
            for (int i = song.Length; i < patternInstances.Length; i++)
                patternInstances[i] = null;
        }

        public void ClearNotesPastSongLength()
        {
            foreach (var pattern in patterns)
                pattern.ClearNotesPastSongLength();
        }

#if DEBUG
        public void Validate(Song song)
        {
            Debug.Assert(this == song.Channels[type]);
            Debug.Assert(this.song == song);
            foreach (var inst in patternInstances)
                Debug.Assert(inst == null || patterns.Contains(inst));
            foreach (var pat in patterns)
                pat.Validate(this);
        }
#endif

        public void SerializeState(ProjectBuffer buffer)
        {
            if (buffer.IsWriting)
                CleanupUnusedPatterns();

            int patternCount = patterns.Count;

            buffer.Serialize(ref song);
            buffer.Serialize(ref patternCount);

            // At version 4 (FamiStudio 1.4.0) we added basic expansion audio.
            if (buffer.Version >= 4)
                buffer.Serialize(ref type);

            buffer.InitializeList(ref patterns, patternCount);
            foreach (var pattern in patterns)
                pattern.SerializeState(buffer);

            for (int i = 0; i < patternInstances.Length; i++)
                buffer.Serialize(ref patternInstances[i], this);
        }
    }
}
