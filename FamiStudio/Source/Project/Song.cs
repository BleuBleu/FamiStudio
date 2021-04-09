using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

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
        private int patternLength = 96;
        private int songLength = 16;
        private int beatLength = 40;
        private string name;
        private int loopPoint = 0;
        private PatternCustomSetting[] patternCustomSettings = new PatternCustomSetting[Song.MaxLength];
        private int[] patternStartNote = new int[Song.MaxLength + 1];

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
            this.color = ThemeBase.RandomCustomColor();

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

        public void CreateChannels(bool preserve = false, int numChannelsToPreserve = ChannelType.ExpansionAudioStart)
        {
            int channelCount = project.GetActiveChannelCount();

            if (preserve)
            {
                Array.Resize(ref channels, channelCount);
                for (int i = numChannelsToPreserve; i < channels.Length; i++)
                    channels[i] = null;
            }
            else
            {
                channels = new Channel[channelCount];
            }

            for (int i = 0; i < ChannelType.Count; i++)
            {
                var idx = Channel.ChannelTypeToIndex(i);
                if (project.IsChannelActive(i) && channels[idx] == null)
                    channels[idx] = new Channel(this, i, songLength);
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
                    beatLength = beatLengths[beatLengths.Length / 2];
                }
            }
            else
            {
                beatLength = noteLength * 4;
                while (beatLength > patternLength)
                    beatLength /= 2;
            }
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
            patternCustomSettings[patternIdx].Clear();
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

        public int GetPatternStartNote(int patternIdx, int note = 0)
        {
            return patternStartNote[patternIdx] + note;
        }

        public Channel GetChannelByType(int type)
        {
            return channels[Channel.ChannelTypeToIndex(type)];
        }

        public void Trim()
        {
            int maxLength = 0;
            foreach (var channel in channels)
            {
                for (int i = 0; i < Length; i++)
                {
                    if (channel.PatternInstances[i] != null)
                    {
                        maxLength = Math.Max(maxLength, i);
                    }
                }
            }

            SetLength(maxLength);
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
            var ratio = (float)(oldNoteLength - 1) / (newNoteLength - 1);

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

                pattern.Notes.Clear();

                // Resize the pattern while applying some kind of priority in case we
                // 2 notes append to map to the same note (when shortening notes). 
                //
                // From highest to lowest:
                //   1) Note attacks and stop notes.
                //   2) Release notes
                //   3) Anything else that is not an empty note.
                //   4) Empty note.

                // TODO: Merge notes/slide + fx seperately.
                for (int i = 0; i < oldPatternLength / oldNoteLength; i++)
                {
                    for (int j = 0; j < oldNoteLength; j++)
                    {
                        var oldIdx = i * oldNoteLength + j;
                        var newIdx = i * newNoteLength + (int)Math.Round(j / ratio);

                        oldNotes.TryGetValue(oldIdx, out var oldNote);
                        pattern.Notes.TryGetValue(newIdx, out var newNote);

                        if (oldNote == null)
                            continue;

                        int oldPriority = GetNoteResizePriority(oldNote);
                        int newPriority = GetNoteResizePriority(newNote);

                        if (oldPriority < newPriority)
                            pattern.SetNoteAt(newIdx, oldNote);
                    }
                }

                pattern.ClearLastValidNoteCache();

                if (processedPatterns != null)
                    processedPatterns.Add(pattern);
            }
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
                            if (note.IsValid && !note.IsStop)
                            {
                                var mapping = project.GetDPCMMapping(note.Value);

                                if (mapping != null && mapping.Sample != null)
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

#if DEBUG
        public void Validate(Project project, Dictionary<int, object> idMap)
        {
            Debug.Assert(project.Songs.Contains(this));
            Debug.Assert(project.GetSong(id) == this);

            project.ValidateId(id);

            if (idMap.TryGetValue(id, out var foundObj))
                Debug.Assert(foundObj == this);
            else
                idMap.Add(id, this);

            var uniqueNotes = new HashSet<Note>();
            var uniquePatterns = new HashSet<Pattern>();

            foreach (var channel in channels)
            {
                channel.Validate(this, idMap);

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
                Debug.Assert(noteLength == Utils.Min(groove)); // TEMPOTODO: Assert here when switching tempo mode.

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
                            if (loopPoint == 0 && note.FxJump != 0xff)
                            {
                                SetLoopPoint(note.FxJump);
                            }

                            // Converts old Skip effects to custom pattern instances lengths.
                            if (note.FxSkip != 0xff)
                            {
                                SetPatternCustomSettings(i, kv.Key + 1, BeatLength);
                            }
                        }
                    }
                }
            }
        }

        public int FindPatternInstanceIndex(int idx, out int noteIdx)
        {
            noteIdx = -1;

            // TODO: Binary search
            for (int i = 0; i < songLength; i++)
            {
                if (idx < patternStartNote[i + 1])
                {
                    noteIdx = idx - patternStartNote[i];
                    return i;
                }
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
                        pattern.Notes[kv.Key * newNoteLength] = note;
                    }

                    pattern.ClearLastValidNoteCache();
                }
            }

            for (int p = 0; p < songLength; p++)
            {
                if (PatternHasCustomSettings(p))
                {
                    patternCustomSettings[p].noteLength    = famitrackerSpeed;
                    patternCustomSettings[p].patternLength = patternCustomSettings[p].patternLength / famitrackerSpeed * famitrackerSpeed;
                    patternCustomSettings[p].beatLength     = Math.Max(patternCustomSettings[p].beatLength / famitrackerSpeed, 1);
                }
            }

            noteLength    = newNoteLength;
            beatLength    = newBeatLength;
            patternLength = newPatternLength;

            RemoveUnsupportedEffects();
            UpdatePatternStartNotes();
            DeleteNotesPastMaxInstanceLength();
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
                                if (note.HasValidEffectValue(i) && !channel.SupportsEffect(i))
                                    note.ClearEffectValue(i);
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

                            if (note.Instrument != null && !channel.SupportsInstrument(note.Instrument) || channel.Type == ChannelType.Dpcm)
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

                foreach (var c in channels)
                {
                    for (int p = 0; p < songLength; p++)
                    {
                        var pattern = c.PatternInstances[p];

                        if (pattern == null || processedPatterns.Contains(pattern))
                            continue;

                        var groove = GetPatternGroove(p);
                        var newPatternLength = GetPatternLength(p);
                        
                        // Nothing to do for integral tempos.
                        if (groove.Length > 1)
                        {
                            var groovePadMode  = GetPatternGroovePaddingMode(p);
                            var maxPatternLen = pattern.GetMaxInstanceLength();
                            var originalNotes = new SortedList<int, Note>(pattern.Notes);

                            pattern.Notes.Clear();

                            var grooveIterator = new GrooveIterator(groove, groovePadMode);

                            // TEMPOTODO : Compare that it gives the same result as the playback.
                            for (int i = 0; i < maxPatternLen; i++)
                            {
                                if (grooveIterator.IsPadFrame)
                                    grooveIterator.Advance();

                                if (originalNotes.TryGetValue(i, out var note) && note != null)
                                    pattern.Notes.Add(grooveIterator.FrameIndex, note);

                                // Advance groove.
                                grooveIterator.Advance();
                            }

                            newPatternLength = grooveIterator.FrameIndex;
                        }

                        if (PatternHasCustomSettings(p))
                        {
                            GetPatternCustomSettings(p).patternLength = newPatternLength;
                        }

                        processedPatterns.Add(pattern);
                    }
                }

                patternLength = FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(patternLength, groove, groovePaddingMode);

                // TEMPOTODO : HOW DO WE MARK PATTERNS AS PROCESSED? 
                // FT2 export still compares grooves and stuff.

                UpdatePatternStartNotes();
            }
        }

        public bool ApplySpeedEffectAt(int patternIdx, int noteIdx, ref int speed)
        {
            if (UsesFamiStudioTempo)
                return false;

            foreach (var channel in channels)
            {
                var pattern = channel.PatternInstances[patternIdx];
                if (pattern != null)
                {
                    if (pattern.Notes.TryGetValue(noteIdx, out var note) && note != null && note.HasSpeed)
                    {
                        speed = note.Speed;
                        return true;
                    }
                }
            }

            return false;
        }

        public int CountNotesBetween(int p0, int n0, int p1, int n1)
        {
            int noteCount = 0;

            while (p0 != p1)
            {
                noteCount += GetPatternLength(p0) - n0;
                p0++;
                n0 = 0;
            }

            noteCount += (n1 - n0);

            return noteCount;
        }

        public float CountFramesBetween(int p0, int n0, int p1, int n1, int currentSpeed, bool pal)
        {
            // This is simply an approximation that is used to compute slide notes.
            // It doesn't take into account the real state of the tempo accumulator.
            if (project.UsesFamiTrackerTempo)
            {
                float frameCount = 0;

                while ((p0 != p1 || n0 != n1) && p0 < songLength)
                {
                    ApplySpeedEffectAt(p0, n0, ref currentSpeed);
                    float tempoRatio = (pal ? NativeTempoPAL : NativeTempoNTSC) / (float)famitrackerTempo;
                    frameCount += currentSpeed * tempoRatio;

                    if (++n0 >= GetPatternLength(p0))
                    {
                        n0 = 0;
                        p0++;
                    }
                }

                return frameCount;
            }
            else
            {
                // TEMPOTODO : What about PAL here?
                // TEMPOTODO : Also, the param shouldnt be PAL, it should be NTSC -> PAL, or PAL -> NTSC.

                var frameCount = 0;

                if (p0 == p1)
                {
                    frameCount =
                        FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(n1, GetPatternGroove(p0), GetPatternGroovePaddingMode(p0)) -
                        FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(n0, GetPatternGroove(p0), GetPatternGroovePaddingMode(p0));
                }
                else
                {
                    // End of first pattern.
                    if (n0 != 0)
                    {
                        frameCount =
                            FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(GetPatternLength(p0), GetPatternGroove(p0), GetPatternGroovePaddingMode(p0)) -
                            FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(n0, GetPatternGroove(p0), GetPatternGroovePaddingMode(p0));
                        p0++;
                    }

                    // Complete patterns in between.
                    while (p0 != p1)
                    {
                        frameCount += FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(GetPatternLength(p0), GetPatternGroove(p0), GetPatternGroovePaddingMode(p0));
                        p0++;
                    }

                    // First bit of last pattern.
                    if (n1 != 0)
                    {
                        frameCount += FamiStudioTempoUtils.ComputeNumberOfFrameForGroove(n1, GetPatternGroove(p0), GetPatternGroovePaddingMode(p0));
                    }
                }

                return frameCount;
            }
        }

        public bool AdvanceNumberOfNotes(int noteCount, ref int p, ref int n)
        {
            float count = 0;

            if (noteCount > 0)
            {
                while (count < noteCount && p < songLength)
                {
                    count++;
                    if (++n >= GetPatternLength(p))
                    {
                        n = 0;
                        p++;
                    }
                }

                return p < songLength;
            }
            else if (noteCount < 0)
            {
                noteCount = -noteCount;
                while (count < noteCount && p >= 0)
                {
                    count++;
                    if (--n < 0)
                    {
                        p--;
                        n = GetPatternLength(p) - 1;
                    }
                }

                return p >= 0;
            }

            return true;
        }

        public void AdvanceNumberOfFrames(int frameCount, int initialCount, int currentSpeed, bool pal, ref int p, ref int n)
        {
            Debug.Assert(UsesFamiTrackerTempo);

            float count = initialCount;

            while (count < frameCount && p < songLength)
            {
                if (UsesFamiTrackerTempo)
                {
                    ApplySpeedEffectAt(p, n, ref currentSpeed);
                    float tempoRatio = (pal ? NativeTempoPAL : NativeTempoNTSC) / (float)famitrackerTempo;
                    count += currentSpeed * tempoRatio;
                }
                else
                {
                    count++;
                }

                if (++n >= GetPatternLength(p))
                {
                    n = 0;
                    p++;
                }
            }
        }

        public void ChangeId(int newId)
        {
            id = newId;
        }

        public void SerializeState(ProjectBuffer buffer)
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

            if (buffer.IsReading)
            {
                CreateChannels();
                UpdatePatternStartNotes();
            }

            foreach (var channel in channels)
                channel.SerializeState(buffer);

            if (buffer.Version < 5)
                ConvertJumpSkipEffects();

            // Before 2.3.0, songs had an invalid color by default.
            if (buffer.Version < 8 && color.ToArgb() == Color.Azure.ToArgb())
                color = ThemeBase.RandomCustomColor();
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
