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

        private byte lastVolumeValue = Note.VolumeInvalid;
        private byte lastValidNoteValue = Note.NoteInvalid;
        private Instrument lastValidNoteInstrument = null;
        private byte lastValidNoteIdx = 0;

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
            {
                notes[i].Value  = Note.NoteInvalid;
                notes[i].Volume = Note.VolumeInvalid;
            }
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

        public bool HasAnyEffect
        {
            get
            {
                for (int i = 0; i < song.PatternLength; i++)
                {
                    var n = notes[i];
                    if (n.Effect != Note.EffectNone)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void UpdateLastValidNotesAndVolume()
        {
            lastVolumeValue = Note.VolumeInvalid;
            lastValidNoteValue = Note.NoteInvalid;
            lastValidNoteIdx = 0;
            lastValidNoteInstrument = null;

            for (int i = song.PatternLength - 1; i >= 0; i--)
            {
                var note = notes[i];
                if (note.IsValid && lastValidNoteValue == Note.NoteInvalid)
                {
                    lastValidNoteIdx = (byte)i;
                    lastValidNoteValue = note.Value;
                    lastValidNoteInstrument = note.Instrument;
                    if (lastVolumeValue != Note.VolumeInvalid)
                        break;
                }
                if (note.HasVolume && lastVolumeValue == Note.VolumeInvalid)
                {
                    lastVolumeValue = note.Volume;
                    if (lastValidNoteValue != Note.NoteInvalid)
                        break;
                }
            }
        }

        public byte LastValidNoteTime
        {
            get { return lastValidNoteIdx; }
        }

        public byte LastValidNoteValue
        {
            get { return lastValidNoteValue; }
        }

        public Instrument LastValidNoteInstrument
        {
            get { return lastValidNoteInstrument; }
        }

        public byte LastVolumeValue
        {
            get { return lastVolumeValue; }
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

                // At version 3 (FamiStudio 1.2.0), we added a volume track.
                if (buffer.Version >= 3)
                    buffer.Serialize(ref notes[i].Volume);
                else
                    notes[i].Volume = Note.VolumeInvalid;

                buffer.Serialize(ref notes[i].Instrument);
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
                buffer.Serialize(ref lastVolumeValue);
                buffer.Serialize(ref lastValidNoteIdx);
                buffer.Serialize(ref lastValidNoteValue);
                buffer.Serialize(ref lastValidNoteInstrument);
            }
            else if (buffer.IsReading)
            {
                UpdateLastValidNotesAndVolume();
            }
        }
    }
}
