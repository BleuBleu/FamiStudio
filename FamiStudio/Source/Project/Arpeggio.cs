using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class Arpeggio
    {
        private int id;
        private string name;
        private Envelope envelope = new Envelope(EnvelopeType.Arpeggio);
        private Color color;
        private string folderName;
        private Project project;

        public int Id => id;
        public Envelope Envelope => envelope;
        public string Name { get => name; set => name = value; }
        public Color Color { get => color; set => color = value; }
        public string FolderName { get => folderName; set => folderName = value; }
        public Folder Folder => string.IsNullOrEmpty(folderName) ? null : project.GetFolder(FolderType.Arpeggio, folderName);

        public Arpeggio()
        {
        }

        public Arpeggio(Project project, int id, string name)
        {
            this.project = project;
            this.id = id;
            this.name = name;
            this.color = Theme.RandomCustomColor();

            // Make a major chord by default.
            this.envelope.Values[0] = 0;
            this.envelope.Values[1] = 4;
            this.envelope.Values[2] = 7;
            this.envelope.Length = 3;
            this.envelope.Loop = 0;
        }

        public void SetProject(Project newProject)
        {
            project = newProject;
        }

        public void ValidateIntegrity(Project project, Dictionary<int, object> idMap)
        {
#if DEBUG
            project.ValidateId(id);

            Debug.Assert(project == this.project);

            if (idMap.TryGetValue(id, out var foundObj))
                Debug.Assert(foundObj == this);
            else
                idMap.Add(id, this);

            Debug.Assert(project.GetArpeggio(id) == this);
            Debug.Assert(!string.IsNullOrEmpty(name.Trim()));
            Debug.Assert(string.IsNullOrEmpty(folderName) || project.FolderExists(FolderType.Arpeggio, folderName));
#endif
        }

        public uint ComputeCRC(uint crc = 0)
        {
            var serializer = new ProjectCrcBuffer(crc);
            Serialize(serializer);
            return serializer.CRC;
        }

        public void Serialize(ProjectBuffer buffer)
        {
            if (buffer.IsReading)
                project = buffer.Project;

            buffer.Serialize(ref id, true);
            buffer.Serialize(ref name);
            buffer.Serialize(ref color);

            // At version 16 (FamiStudio 4.2.0) we added little folders in the project explorer.
            if (buffer.Version >= 16)
            {
                buffer.Serialize(ref folderName);
            }

            envelope.Serialize(buffer, EnvelopeType.Arpeggio);
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
            if (envelope.IsEmpty(EnvelopeType.Arpeggio))
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
