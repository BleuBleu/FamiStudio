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
        private byte[] patternInstanceLengths = new byte[Song.MaxLength]; // 0 = default song pattern length
        private int[] patternInstancesStartNote = new int[Song.MaxLength];

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
            UpdatePatternInstancesStartNotes();
        }

        public void CreateChannels(bool preserve = false)
        {
            int channelCount = project.GetActiveChannelCount();

            if (preserve)
                Array.Resize(ref channels, channelCount);
            else
                channels = new Channel[channelCount];

            int idx = preserve ? Channel.ExpansionAudioStart : 0;
            for (int i = idx; i < Channel.Count; i++)
            {
                if (project.IsChannelActive(i))
                    channels[idx++] = new Channel(this, i, songLength);
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
                var oldChannelPatternInstances = new PatternInstance[channels.Length][];

                for (int c = 0; c < channels.Length; c++)
                {
                    var channel = channels[c];

                    oldChannelPatterns[c] = new Pattern[channel.Patterns.Count];
                    oldChannelPatterns[c] = channel.Patterns.ToArray();

                    oldChannelPatternInstances[c] = new PatternInstance[channel.PatternInstances.Length];
                    oldChannelPatternInstances[c] = channel.PatternInstances.Clone() as PatternInstance[];

                    channel.Patterns.Clear();
                    channel.CreatePatternInstances();
                }

                var newSongLength = 0;
                var newLoopPoint = 0;
                var newPatternInstanceLengths = new List<byte>();
                var newPatternMap = new Dictionary<Pattern, Pattern[]>();
                var newPatternLength = patternLength / factor;

                for (int p = 0; p < songLength; p++)
                {
                    var instLen = GetPatternInstanceLength(p);
                    var chunkCount = (int)Math.Ceiling(instLen / (float)newPatternLength);

                    if (p == loopPoint)
                        newLoopPoint = newSongLength;

                    if (patternInstanceLengths[p] == 0)
                    {
                        newPatternInstanceLengths.AddRange(new byte[chunkCount]);
                    }
                    else
                    {
                        for (int i = 0, notesLeft = instLen; i < chunkCount; i++, notesLeft -= newPatternLength)
                        {
                            var newLength = (byte)Math.Min(newPatternLength, notesLeft);
                            newPatternInstanceLengths.Add(newLength == newPatternLength ? (byte)0 : newLength);
                        }
                    }

                    newSongLength += chunkCount;

                    for (int c = 0; c < channels.Length; c++)
                    {
                        var channel  = channels[c];
                        var instance = oldChannelPatternInstances[c][p];
                        var pattern  = instance.Pattern;

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
                                }

                                newPatternMap[pattern] = splitPatterns;
                                channel.Patterns.AddRange(splitPatterns);
                            }

                            Debug.Assert(splitPatterns.Length == chunkCount);

                            for (int i = 0; i < splitPatterns.Length; i++)
                            {
                                var inst = channel.PatternInstances[newPatternInstanceLengths.Count - chunkCount + i];
                                inst.Pattern = splitPatterns[i];
                                inst.UpdateLastValidNote();
                            }
                        }
                    }
                }

                patternInstanceLengths = newPatternInstanceLengths.ToArray();
                patternLength = newPatternLength;
                songLength = newSongLength;
                loopPoint = newLoopPoint;
                barLength /= factor;

                if (barLength <= 1)
                    barLength = 2;

                if ((patternLength % barLength) != 0)
                    SetSensibleBarLength();

                UpdatePatternsMaxInstanceLength();
                UpdatePatternInstancesStartNotes();

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

            for (int i = songLength; i < Song.MaxLength; i++)
                patternInstanceLengths[i] = 0;
        }

        public void SetPatternLength(int newLength)
        {
            patternLength = newLength;

            UpdatePatternsMaxInstanceLength();
            UpdatePatternInstancesStartNotes();

            foreach (var channel in channels)
                channel.ClearNotesPastSongLength();
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
            loopPoint = Utils.Clamp(loop, 0, songLength - 1);
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

        public void SetPatternInstanceLength(int patternIdx, int len)
        {
            if (len <= 0 || len >= Pattern.MaxLength || len == patternLength)
                patternInstanceLengths[patternIdx] = 0;
            else
                patternInstanceLengths[patternIdx] = (byte)len;

            UpdatePatternInstancesStartNotes();
        }

        public bool PatternInstanceHasCustomLength(int patternIdx)
        {
            return patternInstanceLengths[patternIdx] != 0;
        }

        public int GetPatternInstanceLength(int patternIdx)
        {
            int len = patternInstanceLengths[patternIdx];
            return len == 0 ? patternLength : len;
        }

        public int GetPatternInstanceStartNote(int patternIdx, int note = 0)
        {
            return patternInstancesStartNote[patternIdx] + note;
        }

        public Channel GetChannelByType(int type)
        {
            return channels[Channel.ChannelTypeToIndex(type)];
        }

        public Song DeepClone()
        {
            var saveSerializer = new ProjectSaveBuffer(project);
            SerializeState(saveSerializer);
            var newSong = new Song();
            var loadSerializer = new ProjectLoadBuffer(project, saveSerializer.GetBuffer(), Project.Version);
            newSong.SerializeState(loadSerializer);
            return newSong;
        }

        public void Trim()
        {
            int maxLength = 0;
            foreach (var channel in channels)
            {
                for (int i = 0; i < Length; i++)
                {
                    if (channel.PatternInstances[i].Pattern != null)
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
                    var pattern = channels[Channel.Dpcm].PatternInstances[p].Pattern;
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
            foreach (var channel in channels)
                channel.Validate(this);
        }
#endif

        private void ConvertJumpSkipEffects()
        {
            for (int i = 0; i < songLength; i++)
            {
                foreach (var channel in channels)
                {
                    var pattern = channel.PatternInstances[i].Pattern;

                    if (pattern != null)
                    {
                        for (int j = 0; j < patternLength; j++)
                        {
                            // Converts old Jump effects to loop points.
                            // The first Jump effect will give us our loop point.
                            if (loopPoint == 0 && pattern.Notes[j].Jump != Note.JumpInvalid)
                            {
                                SetLoopPoint(pattern.Notes[j].Jump);
                            }

                            // Converts old Skip effects to custom pattern instances lengths.
                            if (pattern.Notes[j].Skip != Note.SkipInvalid)
                            {
                                SetPatternInstanceLength(i, j + 1);
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
                if (idx < patternInstancesStartNote[i + 1])
                {
                    noteIdx = idx - patternInstancesStartNote[i];
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

        public void UpdatePatternInstancesStartNotes()
        {
            patternInstancesStartNote[0] = 0;
            for (int i = 1; i <= songLength; i++)
            {
                int len = patternInstanceLengths[i - 1];
                Debug.Assert(len == 0 || len != patternLength);
                patternInstancesStartNote[i] = patternInstancesStartNote[i - 1] + (len == 0 ? patternLength : len);
            }
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

            buffer.Serialize(ref id);
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
                buffer.Serialize(ref patternInstanceLengths);
            }

            if (buffer.IsReading)
            {
                CreateChannels();
                UpdatePatternInstancesStartNotes();
            }

            foreach (var channel in channels)
                channel.SerializeState(buffer);

            if (buffer.Version < 5)
                ConvertJumpSkipEffects();
        }
    }
}
