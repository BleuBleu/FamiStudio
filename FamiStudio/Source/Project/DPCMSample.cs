using System;

namespace FamiStudio
{
    public class DPCMSample
    {
        private int id;
        private string name;
        private byte[] data;

        public int Id => id;
        public string Name { get => name; set => name = value; }
        public byte[] Data { get => data; set => data = value; }

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

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref id);
            buffer.Serialize(ref name);
            buffer.Serialize(ref data);
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
