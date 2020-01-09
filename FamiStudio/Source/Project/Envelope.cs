using System;
using System.Diagnostics;

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
        bool relative = false;

        public sbyte[] Values => values;

        public int Length
        {
            get { return length; }
            set
            {
                length = Utils.Clamp(value, 0, MaxLength);
                if (loop >= length)
                    loop = -1;
                if (release >= length)
                    release = -1;
            }
        }

        public int Loop
        {
            get { return loop; }
            set
            {
                if (value >= 0)
                {
                    if (release >= 0)
                        loop = Utils.Clamp(value, 0, release - 1);
                    else
                        loop = Math.Min(value, MaxLength);

                    if (loop >= length)
                        loop = -1;
                }
                else
                {
                    loop = -1;
                    release = -1;
                }
            }
        }

        public int Release
        {
            get { return release; }
            set
            {
                if (value >= 0)
                {
                    if (loop >= 0)
                        release = Utils.Clamp(value, loop + 1, MaxLength);

                    if (release >= length)
                        release = -1;
                }
                else
                {
                    release = -1;
                }
            }
        }

        public bool Relative
        {
            get { return relative; }
            set { relative = value; }
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
            env.Length = length;
            env.Loop = loop;
            env.Release = release;
            env.Relative = relative;
            values.CopyTo(env.values, 0);
            return env;
        }

        // Based on FamiTracker, but simplified to use regular envelopes.
        static readonly int[] VibratoDepthLookup = new[] { 0x01, 0x03, 0x05, 0x07, 0x09, 0x0d, 0x13, 0x17, 0x1b, 0x21, 0x2b, 0x3b, 0x57, 0x7f, 0xbf, 0xff };

        public static Envelope CreateVibratoEnvelope(int speed, int depth)
        {
            Debug.Assert(speed >= 0 && speed < 16 && depth >= 0 && depth < 16);

            var env = new Envelope();

            if (speed == 0 || depth == 0)
            {
                env.Length = 0;
                env.Values[0] = 0;
            }
            else
            {
                env.Length = (int)Math.Round(64.0f / speed);
                env.Loop = 0;

                // Since we use a regular envelope, we can't go as deep as FamiTracker.
                depth = Math.Min(depth, 13); // VibratoDepthLookup[13] = 0x7f, which is the maximum value for a signed byte.

                for (int i = 0; i < env.Length; i++)
                    env.Values[i] = (sbyte)(Math.Sin(i * 2.0f * Math.PI / env.Length) * VibratoDepthLookup[depth]);
            }

            return env;
        }

        public uint CRC
        {
            get
            {
                uint crc = 0;
                crc = CRC32.Compute(BitConverter.GetBytes(length), crc);
                crc = CRC32.Compute(BitConverter.GetBytes(loop), crc);
                crc = CRC32.Compute(BitConverter.GetBytes(release), crc);
                crc = CRC32.Compute(BitConverter.GetBytes(relative), crc);
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

        public static void GetMinMaxValue(int type, out int min, out int max)
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
            if (buffer.Version >= 3)
                buffer.Serialize(ref release);
            if (buffer.Version >= 4)
                buffer.Serialize(ref relative);
            buffer.Serialize(ref values);
        }
    }
}
