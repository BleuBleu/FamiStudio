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
        void Serialize(ref sbyte b);
        void Serialize(ref short i);
        void Serialize(ref ushort i);
        void Serialize(ref int b, bool id = false);
        void Serialize(ref long b);
        void Serialize(ref uint b);
        void Serialize(ref ulong b);
        void Serialize(ref float b);
        void Serialize(ref Color b, bool forceThemeColor = true);
        void Serialize(ref string b);
        void Serialize(ref byte[] values);
        void Serialize(ref sbyte[] values);
        void Serialize(ref short[] values);
        void Serialize(ref int[] values);
        void Serialize(ref Song song);
        void Serialize(ref Instrument instrument);
        void Serialize(ref Arpeggio arpeggio);
        void Serialize(ref Pattern pattern, Channel channel);
        void Serialize(ref DPCMSample pattern);
        bool IsReading { get; }
        bool IsWriting { get; }
        bool IsForUndoRedo  { get; }
        bool IsForClipboard { get; }
        int Version { get; }
        Project Project { get; }
        void InitializeList<T>(ref List<T> list, int count) where T : new();
    };

    [Flags]
    public enum ProjectBufferFlags
    {
        None      = 0,
        UndoRedo  = 1,
        Clipboard = 2
    };

    public class ProjectSaveBuffer : ProjectBuffer
    {
        private Project project;
        private List<byte> buffer = new List<byte>();
        private int idx = 0;
        private ProjectBufferFlags flags = ProjectBufferFlags.None;

        public ProjectSaveBuffer(Project p, ProjectBufferFlags flags = ProjectBufferFlags.None)
        {
            this.project = p;
            this.flags = flags;
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

        public void Serialize(ref sbyte b)
        {
            buffer.Add((byte)b);
            idx += sizeof(sbyte);
        }

        public void Serialize(ref short i)
        {
            buffer.AddRange(BitConverter.GetBytes(i));
            idx += sizeof(short);
        }

        public void Serialize(ref ushort i)
        {
            buffer.AddRange(BitConverter.GetBytes(i));
            idx += sizeof(ushort);
        }

        public void Serialize(ref int i, bool id = false)
        {
            buffer.AddRange(BitConverter.GetBytes(i));
            idx += sizeof(int);
        }

        public void Serialize(ref long i)
        {
            buffer.AddRange(BitConverter.GetBytes(i));
            idx += sizeof(long);
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

        public void Serialize(ref float f)
        {
            buffer.AddRange(BitConverter.GetBytes(f));
            idx += sizeof(float);
        }

        public void Serialize(ref Color b, bool forceThemeColor = true)
        {
            int argb = b.ToArgb();
            Serialize(ref argb);
        }

        public void Serialize(ref string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                buffer.AddRange(BitConverter.GetBytes(-1));
                idx += sizeof(int);
            }
            else
            {
                var bytes = Encoding.Unicode.GetBytes(str);
                buffer.AddRange(BitConverter.GetBytes(bytes.Length));
                idx += sizeof(int);
                buffer.AddRange(bytes);
                idx += str.Length;
            }
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

        public void Serialize(ref short[] values)
        {
            if (values == null)
            {
                buffer.AddRange(BitConverter.GetBytes(-1));
                idx += sizeof(int);
            }
            else
            {
                buffer.AddRange(BitConverter.GetBytes(values.Length));
                idx += sizeof(int);
                for (int i = 0; i < values.Length; i++)
                    buffer.AddRange(BitConverter.GetBytes(values[i]));
                idx += values.Length * sizeof(short);
            }
        }

        public void Serialize(ref int[] values)
        {
            if (values == null)
            {
                buffer.AddRange(BitConverter.GetBytes(-1));
                idx += sizeof(int);
            }
            else
            {
                buffer.AddRange(BitConverter.GetBytes(values.Length));
                idx += sizeof(int);
                for (int i = 0; i < values.Length; i++)
                    buffer.AddRange(BitConverter.GetBytes(values[i]));
                idx += values.Length * sizeof(int);
            }
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

        public void Serialize(ref Arpeggio arpeggio)
        {
            int arpeggioId = arpeggio == null ? -1 : arpeggio.Id;
            Serialize(ref arpeggioId);
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
        public bool IsForUndoRedo  => flags.HasFlag(ProjectBufferFlags.UndoRedo);
        public bool IsForClipboard => flags.HasFlag(ProjectBufferFlags.Clipboard);
        public int Version => Project.Version;
    };

    public class ProjectLoadBuffer : ProjectBuffer
    {
        private Project project;
        private byte[] buffer;
        private int idx = 0;
        private int version;
        private ProjectBufferFlags flags = ProjectBufferFlags.None;
        private Dictionary<int, int> idRemapTable = new Dictionary<int, int>();

        public ProjectLoadBuffer(Project p, byte[] buffer, int version, ProjectBufferFlags flags = ProjectBufferFlags.None)
        {
            this.project = p;
            this.buffer = buffer;
            this.version = version;
            this.flags = flags;
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

        public void Serialize(ref sbyte b)
        {
            b = (sbyte)buffer[idx];
            idx += sizeof(sbyte);
        }

        public void Serialize(ref short i)
        {
            i = BitConverter.ToInt16(buffer, idx);
            idx += sizeof(short);
        }

        public void Serialize(ref ushort i)
        {
            i = BitConverter.ToUInt16(buffer, idx);
            idx += sizeof(ushort);
        }

        public void Serialize(ref int i, bool id = false)
        {
            i = BitConverter.ToInt32(buffer, idx);
            if (id)
                i = GetRemappedId(i);
            idx += sizeof(int);
        }

        public void Serialize(ref long i)
        {
            i = BitConverter.ToInt64(buffer, idx);
            idx += sizeof(long);
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

        public void Serialize(ref float f)
        {
            f = BitConverter.ToSingle(buffer, idx);
            idx += sizeof(float);
        }

        public void Serialize(ref Color c, bool forceThemeColor = true)
        {
            int argb = 0;
            Serialize(ref argb);
            c = Color.FromArgb(argb);

            if (forceThemeColor && !IsForUndoRedo)
                c = Theme.EnforceThemeColor(c);
        }

        public void Serialize(ref string str)
        {
            int len = BitConverter.ToInt32(buffer, idx);
            idx += sizeof(int);

            if (len < 0)
            {
                str = "";
            }
            else
            {
                // At version 14 (FamiStudio 4.0.0) we switch to Unicode strings.
                if (version < 14)
                    str = Encoding.ASCII.GetString(buffer, idx, len);
                else
                    str = Encoding.Unicode.GetString(buffer, idx, len);

                // Some old project have weird \0 in the strings. Not sure where that came from.
                if (str.Contains('\0'))
                {
                    str = str.Replace("\0", ""); 
                }

                idx += len;
            }
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

        public void Serialize(ref short[] dest)
        {
            int len = BitConverter.ToInt32(buffer, idx);
            idx += sizeof(int);
            if (len < 0)
            {
                dest = null;
            }
            else
            {
                dest = new short[len];
                for (int i = 0; i < dest.Length; i++)
                {
                    dest[i] = BitConverter.ToInt16(buffer, idx);
                    idx += sizeof(short);
                }
            }
        }

        public void Serialize(ref int[] dest)
        {
            int len = BitConverter.ToInt32(buffer, idx);
            idx += sizeof(int);
            if (len < 0)
            {
                dest = null;
            }
            else
            {
                dest = new int[len];
                for (int i = 0; i < dest.Length; i++)
                {
                    dest[i] = BitConverter.ToInt16(buffer, idx);
                    idx += sizeof(int);
                }
            }
        }

        public void Serialize(ref Song song)
        {
            int songId = -1;
            Serialize(ref songId, true);
            song = project.GetSong(songId);
        }

        public void Serialize(ref Instrument instrument)
        {
            int instrumentId = -1;
            Serialize(ref instrumentId, true);
            instrument = project.GetInstrument(instrumentId);
        }

        public void Serialize(ref Arpeggio arpeggio)
        {
            int arpeggioId = -1;
            Serialize(ref arpeggioId, true);
            arpeggio = project.GetArpeggio(arpeggioId);
        }

        public void Serialize(ref Pattern pattern, Channel channel)
        {
            int patternId = -1;
            Serialize(ref patternId, true);
            pattern = channel.GetPattern(patternId);
        }

        public void Serialize(ref DPCMSample sample)
        {
            int sampleId = -1;
            Serialize(ref sampleId, true);
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
        public bool IsForUndoRedo  => flags.HasFlag(ProjectBufferFlags.UndoRedo);
        public bool IsForClipboard => flags.HasFlag(ProjectBufferFlags.Clipboard);
        public int Version => version;
    }
    
    public class ProjectCrcBuffer : ProjectBuffer
    {
        private uint crc = 0;

        public ProjectCrcBuffer(uint crc = 0)
        {
            this.crc = crc;
        }

        public uint CRC => crc;

        public void Serialize(ref bool b)
        {
            crc = CRC32.Compute((byte)(b ? 1 : 0), crc);
        }

        public void Serialize(ref byte b)
        {
            crc = CRC32.Compute(b, crc);
        }

        public void Serialize(ref sbyte b)
        {
            crc = CRC32.Compute((byte)b, crc);
        }

        public void Serialize(ref short i)
        {
            crc = CRC32.Compute(BitConverter.GetBytes(i), crc);
        }

        public void Serialize(ref ushort i)
        {
            crc = CRC32.Compute(BitConverter.GetBytes(i), crc);
        }

        public void Serialize(ref int i, bool id = false)
        {
            // Ignore IDs for CRC
            if (!id)
                crc = CRC32.Compute(BitConverter.GetBytes(i), crc);
        }

        public void Serialize(ref long i)
        {
            crc = CRC32.Compute(BitConverter.GetBytes(i), crc);
        }

        public void Serialize(ref uint i)
        {
            crc = CRC32.Compute(BitConverter.GetBytes(i), crc);
        }

        public void Serialize(ref ulong i)
        {
            crc = CRC32.Compute(BitConverter.GetBytes(i), crc);
        }

        public void Serialize(ref float f)
        {
            crc = CRC32.Compute(BitConverter.GetBytes(f), crc);
        }

        public void Serialize(ref Color b, bool forceThemeColor = true)
        {
            // Ignore colors for CRC
        }

        public void Serialize(ref string str)
        {
            // Ignore names for CRC
        }

        public void Serialize(ref byte[] values)
        {
            crc = CRC32.Compute(BitConverter.GetBytes(values.Length), crc);
            crc = CRC32.Compute(values, crc);
        }

        public void Serialize(ref sbyte[] values)
        {
            crc = CRC32.Compute(BitConverter.GetBytes(values.Length), crc);
            crc = CRC32.Compute(values, crc);
        }

        public void Serialize(ref short[] values)
        {
            if (values == null)
            {
                crc = CRC32.Compute(BitConverter.GetBytes(-1), crc);
            }
            else
            { 
                crc = CRC32.Compute(BitConverter.GetBytes(values.Length), crc);
                for (int i = 0; i < values.Length; i++)
                    crc = CRC32.Compute(BitConverter.GetBytes(values[i]), crc);
            }
        }

        public void Serialize(ref int[] values)
        {
            if (values == null)
            {
                crc = CRC32.Compute(BitConverter.GetBytes(-1), crc);
            }
            else
            {
                crc = CRC32.Compute(BitConverter.GetBytes(values.Length), crc);
                for (int i = 0; i < values.Length; i++)
                    crc = CRC32.Compute(BitConverter.GetBytes(values[i]), crc);
            }
        }

        public void Serialize(ref Song song)
        {
            int songId = song == null ? -1 : song.Id;
            Serialize(ref songId, true);
        }

        public void Serialize(ref Instrument instrument)
        {
            int instrumentId = instrument == null ? -1 : instrument.Id;
            Serialize(ref instrumentId, true);
        }

        public void Serialize(ref Arpeggio arpeggio)
        {
            int arpeggioId = arpeggio == null ? -1 : arpeggio.Id;
            Serialize(ref arpeggioId, true);
        }

        public void Serialize(ref Pattern pattern, Channel channel)
        {
            int patternId = pattern == null ? -1 : pattern.Id;
            Serialize(ref patternId, true);
        }

        public void Serialize(ref DPCMSample sample)
        {
            int sampleId = sample == null ? -1 : sample.Id;
            Serialize(ref sampleId, true);
        }

        public void InitializeList<T>(ref List<T> list, int count) where T : new()
        {
        }

        public Project Project => null;
        public bool IsReading => false;
        public bool IsWriting => true;
        public bool IsForUndoRedo => false;
        public bool IsForClipboard => false;
        public int Version => Project.Version;
    };
}
