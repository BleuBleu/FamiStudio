using System;
using System.Drawing;

namespace FamiStudio
{
    public class Pattern
    {
        public const int MaxLength = 256;

        private int id;
        private string name;
        private Song song;
        private int channelType;
        private Color color;
        private Note[] notes = new Note[MaxLength];

        public int Id => id;
        public int ChannelType => channelType;
        public Color Color { get => color; set => color = value; }

        public Pattern()
        {
            // For serialization.
        }

        public Pattern(int id, Song song, int channelType, string n)
        {
            this.id = id;
            this.song = song;
            this.channelType = channelType;
            this.name = n;
            this.color = Theme.RandomCustomColor();
            for (int i = 0; i < notes.Length; i++)
                notes[i].Value = 0xff;
        }

        public Note[] Notes
        {
            get { return notes; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public Pattern[] Split(int factor)
        {
            if ((song.PatternLength % factor) == 0)
            {
                var newPatternLength = song.PatternLength / factor;
                var newPatterns = new Pattern[factor];

                for (int i = 0; i < factor; i++)
                {
                    newPatterns[i] = new Pattern(song.Project.GenerateUniqueId(), song, channelType, song.Channels[channelType].GenerateUniquePatternName(name));
                    newPatterns[i].color = color;
                    Array.Copy(notes, newPatternLength * i, newPatterns[i].notes, 0, newPatternLength);
                }

                return newPatterns;
            }

            return new Pattern[] { this };
        }

        public bool GetMinMaxNote(out Note min, out Note max)
        {
            bool valid = false;

            min = new Note() { Value = 255 };
            max = new Note() { Value =   0 };

            if (song != null)
            {
                for (int i = 0; i < song.PatternLength; i++)
                {
                    var n = notes[i];
                    if (n.IsValid && !n.IsStop)
                    {
                        if (n.Value < min.Value) min = n;
                        if (n.Value > max.Value) max = n;
                        valid = true;
                    }
                }
            }

            return valid;
        }

        public bool HasAnyNotes
        {
            get
            {
                for (int i = 0; i < song.PatternLength; i++)
                {
                    var n = notes[i];
                    if (n.IsValid || n.IsStop)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref id);
            buffer.Serialize(ref name);
            buffer.Serialize(ref channelType);
            buffer.Serialize(ref color);
            buffer.Serialize(ref song);

            for (int i = 0; i < 256; i++)
            {
                buffer.Serialize(ref notes[i].Value);
                buffer.Serialize(ref notes[i].Effect);
                buffer.Serialize(ref notes[i].EffectParam);

                int instrumentId = notes[i].Instrument == null ? -1 : notes[i].Instrument.Id;
                buffer.Serialize(ref instrumentId);
                if (!buffer.IsWriting)
                    notes[i].Instrument = buffer.Project.GetInstrument(instrumentId);
            }
        }
    }
}
