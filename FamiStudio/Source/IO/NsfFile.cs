using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FamiStudio
{
    public class NsfFile
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

        const int NsfGlobalVarsSize     = 4;
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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct NsfeHeader
        {
            public fixed byte id[4];
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct NsfeChunkHeader
        {
            public int size;
            public fixed byte id[4];

            public void SetId(string s)
            {
                id[0] = (byte)s[0];
                id[1] = (byte)s[1];
                id[2] = (byte)s[2];
                id[3] = (byte)s[3];
            }
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct NsfeInfoChunk
        {
            public ushort loadAddr;
            public ushort initAddr;
            public ushort playAddr;
            public byte palNtscFlags;
            public byte extensionFlags;
            public byte numSongs;
            public byte startSong;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct NsfeBankChunk
        {
            public fixed byte banks[8];
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct NsfeRateChunk
        {
            public ushort playSpeedNTSC;
            public ushort playSpeedPAL;
            public ushort playSpeedDendy;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        unsafe struct NsfeRegionChunk
        {
            public byte supported;
            public byte preferred;
        };

        byte GetNsfExtensionFlags(int mask)
        {
            byte flags = 0;

            if ((mask & ExpansionType.Vrc6Mask) != 0) flags |= 0x01;
            if ((mask & ExpansionType.Vrc7Mask) != 0) flags |= 0x02;
            if ((mask & ExpansionType.FdsMask)  != 0) flags |= 0x04;
            if ((mask & ExpansionType.Mmc5Mask) != 0) flags |= 0x08;
            if ((mask & ExpansionType.N163Mask) != 0) flags |= 0x10;
            if ((mask & ExpansionType.S5BMask)  != 0) flags |= 0x20;
            if ((mask & ExpansionType.EPSMMask) != 0) flags |= 0x80;

            return flags;
        }

        public unsafe bool Save(Project originalProject, int kernel, string filename, int[] songIds, string name, string author, string copyright, int machine, bool nsfe)
        {
#if !DEBUG
            try
            {
#endif
                if (songIds.Length == 0)
                    return false;

                Debug.Assert(!originalProject.UsesAnyExpansionAudio || machine == MachineType.NTSC);

                var project = originalProject.DeepClone();
                project.DeleteAllSongsBut(songIds);

                // In the multiple expansion driver, with EPSM enabled, we run out of RAM and use MMC5 EXRAM
                // so we need to force enable it here.
                if (project.UsesMultipleExpansionAudios && project.UsesEPSMExpansion && !project.UsesMmc5Expansion)
                {
                    project.SetExpansionAudioMask(project.ExpansionAudioMask | ExpansionType.Mmc5Mask);
                }

                var nsfBytes = new List<byte>();

                string kernelBinary = "nsf";
                if (kernel == FamiToneKernel.FamiStudio)
                {
                    kernelBinary += "_famistudio";

                    if (project.UsesFamiTrackerTempo)
                    {
                        kernelBinary += "_famitracker";
                    }

                    if (project.UsesSingleExpansionAudio)
                    {
                        kernelBinary += $"_{ExpansionType.ShortNames[project.SingleExpansion].ToLower()}";
                    
                        if (project.UsesN163Expansion)
                            kernelBinary += $"_{project.ExpansionNumN163Channels}ch";
                    }
                    else if (project.UsesMultipleExpansionAudios)
                    {
                        var numN163Channels = project.UsesN163Expansion ? project.ExpansionNumN163Channels : 1;

                        kernelBinary += $"_multi";
                        kernelBinary += $"_n163_{numN163Channels}ch";

                        if (project.UsesEPSMExpansion)
                        {
                            kernelBinary += $"_epsm";
                        }
                    }
                }
                else
                {
                    kernelBinary += "_famitone2";
                }

                switch (machine)
                {
                    case MachineType.NTSC: kernelBinary += "_ntsc"; break;
                    case MachineType.PAL:  kernelBinary += "_pal";  break;
                    case MachineType.Dual: kernelBinary += "_dual"; break;
                }

                kernelBinary += ".bin";

                // Code/sound engine
                var nsfBinStream = typeof(NsfFile).Assembly.GetManifestResourceStream("FamiStudio.Nsf." + kernelBinary);
                var nsfBinBuffer = new byte[nsfBinStream.Length - 128]; // Skip header.
                nsfBinStream.Seek(128, SeekOrigin.Begin);
                nsfBinStream.Read(nsfBinBuffer, 0, nsfBinBuffer.Length);

                var driverSizeRounded = Utils.RoundUp(nsfBinBuffer.Length, NsfPageSize);

                nsfBytes.AddRange(nsfBinBuffer);

                Log.LogMessage(LogSeverity.Info, $"Sound engine code size: {nsfBinBuffer.Length} bytes.");

                var songTableIdx  = nsfBytes.Count;
                var songTableSize = NsfGlobalVarsSize + project.Songs.Count * NsfSongTableEntrySize;

                nsfBytes.AddRange(new byte[songTableSize]);

                Log.LogMessage(LogSeverity.Info, $"Song table size: {songTableSize} bytes.");

                var songDataIdx  = nsfBytes.Count;
                var dpcmBaseAddr = NsfDpcmOffset;
                var dpcmPadding  = 0;

                if (project.UsesSamples)
                {
                    var totalSampleSize = project.GetTotalSampleSize();

                    // Samples need to be 64-bytes aligned.
                    var initPaddingSize = 64 - (nsfBytes.Count & 0x3f);
                    nsfBytes.AddRange(new byte[initPaddingSize]);

                    // We start putting the samples right after the code, so the first page is not a
                    // full one. If we have near 16KB of samples, we might go over the 4 page limit.
                    // In this case, we will introduce padding until the next page.
                    if (nsfBytes.Count + totalSampleSize > Project.MaxMappedSampleSize)
                    {
                        dpcmPadding = NsfPageSize - (nsfBytes.Count & (NsfPageSize - 1));
                        nsfBytes.AddRange(new byte[dpcmPadding]);
                    }

                    var dpcmPageStart = (nsfBytes.Count) / NsfPageSize;
                    var dpcmPageEnd   = (nsfBytes.Count + totalSampleSize - 1) / NsfPageSize;
                    var dpcmPageCount = dpcmPageEnd - dpcmPageStart + 1;

                    // Otherwise we will allocate at least a full page for the samples and use the following mapping:
                    //    0KB -  4KB samples: starts at 0xf000
                    //    4KB -  8KB samples: starts at 0xe000
                    //    8KB - 12KB samples: starts at 0xd000
                    //   12KB - 16KB samples: starts at 0xc000
                    dpcmBaseAddr += (4 - dpcmPageCount) * NsfPageSize + (nsfBytes.Count & (NsfPageSize - 1));

                    nsfBytes.AddRange(project.GetPackedSampleData());

                    nsfBytes[songTableIdx + 0] = (byte)dpcmPageStart;
                    nsfBytes[songTableIdx + 1] = (byte)dpcmPageCount;

                    Log.LogMessage(LogSeverity.Info, $"DPCM samples size: {totalSampleSize} bytes.");
                    Log.LogMessage(LogSeverity.Info, $"DPCM padding size: {initPaddingSize + dpcmPadding} bytes.");
                }

                // This is only used in multi-expansion.
                nsfBytes[songTableIdx + 2] = (byte)project.ExpansionAudioMask;

                // Export each song individually, build TOC at the same time.
                for (int i = 0; i < project.Songs.Count; i++)
                {
                    var song = project.Songs[i];

                    // If we are in the same page as the driver, the song will start in a 0x8000 address (0x9000 for multi and epsm)
                    // so we need to increment the page by one so that the NSF driver correctly maps the subsequent pages.
                    var samePageAsDriver = nsfBytes.Count < NsfPageSize;
                    int page = nsfBytes.Count / NsfPageSize + (samePageAsDriver ? 1 : 0);
                    int addr = NsfMemoryStart + (samePageAsDriver ? 0 : driverSizeRounded ) + (nsfBytes.Count & (NsfPageSize - 1));
                    var songBytes = new FamitoneMusicFile(kernel, false).GetBytes(project, new int[] { song.Id }, addr, dpcmBaseAddr, machine);

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

                    Log.LogMessage(LogSeverity.Info, $"Song '{song.Name}' size: {songBytes.Length} bytes.");
                }

                if (nsfe)
                {
                    var nsfePreData = new byte[4096];
                    var nsfePreDataSize = 0;
                    var nsfePostData = new byte[8];
                    
                    fixed (byte* fixedPtr = &nsfePreData[0])
                    {
                        byte* p = fixedPtr;

                        // NSFE
                        NsfeHeader* header = (NsfeHeader*)p;
                        header->id[0] = (byte)'N';
                        header->id[1] = (byte)'S';
                        header->id[2] = (byte)'F';
                        header->id[3] = (byte)'E';
                        p += sizeof(NsfeHeader);

                        // INFO chunk
                        NsfeChunkHeader* infoHeader = (NsfeChunkHeader*)p;
                        infoHeader->SetId("INFO");
                        infoHeader->size = sizeof(NsfeInfoChunk);
                        p += sizeof(NsfeChunkHeader);

                        NsfeInfoChunk* infoChunk = (NsfeInfoChunk*)p;
                        infoChunk->loadAddr = 0x8000;
                        infoChunk->initAddr = NsfInitAddr;
                        infoChunk->playAddr = NsfPlayAddr;
                        infoChunk->palNtscFlags = (byte)machine;
                        infoChunk->extensionFlags = GetNsfExtensionFlags(project.ExpansionAudioMask);
                        infoChunk->numSongs = (byte)project.Songs.Count;
                        infoChunk->startSong = 1;
                        p += sizeof(NsfeInfoChunk);
                        
                        // BANK chunk
                        NsfeChunkHeader* bankHeader = (NsfeChunkHeader*)p;
                        bankHeader->SetId("BANK");
                        bankHeader->size = 8;
                        p += sizeof(NsfeChunkHeader);
                        p[0] = 0;
                        p[1] = 1;
                        p[2] = 2;
                        p[3] = 3;
                        p[4] = 4;
                        p[5] = 5;
                        p[6] = 6;
                        p[7] = 7;
                        p += 8;

                        // auth chunk.
                        var nameBytes      = Encoding.ASCII.GetBytes(name + "\0");
                        var artistBytes    = Encoding.ASCII.GetBytes(author + "\0");
                        var copyrightBytes = Encoding.ASCII.GetBytes(copyright + "\0");
                        var ripperBytes    = Encoding.ASCII.GetBytes("FamiStudio\0");

                        NsfeChunkHeader* authHeader = (NsfeChunkHeader*)p;
                        authHeader->SetId("auth");
                        authHeader->size = nameBytes.Length + artistBytes.Length + copyrightBytes.Length + ripperBytes.Length;
                        p += sizeof(NsfeChunkHeader);

                        Marshal.Copy(nameBytes, 0, (IntPtr)p, nameBytes.Length);
                        p += nameBytes.Length;
                        Marshal.Copy(artistBytes, 0, (IntPtr)p, artistBytes.Length);
                        p += artistBytes.Length;
                        Marshal.Copy(copyrightBytes, 0, (IntPtr)p, copyrightBytes.Length);
                        p += copyrightBytes.Length;
                        Marshal.Copy(ripperBytes, 0, (IntPtr)p, ripperBytes.Length);
                        p += ripperBytes.Length;

                        // time chunk.
                        NsfeChunkHeader* timeHeader = (NsfeChunkHeader*)p;
                        timeHeader->SetId("time");
                        timeHeader->size = project.Songs.Count * 4;
                        p += sizeof(NsfeChunkHeader);

                        for (int i = 0; i < project.Songs.Count; i++)
                        {
                            // Use approximation to compute the time. We could *really* run the player
                            // and know the exact number of samples, but this should be good enough.
                            var pal = machine == MachineType.PAL;
                            var song = project.Songs[i];
                            var frames = song.CountFramesBetween(new NoteLocation(0, 0), song.EndLocation, song.FamitrackerSpeed, pal);
                            var time = (int)(frames / (double)(pal ? NesApu.FpsPAL : NesApu.FpsNTSC) * 1000.0);
                            var timeBytes = BitConverter.GetBytes(time);

                            p[0] = timeBytes[0];
                            p[1] = timeBytes[1];
                            p[2] = timeBytes[2];
                            p[3] = timeBytes[3];
                            p += 4;
                        }

                        // tlbl chunk
                        NsfeChunkHeader* tlblHeader = (NsfeChunkHeader*)p;
                        tlblHeader->SetId("tlbl");
                        p += sizeof(NsfeChunkHeader);

                        for (int i = 0; i < project.Songs.Count; i++)
                        {
                            var song = project.Songs[i];
                            var songNameBytes = Encoding.ASCII.GetBytes(song.Name + "\0");
                            Marshal.Copy(songNameBytes, 0, (IntPtr)p, songNameBytes.Length);
                            p += songNameBytes.Length;
                        }

                        tlblHeader->size = (int)(p - (byte*)tlblHeader) - 8;

                        // DATA chunk (must be at end).
                        NsfeChunkHeader* dataHeader = (NsfeChunkHeader*)p;
                        dataHeader->SetId("DATA");
                        dataHeader->size = nsfBytes.Count;
                        p += sizeof(NsfeChunkHeader);

                        nsfePreDataSize = (int)(p - fixedPtr);
                    }

                    fixed (byte* fixedPtr = &nsfePostData[0])
                    {
                        // NEND chunk
                        NsfeChunkHeader* nendHeader = (NsfeChunkHeader*)fixedPtr;
                        nendHeader->SetId("NEND");
                        nendHeader->size = 0;
                    }
                    
                    // Finally insert the NSFE header before the data, not very efficient, but easy.
                    nsfBytes.InsertRange(0, nsfePreData.Take(nsfePreDataSize));

                    // Add NEND chunk at end.
                    nsfBytes.AddRange(nsfePostData);
                }
                else
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
                    header.palNtscFlags = (byte)machine;
                    header.extensionFlags = GetNsfExtensionFlags(project.ExpansionAudioMask);
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

                    // Finally insert the NSF header, not very efficient, but easy.
                    nsfBytes.InsertRange(0, headerBytes);
                }

                File.WriteAllBytes(filename, nsfBytes.ToArray());

                Log.LogMessage(LogSeverity.Info, $"NSF export successful, final file size {nsfBytes.Count} bytes.");
#if !DEBUG
            }
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

        class ChannelState
        {
            public const int Triggered = 1;
            public const int Released  = 2;
            public const int Stopped   = 0;

            public int  period  = -1;
            public int  note    =  0;
            public int  pitch   =  0;
            public int  volume  = 15;
            public int  octave  = -1;
            public int  state   = Stopped;
            public int  dmc     = 0;

            public int fdsModDepth = 0;
            public int fdsModSpeed = 0;

            public Instrument instrument = null;
        };

        IntPtr nsf;
        Song song;
        Project project;
        ChannelState[] channelStates;
        bool preserveDpcmPadding;

        public int GetBestMatchingNote(int period, ushort[] noteTable, out int finePitch)
        {
            int bestNote = -1;
            int minDiff  = 9999999;

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

        private Pattern GetOrCreatePattern(Channel channel, int patternIdx)
        {
            if (channel.PatternInstances[patternIdx] == null)
                channel.PatternInstances[patternIdx] = channel.CreatePattern();
            return channel.PatternInstances[patternIdx];
        }

        private Instrument GetDutyInstrument(Channel channel, int duty)
        {
            var expansion = channel.Expansion;
            var expPrefix = expansion == ExpansionType.None ? "" : ExpansionType.ShortNames[expansion] + " ";
            var name = $"{expPrefix}Duty {duty}";

            var instrument = project.GetInstrument(name);
            if (instrument == null)
            {
                instrument = project.CreateInstrument(expansion, name);
                instrument.Envelopes[EnvelopeType.DutyCycle].Length = 1;
                instrument.Envelopes[EnvelopeType.DutyCycle].Values[0] = (sbyte)duty;
            }

            if (expansion == ExpansionType.Vrc6)
                instrument.Vrc6SawMasterVolume = Vrc6SawMasterVolumeType.Full;

            return instrument;
        }

        private Instrument GetFdsInstrument(sbyte[] wavEnv, sbyte[] modEnv, byte masterVolume)
        {
            foreach (var inst in project.Instruments)
            {
                if (inst.IsFds)
                {
                    if (inst.FdsMasterVolume == masterVolume &&
                        wavEnv.SequenceEqual(inst.Envelopes[EnvelopeType.FdsWaveform].Values.Take(64)) &&
                        modEnv.SequenceEqual(inst.Envelopes[EnvelopeType.FdsModulation].Values.Take(32)))
                    {
                        return inst;
                    }
                }
            }

            for (int i = 1; ; i++)
            {
                var name = $"FDS {i}";
                if (project.IsInstrumentNameUnique(name))
                {
                    var instrument = project.CreateInstrument(ExpansionType.Fds, name);

                    Array.Copy(wavEnv, instrument.Envelopes[EnvelopeType.FdsWaveform].Values,   64);
                    Array.Copy(modEnv, instrument.Envelopes[EnvelopeType.FdsModulation].Values, 32);

                    instrument.FdsMasterVolume = masterVolume;
                    instrument.FdsWavePreset   = WavePresetType.Custom;
                    instrument.FdsModPreset    = WavePresetType.Custom;

                    return instrument;
                }
            }
        }

        private Instrument GetVrc7Instrument(byte patch, byte[] patchRegs)
        {
            if (patch == Vrc7InstrumentPatch.Custom)
            {
                // Custom instrument, look for a match.
                foreach (var inst in project.Instruments)
                {
                    if (inst.IsVrc7)
                    {
                        if (inst.Vrc7Patch == 0 && inst.Vrc7PatchRegs.SequenceEqual(patchRegs))
                            return inst;
                    }
                }

                for (int i = 1; ; i++)
                {
                    var name = $"VRC7 Custom {i}";
                    if (project.IsInstrumentNameUnique(name))
                    {
                        var instrument = project.CreateInstrument(ExpansionType.Vrc7, name);
                        instrument.Vrc7Patch = patch;
                        Array.Copy(patchRegs, instrument.Vrc7PatchRegs, 8);
                        return instrument;
                    }
                }
            }
            else
            {
                // Built-in patch, simply find by name.
                var name = $"VRC7 {Instrument.GetVrc7PatchName(patch)}";
                var instrument = project.GetInstrument(name);

                if (instrument == null)
                {
                    instrument = project.CreateInstrument(ExpansionType.Vrc7, name);
                    instrument.Vrc7Patch = patch;
                }

                return instrument;
            }
        }

        private Instrument GetN163Instrument(sbyte[] waveData, byte wavePos)
        {
            foreach (var inst in project.Instruments)
            {
                if (inst.IsN163)
                {
                    if (inst.N163WavePos  == wavePos &&
                        inst.N163WaveSize == waveData.Length &&
                        waveData.SequenceEqual(inst.Envelopes[EnvelopeType.N163Waveform].Values.Take(waveData.Length)))
                    {
                        return inst;
                    }
                }
            }

            for (int i = 1; ; i++)
            {
                var name = $"N163 {i}";
                if (project.IsInstrumentNameUnique(name))
                {
                    var instrument = project.CreateInstrument(ExpansionType.N163, name);

                    instrument.N163WavePreset = WavePresetType.Custom;
                    instrument.N163WaveSize   = (byte)waveData.Length;
                    instrument.N163WavePos    = wavePos;
                    Array.Copy(waveData, instrument.Envelopes[EnvelopeType.N163Waveform].Values, waveData.Length);

                    return instrument;
                }
            }
        }

        private Instrument GetS5BInstrument()
        {
            foreach (var inst in project.Instruments)
            {
                if (inst.IsS5B)
                    return inst;
            }

            return project.CreateInstrument(ExpansionType.S5B, "S5B");
        }
        
        private bool UpdateChannel(int p, int n, Channel channel, ChannelState state)
        {
            var project = channel.Song.Project;
            var hasNote = false;

            if (channel.Type == ChannelType.Dpcm)
            {
                var dmc = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_DPCMCOUNTER, 0);
                var len = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_DPCMSAMPLELENGTH, 0);

                if (len > 0) 
                {
                    // Subtracting one here is not correct. But it is a fact that a lot of games
                    // seemed to favor tight sample packing and did not care about playing one
                    // extra sample of garbage.
                    if (!preserveDpcmPadding)
                    {
                        Debug.Assert((len & 0xf) == 1);
                        len--;
                        Debug.Assert((len & 0xf) == 0);
                    }

                    var sampleData = new byte[len];
                    for (int i = 0; i < len; i++)
                        sampleData[i] = (byte)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_DPCMSAMPLEDATA, i);

                    var sample = project.FindMatchingSample(sampleData);
                    if (sample == null)
                        sample = project.CreateDPCMSampleFromDmcData($"Sample {project.Samples.Count + 1}", sampleData);

                    var loop  = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_DPCMLOOP, 0) != 0;
                    var pitch = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_DPCMPITCH, 0);

                    var noteValue = project.FindDPCMSampleMapping(sample, pitch, loop);
                    if (noteValue == -1)
                    {
                        for (int i = Note.DPCMNoteMin + 1; i <= Note.DPCMNoteMax; i++)
                        {
                            if (project.GetDPCMMapping(i) == null)
                            {
                                noteValue = i;
                                project.MapDPCMSample(i, sample, pitch, loop);
                                break;
                            }
                        }
                    }

                    if (noteValue != -1)
                    {
                        var note = GetOrCreatePattern(channel, p).GetOrCreateNoteAt(n);
                        note.Value = (byte)noteValue;
                        if (state.dmc != dmc)
                        {
                            note.DeltaCounter = (byte)dmc;
                            state.dmc = dmc;
                        }
                        hasNote = true;
                    }
                }
                else if (dmc != state.dmc)
                {
                    GetOrCreatePattern(channel, p).GetOrCreateNoteAt(n).DeltaCounter = (byte)dmc;
                    state.dmc = dmc;
                }
            }
            else
            {
                var period  = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_PERIOD, 0);
                var volume  = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_VOLUME, 0);
                var duty    = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_DUTYCYCLE, 0);
                var force   = false;
                var stop    = false;
                var release = false;
                var attack  = true;
                var octave  = -1;

                // VRC6 has a much larger volume range (6-bit) than our volume (4-bit).
                if (channel.Type == ChannelType.Vrc6Saw)
                {
                    volume >>= 2;
                }
                else if (channel.Type == ChannelType.FdsWave)
                {
                    volume = Math.Min(Note.VolumeMax, volume >> 1);
                }
                else if (channel.Type >= ChannelType.Vrc7Fm1 && channel.Type <= ChannelType.Vrc7Fm6)
                {
                    volume = 15 - volume;
                }

                var hasOctave  = channel.IsVrc7Channel;
                var hasVolume  = channel.Type != ChannelType.Triangle;
                var hasPitch   = channel.Type != ChannelType.Noise;
                var hasDuty    = channel.Type == ChannelType.Square1 || channel.Type == ChannelType.Square2 || channel.Type == ChannelType.Noise || channel.Type == ChannelType.Vrc6Square1 || channel.Type == ChannelType.Vrc6Square2 || channel.Type == ChannelType.Mmc5Square1 || channel.Type == ChannelType.Mmc5Square2;
                var hasTrigger = channel.IsVrc7Channel;

                if (channel.Type >= ChannelType.Vrc7Fm1 && channel.Type <= ChannelType.Vrc7Fm6)
                {
                    var trigger = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_VRC7TRIGGER, 0);
                    var sustain = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_VRC7SUSTAIN, 0) != 0;

                    var newState = state.state;

                    if (trigger == 0)
                        attack = false;
                    else
                        newState = trigger > 0 ? ChannelState.Triggered : (sustain ? ChannelState.Released : ChannelState.Stopped);

                    if (newState != state.state || trigger > 0)
                    {
                        stop    = newState == ChannelState.Stopped;
                        release = newState == ChannelState.Released;
                        state.state = newState;
                        force |= true;
                    }

                    octave = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_VRC7OCTAVE, 0);
                }
                else
                {
                    var newState = volume != 0 && (channel.Type == ChannelType.Noise || period != 0) ? ChannelState.Triggered : ChannelState.Stopped;

                    if (newState != state.state)
                    {
                        stop = newState == ChannelState.Stopped;
                        force |= true;
                        state.state = newState;
                    }
                }

                if (hasVolume)
                {
                    if (state.volume != volume && (volume != 0 || hasTrigger))
                    {
                        var pattern = GetOrCreatePattern(channel, p).GetOrCreateNoteAt(n).Volume = (byte)volume;
                        state.volume = volume;
                    }
                }

                Instrument instrument = null;

                if (hasDuty)
                {
                    instrument = GetDutyInstrument(channel, duty);
                }
                else if (channel.Type == ChannelType.FdsWave)
                {
                    var wavEnv = new sbyte[64];
                    var modEnv = new sbyte[32];

                    for (int i = 0; i < 64; i++)
                        wavEnv[i] = (sbyte)(NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FDSWAVETABLE, i) & 0x3f);
                    for (int i = 0; i < 32; i++)
                        modEnv[i] = (sbyte)(NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FDSMODULATIONTABLE, i));

                    Envelope.ConvertFdsModulationToAbsolute(modEnv);

                    var masterVolume = (byte)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FDSMASTERVOLUME, 0);

                    instrument = GetFdsInstrument(wavEnv, modEnv, masterVolume);

                    int modDepth = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FDSMODULATIONDEPTH, 0);
                    int modSpeed = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FDSMODULATIONSPEED, 0);

                    if (state.fdsModDepth != modDepth)
                    {
                        var pattern = GetOrCreatePattern(channel, p).GetOrCreateNoteAt(n).FdsModDepth = (byte)modDepth;
                        state.fdsModDepth = modDepth;
                    }

                    if (state.fdsModSpeed != modSpeed)
                    {
                        var pattern = GetOrCreatePattern(channel, p).GetOrCreateNoteAt(n).FdsModSpeed = (ushort)modSpeed;
                        state.fdsModSpeed = modSpeed;
                    }
                }
                else if (channel.Type >= ChannelType.N163Wave1 &&
                         channel.Type <= ChannelType.N163Wave8)
                {
                    var wavePos = (byte)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_N163WAVEPOS,  0);
                    var waveLen = (byte)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_N163WAVESIZE, 0);

                    if (waveLen > 0)
                    {
                        var waveData = new sbyte[waveLen];
                        for (int i = 0; i < waveLen; i++)
                            waveData[i] = (sbyte)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_N163WAVE, wavePos + i);

                        instrument = GetN163Instrument(waveData, wavePos);
                    }

                    period >>= 2; 
                }
                else if (channel.Type >= ChannelType.Vrc7Fm1 &&
                         channel.Type <= ChannelType.Vrc7Fm6)
                {
                    var patch = (byte)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_VRC7PATCH, 0);
                    var regs = new byte[8];

                    if (patch == 0)
                    {
                        for (int i = 0; i < 8; i++)
                            regs[i] = (byte)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_VRC7PATCHREG, i);
                    }

                    instrument = GetVrc7Instrument(patch, regs);
                }
                else if (channel.Type >= ChannelType.S5BSquare1 && channel.Type <= ChannelType.S5BSquare3)
                {
                    instrument = GetS5BInstrument();
                }
                else 
                {
                    instrument = GetDutyInstrument(channel, 0);
                }

                if ((state.period != period) || (hasOctave && state.octave != octave) || (instrument != state.instrument) || force)
                {
                    var noteTable = NesApu.GetNoteTableForChannelType(channel.Type, project.PalMode, project.ExpansionNumN163Channels);
                    var note = release ? Note.NoteRelease : (stop ? Note.NoteStop : state.note);
                    var finePitch = 0;

                    if (!stop && !release && state.state != ChannelState.Stopped)
                    {
                        if (channel.Type == ChannelType.Noise)
                            note = (period ^ 0x0f) + 32;
                        else
                            note = (byte)GetBestMatchingNote(period, noteTable, out finePitch);

                        if (hasOctave)
                        {
                            period *= (1 << octave);
                            while (note > 12)
                            {
                                note -= 12;
                                octave++;
                            }
                            note += octave * 12;
                            note = Math.Min(note, noteTable.Length - 1);
                            finePitch = period - noteTable[note];
                        }
                    }

                    if (note < Note.MusicalNoteMin || note > Note.MusicalNoteMax)
                        instrument = null;

                    if ((state.note != note) || (state.instrument != instrument && instrument != null) || force)
                    {
                        var pattern = GetOrCreatePattern(channel, p);
                        var newNote = pattern.GetOrCreateNoteAt(n);
                        newNote.Value = (byte)note;
                        newNote.Instrument = instrument;
                        state.note = note;
                        state.octave = octave;
                        if (instrument != null)
                            state.instrument = instrument;
                        if (!attack)
                            newNote.HasAttack = false;
                        hasNote = note != 0;
                    }

                    if (hasPitch && !stop)
                    {
                        Channel.GetShiftsForType(channel.Type, project.ExpansionNumN163Channels, out int pitchShift, out _);

                        // We scale all pitches changes (slides, fine pitch, pitch envelopes) for
                        // some channels with HUGE pitch values (N163, VRC7).
                        finePitch >>= pitchShift;

                        var pitch = (sbyte)Utils.Clamp(finePitch, Note.FinePitchMin, Note.FinePitchMax);

                        if (pitch != state.pitch)
                        {
                            var pattern = GetOrCreatePattern(channel, p).GetOrCreateNoteAt(n).FinePitch = pitch;
                            state.pitch = pitch;
                        }
                    }

                    state.period = period;
                }
            }

            return hasNote;
        }

        public static string[] GetSongNames(string filename)
        {
            var nsf = NotSoFatso.NsfOpen(filename);

            if (nsf == IntPtr.Zero)
                return null;

            var trackCount = NotSoFatso.NsfGetTrackCount(nsf);
            var trackNames = new string[trackCount];

            for (int i = 0; i < trackCount; i++)
            {
                var name = Marshal.PtrToStringAnsi(NotSoFatso.NsfGetTrackName(nsf, i));
                if (string.IsNullOrEmpty(name))
                {
                    trackNames[i] = $"Song {i+1}";
                }
                else
                {
                    trackNames[i] = name;
                }
            }

            NotSoFatso.NsfClose(nsf);

            return trackNames;
        }

        private int GetNumNamcoChannels(string filename, int songIndex, int numFrames)
        {
            var tmpNsf = NotSoFatso.NsfOpen(filename);

            NotSoFatso.NsfSetTrack(tmpNsf, songIndex);

            int numNamcoChannels = 1;
            for (int i = 0; i < numFrames; i++)
            {
                var playCalled = NotSoFatso.NsfRunFrame(tmpNsf);
                if (playCalled != 0)
                    numNamcoChannels = Math.Max(numNamcoChannels, NotSoFatso.NsfGetState(tmpNsf, ChannelType.N163Wave1, NotSoFatso.STATE_N163NUMCHANNELS, 0));
            }

            NotSoFatso.NsfClose(tmpNsf);

            return numNamcoChannels;
        }

        int GetExpansionMaskFromNsfFlags(int flags)
        {
            var mask = 0;

            if ((flags & 0x01) != 0) mask |= ExpansionType.Vrc6Mask;
            if ((flags & 0x02) != 0) mask |= ExpansionType.Vrc7Mask;
            if ((flags & 0x04) != 0) mask |= ExpansionType.FdsMask;
            if ((flags & 0x08) != 0) mask |= ExpansionType.Mmc5Mask;
            if ((flags & 0x10) != 0) mask |= ExpansionType.N163Mask;
            if ((flags & 0x20) != 0) mask |= ExpansionType.S5BMask;
            if ((flags & 0x80) != 0) mask |= ExpansionType.EPSMMask;

            return mask;
        }

        public Project Load(string filename, int songIndex, int duration, int patternLength, int startFrame, bool removeIntroSilence, bool reverseDpcm, bool preserveDpcmPad)
        {
            nsf = NotSoFatso.NsfOpen(filename);

            if (nsf == IntPtr.Zero)
            {
                Log.LogMessage(LogSeverity.Error, "Error opening NSF. File may be corrupted or may be a NSF2 using advanced features such as IRQ which are not supported at the moment.");
                return null;
            }

            var trackCount = NotSoFatso.NsfGetTrackCount(nsf);

            if (songIndex < 0 || songIndex > trackCount)
                return null;

            preserveDpcmPadding = preserveDpcmPad;

            var palSource = (NotSoFatso.NsfIsPal(nsf) & 1) == 1;
            var numFrames = duration * (palSource ? 50 : 60);
            var clockSpeed = NotSoFatso.NsfGetClockSpeed(nsf);

            // Clock speed sanity check.
            if (( palSource && Math.Abs(clockSpeed - 19997) > 100) ||
                (!palSource && Math.Abs(clockSpeed - 16639) > 100))
            {
                Log.LogMessage(LogSeverity.Warning, "NSF uses non-standard clock speed and will play at the wrong speed.");
            }

            project = new Project();

            project.Name      = Marshal.PtrToStringAnsi(NotSoFatso.NsfGetTitle(nsf));
            project.Author    = Marshal.PtrToStringAnsi(NotSoFatso.NsfGetArtist(nsf));
            project.Copyright = Marshal.PtrToStringAnsi(NotSoFatso.NsfGetCopyright(nsf));
            project.PalMode   = palSource;

            // Our expansion mask is the same as NSF.
            var expansionMask = GetExpansionMaskFromNsfFlags(NotSoFatso.NsfGetExpansion(nsf));

            // The 2 upper bits of the mask need to be zero, we dont support these.
            if (expansionMask != (expansionMask & ExpansionType.AllMask))
            {
                Log.LogMessage(LogSeverity.Error, "NSF uses unknown or unsupported expansion chips, aborting.");
                NotSoFatso.NsfClose(nsf);
                return null;
            }

            if ((expansionMask & ExpansionType.EPSMMask) != 0)
            {
                Log.LogMessage(LogSeverity.Warning, "NSF seem to use EPSM, import is still not supported.");
                expansionMask &= (~ExpansionType.EPSMMask);
            }

            var numN163Channels = (expansionMask & ExpansionType.N163Mask) != 0 ? GetNumNamcoChannels(filename, songIndex, numFrames) : 1;
            project.SetExpansionAudioMask(expansionMask, numN163Channels);

            var songName = Marshal.PtrToStringAnsi(NotSoFatso.NsfGetTrackName(nsf, songIndex));

            song = project.CreateSong(string.IsNullOrEmpty(songName) ? $"Song {songIndex + 1}" : songName);
            channelStates = new ChannelState[song.Channels.Length];

            NotSoFatso.NsfSetTrack(nsf, songIndex);

            song.ChangeFamiStudioTempoGroove(new[] { 1 }, false);
            song.SetDefaultPatternLength(patternLength);

            for (int i = 0; i < song.Channels.Length; i++)
                channelStates[i] = new ChannelState();

            var foundFirstNote = !removeIntroSilence;

            var p = 0;
            var n = 0;
            var f = startFrame;

            for (int i = 0; i < numFrames; i++)
            {
                p = f / song.PatternLength;
                n = f % song.PatternLength;

                if (p >= Song.MaxLength - 1)
                    break;

                var playCalled = 0;
                var waitFrameCount = 0;
                do
                {
                    playCalled = NotSoFatso.NsfRunFrame(nsf);

                    if (++waitFrameCount == 1000)
                    {
                        Log.LogMessage(LogSeverity.Error, "NSF did not call PLAY after 1000 frames, aborting.");
                        NotSoFatso.NsfClose(nsf);
                        return null;
                    }
                }
                while (playCalled == 0);

                for (int c = 0; c < song.Channels.Length; c++)
                    foundFirstNote |= UpdateChannel(p, n, song.Channels[c], channelStates[c]);

                if (foundFirstNote)
                {
                    f++;
                }
                else
                {
                    // Reset everything until we find our first note.
                    project.DeleteAllInstruments();
                    project.DeleteAllSamples();
                    for (int c = 0; c < song.Channels.Length; c++)
                        channelStates[c] = new ChannelState();
                }
            }

            song.SetLength(p + 1);

            NotSoFatso.NsfClose(nsf);

            var factors = Utils.GetFactors(song.PatternLength, FamiStudioTempoUtils.MaxNoteLength);
            if (factors.Length > 0)
            {
                var noteLen = factors[0];

                // Look for a factor that generates a note length < 10 and gives a pattern length that is a multiple of 16.
                foreach (var factor in factors)
                {
                    if (factor <= 10)
                    {
                        noteLen = factor;
                        if (((song.PatternLength / noteLen) % 16) == 0)
                            break;
                    }
                }

                song.ChangeFamiStudioTempoGroove(new[] { noteLen }, false);
            }
            else
            {
                song.ChangeFamiStudioTempoGroove(new[] { 1 }, false);
            }

            song.SetSensibleBeatLength();
            song.ConvertToCompoundNotes();
            song.DeleteEmptyPatterns();
            song.UpdatePatternStartNotes();
            song.InvalidateCumulativePatternCache();
            project.DeleteUnusedInstruments();

            foreach (var sample in project.Samples)
                sample.ReverseBits = reverseDpcm;

            return project;
        }
    }
}
