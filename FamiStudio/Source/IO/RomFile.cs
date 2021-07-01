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
        // ROM memory layout (new MMC3 layout)
        //   0x8000 [swap]  : start of song data (max 16KB per song)
        //   0xa000 [swap]  : continued song data
        //   0xc000 [fixed] : DPCM samples start
        //   0xec00 [fixed] : Sound engine code (A bit over 4K)
        //   0xfe00 [fixed] : Song table (max 15 songs, to not step over vectors).

        const int RomSongDataStart    = 0x8000;
        const int RomPageSize         = 0x2000; // MMC3 has 8KB pages.
        const int RomCodeAndTocSize   = 0x1400; // 5KB of code + TOC + vectors. 
        const int RomTocOffset        = 0x1200; // Table of content is right after the code at FE00
        const int RomTileSize         = 0x2000; // 8KB CHR data.
        const int MaxSongSize         = 0x4000; // 16KB max per song.
        const int RomMinNumberBanks   = 2;
        const int RomCodeDpcmNumBanks = 2;
        const int RomHeaderLength     = 16;     // INES header size.
        const int RomHeaderPrgOffset  = 4;      // Offset of the PRG page count in INES header.
        const int RomDpcmStart        = 0xc000;
        const int MaxDpcmSize         = 0x2c00; // 11KB

        public unsafe bool Save(Project originalProject, string filename, int[] songIds, string name, string author, bool pal)
        {
#if !DEBUG
            try
#endif
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
                var codeBytes   = new byte[RomCodeAndTocSize + RomTileSize];

                // Load ROM header (16 bytes) + code/tiles (12KB).
                string romName = "FamiStudio.Rom.rom";
                if (project.UsesFamiTrackerTempo)
                    romName += "_famitracker";
                romName += pal ? "_pal" : "_ntsc";
                romName += ".nes";

                var romBinStream = typeof(RomFile).Assembly.GetManifestResourceStream(romName);
                romBinStream.Read(headerBytes, 0, RomHeaderLength);
                romBinStream.Seek(-RomCodeAndTocSize - RomTileSize, SeekOrigin.End);
                romBinStream.Read(codeBytes, 0, RomCodeAndTocSize + RomTileSize);

                Log.LogMessage(LogSeverity.Info, $"ROM code and graphics size: {codeBytes.Length} bytes.");

                // Build project info + song table of content.
                var projectInfo = BuildProjectInfo(songIds, name, author);
                var songTable   = BuildSongTableOfContent(project);

                // Gathersong data.
                var songDataBytes = new List<byte>();

                // Export each song individually, build TOC at the same time.
                for (int i = 0; i < project.Songs.Count; i++)
                {
                    if (i == MaxSongs)
                    {
                        Log.LogMessage(LogSeverity.Warning, $"Too many songs. There is a hard limit of {MaxSongs} at the moment. Ignoring any extra songs.");
                        break;
                    }

                    var song = project.Songs[i];
                    var songBytes = new FamitoneMusicFile(FamiToneKernel.FamiStudio, false).GetBytes(project, new int[] { song.Id }, RomSongDataStart, RomDpcmStart, pal ? MachineType.PAL : MachineType.NTSC);

                    if (songBytes.Length > MaxSongSize)
                    {
                        Log.LogMessage(LogSeverity.Warning, $"Song {song.Name} has a size of {songDataBytes.Count}, which is larger than the maximum allowed for ROM export ({MaxSongSize}). Truncating.");
                        Array.Resize(ref songBytes, MaxSongSize);
                    }

                    var numPages = Utils.DivideAndRoundUp(songBytes.Length, RomPageSize);
                    Debug.Assert(numPages <= 2);

                    // Use full pages, pad with zero.
                    Array.Resize(ref songBytes, numPages * RomPageSize);

                    songTable[i].page    = (byte)(songDataBytes.Count / RomPageSize);
                    songTable[i].address = (ushort)(RomSongDataStart);
                    songTable[i].flags   = (byte)(song.UsesDpcm ? 1 : 0);

                    songDataBytes.AddRange(songBytes);

                    Log.LogMessage(LogSeverity.Info, $"Song '{song.Name}' size: {songBytes.Length} bytes.");
                }

                //File.WriteAllBytes("D:\\debug.bin", songDataBytes.ToArray());

                // Add extra empty banks if we havent reached the minimum.
                int numPrgBanks = songDataBytes.Count / RomPageSize;

                if (numPrgBanks < RomMinNumberBanks)
                {
                    for (int i = numPrgBanks; i < RomMinNumberBanks; i++)
                    {
                        numPrgBanks++;
                        songDataBytes.AddRange(new byte[RomPageSize]);
                    }
                }
                else if ((numPrgBanks & 1) != 0)
                {
                    numPrgBanks++;
                    songDataBytes.AddRange(new byte[RomPageSize]);
                }

                // Patch in code (project info and song table are after the code, 0xf000).
                Marshal.Copy(new IntPtr(&projectInfo), codeBytes, RomTocOffset, sizeof(RomProjectInfo));

                for (int i = 0; i < MaxSongs; i++)
                {
                    fixed (RomSongEntry* songEntry = &songTable[i])
                        Marshal.Copy(new IntPtr(songEntry), codeBytes, RomTocOffset + sizeof(RomProjectInfo) + i * sizeof(RomSongEntry), sizeof(RomSongEntry));
                }

                // Patch header (iNES header always counts in 16KB pages, MMC3 counts in 8KB pages)
                headerBytes[RomHeaderPrgOffset] = (byte)((numPrgBanks + RomCodeDpcmNumBanks) * RomPageSize / 0x4000);

                // Build final ROM and save.
                var romBytes = new List<byte>();
                romBytes.AddRange(headerBytes);
                romBytes.AddRange(songDataBytes);

                // Samples are at the end, right before the source engine code. MMC3 second to last and last banks respectively.
                if (project.UsesSamples)
                {
                    // Since we keep the code/engine at f000 all the time, we are limited to 12KB of samples in ROM.
                    var dpcmBytes = project.GetPackedSampleData();

                    Log.LogMessage(LogSeverity.Info, $"DPCM size: {dpcmBytes.Length} bytes.");

                    if (dpcmBytes.Length > MaxDpcmSize)
                        Log.LogMessage(LogSeverity.Warning, $"DPCM samples size ({dpcmBytes.Length}) is larger than the maximum allowed for ROM export ({MaxDpcmSize}). Truncating.");

                    // Always allocate the full 11KB of samples.
                    Array.Resize(ref dpcmBytes, MaxDpcmSize);

                    romBytes.AddRange(dpcmBytes);
                }
                else
                {
                    romBytes.AddRange(new byte[MaxDpcmSize]);
                }

                romBytes.AddRange(codeBytes);

                File.WriteAllBytes(filename, romBytes.ToArray());

                Log.LogMessage(LogSeverity.Info, $"ROM export successful, final file size {romBytes.Count} bytes.");
            }
#if !DEBUG
            catch (Exception e)
            {
                Log.LogMessage(LogSeverity.Error, "Please contact the developer on GitHub!");
                Log.LogMessage(LogSeverity.Error, e.Message);
                Log.LogMessage(LogSeverity.Error, e.StackTrace);
                return false;
            }
#endif

            return true;
        }
    }
}
