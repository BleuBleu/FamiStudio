using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace FamiStudio
{
    public interface ProjectBuffer
    {
        void Serialize(ref bool b);
        void Serialize(ref byte b);
        void Serialize(ref int b, bool id = false);
        void Serialize(ref uint b);
        void Serialize(ref ulong b);
        void Serialize(ref System.Drawing.Color b);
        void Serialize(ref string b);
        void Serialize(ref byte[] values);
        void Serialize(ref sbyte[] values);
        void Serialize(ref Song song);
        void Serialize(ref Instrument instrument);
        void Serialize(ref Pattern pattern, Channel channel);
        void Serialize(ref DPCMSample pattern);
        bool IsReading { get; }
        bool IsWriting { get; }
        bool IsForUndoRedo { get; }
        int Version { get; }
        Project Project { get; }
        void InitializeList<T>(ref List<T> list, int count) where T : new();
    };

    public class ProjectSaveBuffer : ProjectBuffer
    {
        private Project project;
        private List<byte> buffer = new List<byte>();
        private int idx = 0;
        private bool undoRedo;

        public ProjectSaveBuffer(Project p, bool forUndoRedo = false)
        {
            project = p;
            undoRedo = forUndoRedo;
        }

        public byte[] GetBuffer()
        {
            return buffer.ToArray();
        }

        public void Serialize(ref bool b)
        {
            buffer.Add((byte)(b ? 1 : 0));
            idx += sizeof(byte);
        }

        public void Serialize(ref byte b)
        {
            buffer.Add(b);
            idx += sizeof(byte);
        }

        public void Serialize(ref int i, bool id = false)
        {
            buffer.AddRange(BitConverter.GetBytes(i));
            idx += sizeof(int);
        }

        public void Serialize(ref uint i)
        {
            buffer.AddRange(BitConverter.GetBytes(i));
            idx += sizeof(uint);
        }

        public void Serialize(ref ulong i)
        {
            buffer.AddRange(BitConverter.GetBytes(i));
            idx += sizeof(ulong);
        }

        public void Serialize(ref System.Drawing.Color b)
        {
            int argb = b.ToArgb();
            Serialize(ref argb);
        }

        public void Serialize(ref string str)
        {
            buffer.AddRange(BitConverter.GetBytes(str.Length));
            idx += sizeof(int);
            buffer.AddRange(Encoding.ASCII.GetBytes(str));
            idx += str.Length;
        }

        public void Serialize(ref byte[] values)
        {
            buffer.AddRange(BitConverter.GetBytes(values.Length));
            idx += sizeof(int);
            buffer.AddRange(values);
            idx += values.Length;
        }

        public void Serialize(ref sbyte[] values)
        {
            buffer.AddRange(BitConverter.GetBytes(values.Length));
            idx += sizeof(int);
            for (int i = 0; i < values.Length; i++)
                buffer.Add((byte)values[i]);
            idx += values.Length;
        }

        public void Serialize(ref Song song)
        {
            int songId = song == null ? -1 : song.Id;
            Serialize(ref songId);
        }

        public void Serialize(ref Instrument instrument)
        {
            int instrumentId = instrument == null ? -1 : instrument.Id;
            Serialize(ref instrumentId);
        }

        public void Serialize(ref Pattern pattern, Channel channel)
        {
            int patternId = pattern == null ? -1 : pattern.Id;
            Serialize(ref patternId);
        }

        public void Serialize(ref DPCMSample sample)
        {
            int sampleId = sample == null ? -1 : sample.Id;
            Serialize(ref sampleId);
        }

        public void InitializeList<T>(ref List<T> list, int count) where T : new()
        {
        }

        public Project Project => project;
        public bool IsReading => false;
        public bool IsWriting => true;
        public bool IsForUndoRedo => undoRedo;
        public int Version => Project.Version;
    };

    public class ProjectLoadBuffer : ProjectBuffer
    {
        private Project project;
        private byte[] buffer;
        private int idx = 0;
        private int version;
        private bool undoRedo;
        private Dictionary<int, int> idRemapTable = new Dictionary<int, int>();

        public ProjectLoadBuffer(Project p, byte[] buffer, int version, bool forUndoRedo = false)
        {
            this.project = p;
            this.buffer = buffer;
            this.version = version;
            this.undoRedo = forUndoRedo;
        }

        public void RemapId(int oldId, int newId)
        {
            idRemapTable[oldId] = newId;
        }

        private int GetRemappedId(int id)
        {
            if (idRemapTable.TryGetValue(id, out int newId))
                return newId;
            return id;
        }

        public void Serialize(ref bool b)
        {
            b = buffer[idx] == 0 ? false : true;
            idx += sizeof(byte);
        }

        public void Serialize(ref byte b)
        {
            b = buffer[idx];
            idx += sizeof(byte);
        }

        public void Serialize(ref int i, bool id = false)
        {
            i = BitConverter.ToInt32(buffer, idx);
            if (id)
                i = GetRemappedId(i);
            idx += sizeof(int);
        }

        public void Serialize(ref uint i)
        {
            i = BitConverter.ToUInt32(buffer, idx);
            idx += sizeof(uint);
        }

        public void Serialize(ref ulong i)
        {
            i = BitConverter.ToUInt64(buffer, idx);
            idx += sizeof(ulong);
        }

        public void Serialize(ref System.Drawing.Color b)
        {
            int argb = 0;
            Serialize(ref argb);
            b = System.Drawing.Color.FromArgb(argb);
        }

        public void Serialize(ref string str)
        {
            int len = BitConverter.ToInt32(buffer, idx);
            idx += sizeof(int);
            str = Encoding.ASCII.GetString(buffer, idx, len);
            idx += len;
        }

        public void Serialize(ref byte[] values)
        {
            int len = BitConverter.ToInt32(buffer, idx);
            idx += sizeof(int);
            values = new byte[len];
            Array.Copy(buffer, idx, values, 0, values.Length);
            idx += values.Length;
        }

        public void Serialize(ref sbyte[] dest)
        {
            int len = BitConverter.ToInt32(buffer, idx);
            idx += sizeof(int);
            dest = new sbyte[len];
            for (int i = 0; i < dest.Length; i++)
            {
                dest[i] = (sbyte)buffer[idx++];
            }
        }

        public void Serialize(ref Song song)
        {
            int songId = -1;
            Serialize(ref songId);
            song = project.GetSong(songId);
        }

        public void Serialize(ref Instrument instrument)
        {
            int instrumentId = -1;
            Serialize(ref instrumentId, true);
            instrument = project.GetInstrument(instrumentId);
        }

        public void Serialize(ref Pattern pattern, Channel channel)
        {
            int patternId = -1;
            Serialize(ref patternId);
            pattern = channel.GetPattern(patternId);
        }

        public void Serialize(ref DPCMSample sample)
        {
            int sampleId = -1;
            Serialize(ref sampleId);
            sample = project.GetSample(sampleId);
        }

        public void InitializeList<T>(ref List<T> list, int count) where T : new()
        {
            list.Clear();
            for (int i = 0; i < count; i++)
                list.Add(new T());
        }

        public Project Project => project;
        public bool IsReading => true;
        public bool IsWriting => false;
        public bool IsForUndoRedo => undoRedo;
        public int Version => version;
    }
}
