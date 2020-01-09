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

        private int    firstValidNoteTime    = -1;
        private int    lastValidNoteTime     = -1;
        private byte[] lastEffectValues      = new byte[Note.EffectCount];
        private bool   lastValidNoteReleased = false;

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
            for (int i = 0; i < Note.EffectCount; i++)
                lastEffectValues[i] = 0xff;
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
                    if (n.IsValid || n.HasVolume || n.HasVolume)
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

        public void UpdateLastValidNote()
        {
            for (int i = 0; i < Note.EffectCount; i++)
                lastEffectValues[i] = 0xff;
            lastValidNoteTime = -1;
            lastValidNoteReleased = false;
            
            for (int n = song.PatternLength - 1; n >= 0; n--)
            {
                var note = notes[n];

                if (lastValidNoteTime < 0)
                {
                    if (note.IsRelease)
                    {
                        lastValidNoteReleased = true;
                    }
                    else
                    {
                        if (note.IsStop)
                        {
                            lastValidNoteReleased = false;
                        }
                        if (note.IsValid)
                        {
                            lastValidNoteTime = (byte)n;
                        }
                    }
                }

                if (note.IsMusical && note.HasAttack)
                {
                    lastValidNoteReleased = false;
                }

                for (int i = 0; i < Note.EffectCount; i++)
                {
                    if (note.HasValidEffectValue(i) && lastEffectValues[i] == 0xff)
                        lastEffectValues[i] = (byte)note.GetEffectValue(i);
                }
            }

            firstValidNoteTime = -1;

            for (int i = 0; i < song.PatternLength; i++)
            {
                var note = notes[i];

                if (note.IsValid && !note.IsRelease)
                {
                    firstValidNoteTime = (byte)i;
                    break;
                }
            }
        }

        public int FirstValidNoteTime
        {
            get { return firstValidNoteTime; }
        }

        public Note FirstValidNote
        {
            get
            {
                Debug.Assert(firstValidNoteTime >= 0);
                return notes[firstValidNoteTime];
            }
        }

        public int LastValidNoteTime
        {
            get { return lastValidNoteTime; }
        }

        public Note LastValidNote
        {
            get
            {
                Debug.Assert(lastValidNoteTime >= 0);
                return notes[lastValidNoteTime];
            }
        }

        public bool LastValidNoteReleased
        {
            get { return lastValidNoteReleased; }
        }

        public byte GetLastEffectValue(int effect)
        {
            return lastEffectValues[effect];
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

            if (buffer.IsForUndoRedo)
            {
                buffer.Serialize(ref firstValidNoteTime);
                buffer.Serialize(ref lastValidNoteTime);
                buffer.Serialize(ref lastValidNoteReleased);
                for (int i = 0; i < Note.EffectCount; i++)
                    buffer.Serialize(ref lastEffectValues[i]);
            }
            else if (buffer.IsReading)
            {
                UpdateLastValidNote();
            }
        }
    }
}
