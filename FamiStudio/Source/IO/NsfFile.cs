using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public static class NsfFile
    {
        // NSF memory layout
        //   0x8000: start of code
        //   0x8500: song table of content, 4 bytes per song, max 64 songs:
        //      - first page of the song (1 byte)
        //      - address of the start of the song in page starting at 0x9000 (2 byte)
        //      - flags (uses DPCM)
        //   0x9000: start of first song. (Max 0x3000 if uses samples, 0x7000 if not)
        //   0xc000: DPCM samples (16KB max, if the song uses them)

        const int NsfCodeSize        = 0x0700;
        const int NsfCodeStart       = 0x8000;
        const int NsfInitAddr        = 0x8500; // Hardcoded in ASM
        const int NsfPlayAddr        = 0x8600; // Hardcoded in ASM
        const int NsfSongTableAddr   = 0x8700;
        const int NsfSongTableSize   = 0x0900;
        const int NsfSongAddr        = 0x9000;
        const int NsfDpcmOffset      = 0xc000;
        const int NsfPageSize        = 0x1000;
        const int NsfMaxSongSizeDpcm = 0x3000;
        const int NsfMaxSongSize     = 0x7000;
        const int NsfDpcmSize        = 0x4000;

        const int NsfMaxSongs     = 0x0900 / 4; // 4 = sizeof(NsfSongTableEntry);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct NsfHeader
        {
            public fixed byte id[5];
            public byte version;
            public byte numSongs;
            public byte startingSong;
            public ushort loadAddr;
            public ushort initAddr;
            public ushort playAddr;
            public fixed byte song[32];
            public fixed byte artist[32];
            public fixed byte copyright[32];
            public ushort playSpeedNTSC;
            public fixed byte banks[8];
            public ushort playSpeedPAL;
            public byte palNtscFlags;
            public byte extensionFlags;
            public byte reserved;
            public fixed byte programSize[3];
        };

        public unsafe static bool Save(Project project, string filename)
        {
            try
            {
                using (var file = new FileStream(filename, FileMode.Create))
                {
                    // Header
                    var header = new NsfHeader();
                    header.id[0] = (byte)'N';
                    header.id[1] = (byte)'E';
                    header.id[2] = (byte)'S';
                    header.id[3] = (byte)'M';
                    header.id[4] = (byte)0x1a;
                    header.version = 1;
                    header.numSongs = (byte)project.Songs.Count;
                    header.startingSong = 1;
                    header.loadAddr = 0x8000;
                    header.initAddr = NsfInitAddr;
                    header.playAddr = NsfPlayAddr;
                    header.playSpeedNTSC = 16639;
                    header.playSpeedPAL = 19997;
                    header.banks[0] = 0;
                    header.banks[1] = 1;
                    header.banks[2] = 2;
                    header.banks[3] = 3;
                    header.banks[4] = 4;
                    header.banks[5] = 5;
                    header.banks[6] = 6;
                    header.banks[7] = 7;

                    var headerBytes = new byte[sizeof(NsfHeader)];
                    Marshal.Copy(new IntPtr(&header), headerBytes, 0, headerBytes.Length);
                    file.Write(headerBytes, 0, headerBytes.Length);

                    // Code/sound engine
                    var nsfBinStream = typeof(NsfFile).Assembly.GetManifestResourceStream("FamiStudio.Nsf.nsf.bin");
                    var nsfBinBuffer = new byte[NsfCodeSize];
                    nsfBinStream.Read(nsfBinBuffer, 0, nsfBinBuffer.Length);

                    Debug.Assert(nsfBinStream.Length == NsfCodeSize);

                    file.Write(nsfBinBuffer, 0, nsfBinBuffer.Length);

                    var projectUsesSamples = project.UsesSamples;
                    var songTable = new byte[NsfMaxSongs * 4];
                    var songBytes = new List<byte>();
                    byte[] dpcmBytes = null;

                    // Export each song individually, build TOC at the same time.
                    for (int i = 0; i < project.Songs.Count && i < NsfMaxSongs; i++)
                    {
                        var song = project.Songs[i];
                        int page = songBytes.Count / NsfPageSize + 1;
                        int addr = NsfSongAddr + (songBytes.Count & (NsfPageSize - 1));

                        var famiToneFile = new FamitoneMusicFile();
                        famiToneFile.GetBytes(project, new int[] { song.Id }, addr, NsfDpcmOffset, out var currentSongBytes, out dpcmBytes);

                        int numDpcmPages = dpcmBytes != null ? (dpcmBytes.Length + NsfPageSize - 1) / NsfPageSize : 0;

                        songTable[i * 4 + 0] = (byte)(page + numDpcmPages);
                        songTable[i * 4 + 1] = (byte)((addr >> 0) & 0xff);
                        songTable[i * 4 + 2] = (byte)((addr >> 8) & 0xff);
                        songTable[i * 4 + 3] = (byte)(song.UsesDpcm ? 1 : 0);

                        songBytes.AddRange(currentSongBytes);
                    }

                    // Song table
                    file.Write(songTable, 0, songTable.Length);

                    // DPCM will be on the first 4 pages (1,2,3,4)
                    if (project.UsesSamples && dpcmBytes != null)
                    {
                        if (songBytes.Count > NsfMaxSongSizeDpcm)
                        {
                            // TODO: Error message.
                            return false;
                        }

                        int numDpcmPages = (dpcmBytes.Length + NsfPageSize - 1) / NsfPageSize;

                        Array.Resize(ref dpcmBytes, numDpcmPages * NsfPageSize);

                        file.Write(dpcmBytes, 0, dpcmBytes.Length);
                    }
                    else
                    {
                        if (songBytes.Count > NsfMaxSongSize)
                        {
                            // TODO: Error message.
                            return false;
                        }
                    }

                    // Song
                    file.Write(songBytes.ToArray(), 0, songBytes.Count);

                    file.Flush();
                    file.Close();
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
