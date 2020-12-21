using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace FamiStudio
{
    public class Song
    {
        public const int MaxLength = 256;
        public const int MinNoteLength = 1;
        public const int MaxNoteLength = 18;

        public const int NativeTempoNTSC = 150;
        public const int NativeTempoPAL  = 125;

        public class PatternCustomSetting
        {
            public bool useCustomSettings;
            public int  patternLength;
            public int  noteLength;
            public int  barLength;

            public void Clear()
            {
                useCustomSettings = false;
                patternLength = 0;
                noteLength = 0;
                barLength = 0;
            }

            public PatternCustomSetting Clone()
            {
                var clone = new PatternCustomSetting();
                clone.useCustomSettings = useCustomSettings;
                clone.patternLength = patternLength;
                clone.noteLength = noteLength;
                clone.barLength = barLength;
                return clone;
            }
        };

        private int id;
        private Project project;
        private Channel[] channels;
        private Color color;
        private int patternLength = 96;
        private int songLength = 16;
        private int barLength = 24;
        private string name;
        private int loopPoint = 0;
        private PatternCustomSetting[] patternCustomSettings = new PatternCustomSetting[Song.MaxLength];
        private int[] patternStartNote = new int[Song.MaxLength + 1];

        // These are specific to FamiTracker tempo mode
        private int famitrackerTempo = 150;
        private int famitrackerSpeed = 6;

        // These are for FamiStudio tempo mode
        private int noteLength = 10;

        public int Id => id;
        public Project Project => project;
        public Channel[] Channels => channels;
        public Color Color { get => color; set => color = value; }
        public string Name { get => name; set => name = value; }
        public int Length { get => songLength; }
        public int PatternLength { get => patternLength; }
        public int BarLength { get => barLength; }
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
            if (tempoMode == Project.TempoFamiStudio)
            {
                noteLength = 10;
                barLength = noteLength * 4;
            }
            else
            {
                famitrackerTempo = Song.NativeTempoNTSC;
                famitrackerSpeed = 10;
                barLength = 4;
            }

            patternLength = barLength * 4;
        }

        private void CreateCustomSettings()
        {
            for (int i = 0; i < patternCustomSettings.Length; i++)
                patternCustomSettings[i] = new PatternCustomSetting();
        }

        public void CreateChannels(bool preserve = false, int numChannelsToPreserve = Channel.ExpansionAudioStart)
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

            for (int i = 0; i < Channel.Count; i++)
            {
                var idx = Channel.ChannelTypeToIndex(i);
                if (project.IsChannelActive(i) && channels[idx] == null)
                    channels[idx] = new Channel(this, i, songLength);
            }
        }

        public void DuplicateInstancesWithDifferentLengths()
        {
            foreach (var channel in channels)
            {
                channel.DuplicateInstancesWithDifferentLengths();
            }
        }

        public bool Split(int factor)
        {
            DuplicateInstancesWithDifferentLengths();

            if (factor == 1)
                return true;

            var numNotes = patternLength / (UsesFamiStudioTempo ? noteLength : 1);

            if ((numNotes % factor) == 0)
            {
                var newSongLength = 0;
                var newLoopPoint = 0;
                var newPatternCustomSettings = new List<PatternCustomSetting>();
                var newPatternMap = new Dictionary<Pattern, Pattern[]>();
                var newNumNotes = numNotes / factor;
                
                // First check if we wont end up with more than 256 patterns.
                for (int p = 0; p < songLength; p++)
                {
                    var patternLen = GetPatternLength(p);
                    var patternNoteLength = UsesFamiStudioTempo ? GetPatternNoteLength(p) : 1;
                    var patternNewLen = newNumNotes * patternNoteLength;
                    var chunkCount = (int)Math.Ceiling(patternLen / (float)patternNewLen);

                    newSongLength += chunkCount;
                }

                if (newSongLength > MaxLength)
                    return false;

                newSongLength = 0;

                var oldChannelPatterns  = new Pattern[channels.Length][];
                var oldChannelInstances = new Pattern[channels.Length][];

                for (int c = 0; c < channels.Length; c++)
                {
                    var channel = channels[c];

                    oldChannelPatterns[c]  = channel.Patterns.ToArray();
                    oldChannelInstances[c] = channel.PatternInstances.Clone() as Pattern[];

                    Array.Clear(channel.PatternInstances, 0, channel.PatternInstances.Length);

                    channel.Patterns.Clear();
                }

                for (int p = 0; p < songLength; p++)
                {
                    var patternLen        = GetPatternLength(p);
                    var patternBarLength  = UsesFamiStudioTempo ? GetPatternBarLength(p)  : barLength;
                    var patternNoteLength = UsesFamiStudioTempo ? GetPatternNoteLength(p) : 1;
                    var patternNumNotes   = patternLen / patternNoteLength;
                    var patternNewLen     = newNumNotes * patternNoteLength;

                    var chunkCount = (int)Math.Ceiling(patternLen / (float)patternNewLen);

                    if (p == loopPoint)
                        newLoopPoint = newSongLength;

                    if (patternCustomSettings[p].patternLength == 0)
                    {
                        for (int i = 0; i < chunkCount; i++)
                            newPatternCustomSettings.Add(new PatternCustomSetting());
                    }
                    else
                    {
                        for (int i = 0, notesLeft = patternLen; i < chunkCount; i++, notesLeft -= patternNewLen)
                        {
                            var customSettings = new PatternCustomSetting();
                            customSettings.useCustomSettings = true;
                            customSettings.patternLength = Math.Min(patternNewLen, notesLeft);
                            customSettings.barLength = patternBarLength;
                            customSettings.noteLength = patternNoteLength;
                            newPatternCustomSettings.Add(customSettings);
                        }
                    }

                    newSongLength += chunkCount;

                    for (int c = 0; c < channels.Length; c++)
                    {
                        var channel = channels[c];
                        var pattern = oldChannelInstances[c][p];

                        if (pattern != null)
                        {
                            Pattern[] splitPatterns = null;

                            if (!newPatternMap.TryGetValue(pattern, out splitPatterns))
                            {
                                splitPatterns = new Pattern[chunkCount];

                                for (int i = 0, notesLeft = patternLen; i < chunkCount; i++, notesLeft -= patternNewLen)
                                {
                                    splitPatterns[i] = new Pattern(Project.GenerateUniqueId(), this, channel.Type, channel.GenerateUniquePatternName(pattern.Name));
                                    splitPatterns[i].Color = pattern.Color;

                                    var noteIdx0 = (i + 0) * patternNewLen;
                                    var noteIdx1 = noteIdx0 + Math.Min(patternNewLen, notesLeft);

                                    foreach (var kv in pattern.Notes)
                                    {
                                        if (kv.Key >= noteIdx0 && kv.Key < noteIdx1)
                                            splitPatterns[i].SetNoteAt(kv.Key - noteIdx0, kv.Value.Clone());
                                    }

                                    splitPatterns[i].ClearLastValidNoteCache();
                                }

                                newPatternMap[pattern] = splitPatterns;
                                channel.Patterns.AddRange(splitPatterns);
                            }

                            Debug.Assert(splitPatterns.Length == chunkCount);

                            for (int i = 0; i < splitPatterns.Length; i++)
                                channel.PatternInstances[newPatternCustomSettings.Count - chunkCount + i] = splitPatterns[i];
                        }
                    }
                }

                while (newPatternCustomSettings.Count != MaxLength)
                    newPatternCustomSettings.Add(new PatternCustomSetting());

                patternCustomSettings = newPatternCustomSettings.ToArray();

                patternLength = (patternLength / (UsesFamiStudioTempo ? noteLength : 1)) / factor * (UsesFamiStudioTempo ? noteLength : 1);
                songLength = newSongLength;
                loopPoint = newLoopPoint;

                UpdatePatternStartNotes();

                return true;
            }

            return false;
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
            barLength = Math.Min(barLength, patternLength);

            UpdatePatternStartNotes();
        }

        public void SetBarLength(int newBarLength)
        {
            if (barLength < patternLength)
                barLength = newBarLength;
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

        public void SetSensibleBarLength()
        {
            if (UsesFamiTrackerTempo)
            {
                var barLengths = Utils.GetFactors(patternLength);
                barLength = barLengths[barLengths.Length / 2];
            }
            else
            {
                barLength = noteLength * 4;
                while (barLength > patternLength)
                    barLength /= 2;
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

        public void SetPatternCustomSettings(int patternIdx, int customPatternLength, int customNoteLength = 0, int customBarLength = 0)
        {
            Debug.Assert(customPatternLength > 0 && customPatternLength < Pattern.MaxLength);

            patternCustomSettings[patternIdx].Clear();
            patternCustomSettings[patternIdx].useCustomSettings = true;

            if (project.UsesFamiTrackerTempo)
            {
                Debug.Assert(customNoteLength == 0);
                Debug.Assert(customBarLength == 0);

                patternCustomSettings[patternIdx].patternLength = customPatternLength;
            }
            else
            {
                Debug.Assert(customPatternLength % customNoteLength == 0);
                Debug.Assert(customNoteLength != 0);
                Debug.Assert(customBarLength != 0);

                patternCustomSettings[patternIdx].patternLength = customPatternLength;
                patternCustomSettings[patternIdx].barLength = customBarLength;
                patternCustomSettings[patternIdx].noteLength = customNoteLength;
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

        public int GetPatternBarLength(int patternIdx)
        {
            var settings = patternCustomSettings[patternIdx];
            return settings.useCustomSettings && UsesFamiStudioTempo ? settings.barLength : barLength;
        }
        
        public int GetPatternLength(int patternIdx)
        {
            var settings = patternCustomSettings[patternIdx];
            return settings.useCustomSettings ? settings.patternLength : patternLength;
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

        public void ResizeNotes(int newNoteLength, bool convert)
        {
            Debug.Assert(UsesFamiStudioTempo);
            Debug.Assert(newNoteLength >= MinNoteLength && newNoteLength <= MaxNoteLength);

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
        }

        public static int ComputeFamiTrackerBPM(bool palPlayback, int speed, int tempo)
        {
            return tempo * (palPlayback ? 5 : 6) / speed;
        }

        public static int ComputeFamiStudioBPM(bool palSource, int noteLength)
        {
            return (palSource? 750 : 900) / noteLength;
        }

        public int BPM
        {
            get
            {
                if (UsesFamiStudioTempo)
                    return ComputeFamiStudioBPM(project.PalMode, noteLength);
                else
                    return ComputeFamiTrackerBPM(project.PalMode, famitrackerSpeed, famitrackerTempo);
            }
        }

        public bool UsesDpcm
        {
            get
            {
                for (int p = 0; p < songLength; p++)
                {
                    var pattern = channels[Channel.Dpcm].PatternInstances[p];
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
                                SetPatternCustomSettings(i, kv.Key + 1);
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
            int newBarLength = barLength * newNoteLength;
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
                    patternCustomSettings[p].barLength     = Math.Max(patternCustomSettings[p].barLength / famitrackerSpeed, 1);
                }
            }

            noteLength    = newNoteLength;
            barLength     = newBarLength;
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

                            if (note.Instrument != null && !channel.SupportsInstrument(note.Instrument) || channel.Type == Channel.Dpcm)
                                note.Instrument = null;
                        }
                    }
                }
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
                return CountNotesBetween(p0, n0, p1, n1);
            }
        }

        public void AdvanceNumberOfFrames(int frameCount, int initialCount, int currentSpeed, bool pal, ref int p, ref int n)
        {
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
            buffer.Serialize(ref barLength);
            buffer.Serialize(ref name);
            buffer.Serialize(ref famitrackerTempo);
            buffer.Serialize(ref famitrackerSpeed);
            buffer.Serialize(ref color);

            // At version 5 (FamiStudio 2.0.0), we replaced the jump/skips effects by loop points and custom pattern length and we added a new tempo mode.
            if (buffer.Version >= 5)
            {
                buffer.Serialize(ref loopPoint);
                buffer.Serialize(ref noteLength);

                for (int i = 0; i < songLength; i++)
                {
                    buffer.Serialize(ref patternCustomSettings[i].useCustomSettings);
                    buffer.Serialize(ref patternCustomSettings[i].patternLength);
                    buffer.Serialize(ref patternCustomSettings[i].noteLength);
                    buffer.Serialize(ref patternCustomSettings[i].barLength);
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
    }
}
