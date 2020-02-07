using System;
using System.Diagnostics;
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
        public Song Song => song;

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
            this.color = ThemeBase.RandomCustomColor();
            for (int i = 0; i < notes.Length; i++)
                notes[i] = new Note(Note.NoteInvalid);
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
                    if (n.IsMusical)
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
                    if (n.IsValid || n.HasVolume || n.HasSpeed)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool HasAnyEffect
        {
            get
            {
                for (int n = 0; n < song.PatternLength; n++)
                {
                    var note = notes[n];
                    for (int i = 0; i < Note.EffectCount; i++)
                    { 
                        if (note.HasValidEffectValue(i))
                            return true;
                    }
                }

                return false;
            }
        }

        public void ClearNotesPastSongLength()
        {
            for (int i = song.PatternLength; i < notes.Length; i++)
                notes[i].Clear();
        }

#if DEBUG
        public void Validate(Channel channel)
        {
            Debug.Assert(this.song == channel.Song);

            for (int i = 0; i < MaxLength; i++)
            {
                var inst = notes[i].Instrument;
                Debug.Assert(inst == null || song.Project.InstrumentExists(inst));
                Debug.Assert(inst == null || song.Project.GetInstrument(inst.Id) == inst);
                Debug.Assert(inst == null || channel.SupportsInstrument(inst));
            }
        }
#endif

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref id, true);
            buffer.Serialize(ref name);
            buffer.Serialize(ref channelType);
            buffer.Serialize(ref color);
            buffer.Serialize(ref song);

            for (int i = 0; i < MaxLength; i++)
            {
                notes[i].SerializeState(buffer);
            }

            // At version 3 (FamiStudio 1.2.0), we extended the range of notes.
            if (buffer.Version < 3 && channelType != Channel.Noise)
            {
                for (int i = 0; i < 256; i++)
                {
                    if (!notes[i].IsStop && notes[i].IsValid)
                        notes[i].Value += 12;
                }
            }

            if (buffer.IsReading && !buffer.IsForUndoRedo)
            {
                ClearNotesPastSongLength();
            }
        }
    }
}
