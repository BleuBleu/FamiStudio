using System;
using System.Drawing;

namespace FamiStudio
{
    public class Song
    {
        public const int MaxLength = 256;

        private int id;
        private Project project;
        private Channel[] channels = new Channel[5];
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
            if (channels[0] == null)
            {
                CreateChannels();
            }
        }

        public Song(Project project, int id, string name)
        {
            this.project = project;
            this.id = id;
            this.name = name;
            this.color = Color.Azure;

            CreateChannels();
        }

        private void CreateChannels()
        {
            channels[0] = new Channel(this, Channel.Square1, songLength);
            channels[1] = new Channel(this, Channel.Square2, songLength);
            channels[2] = new Channel(this, Channel.Triangle, songLength);
            channels[3] = new Channel(this, Channel.Noise, songLength);
            channels[4] = new Channel(this, Channel.DPCM, songLength);
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

        public void SerializeState(ProjectBuffer buffer)
        {
            if (buffer.IsReading && !buffer.IsForUndoRedo)
                project = buffer.Project;

            buffer.Serialize(ref id);
            buffer.Serialize(ref patternLength);
            buffer.Serialize(ref songLength);
            buffer.Serialize(ref barLength);
            buffer.Serialize(ref name);
            buffer.Serialize(ref tempo);
            buffer.Serialize(ref speed);
            buffer.Serialize(ref color);

            foreach (var channel in channels)
                channel.SerializeState(buffer);
        }
    }
}
