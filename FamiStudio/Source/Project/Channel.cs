using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class Channel
    {
        private Song song;
        private Pattern[] patternInstances = new Pattern[Song.MaxLength];
        private List<Pattern> patterns = new List<Pattern>();
        private int type;

        public int Type => type;
        public string Name => ChannelNames[(int)type];
        public Song Song => song;
        public Pattern[] PatternInstances => patternInstances;
        public List<Pattern> Patterns => patterns;
        public bool IsExpansionChannel => type >= ExpansionAudioStart;

        // Channel types.
        public const int Square1 = 0;
        public const int Square2 = 1;
        public const int Triangle = 2;
        public const int Noise = 3;
        public const int Dpcm = 4;
        public const int ExpansionAudioStart = 5;
        public const int Vrc6Square1 = 5;
        public const int Vrc6Square2 = 6;
        public const int Vrc6Saw = 7;
        public const int Vrc7Fm1 = 8;
        public const int Vrc7Fm2 = 9;
        public const int Vrc7Fm3 = 10;
        public const int Vrc7Fm4 = 11;
        public const int Vrc7Fm5 = 12;
        public const int Vrc7Fm6 = 13;
        public const int FdsWave = 14;
        public const int Mmc5Square1 = 15;
        public const int Mmc5Square2 = 16;
        public const int Mmc5Dpcm = 17; 
        public const int NamcoWave1 = 18;
        public const int NamcoWave2 = 19;
        public const int NamcoWave3 = 20;
        public const int NamcoWave4 = 21;
        public const int NamcoWave5 = 22;
        public const int NamcoWave6 = 23;
        public const int NamcoWave7 = 24;
        public const int NamcoWave8 = 25;
        public const int SunsoftSquare1 = 26;
        public const int SunsoftSquare2 = 27;
        public const int SunsoftSquare3 = 28;
        public const int Count = 29;

        public static string[] ChannelNames =
        {
            "Square 1",
            "Square 2",
            "Triangle",
            "Noise",
            "DPCM",
            "Square 1", // VRC6
            "Square 2", // VRC6
            "Saw", // VRC6
            "FM 1", // VRC7
            "FM 2", // VRC7
            "FM 3", // VRC7
            "FM 4", // VRC7
            "FM 5", // VRC7
            "FM 6", // VRC7
            "FDS", // FDS
            "Square 1", // MMC5
            "Square 2", // MMC5
            "DPCM", // MMC5
            "Wave 1", // Namco
            "Wave 2", // Namco
            "Wave 3", // Namco
            "Wave 4", // Namco
            "Wave 5", // Namco
            "Wave 6", // Namco
            "Wave 7", // Namco
            "Wave 8", // Namco
            "Square 1", // Sunsoft
            "Square 2", // Sunsoft
            "Square 3", // Sunsoft
        };

        public static string[] ChannelExportNames =
        {
            "Square1",
            "Square2",
            "Triangle",
            "Noise",
            "DPCM",
            "VRC6Square1", // VRC6
            "VRC6Square2", // VRC6
            "VRC6Saw", // VRC6
            "VRC7FM1", // VRC7
            "VRC7FM2", // VRC7
            "VRC7FM3", // VRC7
            "VRC7FM4", // VRC7
            "VRC7FM5", // VRC7
            "VRC7FM6", // VRC7
            "FDS", // FDS
            "MMC5Square1", // MMC5
            "MMC5Square2", // MMC5
            "MMC5DPCM", // MMC5
            "NamcoWave1", // Namco
            "NamcoWave2", // Namco
            "NamcoWave3", // Namco
            "NamcoWave4", // Namco
            "NamcoWave5", // Namco
            "NamcoWave6", // Namco
            "NamcoWave7", // Namco
            "NamcoWave8", // Namco
            "SunsoftSquare1", // Sunsoft
            "SunsoftSquare2", // Sunsoft
            "SunsoftSquare3", // Sunsoft
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
                return type == Dpcm;

            if (type == Dpcm)
                return true;

            if (instrument.ExpansionType == Project.ExpansionNone && type < ExpansionAudioStart)
                return true;

            if (instrument.ExpansionType == Project.ExpansionVrc6 && type >= Vrc6Square1 && type <= Vrc6Saw)
                return true;

            if (instrument.ExpansionType == Project.ExpansionVrc7 && type >= Vrc7Fm1 && type <= Vrc7Fm6)
                return true;

            if (instrument.ExpansionType == Project.ExpansionFds && type == FdsWave)
                return true;

            if (type >= Mmc5Square1 && type <= Mmc5Square2)
                return true;

            if (instrument.ExpansionType == Project.ExpansionNamco && type >= NamcoWave1 && type <= NamcoWave8)
                return true;

            // MATTT: Will we want special instrument for S5B? Gimmick doesnt use noise or envelopes I think.
            if (instrument.ExpansionType == Project.ExpansionSunsoft && type >= SunsoftSquare1 && type <= SunsoftSquare3)
                return true;

            return false;
        }

        public bool SupportsReleaseNotes => type != Dpcm;
        public bool SupportsSlideNotes => type != Noise && type != Dpcm;
        public bool SupportsVibrato => type != Noise && type != Dpcm;

        public bool SupportsEffect(int effect)
        {
            switch (effect)
            {
                case Note.EffectVolume:       return type != Dpcm;
                case Note.EffectVibratoSpeed: return SupportsVibrato;
                case Note.EffectVibratoDepth: return SupportsVibrato;
            }

            return true;
        }

        public void DuplicateInstancesWithDifferentLengths()
        {
            var instanceLengthMap = new Dictionary<Pattern, int>();

            for (int p = 0; p < song.Length; p++)
            {
                var pattern = patternInstances[p];
                var patternLen = song.GetPatternLength(p);

                if (pattern != null)
                {
                    if (instanceLengthMap.TryGetValue(pattern, out var prevLength))
                    {
                        if (patternLen != prevLength)
                        {
                            pattern = pattern.ShallowClone(); ;
                            patternInstances[p] = pattern;
                        }
                    }

                    instanceLengthMap[pattern] = patternLen;
                }
            }

            UpdatePatternsMaxInstanceLength();
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

        public int GetLastValidEffectValue(int startPatternIdx, int effect)
        {
            for (int p = startPatternIdx; p >= 0; p--)
            {
                var pattern = patternInstances[p];
                if (pattern != null)
                {
                    var lastPatternNoteIdx = song.GetPatternLength(p) - 1;
                    if (pattern.HasLastEffectValueAt(lastPatternNoteIdx, effect))
                        return pattern.GetLastEffectValueAt(lastPatternNoteIdx, effect);
                }
            }

            return Note.GetEffectDefaultValue(song, effect);
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
                    var lastPatternNoteIdx = song.GetPatternLength(patternIdx) - 1;

                    if (pattern.GetLastValidNoteTimeAt(lastPatternNoteIdx) >= 0)
                    {
                        note = pattern.GetLastValidNoteAt(lastPatternNoteIdx);
                        noteIdx = pattern.GetLastValidNoteTimeAt(lastPatternNoteIdx);
                        Debug.Assert(note.IsValid);
                    }

                    released = note.IsStop ? false : released || pattern.GetLastValidNoteReleasedAt(lastPatternNoteIdx);

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
            var patternLength = song.GetPatternLength(patternIdx);

            for (int n = noteIdx + 1; n < patternLength && noteCount < maxNotes; n++, noteCount++)
            {
                var tmpNote = patternInstances[patternIdx].Notes[n];
                if (tmpNote.IsMusical || tmpNote.IsStop)
                    return song.GetPatternStartNote(patternIdx, n) - song.GetPatternStartNote(patternIdx, noteIdx);
            }

            for (int p = patternIdx + 1; p < song.Length && noteCount < maxNotes; p++)
            {
                var pattern = patternInstances[p];
                if (pattern != null && pattern.FirstValidNoteTime >= 0)
                    return song.GetPatternStartNote(p, pattern.FirstValidNoteTime) - song.GetPatternStartNote(patternIdx, noteIdx);
                else
                    noteCount += song.GetPatternLength(p);
            }

            // This mean we hit the end of the song.
            if (noteCount < maxNotes)
                return song.GetPatternStartNote(song.Length) - song.GetPatternStartNote(patternIdx, noteIdx);

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
                if (pattern != null)
                {
                    var lastPatternNoteIdx = song.GetPatternLength(p) - 1;
                    noteIdx = pattern.GetLastValidNoteTimeAt(lastPatternNoteIdx);
                    if (noteIdx >= 0)
                    {
                        var lastNote = pattern.GetLastValidNoteAt(lastPatternNoteIdx);
                        if (lastNote.IsValid && lastNote.Value == noteValue)
                        {
                            patternIdx = p;
                            return true;
                        }
                    }
                }

                if (--p < 0) break;
                n = song.GetPatternLength(p) - 1;
            }

            return false;
        }

        public void ClearPatternsInstancesPastSongLength()
        {
            for (int i = song.Length; i < patternInstances.Length; i++)
                patternInstances[i] = null;
        }

        public static int ChannelTypeToIndex(int type)
        {
            if (type < ExpansionAudioStart)
                return type;
            if (type >= Vrc6Square1 && type <= Vrc6Saw)
                return ExpansionAudioStart + type - Vrc6Square1;
            if (type >= Vrc7Fm1 && type <= Vrc7Fm6)
                return ExpansionAudioStart + type - Vrc7Fm1;
            if (type == FdsWave)
                return ExpansionAudioStart;
            if (type >= Mmc5Square1 && type <= Mmc5Square2)
                return ExpansionAudioStart + type - Mmc5Square1;
            if (type == Mmc5Dpcm)
                return -1;
            if (type >= NamcoWave1 && type <= NamcoWave8)
                return ExpansionAudioStart + type - NamcoWave1;
            if (type >= SunsoftSquare1 && type <= SunsoftSquare3)
                return ExpansionAudioStart + type - SunsoftSquare1;
            Debug.Assert(false);
            return -1;
        }

#if DEBUG
        public void Validate(Song song)
        {
            Debug.Assert(this == song.GetChannelByType(type));
            Debug.Assert(this.song == song);
            foreach (var inst in patternInstances)
                Debug.Assert(inst == null || patterns.Contains(inst));
            foreach (var pat in patterns)
                pat.Validate(this);
        }
#endif

        public void UpdatePatternsMaxInstanceLength()
        {
            foreach (var pattern in patterns)
                pattern.UpdateMaxInstanceLength();
        }

        public void UpdateLastValidNotes()
        {
            foreach (var pattern in patterns)
                pattern.UpdateLastValidNotes();
        }

        public void MergeIdenticalPatterns()
        {
            var patternCrcMap = new Dictionary<uint, Pattern>();

            for (int i = 0; i < patterns.Count; )
            {
                var pattern = patterns[i];
                var crc = pattern.ComputeCRC();

                if (patternCrcMap.TryGetValue(crc, out var matchingPattern))
                {
                    Debug.Assert(pattern.IdenticalTo(matchingPattern));

                    patterns.RemoveAt(i);

                    for (int j = 0; j < song.Length; j++)
                    {
                        if (patternInstances[j] == pattern)
                            patternInstances[j] = matchingPattern;
                    }
                }
                else
                {
                    patternCrcMap[crc] = pattern;
                    i++;
                }
            }

            UpdateLastValidNotes();
            UpdatePatternsMaxInstanceLength();
        }

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

            if (buffer.IsReading && !buffer.IsForUndoRedo)
            {
                ClearPatternsInstancesPastSongLength();
                UpdatePatternsMaxInstanceLength();
            }
        }
    }
}
