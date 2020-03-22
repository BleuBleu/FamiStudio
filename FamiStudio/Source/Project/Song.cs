using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace FamiStudio
{
    public class Song
    {
        public const int MaxLength = 256;

        private int id;
        private Project project;
        private Channel[] channels;
        private Color color;
        private int patternLength = 256;
        private int songLength = 64;
        private int barLength = 16;
        private string name;
        private int tempo = 150;
        private int speed = 6;
        private int loopPoint = 0;
        private byte[] patternLengths = new byte[Song.MaxLength]; // 0 = default song pattern length
        private int[] patternStartNote = new int[Song.MaxLength];

        public int Id => id;
        public Project Project => project;
        public Channel[] Channels => channels;
        public Color Color { get => color; set => color = value; }
        public string Name { get => name; set => name = value; }
        public int Tempo { get => tempo; set => tempo = value; }
        public int Speed { get => speed; set => speed = value; }
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

            if ((patternLength % factor) == 0 && (songLength * factor) < MaxLength)
            {
                var oldChannelPatterns = new Pattern[channels.Length][];
                var oldChannelPatternInstances = new Pattern[channels.Length][];

                for (int c = 0; c < channels.Length; c++)
                {
                    var channel = channels[c];

                    oldChannelPatterns[c] = new Pattern[channel.Patterns.Count];
                    oldChannelPatterns[c] = channel.Patterns.ToArray();

                    oldChannelPatternInstances[c] = new Pattern[channel.PatternInstances.Length];
                    oldChannelPatternInstances[c] = channel.PatternInstances.Clone() as Pattern[];

                    channel.Patterns.Clear();
                }

                var newSongLength = 0;
                var newLoopPoint = 0;
                var newPatternLengths = new List<byte>();
                var newPatternMap = new Dictionary<Pattern, Pattern[]>();
                var newPatternLength = patternLength / factor;

                for (int p = 0; p < songLength; p++)
                {
                    var instLen = GetPatternLength(p);
                    var chunkCount = (int)Math.Ceiling(instLen / (float)newPatternLength);

                    if (p == loopPoint)
                        newLoopPoint = newSongLength;

                    if (patternLengths[p] == 0)
                    {
                        newPatternLengths.AddRange(new byte[chunkCount]);
                    }
                    else
                    {
                        for (int i = 0, notesLeft = instLen; i < chunkCount; i++, notesLeft -= newPatternLength)
                        {
                            var newLength = (byte)Math.Min(newPatternLength, notesLeft);
                            newPatternLengths.Add(newLength == newPatternLength ? (byte)0 : newLength);
                        }
                    }

                    newSongLength += chunkCount;

                    for (int c = 0; c < channels.Length; c++)
                    {
                        var channel = channels[c];
                        var pattern = oldChannelPatternInstances[c][p];

                        if (pattern != null)
                        {
                            Pattern[] splitPatterns = null;

                            if (!newPatternMap.TryGetValue(pattern, out splitPatterns))
                            {
                                splitPatterns = new Pattern[chunkCount];

                                for (int i = 0, notesLeft = instLen; i < chunkCount; i++, notesLeft -= newPatternLength)
                                {
                                    splitPatterns[i] = new Pattern(Project.GenerateUniqueId(), this, channel.Type, channel.GenerateUniquePatternName(pattern.Name));
                                    splitPatterns[i].Color = pattern.Color;
                                    Array.Copy(pattern.Notes, newPatternLength * i, splitPatterns[i].Notes, 0, Math.Min(newPatternLength, notesLeft));
                                    splitPatterns[i].UpdateLastValidNotes();
                                }

                                newPatternMap[pattern] = splitPatterns;
                                channel.Patterns.AddRange(splitPatterns);
                            }

                            Debug.Assert(splitPatterns.Length == chunkCount);

                            for (int i = 0; i < splitPatterns.Length; i++)
                                channel.PatternInstances[newPatternLengths.Count - chunkCount + i] = splitPatterns[i];
                        }
                    }
                }

                patternLengths = newPatternLengths.ToArray();
                patternLength = newPatternLength;
                songLength = newSongLength;
                loopPoint = newLoopPoint;
                barLength /= factor;

                if (barLength <= 1)
                    barLength = 2;

                if ((patternLength % barLength) != 0)
                    SetSensibleBarLength();

                UpdatePatternsMaxInstanceLength();
                UpdatePatternStartNotes();

                return true;
            }

            return false;
        }

        public void SetLength(int newLength)
        {
            songLength = newLength;

            foreach (var channel in channels)
                channel.ClearPatternsInstancesPastSongLength();

            if (loopPoint >= songLength)
                loopPoint = 0;

            UpdatePatternsMaxInstanceLength();
            UpdatePatternStartNotes();
        }

        public void SetDefaultPatternLength(int newLength)
        {
            patternLength = newLength;

            for (int i = 0; i < songLength; i++)
                SetPatternLength(i, patternLengths[i]);

            UpdatePatternsMaxInstanceLength();
            UpdatePatternStartNotes();
        }

        public void SetBarLength(int newBarLength)
        {
            if (Array.IndexOf(GenerateBarLengths(patternLength), newBarLength) >= 0)
            {
                barLength = newBarLength;
            }
        }

        public void SetLoopPoint(int loop)
        {
            loopPoint = Math.Min(loop, songLength - 1);
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
            var barLengths = GenerateBarLengths(patternLength);
            barLength = barLengths[barLengths.Length / 2];
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
            if (len <= 0 || len >= patternLength)
                patternLengths[patternIdx] = 0;
            else
                patternLengths[patternIdx] = (byte)len;

            UpdatePatternStartNotes();
        }

        public bool PatternHasCustomLength(int patternIdx)
        {
            return patternLengths[patternIdx] != 0;
        }

        public int GetPatternLength(int patternIdx)
        {
            int len = patternLengths[patternIdx];
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
            {
                channel.RemoveEmptyPatterns();   
            }
        }

        public void CleanupUnusedPatterns()
        {
            foreach (var channel in channels)
            {
                channel.CleanupUnusedPatterns();
            }
        }

        public override string ToString()
        {
            return name;
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
                        for (int i = 0; i < patternLength; i++)
                        {
                            var note = pattern.Notes[i];
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

            foreach (var channel in channels)
                channel.Validate(this);

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
                        for (int j = 0; j < patternLength; j++)
                        {
                            // Converts old Jump effects to loop points.
                            // The first Jump effect will give us our loop point.
                            if (loopPoint == 0 && pattern.Notes[j].FxJump != 0xff)
                            {
                                SetLoopPoint(pattern.Notes[j].FxJump);
                            }

                            // Converts old Skip effects to custom pattern instances lengths.
                            if (pattern.Notes[j].FxSkip != 0xff)
                            {
                                SetPatternLength(i, j + 1);
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

        public void UpdatePatternsMaxInstanceLength()
        {
            foreach (var channel in channels)
                channel.UpdatePatternsMaxInstanceLength();
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

        public void SerializeState(ProjectBuffer buffer)
        {
            if (buffer.IsReading)
                project = buffer.Project;

            buffer.Serialize(ref id, true);
            buffer.Serialize(ref patternLength);
            buffer.Serialize(ref songLength);
            buffer.Serialize(ref barLength);
            buffer.Serialize(ref name);
            buffer.Serialize(ref tempo);
            buffer.Serialize(ref speed);
            buffer.Serialize(ref color);

            // At version 5 (FamiStudio 1.5.0), we replaced the jump/skips effects by loop points and custom pattern length.
            if (buffer.Version >= 5)
            {
                buffer.Serialize(ref loopPoint);
                buffer.Serialize(ref patternLengths);
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

            // Needs to be done after converting the jump/skip effects.
            if (buffer.IsReading)
            {
                foreach (var channel in channels)
                    channel.UpdatePatternsMaxInstanceLength();
            }
        }
    }
}
