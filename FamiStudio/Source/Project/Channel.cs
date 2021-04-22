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
        public string NameWithExpansion => IsExpansionChannel ? $"{Name} ({ExpansionType.ShortNames[song.Project.ExpansionAudio]})" : Name;
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

        public static int[] GetChannelsForExpansion(int expansion)
        {
            var channels = new List<int>(); ;

            channels.Add(ChannelType.Square1);
            channels.Add(ChannelType.Square2);
            channels.Add(ChannelType.Triangle);
            channels.Add(ChannelType.Noise);
            channels.Add(ChannelType.Dpcm);

            switch (expansion)
            {
                case ExpansionType.Vrc6:
                    channels.Add(ChannelType.Vrc6Square1);
                    channels.Add(ChannelType.Vrc6Square2);
                    channels.Add(ChannelType.Vrc6Saw);
                    break;
                case ExpansionType.Vrc7:
                    channels.Add(ChannelType.Vrc7Fm1);
                    channels.Add(ChannelType.Vrc7Fm2);
                    channels.Add(ChannelType.Vrc7Fm3);
                    channels.Add(ChannelType.Vrc7Fm4);
                    channels.Add(ChannelType.Vrc7Fm5);
                    channels.Add(ChannelType.Vrc7Fm6);
                    break;
                case ExpansionType.Fds:
                    channels.Add(ChannelType.FdsWave);
                    break;
                case ExpansionType.Mmc5:
                    channels.Add(ChannelType.Mmc5Square1);
                    channels.Add(ChannelType.Mmc5Square2);
                    break;
                case ExpansionType.N163:
                    channels.Add(ChannelType.N163Wave1);
                    channels.Add(ChannelType.N163Wave2);
                    channels.Add(ChannelType.N163Wave3);
                    channels.Add(ChannelType.N163Wave4);
                    channels.Add(ChannelType.N163Wave5);
                    channels.Add(ChannelType.N163Wave6);
                    channels.Add(ChannelType.N163Wave7);
                    channels.Add(ChannelType.N163Wave8);
                    break;
                case ExpansionType.S5B:
                    channels.Add(ChannelType.S5BSquare1);
                    channels.Add(ChannelType.S5BSquare2);
                    channels.Add(ChannelType.S5BSquare3);
                    break;
            }

            return channels.ToArray();
        }

        public static int GetChannelCountForExpansion(int expansion)
        {
            var count = 5;

            switch (expansion)
            {
                case ExpansionType.Vrc6:
                    count += 3;
                    break;
                case ExpansionType.Vrc7:
                    count += 6;
                    break;
                case ExpansionType.Fds:
                    count += 1;
                    break;
                case ExpansionType.Mmc5:
                    count += 2;
                    break;
                case ExpansionType.N163:
                    count += 8;
                    break;
                case ExpansionType.S5B:
                    count += 3;
                    break;
            }

            return count;
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

            patternIdx = 0;
            noteIdx    = 0;

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

        public SparseChannelNoteIterator GetSparseNoteIterator(NoteLocation start, NoteLocation end)
        {
            return new SparseChannelNoteIterator(this, start, end);
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

        // NOTETODO: Needs to know about note durations.
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

            // NOTETODO : If we are always at a valid note, we just need a function to find the next. No need for dense iterator.
            for (var it = pattern.GetDenseNoteIterator(noteIdx + 1, patternLength); !it.Done && noteCount < maxNotes; it.Next(), noteCount++)
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

        // Converts old (pre FamiStudio 3.0.0) release/stop notes to notes that have their own release point/duration.
        public void ConvertToSolidNotes()
        {
            //var processedPatterns = new HashSet<Pattern>();

            var p0 = -1;
            var t0 = -1;
            var n0 = (Note)null;

            // 
            for (int p1 = 0; p1 < song.Length; p1++)
            {
                var pattern = patternInstances[p1];

                // NOTETODO : Handle cases where pattern starts with a release/stop note. Duplicate + log message.
                // NOTETODO : Also, a pattern can loop with itself, so solve that too. Max of all durations?
                if (pattern == null /* || processedPatterns.Contains(pattern)*/)
                    continue;

                foreach (var kv in pattern.Notes)
                {
                    var t1 = kv.Key;
                    var n1 = kv.Value;

                    if (n1.IsRelease)
                    {
                        if (n0 == null)
                        {
                            Log.LogMessage(LogSeverity.Warning, "Orphan release note."); // NOTETODO : Better error message.
                            continue;
                        }

                        var release = (ushort)song.CountNotesBetween(p0, t0, p1, t1);

                        if (n0.Release > 0 && n0.Release != release)
                        {
                            Log.LogMessage(LogSeverity.Warning, $"Note {n0.FriendlyName} in song {song}, channel {NameWithExpansion}, pattern {patternInstances[p0].Name} has multiple release points, " +
                                "the shortest one will be used. This usually happens when a pattern is re-used, but followed by different patterns starting with a release note. Manual correction may be required.");
                        }

                        n0.Release = Math.Min(release, n0.Duration);
                    }
                    else if (n1.IsStop)
                    {
                        if (n0 == null)
                        {
                            Log.LogMessage(LogSeverity.Warning, "Orphan stop note."); // NOTETODO : Better error message.
                            continue;
                        }

                        var duration = (ushort)song.CountNotesBetween(p0, t0, p1, t1);

                        if (n0.Duration > 0 && n0.Duration != duration)
                        {
                            Log.LogMessage(LogSeverity.Warning, $"Note {n0.FriendlyName} in song {song}, channel {NameWithExpansion}, pattern {patternInstances[p0].Name} has multiple durations, " +
                                "the longest one will be used. This usually happens when a pattern is re-used, but followed by different patterns starting with a stop note. Manual correction may be required.");
                        }

                        n0.Duration = Math.Max(duration, n0.Duration);
                        n0 = null;
                    }
                    else if (n1.IsMusical)
                    {
                        if (n0 != null)
                        {
                            var duration = (ushort)song.CountNotesBetween(p0, t0, p1, t1);

                            if (n0.Duration > 0 && n0.Duration != duration)
                            {
                                Log.LogMessage(LogSeverity.Warning, $"Note {n0.FriendlyName} in song {song}, channel {NameWithExpansion}, pattern {patternInstances[p0].Name} has multiple durations, " +
                                    "the longest one will be used. This usually happens when a pattern is re-used, but followed by different patterns starting with a different note. Manual correction may be required.");
                            }

                            n0.Duration = Math.Max(duration, n0.Duration);
                        }

                        p0 = p1;
                        t0 = t1;
                        n0 = n1;
                    }
                }

                //processedPatterns.Add(pattern);
            }

            // Last note.
            if (n0 != null)
                n0.Duration = (ushort)song.CountNotesBetween(p0, t0, song.Length, 0); // NOTETODO : Review this.

            // Cleanup.
            foreach (var pattern in patterns)
            {
                foreach (var kv in pattern.Notes)
                {
                    if (kv.Value.IsStop || kv.Value.IsRelease)
                        kv.Value.Value = Note.NoteInvalid;
                }

                pattern.RemoveEmptyNotes();
            }
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
    /*
    // Iterator to make it easy to find notes and their release/stop points.
    // Basically to ease the migration to solid notes at FamiStudio 3.0.0.
    public class DenseChannelNoteIterator
    {
        // Current note pattern/time
        private int p0 = -1;
        private int t0 = -1;

        // Next note pattern/time
        private int pn = -1;
        private int tn = -1;

        // End pattern/time
        private int p1 = -1;
        private int t1 = -1;

        // Release point of current note.
        private int pr = -1;
        private int tr = -1;

        // Stop point of current note.
        private int ps = -1;
        private int ts = -1;

        private Channel channel;
        private Pattern pattern;
        private Note note;
        private int idx;
        private bool musicalOnly;

        public DenseChannelNoteIterator(Channel c, int p0, int t0, int p1, int t1)
        {
            Debug.Assert(p0 <= p1 || t0 <= t1);

            this.channel = c;
            this.p1 = p1;
            this.t1 = t1;
            this.musicalOnly = musicalOnly;

            idx = pattern.BinarySearchList(pattern.Notes.Keys, t0, true);
        }

        public int  CurrentPatternIndex => p0;
        public int  CurrentTime => t0;
        public Note CurrentNote => note;

        public bool Done          => p0 >= p1 && t0 > t1;
        public bool IsNoteRelease => p0 == pr && t0 == tr;
        public bool IsNoteStop    => p0 == ps && t0 == ts;

        private void SetCurrentNote(int p, int t)
        {
            p0 = p;
            t0 = t;

            var pattern = channel.PatternInstances[p0];

            // Must start on a musical note right now.
            Debug.Assert(pattern != null);
            Debug.Assert(pattern.Notes.ContainsKey(t0));

            note = pattern.Notes[t0];

            Debug.Assert(note.IsMusical);

            if (note.Release > 0)
            {
                pr = p0;
                tr = t0;
                channel.Song.AdvanceNumberOfNotes(note.Release, ref pr, ref tr);
            }

            if (note.Release > 0)
            {
                ps = p0;
                ts = t0;
                channel.Song.AdvanceNumberOfNotes(note.Duration, ref pr, ref tr);
            }

            //FindNextMusicalNote();
        }

        public void Next()
        {
            t0++;
            if (idx >= 0 && t0 > pattern.Notes.Keys[idx] && idx < pattern.Notes.Values.Count - 1)
                idx++;



            SetCurrentNote(pn, tn);
        }
    }
    */

    // Iterator to to iterate on musical notes in a range of the song and automatically find the following note.
    // Basically to ease the migration to solid notes at FamiStudio 3.0.0.
    public class SparseChannelNoteIterator
    {
        private NoteLocation current; 
        private NoteLocation next;
        private NoteLocation end;

        private Channel channel;
        private Pattern pattern;
        private Note note;
        private int currIdx;
        private int nextIdx;

        public int PatternIndex => current.PatternIndex;
        public int NoteIndex    => current.NoteIndex;

        public Pattern Pattern => pattern;
        public Note    Note    => note;

        public int DistanceToNextNote => channel.Song.CountNotesBetween(current, next);

        public bool Done => current >= end;

        public SparseChannelNoteIterator(Channel c, NoteLocation start, NoteLocation end)
        {
            Debug.Assert(start < end);

            this.channel = c;
            this.end     = end;

            // Look forward for a first musical note.
            do
            {
                pattern = channel.PatternInstances[start.PatternIndex];

                if (pattern != null)
                {
                    var idx = pattern.BinarySearchList(pattern.Notes.Keys, start.NoteIndex, true);

                    if (idx >= 0)
                    {
                        for (; idx < pattern.Notes.Values.Count; idx++)
                        {
                            if (pattern.Notes.Values[idx].IsMusical)
                            {
                                start.NoteIndex = pattern.Notes.Keys[idx];
                                SetCurrentNote(start, idx);
                                return;
                            }
                        }
                    }
                }

                start.PatternIndex++;
                start.NoteIndex = 0;
            }
            while (pattern == null && start.PatternIndex < end.PatternIndex);

            // Done.
            current = end;
        }

        private void SetCurrentNote(NoteLocation location, int listIdx)
        {
            current = location;

            if (Done)
                return;

            pattern = channel.PatternInstances[current.PatternIndex];
            currIdx = listIdx;

            // Must start on a musical note right now.
            Debug.Assert(pattern != null);
            Debug.Assert(pattern.Notes.ContainsKey(current.NoteIndex));

            note = pattern.Notes.Values[currIdx];

            Debug.Assert(note.IsMusical);

            // Find next note.
            nextIdx = currIdx;

            // Look in the same pattern.
            while (++nextIdx < pattern.Notes.Values.Count)
            {
                // Only considering musical notes for now.
                if (pattern.Notes.Values[nextIdx].IsMusical)
                {
                    next.PatternIndex = current.PatternIndex;
                    next.NoteIndex    = pattern.Notes.Keys[nextIdx];
                    return;
                }
            }

            // Next patterns.
            var p = current.PatternIndex + 1;
            for (; p <= end.PatternIndex; p++)
            {
                var pat = channel.PatternInstances[p];

                if (pat != null && pat.Notes.Count > 0)
                {
                    nextIdx = 0;

                    do
                    {
                        // Only considering musical notes for now.
                        if (pat.Notes.Values[nextIdx].IsMusical)
                        {
                            next.PatternIndex = p;
                            next.NoteIndex    = pat.Notes.Keys[nextIdx];
                            return;
                        }
                    }
                    while (++nextIdx < pat.Notes.Values.Count);
                }
            }

            // If we dont find anything, position at end of song.
            next = new NoteLocation(channel.Song.Length, 0);
        }

        public void Next()
        {
            SetCurrentNote(next, nextIdx);
        }
    }

    /*
// Iterator to make it easy to find notes and their release/stop points.
// Basically to ease the migration to solid notes at FamiStudio 3.0.0.
public class SparseChannelNoteIterator
{
    // Current note pattern/time
    private int currPatIdx  = -1;
    private int currNoteIdx = -1;

    // Next note pattern/time
    private int nextPatIdx  = -1;
    private int nextNoteIdx = -1;

    // End pattern/time
    private int endPatIdx  = -1;
    private int endNoteIdx = -1;

    // Release point of current note.
    private int  relPatIdx  = -1;
    private int  relNoteIdx = -1;
    private bool rel = false;

    // Stop point of current note.
    private int  stopPatIdx  = -1;
    private int  stopNoteIdx = -1;
    private bool stop = false;

    private Channel channel;
    private Pattern pattern;
    private Note note;
    private int currIdx;
    private int nextIdx;

    public SparseChannelNoteIterator(Channel c, int p0, int t0, int p1, int t1)
    {
        Debug.Assert(p0 <= p1 || t0 <= t1);

        this.channel = c;
        this.endPatIdx = p1;
        this.endNoteIdx = t1;

        pattern = channel.PatternInstances[p0];
        currIdx = pattern.BinarySearchList(pattern.Notes.Keys, t0, true);

        SetCurrentNote(p0, t0, currIdx);
    }

    public int     CurrentPatternIndex => currPatIdx;
    public int     CurrentNoteIndex    => currNoteIdx;

    public Pattern CurrentPattern      => pattern;
    public Note    CurrentNote         => note;

    public bool Done      => currPatIdx >= endPatIdx && currNoteIdx > endNoteIdx;
    public bool IsRelease => currPatIdx == relPatIdx && currNoteIdx == relNoteIdx;
    public bool IsStop    => currPatIdx == stopPatIdx && currNoteIdx == stopNoteIdx;

    private void SetCurrentNote(int p, int t, int idx)
    {
        currPatIdx = p;
        currNoteIdx = t;
        pattern = channel.PatternInstances[currPatIdx];
        currIdx = idx;

        // Must start on a musical note right now.
        Debug.Assert(pattern != null);
        Debug.Assert(pattern.Notes.ContainsKey(currNoteIdx));

        note = pattern.Notes.Values[currIdx];

        Debug.Assert(note.IsMusical);

        if (note.Release > 0)
        {
            relPatIdx  = currPatIdx;
            relNoteIdx = currNoteIdx;
            rel = true;
            channel.Song.AdvanceNumberOfNotes(note.Release, ref relPatIdx, ref relNoteIdx);
        }

        if (note.Duration > 0)
        {
            stopPatIdx  = currPatIdx;
            stopNoteIdx = currNoteIdx;
            stop = true;
            channel.Song.AdvanceNumberOfNotes(note.Duration, ref stopPatIdx, ref stopNoteIdx);
        }

        FindNextMusicalNote();
    }

    private void FindNextMusicalNote()
    {
        nextPatIdx = -1;
        nextNoteIdx = -1;

        // This isn't very efficient.
        var pattern = channel.PatternInstances[currPatIdx];

        nextIdx = currIdx;

        // Look in the same pattern.
        while (++nextIdx < pattern.Notes.Values.Count)
        {
            // Only considering musical notes for now.
            if (pattern.Notes.Values[nextIdx].IsMusical)
            {
                nextPatIdx  = currPatIdx;
                nextNoteIdx = pattern.Notes.Keys[nextIdx];
                return;
            }
        }

        // Next patterns.
        var p = currPatIdx + 1;
        for (; p <= endPatIdx; p++)
        {
            var pat = channel.PatternInstances[p];

            if (pat != null)
            {
                nextIdx = 0;

                do
                {
                    // Only considering musical notes for now.
                    if (pat.Notes.Values[nextIdx].IsMusical)
                    {
                        nextPatIdx  = p;
                        nextNoteIdx = pat.Notes.Keys[nextIdx];
                        return;
                    }
                }
                while (++nextIdx < pat.Notes.Values.Count);
            }
        }
    }

    public void Next()
    {
        // Is there a pending release, and is it before the next note?
        if (rel && relPatIdx <= nextPatIdx && relNoteIdx < nextNoteIdx)
        {
            currPatIdx  = relPatIdx;
            currNoteIdx = relNoteIdx;
            rel = false;
            return;
        }

        // Is there a pending stop, and is it before the next note?
        if (stop && stopPatIdx <= nextPatIdx && stopNoteIdx < nextNoteIdx)
        {
            currPatIdx  = stopPatIdx;
            currNoteIdx = stopNoteIdx;
            stop = false;
            return;
        }

        SetCurrentNote(nextPatIdx, nextNoteIdx, nextIdx);
    }
}
*/
}
