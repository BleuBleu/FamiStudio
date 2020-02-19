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
        private int maxInstanceLength;

        const byte MaskTimeValid = 0x80;
        const byte MaskReleased  = 0x40;

        unsafe private struct LastValidNoteInfo
        {
            public byte mask;
            public byte time;
            public fixed byte effectValues[Note.EffectCount];
        }

        private short firstValidNoteTime = -1;
        private LastValidNoteInfo[] lastValidNotes = new LastValidNoteInfo[MaxLength];

        public int Id => id;
        public int ChannelType => channelType;
        public Color Color { get => color; set => color = value; }
        public Song Song => song;
        public int MaxInstanceLength => maxInstanceLength;

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

        public bool GetMinMaxNote(out Note min, out Note max)
        {
            bool valid = false;

            min = new Note() { Value = 255 };
            max = new Note() { Value =   0 };

            if (song != null)
            {
                for (int i = 0; i < maxInstanceLength; i++)
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
                for (int i = 0; i < maxInstanceLength; i++)
                {
                    var n = notes[i];
                    if (n.IsValid)
                    {
                        return true;
                    }
                    else
                    {
                        for (int j = 0; j < Note.EffectCount; j++)
                        {
                            if (n.HasValidEffectValue(i))
                                return true;
                        }
                    }
                }

                return false;
            }
        }

        public unsafe void UpdateValidNotes()
        {
            firstValidNoteTime = -1;
            lastValidNotes[0].mask = 0;
            lastValidNotes[0].time = 0;
            for (int j = 0; j < Note.EffectCount; j++)
                lastValidNotes[0].effectValues[j] = 0;

            for (int i = 0; i < maxInstanceLength; i++)
            {
                var note = notes[i];

                if (i > 0)
                    lastValidNotes[i] = lastValidNotes[i - 1];

                if (firstValidNoteTime < 0 && note.IsValid && !note.IsRelease)
                {
                    firstValidNoteTime = (byte)i;
                }

                if (note.IsRelease)
                {
                    lastValidNotes[i].mask |= MaskReleased;
                }
                else
                {
                    if (note.IsStop)
                    {
                        lastValidNotes[i].mask &= unchecked((byte)(~(MaskReleased | MaskTimeValid)));
                    }
                    if (note.IsValid)
                    {
                        lastValidNotes[i].mask |= MaskTimeValid;
                        lastValidNotes[i].time = (byte)i;
                    }
                }

                if (note.IsMusical && note.HasAttack)
                    lastValidNotes[i].mask &= unchecked((byte)(~MaskReleased));

                for (int j = 0; j < Note.EffectCount; j++)
                {
                    byte mask = (byte)(1 << j);
                    if (note.HasValidEffectValue(j))
                    {
                        lastValidNotes[i].mask |= mask;
                        lastValidNotes[i].effectValues[j] = (byte)note.GetEffectValue(j);
                    }
                }
            }

            for (int i = maxInstanceLength; i < MaxLength; i++)
            {
                lastValidNotes[i] = lastValidNotes[maxInstanceLength - 1];
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

        public int GetLastValidNoteTimeAt(int time)
        {
            return (lastValidNotes[time].mask & MaskTimeValid) != 0 ? lastValidNotes[time].time : -1;
        }

        public Note GetLastValidNoteAt(int time)
        {
            Debug.Assert((lastValidNotes[time].mask & MaskTimeValid) != 0);
            return notes[lastValidNotes[time].time];
        }

        public bool GetLastValidNoteReleasedAt(int time)
        {
            return (lastValidNotes[time].mask & MaskReleased) != 0;
        }

        public bool HasLastEffectValueAt(int time, int effect)
        {
            return (lastValidNotes[time].mask & (1 << effect)) != 0;
        }

        public unsafe byte GetLastEffectValueAt(int time, int effect)
        {
            Debug.Assert(HasLastEffectValueAt(time, effect));
            return lastValidNotes[time].effectValues[effect];
        }

        public void ClearNotesPastMaxInstanceLength()
        {
            for (int i = maxInstanceLength; i < notes.Length; i++)
                notes[i].Clear();
        }

        public void UpdateMaxInstanceLength()
        {
            var channel = song.GetChannelByType(channelType);

            maxInstanceLength = 0;
            for (int i = 0; i < song.Length; i++)
            {
                if (channel.PatternInstances[i] == this)
                    maxInstanceLength = Math.Max(maxInstanceLength, song.GetPatternInstanceLength(i));
            }
        }

        public Pattern ShallowClone()
        {
            var channel = song.GetChannelByType(channelType);
            var pattern = channel.CreatePattern();
            Array.Copy(notes, pattern.notes, notes.Length);
            UpdateMaxInstanceLength();
            UpdateValidNotes();
            return pattern;
        }

        public uint ComputeCRC(uint crc = 0)
        {
            crc = CRC32.Compute(BitConverter.GetBytes(maxInstanceLength), crc);

            for (int i = 0; i < maxInstanceLength; i++)
                crc = notes[i].ComputeCRC(crc);

            return crc;
        }

        public bool IdenticalTo(Pattern other)
        {
            for (int i = 0; i < maxInstanceLength; i++)
            {
                if (!notes[i].IdenticalTo(other.notes[i]))
                    return false;
            }

            return true;
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

            var oldMaxInstanceLength = maxInstanceLength;
            UpdateMaxInstanceLength();
            Debug.Assert(oldMaxInstanceLength == maxInstanceLength);
        }
#endif

        public unsafe void SerializeState(ProjectBuffer buffer)
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
                for (int i = 0; i < MaxLength; i++)
                {
                    if (!notes[i].IsStop && notes[i].IsValid)
                        notes[i].Value += 12;
                }
            }

            if (buffer.IsForUndoRedo)
            {
                buffer.Serialize(ref firstValidNoteTime);

                for (int i = 0; i < MaxLength; i++)
                {
                    buffer.Serialize(ref lastValidNotes[i].mask);
                    buffer.Serialize(ref lastValidNotes[i].time);
                    for (int j = 0; j < Note.EffectCount; j++)
                        buffer.Serialize(ref lastValidNotes[i].effectValues[j]);
                }
            }
            else if (buffer.IsReading)
            {
                UpdateValidNotes();
                ClearNotesPastMaxInstanceLength();
            }
        }
    }
}
