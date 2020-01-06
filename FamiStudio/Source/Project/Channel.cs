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

            return Note.VolumeMax;
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

        public bool ComputeSlideNoteParams(int patternIdx, int noteIdx, ushort[] noteTable, out int pitchDelta, out int stepSize, out int noteDuration, out int slideFrom, out int slideTo)
        {
            stepSize = 0;
            slideFrom = Note.NoteInvalid;
            slideTo = Note.NoteInvalid;
            pitchDelta = 0;
            noteDuration = -1;

            var note = patternInstances[patternIdx].Notes[noteIdx];
            var prevNote = Note.NoteInvalid;

            // For portamento, we need to find the previous note value.
            if (note.IsPortamento)
            {
                var found = false;
                for (int n = noteIdx - 1; n >= 0; n--)
                {
                    var tmpNote = patternInstances[patternIdx].Notes[n];
                    if (tmpNote.IsMusical || tmpNote.IsStop)
                    {
                        prevNote = tmpNote.Value;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    for (var p = patternIdx - 1; p >= 0; p--)
                    {
                        var pattern = patternInstances[p];
                        if (pattern != null && pattern.LastValidNoteTime >= 0)
                        {
                            prevNote = pattern.LastValidNote.Value;
                            break;
                        }
                    }
                }

                if (prevNote == Note.NoteInvalid || prevNote == Note.NoteStop)
                    return false;
            }

            // Then we need the next note to find calculate the slope.
            {
                var found = false;
                for (int n = noteIdx + 1; n < song.PatternLength; n++)
                {
                    var tmpNote = patternInstances[patternIdx].Notes[n];
                    if (tmpNote.IsMusical || tmpNote.IsStop)
                    {
                        noteDuration = (patternIdx * song.PatternLength + n) - (patternIdx * song.PatternLength + noteIdx);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    for (int p = patternIdx + 1; p < song.Length; p++)
                    {
                        var pattern = patternInstances[p];
                        if (pattern != null && pattern.FirstValidNoteTime >= 0)
                        {
                            noteDuration = (p * song.PatternLength + pattern.FirstValidNoteTime) - (patternIdx * song.PatternLength + noteIdx);
                            break;
                        }
                    }
                }

                if (noteDuration < 0)
                    noteDuration =  1024 / song.Speed; // This is kind of arbitrary. 
            }

            if (note.IsPortamento && note.PortamentoLength > 0)
                noteDuration = Math.Min(noteDuration, note.PortamentoLength);

            slideFrom = note.IsPortamento ? prevNote   : note.Value;
            slideTo   = note.IsPortamento ? note.Value : note.SlideNoteTarget;

            if (slideFrom == Note.NoteStop || slideTo == Note.NoteStop)
                return false;

            if (noteTable == null)
            {
                return slideFrom != slideTo;
            }
            else
            {
                pitchDelta = (int)noteTable[slideFrom] - (int)noteTable[slideTo];

                if (pitchDelta != 0)
                {
                    pitchDelta <<= 1; // We have 1 bit of fraction to better handle various slopes.

                    var frameCount = Math.Min(noteDuration * song.Speed + 1, 255);
                    var floatStep  = Math.Abs(pitchDelta) / (float)frameCount;

                    stepSize = Utils.Clamp((int)Math.Ceiling(floatStep) * -Math.Sign(pitchDelta), sbyte.MinValue, sbyte.MaxValue);

                    noteDuration = (int)Math.Abs(Math.Ceiling(pitchDelta / (float)stepSize / song.Speed)); // MATTT

                    return true;
                }

                return false;
            }
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
