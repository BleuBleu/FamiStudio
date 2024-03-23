using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class RomFileBase
    {
        // 64 bytes header.
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        protected unsafe struct RomProjectInfo
        {
            public byte maxSong;
            public byte firstDpcmBank;
            public fixed byte reserved[6];
            public fixed byte name[28];
            public fixed byte author[28];
        }

        // 32 bytes entry per-song
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        protected unsafe struct RomSongEntry
        {
            public fixed byte name[28];
            public byte bank;
            public byte flags;
            public ushort address;
        }

        protected static readonly Dictionary<char, byte> specialCharMap = new Dictionary<char, byte>
        {
            { '.', 62 },
            { ':', 63 },
            { '?', 64 },
            { '!', 65 },
            { '(', 66 },
            { ')', 67 },
            { '/', 71 },
            { '-', 72 },
            { '[', 73 },
            { ']', 74 },
            { ' ', 255 },
        };

        // Encodes a string in the format of our characters in the CHR data.
        protected byte[] EncodeAndCenterString(string s)
        {
            const int MaxLen = 28;

            var len = Math.Min(s.Length, MaxLen);
            var start = (MaxLen - len) / 2;
            var end = start + len;
            var encoded = new byte[MaxLen];

            for (int i = 0; i < MaxLen; i++)
            {
                if (i >= start && i < end)
                {
                    char c = s[i - start];

                    if (c >= 'A' && c <= 'Z')
                        encoded[i] = (byte)(c - 'A');
                    else if (c >= 'a' && c <= 'z')
                        encoded[i] = (byte)(26 + (c - 'a'));
                    else if (c >= '0' && c <= '9')
                        encoded[i] = (byte)(52 + (c - '0'));
                    else
                    {
                        if (specialCharMap.TryGetValue(c, out var val))
                            encoded[i] = val;
                        else
                            encoded[i] = specialCharMap[' '];
                    }
                }
                else
                {
                    encoded[i] = specialCharMap[' '];
                }
            }

            return encoded;
        }

        protected unsafe RomProjectInfo BuildProjectInfo(int[] songIds, string name, string author)
        {
            var projectInfo = new RomProjectInfo();

            projectInfo.maxSong = (byte)(songIds.Length - 1);
            Marshal.Copy(EncodeAndCenterString(name),   0, new IntPtr(projectInfo.name), 28);
            Marshal.Copy(EncodeAndCenterString(author), 0, new IntPtr(projectInfo.author), 28);

            return projectInfo;
        }

        protected unsafe RomSongEntry[] BuildSongTableOfContent(Project project, int maxSongs, int songLoadAddr = 0x8000)
        {
            var songTable = new RomSongEntry[maxSongs];

            for (int i = 0; i < project.Songs.Count; i++)
            {
                fixed (RomSongEntry* songEntry = &songTable[i])
                {
                    songEntry->bank = 0;
                    songEntry->address = (ushort)songLoadAddr;
                    Marshal.Copy(EncodeAndCenterString(project.Songs[i].Name), 0, new IntPtr(songEntry->name), 28);
                }
            }

            return songTable;
        }
    }
}
