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
        public string Name => ChannelType.Names[type];
        public string ShortName => ChannelType.ShortNames[(int)type];
        public Song Song => song;
        public Pattern[] PatternInstances => patternInstances;
        public List<Pattern> Patterns => patterns;
        public bool IsExpansionChannel => type >= ChannelType.ExpansionAudioStart;

        public bool IsFdsWaveChannel => type == ChannelType.FdsWave;
        public bool IsN163WaveChannel => type >= ChannelType.N163Wave1 && type <= ChannelType.N163Wave8;
        public bool IsVrc7FmChannel => type >= ChannelType.Vrc7Fm1 && type <= ChannelType.Vrc7Fm6;

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
                return type == ChannelType.Dpcm;

            if (type == ChannelType.Dpcm)
                return true;

            if (instrument.ExpansionType == ExpansionType.None && type < ChannelType.ExpansionAudioStart)
                return true;

            if (instrument.ExpansionType == ExpansionType.Vrc6 && type >= ChannelType.Vrc6Square1 && type <= ChannelType.Vrc6Saw)
                return true;

            if (instrument.ExpansionType == ExpansionType.Vrc7 && type >= ChannelType.Vrc7Fm1 && type <= ChannelType.Vrc7Fm6)
                return true;

            if (instrument.ExpansionType == ExpansionType.Fds && type == ChannelType.FdsWave)
                return true;

            if (type >= ChannelType.Mmc5Square1 && type <= ChannelType.Mmc5Square2)
                return true;

            if (instrument.ExpansionType == ExpansionType.N163 && type >= ChannelType.N163Wave1 && type <= ChannelType.N163Wave8)
                return true;

            if (instrument.ExpansionType == ExpansionType.S5B && type >= ChannelType.S5BSquare1 && type <= ChannelType.S5BSquare3)
                return true;

            return false;
        }

        public bool SupportsReleaseNotes => type != ChannelType.Dpcm;
        public bool SupportsSlideNotes => type != ChannelType.Noise && type != ChannelType.Dpcm;
        public bool SupportsArpeggios => type != ChannelType.Dpcm;

        public bool SupportsEffect(int effect)
        {
            switch (effect)
            {
                case Note.EffectVolume: return type != ChannelType.Dpcm;
                case Note.EffectFinePitch: return type != ChannelType.Noise && type != ChannelType.Dpcm;
                case Note.EffectVibratoSpeed: return type != ChannelType.Noise && type != ChannelType.Dpcm;
                case Note.EffectVibratoDepth: return type != ChannelType.Noise && type != ChannelType.Dpcm;
                case Note.EffectFdsModDepth: return type == ChannelType.FdsWave;
                case Note.EffectFdsModSpeed: return type == ChannelType.FdsWave;
                case Note.EffectSpeed: return song.UsesFamiTrackerTempo;
                case Note.EffectDutyCycle: return type == ChannelType.Square1 || type == ChannelType.Square2 || type == ChannelType.Mmc5Square1 || type == ChannelType.Mmc5Square2 || type == ChannelType.Vrc6Square1 || type == ChannelType.Vrc6Square2 || type == ChannelType.Noise;
                case Note.EffectNoteDelay: return song.UsesFamiTrackerTempo;
                case Note.EffectCutDelay: return song.UsesFamiTrackerTempo;
            }

            return true;
        }

        public void MakePatternsWithDifferentLengthsUnique()
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
                            pattern = pattern.ShallowClone();
                            patternInstances[p] = pattern;
                        }
                    }

                    instanceLengthMap[pattern] = patternLen;
                }
            }
        }

        public void MakePatternsWithDifferentGroovesUnique()
        {
            var instanceLengthMap = new Dictionary<Pattern, Tuple<int[], int>>();

            for (int p = 0; p < song.Length; p++)
            {
                var pattern = patternInstances[p];
                var groove = song.GetPatternGroove(p);
                var groovePadMode = song.GetPatternGroovePaddingMode(p);

                if (pattern != null)
                {
                    if (instanceLengthMap.TryGetValue(pattern, out var grooveAndPadMode))
                    {
                        if (groove        != grooveAndPadMode.Item1 ||
                            groovePadMode != grooveAndPadMode.Item2)
                        {
                            pattern = pattern.ShallowClone();
                            patternInstances[p] = pattern;
                        }
                    }

                    instanceLengthMap[pattern] = Tuple.Create(groove, groovePadMode);
                }
            }
        }
       
        // Inputs are absolute note indices from beginning of song.
        public void DeleteNotesBetween(int minFrame, int maxFrame, bool preserveFx = false)
        {
            var patternIdxMin = Song.FindPatternInstanceIndex(minFrame, out var patternNoteIdxMin);
            var patternIdxMax = Song.FindPatternInstanceIndex(maxFrame, out var patternNoteIdxMax);

            if (patternIdxMin == patternIdxMax)
            {
                if (patternIdxMin < song.Length)
                {
                    var pattern = patternInstances[patternIdxMin];
                    if (pattern != null)
                    {
                        pattern.DeleteNotesBetween(patternNoteIdxMin, patternNoteIdxMax, preserveFx);
                        pattern.ClearLastValidNoteCache();
                    }
                }
            }
            else
            {
                for (int p = patternIdxMin; p <= patternIdxMax && p < song.Length; p++)
                {
                    var pattern = patternInstances[p];

                    if (pattern != null)
                    {
                        if (p == patternIdxMin)
                        {
                            pattern.DeleteNotesBetween(patternNoteIdxMin, Pattern.MaxLength, preserveFx);
                        }
                        else if (p == patternIdxMax)
                        {
                            pattern.DeleteNotesBetween(0, patternNoteIdxMax, preserveFx);
                        }
                        else
                        {
                            if (preserveFx)
                                pattern.DeleteNotesBetween(0, Pattern.MaxLength, true);
                            else
                                pattern.Notes.Clear();
                        }

                        pattern.ClearLastValidNoteCache();
                    }
                }
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

        public Pattern CreatePatternAndInstance(int idx, string name = null)
        {
            var pattern = CreatePattern(name);
            patternInstances[idx] = pattern;
            return pattern;
        }

        public void ColorizePatterns()
        {
            foreach (var pat in patterns)
            {
                pat.Color = ThemeBase.RandomCustomColor();
            }
        }

        public void DeleteEmptyPatterns()
        {
            for (int i = 0; i < patternInstances.Length; i++)
            {
                if (patternInstances[i] != null && !patternInstances[i].HasAnyNotes)
                {
                    patternInstances[i] = null;
                }
            }

            DeleteUnusedPatterns();
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

        public string GenerateUniquePatternNameSmart(string oldName)
        {
            int firstDigit;

            for (firstDigit = oldName.Length - 1; firstDigit >= 0; firstDigit--)
            {
                if (!char.IsDigit(oldName[firstDigit]))
                    break;
            }

            // Name doesnt end with a number.
            if (firstDigit == oldName.Length - 1)
            {
                if (!oldName.EndsWith(" "))
                    oldName += " ";
                return GenerateUniquePatternName(oldName);
            }
            else
            {
                firstDigit++;

                var number = int.Parse(oldName.Substring(firstDigit)) + 1;
                var baseName = oldName.Substring(0, firstDigit);

                for (; ; number++)
                {
                    var newName = baseName + number.ToString();

                    if (IsPatternNameUnique(newName))
                    {
                        return newName;
                    }
                }
            }
        }

        public bool UsesArpeggios
        {
            get
            {
                foreach (var pattern in patterns)
                {
                    foreach (var note in pattern.Notes.Values)
                    {
                        if (note != null && note.IsArpeggio)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public void DeleteUnusedPatterns()
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

        public void DeleteNotesPastMaxInstanceLength()
        {
            foreach (var pattern in patterns)
                pattern.ClearNotesPastMaxInstanceLength();
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

        public bool GetLastValidNote(ref int patternIdx, ref Note lastValidNote, out int noteIdx, out bool released)
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
                    {
                        lastValidNote = note;
                        return true;
                    }
                }
            }

            return false;
        }

        public static void GetShiftsForType(int type, int numN163Channels, out int pitchShift, out int slideShift)
        {
            if (type >= ChannelType.Vrc7Fm1 && type <= ChannelType.Vrc7Fm6)
            {
                // VRC7 has large pitch values
                slideShift = 3;
                pitchShift = 3;
            }
            else if (type >= ChannelType.N163Wave1 && type <= ChannelType.N163Wave8)
            {
                // Every time we double the number of N163 channels, the pitch values double.
                switch (numN163Channels)
                {
                    case 1:
                        slideShift = 2;
                        pitchShift = 2;
                        break;
                    case 2:
                        slideShift = 3;
                        pitchShift = 3;
                        break;
                    case 3:
                    case 4:
                        slideShift = 4;
                        pitchShift = 4;
                        break;
                    default:
                        slideShift = 5;
                        pitchShift = 5;
                        break;
                }
            }
            else
            {
                // For most channels, we have 1 bit of fraction to better handle slopes.
                slideShift = -1;
                pitchShift = 0;
            }
        }

        // Duration in number of notes, simply to draw in the piano roll.
        public int GetSlideNoteDuration(Note note, int patternIdx, int noteIdx)
        {
            Debug.Assert(note.IsMusical);
            FindNextNoteForSlide(patternIdx, noteIdx, 256, out var nextPatternIdx, out var nextNoteIdx); // 256 is kind of arbitrary. 
            return Song.CountNotesBetween(patternIdx, noteIdx, nextPatternIdx, nextNoteIdx);
        }

        public Note GetNoteAt(int patternIdx, int noteIdx)
        {
            if (patternIdx < song.Length)
            {
                var pattern = patternInstances[patternIdx];
                if (pattern != null && pattern.Notes.ContainsKey(noteIdx))
                {
                    return pattern.Notes[noteIdx];
                }
            }

            return null;
        }

        public bool ComputeSlideNoteParams(Note note, int patternIdx, int noteIdx, int famitrackerSpeed, ushort[] noteTable, bool pal, bool applyShifts, out int pitchDelta, out int stepSize, out float stepSizeFloat)
        {
            Debug.Assert(note.IsMusical);

            var slideShift = 0;

            if (applyShifts)
                GetShiftsForType(type, song.Project.ExpansionNumChannels, out _, out slideShift);

            pitchDelta = noteTable[note.Value] - noteTable[note.SlideNoteTarget];

            if (pitchDelta != 0)
            {
                pitchDelta = slideShift < 0 ? (pitchDelta << -slideShift) : (pitchDelta >> slideShift);

                // Find the next note to calculate the slope.
                FindNextNoteForSlide(patternIdx, noteIdx, 256, out var nextPatternIdx, out var nextNoteIdx); // 256 is kind of arbitrary. 

                // Approximate how many frames separates these 2 notes.
                var frameCount = 0.0f;
                if (patternIdx != nextPatternIdx || noteIdx != nextNoteIdx)
                {
                    // Take delayed notes/cuts into account.
                    var delayFrames = -(note.HasNoteDelay ? note.NoteDelay : 0);
                    if (Song.UsesFamiTrackerTempo)
                    {
                        var nextNote = GetNoteAt(nextPatternIdx, nextNoteIdx);
                        if (nextNote != null)
                        {
                            if (nextNote.HasNoteDelay)
                            {
                                if (nextNote.HasCutDelay)
                                    delayFrames += Math.Min(nextNote.NoteDelay, nextNote.CutDelay);
                                else
                                    delayFrames += nextNote.NoteDelay;
                            }
                            else if (nextNote.HasCutDelay)
                            {
                                delayFrames += nextNote.CutDelay;
                            }
                        }
                    }

                    frameCount = Song.CountFramesBetween(patternIdx, noteIdx, nextPatternIdx, nextNoteIdx, famitrackerSpeed, pal) + delayFrames;
                }
                else
                {
                    Debug.Assert(note.HasCutDelay && Song.UsesFamiTrackerTempo);

                    // Slide note starts and end on same note, this mean we have a delayed cut.
                    frameCount = note.HasCutDelay ? note.CutDelay : 0;
                }

                var absStepPerFrame = Math.Abs(pitchDelta) / Math.Max(1, frameCount);

                stepSize = Utils.Clamp((int)Math.Ceiling(absStepPerFrame) * -Math.Sign(pitchDelta), sbyte.MinValue, sbyte.MaxValue);
                stepSizeFloat = pitchDelta / Math.Max(1, frameCount);

                return true;
            }
            else
            {
                stepSize = 0;
                stepSizeFloat = 0.0f;

                return false;
            }
        }

        public bool FindNextNoteForSlide(int patternIdx, int noteIdx, int maxNotes, out int nextPatternIdx, out int nextNoteIdx)
        {
            nextPatternIdx = patternIdx;
            nextNoteIdx = noteIdx;

            var noteCount = 0;
            var patternLength = song.GetPatternLength(patternIdx);
            var pattern = patternInstances[patternIdx];

            if (pattern.Notes.ContainsKey(noteIdx) &&
                pattern.Notes[noteIdx].HasCutDelay)
            {
                return true;
            }

            for (var it = pattern.GetNoteIterator(noteIdx + 1, patternLength); !it.Done && noteCount < maxNotes; it.Next(), noteCount++)
            {
                var time = it.CurrentTime;
                var note = it.CurrentNote;
                if (note != null && (note.IsMusical || note.IsStop || note.HasCutDelay))
                {
                    nextPatternIdx = patternIdx;
                    nextNoteIdx = time;
                    return true;
                }
            }

            for (int p = patternIdx + 1; p < song.Length && noteCount < maxNotes; p++)
            {
                pattern = patternInstances[p];
                if (pattern != null && pattern.FirstValidNoteTime >= 0)
                {
                    nextPatternIdx = p;
                    nextNoteIdx = noteCount + pattern.FirstValidNoteTime > maxNotes ? maxNotes - noteCount : pattern.FirstValidNoteTime; 
                    return true;
                }
                else
                {
                    noteCount += song.GetPatternLength(p);
                }
            }

            // This mean we hit the end of the song.
            if (noteCount < maxNotes)
            {
                nextPatternIdx = song.Length;
                nextNoteIdx = 0;
                return true;
            }

            // Didnt find anything, just advance to the max note location and return false.
            song.AdvanceNumberOfNotes(maxNotes, ref nextPatternIdx, ref nextNoteIdx);

            return false;
        }

        // Pass -1 to noteValue to get any previous musical note.
        public bool FindPreviousMatchingNote(int noteValue, ref int patternIdx, ref int noteIdx)
        {
            var p = patternIdx;
            var n = noteIdx;

            var pattern = patternInstances[p];
            if (pattern != null)
            {
                // Check current note immediately, if its musical, not need to do anything.
                if (pattern.Notes.TryGetValue(noteIdx, out var currentNote) && currentNote != null && currentNote.IsMusical)
                {
                    return noteValue < 0 || currentNote.Value == noteValue;
                }

                int prevTime = -1;
                Note prevNote = null;

                foreach (var kv in pattern.Notes)
                {
                    var time = kv.Key;
                    if (time >= noteIdx)
                        break;

                    var note = kv.Value;
                    if (note.IsMusical || note.IsStop)
                    {
                        prevTime = time;
                        prevNote = note;
                    }
                }

                if (prevNote != null)
                {
                    if (prevNote.IsMusical && (prevNote.Value == noteValue || noteValue < 0))
                    {
                        patternIdx = p;
                        noteIdx = prevTime;
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
                    n = pattern.GetLastValidNoteTimeAt(lastPatternNoteIdx);
                    if (n >= 0)
                    {
                        var lastNote = pattern.GetLastValidNoteAt(lastPatternNoteIdx);
                        if (lastNote.IsMusical)
                        {
                            if (lastNote.Value == noteValue || noteValue < 0)
                            {
                                noteIdx = n;
                                patternIdx = p;
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }

                if (--p < 0) break;
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
            if (type < ChannelType.ExpansionAudioStart)
                return type;
            if (type >= ChannelType.Vrc6Square1 && type <= ChannelType.Vrc6Saw)
                return ChannelType.ExpansionAudioStart + type - ChannelType.Vrc6Square1;
            if (type >= ChannelType.Vrc7Fm1 && type <= ChannelType.Vrc7Fm6)
                return ChannelType.ExpansionAudioStart + type - ChannelType.Vrc7Fm1;
            if (type == ChannelType.FdsWave)
                return ChannelType.ExpansionAudioStart;
            if (type >= ChannelType.Mmc5Square1 && type <= ChannelType.Mmc5Square2)
                return ChannelType.ExpansionAudioStart + type - ChannelType.Mmc5Square1;
            if (type == ChannelType.Mmc5Dpcm)
                return -1;
            if (type >= ChannelType.N163Wave1 && type <= ChannelType.N163Wave8)
                return ChannelType.ExpansionAudioStart + type - ChannelType.N163Wave1;
            if (type >= ChannelType.S5BSquare1 && type <= ChannelType.S5BSquare3)
                return ChannelType.ExpansionAudioStart + type - ChannelType.S5BSquare1;
            Debug.Assert(false);
            return -1;
        }

#if DEBUG
        public void Validate(Song song, Dictionary<int, object> idMap)
        {
            Debug.Assert(this == song.GetChannelByType(type));
            Debug.Assert(this.song == song);
            foreach (var inst in patternInstances)
                Debug.Assert(inst == null || patterns.Contains(inst));
            foreach (var pat in patterns)
                pat.Validate(this, idMap);
        }
#endif

        public void ClearPatternsLastValidNotesCache()
        {
            foreach (var pattern in patterns)
                pattern.ClearLastValidNoteCache();
        }

        public void MergeIdenticalPatterns()
        {
            var patternCrcMap = new Dictionary<uint, Pattern>();

            for (int i = 0; i < patterns.Count;)
            {
                var pattern = patterns[i];
                var crc = pattern.ComputeCRC();

                if (patternCrcMap.TryGetValue(crc, out var matchingPattern))
                {
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

            ClearPatternsLastValidNotesCache();
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            if (buffer.IsWriting)
                DeleteUnusedPatterns();

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
                ClearPatternsInstancesPastSongLength();
        }
    }

    public static class ChannelType
    {
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
        public const int N163Wave1 = 18;
        public const int N163Wave2 = 19;
        public const int N163Wave3 = 20;
        public const int N163Wave4 = 21;
        public const int N163Wave5 = 22;
        public const int N163Wave6 = 23;
        public const int N163Wave7 = 24;
        public const int N163Wave8 = 25;
        public const int S5BSquare1 = 26;
        public const int S5BSquare2 = 27;
        public const int S5BSquare3 = 28;
        public const int Count = 29;

        public static readonly string[] Names =
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
            "Wave 1", // N163
            "Wave 2", // N163
            "Wave 3", // N163
            "Wave 4", // N163
            "Wave 5", // N163
            "Wave 6", // N163
            "Wave 7", // N163
            "Wave 8", // N163
            "Square 1", // S5B
            "Square 2", // S5B
            "Square 3", // S5B
        };

        public static readonly string[] ShortNames =
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
            "N163Wave1", // N163
            "N163Wave2", // N163
            "N163Wave3", // N163
            "N163Wave4", // N163
            "N163Wave5", // N163
            "N163Wave6", // N163
            "N163Wave7", // N163
            "N163Wave8", // N163
            "S5BSquare1", // S5B
            "S5BSquare2", // S5B
            "S5BSquare3", // S5B
        };

        // TODO: This is really UI specific, move somewhere else...
        public static readonly string[] Icons =
        {
            "Square",
            "Square",
            "Triangle",
            "Noise",
            "DPCM",
            "Square",
            "Square",
            "Saw",
            "FM",
            "FM",
            "FM",
            "FM",
            "FM",
            "FM",
            "WaveTable",
            "Square",
            "Square",
            "DPCM",
            "WaveTable",
            "WaveTable",
            "WaveTable",
            "WaveTable",
            "WaveTable",
            "WaveTable",
            "WaveTable",
            "WaveTable",
            "Square",
            "Square",
            "Square"
        };

        public static int GetValueForName(string str)
        {
            return Array.IndexOf(Names, str);
        }

        public static int GetValueForShortName(string str)
        {
            return Array.IndexOf(ShortNames, str);
        }
    }
}
