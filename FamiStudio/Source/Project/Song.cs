using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace FamiStudio
{
    public class Song
    {
        public const int MaxLength = 256;
        public const int MaxNoteLength = 16;

        class PatternCustomSetting
        {
            public int   patternLength;
            public int   noteLength;
            public int   barLength;
            public int[] palSkipFrames = new int[2];

            public void Clear()
            {
                patternLength = 0;
                noteLength = 0;
                barLength = 0;
                palSkipFrames[0] = 0;
                palSkipFrames[1] = 0;
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
        private int[] patternStartNote = new int[Song.MaxLength];

        // These are specific to FamiTracker tempo mode
        private int famitrackerTempo = 150;
        private int famitrackerSpeed = 6;

        // These are for FamiStudio tempo mode
        private int   noteLength = 10;
        private int[] palSkipFrames = new int[2];

        public int Id => id;
        public Project Project => project;
        public Channel[] Channels => channels;
        public Color Color { get => color; set => color = value; }
        public string Name { get => name; set => name = value; }
        public int Length { get => songLength; }
        public int PatternLength { get => patternLength; }
        public int BarLength { get => barLength; }
        public int LoopPoint { get => loopPoint; }
        public bool UsesFamiStudioTempo  => project.UsesFamiStudioTempo;
        public bool UsesFamiTrackerTempo => project.UsesFamiTrackerTempo;
        public int FamitrackerTempo { get => famitrackerTempo; set => famitrackerTempo = value; }
        public int FamitrackerSpeed { get => famitrackerSpeed; set => famitrackerSpeed = value; }
        public int[] PalSkipFrames => palSkipFrames;

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
            this.color = Color.Azure;

            CreateCustomSettings();
            GetDefaultPalSkipFrames(noteLength, palSkipFrames);
            SetDefaultsForTempoMode(project.TempoMode);
            CreateChannels();
            UpdatePatternStartNotes();
        }

        public void SetDefaultsForTempoMode(int tempoMode)
        {
            if (tempoMode == Project.TempoFamiStudio)
            {
                noteLength = 10;
                GetDefaultPalSkipFrames(noteLength, palSkipFrames);
                barLength = noteLength * 4;
            }
            else
            {
                famitrackerTempo = 150;
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

            // MATTT: Make sure that makes sense with FamiStudio tempo, we dont want to split notes in 1/2.
            if ((patternLength % factor) == 0 && (songLength * factor) < MaxLength)
            {
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

                var newSongLength = 0;
                var newLoopPoint = 0;
                var newPatternCustomSettings = new List<PatternCustomSetting>();
                var newPatternMap = new Dictionary<Pattern, Pattern[]>();
                var newPatternLength = patternLength / factor;

                for (int p = 0; p < songLength; p++)
                {
                    var patternLen = GetPatternLength(p);
                    var chunkCount = (int)Math.Ceiling(patternLen / (float)newPatternLength);

                    if (p == loopPoint)
                        newLoopPoint = newSongLength;

                    if (patternCustomSettings[p].patternLength == 0)
                    {
                        for (int i = 0; i < chunkCount;i++)
                            newPatternCustomSettings.Add(new PatternCustomSetting());
                    }
                    else
                    {
                        for (int i = 0, notesLeft = patternLen; i < chunkCount; i++, notesLeft -= newPatternLength)
                        {
                            var newLength = (byte)Math.Min(newPatternLength, notesLeft);
                            newPatternCustomSettings.Add(new PatternCustomSetting() { patternLength = newLength == newPatternLength ? (byte)0 : newLength });
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

                                for (int i = 0, notesLeft = patternLen; i < chunkCount; i++, notesLeft -= newPatternLength)
                                {
                                    splitPatterns[i] = new Pattern(Project.GenerateUniqueId(), this, channel.Type, channel.GenerateUniquePatternName(pattern.Name));
                                    splitPatterns[i].Color = pattern.Color;

                                    var noteIdx0 = (i + 0) * newPatternLength;
                                    var noteIdx1 = noteIdx0 + Math.Min(newPatternLength, notesLeft);

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

                patternLength = newPatternLength;
                songLength = newSongLength;
                loopPoint = newLoopPoint;
                barLength /= factor;

                if (barLength <= 1)
                    barLength = 2;

                if ((patternLength % barLength) != 0)
                    SetSensibleBarLength();

                UpdatePatternStartNotes();

                return true;
            }

            return false;
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

        public void SetPatternCustomSettings(int patternIdx, int customPatternLength, int customNoteLength = 0, int customBarLength = 0, int[] palSkipFrames = null)
        {
            Debug.Assert(customPatternLength > 0 && customPatternLength < Pattern.MaxLength);

            patternCustomSettings[patternIdx].Clear();

            if (project.UsesFamiTrackerTempo)
            {
                Debug.Assert(customNoteLength == 0);
                Debug.Assert(customBarLength  == 0);

                patternCustomSettings[patternIdx].patternLength = customPatternLength;
            }
            else
            {
                Debug.Assert(customPatternLength % customNoteLength == 0);
                Debug.Assert(customNoteLength != 0);
                Debug.Assert(customBarLength != 0);
                Debug.Assert(palSkipFrames.Length == 2);

                patternCustomSettings[patternIdx].patternLength = customPatternLength;
                patternCustomSettings[patternIdx].barLength = customBarLength;
                patternCustomSettings[patternIdx].noteLength = customNoteLength;
                patternCustomSettings[patternIdx].palSkipFrames[0] = palSkipFrames[0];
                patternCustomSettings[patternIdx].palSkipFrames[1] = palSkipFrames[1];
            }

            UpdatePatternStartNotes();
        }

        public bool PatternHasCustomSettings(int patternIdx)
        {
            return patternCustomSettings[patternIdx].patternLength != 0;
        }

        public int GetPatternNoteLength(int patternIdx)
        {
            int len = patternCustomSettings[patternIdx].noteLength;
            Debug.Assert(UsesFamiStudioTempo || len == 0);
            return len == 0 ? noteLength : len;
        }

        public int GetPatternBarLength(int patternIdx)
        {
            int len = patternCustomSettings[patternIdx].barLength;
            Debug.Assert(UsesFamiStudioTempo || len == 0);
            return len == 0 ? barLength : len;
        }

        public int[] GetPatternPalSkipFrames(int patternIdx)
        {
            int len = patternCustomSettings[patternIdx].patternLength;
            Debug.Assert(UsesFamiStudioTempo || len == 0);
            return len == 0 ? palSkipFrames : patternCustomSettings[patternIdx].palSkipFrames;
        }

        public int GetPatternLength(int patternIdx)
        {
            int len = patternCustomSettings[patternIdx].patternLength;
            return len == 0 ? patternLength : len;
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
                        int oldIdx = i * oldNoteLength + j;
                        int newIdx = i * newNoteLength + (int)Math.Round(j / (float)(oldNoteLength - 1) * (newNoteLength - 1));

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
            Debug.Assert(newNoteLength > 0 && newNoteLength <= MaxNoteLength);

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

        // For a given number of NTSC frames (60Hz), the number of PAL frames (50Hz)
        // that minimizes the tempo error.
        public readonly static int[] PalNoteLengthLookup = new[]
        {
            0,  // 0 (unused)
            1,  // 1 (terrible)
            2,  // 2 (terrible)
            3,  // 3 (terrible)
            3,  // 4
            4,  // 5
            5,  // 6
            6,  // 7
            7,  // 8
            8,  // 9
            8,  // 10
            9,  // 11
            10, // 12
            11, // 13
            12, // 14
            13, // 15
            14  // 16
        };

        public static int ComputeFamiTrackerBPM(int speed, int tempo)
        {
            return tempo * 6 / speed;
        }

        public static int ComputeFamiStudioBPM(int noteLength)
        {
            return 900 / noteLength;
        }

        public int BPM
        {
            get
            {
                if (UsesFamiStudioTempo)
                    return ComputeFamiStudioBPM(noteLength);
                else
                    return ComputeFamiTrackerBPM(famitrackerSpeed, famitrackerTempo);
            }
        }
        
        public static float ComputePalError(int noteLength)
        {
            float ntsc = (1000.0f / 60.0988f) * noteLength;
            float pal  = (1000.0f / 50.0070f) * PalNoteLengthLookup[noteLength];
            float diff = Math.Abs(ntsc - pal);
            return diff * 100.0f / Math.Max(ntsc, pal);
        }

        public static int GetNumPalSkipFrames(int noteLength)
        {
            return noteLength - PalNoteLengthLookup[noteLength];
        }

        public static void GetDefaultPalSkipFrames(int noteLength, int[] frames)
        {
            int numSkipFrames = GetNumPalSkipFrames(noteLength);

            Debug.Assert(numSkipFrames >= 0 && numSkipFrames <= 2);

            frames[0] = -1;
            frames[1] = -1;

            // By default, put the skip frames in the middle of the notes, this is
            // where its least likely to have anything interesting (attacks tend
            // to be at the beginning, stop notes at the end).
            for (int i = 0; i < numSkipFrames; i++)
            {
                float ratio = (i + 0.5f) / (numSkipFrames);
                frames[i] = (int)Math.Round(ratio * (noteLength - 1));
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
        public void Validate(Project project)
        {
            Debug.Assert(project.Songs.Contains(this));
            Debug.Assert(project.GetSong(id) == this);

            var uniqueNotes    = new HashSet<Note>();
            var uniquePatterns = new HashSet<Pattern>();

            foreach (var channel in channels)
            {
                channel.Validate(this);

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

            var oldPatternInstancesStartNote = new int[MaxLength];
            Array.Copy(patternStartNote, oldPatternInstancesStartNote, MaxLength);
            UpdatePatternStartNotes();
            for (int i = 0; i < MaxLength; i++)
                Debug.Assert(oldPatternInstancesStartNote[i] == patternStartNote[i]);

            var numSkipFrames = (palSkipFrames[0] >= 0 ? 1 : 0) + (palSkipFrames[1] >= 0 ? 1 : 0);
            Debug.Assert(numSkipFrames == GetNumPalSkipFrames(noteLength));
            Debug.Assert(numSkipFrames != 2 || palSkipFrames[0] != palSkipFrames[1]);
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
            int newNoteLength    = famitrackerSpeed;
            int newBarLength     = barLength * newNoteLength;
            int newPatternLength = patternLength * newNoteLength;

            // MATTT: Custom pattern tempo.
            foreach (var channel in channels)
            {
                foreach (var pattern in channel.Patterns)
                {
                    var notesCopy = new SortedList<int, Note>(pattern.Notes);

                    pattern.Notes.Clear();
                    foreach (var kv in notesCopy)
                    {
                        var note = kv.Value;
                        note.ClearEffectValue(Note.EffectSpeed);
                        pattern.Notes[kv.Key * newNoteLength] = note;
                    }

                    pattern.ClearLastValidNoteCache();
                }
            }

            noteLength    = newNoteLength;
            barLength     = newBarLength;
            patternLength = newPatternLength;

            UpdatePatternStartNotes();
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

            // At version 5 (FamiStudio 1.5.0), we replaced the jump/skips effects by loop points and custom pattern length and we added a new tempo mode.
            if (buffer.Version >= 5)
            {
                buffer.Serialize(ref loopPoint);
                buffer.Serialize(ref noteLength);
                buffer.Serialize(ref palSkipFrames[0]);
                buffer.Serialize(ref palSkipFrames[1]);

                for (int i = 0; i < songLength; i++)
                {
                    buffer.Serialize(ref patternCustomSettings[i].patternLength);
                    buffer.Serialize(ref patternCustomSettings[i].noteLength);
                    buffer.Serialize(ref patternCustomSettings[i].barLength);
                    buffer.Serialize(ref patternCustomSettings[i].palSkipFrames[0]);
                    buffer.Serialize(ref patternCustomSettings[i].palSkipFrames[1]);
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
        }
    }
}
