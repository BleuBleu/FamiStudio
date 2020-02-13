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

                var project = originalProject.DeepClone();
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
                header.extensionFlags = (byte)(project.ExpansionAudio == Project.ExpansionVrc6 ? 1 : 0);
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
                    kernelBinary = project.ExpansionAudio == Project.ExpansionVrc6 ? 
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

#if FAMISTUDIO_WINDOWS
        private const string NotSoFatsoDll = "NotSoFatso.dll";
#elif FAMISTUDIO_MACOS
        private const string NotSoFatsoDll = "NotSoFatso.dylib";
#else
        private const string NotSoFatsoDll = "NotSoFatso.so";
#endif

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static IntPtr NsfOpen(string file);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static int NsfGetTrackCount(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static int NsfIsPal(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static int NsfGetExpansion(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static IntPtr NsfGetTitle(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static IntPtr NsfGetArtist(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static IntPtr NsfGetCopyright(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static IntPtr NsfGetTrackName(IntPtr nsf, int track);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static void NsfClose(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static void NsfSetTrack(IntPtr nsf, int track);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static void NsfRunFrame(IntPtr nsf);

        [DllImport(NotSoFatsoDll, CallingConvention = CallingConvention.StdCall)]
        public extern static int NsfGetState(IntPtr nsf, int channel, int state, int sub);

        const int EXTSOUND_VRC6  = 0x01;
        const int EXTSOUND_VRC7  = 0x02;
        const int EXTSOUND_FDS   = 0x04;
        const int EXTSOUND_MMC5  = 0x08;
        const int EXTSOUND_N106  = 0x10;
        const int EXTSOUND_FME07 = 0x20;

        const int STATE_VOLUME             = 0;
        const int STATE_PERIOD             = 1;
        const int STATE_DUTYCYCLE          = 2;
        const int STATE_DPCMSAMPLELENGTH   = 3;
        const int STATE_DPCMSAMPLEADDR     = 4;
        const int STATE_DPCMSAMPLEDATA     = 5;
        const int STATE_DPCMLOOP           = 6;
        const int STATE_DPCMPITCH          = 7;
        const int STATE_FDSWAVETABLE       = 8;
        const int STATE_FDSMODULATIONTABLE = 9;

        class ChannelState
        {
            public int period  = -1;
            public int note    = -1;
            public int volume  = -1;
            public int duty    = -1;
        };

        public static int GetBestMatchingNote(int period, ushort[] noteTable, out int finePitch)
        {
            int bestNote = -1;
            int minDiff  = 9999;

            for (int i = 0; i < noteTable.Length; i++)
            {
                var diff = Math.Abs(noteTable[i] - period);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    bestNote = i;
                }
            }

            finePitch = period - noteTable[bestNote];

            return bestNote;
        }

        private static Pattern GetOrCreatePattern(Channel channel, int patternIdx)
        {
            if (channel.PatternInstances[patternIdx].Pattern == null)
                channel.PatternInstances[patternIdx].Pattern = channel.CreatePattern();
            return channel.PatternInstances[patternIdx].Pattern;
        }

        private static Instrument GetDutyInstrument(Project project, Channel channel, int duty)
        {
            var expansion = channel.IsExpansionChannel ? project.ExpansionAudio : Project.ExpansionNone;
            var name = $"{Project.ExpansionShortNames[expansion]} Duty {duty}";

            var instrument = project.GetInstrument(name);
            if (instrument == null)
            {
                instrument = project.CreateInstrument(expansion, name);
                instrument.DutyCycle = duty;
            }

            return instrument;
        }

        private static bool UpdateChannel(IntPtr nsf, int p, int n, Channel channel, ChannelState state)
        {
            var project = channel.Song.Project;
            var channelIdx = Channel.ChannelTypeToIndex(channel.Type);
            var hasNote = false;

            if (channel.Type == Channel.Dpcm)
            {
                var len = NsfGetState(nsf, channel.Type, STATE_DPCMSAMPLELENGTH, 0);

                if (len > 0)
                {
                    var addr = NsfGetState(nsf, channel.Type, STATE_DPCMSAMPLEADDR, 0);
                    var name = $"Sample 0x{addr:x8}";
                    var sample = project.GetSample(name);

                    if (sample == null)
                    {
                        var sampleData = new byte[len];
                        for (int i = 0; i < len; i++)
                            sampleData[i] = (byte)NsfGetState(nsf, channel.Type, STATE_DPCMSAMPLEDATA, i);
                        sample = project.CreateDPCMSample(name, sampleData);
                    }

                    var loop  = NsfGetState(nsf, channel.Type, STATE_DPCMLOOP, 0) != 0;
                    var pitch = NsfGetState(nsf, channel.Type, STATE_DPCMPITCH, 0);

                    var note = project.FindDPCMSampleMapping(sample, pitch, loop);
                    if (note == -1)
                    {
                        for (int i = Note.DPCMNoteMin + 1; i <= Note.DPCMNoteMax; i++)
                        {
                            if (project.GetDPCMMapping(i) == null)
                            {
                                note = i;
                                project.MapDPCMSample(i, sample, pitch, loop);
                                break;
                            }
                        }
                    }

                    if (note != -1)
                    {
                        var pattern = GetOrCreatePattern(channel, p);
                        pattern.Notes[n].Value = (byte)note;
                        hasNote = true;
                    }
                }
            }
            else
            {
                var period = NsfGetState(nsf, channel.Type, STATE_PERIOD, 0);
                var volume = NsfGetState(nsf, channel.Type, STATE_VOLUME, 0);
                var duty   = NsfGetState(nsf, channel.Type, STATE_DUTYCYCLE, 0);
                var force  = false;
                var stop   = false;

                // VRC6 has a much larger volume range (6-bit) than our volume (4-bit).
                // We also use odd duties to double the volume values.
                if (channel.Type == Channel.Vrc6Saw)
                {
                    volume >>= 1;
                    if (volume >= 0x10)
                    {
                        duty = 1;
                        volume >>= 1;
                        volume--;
                    }
                    else
                    {
                        duty = 0;
                    }
                }

                var hasVolume = channel.Type != Channel.Dpcm;
                var hasPeriod = channel.Type != Channel.Dpcm;
                var hasDuty   = channel.Type == Channel.Square1 || channel.Type == Channel.Square2 || channel.Type == Channel.Noise || channel.Type == Channel.Vrc6Square1 || channel.Type == Channel.Vrc6Square2 || channel.Type == Channel.Vrc6Saw;

                if (hasVolume && state.volume != volume)
                {
                    var pattern = GetOrCreatePattern(channel, p);

                    if (volume == 0)
                    {
                        if (state.volume != 0)
                        {
                            stop = true;
                            force |= true;
                        }
                    }
                    else
                    {
                        if (state.volume == 0)
                            force |= true;

                        pattern.Notes[n].Volume = (byte)volume;
                    }

                    state.volume = volume;
                }

                Instrument instrument = null;

                if (hasDuty)
                {
                    instrument = GetDutyInstrument(project, channel, duty);

                    if (state.duty != duty)
                    {
                        state.duty = duty;
                        force |= state.volume != 0;
                    }
                }
                else 
                {
                    instrument = GetDutyInstrument(project, channel, 0);
                }

                if (hasPeriod && (state.period != period || force))
                {
                    var pattern = GetOrCreatePattern(channel, p);
                    var noteTable = NesApu.GetNoteTableForChannelType(channel.Type, false);
                    var note = 0;
                    var finePitch = 0;

                    if (!stop)
                    {
                        if (channel.Type == Channel.Noise)
                            note = (period ^ 0x0f) + 32;
                        else
                            note = (byte)GetBestMatchingNote(period, noteTable, out finePitch);
                    }

                    if (state.note != note || force)
                    {
                        pattern.Notes[n].Value = (byte)note;
                        pattern.Notes[n].Instrument = stop ? null : instrument;
                        if (!stop)
                            pattern.Notes[n].Volume = (byte)volume;
                        state.note = note;
                        hasNote = note != 0;
                    }

                    if (!stop)
                    {
                        var pitch = (sbyte)Utils.Clamp(finePitch, Note.FinePitchMin, Note.FinePitchMax);

                        if (pitch != 0)
                            pattern.Notes[n].FinePitch = pitch;
                    }

                    state.period = period;
                }
            }

            return hasNote;
        }

        public static string[] GetSongNames(string filename)
        {
            var nsf = NsfOpen(filename);

            if (nsf == null)
                return null;

            var trackCount = NsfGetTrackCount(nsf);
            var trackNames = new string[trackCount];

            for (int i = 0; i < trackCount; i++)
            {
                var name = Marshal.PtrToStringAnsi(NsfGetTrackName(nsf, i));
                if (string.IsNullOrEmpty(name))
                {
                    trackNames[i] = $"Song {i}";
                }
                else
                {
                    trackNames[i] = name;
                }
            }

            NsfClose(nsf);

            return trackNames;
        }

        public static Project Load(string filename, int songIndex, int duration, int patternLength, bool removeIntroSilence)
        {
            var nsf = NsfOpen(filename);

            if (nsf == null)
                return null;

            var project = new Project();

            project.Name      = Marshal.PtrToStringAnsi(NsfGetTitle(nsf));
            project.Author    = Marshal.PtrToStringAnsi(NsfGetArtist(nsf));
            project.Copyright = Marshal.PtrToStringAnsi(NsfGetCopyright(nsf));

            switch (NsfGetExpansion(nsf))
            {
                case 0: 
                    break;
                case EXTSOUND_VRC6:
                    project.SetExpansionAudio(Project.ExpansionVrc6);
                    break;
                case EXTSOUND_VRC7:
                    break;
                case EXTSOUND_FDS:
                    break;
                case EXTSOUND_MMC5:
                    break;
                case EXTSOUND_N106:
                    break;
                case EXTSOUND_FME07:
                    break;
                default:
                    NsfClose(nsf); // Unsupported expansion combination.
                    return null;
            }

            var songName = Marshal.PtrToStringAnsi(NsfGetTrackName(nsf, songIndex));
            var song = project.CreateSong(string.IsNullOrEmpty(songName) ? null : songName); 
            var channelStates = new ChannelState[song.Channels.Length];

            NsfSetTrack(nsf, songIndex);

            song.Speed = 1;
            song.SetPatternLength(patternLength);
            song.SetSensibleBarLength();

            for (int i = 0; i < song.Channels.Length; i++)
                channelStates[i] = new ChannelState();

            var foundFirstNote = !removeIntroSilence;
            var numFrames = duration * (NsfIsPal(nsf) != 0 ? 50 : 60);

            for (int f = 0, i = 0; f < numFrames; f++)
            {
                var p = i / song.DefaultPatternLength;
                var n = i % song.DefaultPatternLength;

                NsfRunFrame(nsf);

                for (int c = 0; c < song.Channels.Length; c++)
                    foundFirstNote |= UpdateChannel(nsf, p, n, song.Channels[c], channelStates[c]);

                if (foundFirstNote) i++;
            }

            NsfClose(nsf);

            return project;
        }
    }
}
