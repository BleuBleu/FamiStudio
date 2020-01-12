using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FamiStudio
{
    public static class NsfFile
    {
        // NSF memory layout
        //   0x8000: start of code
        //   0x8000: nsf_init
        //   0x8060: nsf_play
        //   0x8080: FamiTone kernel code (variable size depending on expansion)
        //   0x8???: song table of content, 4 bytes per song:
        //      - first page of the song (1 byte)
        //      - address of the start of the song in page starting at 0x9000 (2 byte)
        //      - flags (1 = use DPCM)
        //   0x????: DPCM samples, if any.
        //   0x????: Song data.

        const int NsfMemoryStart     = 0x8000;
        const int NsfInitAddr        = 0x8000; // Hardcoded in asm config.
        const int NsfPlayAddr        = 0x8060; // Hardcoded in asm config.
        const int NsfKernelAddr      = 0x8080; // Hardcoded in asm config.
        const int NsfDpcmOffset      = 0xc000;
        const int NsfPageSize        = 0x1000;

        const int NsfGlobalVarsSize     = 2;
        const int NsfSongTableEntrySize = 4;

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

        public unsafe static bool Save(Project originalProject, FamitoneMusicFile.FamiToneKernel kernel, string filename, int[] songIds, string name, string author, string copyright)
        {
            try
            {
                if (songIds.Length == 0)
                    return false;

                var project = originalProject.Clone();
                project.RemoveAllSongsBut(songIds);

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
                header.extensionFlags = (byte)(project.ExpansionAudio == Project.ExpansionVRC6 ? 1 : 0);
                header.banks[0] = 0;
                header.banks[1] = 1;
                header.banks[2] = 2;
                header.banks[3] = 3;
                header.banks[4] = 4;
                header.banks[5] = 5;
                header.banks[6] = 6;
                header.banks[7] = 7;

                var nameBytes      = Encoding.ASCII.GetBytes(name);
                var artistBytes    = Encoding.ASCII.GetBytes(author);
                var copyrightBytes = Encoding.ASCII.GetBytes(copyright);

                Marshal.Copy(nameBytes,      0, new IntPtr(header.song),      Math.Min(31, nameBytes.Length));
                Marshal.Copy(artistBytes,    0, new IntPtr(header.artist),    Math.Min(31, artistBytes.Length));
                Marshal.Copy(copyrightBytes, 0, new IntPtr(header.copyright), Math.Min(31, copyrightBytes.Length));

                var headerBytes = new byte[sizeof(NsfHeader)];
                Marshal.Copy(new IntPtr(&header), headerBytes, 0, headerBytes.Length);

                List<byte> nsfBytes = new List<byte>();

                string kernelBinary;
                if (kernel == FamitoneMusicFile.FamiToneKernel.FamiTone2FS)
                {
                    kernelBinary = project.ExpansionAudio == Project.ExpansionVRC6 ? 
                        "nsf_ft2_fs_vrc6.bin" :
                        "nsf_ft2_fs.bin";
                }
                else
                {
                    kernelBinary = "nsf_ft2.bin";
                }

                // Code/sound engine
                var nsfBinStream = typeof(NsfFile).Assembly.GetManifestResourceStream("FamiStudio.Nsf." + kernelBinary);
                var nsfBinBuffer = new byte[nsfBinStream.Length];
                nsfBinStream.Read(nsfBinBuffer, 0, nsfBinBuffer.Length);

                nsfBytes.AddRange(nsfBinBuffer);

                var songTableIdx  = nsfBytes.Count;
                var songTableSize = NsfGlobalVarsSize + project.Songs.Count * NsfSongTableEntrySize;

                nsfBytes.AddRange(new byte[songTableSize]);

                var songDataIdx  = nsfBytes.Count;
                var dpcmBaseAddr = NsfDpcmOffset;
                var dpcmPadding  = 0;

                if (project.UsesSamples)
                {
                    var totalSampleSize = project.GetTotalSampleSize();

                    // Samples need to be 64-bytes aligned.
                    nsfBytes.AddRange(new byte[64 - (nsfBytes.Count & 0x3f)]);

                    // We start putting the samples right after the code, so the first page is not a
                    // full one. If we have near 16KB of samples, we might go over the 4 page limit.
                    // In this case, we will introduce padding until the next page.
                    if (nsfBytes.Count + totalSampleSize > Project.MaxSampleSize)
                    {
                        dpcmPadding = NsfPageSize - (nsfBytes.Count & (NsfPageSize - 1));
                        nsfBytes.AddRange(new byte[dpcmPadding]);
                    }

                    var dpcmPageStart = (nsfBytes.Count) / NsfPageSize;
                    var dpcmPageEnd   = (nsfBytes.Count + totalSampleSize) / NsfPageSize;
                    var dpcmPageCount = dpcmPageEnd - dpcmPageStart + 1;

                    // Otherwise we will allocate at least a full page for the samples and use the following mapping:
                    //    0KB -  4KB samples: starts at 0xf000
                    //    4KB -  8KB samples: starts at 0xe000
                    //    8KB - 12KB samples: starts at 0xd000
                    //   12KB - 16KB samples: starts at 0xc000
                    dpcmBaseAddr += (4 - dpcmPageCount) * NsfPageSize + (nsfBytes.Count & (NsfPageSize - 1));

                    nsfBytes.AddRange(project.GetPackedSampleData());

                    nsfBytes[songTableIdx + 0] = (byte)dpcmPageStart; // DPCM_PAGE_START
                    nsfBytes[songTableIdx + 1] = (byte)dpcmPageCount; // DPCM_PAGE_CNT
                }

                // Export each song individually, build TOC at the same time.
                for (int i = 0; i < project.Songs.Count; i++)
                {
                    var song = project.Songs[i];
                    var firstPage = nsfBytes.Count < NsfPageSize;
                    int page = nsfBytes.Count / NsfPageSize + (firstPage ? 1 : 0);
                    int addr = NsfMemoryStart + (firstPage ? 0 : NsfPageSize ) + (nsfBytes.Count & (NsfPageSize - 1));
                    var songBytes = new FamitoneMusicFile(kernel).GetBytes(project, new int[] { song.Id }, addr, dpcmBaseAddr);

                    // If we introduced padding for the samples, we can try to squeeze a song in there.
                    if (songBytes.Length < dpcmPadding)
                    {
                        // TODO. We should start writing at [songDataIdx] until we run out of dpcmPadding.
                    }

                    var idx = songTableIdx + NsfGlobalVarsSize + i * NsfSongTableEntrySize;
                    nsfBytes[idx + 0] = (byte)(page);
                    nsfBytes[idx + 1] = (byte)((addr >> 0) & 0xff);
                    nsfBytes[idx + 2] = (byte)((addr >> 8) & 0xff);
                    nsfBytes[idx + 3] = (byte)0;

                    nsfBytes.AddRange(songBytes);
                }

                // Finally insert the header, not very efficient, but easy.
                nsfBytes.InsertRange(0, headerBytes);

                File.WriteAllBytes(filename, nsfBytes.ToArray());
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
