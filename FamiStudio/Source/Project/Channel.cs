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
        private int maxValidCacheIndex = -1;
        private PatternCumulativeCache[] patternCache = new PatternCumulativeCache[Song.MaxLength];
        private List<Pattern> patterns = new List<Pattern>();
        private int type;

        public int Type => type;
        public string Name => ChannelType.InternalNames[type];
        public string LocalizedName => ChannelType.LocalizedNames[type];
        public string ShortName => ChannelType.InternalNames[type];
        public string NameWithExpansion => ChannelType.GetLocalizedNameWithExpansion(type);
        public Song Song => song;
        public Pattern[] PatternInstances => patternInstances;
        public List<Pattern> Patterns => patterns;
        public bool IsExpansionChannel => type >= ChannelType.ExpansionAudioStart;
        public int Expansion => ChannelType.GetExpansionTypeForChannelType(type);
        public int ExpansionChannelIndex => ChannelType.GetExpansionChannelIndexForChannelType(type);
        public int Index => ChannelTypeToIndex(type, song.Project.ExpansionAudioMask, song.Project.ExpansionNumN163Channels);

        public bool IsRegularChannel    => ChannelType.IsRegularChannel(type);
        public bool IsFdsChannel        => ChannelType.IsFdsChannel(type);
        public bool IsN163Channel       => ChannelType.IsN163Channel(type);
        public bool IsVrc6Channel       => ChannelType.IsVrc6Channel(type);
        public bool IsVrc7Channel       => ChannelType.IsVrc7Channel(type);
        public bool IsMmc5Channel       => ChannelType.IsMmc5Channel(type);
        public bool IsS5BChannel        => ChannelType.IsS5BChannel(type);
        public bool IsNoiseChannel      => ChannelType.IsNoiseChannel(type);
        public bool IsDpcmChannel       => ChannelType.IsDpcmChannel(type);
        public bool IsEPSMChannel       => ChannelType.IsEPSMChannel(type);
        public bool IsEPSMSquareChannel => ChannelType.IsEPSMSquareChannel(type);
        public bool IsEPSMFmChannel     => ChannelType.IsEPSMFmChannel(type);
        public bool IsEPSMRythmChannel  => ChannelType.IsEPSMRythmChannel(type);

        public Channel(Song song, int type, int songLength)
        {
            this.song = song;
            this.type = type;

            for (int i = 0; i < patternCache.Length; i++)
                patternCache[i] = new PatternCumulativeCache();
        }

        public override string ToString()
        {
            return NameWithExpansion;
        }

        public Pattern GetPattern(string name)
        {
            return patterns.Find(p => p.Name == name);
        }

        public Pattern GetPattern(int id)
        {
            return patterns.Find(p => p.Id == id);
        }

        public Pattern GetOrCreatePattern(int patternIdx)
        {
            if (patternInstances[patternIdx] == null)
                patternInstances[patternIdx] = CreatePattern();
            return patternInstances[patternIdx];
        }

        public bool SupportsInstrument(Instrument instrument, bool allowNull = true)
        {
            if (instrument == null)
                return allowNull;

            if (instrument.Expansion == ExpansionType.None && (IsRegularChannel || IsMmc5Channel))
                return true;

            return instrument.Expansion == Expansion;
        }

        public static int[] GetChannelsForExpansionMask(int expansionMask, int numN163Channels = 1)
        {
            var channels = new List<int>();

            for (int i = 0; i < ChannelType.Count; i++)
            {
                if (Project.IsChannelActive(i, expansionMask, numN163Channels))
                    channels.Add(i);
            }

            return channels.ToArray();
        }

        public static int GetChannelCountForExpansionMask(int expansionMask, int numN163Channels = 1)
        {
            var count = 5;

            if ((expansionMask & ExpansionType.Vrc6Mask) != 0) count += 3;
            if ((expansionMask & ExpansionType.Vrc7Mask) != 0) count += 6;
            if ((expansionMask & ExpansionType.FdsMask)  != 0) count += 1;
            if ((expansionMask & ExpansionType.Mmc5Mask) != 0) count += 2;
            if ((expansionMask & ExpansionType.N163Mask) != 0) count += numN163Channels;
            if ((expansionMask & ExpansionType.S5BMask)  != 0) count += 3;
            if ((expansionMask & ExpansionType.EPSMMask) != 0) count += 15;

            return count;
        }

        public bool SupportsStopNotes     => true;
        public bool SupportsReleaseNotes  => type != ChannelType.Dpcm;
        public bool SupportsSlideNotes    => type != ChannelType.Dpcm;
        public bool SupportsArpeggios     => type != ChannelType.Dpcm;
        public bool SupportsNoAttackNotes => type != ChannelType.Dpcm;

        public bool SupportsEffect(int effect)
        {
            switch (effect)
            {
                case Note.EffectVolume:
                case Note.EffectVolumeSlide: return type != ChannelType.Dpcm;
                case Note.EffectFinePitch: return type != ChannelType.Noise && type != ChannelType.Dpcm && !IsEPSMRythmChannel;
                case Note.EffectVibratoSpeed: return type != ChannelType.Noise && type != ChannelType.Dpcm && !IsEPSMRythmChannel;
                case Note.EffectVibratoDepth: return type != ChannelType.Noise && type != ChannelType.Dpcm && !IsEPSMRythmChannel;
                case Note.EffectFdsModDepth: return type == ChannelType.FdsWave;
                case Note.EffectFdsModSpeed: return type == ChannelType.FdsWave;
                case Note.EffectSpeed: return song.UsesFamiTrackerTempo;
                case Note.EffectDutyCycle: return type == ChannelType.Square1 || type == ChannelType.Square2 || type == ChannelType.Mmc5Square1 || type == ChannelType.Mmc5Square2 || type == ChannelType.Vrc6Square1 || type == ChannelType.Vrc6Square2 || type == ChannelType.Noise;
                case Note.EffectNoteDelay: return song.UsesFamiTrackerTempo;
                case Note.EffectCutDelay: return song.UsesFamiTrackerTempo;
                case Note.EffectDeltaCounter: return type == ChannelType.Dpcm;
                case Note.EffectPhaseReset: return 
                        type == ChannelType.Square1 || type == ChannelType.Square2 || 
                        type == ChannelType.Mmc5Square1 || type == ChannelType.Mmc5Square2 || 
                        IsVrc6Channel || IsFdsChannel || IsN163Channel;
                case Note.EffectEnvelopePeriod: return IsS5BChannel || IsEPSMSquareChannel;
            }

            return true;
        }

        public bool ShouldDisplayEffect(int effect)
        {
            return SupportsEffect(effect) && effect != Note.EffectVolumeSlide;
        }

        public void RemoveUnsupportedFeatures(bool checkOnly = false)
        {
            foreach (var pattern in patterns)
                pattern.RemoveUnsupportedChannelFeatures(checkOnly);
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

            InvalidateCumulativePatternCache();
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
                        if (groove != grooveAndPadMode.Item1 ||
                            groovePadMode != grooveAndPadMode.Item2)
                        {
                            pattern = pattern.ShallowClone();
                            patternInstances[p] = pattern;
                        }
                    }

                    instanceLengthMap[pattern] = Tuple.Create(groove, groovePadMode);
                }
            }

            InvalidateCumulativePatternCache();
        }

        // Inputs are absolute note indices from beginning of song.
        public void DeleteNotesBetween(int minFrame, int maxFrame, bool preserveFx = false)
        {
            var minLocation = NoteLocation.FromAbsoluteNoteIndex(Song, minFrame);
            var maxLocation = NoteLocation.FromAbsoluteNoteIndex(Song, maxFrame);

            if (minLocation.PatternIndex == maxLocation.PatternIndex)
            {
                if (minLocation.PatternIndex < song.Length)
                {
                    var pattern = patternInstances[minLocation.PatternIndex];
                    if (pattern != null)
                    {
                        pattern.DeleteNotesBetween(minLocation.NoteIndex, maxLocation.NoteIndex, preserveFx);
                    }
                }
            }
            else
            {
                for (int p = minLocation.PatternIndex; p <= maxLocation.PatternIndex && p < song.Length; p++)
                {
                    var pattern = patternInstances[p];

                    if (pattern != null)
                    {
                        if (p == minLocation.PatternIndex)
                        {
                            pattern.DeleteNotesBetween(minLocation.NoteIndex, Pattern.MaxLength, preserveFx);
                        }
                        else if (p == maxLocation.PatternIndex)
                        {
                            pattern.DeleteNotesBetween(0, maxLocation.NoteIndex, preserveFx);
                        }
                        else
                        {
                            if (preserveFx)
                                pattern.DeleteNotesBetween(0, Pattern.MaxLength, true);
                            else
                                pattern.DeleteAllNotes();
                        }
                    }
                }
            }

            InvalidateCumulativePatternCache(minLocation.PatternIndex, maxLocation.PatternIndex);
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
                pat.Color = Theme.RandomCustomColor();
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
            if (string.IsNullOrEmpty(name))
                return false;

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

        // TODO : We should not reference Settings from here.
        public string GenerateUniquePatternName(string baseName = null)
        {
            if (baseName == null)
                baseName = Settings.PatternNamePrefix;

            var numberFormat = $"D{Settings.PatternNameNumDigits}";

            for (int i = 1; ; i++)
            {
                string name = baseName + i.ToString(numberFormat);
                if (IsPatternNameUnique(name))
                {
                    return name;
                }
            }
        }

        // TODO : We should not reference Settings from here.
        public string GenerateUniquePatternNameSmart(string oldName)
        {
            int firstDigit;

            for (firstDigit = oldName.Length - 1; firstDigit >= 0; firstDigit--)
            {
                if (!Utils.IsAsciiDigit(oldName[firstDigit]))
                    break;
            }

            // Name doesnt end with a number.
            if (firstDigit == oldName.Length - 1)
            {
                if (!oldName.EndsWith(" ") && Settings.PatternNamePrefix.EndsWith(" "))
                    oldName += " ";
                return GenerateUniquePatternName(oldName);
            }
            else
            {
                firstDigit++;

                var number = int.Parse(oldName.Substring(firstDigit)) + 1;
                var baseName = oldName.Substring(0, firstDigit);
                var numberFormat = $"D{Settings.PatternNameNumDigits}";

                for (; ; number++)
                {
                    var newName = baseName + number.ToString(numberFormat);

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

        public static void GetShiftsForType(int type, int numN163Channels, out int pitchShift, out int slideShift)
        {
            if (type >= ChannelType.Vrc7Fm1 && type <= ChannelType.Vrc7Fm6)
            {
                // VRC7 has large pitch values
                slideShift = 3;
                pitchShift = 3;
            }
            else if (type >= ChannelType.EPSMFm1 && type <= ChannelType.EPSMFm6)
            {
                // EPSM has large pitch values
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
            else if (type == ChannelType.Noise)
            {
                slideShift = -4;
                pitchShift = 0;
            }
            else
            {
                // For most channels, we have 1 bit of fraction to better handle slopes.
                slideShift = -1;
                pitchShift = 0;
            }
        }

        // Duration in number of notes, simply to draw in the piano roll.
        public int GetSlideNoteDuration(NoteLocation location)
        {
            Debug.Assert(GetNoteAt(location).IsMusical);
            FindNextNoteForSlide(location, 256, out var nextLocation, true); // 256 is kind of arbitrary. 
            return Song.CountNotesBetween(location, nextLocation);
        }

        // Duration in number of notes, simply to draw in the piano roll.
        public int GetVolumeSlideDuration(NoteLocation location)
        {
            Debug.Assert(GetNoteAt(location).HasVolume);
            FindNextNoteForVolumeSlide(location, 256, out var nextLocation); // 256 is kind of arbitrary. 
            return Song.CountNotesBetween(location, nextLocation);
        }

        public Note GetNoteAt(NoteLocation location)
        {
            if (location.PatternIndex < song.Length)
            {
                var pattern = patternInstances[location.PatternIndex];
                if (pattern != null && pattern.Notes.ContainsKey(location.NoteIndex))
                    return pattern.Notes[location.NoteIndex];
            }

            return null;
        }

        public SparseChannelNoteIterator GetSparseNoteIterator(NoteLocation start, NoteLocation end, NoteFilter filter = NoteFilter.Musical | NoteFilter.Stop | NoteFilter.EffectCutDelay)
        {
            return new SparseChannelNoteIterator(this, start, end, filter);
        }

        public bool ComputeSlideNoteParams(Note note, NoteLocation location, int famitrackerSpeed, ushort[] noteTable, bool pal, bool applyShifts, out int pitchDelta, out int stepSize, out float stepSizeFloat)
        {
            Debug.Assert(note.IsMusical);

            var slideShift = 0;

            if (applyShifts)
                GetShiftsForType(type, song.Project.ExpansionNumN163Channels, out _, out slideShift);

            // Noise is special, no periods.
            if (type == ChannelType.Noise)
                pitchDelta = note.Value - note.SlideNoteTarget;
            else
                pitchDelta = noteTable[note.Value] - noteTable[note.SlideNoteTarget];

            if (pitchDelta != 0)
            {
                pitchDelta = slideShift < 0 ? (pitchDelta << -slideShift) : (pitchDelta >> slideShift);

                // Find the next note to calculate the slope.
                FindNextNoteForSlide(location, 256, out var nextLocation, false); // 256 is kind of arbitrary. 

                // Approximate how many frames separates these 2 notes.
                var frameCount = 0.0f;
                if (location != nextLocation)
                {
                    // Take delayed notes/cuts into account.
                    var delayFrames = -(note.HasNoteDelay ? note.NoteDelay : 0);
                    if (Song.UsesFamiTrackerTempo)
                    {
                        var nextNote = GetNoteAt(nextLocation);
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

                    frameCount = Song.CountFramesBetween(location, nextLocation, famitrackerSpeed, pal) + delayFrames;
                }
                else
                {
                    Debug.Assert(note.HasCutDelay && Song.UsesFamiTrackerTempo);

                    // Slide note starts and end on same note, this mean we have a delayed cut.
                    frameCount = note.HasCutDelay ? note.CutDelay : 0;
                }

                // Compute slide params.
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

        public bool ComputeVolumeSlideNoteParams(Note note, NoteLocation location, int famitrackerSpeed, bool pal, out int stepSize, out float stepSizeFloat)
        {
            Debug.Assert(note.HasVolumeSlide);

            var volumeDelta = note.Volume - note.VolumeSlideTarget;

            if (volumeDelta != 0)
            {
                // Find the next note to calculate the slope.
                FindNextNoteForVolumeSlide(location, 256, out var nextLocation); // 256 is kind of arbitrary. 

                // Approximate how many frames separates these 2 notes.
                var delayFrames = -(note.HasNoteDelay ? note.NoteDelay : 0);
                if (Song.UsesFamiTrackerTempo)
                {
                    var nextNote = GetNoteAt(nextLocation);
                    if (nextNote != null && nextNote.HasNoteDelay)
                        delayFrames += nextNote.NoteDelay;
                }

                var frameCount = Song.CountFramesBetween(location, nextLocation, famitrackerSpeed, pal) + delayFrames;
                var volumeDeltaUnshifted = volumeDelta;

                volumeDelta <<= 4;

                // Compute slide params.
                var absStepPerFrame = Math.Abs(volumeDelta) / Math.Max(1, frameCount);

                // SMMMFFFF : We have 4-bits of fraction for volume slides. 
                stepSize = Utils.Clamp((int)Math.Ceiling(absStepPerFrame) * -Math.Sign(volumeDelta), sbyte.MinValue, sbyte.MaxValue);
                stepSizeFloat = volumeDeltaUnshifted / (float)Math.Max(1, frameCount);

                return true;
            }
            else
            {
                stepSize = 0;
                stepSizeFloat = 0.0f;

                return false;
            }
        }

        public Note FindPatternFirstMusicalNote(int patternIndex)
        {
            var pattern = PatternInstances[patternIndex];
            if (pattern != null)
            {
                foreach (var note in pattern.Notes.Values)
                {
                    if (note != null && note.IsMusical)
                        return note;
                }
            }

            return null;
        }

        public int GetCachedLastValidEffectValue(int patternIdx, int effect, out NoteLocation lastLocation)
        {
            if (patternIdx >= 0)
            {
                ConditionalUpdateCumulativeCache(patternIdx);

                var cache = patternCache[patternIdx];
                if ((cache.lastEffectMask & (1 << effect)) != 0)
                {
                    lastLocation = cache.lastEffectLocation[effect];
                    return cache.lastEffectValues[effect];
                }
            }

            lastLocation = NoteLocation.Invalid;
            return Note.GetEffectDefaultValue(song, effect);
        }

        public int GetLastEffectValue(NoteLocation location, int effect)
        {
            var lastLocation = GetLastEffectLocation(location, effect);

            if (lastLocation.IsValid)
            {
                return GetNoteAt(lastLocation).GetEffectValue(effect);
            }
            else
            {
                return Note.GetEffectDefaultValue(song, effect);
            }
        }

        public NoteLocation GetLastEffectLocation(NoteLocation location, int effect)
        {
            var effectLocation = NoteLocation.Invalid;

            // First look in the current pattern.
            for (var it = GetSparseNoteIterator(new NoteLocation(location.PatternIndex, 0), location, Note.GetFilterForEffect(effect)); !it.Done; it.Next())
            {
                if (it.Note != null && it.Note.HasValidEffectValue(effect))
                    effectLocation = it.Location;
            }

            // Go back to previous is we didnt find anything.
            if (!effectLocation.IsValid)
            {
                GetCachedLastValidEffectValue(location.PatternIndex - 1, effect, out effectLocation);
            }

            return effectLocation;
        }

        public NoteLocation GetCachedLastMusicalNoteWithAttackLocation(int patternIdx)
        {
            if (patternIdx >= 0)
            {
                ConditionalUpdateCumulativeCache(patternIdx);
                return patternCache[patternIdx].lastNoteLocation;
            }

            return NoteLocation.Invalid;
        }

        public int GetCachedFirstNoteIndex(int patternIdx)
        {
            if (patternIdx >= 0)
            {
                ConditionalUpdateCumulativeCache(patternIdx);
                return patternCache[patternIdx].firstNoteIndex;
            }

            return -1;
        }

        public int GetCachedFirstVolumeIndex(int patternIdx)
        {
            if (patternIdx >= 0)
            {
                ConditionalUpdateCumulativeCache(patternIdx);
                return patternCache[patternIdx].firstVolumeIndex;
            }

            return -1;
        }

        public void InvalidateCumulativePatternCache()
        {
            maxValidCacheIndex = -1;
        }

        public void InvalidateCumulativePatternCache(int startPatternIdx, int endPatternIdx)
        {
            for (int idx = startPatternIdx; idx <= endPatternIdx && idx < Song.MaxLength && maxValidCacheIndex != -1; idx++)
            {
                var pattern = patternInstances[idx];
                if (pattern != null)
                {
                    InvalidateCumulativePatternCache(pattern);
                }
            }
        }

        public void InvalidateCumulativePatternCache(Pattern pattern)
        {
            Debug.Assert(pattern != null && patterns.Contains(pattern));

            for (int i = 0; i <= maxValidCacheIndex; i++)
            {
                if (patternInstances[i] == pattern)
                {
                    maxValidCacheIndex = Math.Min(i - 1, maxValidCacheIndex);
                    return;
                }
            }
        }

        public void ConditionalUpdateCumulativeCache(int patternIndex)
        {
            if (maxValidCacheIndex >= patternIndex)
                return;

            // Update from the last valid pattern until now.
            for (int p = maxValidCacheIndex + 1; p <= patternIndex; p++)
            {
                var cache = patternCache[p];

                // If we are not at the start, start with the state of the previous
                // pattern cache.
                if (p > 0)
                    cache.CopyFrom(patternCache[p - 1]);
                else
                    cache.Invalidate();

                var pattern = patternInstances[p];
                if (pattern == null)
                    continue;

                var patternLength = Song.GetPatternLength(p);

                foreach (var kv in pattern.Notes)
                {
                    var time = kv.Key;
                    var note = kv.Value;

                    if (time >= patternLength)
                        break;

                    // Cant keep this assert. We sometimes will go through here 
                    // when using simple notes (NSF export and other formats).
                    // TODO : Add a project flag to tell us if we are in compound
                    // or simple notes.

                    // Debug.Assert(!note.IsRelease);

                    if ((note.IsStop || note.IsMusical) && cache.firstNoteIndex < 0)
                    {
                        cache.firstNoteIndex = time;
                    }

                    if (note.HasVolume && cache.firstVolumeIndex < 0)
                    {
                        cache.firstVolumeIndex = time;
                    }

                    if (note.IsMusical)
                    {
                        // If its the first note ever, consider it has an attack.
                        if (!cache.lastNoteLocation.IsValid || note.HasAttack)
                            cache.lastNoteLocation = new NoteLocation(p, time);
                    }

                    for (int j = 0; j < Note.EffectCount; j++)
                    {
                        if (note.HasValidEffectValue(j))
                        {
                            cache.lastEffectMask |= (1 << j);
                            cache.lastEffectValues[j] = note.GetEffectValue(j);
                            cache.lastEffectLocation[j] = new NoteLocation(p, time);
                        }
                    }
                }
            }

            maxValidCacheIndex = patternIndex;
        }

        public int GetDistanceToNextNote(NoteLocation location, NoteFilter filter = NoteFilter.CutDurationMask, bool endAfterCutDelay = true)
        {
            var startLocation = location;

            song.AdvanceNumberOfNotes(ref location, 1);
            if (location.PatternIndex < patternInstances.Length)
            {
                var pattern = patternInstances[location.PatternIndex];

                // Look in current pattern.
                if (pattern != null)
                {
                    var idx = pattern.BinarySearchList(pattern.Notes.Keys, location.NoteIndex, true);

                    if (idx >= 0)
                    {
                        for (; idx < pattern.Notes.Values.Count; idx++)
                        {
                            var note = pattern.Notes.Values[idx];
                            if (note.MatchesFilter(filter))
                            {
                                location.NoteIndex = pattern.Notes.Keys[idx];
                                return Song.CountNotesBetween(startLocation, location) + (endAfterCutDelay && note.HasCutDelay ? 1 : 0);
                            }
                        }
                    }
                }

                // Then look in the following patterns using the cache.
                for (var p = location.PatternIndex + 1; p < song.Length; p++)
                {
                    var firstNoteIdx = GetCachedFirstNoteIndex(p);
                    if (firstNoteIdx >= 0)
                    {
                        var note = patternInstances[p].Notes[firstNoteIdx];
                        location.PatternIndex = p;
                        location.NoteIndex = firstNoteIdx;
                        return Song.CountNotesBetween(startLocation, location) + (endAfterCutDelay && note.HasCutDelay ? 1 : 0);
                    }
                }
            }

            return Song.CountNotesBetween(startLocation, Song.EndLocation);
        }

        public bool FindNextNoteForSlide(NoteLocation location, int maxNotes, out NoteLocation nextNoteLocation, bool endAfterCutDelay)
        {
            nextNoteLocation = location;

            var patternLength = song.GetPatternLength(location.PatternIndex);
            var pattern = patternInstances[location.PatternIndex];

            Debug.Assert(pattern.Notes.ContainsKey(location.NoteIndex));
            Debug.Assert(pattern.Notes[location.NoteIndex].IsMusical);

            var note = pattern.Notes[location.NoteIndex];

            if (note.HasCutDelay)
            {
                return true;
            }

            NoteLocation maxLocation = location;
            Song.AdvanceNumberOfNotes(ref maxLocation, maxNotes);

            int duration = GetDistanceToNextNote(location, NoteFilter.CutDurationMask, endAfterCutDelay);

            duration = duration < 0 ? maxNotes : Math.Min(duration, maxNotes);
            duration = Math.Min(note.Duration, duration);

            Song.AdvanceNumberOfNotes(ref nextNoteLocation, duration);

            return true;
        }

        public bool FindNextNoteForVolumeSlide(NoteLocation location, int maxNotes, out NoteLocation nextNoteLocation)
        {
            var startPatternIndex = location.PatternIndex;
            var pattern = patternInstances[startPatternIndex];

            Debug.Assert(pattern.Notes.ContainsKey(location.NoteIndex));
            Debug.Assert(pattern.Notes[location.NoteIndex].HasVolume);

            NoteLocation maxLocation = location;
            Song.AdvanceNumberOfNotes(ref maxLocation, maxNotes);
            song.AdvanceNumberOfNotes(ref location, 1);

            // Look in current pattern (if we werent the very last note)..
            if (location.PatternIndex == startPatternIndex)
            {
                var idx = pattern.BinarySearchList(pattern.Notes.Keys, location.NoteIndex, true);

                if (idx >= 0)
                {
                    for (; idx < pattern.Notes.Values.Count; idx++)
                    {
                        var note = pattern.Notes.Values[idx];
                        if (note.MatchesFilter(NoteFilter.EffectVolume))
                        {
                            location.NoteIndex = pattern.Notes.Keys[idx];
                            nextNoteLocation = NoteLocation.Min(maxLocation, location);
                            return true;
                        }
                    }
                }
            }

            // Then look in the following patterns using the cache.
            for (var p = startPatternIndex + 1; p < song.Length; p++)
            {
                var firstNoteIdx = GetCachedFirstVolumeIndex(p);
                if (firstNoteIdx >= 0)
                {
                    var note = patternInstances[p].Notes[firstNoteIdx];
                    nextNoteLocation.PatternIndex = p;
                    nextNoteLocation.NoteIndex = firstNoteIdx;
                    nextNoteLocation = NoteLocation.Min(maxLocation, nextNoteLocation);
                    return true;
                }
            }

            nextNoteLocation = NoteLocation.Min(maxLocation, song.EndLocation);
            return true;
        }

        // If note value is negative, will return any note.
        public Note FindMusicalNoteAtLocation(ref NoteLocation location, int noteValue)
        {
            var startLocation = new NoteLocation(location.PatternIndex, 0);
            var lastNoteValue = Note.MusicalNoteC4;

            // Start search at the last note with an attack.
            var lastNoteLocation = GetCachedLastMusicalNoteWithAttackLocation(location.PatternIndex - 1);
            if (lastNoteLocation.IsValid)
            {
                lastNoteValue = GetNoteAt(lastNoteLocation).Value;
                startLocation = lastNoteLocation;
            }

            var loc0 = startLocation;
            var loc1 = location;

            for (var it = GetSparseNoteIterator(loc0, loc1); !it.Done; it.Next())
            {
                var note = it.Note;
                var duration = Math.Min(note.Duration, it.DistanceToNextCut);

                if (note.IsMusical)
                    lastNoteValue = note.Value;

                if (note.IsMusical || note.IsStop)
                {
                    var distance = Song.CountNotesBetween(it.Location, location);

                    if (distance < it.DistanceToNextCut &&
                        distance < note.Duration)
                    {
                        if (noteValue < 0)
                        {
                            location = it.Location;
                            return note;
                        }

                        var noteValueToCompare = note.IsStop ? lastNoteValue : note.Value;
                        if (lastNoteValue == noteValue)
                        {
                            location = it.Location;
                            return note;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }

            return null;
        }

        public void ClearPatternsInstancesPastSongLength()
        {
            for (int i = song.Length; i < patternInstances.Length; i++)
                patternInstances[i] = null;
        }

        public void DeleteEmptyNotes()
        {
            foreach (var pattern in patterns)
                pattern.DeleteEmptyNotes();
        }

        public void RemoveDpcmNotesWithoutMapping()
        {
            if (IsDpcmChannel)
            {
                foreach (var pattern in patterns)
                    pattern.RemoveDpcmNotesWithoutMapping();
            }
        }

        public bool HasAnyPatternInstances
        {
            get
            {
                for (int i = 0; i < song.Length; i++)
                {
                    if (patternInstances[i] != null)
                        return true;
                }

                return false;
            }
        }

        public void SetNoteDurationToMaximumLength(NoteLocation start, NoteLocation end)
        {
            var maxNoteLengths = new Dictionary<Note, int>();

            for (var it = GetSparseNoteIterator(start, end); !it.Done; it.Next())
            {
                if (it.Note.IsMusical)
                {
                    var duration = Math.Min(it.Note.Duration, it.DistanceToNextCut);

                    if (maxNoteLengths.TryGetValue(it.Note, out var maxDuration))
                        maxNoteLengths[it.Note] = Math.Max(maxDuration, duration);
                    else
                        maxNoteLengths[it.Note] = duration;
                }
            }

            foreach (var kv in maxNoteLengths)
            {
                kv.Key.Duration = kv.Value;
            }

            InvalidateCumulativePatternCache();
        }

        public void SetNoteDurationToMaximumLength()
        {
            SetNoteDurationToMaximumLength(Song.StartLocation, Song.EndLocation);
        }

        public unsafe static int ChannelTypeToIndex(int type, int activeExpansions, int numN163Channels)
        {
            if (type < ChannelType.ExpansionAudioStart)
                return type;

            var exp = ChannelType.GetExpansionTypeForChannelType(type);
            var idx = 5 + ChannelType.GetExpansionChannelIndexForChannelType(type);

            if (exp == ExpansionType.Vrc6) return idx;
            if ((activeExpansions & ExpansionType.Vrc6Mask) != 0) idx += 3;
            if (exp == ExpansionType.Vrc7) return idx;
            if ((activeExpansions & ExpansionType.Vrc7Mask) != 0) idx += 6;
            if (exp == ExpansionType.Fds)  return idx; 
            if ((activeExpansions & ExpansionType.FdsMask)  != 0) idx += 1;
            if (exp == ExpansionType.Mmc5) return idx;
            if ((activeExpansions & ExpansionType.Mmc5Mask) != 0) idx += 2; // (We never use the DPCM)
            if (exp == ExpansionType.N163) return idx;
            if ((activeExpansions & ExpansionType.N163Mask) != 0) idx += numN163Channels;
            if (exp == ExpansionType.S5B) return idx;
            if ((activeExpansions & ExpansionType.S5BMask) != 0) idx += 3;

            Debug.Assert((activeExpansions & ExpansionType.EPSMMask) != 0);

            return idx; 
        }

#if DEBUG
        public void ValidateIntegrity(Song song, Dictionary<int, object> idMap)
        {
            Debug.Assert(this == song.GetChannelByType(type));
            Debug.Assert(this.song == song);

            foreach (var inst in patternInstances)
                Debug.Assert(inst == null || patterns.Contains(inst));
            foreach (var pat in patterns)
                pat.ValidateIntegrity(this, idMap);

            for (int i = 0; i <= maxValidCacheIndex; i++)
            {
                var cache = patternCache[i];
                if (cache.lastNoteLocation.IsValid)
                {
                    var note = GetNoteAt(cache.lastNoteLocation);
                    Debug.Assert(note.IsMusical); // We cant check if it has an attack since the first note of the entire song is assumed to have one.
                }
                if (cache.firstNoteIndex >= 0)
                {
                    var note = patternInstances[i].Notes[cache.firstNoteIndex];
                    Debug.Assert(note.IsMusical || note.IsStop || note.HasCutDelay);
                }
            }

            var oldMaxValidCacheIndex = maxValidCacheIndex;
            var patternCacheCopy = new PatternCumulativeCache[maxValidCacheIndex + 1];

            for (int i = 0; i <= maxValidCacheIndex; i++)
            {
                patternCacheCopy[i] = new PatternCumulativeCache();
                patternCacheCopy[i].CopyFrom(patternCache[i]);
                patternCacheCopy[i].firstNoteIndex = patternCache[i].firstNoteIndex;
                patternCacheCopy[i].firstVolumeIndex = patternCache[i].firstVolumeIndex;
            }

            InvalidateCumulativePatternCache();
            ConditionalUpdateCumulativeCache(oldMaxValidCacheIndex);

            for (int i = 0; i <= maxValidCacheIndex; i++)
                Debug.Assert(patternCacheCopy[i].IsEqual(patternCache[i]));
        }
#endif

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

            InvalidateCumulativePatternCache();
        }

        // Converts old (pre-FamiStudio 3.0.0) release/stop notes to compound notes that have their 
        // own release point + duration. The piano roll can only handle compound notes and eventually
        // the entire app will be converted.
        public void ConvertToCompoundNotes()
        {
            // Reset all durations to zero.
            foreach (var pattern in patterns)
            {
                foreach (var kv in pattern.Notes)
                {
                    var note = kv.Value;

                    if (note.IsMusical)
                        note.Duration = 0;
                    else if (note.IsStop || note.IsRelease)
                        note.Duration = 1;
                }
            }

            var l0 = NoteLocation.Invalid;
            var n0 = (Note)null;

            // Do a first pass where we handle trivial cases and look for inconsistent durations/releases.
            for (int p1 = 0; p1 < song.Length; p1++)
            {
                var pattern = patternInstances[p1];

                if (pattern == null)
                    continue;

                foreach (var kv in pattern.Notes)
                {
                    var l1 = new NoteLocation(p1, kv.Key);
                    var n1 = kv.Value;

                    if (n1.IsRelease)
                    {
                        // Useless release, discarding.
                        if (n0 == null)
                            continue;

                        var release = song.CountNotesBetween(l0, l1);

                        if (n0.Release == 0)
                            n0.SetReleaseNoDurationCheck(release);
                        else
                            n0.SetReleaseNoDurationCheck(Math.Min(release, n0.Duration)); // Keep the minimum.
                    }
                    else if (n1.IsStop || n1.IsMusical)
                    {
                        if (n0 != null)
                        {
                            var duration = (ushort)song.CountNotesBetween(l0, l1);

                            if (n0.Duration == 0)
                                n0.Duration = duration;
                            else
                                n0.Duration = Math.Max(duration, n0.Duration);
                        }

                        if (n1.IsStop)
                        {
                            n0 = null;
                        }
                        else
                        {
                            l0 = l1;
                            n0 = n1;
                        }
                    }
                }
            }

            // Last note.
            if (n0 != null)
            {
                var duration = song.CountNotesBetween(l0, new NoteLocation(song.Length, 0));

                if (n0.Duration == 0)
                    n0.Duration = duration;
                else
                    n0.Duration = Math.Max(duration, n0.Duration);
            }

            // Do a second pass to handle inconsistent durations and find the stop notes to keep.
            var inconsistentDurationStopNotes = new Dictionary<Note, Pattern>();
            var loopPointStopNote = (Note)null;
            var loopPointStopNotePattern = (Pattern)null;
            var foundMusicalNoteAfterLoopPoint = false;

            l0 = NoteLocation.Invalid;
            n0 = (Note)null;

            for (int p1 = 0; p1 < song.Length; p1++)
            {
                var pattern = patternInstances[p1];

                if (pattern == null)
                    continue;

                foreach (var kv in pattern.Notes)
                {
                    var l1 = new NoteLocation(p1, kv.Key);
                    var n1 = kv.Value;

                    if (n1.IsStop)
                    {
                        if (n0 != null)
                        {
                            Debug.Assert(n0.Duration != 0);

                            var duration = (ushort)song.CountNotesBetween(l0, l1);
                            if (n0.Duration != duration)
                            {
                                inconsistentDurationStopNotes[n1] = pattern;
                            }

                            n0 = null;
                        }
                        if (song.LoopPoint >= 0 && l1.PatternIndex >= song.LoopPoint && !foundMusicalNoteAfterLoopPoint && loopPointStopNote == null)
                        {
                            loopPointStopNote = n1;
                            loopPointStopNotePattern = pattern;
                        }
                    }
                    else if (n1.IsMusical)
                    {
                        l0 = l1;
                        n0 = n1;

                        if (song.LoopPoint >= 0 && l1.PatternIndex >= song.LoopPoint && !foundMusicalNoteAfterLoopPoint)
                        {
                            foundMusicalNoteAfterLoopPoint = true;
                        }
                    }
                }
            }

            // Print messages.
            foreach (var kv in inconsistentDurationStopNotes)
                Log.LogMessage(LogSeverity.Warning, $"Inconsistent note duration, orphan stop note added. (Song={song.Name}, Channel={NameWithExpansion}, Pattern={kv.Value.Name})");

            if (loopPointStopNote != null)
                Log.LogMessage(LogSeverity.Warning, $"Stop note found at beginning of loop point, orphan stop note added. (Song={song.Name}, Channel={NameWithExpansion}, Pattern={loopPointStopNotePattern.Name})");

            // Cleanup.
            foreach (var pattern in patterns)
            {
                foreach (var kv in pattern.Notes)
                {
                    var note = kv.Value;

                    if ((note.IsStop || note.IsRelease) && !inconsistentDurationStopNotes.ContainsKey(note) && note != loopPointStopNote)
                    {
                        note.Value = Note.NoteInvalid;
                        note.Duration = 0;
                    }

                    note.ClearReleaseIfPastDuration();
                }
            }

            DeleteEmptyNotes();
            InvalidateCumulativePatternCache();
            DeleteUnusedPatterns();
        }

        // Converts compound notes to old (pre-FamiStudio 3.0.0) release/stop notes. This is
        // needed since some exporter (FamiTracker, etc.) have not been yet ported to handle
        // compound notes. This is to help the migration.
        public void ConvertToSimpleNotes()
        {
            // Record all combinations of release/stops.
            var patternReleaseStops = new int[song.Length, 2];
            for (int i = 0; i < song.Length; i++)
            {
                patternReleaseStops[i, 0] = -1;
                patternReleaseStops[i, 1] = -1;
            }

            var releasesToCreate = new HashSet<NoteLocation>();
            var stopsToCreate = new HashSet<NoteLocation>();

            var loc0 = new NoteLocation(0, 0);
            var loc1 = new NoteLocation(Song.Length, 0);

            for (var it = GetSparseNoteIterator(loc0, loc1, NoteFilter.Musical | NoteFilter.Stop); !it.Done; it.Next())
            {
                var note = it.Note;

                Debug.Assert(note.IsMusical || note.IsStop);

                if (it.Note.IsMusical)
                {
                    var duration = Math.Min(note.Duration, it.DistanceToNextCut);

                    // Find where the release is
                    if (note.HasRelease && note.Release < duration)
                    {
                        var releaseLocation = it.Location;
                        Song.AdvanceNumberOfNotes(ref releaseLocation, note.Release);

                        if (releaseLocation < loc1)
                        {
                            // Does it fall in a different pattern?
                            if (releaseLocation < loc1 && releaseLocation.PatternIndex != it.Location.PatternIndex)
                            {
                                // Create missing pattern.
                                var pattern = patternInstances[releaseLocation.PatternIndex];
                                if (pattern == null)
                                    pattern = CreatePatternAndInstance(releaseLocation.PatternIndex);

                                Debug.Assert(patternReleaseStops[releaseLocation.PatternIndex, 0] == -1);
                                patternReleaseStops[releaseLocation.PatternIndex, 0] = releaseLocation.NoteIndex;
                            }

                            releasesToCreate.Add(releaseLocation);
                        }
                    }

                    // Find where the notes ends.
                    var stopLocation = it.Location;
                    Song.AdvanceNumberOfNotes(ref stopLocation, duration);

                    if (stopLocation < loc1)
                    {
                        // Does it fall in a different pattern?
                        if (stopLocation.PatternIndex != it.Location.PatternIndex)
                        {
                            // Create missing pattern.
                            var pattern = patternInstances[stopLocation.PatternIndex];
                            if (pattern == null)
                                pattern = CreatePatternAndInstance(stopLocation.PatternIndex);

                            Debug.Assert(patternReleaseStops[stopLocation.PatternIndex, 1] == -1);
                            patternReleaseStops[stopLocation.PatternIndex, 1] = stopLocation.NoteIndex;
                        }

                        stopsToCreate.Add(stopLocation);
                    }
                }
            }

            // Duplicate pattern as needed.
            var visitedPatterns = new HashSet<Pattern>();
            var patternStopReleaseMap = new Dictionary<Tuple<Pattern, int, int>, Pattern>();

            for (int i = 0; i < song.Length; i++)
            {
                if (patternReleaseStops[i, 0] >= 0 ||
                    patternReleaseStops[i, 1] >= 0)
                {
                    var pattern = patternInstances[i];
                    var key = new Tuple<Pattern, int, int>(pattern, patternReleaseStops[i, 0], patternReleaseStops[i, 1]);

                    // First time we see a pattern, just use it.
                    if (!visitedPatterns.Contains(pattern))
                    {
                        visitedPatterns.Add(pattern);
                        patternStopReleaseMap[key] = pattern;
                    }
                    else
                    {
                        // Otherwise create a unique pattern for each combination of release/stop.
                        if (patternStopReleaseMap.TryGetValue(key, out var patternToCopy))
                        {
                            patternInstances[i] = patternToCopy;
                        }
                        else
                        {
                            Log.LogMessage(LogSeverity.Debug, $"Duplicating pattern {pattern.Name} in song {song.Name} since it has inconsistent previous notes.");
                            patternInstances[i] = pattern.ShallowClone();
                            patternStopReleaseMap.Add(key, patternInstances[i]);
                        }
                    }
                }
            }

            // Create release notes.
            foreach (var releaseLocation in releasesToCreate)
            {
                var pattern = patternInstances[releaseLocation.PatternIndex];
                var releaseNote = pattern.GetOrCreateNoteAt(releaseLocation.NoteIndex);
                if (!releaseNote.IsValid)
                {
                    releaseNote.Value = Note.NoteRelease;
                    releaseNote.Duration = 1;
                }
            }

            // Create stop notes.
            foreach (var stopLocation in stopsToCreate)
            {
                var pattern = patternInstances[stopLocation.PatternIndex];
                var stopNote = pattern.GetOrCreateNoteAt(stopLocation.NoteIndex);
                if (!stopNote.IsValid)
                {
                    stopNote.Value = Note.NoteStop;
                    stopNote.Duration = 1;
                }
            }

            InvalidateCumulativePatternCache();
            DeleteUnusedPatterns();
        }

        // This should mimic the behavior of the sound engine, only these instrument
        // have the proper logic to reload an instrument when there is no attack.
        public static bool CanDisableAttack(int channelType, Instrument i1, Instrument i2)
        {
            if (i1 == null || i2 == null)
            {
                return false;
            }

            // TODO : Maybe no attack on DPCM could mean switch sample without restting DAC? 
            if (ChannelType.IsDpcmChannel(channelType))
            {
                return false;
            }

            if (i1 == i2)
            {
                return true;
            }

            Debug.Assert(i1.Expansion == i2.Expansion);

            if (ChannelType.IsN163Channel(channelType))
            {
                // N163 instruments is a bit to complex to allow disabling
                // attack on instrument switch.
                return false;
            }
            else
            {
                // For regular and FM instrument, as long as envelopes matches, it 
                // garantees we wont have out-of-bounds pointers when switching envelopes.
                return Instrument.AllEnvelopesAreIdentical(i1, i2);
            }
        }

        public void Serialize(ProjectBuffer buffer)
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
                pattern.Serialize(buffer);

            for (int i = 0; i < patternInstances.Length; i++)
                buffer.Serialize(ref patternInstances[i], this);

            if (buffer.IsReading && !buffer.IsForUndoRedo)
                ClearPatternsInstancesPastSongLength();

            if (buffer.IsReading)
                InvalidateCumulativePatternCache();
        }

        private class PatternCumulativeCache
        {
            // Index of the first note in the pattern, -1 if none.
            public int firstNoteIndex;
            public int firstVolumeIndex;

            // Last note that had an attack before the end of the pattern.
            public NoteLocation lastNoteLocation;

            // Cumulative effect values from all the previous patterns.
            public int lastEffectMask;
            public int[] lastEffectValues = new int[Note.EffectCount];
            public NoteLocation[] lastEffectLocation = new NoteLocation[Note.EffectCount];

            public PatternCumulativeCache()
            {
                Invalidate();
            }

            public void CopyFrom(PatternCumulativeCache other)
            {
                // Not copying first note as this is note a cumulative thing.
                lastNoteLocation = other.lastNoteLocation;
                lastEffectMask = other.lastEffectMask;
                firstNoteIndex = -1;
                firstVolumeIndex = -1;
                Array.Copy(other.lastEffectValues, lastEffectValues, lastEffectValues.Length);
                Array.Copy(other.lastEffectLocation, lastEffectLocation, lastEffectLocation.Length);
            }

            public bool IsEqual(PatternCumulativeCache other)
            {
                bool equal = lastNoteLocation == other.lastNoteLocation &&
                    lastEffectMask == other.lastEffectMask &&
                    firstNoteIndex == other.firstNoteIndex &&
                    firstVolumeIndex == other.firstVolumeIndex;

                for (int i = 0; i < Note.EffectCount; i++)
                {
                    if ((lastEffectMask & (1 << i)) != 0)
                    {
                        equal &= (lastEffectValues[i] == other.lastEffectValues[i]);
                        equal &= (lastEffectLocation[i] == other.lastEffectLocation[i]);
                    }
                }

                return equal;
            }

            public void Invalidate()
            {
                firstNoteIndex = -1;
                firstVolumeIndex = -1;
                lastNoteLocation = NoteLocation.Invalid;
                lastEffectMask = 0;
            }
        };
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
        public const int Mmc5Dpcm = 17; // Rename me
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
        public const int EPSMSquare1 = 29;
        public const int EPSMSquare2 = 30;
        public const int EPSMSquare3 = 31;
        public const int EPSMFm1 = 32;
        public const int EPSMFm2 = 33;
        public const int EPSMFm3 = 34;
        public const int EPSMFm4 = 35;
        public const int EPSMFm5 = 36;
        public const int EPSMFm6 = 37;
        public const int EPSMrythm1 = 38;
        public const int EPSMrythm2 = 39;
        public const int EPSMrythm3 = 40;
        public const int EPSMrythm4 = 41;
        public const int EPSMrythm5 = 42;
        public const int EPSMrythm6 = 43;
        public const int Count = 44;

        // Use these to display to user
        public static LocalizedString[] LocalizedNames = new LocalizedString[Count];

        // Use these to save in files, etc.
        public static readonly string[] InternalNames =
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
            "EPSMSquare1", // EPSM
            "EPSMSquare2", // EPSM
            "EPSMSquare3", // EPSM
            "EPSMFM1", // EPSM
            "EPSMFM2", // EPSM
            "EPSMFM3", // EPSM
            "EPSMFM4", // EPSM
            "EPSMFM5", // EPSM
            "EPSMFM6", // EPSM
            "EPSMRythm1", // EPSM
            "EPSMRythm2", // EPSM
            "EPSMRythm3", // EPSM
            "EPSMRythm4", // EPSM
            "EPSMRythm5", // EPSM
            "EPSMRythm6", // EPSM
        };

        // TODO: This is really UI specific, move somewhere else...
        public static readonly string[] Icons =
        {
            "ChannelSquare",
            "ChannelSquare",
            "ChannelTriangle",
            "ChannelNoise",
            "ChannelDPCM",
            "ChannelSquare",
            "ChannelSquare",
            "ChannelSaw",
            "ChannelFM",
            "ChannelFM",
            "ChannelFM",
            "ChannelFM",
            "ChannelFM",
            "ChannelFM",
            "ChannelWaveTable",
            "ChannelSquare",
            "ChannelSquare",
            "ChannelDPCM",
            "ChannelWaveTable",
            "ChannelWaveTable",
            "ChannelWaveTable",
            "ChannelWaveTable",
            "ChannelWaveTable",
            "ChannelWaveTable",
            "ChannelWaveTable",
            "ChannelWaveTable",
            "ChannelSquare",
            "ChannelSquare",
            "ChannelSquare",
            "ChannelSquare",
            "ChannelSquare",
            "ChannelSquare",
            "ChannelFM",
            "ChannelFM",
            "ChannelFM",
            "ChannelFM",
            "ChannelFM",
            "ChannelFM",
            "ChannelRythm",
            "ChannelRythm",
            "ChannelRythm",
            "ChannelRythm",
            "ChannelRythm",
            "ChannelRythm",
        };

        public static readonly int[] ExpansionTypes =
        {
            ExpansionType.None,
            ExpansionType.None,
            ExpansionType.None,
            ExpansionType.None,
            ExpansionType.None,
            ExpansionType.Vrc6,
            ExpansionType.Vrc6,
            ExpansionType.Vrc6,
            ExpansionType.Vrc7,
            ExpansionType.Vrc7,
            ExpansionType.Vrc7,
            ExpansionType.Vrc7,
            ExpansionType.Vrc7,
            ExpansionType.Vrc7,
            ExpansionType.Fds,
            ExpansionType.Mmc5,
            ExpansionType.Mmc5,
            ExpansionType.Mmc5,
            ExpansionType.N163,
            ExpansionType.N163,
            ExpansionType.N163,
            ExpansionType.N163,
            ExpansionType.N163,
            ExpansionType.N163,
            ExpansionType.N163,
            ExpansionType.N163,
            ExpansionType.S5B,
            ExpansionType.S5B,
            ExpansionType.S5B,
            ExpansionType.EPSM,
            ExpansionType.EPSM,
            ExpansionType.EPSM,
            ExpansionType.EPSM,
            ExpansionType.EPSM,
            ExpansionType.EPSM,
            ExpansionType.EPSM,
            ExpansionType.EPSM,
            ExpansionType.EPSM,
            ExpansionType.EPSM,
            ExpansionType.EPSM,
            ExpansionType.EPSM,
            ExpansionType.EPSM,
            ExpansionType.EPSM,
            ExpansionType.EPSM,
        };

        public static readonly int[] ExpansionChannelIndex =
        {
            0, // 2A03
            1, // 2A03
            2, // 2A03
            3, // 2A03
            4, // 2A03
            0, // VRC6
            1, // VRC6
            2, // VRC6
            0, // VRC7
            1, // VRC7
            2, // VRC7
            3, // VRC7
            4, // VRC7
            5, // VRC7
            0, // FDS
            0, // MMC5
            1, // MMC5
            2, // MMC5
            0, // N163
            1, // N163
            2, // N163
            3, // N163
            4, // N163
            5, // N163
            6, // N163
            7, // N163
            0, // S5B
            1, // S5B
            2, // S5B
            0, // EPSM
            1, // EPSM
            2, // EPSM
            3, // EPSM
            4, // EPSM
            5, // EPSM
            6, // EPSM
            7, // EPSM
            8, // EPSM
            9, // EPSM
            10, // EPSM
            11, // EPSM
            12, // EPSM
            13, // EPSM
            14 // EPSM
        };

        static ChannelType()
        {
            Localization.LocalizeStatic(typeof(ChannelType));
        }

        public static string GetLocalizedNameWithExpansion(int type)
        {
            var str = LocalizedNames[type].Value;
            if (ExpansionTypes[type] != ExpansionType.None)
                str += $" ({ExpansionType.InternalNames[ExpansionTypes[type]]})"; // Here we use the internal name to keep things short.
            return str;
        }

        public static int GetExpansionTypeForChannelType(int type)
        {
            return ExpansionTypes[type];
        }

        public static int GetExpansionChannelIndexForChannelType(int type)
        {
            return ExpansionChannelIndex[type];
        }

        public static int GetValueForLocalizedName(string str)
        {
            return Array.FindIndex(LocalizedNames, n => n.Value == str);
        }

        public static int GetValueForInternalName(string str)
        {
            return Array.IndexOf(InternalNames, str);
        }

        public static bool IsRegularChannel(int type) => type <= Dpcm;
        public static bool IsFdsChannel(int type) => type == FdsWave;
        public static bool IsN163Channel(int type) => type >= N163Wave1 && type <= N163Wave8;
        public static bool IsVrc6Channel(int type) => type >= Vrc6Square1 && type <= Vrc6Saw;
        public static bool IsVrc7Channel(int type) => type >= Vrc7Fm1 && type <= Vrc7Fm6;
        public static bool IsMmc5Channel(int type) => type >= Mmc5Square1 && type <= Mmc5Dpcm;
        public static bool IsS5BChannel(int type) => type >= S5BSquare1 && type <= S5BSquare3;
        public static bool IsNoiseChannel(int type) => type == Noise;
        public static bool IsDpcmChannel(int type) => type == Dpcm;
        public static bool IsEPSMChannel(int type) => type >= EPSMSquare1 && type <= EPSMrythm6;
        public static bool IsEPSMSquareChannel(int type) => type >= EPSMSquare1 && type <= EPSMSquare3;
        public static bool IsEPSMFmChannel(int type) => type >= EPSMFm1 && type <= EPSMFm6;
        public static bool IsEPSMRythmChannel(int type) => type >= EPSMrythm1 && type <= EPSMrythm6;

    }

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
        private NoteFilter filter;
        private int currIdx;
        private int nextIdx;

        public NoteLocation Location => current;
        public NoteLocation NextLocation => next;
        public int PatternIndex => current.PatternIndex;
        public int NoteIndex => current.NoteIndex;

        public Pattern Pattern => pattern;
        public Note Note => note;

        public int DistanceToNextNote => channel.Song.CountNotesBetween(current, next);

        // Same as DistanceToNextNote, but takes delayed cuts into accounts.
        public int DistanceToNextCut
        {
            get
            {
                // Current note has a cut delay, length = 1.
                if (note != null && note.HasCutDelay)
                    return 1;

                if (next.IsValid && next.IsInSong(channel.Song))
                {
                    // Next note has a cut delay, distance + 1.
                    var nextNote = channel.GetNoteAt(next);
                    if (nextNote != null && !nextNote.IsMusicalOrStop && nextNote.HasCutDelay)
                        return DistanceToNextNote + 1;
                }

                return DistanceToNextNote;
            }
        }

        public bool Done => current > end;

        // Iterate from [start, end]
        public SparseChannelNoteIterator(Channel c, NoteLocation start, NoteLocation end, NoteFilter f = NoteFilter.CutDurationMask)
        {
            Debug.Assert(start <= end);

            // Clamp end to end of song.
            if (end.PatternIndex >= c.Song.Length)
            {
                end.PatternIndex = c.Song.Length - 1;
                end.NoteIndex = c.Song.GetPatternLength(end.PatternIndex);
            }

            this.channel = c;
            this.filter = f;
            this.end = end;

            Debug.Assert(start.NoteIndex < c.Song.GetPatternLength(start.PatternIndex));

            // Look forward for a first musical note.
            do
            {
                pattern = channel.PatternInstances[start.PatternIndex];

                if (pattern != null)
                {
                    var patternLen = channel.Song.GetPatternLength(start.PatternIndex);
                    var idx = pattern.BinarySearchList(pattern.Notes.Keys, start.NoteIndex, true);

                    if (idx >= 0)
                    {
                        for (; idx < pattern.Notes.Values.Count; idx++)
                        {
                            if (pattern.Notes.Keys[idx] >= patternLen)
                            {
                                break;
                            }

                            if (pattern.Notes.Values[idx].MatchesFilter(filter))
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
            while (start.PatternIndex <= end.PatternIndex);

            // Done.
            current = end;
            current.PatternIndex++;
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

            Debug.Assert(note.MatchesFilter(filter));

            // Find next note.
            nextIdx = currIdx;

            var patternLen = channel.Song.GetPatternLength(current.PatternIndex);

            // Look in the same pattern.
            while (++nextIdx < pattern.Notes.Values.Count && pattern.Notes.Keys[nextIdx] < patternLen)
            {
                // Only considering musical notes for now.
                if (pattern.Notes.Values[nextIdx].MatchesFilter(filter))
                {
                    next.PatternIndex = current.PatternIndex;
                    next.NoteIndex = pattern.Notes.Keys[nextIdx];
                    return;
                }
            }

            // Next patterns.
            for (var p = current.PatternIndex + 1; p <= end.PatternIndex; p++)
            {
                var pat = channel.PatternInstances[p];
                patternLen = channel.Song.GetPatternLength(p);

                if (pat != null && pat.Notes.Count > 0)
                {
                    nextIdx = 0;

                    do
                    {
                        if (pat.Notes.Keys[nextIdx] >= patternLen)
                        {
                            break;
                        }

                        // Only considering musical notes for now.
                        if (pat.Notes.Values[nextIdx].MatchesFilter(filter))
                        {
                            next.PatternIndex = p;
                            next.NoteIndex = pat.Notes.Keys[nextIdx];
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

        public void Next(NoteFilter f = NoteFilter.Musical)
        {
            do
            {
                Next();
            }
            while (!Done && !Note.MatchesFilter(f));
        }
    }
}
