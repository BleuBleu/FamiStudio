using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FamiStudio
{
    public static class RomFile
    {
        // ROM memory layout
        //   0x8000: start of song data
        //   0xc000: Optional DPCM samples, position will change (0xc000, 0xd000 or 0xe000) depending on size of samples (4KB to 12KB).
        //   0xf000: Song table + sound engine + UI code + Vectors.

        const int RomMemoryStart     = 0x8000;
        const int RomPageSize        = 0x1000;
        const int RomCodeSize        = 0x1000; // 4KB of code. 
        const int RomTileSize        = 0x2000; // 8KB CHR data.
        const int RomDpcmOffset      = 0xc000;
        const int RomPrgBankSize     = 0x4000; // INES header counts in number of 16KB PRG ROM banks.
        const int RomMinSize         = 0x8000; // Minimum PRG size is 32KB.
        const int RomHeaderLength    = 16;     // INES header size.
        const int RomHeaderPrgOffset = 4;      // Offset of the PRG page count in INES header.
        const int MaxSongs           = 8;
        const int MaxDpcmPages       = 3;

        // 64 bytes header.
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct RomProjectInfo
        {
            public byte numSongs;
            public byte dpcmPageStart;
            public byte dpcmPageCount;
            public fixed byte reserved[5];
            public fixed byte name[28];
            public fixed byte author[28];
        }

        // 32 bytes entry per-song
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct RomSongEntry
        {
            public byte   page;
            public ushort address;
            public byte   flags;
            public fixed byte name[28];
        }

        static readonly Dictionary<char, byte> specialCharMap = new Dictionary<char, byte>
        {
            { '.', 62 },
            { ':', 63 },
            { '?', 64 },
            { '!', 65 },
            { '(', 66 },
            { ')', 67 },
            { '/', 71 },
            { ' ', 255 },
        };

        // Encodes a string in the format of our characters in the CHR data.
        private static byte[] EncodeAndCenterString(string s)
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

        public unsafe static bool Save(Project originalProject, string filename, int[] songIds, string name, string author)
        {
            try
            {
                if (songIds.Length == 0)
                    return false;

                if (songIds.Length > MaxSongs)
                    Array.Resize(ref songIds, MaxSongs);

                var project = originalProject.DeepClone();
                project.RemoveAllSongsBut(songIds);
                project.SetExpansionAudio(Project.ExpansionNone);

                var headerBytes = new byte[RomHeaderLength];
                var codeBytes   = new byte[RomCodeSize + RomTileSize];

                // Load ROM header (16 bytes) + code/tiles (12KB).
                var romBinStream = typeof(RomFile).Assembly.GetManifestResourceStream("FamiStudio.Rom.rom.nes");
                romBinStream.Read(headerBytes, 0, RomHeaderLength);
                romBinStream.Seek(-RomCodeSize - RomTileSize, SeekOrigin.End);
                romBinStream.Read(codeBytes, 0, RomCodeSize + RomTileSize);

                // Build project info + song table of content.
                var projectInfo = new RomProjectInfo();
                projectInfo.numSongs = (byte)songIds.Length;
                Marshal.Copy(EncodeAndCenterString(name), 0, new IntPtr(projectInfo.name), 28);
                Marshal.Copy(EncodeAndCenterString(author), 0, new IntPtr(projectInfo.author), 28);

                var songTable = new RomSongEntry[MaxSongs];
                for (int i = 0; i < project.Songs.Count; i++)
                {
                    fixed (RomSongEntry* songEntry = &songTable[i])
                    {
                        songEntry->page = 0;
                        songEntry->address = 0x8000;
                        Marshal.Copy(EncodeAndCenterString(project.Songs[i].Name), 0, new IntPtr(songEntry->name), 28);
                    }
                }

                // Gather DPCM + song data.
                var songDataBytes = new List<byte>();
                var dpcmBaseAddr  = RomDpcmOffset;

                // We will put samples right at the beginning.
                if (project.UsesSamples)
                {
                    // Since we keep the code/engine at f000 all the time, we are limited to 12KB of samples in ROM.
                    var totalSampleSize = project.GetTotalSampleSize();
                    var dpcmPageCount   = Math.Min(MaxDpcmPages, (totalSampleSize + (RomPageSize - 1)) / RomPageSize);

                    // Otherwise we will allocate at least a full page for the samples and use the following mapping:
                    //    0KB -  4KB samples: starts at 0xe000
                    //    4KB -  8KB samples: starts at 0xd000
                    //    8KB - 12KB samples: starts at 0xc000
                    dpcmBaseAddr += (MaxDpcmPages - dpcmPageCount) * RomPageSize;

                    var dpcmBytes = project.GetPackedSampleData();
                    if (dpcmBytes.Length > (MaxDpcmPages * RomPageSize))
                        Array.Resize(ref dpcmBytes, MaxDpcmPages * RomPageSize);

                    songDataBytes.AddRange(dpcmBytes);

                    projectInfo.dpcmPageCount = (byte)dpcmPageCount;
                    projectInfo.dpcmPageStart = (byte)0;
                }

                // Export each song individually, build TOC at the same time.
                for (int i = 0; i < project.Songs.Count; i++)
                {
                    var song = project.Songs[i];
                    int page = songDataBytes.Count / RomPageSize;
                    int addr = RomMemoryStart + (songDataBytes.Count & (RomPageSize - 1));
                    var songBytes = new FamitoneMusicFile(FamitoneMusicFile.FamiToneKernel.FamiTone2FS).GetBytes(project, new int[] { song.Id }, addr, dpcmBaseAddr);

                    songTable[i].page = (byte)(page);
                    songTable[i].address = (ushort)(addr);
                    songTable[i].flags = (byte)(song.UsesDpcm ? 1 : 0);

                    songDataBytes.AddRange(songBytes);
                }

                int numPrgBanks = RomMinSize / RomPrgBankSize;

                if (songDataBytes.Count > (RomMinSize - RomCodeSize))
                    numPrgBanks = (songDataBytes.Count + (RomPrgBankSize - 1)) / RomPrgBankSize;

                int padding = (numPrgBanks * RomPrgBankSize) - RomCodeSize - songDataBytes.Count;
                songDataBytes.AddRange(new byte[padding]);

                // Patch in code (project info and song table are at the beginning, 0xf000).
                Marshal.Copy(new IntPtr(&projectInfo), codeBytes, 0, sizeof(RomProjectInfo));

                for (int i = 0; i < MaxSongs; i++)
                {
                    fixed (RomSongEntry* songEntry = &songTable[i])
                        Marshal.Copy(new IntPtr(songEntry), codeBytes, sizeof(RomProjectInfo) + i * sizeof(RomSongEntry), sizeof(RomSongEntry));
                }

                // Patch header.
                headerBytes[RomHeaderPrgOffset] = (byte)numPrgBanks;

                // Build final ROM and save.
                var romBytes = new List<byte>();
                romBytes.AddRange(headerBytes);
                romBytes.AddRange(songDataBytes);
                romBytes.AddRange(codeBytes);

                File.WriteAllBytes(filename, romBytes.ToArray());
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
