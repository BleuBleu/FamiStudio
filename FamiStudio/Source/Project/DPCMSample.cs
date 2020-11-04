using System.Collections.Generic;
using System.Diagnostics;

namespace FamiStudio
{
    public class DPCMSample
    {
        private int id;
        private string name;
        private byte[] data;
        private bool reverseBits;

        public int Id => id;
        public string Name { get => name; set => name = value; }
        public byte[] Data { get => data; set => data = value; }
        public bool ReverseBits { get => reverseBits; set => reverseBits = value; }

        public DPCMSample()
        {
            // For serializtion.
        }

        public DPCMSample(int id, string name, byte[] data)
        {
            this.id = id;
            this.name = name;
            this.data = data;
        }

        public void ChangeId(int newId)
        {
            id = newId;
        }

        public void Validate(Project project, Dictionary<int, object> idMap)
        {
#if DEBUG
            if (idMap.TryGetValue(id, out var foundObj))
                Debug.Assert(foundObj == this);
            else
                idMap.Add(id, this);

            Debug.Assert(project.GetSample(id) == this);
#endif
        }

        public byte[] GetDataWithReverse()
        {
            if (reverseBits)
            {
                var copy = data.Clone() as byte[];
                Utils.ReverseBits(copy);
                return copy;
            }
            else
            {
                return data;
            }
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref id);
            buffer.Serialize(ref name);
            buffer.Serialize(ref data);
            if (buffer.Version >= 8)
                buffer.Serialize(ref reverseBits);
        }
    }

    public class DPCMSampleMapping
    {
        private DPCMSample sample;
        private bool loop = false;
        private int pitch = 15;

        public DPCMSample Sample { get => sample; set => sample = value; }
        public bool Loop { get => loop; set => loop = value; }
        public int Pitch { get => pitch; set => pitch = value; }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref sample);
            buffer.Serialize(ref loop);
            buffer.Serialize(ref pitch);
        }
    }
}
