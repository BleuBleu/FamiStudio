using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FamiStudio
{
    class FdsFile : RomFileBase
    {
        // FDS memory layout, must match CFG file.
        //   0x6000: Sound engine + ROM code.
        //   0x7400: Song table of content
        //   0x7600: Song data for the current song
        //   0xc000: DPCM data

        const int FdsSongDataAddr    = 0x7600; // MATTT : Switch to 15 songs for FDS too.
        const int FdsMaxDpcmSize     = 0x2000 - 10; // 8KB - 10 bytes of vectors
        const int FdsDpcmStart       = 0xc000;
        const int FdsMaxFileSize     = 65500; // 1 side for now.
        const int FdsMaxSongSize     = FdsDpcmStart - FdsSongDataAddr;
        const int FdsFirstFileIndex  = 6;
        const int FdsBlockHeaderSize = 17;

        private int FindString(byte[] fdsData, string str)
        {
            var filenameAscii = Encoding.ASCII.GetBytes(str);

            for (int i = 0; i < fdsData.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < filenameAscii.Length; j++)
                {
                    if (filenameAscii[j] != fdsData[i + j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                    return i;
            }

            return -1;
        }

        private void TruncateToLastFile(ref byte[] fdsData)
        {
            //var i = FindString(fds, "BYPASS..");
            //Array.Resize(ref fds, i + 15);
            var i = FindString(fdsData, "KYODAKU-");
            Array.Resize(ref fdsData, i + 238);
        }

        private void PatchFile(byte[] fdsData, string filename, byte[] newFileData)
        {
            var i = FindString(fdsData, filename);

            for (int j = 0; j < newFileData.Length; j++)
                fdsData[i + j + 14] = newFileData[j];
        }

        private void AddFile(List<byte> fdsData, int fileIndex, int loadAddr, string filename, byte[] newFileData)
        {
            fdsData.Add(0x03);
            fdsData.Add((byte)fileIndex);
            fdsData.Add((byte)fileIndex);
            fdsData.AddRange(Encoding.ASCII.GetBytes(filename));
            fdsData.Add((byte)((loadAddr >> 0) & 0xff));
            fdsData.Add((byte)((loadAddr >> 8) & 0xff));
            fdsData.Add((byte)((newFileData.Length >> 0) & 0xff));
            fdsData.Add((byte)((newFileData.Length >> 8) & 0xff));
            fdsData.Add(0x00);
            fdsData.Add(0x04);
            fdsData.AddRange(newFileData);
        }

        public unsafe bool Save(Project originalProject, string filename, int[] songIds, string name, string author)
        {
            try
            {
                if (songIds.Length == 0)
                    return false;

                if (songIds.Length > MaxSongs)
                    Array.Resize(ref songIds, MaxSongs);

                var project = originalProject.DeepClone();
                project.DeleteAllSongsBut(songIds);

                if (project.ExpansionAudio != ExpansionType.Fds)
                    project.SetExpansionAudio(ExpansionType.Fds);

                string fdsDiskName = "FamiStudio.Rom.fds";
                if (project.UsesFamiTrackerTempo)
                    fdsDiskName += "_famitracker";
                fdsDiskName += ".fds";

                // Read FDS disk header + code.
                var fdsDiskBinStream = typeof(RomFile).Assembly.GetManifestResourceStream(fdsDiskName);
                var fdsDiskInitBytes = new byte[fdsDiskBinStream.Length];
                fdsDiskBinStream.Read(fdsDiskInitBytes, 0, fdsDiskInitBytes.Length);

                TruncateToLastFile(ref fdsDiskInitBytes);

                var fdsFileBytes = new List<byte>();
                fdsFileBytes.AddRange(fdsDiskInitBytes);

                Log.LogMessage(LogSeverity.Info, $"FDS code and graphics files: {fdsDiskInitBytes.Length} bytes.");

                var fileIndex     = FdsFirstFileIndex;
                var dpcmFileIndex = 0;

                // Create the DPCM file if needed.
                if (project.UsesSamples)
                {
                    var dpcmBytes = project.GetPackedSampleData();

                    if (dpcmBytes.Length > FdsMaxDpcmSize)
                    {
                        Log.LogMessage(LogSeverity.Warning, $"DPCM samples size ({dpcmBytes.Length}) is larger than the maximum allowed for FDS export ({FdsMaxDpcmSize}). Truncating.");
                        Array.Resize(ref dpcmBytes, FdsMaxDpcmSize);
                    }

                    AddFile(fdsFileBytes, fileIndex, FdsDpcmStart, "DPCM....", dpcmBytes);

                    dpcmFileIndex = fileIndex;
                    fileIndex++;

                    Log.LogMessage(LogSeverity.Info, $"DPCM file size: {dpcmBytes.Length} bytes.");
                }

                var projectInfo = BuildProjectInfo(songIds, name, author);
                var songTable   = BuildSongTableOfContent(project);

                // Export each song as an individual file.
                for (int i = 0; i < project.Songs.Count; i++)
                {
                    var song = project.Songs[i];
                    var songBytes = new FamitoneMusicFile(FamiToneKernel.FamiStudio, false).GetBytes(project, new int[] { song.Id }, FdsSongDataAddr, FdsDpcmStart, MachineType.NTSC);

                    songTable[i].page  = (byte)fileIndex;
                    songTable[i].flags = (byte)(song.UsesDpcm ? dpcmFileIndex : 0);

                    if (songBytes.Length > FdsMaxSongSize)
                    {
                        Log.LogMessage(LogSeverity.Warning, $"Song '{song.Name}' is too large ({songBytes.Length} bytes, maximum is {FdsMaxSongSize}). File will be corrupted.");
                        Array.Resize(ref songBytes, FdsMaxSongSize);
                    }

                    if (fdsFileBytes.Count + FdsBlockHeaderSize + songBytes.Length > FdsMaxFileSize)
                    {
                        Log.LogMessage(LogSeverity.Warning, $"Reached maximum file size ({FdsMaxFileSize}). Songs will be missing.");
                        break;
                    }

                    AddFile(fdsFileBytes, fileIndex, FdsSongDataAddr, $"SONG{i}...", songBytes);

                    fileIndex++;

                    Log.LogMessage(LogSeverity.Info, $"Song '{song.Name}' file size: {songBytes.Length} bytes.");
                }

                //File.WriteAllBytes("D:\\dump\\fdsdata.bin", fdsFileBytes.ToArray());

                // Use this field for the number of files.
                projectInfo.fdsFileCount = (byte)fileIndex;

                // Pad rest with zeroes.
                fdsFileBytes.AddRange(new byte[FdsMaxFileSize - fdsFileBytes.Count]);

                // Build project info + song table of content.
                var tocBytes = new byte[sizeof(RomProjectInfo) + sizeof(RomSongEntry) * songTable.Length];

                Marshal.Copy(new IntPtr(&projectInfo), tocBytes, 0, sizeof(RomProjectInfo));

                for (int i = 0; i < MaxSongs; i++)
                {
                    fixed (RomSongEntry* songEntry = &songTable[i])
                        Marshal.Copy(new IntPtr(songEntry), tocBytes, sizeof(RomProjectInfo) + i * sizeof(RomSongEntry), sizeof(RomSongEntry));
                }

                //File.WriteAllBytes("D:\\dump\\fdstoc.bin", tocBytes);

                // Path TOC file.
                var byteArray = fdsFileBytes.ToArray();
                PatchFile(byteArray, "TOC.....", tocBytes);

                // Build final ROM and save.
                File.WriteAllBytes(filename, byteArray);

                Log.LogMessage(LogSeverity.Info, $"FDS export successful, final file size {byteArray.Length} bytes.");
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
