using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Channels;

namespace FamiStudio
{
    public class Song
    {
        public const int MaxLength       = 256;
        public const int NativeTempoNTSC = 150;
        public const int NativeTempoPAL  = 125;

        private int id;
        private Project project;
        private Channel[] channels;
        private Color color;
        private string folderName;
        private int patternLength = 96;
        private int songLength = 16;
        private int beatLength = 40;
        private string name;
        private int loopPoint = 0;
        private PatternCustomSetting[] patternCustomSettings = new PatternCustomSetting[Song.MaxLength];
        private int[] patternStartNote = new int[Song.MaxLength + 1];

        // This lock is to address the use case where we undo a Song-scope transaction and we end up 
        // recreating all the channels. The SongPlayer/ChannelState may be trying to get the channel
        // with GetChannelByType() which this is happening. Another approach would be to stop the audio
        // every time we undo a Song transaction, but that's super intrusive.
        private object songLock = new object(); 

        // These are specific to FamiTracker tempo mode
        private int famitrackerTempo = 150;
        private int famitrackerSpeed = 6;

        // These are for FamiStudio tempo mode
        private int   noteLength = 10;
        private int[] groove = new[] { 10 };
        private int   groovePaddingMode = GroovePaddingType.Middle;

        public int Id => id;
        public Project Project => project;
        public Channel[] Channels => channels;
        public Color Color { get => color; set => color = value; }
        public string Name { get => name; set => name = value; }
        public int Length { get => songLength; }
        public int PatternLength { get => patternLength; }
        public int BeatLength { get => beatLength; }
        public int[] Groove { get => groove; }
        public int GroovePaddingMode { get => groovePaddingMode; }
        public int LoopPoint { get => loopPoint; }
        public bool UsesFamiStudioTempo => project.UsesFamiStudioTempo;
        public bool UsesFamiTrackerTempo => project.UsesFamiTrackerTempo;
        public int FamitrackerTempo { get => famitrackerTempo; set => famitrackerTempo = value; }
        public int FamitrackerSpeed { get => famitrackerSpeed; set => famitrackerSpeed = value; }
        public string FolderName { get => folderName; set => folderName = value; }
        public Folder Folder => string.IsNullOrEmpty(folderName) ? null : project.GetFolder(FolderType.Song, folderName);
        public string NameWithFolder => (string.IsNullOrEmpty(folderName) ? "" : $"{folderName}\\") + name;

        public NoteLocation StartLocation => new NoteLocation(0, 0);
        public NoteLocation EndLocation   => new NoteLocation(songLength, 0);

        public Song()
        {
            // For serialization.
            CreateCustomSettings();
        }

        public Song(Project project, int id, string name)
        {
            this.project = project;
            this.id = id;
            this.name = name;
            this.color = Theme.RandomCustomColor();

            CreateCustomSettings();
            SetDefaultsForTempoMode(project.TempoMode);
            CreateChannels();
            UpdatePatternStartNotes();
        }

        public void SetDefaultsForTempoMode(int tempoMode)
        {
            if (tempoMode == TempoType.FamiStudio)
            {
                noteLength = 10;
                groove = new[] { 10 };
                beatLength = noteLength * 4;
            }
            else
            {
                famitrackerTempo = Song.NativeTempoNTSC;
                famitrackerSpeed = 10;
                beatLength = 4;
            }

            patternLength = beatLength * 4;
        }

        private void CreateCustomSettings()
        {
            for (int i = 0; i < patternCustomSettings.Length; i++)
                patternCustomSettings[i] = new PatternCustomSetting();
        }

        public void CreateChannels(bool preserve = false, int oldExpansionMask = 0, int oldN163Channels = 0)
        {
            var channelCount = project.GetActiveChannelCount();
            var oldChannels = channels;

            channels = new Channel[channelCount];

            // Optionally map the old channel to the new channels.
            if (preserve)
            {
                for (int i = 0; i < ChannelType.Count; i++)
                {
                    var oldActive = Project.IsChannelActive(i, oldExpansionMask, oldN163Channels);
                    var newActive = project.IsChannelActive(i);

                    if (oldActive && newActive)
                    {
                        var oldIdx = Channel.ChannelTypeToIndex(i, oldExpansionMask, oldN163Channels);
                        var newIdx = Channel.ChannelTypeToIndex(i, project.ExpansionAudioMask, project.ExpansionNumN163Channels);

                        channels[newIdx] = oldChannels[oldIdx];
                    }
                }
            }

            // Create the new ones.
            for (int i = 0; i < ChannelType.Count; i++)
            {
                if (project.IsChannelActive(i))
                {
                    var idx = Channel.ChannelTypeToIndex(i, project.ExpansionAudioMask, project.ExpansionNumN163Channels);
                    if (channels[idx] == null)
                    {
                        channels[idx] = new Channel(this, i, songLength);
                    }
                }
            }
        }

        public void MakePatternsWithDifferentLengthsUnique()
        {
            foreach (var channel in channels)
            {
                channel.MakePatternsWithDifferentLengthsUnique();
            }
        }

        public void MakePatternsWithDifferentGroovesUnique()
        {
            foreach (var channel in channels)
            {
                channel.MakePatternsWithDifferentGroovesUnique();
            }
        }

        public void SetProject(Project newProject)
        {
            project = newProject;
        }

        public void SetLength(int newLength)
        {
            Debug.Assert(newLength <= MaxLength);

            songLength = newLength;

            foreach (var channel in channels)
                channel.ClearPatternsInstancesPastSongLength();

            if (loopPoint >= songLength)
                loopPoint = 0;

            UpdatePatternStartNotes();
            InvalidateCumulativePatternCache();
            CleanupUnusedPatterns();
        }

        public void SetDefaultPatternLength(int newLength)
        {
            patternLength = newLength;
            beatLength = Math.Min(beatLength, patternLength);

            UpdatePatternStartNotes();
        }

        public void SetBeatLength(int newBeatLength)
        {
            if (beatLength < patternLength)
                beatLength = newBeatLength;
        }

        public void SetGroovePaddingMode(int mode)
        {
            groovePaddingMode = mode;
        }

        public void SetLoopPoint(int loop)
        {
            loopPoint = Math.Min(loop, songLength - 1);
        }

        public bool IsEmpty
        {
            get
            {
                foreach (var channel in channels)
                {
                    foreach (var pattern in channel.Patterns)
                    {
                        if (!pattern.IsEmpty)
                            return false;
                    }
                }

                return true;
            }
        }

        public void SetSensibleBeatLength()
        {
            if (UsesFamiTrackerTempo)
            {
                // FamiTracker always assumes 4 rows per beat for BPM calculation, let's try to favor that when possible.
                if ((patternLength % 4) == 0)
                {
                    beatLength = 4;
                }
                else
                {
                    var beatLengths = Utils.GetFactors(patternLength);
                    beatLength = beatLengths.Length == 0 ? 1 : beatLengths[beatLengths.Length / 2];
                }
            }
            else
            {
                beatLength = noteLength * 4;
                while (beatLength > patternLength)
                    beatLength /= 2;
            }
        }

        public Pattern GetPatternInstance(PatternLocation location)
        {
            if (location.IsPatternInSong(this) && location.IsChannelInSong(this))
            {
                return channels[location.ChannelIndex].PatternInstances[location.PatternIndex];
            }

            return null;
        }

        public Pattern GetPattern(int id)
        {
            foreach (var channel in channels)
            {
                var pattern = channel.GetPattern(id);
                if (pattern != null)
                    return pattern;
            }

            return null;
        }

        public void ClearPatternCustomSettings(int patternIdx)
        {
            if (patternIdx < songLength)
                patternCustomSettings[patternIdx].Clear();
            InvalidateCumulativePatternCache();
            UpdatePatternStartNotes();
        }

        public void SetPatternCustomSettings(int patternIdx, int customPatternLength, int customBeatLength, int[] groove = null, int groovePaddingMode = GroovePaddingType.Middle)
        {
            Debug.Assert(customPatternLength > 0 && customPatternLength < Pattern.MaxLength);

            patternCustomSettings[patternIdx].Clear();
            patternCustomSettings[patternIdx].useCustomSettings = true;

            if (project.UsesFamiTrackerTempo)
            {
                Debug.Assert(groove == null);

                patternCustomSettings[patternIdx].patternLength = customPatternLength;
                patternCustomSettings[patternIdx].beatLength = customBeatLength;
            }
            else
            {
                FamiStudioTempoUtils.ValidateGroove(groove);

                var customNoteLength = Utils.Min(groove);

                Debug.Assert(customPatternLength % customNoteLength == 0);
                Debug.Assert(customBeatLength != 0);

                patternCustomSettings[patternIdx].patternLength = customPatternLength;
                patternCustomSettings[patternIdx].beatLength = customBeatLength;
                patternCustomSettings[patternIdx].noteLength = customNoteLength;
                patternCustomSettings[patternIdx].groove = groove;
                patternCustomSettings[patternIdx].groovePaddingMode = groovePaddingMode;
            }

            InvalidateCumulativePatternCache();
            UpdatePatternStartNotes();
        }

        public PatternCustomSetting GetPatternCustomSettings(int patternIdx)
        {
            return patternCustomSettings[patternIdx];
        }

        public bool PatternHasCustomSettings(int patternIdx)
        {
            return patternCustomSettings[patternIdx].useCustomSettings;
        }

        public int GetPatternNoteLength(int patternIdx)
        {
            var settings = patternCustomSettings[patternIdx];
            return settings.useCustomSettings ? settings.noteLength : noteLength;
        }

        public int GetPatternBeatLength(int patternIdx)
        {
            var settings = patternCustomSettings[patternIdx];
            return settings.useCustomSettings ? settings.beatLength : beatLength;
        }
        
        public int GetPatternLength(int patternIdx)
        {
            var settings = patternCustomSettings[patternIdx];
            return settings.useCustomSettings ? settings.patternLength : patternLength;
        }

        public int[] GetPatternGroove(int patternIdx)
        {
            var settings = patternCustomSettings[patternIdx];
            return settings.useCustomSettings ? settings.groove : groove;
        }

        public int GetPatternGroovePaddingMode(int patternIdx)
        {
            var settings = patternCustomSettings[patternIdx];
            return settings.useCustomSettings ? settings.groovePaddingMode : groovePaddingMode;
        }
        
        public int GetPatternStartAbsoluteNoteIndex(int patternIdx, int note = 0)
        {
            Debug.Assert(patternIdx <= songLength);
            return patternStartNote[patternIdx] + note;
        }

        public Channel GetChannelByType(int type)
        {
            lock (songLock)
            {
                return channels[Channel.ChannelTypeToIndex(type, project.ExpansionAudioMask, project.ExpansionNumN163Channels)];
            }
        }

        public void Trim()
        {
            int maxPatternIdx = 0;
            foreach (var channel in channels)
            {
                for (int i = 0; i < Length; i++)
                {
                    if (channel.PatternInstances[i] != null)
                    {
                        maxPatternIdx = Math.Max(maxPatternIdx, i);
                    }
                }
            }

            SetLength(maxPatternIdx + 1);
        }

        public void DeleteEmptyPatterns()
        {
            foreach (var channel in channels)
                channel.DeleteEmptyPatterns();
        }

        public void CleanupUnusedPatterns()
        {
            foreach (var channel in channels)
                channel.DeleteUnusedPatterns();
        }

        public void DeleteEmptyNotes()
        {
            foreach (var channel in channels)
                channel.DeleteEmptyNotes();
        }

        public void RemoveDpcmNotesWithoutMapping()
        {
            channels[ChannelType.Dpcm].RemoveDpcmNotesWithoutMapping();
        }

        public void DeleteNotesPastMaxInstanceLength()
        {
            foreach (var channel in channels)
                channel.DeleteNotesPastMaxInstanceLength();
        }

        public override string ToString()
        {
            return name;
        }

        public int NoteLength
        {
            get
            {
                return noteLength;
            }
        }

        private int GetNoteResizePriority(Note note)
        {
            if (note == null || note.IsEmpty)
                return 3;
            if (note.IsMusical || note.IsStop)
                return 0;
            if (note.IsRelease)
                return 1;

            return 2;
        }

        public void ResizePatternNotes(int p, int newNoteLength, HashSet<Pattern> processedPatterns = null)
        {
            Debug.Assert(UsesFamiStudioTempo);

            var oldPatternLength = GetPatternLength(p);
            var oldNoteLength    = GetPatternNoteLength(p);
            var ratio = (float)newNoteLength / oldNoteLength;

            if (ratio == 0.0f)
                ratio = 1.0f;

            foreach (var channel in channels)
            {
                var pattern = channel.PatternInstances[p];

                if (pattern == null || (processedPatterns != null && processedPatterns.Contains(pattern)))
                    continue;

                var oldNotes = new SortedList<int, Note>();

                foreach (var kv in pattern.Notes)
                    oldNotes[kv.Key] = kv.Value.Clone();

                pattern.DeleteAllNotes();

                int GetNewNoteIndex(int oldIdx)
                {
                    var noteIdx  = oldIdx / oldNoteLength;
                    var frameIdx = oldIdx % oldNoteLength;

                    return noteIdx * newNoteLength + (int)Math.Floor(frameIdx * ratio + 0.001f);
                }

                // Resize the pattern while applying some kind of priority in case we
                // 2 notes append to map to the same note (when shortening notes). 
                //
                // From highest to lowest:
                //   1) Note attacks and stop notes.
                //   2) Release notes
                //   3) Anything else that is not an empty note.
                //   4) Empty note.

                var conflictingNotes = new HashSet<int>();

                // TODO: Merge notes/slide + fx seperately.
                for (int i = 0; i < oldPatternLength; i++)
                {
                    var oldIdx = i;
                    var newIdx = GetNewNoteIndex(i);

                    oldNotes.TryGetValue(oldIdx, out var oldNote);
                    pattern.Notes.TryGetValue(newIdx, out var newNote);

                    if (oldNote == null)
                        continue;

                    int oldPriority = GetNoteResizePriority(oldNote);
                    int newPriority = GetNoteResizePriority(newNote);

                    if (oldPriority < newPriority)
                    {
                        if (oldNote.IsMusical)
                        {
                            var release = oldNote.Release;
                            oldNote.Duration = Math.Max(1, GetNewNoteIndex(i + oldNote.Duration) - newIdx);
                            if (release > 0)
                                oldNote.Release = Math.Max(1, GetNewNoteIndex(i + release) - newIdx);
                        }

                        pattern.SetNoteAt(newIdx, oldNote);
                    }
                    else
                    {
                        conflictingNotes.Add(oldIdx);
                    }
                }

                // For conflicting notes, try to place them at the first free slot after.
                foreach (var oldIdx in conflictingNotes)
                {
                    var newIdx = GetNewNoteIndex(oldIdx);

                    var oldNote = oldNotes[oldIdx];
                    var newNote = pattern.Notes[newIdx];

                    // If the old note only had effects, see if we can simply merge them.
                    // This works fine, but disabling for now was it could change the volume of notes at the
                    // wrong time for example, not sure desirable.
                    /*
                    if (!oldNote.IsMusical && !oldNote.IsRelease && !oldNote.IsStop && oldNote.HasAnyEffect)
                    {
                        var effectsConflict = false;
                        for (var i = 0; i < Note.EffectCount; i++)
                        {
                            if (oldNote.HasValidEffectValue(i) && newNote.HasValidEffectValue(i))
                            {
                                effectsConflict = true;
                                break;
                            }
                        }

                        if (!effectsConflict)
                        {
                            for (var i = 0; i < Note.EffectCount; i++)
                            {
                                if (oldNote.HasValidEffectValue(i))
                                {
                                    newNote.SetEffectValue(i, oldNote.GetEffectValue(i));
                                }
                            }

                            continue;
                        }
                    }
                    */

                    // Try to place on the very next frame.
                    var newPatternLen = GetNewNoteIndex(oldPatternLength);
                    newIdx++;
                    if (newIdx < newPatternLen && !pattern.Notes.ContainsKey(newIdx))
                    {
                        pattern.Notes.Add(newIdx, oldNote);
                    }
                }

                if (processedPatterns != null)
                    processedPatterns.Add(pattern);
            }
        }

        public bool DuplicatePatternsForNoteLengthChange(int minPatternIdx, int maxPatternIdx, bool ignoreCustomSettings)
        {
            var duplicatedAnything = false;

            // Gather all patterns inside/outside the range we will be modifying.
            foreach (var channel in channels)
            {
                var patternsInsideRange  = new HashSet<Pattern>();
                var patternsOutsideRange = new HashSet<Pattern>();

                for (var p = 0; p < songLength; p++)
                {
                    var pattern = channel.PatternInstances[p];

                    if (pattern != null)
                    {
                        var inRange = p >= minPatternIdx && p <= maxPatternIdx && (!ignoreCustomSettings || !PatternHasCustomSettings(p));

                        if (inRange)
                            patternsInsideRange.Add(pattern);
                        else
                            patternsOutsideRange.Add(pattern);
                    }
                }

                // For all patterns inside the range, see if there are also instances
                // outside of it. It so, we will need to duplicate the patterns to avoid
                // breaking anything.
                var oldToNewPattern = new Dictionary<Pattern, Pattern>();

                foreach (var pattern in patternsInsideRange)
                {
                    if (patternsOutsideRange.Contains(pattern))
                    {
                        oldToNewPattern.Add(pattern, pattern.ShallowClone());
                        duplicatedAnything = true;
                    }
                }

                for (var p = minPatternIdx; p <= maxPatternIdx; p++)
                {
                    var inRange = !ignoreCustomSettings || !PatternHasCustomSettings(p);
                    if (inRange)
                    {
                        var oldPattern = channel.PatternInstances[p];
                        if (oldPattern != null && oldToNewPattern.TryGetValue(oldPattern, out var newPattern))
                        {
                            channel.PatternInstances[p] = newPattern;
                        }
                    }
                }
            }

            return duplicatedAnything;
        }

        public void ChangeFamiStudioTempoGroove(int[] newGroove, bool convert)
        {
            var newNoteLength = Utils.Min(newGroove);

            Debug.Assert(UsesFamiStudioTempo);
            Debug.Assert(newNoteLength >= FamiStudioTempoUtils.MinNoteLength && newNoteLength <= FamiStudioTempoUtils.MaxNoteLength);

            if (convert)
            {
                Debug.Assert(patternLength % noteLength == 0);

                var processedPatterns = new HashSet<Pattern>();

                for (int p = 0; p < songLength; p++)
                {
                    if (!PatternHasCustomSettings(p))
                        ResizePatternNotes(p, newNoteLength, processedPatterns);
                }
            }

            noteLength = newNoteLength;
            groove     = newGroove;
        }

        public static float ComputeFamiTrackerBPM(bool palPlayback, int speed, int tempo, int beatLength)
        {
            return tempo * (palPlayback ? 20 : 24) / (float)(speed * beatLength);
        }

        public static float ComputeFamiStudioBPM(bool palSource, int[] groove, int beatLength)
        {
            return FamiStudioTempoUtils.ComputeBpmForGroove(palSource, groove, beatLength);
        }

        public float BPM
        {
            get
            {
                if (UsesFamiStudioTempo)
                    return ComputeFamiStudioBPM(project.PalMode, groove, beatLength / noteLength);
                else
                    return ComputeFamiTrackerBPM(project.PalMode, famitrackerSpeed, famitrackerTempo, beatLength);
            }
        }

        public bool UsesDpcm
        {
            get
            {
                for (int p = 0; p < songLength; p++)
                {
                    var pattern = channels[ChannelType.Dpcm].PatternInstances[p];
                    if (pattern != null)
                    {
                        foreach (var note in pattern.Notes.Values)
                        {
                            if (note.IsMusical && note.Instrument != null)
                            {
                                var mapping = note.Instrument.GetDPCMMapping(note.Value);

                                if (mapping != null)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }

                return false;
            }
        }

        public TimeSpan GetTimeAtLocation(NoteLocation location)
        {
            return project.UsesFamiStudioTempo ? TimeSpan.FromMilliseconds(location.ToAbsoluteNoteIndex(this) * 1000.0 / (project.PalMode ? NesApu.FpsPAL : NesApu.FpsNTSC)) : TimeSpan.Zero;
        }

        public TimeSpan Duration
        {
            get
            {
                return GetTimeAtLocation(EndLocation);
            }
        }

#if DEBUG
        public void ValidateIntegrity(Project project, Dictionary<int, object> idMap)
        {
            Debug.Assert(project == this.project);
            Debug.Assert(project.Songs.Contains(this));
            Debug.Assert(project.GetSong(id) == this);
            Debug.Assert(!string.IsNullOrEmpty(name.Trim()));
            Debug.Assert(string.IsNullOrEmpty(folderName) || project.FolderExists(FolderType.Song, folderName));

            project.ValidateId(id);

            if (idMap.TryGetValue(id, out var foundObj))
                Debug.Assert(foundObj == this);
            else
                idMap.Add(id, this);

            var uniqueNotes = new HashSet<Note>();
            var uniquePatterns = new HashSet<Pattern>();

            foreach (var channel in channels)
            {
                channel.ValidateIntegrity(this, idMap);

                // This is extremely heavy handed, but it is important. 
                // Notes used to be struct and they got changed to classes later on.
                // Its important to never assign the same note to 2 places in the song.
                foreach (var pattern in channel.Patterns)
                {
                    foreach (var note in pattern.Notes.Values)
                    {
                        Debug.Assert(!uniqueNotes.Contains(note));
                        uniqueNotes.Add(note);
                    }

                    Debug.Assert(!uniquePatterns.Contains(pattern));
                    uniquePatterns.Add(pattern);
                }
            }

            var oldPatternInstancesStartNote = new int[patternStartNote.Length];
            Array.Copy(patternStartNote, oldPatternInstancesStartNote, patternStartNote.Length);
            UpdatePatternStartNotes();
            for (int i = 0; i < patternStartNote.Length; i++)
                Debug.Assert(oldPatternInstancesStartNote[i] == patternStartNote[i]);

            if (UsesFamiStudioTempo)
            {
                FamiStudioTempoUtils.ValidateGroove(groove);
                Debug.Assert(noteLength == Utils.Min(groove));

                for (int i = 0; i < songLength; i++)
                {
                    var custom = patternCustomSettings[i];
                    if (custom.useCustomSettings)
                    {
                        Debug.Assert(custom.groove != null);
                        Debug.Assert(custom.noteLength == Utils.Min(custom.groove));
                    }
                    else
                    {
                        Debug.Assert(custom.groove == null);
                    }
                }
            }
            else
            {
                for (int i = 0; i < songLength; i++)
                {
                    var custom = patternCustomSettings[i];
                    Debug.Assert(custom.groove == null);
                }
            }
        }
#endif

        private void ConvertJumpSkipEffects()
        {
            for (int i = 0; i < songLength; i++)
            {
                foreach (var channel in channels)
                {
                    var pattern = channel.PatternInstances[i];

                    if (pattern != null)
                    {
                        foreach (var kv in pattern.Notes)
                        {
                            var note = kv.Value;

                            // Converts old Jump effects to loop points.
                            // The first Jump effect will give us our loop point.
                            if (loopPoint == 0 && note.Jump != 0xff)
                            {
                                SetLoopPoint(note.Jump);
                            }

                            // Converts old Skip effects to custom pattern instances lengths.
                            if (note.Skip != 0xff)
                            {
                                SetPatternCustomSettings(i, kv.Key + 1, BeatLength);
                            }

                            note.ClearJumpSkip();
                        }
                    }
                }
            }
        }

        public void ConvertToCompoundNotes()
        {
            foreach (var channel in channels)
                channel.ConvertToCompoundNotes();
        }

        public void ConvertToSimpleNotes()
        {
            foreach (var channel in channels)
                channel.ConvertToSimpleNotes();
        }

        public void SetNoteDurationToMaximumLength()
        {
            foreach (var channel in channels)
                channel.SetNoteDurationToMaximumLength();
        }

        public NoteLocation AbsoluteNoteIndexToNoteLocation(int absoluteNoteIdx)
        {
            // TODO: Binary search
            for (int i = 0; i < songLength; i++)
            {
                if (absoluteNoteIdx < patternStartNote[i + 1])
                    return new NoteLocation(i, absoluteNoteIdx - patternStartNote[i]);
            }

            return new NoteLocation(songLength, 0);
        }

        public int NoteLocationToAbsoluteNoteIndex(NoteLocation location)
        {
            return patternStartNote[location.PatternIndex] + location.NoteIndex;
        }

        public int PatternIndexFromAbsoluteNoteIndex(int idx)
        {
            // TODO: Binary search
            for (int i = 0; i < songLength; i++)
            {
                if (idx < patternStartNote[i + 1])
                    return i;
            }

            return songLength;
        }

        public void UpdatePatternStartNotes()
        {
            patternStartNote[0] = 0;
            for (int i = 1; i <= songLength; i++)
                patternStartNote[i] = patternStartNote[i - 1] + GetPatternLength(i - 1);
        }

        public void MergeIdenticalPatterns()
        {
            foreach (var channel in channels)
                channel.MergeIdenticalPatterns();
        }

        public void ConvertToFamiStudioTempo()
        {
            int newNoteLength = famitrackerSpeed;
            int newBeatLength = beatLength * newNoteLength;
            int newPatternLength = patternLength * newNoteLength;

            foreach (var channel in channels)
            {
                foreach (var pattern in channel.Patterns)
                {
                    var notesCopy = new SortedList<int, Note>(pattern.Notes);

                    pattern.Notes.Clear();
                    foreach (var kv in notesCopy)
                    {
                        var note = kv.Value;

                        if (note.IsMusical)
                            note.Duration *= famitrackerSpeed;

                        pattern.Notes[kv.Key * newNoteLength] = note;
                    }
                }
            }

            for (int p = 0; p < songLength; p++)
            {
                if (PatternHasCustomSettings(p))
                {
                    patternCustomSettings[p].noteLength        = famitrackerSpeed;
                    patternCustomSettings[p].patternLength     = patternCustomSettings[p].patternLength * famitrackerSpeed;
                    patternCustomSettings[p].beatLength        = Math.Max(patternCustomSettings[p].beatLength * famitrackerSpeed, 1);
                    patternCustomSettings[p].groove            = new int[] { famitrackerSpeed };
                    patternCustomSettings[p].groovePaddingMode = GroovePaddingType.Middle;
                }
            }

            groove        = new int[] { newNoteLength };
            noteLength    = newNoteLength;
            beatLength    = newBeatLength;
            patternLength = newPatternLength;

            RemoveUnsupportedEffects();
            UpdatePatternStartNotes();
            DeleteNotesPastMaxInstanceLength();
            InvalidateCumulativePatternCache();
        }

        public void RemoveUnsupportedFeatures(bool checkOnly = false)
        {
            foreach (var channel in channels)
                channel.RemoveUnsupportedFeatures(checkOnly);
        }

        public void InvalidateCumulativePatternCache()
        {
            foreach (var channel in channels)
                channel.InvalidateCumulativePatternCache();
        }

        public void RemoveUnsupportedEffects()
        {
            foreach (var channel in channels)
            {
                foreach (var pattern in channel.Patterns)
                {
                    foreach (var kv in pattern.Notes)
                    {
                        if (kv.Value != null)
                        {
                            var note = kv.Value;

                            for (int i = 0; i < Note.EffectCount; i++)
                            {
                                if (note.HasValidEffectValue(i))
                                {
                                    if (channel.SupportsEffect(i))
                                        note.SetEffectValue(i, Note.ClampEffectValue(this, channel, i, note.GetEffectValue(i)));
                                    else
                                        note.ClearEffectValue(i);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void RemoveUnsupportedInstruments()
        {
            foreach (var channel in channels)
            {
                foreach (var pattern in channel.Patterns)
                {
                    foreach (var kv in pattern.Notes)
                    {
                        if (kv.Value != null)
                        {
                            var note = kv.Value;

                            if (note.Instrument != null && !channel.SupportsInstrument(note.Instrument))
                                note.Instrument = null;
                        }
                    }
                }
            }
        }

        public void PermanentlyApplyGrooves()
        {
            if (UsesFamiStudioTempo)
            {
                MakePatternsWithDifferentGroovesUnique();

                var processedPatterns = new HashSet<Pattern>();

                for (int p = 0; p < songLength; p++)
                {
                    var localGroove        = GetPatternGroove(p);
                    var localPatternLength = GetPatternLength(p);
                    var localGroovePadMode = GetPatternGroovePaddingMode(p);

                    foreach (var c in channels)
                    {
                        var pattern = c.PatternInstances[p];

                        if (pattern == null || processedPatterns.Contains(pattern))
                            continue;

                        // Nothing to do for integral tempos.
                        if (localGroove.Length > 1)
                        {
                            var maxPatternLen = pattern.GetMaxInstanceLength();
                            var originalNotes = new SortedList<int, Note>(pattern.Notes);

                            pattern.Notes.Clear();

                            var grooveIterator = new GrooveIterator(localGroove, localGroovePadMode);

                            for (int i = 0; i < maxPatternLen; i++)
                            {
                                if (grooveIterator.IsPadFrame)
                                    grooveIterator.Advance();

                                if (originalNotes.TryGetValue(i, out var note) && note != null)
                                    pattern.Notes.Add(grooveIterator.FrameIndex, note);

                                grooveIterator.Advance();
                            }
                        }

                        processedPatterns.Add(pattern);
                    }

                    if (PatternHasCustomSettings(p))
                    {
                        GetPatternCustomSettings(p).patternLength = FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(localPatternLength, localGroove, localGroovePadMode);
                        GetPatternCustomSettings(p).beatLength    = Utils.Sum(localGroove);
                        GetPatternCustomSettings(p).groove        = new[] { 1 };
                    }
                }

                patternLength = FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(patternLength, groove, groovePaddingMode);
                beatLength    = Utils.Sum(groove);
                groove        = new [] { 1 };

                UpdatePatternStartNotes();
                InvalidateCumulativePatternCache();
            }
        }

        public void ClearCustomPatternSettingsForFamitrackerTempo()
        {
            Debug.Assert(UsesFamiTrackerTempo);

            for (int p = 0; p < songLength; p++)
            {
                if (PatternHasCustomSettings(p))
                {
                    var settings = GetPatternCustomSettings(p);
                    settings.noteLength = 0;
                    settings.groovePaddingMode = GroovePaddingType.Middle;
                    settings.groove = null;
                }
            }
        }

        public bool ApplySpeedEffectAt(NoteLocation location, ref int speed)
        {
            if (UsesFamiStudioTempo)
                return false;

            foreach (var channel in channels)
            {
                var note = channel.GetNoteAt(location);
                if (note != null && note.HasSpeed)
                {
                    speed = note.Speed;
                    return true;
                }
            }

            return false;
        }

        public int CountNotesBetween(NoteLocation start, NoteLocation end)
        {
            return NoteLocationToAbsoluteNoteIndex(end) - NoteLocationToAbsoluteNoteIndex(start);
        }

        public float CountFramesBetween(NoteLocation l0, NoteLocation l1, int currentSpeed, bool pal)
        {
            // This is simply an approximation that is used to compute slide notes.
            // It doesn't take into account the real state of the tempo accumulator.
            if (project.UsesFamiTrackerTempo)
            {
                float frameCount = 0;

                while (l0 != l1 && l0.PatternIndex < songLength)
                {
                    ApplySpeedEffectAt(l0, ref currentSpeed);
                    float tempoRatio = (pal ? NativeTempoPAL : NativeTempoNTSC) / (float)famitrackerTempo;
                    frameCount += currentSpeed * tempoRatio;

                    if (++l0.NoteIndex >= GetPatternLength(l0.PatternIndex))
                    {
                        l0.NoteIndex = 0;
                        l0.PatternIndex++;
                    }
                }

                return frameCount;
            }
            else
            {
                var frameCount = 0;

                if (l0.PatternIndex == l1.PatternIndex)
                {
                    frameCount =
                        FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(l1.NoteIndex, GetPatternGroove(l1.PatternIndex), GetPatternGroovePaddingMode(l1.PatternIndex)) -
                        FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(l0.NoteIndex, GetPatternGroove(l0.PatternIndex), GetPatternGroovePaddingMode(l0.PatternIndex));
                }
                else
                {
                    // End of first pattern.
                    if (l0.NoteIndex != 0)
                    {
                        frameCount =
                            FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(GetPatternLength(l0.PatternIndex), GetPatternGroove(l0.PatternIndex), GetPatternGroovePaddingMode(l0.PatternIndex)) -
                            FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(l0.NoteIndex, GetPatternGroove(l0.PatternIndex), GetPatternGroovePaddingMode(l0.PatternIndex));
                        l0.PatternIndex++;
                    }

                    // Complete patterns in between.
                    while (l0.PatternIndex != l1.PatternIndex)
                    {
                        frameCount += FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(GetPatternLength(l0.PatternIndex), GetPatternGroove(l0.PatternIndex), GetPatternGroovePaddingMode(l0.PatternIndex));
                        l0.PatternIndex++;
                    }

                    // First bit of last pattern.
                    if (l1.NoteIndex != 0)
                    {
                        frameCount += FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(l1.NoteIndex, GetPatternGroove(l0.PatternIndex), GetPatternGroovePaddingMode(l0.PatternIndex));
                    }
                }

                return frameCount;
            }
        }

        public bool AdvanceNumberOfNotes(ref NoteLocation location, int noteCount)
        {
            if (noteCount > 0)
            {
                location.NoteIndex += noteCount;

                while (true)
                {
                    var patternLen = GetPatternLength(location.PatternIndex);

                    if (location.NoteIndex < patternLen)
                        return true;

                    location.NoteIndex -= patternLen;

                    if (++location.PatternIndex >= songLength)
                    {
                        location.NoteIndex = 0;
                        return false;
                    }
                }
            }
            else if (noteCount < 0)
            {
                location.NoteIndex += noteCount;

                while (true)
                {
                    if (location.NoteIndex >= 0)
                        return true;

                    if (--location.PatternIndex < 0)
                    {
                        location.PatternIndex = 0;
                        location.NoteIndex = 0;
                        return false;
                    }

                    location.NoteIndex += GetPatternLength(location.PatternIndex);
                }
            }

            return true;
        }

        public void AdvanceNumberOfFrames(ref NoteLocation location, int frameCount, int initialCount, int currentSpeed, bool pal)
        {
            Debug.Assert(UsesFamiTrackerTempo);

            float count = initialCount;

            while (count < frameCount && location.PatternIndex < songLength)
            {
                if (UsesFamiTrackerTempo)
                {
                    ApplySpeedEffectAt(location, ref currentSpeed);
                    float tempoRatio = (pal ? NativeTempoPAL : NativeTempoNTSC) / (float)famitrackerTempo;
                    count += currentSpeed * tempoRatio;
                }
                else
                {
                    count++;
                }

                if (++location.NoteIndex >= GetPatternLength(location.PatternIndex))
                {
                    location.PatternIndex++;
                    location.NoteIndex = 0;
                }
            }
        }

        public void ExtendForLooping(int loopCount, bool extendLastNotes = false)
        {
            var lastNotes = (Note[])null;

            // This mimics the regular looping behavior where notes that "touch" the end of the song keep playing, this
            // loop finds those notes.
            if (extendLastNotes)
            {
                lastNotes = new Note[channels.Length];

                for (var c = 0; c < channels.Length; c++) 
                {
                    var channel = channels[c];

                    for (var it = channel.GetSparseNoteIterator(StartLocation, EndLocation, NoteFilter.Musical); !it.Done; it.Next())
                    {
                        var actualDuration = Math.Min(it.Note.Duration, it.DistanceToNextNote);
                        var noteEndLocation = it.Location.Advance(this, actualDuration);

                        if (noteEndLocation >= EndLocation)
                        {
                            lastNotes[c] = it.Note.Clone();
                            break;
                        }
                    }
                }
            }

            // For looping, we simply extend the song by copying pattern instances.
            if (loopCount > 1 && LoopPoint >= 0 && LoopPoint < Length)
            {
                var originalLength = Length;
                var loopSectionLength = originalLength - LoopPoint;

                SetLength(Math.Min(Song.MaxLength, originalLength + loopSectionLength * (loopCount - 1)));

                var srcPatIdx = LoopPoint;                

                for (var i = originalLength; i < Length; i++)
                {
                    for (var c = 0; c < channels.Length; c++)
                    {
                        var channel = channels[c];

                        channel.PatternInstances[i] = channel.PatternInstances[srcPatIdx];

                        // Add a no attack note at the beginning of the loop point to mimic the final note of the song lasting forever.
                        // We cant simply extend the final note since it may have slides, etc. So we really need another note. The max
                        // duration we can set is 65536 frames, hopefully that's good enough.
                        if (extendLastNotes && srcPatIdx == loopPoint && lastNotes[c] != null && (channel.PatternInstances[i] == null || !channel.PatternInstances[i].Notes.TryGetValue(0, out var note) || (!note.IsMusical && !note.IsStop)))
                        {
                            var lastNote = lastNotes[c];
                            channel.PatternInstances[i] = channel.PatternInstances[i] == null ? channel.CreatePattern() : channel.PatternInstances[i].ShallowClone();
                            note = channel.PatternInstances[i].GetOrCreateNoteAt(0);
                            note.Value = lastNote.IsSlideNote ? lastNote.SlideNoteTarget : lastNote.Value;
                            note.Instrument = lastNote.Instrument;
                            note.HasAttack = false;
                            note.Duration = 1000000;
                        }
                    }

                    if (PatternHasCustomSettings(srcPatIdx))
                    {
                        var customSettings = GetPatternCustomSettings(srcPatIdx);
                        SetPatternCustomSettings(i, customSettings.patternLength, customSettings.beatLength, customSettings.groove, customSettings.groovePaddingMode);
                    }

                    if (++srcPatIdx >= originalLength)
                    {
                        srcPatIdx = loopPoint;
                    }
                }
            }
        }

        public void ChangeId(int newId)
        {
            id = newId;
        }

        public void Serialize(ProjectBuffer buffer)
        {
            lock (songLock)
            {
                if (buffer.IsReading)
                    project = buffer.Project;

                buffer.Serialize(ref id, true);
                buffer.Serialize(ref patternLength);
                buffer.Serialize(ref songLength);
                buffer.Serialize(ref beatLength);
                buffer.Serialize(ref name);
                buffer.Serialize(ref famitrackerTempo);
                buffer.Serialize(ref famitrackerSpeed);
                buffer.Serialize(ref color);

                // At version 5 (FamiStudio 2.0.0), we replaced the jump/skips effects by loop points and custom pattern length and we added a new tempo mode.
                if (buffer.Version >= 5)
                {
                    buffer.Serialize(ref loopPoint);
                    buffer.Serialize(ref noteLength);

                    // At version 10 (FamiStudio 3.0.0) we improved tempo.
                    if (buffer.Version >= 10)
                        buffer.Serialize(ref groove);
                    else
                        groove = new[] { noteLength };

                    for (int i = 0; i < songLength; i++)
                    {
                        var customSettings = patternCustomSettings[i];

                        buffer.Serialize(ref customSettings.useCustomSettings);
                        buffer.Serialize(ref customSettings.patternLength);
                        buffer.Serialize(ref customSettings.noteLength);
                        buffer.Serialize(ref customSettings.beatLength);

                        // At version 10 (FamiStudio 3.0.0) we improved tempo.
                        if (buffer.Version >= 10)
                        {
                            buffer.Serialize(ref customSettings.groove);
                            buffer.Serialize(ref customSettings.groovePaddingMode);
                        }
                        else
                        {
                            customSettings.groove = customSettings.useCustomSettings ? new[] { customSettings.noteLength } : null;
                            customSettings.groovePaddingMode = GroovePaddingType.Middle;
                        }

                        // At version 8 (FamiStudio 2.3.0), we added custom beat length for FamiTracker tempo, so we need to initialize the value here.
                        if (buffer.Version < 8 && project.UsesFamiTrackerTempo && patternCustomSettings[i].useCustomSettings && patternCustomSettings[i].beatLength == 0)
                        {
                            patternCustomSettings[i].beatLength = beatLength;
                        }
                    }

                    for (int i = songLength; i < MaxLength; i++)
                        patternCustomSettings[i].Clear();
                }

                // At version 16 (FamiStudio 4.2.0) we added little folders in the project explorer.
                if (buffer.Version >= 16)
                {
                    buffer.Serialize(ref folderName);
                }

                if (buffer.IsReading)
                {
                    CreateChannels();
                    UpdatePatternStartNotes();
                }

                foreach (var channel in channels)
                    channel.Serialize(buffer);

                if (buffer.IsReading && !buffer.IsForUndoRedo)
                    DeleteNotesPastMaxInstanceLength();

                if (buffer.Version < 5)
                    ConvertJumpSkipEffects();

                if (buffer.Version < 10)
                    ConvertToCompoundNotes();

                if (buffer.IsReading && !buffer.IsForUndoRedo)
                {
                    RemoveUnsupportedFeatures();
                    DeleteEmptyNotes();
                }

                // Before 2.3.0, songs had an invalid color by default.
                if (buffer.Version < 8 && color.ToArgb() == Color.Azure.ToArgb())
                    color = Theme.RandomCustomColor();
            }
        }

        public class PatternCustomSetting
        {
            public bool useCustomSettings;
            public int patternLength;
            public int noteLength;
            public int beatLength;
            public int[] groove;
            public int groovePaddingMode;

            public void Clear()
            {
                useCustomSettings = false;
                patternLength = 0;
                noteLength = 0;
                beatLength = 0;
                groovePaddingMode = GroovePaddingType.Middle;
                groove = null;
            }

            public PatternCustomSetting Clone()
            {
                var clone = new PatternCustomSetting();
                clone.useCustomSettings = useCustomSettings;
                clone.patternLength = patternLength;
                clone.noteLength = noteLength;
                clone.beatLength = beatLength;
                clone.groovePaddingMode = groovePaddingMode;
                if (groove != null)
                    clone.groove = groove.Clone() as int[];
                return clone;
            }
        };
    }

    public struct PatternLocation
    {
        public int ChannelIndex;
        public int PatternIndex;

        public bool IsValid => ChannelIndex >= 0 && PatternIndex >= 0;

        public PatternLocation(int c, int p)
        {
            ChannelIndex = c;
            PatternIndex = p;
        }

        public bool IsPatternInSong(Song s)
        {
            return PatternIndex < s.Length;
        }

        public bool IsChannelInSong(Song s)
        {
            return ChannelIndex >= 0 && ChannelIndex < s.Channels.Length;
        }

        public bool IsInSong(Song s)
        {
            return IsPatternInSong(s) && IsChannelInSong(s);
        }

        public override string ToString()
        {
            return $"{ChannelIndex}:{PatternIndex}";
        }

        public override bool Equals(object obj)
        {
            var other = (PatternLocation)obj;
            return ChannelIndex == other.ChannelIndex && PatternIndex == other.PatternIndex;
        }

        public override int GetHashCode()
        {
            return Utils.HashCombine(ChannelIndex, PatternIndex);
        }

        public static bool operator ==(PatternLocation p0, PatternLocation p1)
        {
            return p0.ChannelIndex == p1.ChannelIndex && p0.PatternIndex == p1.PatternIndex;
        }

        public static bool operator !=(PatternLocation p0, PatternLocation p1)
        {
            return p0.ChannelIndex != p1.ChannelIndex || p0.PatternIndex != p1.PatternIndex;
        }

        public static PatternLocation Min(PatternLocation p0, PatternLocation p1)
        {
            return new PatternLocation(
                Math.Min(p0.ChannelIndex, p1.ChannelIndex),
                Math.Min(p0.PatternIndex, p1.PatternIndex));
        }

        public static PatternLocation Max(PatternLocation p0, PatternLocation p1)
        {
            return new PatternLocation(
                Math.Max(p0.ChannelIndex, p1.ChannelIndex),
                Math.Max(p0.PatternIndex, p1.PatternIndex));
        }

        public static readonly PatternLocation Invalid = new PatternLocation(-1, -1);
    }

    public struct NoteLocation
    {
        public int PatternIndex;
        public int NoteIndex;

        public bool IsValid => PatternIndex >= 0 && NoteIndex >= 0;

        public NoteLocation(int p, int n)
        {
            PatternIndex = p;
            NoteIndex = n;
        }

        public bool IsInSong(Song s)
        {
            return PatternIndex >= 0 && PatternIndex < s.Length;
        }

        public int ToAbsoluteNoteIndex(Song s)
        {
            return s.NoteLocationToAbsoluteNoteIndex(this);
        }

        public static NoteLocation FromAbsoluteNoteIndex(Song s, int absoluteNoteIndex)
        {
            return s.AbsoluteNoteIndexToNoteLocation(absoluteNoteIndex);
        }

        public NoteLocation Advance(Song s, int num)
        {
            NoteLocation loc = this;
            s.AdvanceNumberOfNotes(ref loc, num);
            return loc;
        }

        public int DistanceTo(Song s, NoteLocation other)
        {
            return s.CountNotesBetween(this, other);
        }

        public override bool Equals(object obj)
        {
            var other = (NoteLocation)obj;
            return PatternIndex == other.PatternIndex && NoteIndex == other.NoteIndex;
        }

        public override int GetHashCode()
        {
            return Utils.HashCombine(PatternIndex, NoteIndex);
        }

        public override string ToString()
        {
            return $"{PatternIndex}:{NoteIndex}";
        }

        public static bool operator ==(NoteLocation n0, NoteLocation n1)
        {
            return n0.PatternIndex == n1.PatternIndex && n0.NoteIndex == n1.NoteIndex;
        }

        public static bool operator !=(NoteLocation n0, NoteLocation n1)
        {
            return n0.PatternIndex != n1.PatternIndex || n0.NoteIndex != n1.NoteIndex;
        }

        public static bool operator <(NoteLocation n0, NoteLocation n1)
        {
            if (n0.PatternIndex < n1.PatternIndex)
                return true;
            else if (n0.PatternIndex == n1.PatternIndex)
                return n0.NoteIndex < n1.NoteIndex;
            return false;
        }

        public static bool operator <=(NoteLocation n0, NoteLocation n1)
        {
            if (n0.PatternIndex < n1.PatternIndex)
                return true;
            else if (n0.PatternIndex == n1.PatternIndex)
                return n0.NoteIndex <= n1.NoteIndex;
            return false;
        }

        public static bool operator >(NoteLocation n0, NoteLocation n1)
        {
            if (n0.PatternIndex > n1.PatternIndex)
                return true;
            else if (n0.PatternIndex == n1.PatternIndex)
                return n0.NoteIndex > n1.NoteIndex;
            return false;
        }

        public static bool operator >=(NoteLocation n0, NoteLocation n1)
        {
            if (n0.PatternIndex > n1.PatternIndex)
                return true;
            else if (n0.PatternIndex == n1.PatternIndex)
                return n0.NoteIndex >= n1.NoteIndex;
            return false;
        }

        public static NoteLocation Min(NoteLocation n0, NoteLocation n1)
        {
            return n0 < n1 ? n0 : n1;
        }

        public static NoteLocation Max(NoteLocation n0, NoteLocation n1)
        {
            return n0 > n1 ? n0 : n1;
        }

        public static readonly NoteLocation Invalid = new NoteLocation(-1, -1);
    }
    
    public static class GroovePaddingType
    {
        public const int Beginning = 0;
        public const int Middle    = 1;
        public const int End       = 2;

        public static readonly string[] Names =
        {
            "Beginning",
            "Middle",
            "End"
        };

        public static int GetValueForName(string str)
        {
            return Array.IndexOf(Names, str);
        }
    }
}
