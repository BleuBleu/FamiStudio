using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace FamiStudio
{
    public class Song
    {
        public const int MaxLength = 256;

        struct PatternCustomSetting
        {
            public byte patternLength; // 0 = default song pattern length
            public byte noteLength;    // 0 = default song note length (only used by FamiStudio tempo)
            public byte barLength;     // 0 = default song bar length (only used by FamiStudio tempo)
        }

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
        private int noteLength = 10;
        private int palFrameSkipPattern = GetDefaultPalFrameSkipPattern(10);

        public int Id => id;
        public Project Project => project;
        public Channel[] Channels => channels;
        public Color Color { get => color; set => color = value; }
        public string Name { get => name; set => name = value; }
        public int Length { get => songLength; }
        public int DefaultPatternLength { get => patternLength; }
        public int BarLength { get => barLength; }
        public int LoopPoint { get => loopPoint; }

        public Song()
        {
            // For serialization.
        }

        public Song(Project project, int id, string name)
        {
            this.project = project;
            this.id = id;
            this.name = name;
            this.color = Color.Azure;

            if (project.TempoMode == Project.TempoFamiStudio)
            {
                noteLength = 10;
                palFrameSkipPattern = GetDefaultPalFrameSkipPattern(noteLength);
                barLength = noteLength * 4;
            }
            else
            {
                famitrackerTempo = 150;
                famitrackerSpeed = 10;
                barLength = 4;
            }

            patternLength = barLength * 4;

            CreateChannels();
            UpdatePatternStartNotes();
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
                        newPatternCustomSettings.AddRange(new PatternCustomSetting[chunkCount]);
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

                patternCustomSettings = newPatternCustomSettings.ToArray();
                Array.Resize(ref patternCustomSettings, MaxLength);

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

            for (int i = 0; i < songLength; i++)
                SetPatternLength(i, patternCustomSettings[i].patternLength);

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

        public static int[] GenerateBarLengths(int patternLen)
        {
            var barLengths = new List<int>();

            for (int i = patternLen; i >= 2; i--)
            {
                if (patternLen % i == 0)
                {
                    barLengths.Add(i);
                }
            }

            return barLengths.ToArray();
        }

        public void SetSensibleBarLength()
        {
            if (project.TempoMode == Project.TempoFamiTracker)
            {
                var barLengths = GenerateBarLengths(patternLength);
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

        public void SetPatternLength(int patternIdx, int len)
        {
            if (len <= 0 || len >= Pattern.MaxLength)
                patternCustomSettings[patternIdx].patternLength = 0;
            else
                patternCustomSettings[patternIdx].patternLength = (byte)len;

            UpdatePatternStartNotes();
        }

        public bool PatternHasCustomLength(int patternIdx)
        {
            return patternCustomSettings[patternIdx].patternLength != 0;
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

        public void RemoveEmptyPatterns()
        {
            foreach (var channel in channels)
                channel.RemoveEmptyPatterns();   
        }

        public void CleanupUnusedPatterns()
        {
            foreach (var channel in channels)
                channel.CleanupUnusedPatterns();
        }

        public void ClearNotesPastMaxInstanceLength()
        {
            foreach (var channel in channels)
                channel.ClearNotesPastMaxInstanceLength();
        }

        public override string ToString()
        {
            return name;
        }

        public int NoteLength
        {
            get
            {
                Debug.Assert(project.TempoMode == Project.TempoFamiStudio);
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

        public void SetNoteLength(int newNoteLength, bool convert)
        {
            // MATTT: Handle custom pattern note lengths and stuff.
            if (convert)
            {
                Debug.Assert(patternLength % noteLength == 0);

                foreach (var channel in channels)
                {
                    // MATTT custom pattern length.
                    foreach (var pattern in channel.Patterns)
                    {
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
                        for (int i = 0; i < patternLength / noteLength; i++)
                        {
                            for (int j = 0; j < noteLength; j++)
                            {
                                int oldIdx = i * noteLength + j;
                                int newIdx = i * newNoteLength + (int)Math.Round(j / (float)(noteLength - 1) * (newNoteLength - 1));

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
                    }
                }
            }

            noteLength = newNoteLength;
        }

        public int PalFrameSkipPattern
        {
            get
            {
                Debug.Assert(project.TempoMode == Project.TempoFamiStudio);
                return palFrameSkipPattern;
            }
            set
            {
                Debug.Assert(project.TempoMode == Project.TempoFamiStudio);
                Debug.Assert(Utils.NumberOfSetBits(value) == (noteLength - PalNoteLengthLookup[noteLength]));
                palFrameSkipPattern = value;
            }
        }
        
        public int FamitrackerTempo
        {
            get
            {
                // Debug.Assert(project.TempoMode == Project.TempoFamiTracker); MATTT
                return famitrackerTempo;
            }
            set
            {
                Debug.Assert(project.TempoMode == Project.TempoFamiTracker);
                famitrackerTempo = value;
            }
        }


        public int FamitrackerSpeed
        {
            get
            {
                // Debug.Assert(project.TempoMode == Project.TempoFamiTracker); MATTT
                return famitrackerSpeed;
            }
            set
            {
                //Debug.Assert(project.TempoMode == Project.TempoFamiTracker);
                famitrackerSpeed = value;
            }
        }

        // For a given number of NTSC frames (60Hz), the number of PAL frames (50Hz)
        // that minimizes the tempo error.
        public readonly static int[] PalNoteLengthLookup = new[]
        {
            0,  // 0 (unused)
            1,  // 1 (unused)
            2,  // 2 (unused)
            2,  // 3
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

        public static int ComputeBPM(int noteLength)
        {
            return 900 / noteLength;
        }
        
        public static float ComputePalError(int noteLength)
        {
            float ntsc = (1000.0f / 60.0988f) * noteLength;
            float pal  = (1000.0f / 50.0070f) * PalNoteLengthLookup[noteLength];
            float diff = Math.Abs(ntsc - pal);
            return diff * 100.0f / Math.Max(ntsc, pal);
        }

        public static int GetDefaultPalFrameSkipPattern(int noteLength)
        {
            int numSkipFrames = noteLength - PalNoteLengthLookup[noteLength];

            // By default, put the skip frames in the middle of the notes, this is
            // where its least likely to have anything interesting (attacks tend
            // to be at the beginning, stop notes at the end).
            int pattern = 0;
            for (int i = 0; i < numSkipFrames; i++)
            {
                float ratio = (i + 0.5f) / (numSkipFrames);
                int note = (int)Math.Round(ratio * (noteLength - 1));
                pattern |= (1 << note);
            }

            return pattern;
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
                                SetPatternLength(i, kv.Key + 1);
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

            // At version 5 (FamiStudio 1.5.0), we replaced the jump/skips effects by loop points and custom pattern length.
            if (buffer.Version >= 5)
            {
                buffer.Serialize(ref loopPoint);

                for (int i = 0; i < MaxLength; i++)
                {
                    buffer.Serialize(ref patternCustomSettings[i].patternLength);
                    buffer.Serialize(ref patternCustomSettings[i].noteLength);
                    buffer.Serialize(ref patternCustomSettings[i].barLength);
                }
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
