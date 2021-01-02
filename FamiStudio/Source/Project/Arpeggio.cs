using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class Arpeggio
    {
        private int id;
        private string name;
        private Envelope envelope = new Envelope(Envelope.Arpeggio);
        private Color color;

        public int Id => id;
        public Envelope Envelope => envelope;
        public string Name { get => name; set => name = value; }
        public Color Color { get => color; set => color = value; }

        public Arpeggio()
        {
        }

        public Arpeggio(int id, string name)
        {
            this.id = id;
            this.name = name;
            this.color = ThemeBase.RandomCustomColor();

            // Make a major chord by default.
            this.envelope.Values[0] = 0;
            this.envelope.Values[1] = 4;
            this.envelope.Values[2] = 7;
            this.envelope.Length = 3;
            this.envelope.Loop = 0;
        }

        public void Validate(Project project, Dictionary<int, object> idMap)
        {
#if DEBUG
            project.ValidateId(id);

            if (idMap.TryGetValue(id, out var foundObj))
                Debug.Assert(foundObj == this);
            else
                idMap.Add(id, this);

            Debug.Assert(project.GetArpeggio(id) == this);
#endif
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref id, true);
            buffer.Serialize(ref name);
            buffer.Serialize(ref color);
            envelope.SerializeState(buffer);
        }

        public void ChangeId(int newId)
        {
            id = newId;
        }

        public int[] GetChordOffsets()
        {
            var notes = new List<int>();

            for (int i = 0; i < envelope.Length; i++)
            {
                var val = envelope.Values[i];
                if (val != 0 && !notes.Contains(val))
                    notes.Add(val);
            }

            return notes.ToArray();
        }

        public bool GetChordMinMaxOffset(out int minOffset, out int maxOffset)
        {
            if (envelope.IsEmpty)
            {
                minOffset = 0;
                maxOffset = 0;
                return false;
            }

            minOffset = envelope.Values[0];
            maxOffset = envelope.Values[0];

            for (int i = 1; i < envelope.Length; i++)
            {
                var val = envelope.Values[i];
                minOffset = Math.Min(minOffset, val);
                maxOffset = Math.Max(maxOffset, val);
            }

            return true;
        }
    }
}
