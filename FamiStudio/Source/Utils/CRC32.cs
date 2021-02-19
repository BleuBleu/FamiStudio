using System;
using System.Text;

namespace FamiStudio
{
    static class CRC32
    {
        static uint[] table;

        static private void GenerateTable()
        {
            if (table == null)
            {
                table = new uint[256];

                uint polynomial = 0xEDB88320;
                for (uint i = 0; i < 256; i++)
                {
                    uint c = i;
                    for (uint j = 0; j < 8; j++)
                    {
                        if ((c & 1) != 0)
                        {
                            c = polynomial ^ (c >> 1);
                        }
                        else
                        {
                            c >>= 1;
                        }
                    }
                    table[i] = c;
                }
            }
        }

        static public uint Compute(byte val, uint initial = 0)
        {
            GenerateTable();

            uint c = initial ^ 0xFFFFFFFF;
            c = table[(c ^ val) & 0xFF] ^ (c >> 8);
            return c ^ 0xFFFFFFFF;
        }

        static public uint Compute(byte[] data, uint initial = 0)
        {
            GenerateTable();

            uint c = initial ^ 0xFFFFFFFF;
            for (uint i = 0; i < data.Length; ++i)
            {
                c = table[(c ^ data[i]) & 0xFF] ^ (c >> 8);
            }
            return c ^ 0xFFFFFFFF;
        }

        static public uint Compute(sbyte[] data, uint initial = 0)
        {
            GenerateTable();

            uint c = initial ^ 0xFFFFFFFF;
            for (uint i = 0; i < data.Length; ++i)
            {
                c = table[(c ^ data[i]) & 0xFF] ^ (c >> 8);
            }
            return c ^ 0xFFFFFFFF;
        }

        static public uint Compute(int val, uint initial = 0)
        {
            return Compute(BitConverter.GetBytes(val), initial);
        }

        static public uint Compute(string str, uint initial = 0)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(str);
            return Compute(bytes, initial);
        }
    }
}
