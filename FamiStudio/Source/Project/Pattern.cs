using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace FamiStudio
{
    public unsafe class Pattern
    {
        public const int MaxLength = 2048;

        private class LastValidNoteData
        {
            public bool released = false;
            public int time = -1;
            public int effectMask = 0;
            public int[] effectValues = new int[Note.EffectCount];
        }

        private int id;
        private string name;
        private Song song;
        private int channelType;
        private Color color;
        private SortedList<int, Note> notes = new SortedList<int, Note>();
        private int firstValidNoteTime = int.MinValue;
        private SortedList<int, LastValidNoteData> lastValidNoteCache = new SortedList<int, LastValidNoteData>();

        public int Id => id;
        public int ChannelType => channelType;
        public Color Color { get => color; set => color = value; }
        public Song Song => song;
        public SortedList<int, Note> Notes => notes;

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
        }
        
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public bool GetMinMaxNote(out int min, out int max)
        {
            bool valid = false;

            min = 255;
            max = 0;

            if (song != null)
            {
                foreach (var n in notes.Values)
                {
                    if (n.IsMusical)
                    {
                        if (n.Value < min) min = n.Value;
                        if (n.Value > max) max = n.Value;
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
                foreach (var n in notes.Values)
                {
                    if (n.IsValid)
                    {
                        return true;
                    }
                    else
                    {
                        for (int j = 0; j < Note.EffectCount; j++)
                        {
                            if (n.HasValidEffectValue(j))
                                return true;
                        }
                    }
                }

                return false;
            }
        }

        public unsafe void ClearLastValidNoteCache()
        {
            lastValidNoteCache.Clear();
            firstValidNoteTime = int.MinValue;
        }

        private int GetCachedFirstValidNoteTime()
        {
            if (firstValidNoteTime == int.MinValue)
            {
                firstValidNoteTime = -1;

                foreach (var kv in notes)
                {
                    var note = kv.Value;
                    if (note != null && note.IsValid && !note.IsRelease)
                    {
                        firstValidNoteTime = kv.Key;
                        break;
                    }
                }
            }

            return firstValidNoteTime;
        }

        private LastValidNoteData GetCachedLastValidNoteData(int endTime)
        {
            if (lastValidNoteCache.TryGetValue(endTime, out var lastValidData))
                return lastValidData;

            lastValidData = new LastValidNoteData();

            foreach (var kv in notes)
            {
                var i    = kv.Key;
                var note = kv.Value;

                if (i >= endTime)
                    break;

                if (note.IsRelease)
                {
                    lastValidData.released = true;
                }
                else
                {
                    if (note.IsStop)
                    {
                        lastValidData.released = false;
                        lastValidData.time = -1;
                    }
                    if (note.IsValid)
                    {
                        lastValidData.time = i;
                    }
                }

                if (note.IsMusical && note.HasAttack)
                {
                    lastValidData.released = false;
                }

                for (int j = 0; j < Note.EffectCount; j++)
                {
                    if (note.HasValidEffectValue(j))
                    {
                        lastValidData.effectMask |= (1 << j);
                        lastValidData.effectValues[j] = note.GetEffectValue(j);
                    }
                }
            }

            lastValidNoteCache[endTime] = lastValidData;
            return lastValidData;
        }

        public int FirstValidNoteTime
        {
            get
            {
                return GetCachedFirstValidNoteTime();
            }
        }

        public Note FirstValidNote
        {
            get
            {
                Debug.Assert(GetCachedFirstValidNoteTime() >= 0);
                return notes[GetCachedFirstValidNoteTime()];
            }
        }

        public int GetLastValidNoteTimeAt(int time)
        {
            var lastData = GetCachedLastValidNoteData(time);
            return lastData.time >= 0? lastData.time : -1;
        }

        public Note GetLastValidNoteAt(int time)
        {
            var lastData = GetCachedLastValidNoteData(time);
            return notes[lastData.time];
        }

        public bool GetLastValidNoteReleasedAt(int time)
        {
            var lastData = GetCachedLastValidNoteData(time);
            return lastData.released;
        }

        public bool HasLastEffectValueAt(int time, int effect)
        {
            var lastData = GetCachedLastValidNoteData(time);
            return (lastData.effectMask & (1 << effect)) != 0;
        }

        public int GetLastEffectValueAt(int time, int effect)
        {
            Debug.Assert(HasLastEffectValueAt(time, effect));
            var lastData = GetCachedLastValidNoteData(time);
            return lastData.effectValues[effect];
        }

        public void ClearNotesPastMaxInstanceLength()
        {
            var maxInstanceLength = GetMaxInstanceLength();

            if (maxInstanceLength == -1)
                return;

            var keys = notes.Keys;
            var vals = notes.Values;

            for (int i = keys.Count - 1; i >= 0; i--)
            {
                if (keys[i] >= maxInstanceLength)
                    notes.Remove(keys[i]);
                else
                    break;
            }
        }

        public void SetNoteAt(int time, Note note)
        {
            if (note == null || 
                note.IsEmpty)
            {
                notes.Remove(time);
            }
            else
            {
                notes[time] = note;
            }
        }

        public Note GetOrCreateNoteAt(int time)
        {
            if (Notes.TryGetValue(time, out var note))
                return note;

            note = new Note();
            notes[time] = note;
            return note;
        }

        public int GetMaxInstanceLength()
        {
            var channel = song.GetChannelByType(channelType);

            var maxInstanceLength = -1;
            for (int i = 0; i < song.Length; i++)
            {
                if (channel.PatternInstances[i] == this)
                    maxInstanceLength = Math.Max(maxInstanceLength, song.GetPatternLength(i));
            }

            return maxInstanceLength;
        }

        public Pattern ShallowClone()
        {
            var channel = song.GetChannelByType(channelType);
            var pattern = channel.CreatePattern();

            foreach (var kv in notes)
                pattern.Notes[kv.Key] = kv.Value.Clone();

            ClearLastValidNoteCache();
            return pattern;
        }

        public uint ComputeCRC(uint crc = 0)
        {
            var serializer = new ProjectCrcBuffer(crc);
            SerializeState(serializer);
            return serializer.CRC;
        }

        public void DeleteNotesBetween(int idx0, int idx1, bool preserveFx = false)
        {
            var keys = notes.Keys;
            var vals = notes.Values;

            for (int i = keys.Count - 1; i >= 0; i--)
            {
                if (keys[i] >= idx0 && keys[i] < idx1)
                {
                    if (preserveFx)
                    {
                        var note = vals[i];
                        note.Clear(true);
                        if (note.IsEmpty)
                            notes.Remove(keys[i]);
                    }
                    else
                    {
                        notes.Remove(keys[i]);
                    }
                }
            }
        }

        public void RemoveEmptyNotes(bool trim = true)
        {
            var keys = notes.Keys;
            var vals = notes.Values;

            for (int i = vals.Count - 1; i >= 0; i--)
            {
                var note = vals[i];
                if (note == null || note.IsEmpty && !note.HasJumpOrSkip)
                    notes.Remove(keys[i]);
            }

            if (trim)
                notes.TrimExcess();
        }

        public int BinarySearchList(IList<int> list, int value, bool roundUp = false)
        {
            if (list.Count == 0)
                return -1;

            int lo = 0;
            int hi = list.Count - 1;

            while (lo < hi)
            {
                int m = (hi + lo) / 2;

                if (list[m] < value)
                    lo = m + 1;
                else
                    hi = m - 1;
            }

            if (roundUp)
            {
                if (list[lo] < value) lo++;
            }
            else
            {
                if (list[lo] > value) lo--;
            }

            return Utils.Clamp(lo, 0, list.Count - 1);
        }

        public NoteIterator GetNoteIterator(int startIdx, int endIdx, bool reverse = false)
        {
            return new NoteIterator(this, startIdx, endIdx, reverse);
        }

#if DEBUG
        public void Validate(Channel channel)
        {
            Debug.Assert(this.song == channel.Song);

            foreach (var kv in notes)
            {
                var time = kv.Key;
                var note = kv.Value;

                Debug.Assert(time >= 0);
                Debug.Assert(time < MaxLength);
                Debug.Assert(note != null);
                Debug.Assert(!note.IsEmpty);

                var inst = note.Instrument;
                Debug.Assert(inst == null || song.Project.InstrumentExists(inst));
                Debug.Assert(inst == null || song.Project.GetInstrument(inst.Id) == inst);
                Debug.Assert(inst == null || channel.SupportsInstrument(inst));
            }

            // MATTT: Validate last data here.
        }
#endif

        public unsafe void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref id, true);
            buffer.Serialize(ref name);
            buffer.Serialize(ref channelType);
            buffer.Serialize(ref color);
            buffer.Serialize(ref song);

            if (buffer.Version < 5)
            {
                // At version 5 (FamiStudio 1.5.0), we moved to a sparse data structure for notes.
                for (int i = 0; i < 256; i++)
                {
                    var note = new Note();
                    note.SerializeStatePreVer5(buffer);
                    SetNoteAt(i, note);
                }
            }
            else
            {
                if (buffer.IsReading)
                    notes = new SortedList<int, Note>();

                if (buffer.IsWriting)
                    RemoveEmptyNotes(false);
                else
                    notes = new SortedList<int, Note>();

                int notesCount = notes.Count;
                buffer.Serialize(ref notesCount);

                if (buffer.IsWriting)
                {
                    foreach (var kv in notes)
                    {
                        var time = (short)kv.Key;
                        buffer.Serialize(ref time);
                        kv.Value.SerializeState(buffer);
                    }
                }
                else
                {
                    for (int i = 0; i < notesCount; i++)
                    {
                        short time = 0;
                        buffer.Serialize(ref time);
                        var note = new Note();
                        note.SerializeState(buffer);
                        notes[time] = note;
                    }
                }
            }

            // At version 3 (FamiStudio 1.2.0), we extended the range of notes.
            if (buffer.Version < 3 && channelType != Channel.Noise)
            {
                foreach (var note in notes.Values)
                {
                    if (!note.IsStop && note.IsValid)
                        note.Value += 12;
                }
            }

            if (buffer.IsReading)
            {
                ClearLastValidNoteCache();
                ClearNotesPastMaxInstanceLength();
            }
        }
    }

    public class NoteIterator
    {
        private bool reverse;
        private int listIdx;
        private int noteIdx0;
        private int noteIdx1;
        private Pattern pattern;

        public NoteIterator(Pattern p, int i0, int i1, bool rev)
        {
            Debug.Assert(i0 <= i1);

            reverse = rev;
            pattern = p;

            // When going forward, goes from i0   to i1-1
            // When going reverse, goes from i1-1 to i0
            if (reverse)
            {
                reverse = true;
                noteIdx0 = i1 - 1;
                noteIdx1 = i0;
            }
            else
            {
                noteIdx0 = i0;
                noteIdx1 = i1;
            }

            Resync();
        }

        public int  CurrentTime => noteIdx0;
        public Note CurrentNote => listIdx >= 0 && pattern.Notes.Keys[listIdx] == noteIdx0? pattern.Notes.Values[listIdx] : null;
        public bool Done => reverse ? noteIdx0 < noteIdx1 : noteIdx0 >= noteIdx1;

        public void Resync()
        {
            listIdx = pattern.BinarySearchList(pattern.Notes.Keys, noteIdx0, !reverse);
        }

        public void Next()
        {
            if (!reverse)
            {
                noteIdx0++;
                if (listIdx >= 0 && noteIdx0 > pattern.Notes.Keys[listIdx] && listIdx < pattern.Notes.Values.Count - 1)
                    listIdx++;
            }
            else
            {
                noteIdx0--;
                if (listIdx >= 0 && noteIdx0 < pattern.Notes.Keys[listIdx] && listIdx > 0)
                    listIdx--;
            }
        }
    }
}
