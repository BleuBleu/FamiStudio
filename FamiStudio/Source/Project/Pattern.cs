using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class Pattern
    {
        public const int MaxLength = 2048;

        private int id;
        private string name;
        private Song song;
        private int channelType;
        private Color color;
        private SortedList<int, Note> notes = new SortedList<int, Note>();

        public int Id => id;
        public int ChannelType => channelType;
        public Color Color { get => color; set => color = value; }
        public Song Song => song;
        public Channel Channel => song.Channels[Channel.ChannelTypeToIndex(channelType, song.Project.ExpansionAudioMask, song.Project.ExpansionNumN163Channels)];
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
            this.color = Theme.RandomCustomColor();
        }
        
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public override string ToString()
        {
            return name;
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

        public bool IsEmpty
        {
            get
            {
                foreach (var note in notes.Values)
                {
                    if (!note.IsEmpty)
                        return false;
                }

                return true;
            }
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
                if (keys[i] < 0 || keys[i] >= maxInstanceLength)
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

        public bool TryGetNoteWithEffectAt(int time, int fx, out Note note)
        {
            return notes.TryGetValue(time, out note) && note != null && note.HasValidEffectValue(fx);
        }

        public Pattern ShallowClone(Channel newChannel = null)
        {
            var channel = newChannel == null ? song.GetChannelByType(channelType) : newChannel;
            var pattern = channel.CreatePattern();
            pattern.color = color;

            foreach (var kv in notes)
                pattern.Notes[kv.Key] = kv.Value.Clone();

            return pattern;
        }

        public bool RemoveUnsupportedChannelFeatures(bool checkOnly = false)
        {
            var channel = Channel;
            var foundAnyUnsupportedFeature = false;
            var notesToRemove = (HashSet<int>)null;

            foreach (var kv in notes)
            {
                var note = kv.Value;

                for (int i = 0; i < Note.EffectCount; i++)
                {
                    if (note != null && note.HasValidEffectValue(i) && !channel.SupportsEffect(i))
                    {
                        if (!checkOnly)
                            note.ClearEffectValue(i);
                        foundAnyUnsupportedFeature = true;
                    }
                }

                if (note.IsStop && !channel.SupportsStopNotes)
                {
                    if (!checkOnly)
                    {
                        if (notesToRemove == null)
                            notesToRemove = new HashSet<int>();
                        notesToRemove.Add(kv.Key);
                    }
                    foundAnyUnsupportedFeature = true;
                }
                if (note.IsRelease && !channel.SupportsReleaseNotes)
                {
                    if (!checkOnly)
                    {
                        if (notesToRemove == null)
                            notesToRemove = new HashSet<int>();
                        notesToRemove.Add(kv.Key);
                    }
                    foundAnyUnsupportedFeature = true;
                }
                if (note.HasRelease && !channel.SupportsReleaseNotes)
                {
                    if (!checkOnly)
                        note.Release = 0;
                    foundAnyUnsupportedFeature = true;
                }
                if (note.IsSlideNote && !channel.SupportsSlideNotes)
                {
                    if (!checkOnly)
                        note.SlideNoteTarget = 0;
                    foundAnyUnsupportedFeature = true;
                }
                if (note.IsArpeggio && !channel.SupportsArpeggios)
                {
                    if (!checkOnly)
                        note.Arpeggio = null;
                    foundAnyUnsupportedFeature = true;
                }
                if (note.Instrument != null && !channel.SupportsInstrument(note.Instrument))
                {
                    if (!checkOnly)
                        note.Instrument = null;
                    foundAnyUnsupportedFeature = true;
                }
                if (!note.HasAttack && !channel.SupportsNoAttackNotes)
                {
                    if (!checkOnly)
                        note.HasAttack = true;
                    foundAnyUnsupportedFeature = true;
                }
            }

            if (notesToRemove != null)
            {
                foreach (var n in notesToRemove)
                    notes.Remove(n);
            }

            return foundAnyUnsupportedFeature;
        }

        public uint ComputeCRC(uint crc = 0)
        {
            var serializer = new ProjectCrcBuffer(crc);
            Serialize(serializer);
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

            InvalidateCumulativeCache();
        }

        public void DeleteAllNotes()
        {
            notes.Clear();
            InvalidateCumulativeCache();
        }

        public void DeleteEmptyNotes()
        {
            var keys = notes.Keys;
            var vals = notes.Values;

            for (int i = vals.Count - 1; i >= 0; i--)
            {
                var note = vals[i];
                if (note == null || note.IsEmpty || note.IsUseless)
                {
                #if DEBUG
                    if (note != null && note.IsUseless)
                    {
                        Debug.WriteLine($"Removing useless note : {note}");
                    }
                #endif

                    notes.Remove(keys[i]);
                }
            }
        }

        public void FixBadData()
        {
            var vals = notes.Values;

            for (int i = vals.Count - 1; i >= 0; i--)
            {
                // Old version had a FamiTracker import bug that would assign arpeggios to
                // non-musical notes.
                var note = vals[i];
                if (note != null && note.Arpeggio != null && !note.IsMusical)
                    note.Arpeggio = null;
            }
        }

        public void RemoveDpcmNotesWithoutMapping()
        {
            Debug.Assert(Channel.IsDpcmChannel);

            var keys = notes.Keys;
            var vals = notes.Values;

            for (int i = vals.Count - 1; i >= 0; i--)
            {
                var note = vals[i];
                if (note != null && note.IsMusical && (note.Instrument == null || note.Instrument.GetDPCMMapping(note.Value) == null))
                { 
                    note.Clear(true);
                    if (note.IsEmpty)
                        notes.Remove(keys[i]);
                }
            }
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

            if (lo >= 0 && lo < list.Count)
                return lo;

            return -1;
        }

        public void InvalidateCumulativeCache()
        {
            Channel.InvalidateCumulativePatternCache(this);
        }

        public DensePatternNoteIterator GetDenseNoteIterator(int startIdx, int endIdx, bool reverse = false)
        {
            return new DensePatternNoteIterator(this, startIdx, endIdx, reverse);
        }

#if DEBUG
        public void ValidateIntegrity(Channel channel, Dictionary<int, object> idMap)
        {
            Debug.Assert(this.song == channel.Song);
            Debug.Assert(!string.IsNullOrEmpty(name.Trim()));

            song.Project.ValidateId(id);

            if (idMap.TryGetValue(id, out var foundObj))
                Debug.Assert(foundObj == this);
            else
                idMap.Add(id, this);

            foreach (var kv in notes)
            {
                var time = kv.Key;
                var note = kv.Value;

                Debug.Assert(time < GetMaxInstanceLength());
                Debug.Assert(time >= 0);
                Debug.Assert(time < MaxLength);
                Debug.Assert(note != null);
                Debug.Assert(!note.IsEmpty);

                // Not used since FamiStudio 3.0.0
                Debug.Assert(!note.IsRelease);
                Debug.Assert(!note.IsUseless);

                Debug.Assert(note.Release == 0 || note.Release > 0 && note.Release < note.Duration);
                Debug.Assert((note.IsMusical && note.Duration > 0) || (note.IsStop && note.Duration == 1) || (!note.IsMusicalOrStop && note.Duration == 0));
                Debug.Assert(!note.IsValid || note.IsRelease || note.Value <= Note.MusicalNoteMax);
                Debug.Assert(!note.IsStop || note.Instrument == null);
                Debug.Assert(note.Arpeggio == null || note.IsMusical);

                for (int i = 0; i < Note.EffectCount; i++)
                {
                    if (note != null && note.HasValidEffectValue(i))
                    {
                        if(!channel.SupportsEffect(i))
                            Debug.Assert(channel.SupportsEffect(i));

                        var val = note.GetEffectValue(i);
                        var min = Note.GetEffectMinValue(song, channel, i);
                        var max = Note.GetEffectMaxValue(song, channel, i);
                        Debug.Assert(val >= min && val <= max);
                    }
                }

                var inst = note.Instrument;
                Debug.Assert(inst == null || song.Project.InstrumentExists(inst));
                Debug.Assert(inst == null || song.Project.GetInstrument(inst.Id) == inst);
                Debug.Assert(inst == null || channel.SupportsInstrument(inst));

                var arp = note.Arpeggio;
                Debug.Assert(arp == null || song.Project.GetArpeggio(arp.Id) == arp);
            }

            Debug.Assert(!RemoveUnsupportedChannelFeatures(true));
        }
#endif

        public void ChangeId(int newId)
        {
            id = newId;
        }

        public void Serialize(ProjectBuffer buffer)
        {
            buffer.Serialize(ref id, true);
            buffer.Serialize(ref name);
            buffer.Serialize(ref channelType);
            buffer.Serialize(ref color);
            buffer.Serialize(ref song);

            if (buffer.Version < 5)
            {
                // At version 5 (FamiStudio 2.0.0), we moved to a sparse data structure for notes.
                for (int i = 0; i < 256; i++)
                {
                    var note = new Note();
                    note.SerializeStatePreVer5(buffer);

                    // Note using SetNoteAt to preserve jumps/skips
                    if (!note.IsEmpty || note.HasJumpOrSkip)
                        notes[i] = note;
                }
            }
            else
            {
                if (buffer.IsReading)
                    notes = new SortedList<int, Note>();

                if (buffer.IsWriting)
                    DeleteEmptyNotes();
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
                        kv.Value.Serialize(buffer);
                    }
                }
                else
                {
                    for (int i = 0; i < notesCount; i++)
                    {
                        short time = 0;
                        buffer.Serialize(ref time);
                        var note = new Note();
                        note.Serialize(buffer);
                        notes[time] = note;
                    }
                }
            }

            // At version 3 (FamiStudio 1.2.0), we extended the range of notes.
            if (buffer.Version < 3 && channelType != global::FamiStudio.ChannelType.Noise)
            {
                foreach (var note in notes.Values)
                {
                    if (!note.IsStop && note.IsValid)
                        note.Value += 12;
                }
            }

            if (buffer.IsReading)
            {
                if (!buffer.IsForClipboard)
                    InvalidateCumulativeCache();

                // This can happen when pasting from an expansion to another. We wont find the channel.
                if (buffer.Project.IsChannelActive(channelType))
                    ClearNotesPastMaxInstanceLength();

                if (!buffer.IsForUndoRedo)
                    FixBadData();
            }
        }
    }

    public class DensePatternNoteIterator
    {
        private bool reverse;
        private int listIdx;
        private int noteIdx0;
        private int noteIdx1;
        private Pattern pattern;

        public DensePatternNoteIterator(Pattern p, int i0, int i1, bool rev)
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
