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
        private int maxInstanceLength = MaxLength;

        const byte MaskTimeValid = 0x80;
        const byte MaskReleased  = 0x40;

        private short  firstValidNoteTime = -1;
        private byte[] lastValidNoteMasks = new byte[MaxLength];
        private byte[] lastValidNoteTimes = new byte[MaxLength];
        private byte[] lastValidNoteEffectValues = new byte[MaxLength * Note.EffectCount];

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

        public unsafe void UpdateLastValidNotes()
        {
            firstValidNoteTime = -1;
            lastValidNoteMasks[0] = 0;
            lastValidNoteTimes[0] = 0;
            for (int j = 0; j < Note.EffectCount; j++)
                lastValidNoteEffectValues[j] = 0;

            for (int i = 0; i < maxInstanceLength; i++)
            {
                var note = notes[i];

                if (i > 0)
                {
                    lastValidNoteMasks[i] = lastValidNoteMasks[i - 1];
                    lastValidNoteTimes[i] = lastValidNoteTimes[i - 1];
                    Array.Copy(lastValidNoteEffectValues, (i - 1) * Note.EffectCount, lastValidNoteEffectValues, i * Note.EffectCount, Note.EffectCount);
                }

                if (firstValidNoteTime < 0 && note.IsValid && !note.IsRelease)
                {
                    firstValidNoteTime = (short)i;
                }

                if (note.IsRelease)
                {
                    lastValidNoteMasks[i] |= MaskReleased;
                }
                else
                {
                    if (note.IsStop)
                    {
                        lastValidNoteMasks[i] &= unchecked((byte)(~(MaskReleased | MaskTimeValid)));
                    }
                    if (note.IsValid)
                    {
                        lastValidNoteMasks[i] |= MaskTimeValid;
                        lastValidNoteTimes[i] = (byte)i;
                    }
                }

                if (note.IsMusical && note.HasAttack)
                    lastValidNoteMasks[i] &= unchecked((byte)(~MaskReleased));

                for (int j = 0; j < Note.EffectCount; j++)
                {
                    byte mask = (byte)(1 << j);
                    if (note.HasValidEffectValue(j))
                    {
                        lastValidNoteMasks[i] |= mask;
                        lastValidNoteEffectValues[i * Note.EffectCount + j] = (byte)note.GetEffectValue(j);
                    }
                }
            }

            for (int i = maxInstanceLength; i < MaxLength; i++)
            {
                lastValidNoteMasks[i] = lastValidNoteMasks[maxInstanceLength - 1];
                lastValidNoteTimes[i] = lastValidNoteTimes[maxInstanceLength - 1];
                Array.Copy(lastValidNoteEffectValues, (maxInstanceLength - 1) * Note.EffectCount, lastValidNoteEffectValues, i * Note.EffectCount, Note.EffectCount);
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
            return (lastValidNoteMasks[time] & MaskTimeValid) != 0 ? lastValidNoteTimes[time] : -1;
        }

        public Note GetLastValidNoteAt(int time)
        {
            Debug.Assert((lastValidNoteMasks[time] & MaskTimeValid) != 0);
            return notes[lastValidNoteTimes[time]];
        }

        public bool GetLastValidNoteReleasedAt(int time)
        {
            return (lastValidNoteMasks[time] & MaskReleased) != 0;
        }

        public bool HasLastEffectValueAt(int time, int effect)
        {
            return (lastValidNoteMasks[time] & (1 << effect)) != 0;
        }

        public unsafe byte GetLastEffectValueAt(int time, int effect)
        {
            Debug.Assert(HasLastEffectValueAt(time, effect));
            return lastValidNoteEffectValues[time * Note.EffectCount + effect];
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
                    maxInstanceLength = Math.Max(maxInstanceLength, song.GetPatternLength(i));
            }

            ClearNotesPastMaxInstanceLength();
        }

        public Pattern ShallowClone()
        {
            var channel = song.GetChannelByType(channelType);
            var pattern = channel.CreatePattern();
            Array.Copy(notes, pattern.notes, notes.Length);
            UpdateMaxInstanceLength();
            UpdateLastValidNotes();
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

            var emptyNote = new Note(Note.NoteInvalid);
            for (int i = maxInstanceLength; i < MaxLength; i++)
                Debug.Assert(notes[i].IsEmpty);
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
                buffer.Serialize(ref lastValidNoteMasks);
                buffer.Serialize(ref lastValidNoteTimes);
                buffer.Serialize(ref lastValidNoteEffectValues);
            }
            else if (buffer.IsReading)
            {
                UpdateLastValidNotes();
                ClearNotesPastMaxInstanceLength();
            }
        }
    }
}
