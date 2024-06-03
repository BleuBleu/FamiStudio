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
        // NSF memory layout (for 1-bank sized driver).
        //   - 8000-cfff: Song data
        //   - e000-efff: DPCM (if any)
        //   - f000-ffff: Engine code + song table + vectors.
        //     - f000: Song table of content.
        //     - f100: nsf_init
        //     - f160: nsf_play
        //     - f180: Driver code
        //     - f?00: Small DPCM or song data if it fits after driver code.
        //     - fffa: Vectors
        //
        // We have drivers that are 1, 2 and 3 bank large. 
        //   - 1 page : DPCM (if any) is in e000-efff and code is in f000-ffff (as above).
        //   - 2 page : DPCM (if any) is in d000-dfff and code is in e000-ffff.
        //   - 3 page : DPCM (if any) is in c000-cfff and code is in d000-ffff.

        const int NsfMemoryStart     = 0x8000;
        const int NsfInitOffset      = 0x0100;
        const int NsfPlayOffset      = 0x0160;
        const int NsfKernelOffset    = 0x0180;
        const int NsfBankSize        = 0x1000;
        const int NsfKernelLastBank  = 0xf000;
        const int NsfVectorSize      = 6;

        const int NsfGlobalVarsSize     = 4;
        const int NsfSongTableEntrySize = 4;
        const int NsfHeaderSize         = 128;
        const int NsfSongTableSize      = 256;
        const int NsfMaxSongs           = (NsfSongTableSize - NsfGlobalVarsSize) / NsfSongTableEntrySize;

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
                project.SoundEngineUsesExtendedInstruments = true;
                project.SoundEngineUsesDpcmBankSwitching = true;
                project.SoundEngineUsesExtendedDpcm = true;

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
                        kernelBinary += $"_{ExpansionType.InternalNames[project.SingleExpansion].ToLower()}";
                    
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
                var nsfBinBuffer = new byte[nsfBinStream.Length - NsfHeaderSize]; // Skip header.
                nsfBinStream.Seek(NsfHeaderSize, SeekOrigin.Begin);
                nsfBinStream.Read(nsfBinBuffer, 0, nsfBinBuffer.Length);
            
                // Our drivers are all 1, 2 or 3 bank large. 
                Debug.Assert(nsfBinBuffer.Length % NsfBankSize == 0);

                var driverSizePadded = nsfBinBuffer.Length;
                var driverBankCount = driverSizePadded / NsfBankSize;
                var driverActualSize = driverSizePadded;
                
                // Figure out actual code size, round up to next 256-byte page.
                for (; driverActualSize >= 1; driverActualSize--)
                {
                    if (nsfBinBuffer[driverActualSize - 1] != 0)
                        break;
                }

                driverActualSize += 6; // Vectors

                // If we hit this, it means the NSF was compiled with the wrong number of banks.
                Debug.Assert(Utils.RoundUp(driverActualSize, NsfBankSize) == driverSizePadded);

                driverActualSize = Utils.RoundUp(driverActualSize, 256); 

                nsfBytes.AddRange(nsfBinBuffer);

                Log.LogMessage(LogSeverity.Info, $"Sound engine code size: {nsfBinBuffer.Length} bytes.");

                var songTableIdx = 0;
                var codeBaseAddr = 0x10000 - driverSizePadded;
                var dpcmBaseAddr = codeBaseAddr - NsfBankSize;
                var dpcmBankSize = NsfBankSize;
                var dpcmNumBanks = 0;
                var driverBankOffset = driverActualSize;
                var driverBankLeft = driverSizePadded == driverActualSize ? 0 : NsfBankSize - (driverBankOffset & (NsfBankSize - 1)) - NsfVectorSize;
                Debug.Assert(driverBankLeft >= 0 && driverBankLeft < NsfBankSize);

                if (project.UsesSamples)
                {
                    dpcmNumBanks = project.AutoAssignSamplesBanks(dpcmBankSize, out var overflow);

                    var dpcmBankStart = 0;
                    var firstBankSampleData = project.GetPackedSampleData(0, dpcmBankSize);

                    // If there is just 1 bank, try to fit after the driver.
                    if (dpcmNumBanks == 1 && firstBankSampleData.Length <= driverBankLeft)
                    {
                        for (var i = 0; i < firstBankSampleData.Length; i++)
                            nsfBytes[driverBankOffset + i] = firstBankSampleData[i];

                        dpcmBaseAddr += driverBankOffset & (NsfBankSize - 1);
                        driverBankOffset += firstBankSampleData.Length;
                        driverBankLeft -= firstBankSampleData.Length;
                        dpcmBankStart = driverBankCount - 1;

                        Log.LogMessage(LogSeverity.Info, $"Merging DPCM samples with driver code.");
                    }
                    else
                    {
                        dpcmBankStart = (nsfBytes.Count) / NsfBankSize;

                        // TODO : The last bank may not be full, we could squeeze songs in there.
                        for (var i = 0; i < dpcmNumBanks; i++)
                        {
                            nsfBytes.AddRange(project.GetPackedSampleData(i, dpcmBankSize));
                            Utils.PadToNextBank(nsfBytes, dpcmBankSize);
                        }

                        Log.LogMessage(LogSeverity.Info, $"Allocating {dpcmNumBanks} bank(s) ({dpcmNumBanks * NsfBankSize} bytes) for DPCM samples.");
                    }

                    nsfBytes[songTableIdx + 0] = (byte)(dpcmBankStart);
                }

                nsfBytes[songTableIdx + 1] = (byte)((dpcmBaseAddr >> 12) - 8);
                nsfBytes[songTableIdx + 2] = (byte)project.ExpansionAudioMask;
                nsfBytes[songTableIdx + 3] = (byte)(8 - driverBankCount - (dpcmNumBanks > 0 ? 1 : 0));

                // Export each song individually, build TOC at the same time.
                for (var i = 0; i < project.Songs.Count; i++)
                {
                    var song = project.Songs[i];

                    var bank = nsfBytes.Count / NsfBankSize;
                    var addr = NsfMemoryStart + (nsfBytes.Count & (NsfBankSize - 1));
                    var songBytes = new FamitoneMusicFile(kernel, false).GetBytes(project, new int[] { song.Id }, addr, dpcmBankSize, dpcmBaseAddr, machine);
                    var maxSongAddr = project.UsesSamples ? dpcmBaseAddr : codeBaseAddr;

                    if (addr + songBytes.Length > maxSongAddr)
                    {
                        Log.LogMessage(LogSeverity.Error, $"Song '{song.Name}' is too large ({songBytes.Length} bytes). Try reducing its size and/or the DPCM sample memory.");
                        return false;
                    }
                
                    // Is there enough room to fit in the same bank as the driver?
                    if (songBytes.Length <= driverBankLeft)
                    {
                        addr = codeBaseAddr + driverBankOffset;
                        songBytes = new FamitoneMusicFile(kernel, false).GetBytes(project, new int[] { song.Id }, addr, dpcmBankSize, dpcmBaseAddr, machine);
                        for (var j = 0; j < songBytes.Length; j++)
                            nsfBytes[driverBankOffset + j] = songBytes[j];
                        driverBankOffset += songBytes.Length;
                        driverBankLeft -= songBytes.Length;
                        Log.LogMessage(LogSeverity.Info, $"Song '{song.Name}' size: {songBytes.Length} bytes (merged with driver code).");
                    }
                    else
                    {
                        nsfBytes.AddRange(songBytes);
                        Log.LogMessage(LogSeverity.Info, $"Song '{song.Name}' size: {songBytes.Length} bytes.");
                    }

                    var idx = songTableIdx + NsfGlobalVarsSize + i * NsfSongTableEntrySize;
                    nsfBytes[idx + 0] = (byte)(bank);
                    nsfBytes[idx + 1] = (byte)((addr >> 0) & 0xff);
                    nsfBytes[idx + 2] = (byte)((addr >> 8) & 0xff);
                    nsfBytes[idx + 3] = (byte)0;
                }

                // TODO : Check the wiki to see if we need to pad (probably).
                Utils.PadToNextBank(nsfBytes, NsfBankSize);

                Debug.Assert(driverBankLeft >= 0);

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
                        infoChunk->loadAddr = (ushort)(codeBaseAddr);
                        infoChunk->initAddr = (ushort)(codeBaseAddr + NsfInitOffset);
                        infoChunk->playAddr = (ushort)(codeBaseAddr + NsfPlayOffset);
                        infoChunk->palNtscFlags = (byte)machine;
                        infoChunk->extensionFlags = GetNsfExtensionFlags(project.ExpansionAudioMask);
                        infoChunk->numSongs = (byte)project.Songs.Count;
                        infoChunk->startSong = 0;
                        p += sizeof(NsfeInfoChunk);
                        
                        // BANK chunk
                        NsfeChunkHeader* bankHeader = (NsfeChunkHeader*)p;
                        bankHeader->SetId("BANK");
                        bankHeader->size = 8;
                        p += sizeof(NsfeChunkHeader);
                        for (int i = 0, j = 0; i < 8; i++)
                            p[i] = i >= 8 - driverBankCount ? (byte)(j++) : (byte)(i + driverBankCount);
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
                    header.loadAddr = (ushort)(codeBaseAddr);
                    header.initAddr = (ushort)(codeBaseAddr + NsfInitOffset);
                    header.playAddr = (ushort)(codeBaseAddr + NsfPlayOffset);
                    header.playSpeedNTSC = 16639;
                    header.playSpeedPAL = 19997;
                    header.palNtscFlags = (byte)machine;
                    header.extensionFlags = GetNsfExtensionFlags(project.ExpansionAudioMask);

                    for (int i = 0, j = 0; i < 8; i++)
                        header.banks[i] = i >= 8 - driverBankCount ? (byte)(j++) : (byte)(i + driverBankCount);

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

            public int s5bEnvFreq = 0;

            public bool fmTrigger = false;
            public bool fmSustain = false;

            public Instrument instrument = null;
        };

        private IntPtr nsf;
        private Song song;
        private Project project;
        private ChannelState[] channelStates;
        private bool preserveDpcmPadding;
        private readonly int[] DPCMOctaveOrder = new [] { 4, 5, 3, 6, 2, 7, 1, 0 };

        public int GetBestMatchingNote(int period, ushort[] noteTable, out int finePitch)
        {
            int bestNote = -1;
            int minDiff  = 9999999;

            for (int i = 1; i < noteTable.Length; i++)
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

        private Instrument GetDutyInstrument(Channel channel, int duty)
        {
            var expansion = channel.Expansion;
            var expPrefix = expansion == ExpansionType.None || expansion == ExpansionType.Mmc5 ? "" : ExpansionType.InternalNames[expansion] + " ";
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

        private Instrument GetS5BInstrument(int noise, int mixer, bool envEnabled, int envShape)
        {
            Debug.Assert(envShape >= 0 && envShape < 16);

            if (envEnabled)
            {
                if (envShape < 0x4) envShape = 0x9; else 
                if (envShape < 0x8) envShape = 0xf;

                envShape -= 7;
            }
            else
            {
                envShape = 0;
            }

            var toneEnabled  = (mixer & 1) == 0;
            var noiseEnabled = (mixer & 2) == 0;

            var name = "S5B";
            if (toneEnabled)
                name += $" Tone";
            if (noiseEnabled)
                name += $" Noise {noise}";
            if (envShape != 0)
                name += $" Env {envShape + 7:X1}";

            var instrument = project.GetInstrument(name);
            if (instrument == null)
            {
                instrument = project.CreateInstrument(ExpansionType.S5B, name);
                instrument.S5BEnvAutoPitch = false;
                instrument.S5BEnvelopeShape = (byte)envShape;
                instrument.Envelopes[EnvelopeType.S5BNoiseFreq].Length = 1;
                instrument.Envelopes[EnvelopeType.S5BNoiseFreq].Values[0] = (sbyte)noise;
                instrument.Envelopes[EnvelopeType.S5BMixer].Length = 1;
                instrument.Envelopes[EnvelopeType.S5BMixer].Values[0] = (sbyte)mixer;
            }

            return instrument;

        }

        private Instrument GetDPCMInstrument()
        {
            var inst = project.GetInstrument($"DPCM Instrument");

            if (inst == null)
                return project.CreateInstrument(ExpansionType.None, "DPCM Instrument");
            if (inst.SamplesMapping.Count < (Note.MusicalNoteMax - Note.MusicalNoteMin + 1))
                return inst;

            for (int i = 1; ; i++)
            {
                inst = project.GetInstrument($"DPCM Instrument {i}");

                if (inst == null)
                    return project.CreateInstrument(ExpansionType.None, $"DPCM Instrument {i}");
                if (inst.SamplesMapping.Count < (Note.MusicalNoteMax - Note.MusicalNoteMin + 1))
                    return inst;
            }
        }

        private Instrument GetEPSMInstrument(byte chanType, byte[] patchRegs, int noise, int mixer, bool envEnabled, int envShape)
        {
            var name = $"EPSM {Instrument.GetEpsmPatchName(1)}";
            var instrument = project.GetInstrument(name);
            var stereo = "";
            if ((patchRegs[1] & 0xC0) == 0x80)
                stereo = " Left";
            if ((patchRegs[1] & 0xC0) == 0x40)
                stereo = " Right";
            if ((patchRegs[1] & 0xC0) == 0x00 && chanType == 2)
                stereo = " Stop";
            if ((patchRegs[1] & 0xC0) == 0x00 && chanType != 2)
                patchRegs[1] = 0xC0;

            if (chanType == 0)
            {
                if (envEnabled)
                {
                    if (envShape < 0x4) envShape = 0x9; else
                    if (envShape < 0x8) envShape = 0xf;

                    envShape -= 7;
                }
                else
                {
                    envShape = 0;
                }

                var toneEnabled  = (mixer & 1) == 0;
                var noiseEnabled = (mixer & 2) == 0;

                name = "EPSM";
                if (noiseEnabled && noise == 0)
                    noise = 1;
                if (toneEnabled)
                    name += $" Tone";
                if (noiseEnabled)
                    name += $" Noise {noise}";
                if (envShape != 0)
                    name += $" Env {envShape + 7:X1}";

                instrument = project.GetInstrument(name);
                if (instrument == null)
                {
                    instrument = project.CreateInstrument(ExpansionType.EPSM, name);
                    instrument.EPSMSquareEnvAutoPitch = false;
                    instrument.EPSMSquareEnvelopeShape = (byte)envShape;
                    instrument.EpsmPatch = 1;
                    instrument.Envelopes[EnvelopeType.S5BNoiseFreq].Length = 1;
                    instrument.Envelopes[EnvelopeType.S5BNoiseFreq].Values[0] = (sbyte)noise;
                    instrument.Envelopes[EnvelopeType.S5BMixer].Length = 1;
                    instrument.Envelopes[EnvelopeType.S5BMixer].Values[0] = (sbyte)mixer;
                }

                return instrument;
            }

            if (chanType == 2)
            {
                name = $"EPSM Drum{stereo}";
                instrument = project.GetInstrument(name);
                if (instrument == null)
                {
                    instrument = project.CreateInstrument(ExpansionType.EPSM, name);

                    instrument.EpsmPatch = 0;
                    Array.Copy(EpsmInstrumentPatch.Infos[EpsmInstrumentPatch.Default].data, instrument.EpsmPatchRegs, 31);
                    instrument.EpsmPatchRegs[1] = patchRegs[1];
                }
                return instrument;
            }

            if (instrument == null)
            {
                instrument = project.CreateInstrument(ExpansionType.EPSM, name);

                instrument.EpsmPatch = 1;
                if (instrument.EpsmPatchRegs.SequenceEqual(patchRegs))
                    return instrument;
            }

            foreach (var inst in project.Instruments)
            {
                if (inst.IsEpsm)
                {
                    if (inst.EpsmPatchRegs.SequenceEqual(patchRegs))
                        return inst;
                }
            }

            for (int i = 1; ; i++)
            {
                name = $"EPSM Custom{stereo} {i}";
                if (project.IsInstrumentNameUnique(name))
                {
                    instrument = project.CreateInstrument(ExpansionType.EPSM, name);
                    instrument.EpsmPatch = 0;
                    Array.Copy(patchRegs, instrument.EpsmPatchRegs, 31);
                    return instrument;
                }
            }
        }

        private bool UpdateChannel(int p, int n, Channel channel, ChannelState state)
        {
            var project = channel.Song.Project;
            var hasNote = false;
            var invalidPeriodValue = 0;

            if (channel.Type == ChannelType.Dpcm)
            {
                var dmc = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_DPCMCOUNTER, 0);
                var len = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_DPCMSAMPLELENGTH, 0);
                var dmcActive = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_DPCMACTIVE, 0);
                var minSampleLen = preserveDpcmPadding ? 1 : 2;
                var noteTriggeredThisFrame = false;

                if (len >= minSampleLen) 
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
                    var noteValue = -1;
                    var dpcmInst = (Instrument)null;

                    foreach (var inst in project.Instruments)
                    {
                        if (inst.HasAnyMappedSamples)
                        {
                            noteValue = inst.FindDPCMSampleMapping(sample, pitch, loop);
                            if (noteValue >= 0)
                            {
                                dpcmInst = inst;
                                break;
                            }
                        }
                    }

                    if (noteValue < 0)
                    {
                        dpcmInst = GetDPCMInstrument();

                        var found = false;
                        foreach (var o in DPCMOctaveOrder)
                        {
                            for (var i = 0; i < 12; i++)
                            {
                                noteValue = o * 12 + i + 1;
                                if (dpcmInst.GetDPCMMapping(noteValue) == null)
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (found)
                                break;
                        }

                        Debug.Assert(found);
                        dpcmInst.MapDPCMSample(noteValue, sample, pitch, loop);
                    }

                    if (Note.IsMusicalNote(noteValue))
                    {
                        var note = channel.GetOrCreatePattern(p).GetOrCreateNoteAt(n);
                        note.Value = (byte)noteValue;
                        note.Instrument = dpcmInst;
                        if (state.dmc != dmc)
                        {
                            note.DeltaCounter = (byte)dmc;
                            state.dmc = dmc;
                        }
                        hasNote = true;
                        state.state = ChannelState.Triggered;
                        noteTriggeredThisFrame = true;
                    }
                }
                else if (dmc != state.dmc)
                {
                    channel.GetOrCreatePattern(p).GetOrCreateNoteAt(n).DeltaCounter = (byte)dmc;
                    state.dmc = dmc;
                }

                // Some very short sample will enable/disable the DMC channel all within
                // one frame. We'll only stop the note on the next frame. Its best we can 
                // do. (See "Palamedes_SFX.nsfe", first track).
                if (dmcActive == 0 && state.state == ChannelState.Triggered && !noteTriggeredThisFrame)
                {
                    channel.GetOrCreatePattern(p).GetOrCreateNoteAt(n).IsStop = true;
                    state.state = ChannelState.Stopped;
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

                if (channel.IsS5BChannel || channel.IsEPSMSquareChannel)
                {
                    invalidPeriodValue = -1;

                    // We use the NTSC table for S5B and manually add 1, so compensate for that here.
                    if (channel.IsS5BChannel && period > 0)
                    {
                        period -= 1;
                    }
                    else
                    {
                        var envEnabled = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_S5BENVENABLED, 0) != 0;
                        var mixer = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_S5BMIXER, 0);
                        var toneEnabled = (mixer & 1) == 0;

                        // If envelopes are enabled, we may not have a valid period, since the envelope itself may
                        // be making sound, without the tone. In this case, well arbitrarely put the note at C4.
                        if (!toneEnabled && envEnabled)
                        {
                            var noteTable = NesApu.GetNoteTableForChannelType(channel.Type, project.PalMode, project.ExpansionNumN163Channels);
                            period = noteTable[Note.MusicalNoteC4];
                        }
                    }
                }

                var hasOctave  = channel.IsVrc7Channel || channel.IsEPSMFmChannel;
                var hasVolume  = channel.Type != ChannelType.Triangle;
                var hasPitch   = channel.Type != ChannelType.Noise && !channel.IsEPSMRythmChannel;
                var hasDuty    = channel.Type == ChannelType.Square1 || channel.Type == ChannelType.Square2 || channel.Type == ChannelType.Noise || channel.Type == ChannelType.Vrc6Square1 || channel.Type == ChannelType.Vrc6Square2 || channel.Type == ChannelType.Mmc5Square1 || channel.Type == ChannelType.Mmc5Square2;
                var hasTrigger = channel.IsVrc7Channel;

                if (channel.Type >= ChannelType.Vrc7Fm1 && channel.Type <= ChannelType.Vrc7Fm6)
                {
                    var trigger = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FMTRIGGER, 0) != 0;
                    var sustain = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FMSUSTAIN, 0) != 0;
                    var triggerChange = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FMTRIGGERCHANGE, 0);

                    var newState = state.state;

                    if (!state.fmTrigger && trigger)
                        newState = ChannelState.Triggered;
                    else if (state.fmTrigger && !trigger && sustain)
                        newState = ChannelState.Released;
                    else if (!trigger && !sustain)
                        newState = ChannelState.Stopped;

                    if (newState != state.state || triggerChange > 0)
                    {
                        stop    = newState == ChannelState.Stopped;
                        release = newState == ChannelState.Released;
                        state.state = newState;
                        force |= true;
                    }
                    else
                    {
                        attack = false;
                    }

                    octave = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FMOCTAVE, 0);

                    state.fmTrigger = trigger;
                    state.fmSustain = sustain;
                }
                else if (channel.Type >= ChannelType.EPSMFm1 && channel.Type <= ChannelType.EPSMFm6)
                {
                    var trigger = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FMTRIGGER, 0) != 0;
                    var sustain = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FMSUSTAIN, 0) > 0;
                    var stopped = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_VOLUME, 0) == 0;

                    var newState = state.state;

                    if (!trigger)
                        attack = false;
                    
                    newState = sustain ? ChannelState.Triggered : (stopped ? ChannelState.Stopped : ChannelState.Released);

                    if (newState != state.state || trigger)
                    {
                        stop = newState == ChannelState.Stopped;
                        release = newState == ChannelState.Released;
                        state.state = newState;
                        force |= true;
                    }

                    octave = NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FMOCTAVE, 0);

                    state.fmTrigger = trigger;
                    state.fmSustain = sustain;
                }
                else
                {
                    var newState = volume != 0 && (channel.Type == ChannelType.Noise || period != invalidPeriodValue) ? ChannelState.Triggered : ChannelState.Stopped;

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
                        channel.GetOrCreatePattern(p).GetOrCreateNoteAt(n).Volume = (byte)volume;
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
                            regs[i] = (byte)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FMPATCHREG, i);
                    }

                    instrument = GetVrc7Instrument(patch, regs);
                }
                else if (channel.Type >= ChannelType.S5BSquare1 && channel.Type <= ChannelType.S5BSquare3)
                {
                    var noiseFreq  = (byte)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_S5BNOISEFREQUENCY, 0);
                    var mixer      =  (int)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_S5BMIXER, 0);
                    var envEnabled =  (int)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_S5BENVENABLED, 0) != 0;
                    var envShape   =  (int)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_S5BENVSHAPE, 0);
                    var envTrigger =  (int)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_S5BENVTRIGGER, 0);

                    mixer = (mixer & 0x1) + ((mixer & 0x8) >> 2);
                    instrument = GetS5BInstrument(noiseFreq, mixer, envEnabled, envShape);

                    if (envEnabled)
                    {
                        if (envTrigger != 0)
                            force = true;
                        else
                            attack = false;
                    }
                }
                else if (channel.Type >= ChannelType.EPSMSquare1 && channel.Type <= ChannelType.EPSMrythm6)
                {
                    var regs = new byte[31];
                    Array.Clear(regs, 0, regs.Length);
                    if (channel.Type >= ChannelType.EPSMFm1 && channel.Type <= ChannelType.EPSMFm6)
                    {
                        for (int i = 0; i < 31; i++)
                            regs[i] = (byte)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FMPATCHREG, i);

                        instrument = GetEPSMInstrument(1, regs,0,0,false,0);
                    }
                    else if (channel.Type >= ChannelType.EPSMrythm1 && channel.Type <= ChannelType.EPSMrythm6)
                    {
                        regs[1] = (byte)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_STEREO, 0);
                        instrument = GetEPSMInstrument(2, regs,0,0,false,0); 
                    }
                    else
                    {
                        var noiseFreq  = (byte)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_S5BNOISEFREQUENCY, 0);
                        var mixer      =  (int)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_S5BMIXER, 0);
                        var envEnabled =  (int)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_S5BENVENABLED, 0) != 0;
                        var envShape   =  (int)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_S5BENVSHAPE, 0);
                        var envTrigger =  (int)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_S5BENVTRIGGER, 0);

                        mixer = (mixer & 0x1) + ((mixer & 0x8) >> 2);
                        instrument = GetEPSMInstrument(0, regs, noiseFreq, mixer, envEnabled, envShape);
                        if (envEnabled)
                        {
                            if (envTrigger != 0)
                                force = true;
                            else
                                attack = false;
                        }
                    }

                }
                else 
                {
                    instrument = GetDutyInstrument(channel, 0);
                }

                var hasNoteWithAttack = false;

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
                        var pattern = channel.GetOrCreatePattern(p);
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
                        hasNoteWithAttack = newNote.IsMusical && newNote.HasAttack;
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
                            channel.GetOrCreatePattern(p).GetOrCreateNoteAt(n).FinePitch = pitch;
                            state.pitch = pitch;
                        }
                    }

                    state.period = period;
                }

                // Every note with an attack will reset the mod speed/depth to the default (which is zero for FDS). 
                // If there is mod/speed active here, we need to force it again with an effect.
                if (channel.IsFdsChannel)
                {
                    var modDepth =   (byte)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FDSMODULATIONDEPTH, 0);
                    var modSpeed = (ushort)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_FDSMODULATIONSPEED, 0);

                    if (state.fdsModDepth != modDepth || (modDepth != 0 && hasNoteWithAttack))
                    {
                        channel.GetOrCreatePattern(p).GetOrCreateNoteAt(n).FdsModDepth = modDepth;
                        state.fdsModDepth = modDepth;
                    }

                    if (state.fdsModSpeed != modSpeed || (modDepth != 0 && hasNoteWithAttack)) // modDepth is intentional here, if depth = 0, we don't care.
                    {
                        channel.GetOrCreatePattern(p).GetOrCreateNoteAt(n).FdsModSpeed = modSpeed;
                        state.fdsModSpeed = modSpeed;
                    }
                }

                // Same rule applies for S5B and EPSM with manual envelope period, the only difference with FDS is that 
                // envelope period effects apply regardless of which channel they are on.
                if (channel.IsS5BChannel || channel.IsEPSMSquareChannel)
                {
                    var envFreq    = (int)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_S5BENVFREQUENCY, 0);
                    var envEnabled = (int)NotSoFatso.NsfGetState(nsf, channel.Type, NotSoFatso.STATE_S5BENVENABLED, 0) != 0;

                    // All envelope frequency will be on square 1.
                    if (state.s5bEnvFreq != envFreq || hasNoteWithAttack && envEnabled)
                    {
                        var firstChannelType = channel.IsS5BChannel ? ChannelType.S5BSquare1 : ChannelType.EPSMSquare1;
                        var firstChannel = song.GetChannelByType(firstChannelType);
                        firstChannel.GetOrCreatePattern(p).GetOrCreateNoteAt(n).EnvelopePeriod = (ushort)envFreq;
                        state.s5bEnvFreq = envFreq;
                    }
                }
            }

            return hasNote;
        }

        public static string[] GetSongNamesAndDurations(string filename, out int[] durations)
        {
            var nsf = NotSoFatso.NsfOpen(filename);

            if (nsf == IntPtr.Zero)
            {
                durations = null;
                return null;
            }

            var trackCount = NotSoFatso.NsfGetTrackCount(nsf);
            var trackNames = new string[trackCount];

            durations = new int[trackCount];

            for (int i = 0; i < trackCount; i++)
            {
                var name = Utils.PtrToStringAnsi(NotSoFatso.NsfGetTrackName(nsf, i));
                if (string.IsNullOrEmpty(name))
                {
                    trackNames[i] = $"Song {i+1}";
                }
                else
                {
                    trackNames[i] = name;
                }

                durations[i] = Utils.DivideAndRoundUp(NotSoFatso.NsfGetTrackDuration(nsf, i), 1000);
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

            project.Name      = Utils.PtrToStringAnsi(NotSoFatso.NsfGetTitle(nsf));
            project.Author    = Utils.PtrToStringAnsi(NotSoFatso.NsfGetArtist(nsf));
            project.Copyright = Utils.PtrToStringAnsi(NotSoFatso.NsfGetCopyright(nsf));
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

            var numN163Channels = (expansionMask & ExpansionType.N163Mask) != 0 ? GetNumNamcoChannels(filename, songIndex, numFrames) : 1;
            project.SetExpansionAudioMask(expansionMask, numN163Channels);

            var songName = Utils.PtrToStringAnsi(NotSoFatso.NsfGetTrackName(nsf, songIndex));

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
