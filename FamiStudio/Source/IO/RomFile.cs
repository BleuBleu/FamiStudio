using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FamiStudio
{
    public class RomFile : RomFileBase
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
        const int MaxDpcmPages       = 3;

        public unsafe bool Save(Project originalProject, string filename, int[] songIds, string name, string author, bool pal)
        {
            try
            {
                if (songIds.Length == 0)
                    return false;

                Debug.Assert(!originalProject.UsesExpansionAudio || !pal);

                if (songIds.Length > MaxSongs)
                    Array.Resize(ref songIds, MaxSongs);

                var project = originalProject.DeepClone();
                project.DeleteAllSongsBut(songIds);
                project.SetExpansionAudio(ExpansionType.None);

                var headerBytes = new byte[RomHeaderLength];
                var codeBytes   = new byte[RomCodeSize + RomTileSize];

                // Load ROM header (16 bytes) + code/tiles (12KB).
                string romName = "FamiStudio.Rom.rom";
                if (project.UsesFamiTrackerTempo)
                    romName += "_famitracker";
                romName += pal ? "_pal" : "_ntsc";
                romName += ".nes";

                var romBinStream = typeof(RomFile).Assembly.GetManifestResourceStream(romName);
                romBinStream.Read(headerBytes, 0, RomHeaderLength);
                romBinStream.Seek(-RomCodeSize - RomTileSize, SeekOrigin.End);
                romBinStream.Read(codeBytes, 0, RomCodeSize + RomTileSize);

                Log.LogMessage(LogSeverity.Info, $"ROM code and graphics size: {codeBytes.Length} bytes.");

                // Build project info + song table of content.
                var projectInfo = BuildProjectInfo(songIds, name, author);
                var songTable   = BuildSongTableOfContent(project);

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
                    {
                        Log.LogMessage(LogSeverity.Warning, $"DPCM samples size ({dpcmBytes.Length}) is larger than the maximum allowed for ROM export ({MaxDpcmPages * RomPageSize}). Truncating.");
                        Array.Resize(ref dpcmBytes, MaxDpcmPages * RomPageSize);
                    }

                    songDataBytes.AddRange(dpcmBytes);

                    projectInfo.dpcmPageCount = (byte)dpcmPageCount;
                    projectInfo.dpcmPageStart = (byte)0;

                    Log.LogMessage(LogSeverity.Info, $"DPCM allocated size: {dpcmPageCount * RomPageSize} bytes.");
                }

                // Export each song individually, build TOC at the same time.
                for (int i = 0; i < project.Songs.Count; i++)
                {
                    var song = project.Songs[i];
                    int page = songDataBytes.Count / RomPageSize;
                    int addr = RomMemoryStart + (songDataBytes.Count & (RomPageSize - 1));
                    var songBytes = new FamitoneMusicFile(FamiToneKernel.FamiStudio, false).GetBytes(project, new int[] { song.Id }, addr, dpcmBaseAddr, pal ? MachineType.PAL : MachineType.NTSC);

                    songTable[i].page = (byte)(page);
                    songTable[i].address = (ushort)(addr);
                    songTable[i].flags = (byte)(song.UsesDpcm ? 1 : 0);

                    songDataBytes.AddRange(songBytes);

                    Log.LogMessage(LogSeverity.Info, $"Song '{song.Name}' size: {songBytes.Length} bytes.");
                }

                //File.WriteAllBytes("D:\\debug.bin", songDataBytes.ToArray());

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

                Log.LogMessage(LogSeverity.Info, $"ROM export successful, final file size {romBytes.Count} bytes.");
            }
            catch (Exception e)
            {
                Log.LogMessage(LogSeverity.Error, "Please contact the developer on GitHub!");
                Log.LogMessage(LogSeverity.Error, e.Message);
                Log.LogMessage(LogSeverity.Error, e.StackTrace);
                return false;
            }

            return true;
        }
    }
}
