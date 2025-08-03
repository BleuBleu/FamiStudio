using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

namespace FamiStudio
{
    public class RomFile : RomFileBase
    {
        // ROM memory layout:
        //   0x8000 [swap]  : Start of song data (max 16KB per song)
        //   0xa000 [swap]  : Continued song data
        //   0xc000 [swap]  : DPCM samples (will bank switch as the song plays)
        //   0xe000 [fixed] : Sound engine code (allocates for the worst case, which is EPSM) 
        //   0xfe00 [fixed] : Song table (max 12 songs, to not step over vectors).

        const int RomSongDataStart    = 0x8000;
        const int RomBankSize         = 0x2000; // Most expansions have 8KB banks.
        const int RomBankSizeVrc6     = 0x4000; // VRC6 uses 16KB banks.
        const int RomPrgAndTocSize    = 0x2000; // 6.5KB of code + TOC + vectors. 
        const int RomTocOffset        = 0x1800; // Table of content is right after the code at F800
        const int RomTocOffsetEpsm    = 0x1b00; // Table of content is right after the code at FB00 (EPSM)
        const int RomChrSize          = 0x2000; // 8KB CHR data.
        const int MaxSongSize         = 0x4000; // 16KB max per song.
        const int MinRomSizeInKB      = 32;     // 32KB minimum.
        const int RomHeaderLength     = 16;     // INES header size.
        const int RomHeaderPrgOffset  = 4;      // Offset of the PRG bank count in INES header.
        const int RomDpcmStart        = 0xc000;
        
        public const int RomMaxSongs     = 48;
        public const int RomMaxSongsEpsm = 32;

        public unsafe bool Save(Project originalProject, string filename, int[] songIds, string name, string author, bool pal)
        {
#if !DEBUG
            try
#endif
            {
                if (songIds.Length == 0)
                    return false;

                if (originalProject.UsesMultipleExpansionAudios)
                {
                    Log.LogMessage(LogSeverity.Error, "ROM export does not support multiple audio expansions.");
                    return false;
                }

                Debug.Assert(!originalProject.UsesMultipleExpansionAudios);

                var maxSong = originalProject.UsesEPSMExpansion ? RomMaxSongsEpsm : RomMaxSongs;

                if (songIds.Length > maxSong)
                    Array.Resize(ref songIds, maxSong);

                var project = originalProject.DeepClone();
                project.DeleteAllSongsBut(songIds);
                project.SoundEngineUsesExtendedInstruments = true;
                project.SoundEngineUsesDpcmBankSwitching = true;
                project.SoundEngineUsesExtendedDpcm = true;

                if (project.UsesFdsExpansion)
                {
                    Log.LogMessage(LogSeverity.Warning, "Famicom Disk System projects should be exported as FDS disks. Only the 5 regular channels will be included in the ROM.");
                    project.SetExpansionAudioMask(0);
                }

                var headerBytes = new byte[RomHeaderLength];
                var prgBytes    = new byte[RomPrgAndTocSize];
                var chrBytes    = new byte[RomChrSize];

                // Load ROM header (16 bytes) + code/tiles (12KB).
                var romName = "FamiStudio.Rom.rom";
                var expSuffix = project.UsesAnyExpansionAudio ? $"_{ExpansionType.InternalNames[project.SingleExpansion].ToLower()}{(project.UsesN163Expansion ? $"_{project.ExpansionNumN163Channels}ch" : "")}" : "";

                romName += expSuffix;
                romName += pal ? "_pal" : "_ntsc";
                romName += project.UsesFamiTrackerTempo ? "_famitracker" : "";
                romName += ".nes";

                var romBinStream = typeof(RomFile).Assembly.GetManifestResourceStream(romName);
                romBinStream.Read(headerBytes, 0, RomHeaderLength);
                romBinStream.Seek(-RomPrgAndTocSize - RomChrSize, SeekOrigin.End);
                romBinStream.Read(prgBytes, 0, RomPrgAndTocSize);
                romBinStream.Read(chrBytes, 0, RomPrgAndTocSize);

                // Patch note tables if needed
                if (project.Tuning != 440) 
                {
                    var tblFile = Path.ChangeExtension(romName, ".tbl");
                    if (!FamitoneMusicFile.PatchNoteTable(prgBytes, tblFile, project.Tuning, pal ? MachineType.PAL : MachineType.NTSC , project.ExpansionNumN163Channels))
                    {
                        return false;
                    }
                }

                Log.LogMessage(LogSeverity.Info, $"ROM code and graphics size: {prgBytes.Length} bytes.");

                // Build project info + song table of content.
                var projectInfo = BuildProjectInfo(songIds, name, author);
                var songTable   = BuildSongTableOfContent(project, maxSong);

                // Gather song data.
                var songBanks = new List<List<byte>>();
                var songBankSize = project.UsesVrc6Expansion ? RomBankSizeVrc6 : RomBankSize;
                var songBankSizeInKB = songBankSize / 1024;
                var bankSize = RomBankSize;
                var bankSizeInKB = RomBankSize / 1024;
                var numDpcmBanks = 0;

                if (project.UsesSamples)
                {
                    numDpcmBanks = project.AutoAssignSamplesBanks(bankSize, out _);
                }

                // Export each song individually, build TOC at the same time.
                for (int i = 0; i < project.Songs.Count; i++)
                {
                    if (i == maxSong)
                    {
                        Log.LogMessage(LogSeverity.Warning, $"Too many songs. There is a hard limit of {maxSong} at the moment. Ignoring any extra songs.");
                        break;
                    }

                    var song = project.Songs[i];
                    var songBytes = new FamitoneMusicFile(FamiToneKernel.FamiStudio, false).GetBytes(project, new int[] { song.Id }, RomSongDataStart, bankSize, DpcmExportMode.All, false, RomDpcmStart, pal ? MachineType.PAL : MachineType.NTSC); 

                    if (songBytes.Length > MaxSongSize)
                    {
                        Log.LogMessage(LogSeverity.Warning, $"Song {song.Name} has a size of {songBytes.Length}, which is larger than the maximum allowed for ROM export ({MaxSongSize}). Truncating.");
                        Array.Resize(ref songBytes, MaxSongSize);
                    }

                    var numBanks = Utils.DivideAndRoundUp(songBytes.Length, songBankSize);
                    Debug.Assert(numBanks <= 2);

                    var songBank = songBanks.Count;
                    var songAddr = RomSongDataStart;

                    // If single bank, look for an existing bank with some free space at the end.
                    if (numBanks == 1)
                    {
                        var foundExistingBank = false;

                        for (int j = 0; j < songBanks.Count; j++)
                        {
                            var freeSpace = songBankSize - songBanks[j].Count;
                            if (songBytes.Length <= freeSpace)
                            {
                                songBank = j;
                                songAddr = RomSongDataStart + songBanks[j].Count;
                                songBytes = new FamitoneMusicFile(FamiToneKernel.FamiStudio, false).GetBytes(project, new int[] { song.Id }, songAddr, bankSize, DpcmExportMode.All, false, RomDpcmStart, pal ? MachineType.PAL : MachineType.NTSC); 
                                Debug.Assert(songBytes.Length <= freeSpace);
                                foundExistingBank = true;
                                break;
                            }
                        }

                        // No free space found, allocation a new partial bank.
                        if (!foundExistingBank)
                            songBanks.Add(new List<byte>());

                        songBanks[songBank].AddRange(songBytes);

#if false
                        // Enable to update the .bin files that help debug the ROM.
                        var songDataPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"..\\..\\..\\Rom\\song{expSuffix}.bin");
                        File.WriteAllBytes(songDataPath, songBytes);
#endif
                    }
                    else
                    {
                        // When a song uses 2 banks, allocate a new full one and a partial one.
                        var bank0 = new List<byte>();
                        var bank1 = new List<byte>();

                        for (int j = 0; j < songBankSize; j++)
                            bank0.Add(songBytes[j]);
                        for (int j = songBankSize; j < songBytes.Length; j++)
                            bank1.Add(songBytes[j]);

                        songBanks.Add(bank0);
                        songBanks.Add(bank1);
                    }

                    songTable[i].bank    = (byte)songBank;
                    songTable[i].address = (ushort)songAddr;
                    songTable[i].flags   = (byte)(song.UsesDpcm ? 1 : 0);

                    Log.LogMessage(LogSeverity.Info, $"Song '{song.Name}' size: {songBytes.Length} bytes.");
                }

                var numCodeBanks = 1;
                var numPaddingBanks = 0;
                var romSizeInKb  = (numCodeBanks + numDpcmBanks) * bankSizeInKB + (songBanks.Count * songBankSizeInKB);

                // Add extra empty banks if we haven't reached the minimum or if not a power-of-two.
                while (romSizeInKb < MinRomSizeInKB || Utils.NextPowerOfTwo(romSizeInKb) != romSizeInKb)
                {
                    numPaddingBanks++;
                    romSizeInKb += bankSizeInKB;
                }

                // Build final song bank data.
                var songBanksBytes = new byte[songBanks.Count * songBankSize];
                for (int i = 0; i < songBanks.Count; i++)
                    Array.Copy(songBanks[i].ToArray(), 0, songBanksBytes, i * songBankSize, songBanks[i].Count);

                // VRC6 uses 16KB pages, but we count in 8KB pages, so x2.
                projectInfo.firstDpcmBank = (byte)(songBanks.Count * (songBankSize / bankSize));

                var tocOffset = project.UsesEPSMExpansion ? RomTocOffsetEpsm : RomTocOffset;

                // Patch in code (project info and song table are after the code, 0xf000).
                Marshal.Copy(new IntPtr(&projectInfo), prgBytes, tocOffset, sizeof(RomProjectInfo));

                for (int i = 0; i < maxSong; i++)
                {
                    fixed (RomSongEntry* songEntry = &songTable[i])
                        Marshal.Copy(new IntPtr(songEntry), prgBytes, tocOffset + sizeof(RomProjectInfo) + i * sizeof(RomSongEntry), sizeof(RomSongEntry));
                }

                // Patch header (iNES header always counts in 16KB banks, MMC3 counts in 8KB banks)
                headerBytes[RomHeaderPrgOffset] = (byte)(romSizeInKb / 16);

                // Build final ROM and save.
                var romBytes = new List<byte>();
                romBytes.AddRange(headerBytes);
                romBytes.AddRange(songBanksBytes);

                // Samples are in a 8KB bank, located at c000 that we will switch at run-time.
                for (int i = 0; i < numDpcmBanks; i++)
                {
                    var dpcmBankBytes = project.GetPackedSampleData(i, bankSize);
                    Log.LogMessage(LogSeverity.Info, $"DPCM bank {i} size: {dpcmBankBytes.Length} bytes.");

                    Utils.PadToNextBank(ref dpcmBankBytes, bankSize);
                    romBytes.AddRange(dpcmBankBytes);
                }

                for (int i = 0; i < numPaddingBanks; i++)
                {
                    romBytes.AddRange(new byte[bankSize]);
                }

                romBytes.AddRange(prgBytes);
                romBytes.AddRange(chrBytes);

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
