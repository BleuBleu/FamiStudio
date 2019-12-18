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

        public int Id => id;
        public Project Project => project;
        public Channel[] Channels => channels;
        public Color Color { get => color; set => color = value; }
        public string Name { get => name; set => name = value; }
        public int Tempo { get => tempo; set => tempo = value; }
        public int Speed { get => speed; set => speed = value; }
        public int Length { get => songLength; set => songLength = value; }
        public int PatternLength { get => patternLength; set => patternLength = value; }
        public int BarLength { get => barLength; set => barLength = value; }

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

        public bool Split(int factor)
        {
            if (factor == 1)
                return true;

            if ((patternLength % factor) == 0 && (songLength * factor) < MaxLength)
            {
                foreach (var channel in channels)
                {
                    channel.Split(factor);
                }

                patternLength /= factor;
                barLength /= factor;
                songLength *= factor;

                if (barLength <= 1)
                {
                    barLength = 2;
                }

                if ((patternLength % barLength) != 0)
                {
                    bool found = false;
                    for (barLength = patternLength / 2; barLength >= 2; barLength--)
                    {
                        if (patternLength % barLength == 0)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        barLength = patternLength;
                    }
                }

                return true;
            }
            return false;
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

        public Song Clone()
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
                    if (channel.PatternInstances[i] != null)
                    {
                        maxLength = Math.Max(maxLength, i);
                    }
                }
            }

            Length = maxLength;
        }

        public void RemoveEmptyPatterns()
        {
            foreach (var channel in channels)
            {
                channel.RemoveEmptyPatterns();   
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
                    var pattern = channels[Channel.DPCM].PatternInstances[p];
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

            buffer.InitializeArray<Channel>(ref channels, buffer.Project.GetActiveChannelCount());
            foreach (var channel in channels)
                channel.SerializeState(buffer);
        }
    }
}
