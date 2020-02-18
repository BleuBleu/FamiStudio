using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class PatternInstance
    {
        private Song song;
        private int channelType;
        private int index;
        private Pattern pattern;
        private int firstValidNoteTime = -1;
        private int lastValidNoteTime = -1;
        private int lastEffectValuesMask = 0;
        private int[] lastEffectValues = new int[Note.EffectCount];
        private bool lastValidNoteReleased = false;

        public int Length => song.GetPatternInstanceLength(index);
        public int StartNote => song.GetPatternInstanceStartNote(index);

        public PatternInstance(Song s, int c, int idx)
        {
            song = s;
            channelType = c;
            index = idx;
            lastEffectValuesMask = 0;
            for (int i = 0; i < Note.EffectCount; i++)
                lastEffectValues[i] = 0xff;
        }

        public Pattern Pattern
        {
            get
            {
                return pattern;
            }
            set
            {
                pattern = value;
                song.GetChannelByType(channelType).UpdatePatternsMaxInstanceLength();
            }
        }

        public void UpdateLastValidNote()
        {
            lastEffectValuesMask = 0;
            for (int i = 0; i < Note.EffectCount; i++)
                lastEffectValues[i] = 0xff;
            lastValidNoteTime = -1;
            lastValidNoteReleased = false;

            if (Pattern == null)
                return;

            var song = Pattern.Song;

            for (int n = Length - 1; n >= 0; n--)
            {
                var note = Pattern.Notes[n];

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
                    var mask = 1 << i;
                    if (note.HasValidEffectValue(i) && (lastEffectValuesMask & mask) == 0)
                    {
                        lastEffectValuesMask |= mask;
                        lastEffectValues[i] = note.GetEffectValue(i);
                    }
                }
            }

            firstValidNoteTime = -1;

            for (int i = 0; i < Length; i++)
            {
                var note = Pattern.Notes[i];

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
                return Pattern.Notes[firstValidNoteTime];
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
                return Pattern.Notes[lastValidNoteTime];
            }
        }

        public bool LastValidNoteReleased
        {
            get { return lastValidNoteReleased; }
        }

        public bool HasLastEffectValue(int effect)
        {
            return (lastEffectValuesMask & (1 << effect)) != 0;
        }

        public int GetLastEffectValue(int effect)
        {
            return lastEffectValues[effect];
        }

#if DEBUG
        public void Validate(Channel channel)
        {
            int oldFirstValidNoteTime     = firstValidNoteTime;
            int oldLastValidNoteTime      = lastValidNoteTime;
            int oldLastEffectValuesMask   = lastEffectValuesMask;
            bool oldLastValidNoteReleased = lastValidNoteReleased;

            int[] oldLastEffectValues = new int[Note.EffectCount];
            Array.Copy(lastEffectValues, oldLastEffectValues, Note.EffectCount);

            UpdateLastValidNote();

            Debug.Assert(oldFirstValidNoteTime    == firstValidNoteTime);
            Debug.Assert(oldLastValidNoteTime     == lastValidNoteTime);
            Debug.Assert(oldLastEffectValuesMask  == lastEffectValuesMask);
            Debug.Assert(oldLastValidNoteReleased == lastValidNoteReleased);

            for (int i = 0; i < Note.EffectCount; i++)
                Debug.Assert(oldLastEffectValues[i] == lastEffectValues[i]);
        }
#endif

        public void SerializeState(ProjectBuffer buffer, Channel channel)
        {
            buffer.Serialize(ref pattern, channel);

            if (buffer.IsForUndoRedo)
            {
                buffer.Serialize(ref song);
                buffer.Serialize(ref channelType);
                buffer.Serialize(ref firstValidNoteTime);
                buffer.Serialize(ref lastValidNoteTime);
                buffer.Serialize(ref lastValidNoteReleased);
                buffer.Serialize(ref lastEffectValuesMask);
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
