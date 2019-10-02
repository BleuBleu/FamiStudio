using System;

namespace FamiStudio
{
    public class Envelope
    {
        public const int Volume = 0;
        public const int Arpeggio = 1;
        public const int Pitch = 2;
        public const int Max = 3;
        public static readonly string[] EnvelopeStrings = { "Volume", "Arpeggio", "Pitch" };

        public const int MaxLength = 256;

        sbyte[] values = new sbyte[MaxLength];
        int length;
        int loop = -1;
        int release = -1;

        public sbyte[] Values => values;

        public int Length
        {
            get { return length; }
            set
            {
                length = Utils.Clamp(value, 0, MaxLength);
                if (loop >= length)
                    loop = -1;
            }
        }

        public int Loop
        {
            get { return loop; }
            set
            {
                if (release >= 0)
                    loop = Utils.Clamp(value, 0, release - 1);
                else
                    loop = Math.Min(value, MaxLength);

                if (loop >= length)
                    loop = -1;
            }
        }

        public int Release
        {
            get { return release; }
            set
            {
                if (loop >= 0)
                    release = Utils.Clamp(value, loop + 1, MaxLength);

                if (release >= length)
                    release = -1;
            }
        }
        public void ConvertToAbsolute()
        {
            // Pitch are absolute in Famitone.
            sbyte val = 0;
            for (int j = 0; j < length; j++)
            {
                val += values[j];
                val = Math.Min((sbyte)63, val);
                val = Math.Max((sbyte)-64, val);
                values[j] = val;
            }
        }

        public Envelope Clone()
        {
            var env = new Envelope();
            env.Length = Length;
            env.Loop = Loop;
            values.CopyTo(env.values, 0);
            return env;
        }

        public uint CRC
        {
            get
            {
                uint crc = 0;
                crc = CRC32.Compute(BitConverter.GetBytes(length), crc);
                crc = CRC32.Compute(BitConverter.GetBytes(loop), crc);
                crc = CRC32.Compute(values, crc);
                return crc;
            }
        }

        public bool IsEmpty
        {
            get
            {
                if (length == 0)
                    return true;

                foreach (var val in values)
                {
                    if (val != 0)
                        return false;
                }

                return true;
            }
        }

        public static void GetMinValueValue(int type, out int min, out int max)
        {
            if (type == Volume)
            {
                min = 0;
                max = 15;
            }
            else
            {
                min = -64;
                max = 63;
            }
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref length);
            buffer.Serialize(ref loop);
            buffer.Serialize(ref release);
            buffer.Serialize(ref values);
        }
    }
}
