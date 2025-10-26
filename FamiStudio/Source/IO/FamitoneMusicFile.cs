using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;

namespace FamiStudio
{
    public class FamitoneMusicFile
    {
        private Project project;

        private List<string> lines = new List<string>();

        private string db = ".byte";
        private string dw = ".word";
        private string ll = "@";
        private string llp = "@";
        private string lo = ".lobyte";
        private string hi = ".hibyte";
        private string hexp = "$";
        private string iff = ".if";
        private string endif = ".endif";
        private bool allowDashesInName = true;

        private int machine = MachineType.NTSC;
        private int assemblyFormat = AssemblyFormat.NESASM;
        private Dictionary<byte, string> vibratoEnvelopeNames = new Dictionary<byte, string>();
        private Dictionary<Arpeggio, string> arpeggioEnvelopeNames = new Dictionary<Arpeggio, string>();
        private Dictionary<Instrument, int> instrumentIndices = new Dictionary<Instrument, int>();
        private Dictionary<DPCMSampleMapping, int> sampleMappingIndices = new Dictionary<DPCMSampleMapping, int>();
        private string noArpeggioEnvelopeName;

        private int kernel = FamiToneKernel.FamiStudio;

        private bool log = true;
        private int maxRepeatCount = MaxRepeatCountFT2;

        private const int MaxRepeatCountFT2FS = 63;
        private const int MaxRepeatCountFT2   = 60;

        private const int ExtendedInstrumentStart = 64;

        // Matches "famistudio_opcode_jmp" in assembly.
        private const byte OpcodeFirst                 = 0x40;
        private const byte OpcodeExtendedNote          = 0x40;
        private const byte OpcodeSetReference          = 0x41;
        private const byte OpcodeLoop                  = 0x42;
        private const byte OpcodeDisableAttack         = 0x43;
        private const byte OpcodeEndSong               = 0x44;
        private const byte OpcodeReleaseNote           = 0x45;
        private const byte OpcodeSpeed                 = 0x46; // FamiTracker tempo only
        private const byte OpcodeDelayedNote           = 0x47; // FamiTracker tempo only
        private const byte OpcodeDelayedCut            = 0x48; // FamiTracker tempo only
        private const byte OpcodeSetTempoEnv           = 0x47; // FamiStudio tempo only
        private const byte OpcodeResetTempoEnv         = 0x48; // FamiStudio tempo only
        private const byte OpcodeOverridePitchEnv      = 0x49;
        private const byte OpcodeClearPitchEnvOverride = 0x4a;
        private const byte OpcodeOverridArpEnv         = 0x4b;
        private const byte OpcodeClearArpEnvOverride   = 0x4c;
        private const byte OpcodeResetArpEnv           = 0x4d;
        private const byte OpcodeFinePitch             = 0x4e;
        private const byte OpcodeDutyCycle             = 0x4f;
        private const byte OpcodeSlide                 = 0x50;
        private const byte OpcodeVolumeSlide           = 0x51;
        private const byte OpcodeDeltaCounter          = 0x52;
        private const byte OpcodePhaseReset            = 0x53;
        private const byte OpcodeExtendedInstrument    = 0x54;
        private const byte OpcodeVrc6SawMasterVolume   = 0x55; // VRC6 only
        private const byte OpcodeVrc7ReleaseNote       = 0x56; // VRC7 only
        private const byte OpcodeFdsModSpeed           = 0x57; // FDS only
        private const byte OpcodeFdsModDepth           = 0x58; // FDS only
        private const byte OpcodeFdsReleaseNote        = 0x59; // FDS only
        private const byte OpcodeN163ReleaseNote       = 0x5a; // N163 only
        private const byte OpcodeN163PhaseReset        = 0x5b; // N163 only
        private const byte OpcodeEpsmReleaseNote       = 0x5c; // EPSM only
        private const byte OpcodeEPSMManualEnvPeriod   = 0x5d; // EPSM only
        private const byte OpcodeS5BManualEnvPeriod    = 0x5e; // S5B only

        private const byte OpcodeSetReferenceFT2       = 0xff; // FT2
        private const byte OpcodeLoopFT2               = 0xfd; // FT2
        private const byte OpcodeSpeedFT2              = 0xfb; // FT2

        private const byte OpcodeVolumeBits            = 0x70;

        private const int SingleByteNoteMin = 12;
        private const int SingleByteNoteMax = SingleByteNoteMin + (OpcodeFirst - 1);

        private const int MaxDPMCSampleMappingsBase = 63;
        private const int MaxDPMCSampleMappingsExt  = 255;

        private bool usesFamiTrackerTempo = false;
        private bool usesVolumeTrack = false;
        private bool usesVolumeSlide = false;
        private bool usesPitchTrack = false;
        private bool usesSlideNotes = false;
        private bool usesNoiseSlideNotes = false;
        private bool usesVibrato = false;
        private bool usesArpeggio = false;
        private bool usesDutyCycleEffect = false;
        private bool usesDelayedNotesOrCuts = false;
        private bool usesDeltaCounter = false;
        private bool usesReleaseNotes = false;
        private bool usesPhaseReset = false;
        private bool usesFdsAutoMod = false;
        static readonly int[] epsmRegOrder = new[] { 0, 1, 2, 4, 5, 6, 7, 8, 9, 11, 12, 13, 14, 15, 16, 18, 19, 20, 21, 22, 23, 25, 26, 27, 28, 29, 3, 10, 17, 24, 30 };

        public FamitoneMusicFile(int kernel, bool outputLog)
        {
            this.log = outputLog;
            this.kernel = kernel;
            this.maxRepeatCount = kernel == FamiToneKernel.FamiStudio ? MaxRepeatCountFT2FS : MaxRepeatCountFT2;
        }

        private string GetLocalLabelPrefix(string name)
        {
            string llp = ll;
            if (assemblyFormat == AssemblyFormat.SDAS)
            {
                // For SDAS we have no support for named local labels. So we have to expand
                // the local label prefix with a globally unique name to avoid name collisions.
                llp += $"music_data_{name}_";
            }
            return llp;
        }

        private void CleanupEnvelopes()
        {
            // All instruments must have a volume envelope.
            foreach (var instrument in project.Instruments)
            {
                var env = instrument.Envelopes[EnvelopeType.Volume];
                if (env == null)
                {
                    env = new Envelope(EnvelopeType.Volume);
                    instrument.Envelopes[EnvelopeType.Volume] = env;
                }
                if (env.Length == 0 || env.AllValuesEqual(Note.VolumeMax))
                {
                    env.Length  =  1;
                    env.Loop    = -1;
                    env.Release = -1;
                    env.Values[0] = 15;
                }
            }
        }

        private void GatherDPCMMappings(int dmcExportMode, bool exportUnusedMappings)
        {
            if (project.UsesSamples)
            {
                var maxMappings = kernel == FamiToneKernel.FamiStudio && project.SoundEngineUsesExtendedDpcm ? MaxDPMCSampleMappingsExt : MaxDPMCSampleMappingsBase;

                if (dmcExportMode == DpcmExportMode.Minimum || !exportUnusedMappings)
                {
                    // Gather all unique mappings. Since we don't clean up the mappings to keep stable offsets
                    // between song export, we scan here to find only the ones that are actually used by the song.
                    // TODO : Sort them by number of uses and favor the most commonly used ones for single-byte notes.
                    foreach (var song in project.Songs)
                    {
                        var channel = song.Channels[ChannelType.Dpcm];

                        for (var it = channel.GetSparseNoteIterator(song.StartLocation, song.EndLocation, NoteFilter.Musical); !it.Done; it.Next())
                        {
                            var instrument = it.Note.Instrument;
                            var mapping = instrument.GetDPCMMapping(it.Note.Value);
                            if (mapping != null && !sampleMappingIndices.ContainsKey(mapping))
                            {
                                sampleMappingIndices.Add(mapping, 0);

                                if (sampleMappingIndices.Count >= maxMappings)
                                {
                                    Log.LogMessage(LogSeverity.Error, $"The limit of {maxMappings} unique DPCM sample mappings has been reached. Some samples will not be played correctly.");
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (var inst in project.Instruments)
                    {
                        if (inst.SamplesMapping == null) continue;
                        foreach (var mapping in inst.SamplesMapping.Values)
                        {
                            if (!sampleMappingIndices.TryAdd(mapping, 0))
                            {
                                Log.LogMessage(LogSeverity.Debug, $"Found a duplicate mapping.");
                            }

                            if (sampleMappingIndices.Count >= maxMappings)
                            {
                                Log.LogMessage(LogSeverity.Error, $"The limit of {maxMappings} unique DPCM sample mappings has been reached. Some samples will not be played correctly.");
                                break;
                            }
                        }
                    }
                }
            }
        }

        private int OutputHeader(bool separateSongs)
        {
            var size = 5;
            var name = Utils.MakeNiceAsmName(separateSongs ? project.Songs[0].Name : project.Name, allowDashesInName);
            lines.Add($"; This file is for the {(kernel == FamiToneKernel.FamiTone2 ? "FamiTone2 library" : "FamiStudio Sound Engine")} and was generated by FamiStudio");
            lines.Add("");

            if (assemblyFormat == AssemblyFormat.CA65)
            {
                // For CA65 we add a re-export of the symbol prefixed with _ for the C code to see
                // For some reason though, we can only rexport these symbols either before they are defined,
                // or after all of the data is completely written.
                lines.Add($".export _music_data_{name}:=music_data_{name}");
                lines.Add("");
            }

            lines.Add($"music_data_{name}:");
            lines.Add($"\t{db} {project.Songs.Count}");
            lines.Add($"\t{dw} {llp}instruments");

            if (assemblyFormat == AssemblyFormat.SDAS)
            {
                // For SDAS we add a re-export of the symbol prefixed with _ for the C code to see
                // For some reason we can only do this after the symbol is defined.
                // Doing it earlier will compile without errors, but results in an incorrect address.
                lines.Add($"_music_data_{name}=music_data_{name}");
                lines.Add($".globl _music_data_{name}");
                lines.Add("");
            }

            if (project.UsesFdsExpansion || project.UsesN163Expansion || project.UsesVrc7Expansion || project.UsesEPSMExpansion || project.UsesS5BExpansion)
            {
                lines.Add($"\t{dw} {llp}instruments_exp");
                size += 2;
            }

            lines.Add($"\t{dw} {llp}samples-{(kernel == FamiToneKernel.FamiTone2 ? 3 : (project.SoundEngineUsesDpcmBankSwitching ? 5 : 4))}");

            for (int i = 0; i < project.Songs.Count; i++)
            {
                var song = project.Songs[i];

                lines.Add($"; {i:x2} : {song.Name}");

                for (int chn = 0; chn < song.Channels.Length; ++chn)
                {
                    var channel = song.Channels[chn];
                    if (channel.IsEPSMSquareChannel)
                    {
                        var channel_count = channel.Type - ChannelType.EPSMSquare1;
                        lines.Add($"\t{iff} FAMISTUDIO_EXP_EPSM_SSG_CHN_CNT > {channel_count}");
                    }
                    else if (channel.IsEPSMFmChannel)
                    {
                        var channel_count = channel.Type - ChannelType.EPSMFm1;
                        lines.Add($"\t{iff} FAMISTUDIO_EXP_EPSM_FM_CHN_CNT > {channel_count}");
                    }
                    else if (channel.IsEPSMRythmChannel)
                    {
                        var channel_id = channel.Type - ChannelType.EPSMrythm1 + 1;
                        lines.Add($"\t{iff} FAMISTUDIO_EXP_EPSM_RHYTHM_CHN{channel_id}_ENABLE");
                    }
                    lines.Add($"\t{dw} {llp}song{i}ch{chn}");
                    if (channel.IsEPSMChannel)
                    {
                        lines.Add($"\t{endif}");
                    }
                }

                if (song.UsesFamiTrackerTempo)
                {
                    int tempoPal  = 256 * song.FamitrackerTempo / (50 * 60 / 24);
                    int tempoNtsc = 256 * song.FamitrackerTempo / (60 * 60 / 24);

                    lines.Add($"\t{dw} {tempoPal},{tempoNtsc}");

                    usesFamiTrackerTempo = true;
                }
                else
                {
                    var grooveName = GetGrooveAsmName(song.Groove, song.GroovePaddingMode);
                    lines.Add($"\t{db} {lo}({llp}tempo_env_{grooveName}), {hi}({llp}tempo_env_{grooveName}), {(project.PalMode ? 2 : 0)}, 0"); 
                }

                size += song.Channels.Length * 2 + 4;
            }

            lines.Add("");

            if (assemblyFormat == AssemblyFormat.CA65)
            {
                lines.Add($".export music_data_{name}");
                lines.Add($".global FAMISTUDIO_DPCM_PTR");
                lines.Add("");
            }
            else if(assemblyFormat == AssemblyFormat.SDAS)
            {
                lines.Add($".globl music_data_{name}");
                lines.Add("");
            }

            return size;
        }

        private Envelope ProcessMixerEnvelope(Envelope env)
        {
            Envelope ymenv = env;
            for (int j = 0; j < ymenv.Length; j++)
            {
                ymenv.Values[j] = (sbyte)((((byte)ymenv.Values[j] & 0x1)) | (((byte)ymenv.Values[j] & 0x2) << 2));
            }

            return ymenv;
        }

        private byte[] ProcessEnvelope(Envelope env, bool allowReleases, bool newPitchEnvelope)
        {
            // HACK : Pass dummy type here, volume envelopes have been taken care of already.
            if (env.IsEmpty(EnvelopeType.Count))
                return null;

            env.Optimize();

            // Special case for envelopes with a single value (like duty often are).
            // Make them 127 in length so that they update less often.
            if (env.Length == 1 && !env.Relative && env.Release < 0)
            {
                if (newPitchEnvelope || allowReleases)
                    return new byte[] { 0x00, (byte)(192 + env.Values[0]), 0x7f, 0x00, 0x02 };
                else
                    return new byte[] { (byte)(192 + env.Values[0]), 0x7f, 0x00, 0x00 } ;
            }

            var data = new byte[256];

            byte ptr = (byte)(allowReleases || newPitchEnvelope ? 1 : 0);
            byte ptrLoop = 0xff;
            byte relCount = 0;
            byte prevVal = (byte)(env.Values[0] + 1);//prevent rle match
            bool foundRelease = false;
            bool allowRle = !env.Relative;

            if (newPitchEnvelope)
                data[0] = (byte)(env.Relative ? 0x80 : 0x00);

            for (int j = 0; j < env.Length; j++)
            {
                byte val;

                if (env.Values[j] < -64)
                    val = unchecked((byte)-64);
                else if (env.Values[j] > 63)
                    val = 63;
                else
                    val = (byte)env.Values[j];

                val += 192;

                if (prevVal != val || !allowRle || j == env.Loop || (allowReleases && j == env.Release) || j == env.Length - 1 || (relCount == 127 && newPitchEnvelope))
                {
                    if (relCount != 0)
                    {
                        if (relCount == 1)
                        {
                            data[ptr++] = prevVal;
                        }
                        else
                        {
                            while (relCount > 127)
                            {
                                data[ptr++] = 127;
                                relCount -= 127;
                            }

                            data[ptr++] = relCount;
                        }

                        relCount = 0;
                    }

                    if (j == env.Loop) ptrLoop = ptr;

                    if (j == env.Release && allowReleases)
                    {
                        // A release implies the end of the loop.
                        Debug.Assert(ptrLoop != 0xff && data[ptrLoop] >= 128); // Cant be jumping back to the middle of RLE.
                        foundRelease = true;
                        data[ptr++] = 0;
                        data[ptr++] = ptrLoop;
                        data[0] = ptr;
                    }

                    data[ptr++] = val;

                    prevVal = val;
                }
                else
                {
                    ++relCount;
                }
            }

            if (ptrLoop == 0xff || foundRelease)
            {
                // Non-looping relative pitch envelopes need to end with a zero so they stop doing anything.
                if (newPitchEnvelope && env.Relative && data[ptr - 1] != 192)
                    data[ptr++] = 192;

                ptrLoop = (byte)(ptr - 1);
            }
            else
            {
                Debug.Assert(data[ptrLoop] >= 128); // Cant be jumping back to the middle of RLE.
            }

            data[ptr++] = 0;
            data[ptr++] = ptrLoop;

            Array.Resize(ref data, ptr);

            return data;
        }

        private int DPCMMappingSort(DPCMSampleMapping m1, DPCMSampleMapping m2)
        {
            // Sort by name, then pitch.
            var name = m1.Sample.Name.CompareTo(m2.Sample.Name);
            if (name != 0)
                return name;
            return m1.Pitch.CompareTo(m2.Pitch);
        }

        private int OutputInstruments()
        {
            // Process all envelope, make unique, etc.
            var uniqueEnvelopes = new SortedList<uint, byte[]>();
            var instrumentEnvelopes = new Dictionary<Envelope, uint>();
            var instrumentWaveforms = new Dictionary<Instrument, uint[]>();

            var defaultEnv = new byte[] { 0xc0, 0x7f, 0x00, 0x01 };
            var defaultDutyEnv = new byte[] { 0x7f, 0x00, 0x00 }; // This is a "do nothing" envelope, simply loops, never sets a value.
            var defaultPitchOrReleaseEnv = new byte[] { 0x00, 0xc0, 0x7f, 0x00, 0x02 }; // For pitch, first byte means absolute, for other envelope, it means no release.
            var defaultEnvCRC = CRC32.Compute(defaultEnv);
            var defaultDutyEnvCRC = CRC32.Compute(defaultDutyEnv);
            var defaultPitchOrReleaseEnvCRC = CRC32.Compute(defaultPitchOrReleaseEnv);
            var defaultEnvName = "";
            var defaultPitchEnvName = "";
            var vibToCrc = new Dictionary<byte, uint>();
            var arpToCrc = new Dictionary<Arpeggio, uint>();

            project.AutoAssignN163WavePositions(out var n163AutoWavPosMap);

            uniqueEnvelopes.Add(defaultEnvCRC, defaultEnv);

            if (kernel == FamiToneKernel.FamiStudio)
            {
                uniqueEnvelopes.Add(defaultPitchOrReleaseEnvCRC, defaultPitchOrReleaseEnv);
                uniqueEnvelopes.Add(defaultDutyEnvCRC, defaultDutyEnv);
            }

            Action<Envelope, byte[]> AddProcessedEnvelope = (env, processed) => 
            {
                uint crc = CRC32.Compute(processed);
                uniqueEnvelopes[crc] = processed;
                instrumentEnvelopes[env] = crc;
            };

            foreach (var instrument in project.Instruments)
            {
                for (int i = 0; i < EnvelopeType.Count; i++)
                {
                    var env = instrument.Envelopes[i];

                    if (env == null)
                        continue;

                    if (kernel != FamiToneKernel.FamiStudio && i == EnvelopeType.DutyCycle)
                        continue;

                    byte[] processed = null;

                    switch (i)
                    {
                        case EnvelopeType.N163Waveform:
                        case EnvelopeType.FdsWaveform:
                        case EnvelopeType.WaveformRepeat:
                            // Handled as special case below since multiple-waveform must be splitted and
                            // repeat envelope must be converted.
                            break;
                        case EnvelopeType.FdsModulation:
                            processed = env.BuildFdsModulationTable().Select(m => (byte)m).ToArray();
                            break;
                        case EnvelopeType.S5BMixer:
                            processed = ProcessEnvelope(ProcessMixerEnvelope(env), false, false);
                            break;
                        default:
                            processed = ProcessEnvelope(env,
                                i == EnvelopeType.Volume && kernel == FamiToneKernel.FamiStudio,
                                i == EnvelopeType.Pitch  && kernel == FamiToneKernel.FamiStudio);
                            break;
                    }

                    if (processed == null)
                    {
                        if (kernel == FamiToneKernel.FamiStudio && (i == EnvelopeType.Pitch || i == EnvelopeType.Volume))
                            instrumentEnvelopes[env] = defaultPitchOrReleaseEnvCRC;
                        else if (kernel == FamiToneKernel.FamiStudio && i == EnvelopeType.DutyCycle)
                            instrumentEnvelopes[env] = defaultDutyEnvCRC;
                        else if (kernel == FamiToneKernel.FamiStudio && i == EnvelopeType.Volume)
                            instrumentEnvelopes[env] = defaultPitchOrReleaseEnvCRC;
                        else
                            instrumentEnvelopes[env] = defaultEnvCRC;
                    }
                    else
                    {
                        AddProcessedEnvelope(env, processed);
                    }
                }

                // Special case for N163/FDS multiple waveforms.
                if (instrument.IsN163 || instrument.IsFds)
                {
                    var envType = instrument.IsN163 ? EnvelopeType.N163Waveform : EnvelopeType.FdsWaveform;
                    var envRepeat = instrument.Envelopes[EnvelopeType.WaveformRepeat];

                    instrument.BuildWaveformsAndWaveIndexEnvelope(out var subWaveforms, out var waveIndexEnvelope, true);
                    var processedWaveIndexEnvelope = ProcessEnvelope(waveIndexEnvelope, true, false);

                    if (processedWaveIndexEnvelope != null)
                        AddProcessedEnvelope(envRepeat, processedWaveIndexEnvelope);
                    else
                        instrumentEnvelopes[envRepeat] = defaultPitchOrReleaseEnvCRC;

                    var waveforms = new uint[subWaveforms.GetLength(0)];

                    for (int i = 0; i < subWaveforms.GetLength(0); i++)
                    {
                        var wav = subWaveforms[i];
                        var crc = CRC32.Compute(wav);
                        uniqueEnvelopes[crc] = wav;
                        waveforms[i] = crc;
                    }

                    instrumentWaveforms[instrument] = waveforms;
                }
            }

            if (kernel == FamiToneKernel.FamiStudio)
            {
                // Write the arpeggio envelopes.
                foreach (var arpeggio in project.Arpeggios)
                {
                    var processed = ProcessEnvelope(arpeggio.Envelope, false, false);
                    uint crc = CRC32.Compute(processed);
                    arpToCrc[arpeggio] = crc;
                    uniqueEnvelopes[crc] = processed;
                }

                // Create all the unique vibrato envelopes.
                var uniqueVibratos = new HashSet<byte>();

                foreach (var s in project.Songs)
                {
                    foreach (var c in s.Channels)
                    {
                        foreach (var p in c.Patterns)
                        {
                            foreach (var note in p.Notes.Values)
                            {
                                if (note.HasVibrato)
                                {
                                    uniqueVibratos.Add(note.RawVibrato);
                                }
                            }
                        }
                    }
                }

                foreach (var vib in uniqueVibratos)
                {
                    // TODO : Create a pack/unpack function. We dont need to know how its encoded here.
                    var depth = (vib >> 0) & 0xf;
                    var speed = (vib >> 4) & 0xf;

                    if (speed != 0 && depth != 0)
                    {
                        var env = Envelope.CreateVibratoEnvelope(speed, depth);
                        var processed = ProcessEnvelope(env, false, true);
                        uint crc = CRC32.Compute(processed);

                        vibToCrc.Add(vib, crc);

                        if (!uniqueEnvelopes.ContainsKey(crc))
                        {
                            uniqueEnvelopes[crc] = processed;
                        }
                        else
                        {
                            Debug.Assert(Utils.CompareArrays(uniqueEnvelopes[crc], processed) == 0);
                        }
                    }
                }
            }

            var size = 0;

            // Write instruments
            lines.Add($"{llp}instruments:");

            var instrumentCount = 0;
            var instrumentLimit = project.SoundEngineUsesExtendedInstruments ? 256 : 64;

            for (int i = 0; i < project.Instruments.Count; i++)
            {
                var instrument = project.Instruments[i];

                if (!project.IsInstrumentUsedByOtherChannelThanDPCM(instrument))
                {
                    continue;
                }

                if (!instrument.IsFds  && 
                    !instrument.IsN163 &&
                    !instrument.IsVrc7 &&
                    !instrument.IsS5B  &&
                    !instrument.IsEpsm)
                {
                    var volumeEnvIdx   = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.Volume]]);
                    var arpeggioEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.Arpeggio]]);
                    var pitchEnvIdx    = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.Pitch]]);

                    if (kernel == FamiToneKernel.FamiStudio)
                    {
                        var dutyEnvIdx = instrument.IsEnvelopeActive(EnvelopeType.DutyCycle) ? uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.DutyCycle]]) : uniqueEnvelopes.IndexOfKey(defaultEnvCRC);

                        lines.Add($"\t{dw} {llp}env{volumeEnvIdx},{llp}env{arpeggioEnvIdx},{llp}env{dutyEnvIdx},{llp}env{pitchEnvIdx} ; {instrumentCount:x2} : {instrument.Name}");
                    }
                    else
                    {
                        var duty = instrument.IsEnvelopeActive(EnvelopeType.DutyCycle) ? instrument.Envelopes[EnvelopeType.DutyCycle].Values[0] : 0;
                        var dutyShift = instrument.IsRegular ? 6    : 4;
                        var dutyBits  = instrument.IsRegular ? 0x30 : 0;

                        lines.Add($"\t{db} {hexp}{(duty << dutyShift) | dutyBits:x2} ; {instrumentCount:x2} : {instrument.Name}");
                        lines.Add($"\t{dw} {llp}env{volumeEnvIdx}, {llp}env{arpeggioEnvIdx}, {llp}env{pitchEnvIdx}");
                        lines.Add($"\t{db} {hexp}00");
                    }

                    size += 8;
                    instrumentIndices[instrument] = instrumentCount++;
                }
            }

            if (instrumentCount > instrumentLimit)
                Log.LogMessage(LogSeverity.Error, $"The amount of instruments ({instrumentCount}) exceeds the limit of {instrumentLimit}, songs will not sound correct.");

            lines.Add("");

            // FDS, N163 and VRC7 instruments are special.
            if (project.UsesFdsExpansion  || 
                project.UsesN163Expansion || 
                project.UsesVrc7Expansion ||
                project.UsesS5BExpansion  ||
                project.UsesEPSMExpansion)
            {
                lines.Add($"{llp}instruments_exp:");

                var instrumentCountExp = 0;
                var instrumentLimitExp = project.SoundEngineUsesExtendedInstruments ? 256 : 32;

                for (int i = 0; i < project.Instruments.Count; i++)
                {
                    var instrument = project.Instruments[i];

                    if (instrument.IsFds  || 
                        instrument.IsVrc7 ||
                        instrument.IsN163 ||
                        instrument.IsS5B  ||
                        instrument.IsEpsm)
                    {
                        var volumeEnvIdx   = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.Volume]]);
                        var arpeggioEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.Arpeggio]]);
                        var pitchEnvIdx    = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.Pitch]]);

                        lines.Add($"\t{dw} {llp}env{volumeEnvIdx}, {llp}env{arpeggioEnvIdx}, {llp}env{pitchEnvIdx} ; {instrumentCountExp:x2} : {instrument.Name}");

                        if (instrument.IsFds)
                        {
                            var repeatEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.WaveformRepeat]]);
                            var fdsModEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.FdsModulation]]);

                            lines.Add($"\t{dw} {llp}env{repeatEnvIdx}");
                            lines.Add($"\t{db} {(instrument.FdsModDepth << 2) | instrument.FdsMasterVolume}");
                            lines.Add($"\t{dw} {llp}fds_inst{instrumentCountExp}_waves");
                            lines.Add($"\t{dw} {llp}env{fdsModEnvIdx}");

                            if (instrument.FdsAutoMod)
                            {
                                var numer = (int)instrument.FdsAutoModNumer;
                                var denom = (int)instrument.FdsAutoModDenom;
                                Utils.SimplifyFraction(ref numer, ref denom); // 2/4 is same as 1/2
                                
                                // Set bit 7 of numer for automod enabled
                                lines.Add($"\t{db} {0x80 | numer}, {denom}");
                                usesFdsAutoMod = true;
                            }
                            else
                            {
                                // Bit 7 of the first byte will be clear here (no automod)
                                lines.Add($"\t{dw} {instrument.FdsModSpeed}");
                            }

                            lines.Add($"\t{db} {instrument.FdsModDelay}");
                        }
                        else if (instrument.IsN163)
                        {
                            var repeatEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.WaveformRepeat]]);
                            var wavePos = (int)instrument.N163WavePos;

                            if (instrument.N163WaveAutoPos)
                                n163AutoWavPosMap.TryGetValue(instrument.Id, out wavePos);

                            lines.Add($"\t{dw} {llp}env{repeatEnvIdx}");
                            lines.Add($"\t{db} {hexp}{wavePos:x2}, {hexp}{instrument.N163WaveSize:x2}");
                            lines.Add($"\t{dw} {llp}n163_inst{instrumentCountExp}_waves");
                            lines.Add($"\t{db} {hexp}00, {hexp}00, {hexp}00, {hexp}00");
                        }
                        else if (instrument.IsS5B)
                        {
                            var noiseEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.S5BNoiseFreq]]);
                            var mixerEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.S5BMixer]]);
                            var envShape = instrument.S5BEnvelopeShape > 0 ? instrument.S5BEnvelopeShape + 7 : 0;
                            var envAutoOctave = instrument.S5BEnvAutoPitch ? instrument.S5BEnvAutoPitchOctave : 0x80; // 0x80 = special code that means "manual"
                            var envManualPeriod = instrument.S5BEnvelopePitch;

                            lines.Add($"\t{dw} {llp}env{mixerEnvIdx}, {llp}env{noiseEnvIdx}");
                            lines.Add($"\t{db} {hexp}{envShape:x2}, {hexp}{envAutoOctave:x2}");
                            lines.Add($"\t{dw} {envManualPeriod}");
                            lines.Add($"\t{db} {hexp}00, {hexp}00"); 
                        }
                        else if (instrument.IsVrc7)
                        {
                            lines.Add($"\t{db} {hexp}{(instrument.Vrc7Patch << 4):x2}, {hexp}00");
                            lines.Add($"\t{db} {String.Join(",", instrument.Vrc7PatchRegs.Select(r => $"{hexp}{r:x2}"))}");
                        }
                        else if (instrument.IsEpsm)
                        {
                            var noiseEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.S5BNoiseFreq]]);
                            var mixerEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.S5BMixer]]);
                            var envShape = instrument.EPSMSquareEnvelopeShape > 0 ? instrument.EPSMSquareEnvelopeShape + 7 : 0;
                            var envAutoOctave = instrument.EPSMSquareEnvAutoPitch ? instrument.EPSMSquareEnvAutoPitchOctave : 0x80; // 0x80 = special code that means "manual"
                            var envManualPeriod = instrument.EPSMSquareEnvelopePitch; 

                            byte[] epsmPatchRegsReordered = new byte[epsmRegOrder.Length];
                            for (int reg = 0; reg < epsmRegOrder.Length; reg++)
                            {
                                epsmPatchRegsReordered[reg] = instrument.EpsmPatchRegs[epsmRegOrder[reg]];
                            }
                            lines.Add($"\t{dw} {llp}env{mixerEnvIdx}, {llp}env{noiseEnvIdx}");
                            lines.Add($"\t{db} {hexp}{envShape:x2}, {hexp}{envAutoOctave:x2}");
                            lines.Add($"\t{dw} {envManualPeriod}");
                            lines.Add($"\t{dw} {llp}instrument_epsm_extra_patch{i}");
                        }

                        size += 16;
                        instrumentIndices[instrument] = instrumentCountExp++;
                    }
                }

                if (instrumentCountExp > instrumentLimitExp)
                    Log.LogMessage(LogSeverity.Error, $"Number of expansion instruments ({instrumentCountExp}) exceeds the limit of {instrumentLimitExp}, songs will not sound correct.");

                lines.Add("");
            }

            // EPSM instruments don't fit in the 16 bytes allotted for expansion instruments so we store the extra data
            // for them after all the instrument data
            if (project.UsesEPSMExpansion)
            {
                for (int i = 0; i < project.Instruments.Count; i++)
                {
                    var instrument = project.Instruments[i];
                    if (instrument.IsEpsm)
                    {
                        byte[] epsmPatchRegsReordered = new byte[epsmRegOrder.Length];
                        for (int reg = 0; reg < epsmRegOrder.Length; reg++)
                        {
                            epsmPatchRegsReordered[reg] = instrument.EpsmPatchRegs[epsmRegOrder[reg]];
                        }
                        lines.Add($"{llp}instrument_epsm_extra_patch{i}:");
                        lines.Add($"\t{db} {String.Join(",", epsmPatchRegsReordered.Select(r => $"{hexp}{r:x2}"))}");
                        size += 31;
                    }
                }
                lines.Add("");
            }

            // Write envelopes.
            int idx = 0;
            foreach (var kv in uniqueEnvelopes)
            {
                var labelname = $"{llp}env{idx++}";
                lines.Add($"{labelname}:");
                lines.Add($"\t{db} {String.Join(",", kv.Value.Select(i => $"{hexp}{i:x2}"))}");

                if (kv.Key == defaultEnvCRC)
                    defaultEnvName = labelname;
                if (kv.Key == defaultPitchOrReleaseEnvCRC)
                    defaultPitchEnvName = labelname;

                size += kv.Value.Length;
            }

            lines.Add("");

            // Write the N163/FDS multiple waveforms.
            if (project.UsesN163Expansion || project.UsesFdsExpansion)
            {
                foreach (var instrument in project.Instruments)
                {
                    if (instrument.IsN163 || instrument.IsFds)
                    {
                        var name = instrument.IsN163 ? "n163" : "fds";
                        
                        lines.Add($"{ll}{name}_inst{instrumentIndices[instrument]}_waves:");

                        var waves = instrumentWaveforms[instrument];
                        for (int i = 0; i < waves.Length; i++)
                        {
                            var waveIdx = uniqueEnvelopes.IndexOfKey(waves[i]);
                            lines.Add($"\t{dw} {ll}env{waveIdx}");
                        }

                        size += waves.Length * 2;
                        lines.Add("");
                    }
                }
            }

            noArpeggioEnvelopeName = defaultEnvName;

            // Setup arpeggio envelopes.
            foreach (var kv in arpToCrc)
            {
                arpeggioEnvelopeNames[kv.Key] = $"{llp}env{uniqueEnvelopes.IndexOfKey(kv.Value)}";
            }

            // Setup the vibrato envelopes.
            vibratoEnvelopeNames[0] = defaultPitchEnvName;

            foreach (var kv in vibToCrc)
            {
                vibratoEnvelopeNames[kv.Key] = $"{llp}env{uniqueEnvelopes.IndexOfKey(kv.Value)}";
            }

            return size;
        }

        private int OutputDPCMMappings()
        {
            var size = 0;
            // Write samples.
            lines.Add($"{llp}samples:");

            if (project.UsesSamples)
            {
                var sortedUniqueMappings = sampleMappingIndices.Keys.ToArray();
                Array.Sort(sortedUniqueMappings, DPCMMappingSort);

                var noteValue = kernel == FamiToneKernel.FamiStudio ? SingleByteNoteMin + 1 : 1;

                for (int i = 0; i < sortedUniqueMappings.Length; i++)
                {
                    var mapping = sortedUniqueMappings[i];
                    var offset = project.GetSampleBankOffset(mapping.Sample);

                    var sampleOffset = Math.Max(0, offset) >> 6;
                    var sampleSize = mapping.Sample.ProcessedData.Length >> 4;
                    var sampleInitialDmcValue = mapping.OverrideDmcInitialValue ? mapping.DmcInitialValueDiv2 * 2 : mapping.Sample.DmcInitialValueDiv2 * 2;
                    var samplePitchAndLoop = mapping.Pitch | ((mapping.Loop ? 1 : 0) << 6);
                    var sampleBank = mapping.Sample.Bank;

                    if (kernel == FamiToneKernel.FamiStudio)
                    {
                        if (project.SoundEngineUsesDpcmBankSwitching)
                        {
                            size += 5;
                            lines.Add($"\t{db} {hexp}{sampleOffset:x2}+{lo}(FAMISTUDIO_DPCM_PTR),{hexp}{sampleSize:x2},{hexp}{samplePitchAndLoop:x2},{hexp}{sampleInitialDmcValue:x2},{hexp}{sampleBank:x2} ; {i:x2} {mapping.Sample.Name} (Pitch:{mapping.Pitch})");
                        }
                        else
                        {
                            size += 4;
                            lines.Add($"\t{db} {hexp}{sampleOffset:x2}+{lo}(FAMISTUDIO_DPCM_PTR),{hexp}{sampleSize:x2},{hexp}{samplePitchAndLoop:x2},{hexp}{sampleInitialDmcValue:x2} ; {i:x2} {mapping.Sample.Name} (Pitch:{mapping.Pitch})");
                        }
                    }
                    else
                    {
                        size += 3;
                        lines.Add($"\t{db} {hexp}{sampleOffset:x2}+{lo}(FT_DPCM_PTR),{hexp}{sampleSize:x2},{hexp}{samplePitchAndLoop:x2}\t;{i} {mapping.Sample.Name} (Pitch:{mapping.Pitch})");
                    }

                    sampleMappingIndices[mapping] = noteValue & 0xff;
                    noteValue++;
                }
            }

            lines.Add("");

            return size;
        }

        private string GetGrooveAsmName(int[] groove, int paddingMode)
        {
            var name = string.Join("_", groove);

            switch (paddingMode)
            {
                case GroovePaddingType.Beginning: name += "_beg"; break;
                case GroovePaddingType.Middle:    name += "_mid"; break;
                case GroovePaddingType.End:       name += "_end"; break;
            }

            return name;
        }

        private int OutputTempoEnvelopes()
        {
            var size = 0;
            if (project.UsesFamiStudioTempo)
            {
                var uniqueGrooves = new List<Tuple<int[],int>>();

                foreach (var song in project.Songs)
                {
                    var existintIndex = uniqueGrooves.FindIndex(g => Utils.CompareArrays(g.Item1, song.Groove) == 0 && g.Item2 == song.GroovePaddingMode);
                    if (existintIndex < 0)
                        uniqueGrooves.Add(new Tuple<int[], int>(song.Groove, song.GroovePaddingMode));

                    for (int p = 0; p < song.Length; p++)
                    {
                        if (song.PatternHasCustomSettings(p))
                        {
                            existintIndex = uniqueGrooves.FindIndex(g => Utils.CompareArrays(g.Item1, song.GetPatternGroove(p)) == 0 && g.Item2 == song.GetPatternGroovePaddingMode(p));
                            if (existintIndex < 0)
                                uniqueGrooves.Add(new Tuple<int[], int>(song.GetPatternGroove(p), song.GetPatternGroovePaddingMode(p)));
                        }
                    }
                }

                foreach (var groove in uniqueGrooves)
                {
                    var env = (byte[])FamiStudioTempoUtils.GetTempoEnvelope(groove.Item1, groove.Item2, project.PalMode);

                    lines.Add($"{llp}tempo_env_{GetGrooveAsmName(groove.Item1, groove.Item2)}:");
                    lines.Add($"\t{db} {String.Join(",", env.Select(i => $"{hexp}{i:x2}"))}");

                    size += env.Length;
                }

                lines.Add("");
            }

            return size;
        }

        private int[] OutputDPCMSamples(string filename, string baseDmcFilename, int maxBankSize)
        {
            var bankSizes = new List<int>();

            if (project.UsesSamples)
            {
                if (project.SoundEngineUsesDpcmBankSwitching)
                {
                    Log.LogMessage(LogSeverity.Info, "Project uses DPCM bank switching, separate DMC files will be generated for each bank.");
                }

                var maxBank = project.SoundEngineUsesDpcmBankSwitching ? Project.MaxDPCMBanks : 1;
                var path = Path.GetDirectoryName(filename);
                var projectname = Utils.MakeNiceAsmName(project.Name, allowDashesInName);

                if (baseDmcFilename == null)
                    baseDmcFilename = Path.Combine(path, projectname + ".dmc");

                for (var i = 0; i < maxBank; i++)
                {
                    var sampleData = project.GetPackedSampleData(i, maxBankSize);

                    if (sampleData.Length == 0)
                        continue;

                    var dmcFilename = project.SoundEngineUsesDpcmBankSwitching ? Utils.AddFileSuffix(baseDmcFilename, $"_bank{i}") : baseDmcFilename;
                    File.WriteAllBytes(dmcFilename, sampleData);
                    bankSizes.Add(sampleData.Length);
                }
            }

            return bankSizes.ToArray();
        }

        private int FindEffectParam(Song song, int patternIdx, int noteIdx, int effect)
        {
            foreach (var channel in song.Channels)
            {
                var pattern = channel.PatternInstances[patternIdx];

                if (pattern != null && 
                    pattern.Notes.TryGetValue(noteIdx, out var note) && 
                    note.HasValidEffectValue(effect))
                {
                    return note.GetEffectValue(effect);
                }
            }

            return -1;
        }

        // If we were using a custom VRC7 patch, but another channel uses one too, 
        // we will need to reload our instrument next time we play a note.
        private bool OtherVrc7ChannelUsesCustomPatch(Song song, Channel channel, Instrument instrument, int patternIdx, int noteIdx)
        {
            if (project.UsesVrc7Expansion && 
                channel.IsVrc7Channel &&
                instrument != null &&
                instrument.IsVrc7 &&
                instrument.Vrc7Patch == 0)
            {
                foreach (var c in song.Channels)
                {
                    if (c != channel && c.IsVrc7Channel)
                    {
                        var pattern = c.PatternInstances[patternIdx];

                        if (pattern != null && pattern.Notes.TryGetValue(noteIdx, out Note note) &&
                            note != null &&
                            note.Instrument != null &&
                            note.Instrument != instrument &&
                            note.Instrument.IsVrc7 &&
                            note.Instrument.Vrc7Patch == 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool Square1HasEnvelopePeriodEffect(Song song, Channel channel, int patternIdx, int noteIdx)
        {
            if (channel.IsS5BChannel || channel.IsEPSMSquareChannel)
            {
                var sq1Note = song.GetChannelByType(channel.IsS5BChannel ? ChannelType.S5BSquare1 : ChannelType.EPSMSquare1).GetNoteAt(new NoteLocation(patternIdx, noteIdx));
                if (sq1Note != null && sq1Note.HasEnvelopePeriod)
                {
                    return true;
                }
            }

            return false;
        }

        private int FindEffectParam(Song song, int effect)
        {
            for (int p = 0; p < song.Length; p++)
            {
                for (int i = 0; i < song.GetPatternLength(p); i++)
                {
                    int fx = FindEffectParam(song, p, i, effect);
                    if (fx >= 0)
                    {
                        return fx;
                    }
                }
            }

            return -1;
        }

        private byte EncodeNoteValue(int channel, int value, bool singleByte = false, int numNotes = 0)
        {
            if (kernel != FamiToneKernel.FamiStudio)
            {
                // 0 = stop, 1 = C-1 ... 63 = D-6
                if (value != 0 && channel != ChannelType.Noise && channel != ChannelType.Dpcm) value = Math.Max(1, value - 12); 
                return (byte)(((value & 63) << 1) | numNotes);
            }
            else
            {
                // 0 = stop, 1 = C0 ... 96 = B7
                if (value != 0)
                {
                    Debug.Assert(Note.IsMusicalNote(value));

                    value = Utils.Clamp(value, 1, 96);

                    if (singleByte)
                    {
                        Debug.Assert(value > SingleByteNoteMin && value <= SingleByteNoteMax);
                        value -= SingleByteNoteMin;
                    }
                }

                return (byte)(value);
            }
        }

        [Flags]
        enum OverrideFlags
        {
            None = 0,
            Vibrato = 1,
            Arpeggio = 2,
            FdsModSpeed = 4,
            FdsModDepth = 8,
            EnvPeriod = 16
        }

        private List<string> GetSongData(Song song, int songIdx, int speedChannel)
        {
            var emptyPattern = new Pattern(-1, song, 0, "");
            var emptyNote = new Note(Note.NoteInvalid);
            var allChannelData = new List<List<string>>();

            for (int c = 0; c < song.Channels.Length; c++)
            {
                var channel = song.Channels[c];
                var currentSpeed = song.FamitrackerSpeed;
                var isSpeedChannel = c == speedChannel;
                var instrument = (Instrument)null;
                var previousGroove = song.Groove;
                var previousGroovePadMode = song.GroovePaddingMode;
                var arpeggio = (Arpeggio)null;
                var sawVolume = Vrc6SawMasterVolumeType.Half;
                var sawVolumeChanged = false;
                var lastVolume = 15;
                var firstInstrumentInLoop = (Instrument)null;
                var lastInstrumentInLoop = (Instrument)null;
                var currentFlags = OverrideFlags.None;
                var channelData = new List<string>();
                allChannelData.Add(channelData);

                channelData.Add($"{llp}song{songIdx}ch{c}:");

                if (isSpeedChannel && project.UsesFamiTrackerTempo)
                {
                    channelData.Add($"{hexp}{(kernel == FamiToneKernel.FamiStudio ? OpcodeSpeed : OpcodeSpeedFT2):x2}+");
                    channelData.Add($"{hexp}{song.FamitrackerSpeed:x2}");
                }

                // Look at the first/last instrument in the looping section to see if we 
                // will need to reset it when looping.
                if (song.LoopPoint >= 0)
                {
                    for (var p = song.LoopPoint; p < song.Length; p++)
                    {
                        var pattern = channel.PatternInstances[p];
                        if (pattern != null)
                        {
                            foreach (var kv in pattern.Notes)
                            {
                                if (kv.Value.IsMusical && kv.Value.Instrument != null)
                                {
                                    if (firstInstrumentInLoop == null)
                                        firstInstrumentInLoop = kv.Value.Instrument;
                                    lastInstrumentInLoop = kv.Value.Instrument;
                                }
                            }
                        }
                    }
                }

                for (int p = 0; p < song.Length; p++)
                {
                    var pattern = channel.PatternInstances[p] == null ? emptyPattern : channel.PatternInstances[p];

                    if (p == song.LoopPoint)
                    {
                        channelData.Add($"{llp}song{songIdx}ch{c}loop:");

                        // Only reload the instruments if its different. Setting it to NULL unconditionally
                        // forces re-triggerring attacks on notes that had them disabled.
                        if (lastInstrumentInLoop != firstInstrumentInLoop)
                        {
                            instrument = null;
                        }

                        // TODO : We probably want to do the same for those too?
                        arpeggio = null;
                        lastVolume = -1;

                        if (sawVolumeChanged)
                            sawVolume = -1;

                        // If this channel potentially uses any arpeggios, clear the override since the last
                        // note may have overridden it. TODO: Actually check if thats the case!
                        if (channel.UsesArpeggios)
                            channelData.Add($"{hexp}{OpcodeClearArpEnvOverride:x2}+");
                    }

                    if (isSpeedChannel && project.UsesFamiStudioTempo)
                    {
                        var groove = song.GetPatternGroove(p);
                        var groovePadMode = song.GetPatternGroovePaddingMode(p);

                        // If the groove changes or we are at the loop point, set the tempo envelope again.
                        if (Utils.CompareArrays(groove, previousGroove) != 0 || groovePadMode != previousGroovePadMode || p == song.LoopPoint)
                        {
                            var grooveName = GetGrooveAsmName(groove, groovePadMode);

                            channelData.Add($"{hexp}{OpcodeSetTempoEnv:x2}+");
                            channelData.Add($"{lo}({llp}tempo_env_{grooveName})");
                            channelData.Add($"{hi}({llp}tempo_env_{grooveName})");
                            previousGroove = groove;
                            previousGroovePadMode = groovePadMode;
                        }
                        else if (p != 0)
                        {
                            // Otherwise just reset it so that it realigns to the groove.
                            channelData.Add($"{hexp}{OpcodeResetTempoEnv:x2}+");
                        }
                    }

                    var patternLength = song.GetPatternLength(p);

                    for (var it = pattern.GetDenseNoteIterator(0, patternLength); !it.Done;)
                    {
                        var time = it.CurrentTime;
                        var note = it.CurrentNote;
                        var location = new NoteLocation(p, time);
                        var frameFlags = OverrideFlags.None;

                        if (note == null)
                            note = emptyNote;

                        // We don't allow delaying speed effect at the moment.
                        if (isSpeedChannel && song.UsesFamiTrackerTempo)
                        {
                            var speed = FindEffectParam(song, p, time, Note.EffectSpeed);
                            if (speed >= 0)
                            {
                                currentSpeed = speed;
                                channelData.Add($"{hexp}{OpcodeSpeed:x2}+");
                                channelData.Add($"{hexp}{(byte)speed:x2}");
                            }
                        }

                        if (OtherVrc7ChannelUsesCustomPatch(song, channel, instrument, p, time))
                        {
                            instrument = null;
                        }

                        if (Square1HasEnvelopePeriodEffect(song, channel, p, time))
                        {
                            frameFlags   |= OverrideFlags.EnvPeriod;
                            currentFlags |= OverrideFlags.EnvPeriod;
                        }

                        it.Next();

                        if (note.HasNoteDelay)
                        {
                            channelData.Add($"{hexp}{OpcodeDelayedNote:x2}+");
                            channelData.Add($"{hexp}{note.NoteDelay - 1:x2}");
                            usesDelayedNotesOrCuts = true;
                        }

                        if (note.HasVolume)
                        {
                            if (note.Volume != lastVolume)
                            {
                                channelData.Add($"{hexp}{(byte)(OpcodeVolumeBits | note.Volume):x2}+");
                                lastVolume = note.Volume;
                            }

                            usesVolumeTrack = true;

                            if (note.HasVolumeSlide)
                            {
                                channel.ComputeVolumeSlideNoteParams(note, location, currentSpeed, false, out var stepSizeNtsc, out var _);
                                channel.ComputeVolumeSlideNoteParams(note, location, currentSpeed, false, out var stepSizePal, out var _);

                                if (machine == MachineType.NTSC)
                                    stepSizePal = stepSizeNtsc;
                                else if (machine == MachineType.PAL)
                                    stepSizeNtsc = stepSizePal;

                                var stepSize = Math.Max(Math.Abs(stepSizeNtsc), Math.Abs(stepSizePal)) * Math.Sign(stepSizeNtsc);
                                channelData.Add($"{hexp}{OpcodeVolumeSlide:x2}+");
                                channelData.Add($"{hexp}{(byte)stepSize:x2}");
                                channelData.Add($"{hexp}{note.VolumeSlideTarget << 4:x2}");

                                lastVolume = note.VolumeSlideTarget;
                                usesVolumeSlide = true;
                            }
                        }

                        if (note.HasFinePitch)
                        {
                            channelData.Add($"{hexp}{OpcodeFinePitch:x2}+");
                            channelData.Add($"{hexp}{note.FinePitch:x2}");
                            usesPitchTrack = true;
                        }

                        if (note.HasVibrato)
                        {
                            var vib = (byte)(note.VibratoSpeed == 0 || note.VibratoDepth == 0 ? 0 : note.RawVibrato);

                            // TODO: If note has attack, no point in setting the default vibrato envelope, instrument will do it anyway.
                            channelData.Add($"{hexp}{OpcodeOverridePitchEnv:x2}+");
                            channelData.Add($"{lo}({vibratoEnvelopeNames[vib]})");
                            channelData.Add($"{hi}({vibratoEnvelopeNames[vib]})");

                            if (vib == 0)
                            {
                                channelData.Add($"{hexp}{OpcodeClearPitchEnvOverride:x2}+");
                            }
                            else
                            {
                                frameFlags |= OverrideFlags.Vibrato;
                                currentFlags |= OverrideFlags.Vibrato;
                            }

                            usesVibrato = true;
                        }

                        if (note.HasPhaseReset)
                        {
                            if (channel.IsN163Channel)
                                channelData.Add($"{hexp}{OpcodeN163PhaseReset:x2}+");
                            else
                                channelData.Add($"{hexp}{OpcodePhaseReset:x2}+");
                            usesPhaseReset = true;
                        }

                        if (note.IsMusical)
                        {
                            // Set/clear override when changing arpeggio
                            if (note.Arpeggio != arpeggio)
                            {
                                if (note != null || !note.HasAttack)
                                {
                                    channelData.Add($"{hexp}{OpcodeOverridArpEnv:x2}+");

                                    if (note.Arpeggio == null)
                                    {
                                        channelData.Add($"{lo}({noArpeggioEnvelopeName})");
                                        channelData.Add($"{hi}({noArpeggioEnvelopeName})");
                                    }
                                    else
                                    {
                                        channelData.Add($"{lo}({arpeggioEnvelopeNames[note.Arpeggio]})");
                                        channelData.Add($"{hi}({arpeggioEnvelopeNames[note.Arpeggio]})");
                                    }
                                }

                                // TODO : Shouldnt we only do that when turning off the arp? Dont remember why its like that.
                                if (note.Arpeggio == null)
                                    channelData.Add($"{hexp}{OpcodeClearArpEnvOverride:x2}+");

                                arpeggio = note.Arpeggio;
                                usesArpeggio = true;
                            }
                            // If same arpeggio, but note has an attack, reset it.
                            else if (note.HasAttack && arpeggio != null)
                            {
                                channelData.Add($"{hexp}{OpcodeResetArpEnv:x2}+");
                            }
                        }

                        if (note.HasDutyCycle)
                        {
                            channelData.Add($"{hexp}{OpcodeDutyCycle:x2}+");
                            channelData.Add($"{hexp}{note.DutyCycle:x2}");
                            usesDutyCycleEffect = true;
                        }

                        if (note.HasEnvelopePeriod)
                        {
                            channelData.Add($"{hexp}{(channel.IsS5BChannel ? OpcodeS5BManualEnvPeriod : OpcodeEPSMManualEnvPeriod):x2}+");
                            channelData.Add($"{hexp}{(note.EnvelopePeriod >> 0) & 0xff:x2}");
                            channelData.Add($"{hexp}{(note.EnvelopePeriod >> 8) & 0xff:x2}");
                        }

                        if (note.HasFdsModSpeed)
                        {
                            channelData.Add($"{hexp}{OpcodeFdsModSpeed:x2}+");
                            channelData.Add($"{hexp}{(note.FdsModSpeed >> 0) & 0xff:x2}");
                            channelData.Add($"{hexp}{(note.FdsModSpeed >> 8) & 0xff:x2}");
                            frameFlags |= OverrideFlags.FdsModSpeed;
                            currentFlags |= OverrideFlags.FdsModSpeed;
                        }

                        if (note.HasFdsModDepth)
                        {
                            channelData.Add($"{hexp}{OpcodeFdsModDepth:x2}+");
                            channelData.Add($"{hexp}{note.FdsModDepth:x2}");
                            frameFlags |= OverrideFlags.FdsModDepth;
                            currentFlags |= OverrideFlags.FdsModDepth;
                        }

                        if (note.HasCutDelay)
                        {
                            channelData.Add($"{hexp}{OpcodeDelayedCut:x2}+");
                            channelData.Add($"{hexp}{note.CutDelay:x2}");
                            usesDelayedNotesOrCuts = true;
                        }

                        if (note.HasDeltaCounter)
                        {
                            // Use hi-bit to flag if we need to apply it immediately (no samples playing this frame)
                            //or a bit later (when playing the sample, overriding the initial DMC value).
                            channelData.Add($"{hexp}{OpcodeDeltaCounter:x2}+");
                            channelData.Add($"{hexp}{((note.IsMusical ? 0x00 : 0x80) | (note.DeltaCounter)):x2}");
                            usesDeltaCounter = true;
                        }

                        if (note.IsValid)
                        {
                            // Instrument change.
                            if (note.IsMusical && note.Instrument != null && !channel.IsDpcmChannel)
                            {
                                var attack = note.HasAttack || !Channel.CanDisableAttack(channel.Type, instrument, note.Instrument);

                                if (!attack)
                                {
                                    // TODO: Remove note entirely after a slide that matches the next note with no attack.
                                    channelData.Add($"{hexp}{OpcodeDisableAttack:x2}+");
                                }
                                else
                                {
                                    // If there FDS speed/depth was previously overridden (but is not this frame) and we have an attack,
                                    // we need to rebind the instrument to set the correct speed/depth again. This is pretty wasteful since
                                    // this will rebind the entire wave/mod table, but that's the best we can do right now without adding a
                                    // new opcode or something like that.
                                    if ((!frameFlags.HasFlag(OverrideFlags.FdsModSpeed) && currentFlags.HasFlag(OverrideFlags.FdsModSpeed)))
                                    {
                                        currentFlags &= ~(OverrideFlags.FdsModSpeed);
                                        instrument = null;
                                    }
                                    if ((!frameFlags.HasFlag(OverrideFlags.FdsModDepth) && currentFlags.HasFlag(OverrideFlags.FdsModDepth)))
                                    {
                                        currentFlags &= ~(OverrideFlags.FdsModDepth);
                                        instrument = null;
                                    }

                                    // Same rule for S5B/EPSM square manual envelope period.
                                    if ((!frameFlags.HasFlag(OverrideFlags.EnvPeriod) && currentFlags.HasFlag(OverrideFlags.EnvPeriod)))
                                    {
                                        currentFlags &= ~(OverrideFlags.EnvPeriod);
                                        instrument = null;
                                    }

                                    // Save for vibrator/arps, we need to rebind the instrument at the moment to reset the correct envelopes.
                                    if ((!frameFlags.HasFlag(OverrideFlags.Vibrato) && currentFlags.HasFlag(OverrideFlags.Vibrato)))
                                    {
                                        currentFlags &= ~(OverrideFlags.Vibrato);
                                        instrument = null;
                                    }
                                }

                                if (note.Instrument != instrument)
                                {
                                    // Change saw volume if needed.
                                    if (channel.Type == ChannelType.Vrc6Saw && sawVolume != note.Instrument.Vrc6SawMasterVolume)
                                    {
                                        sawVolume = note.Instrument.Vrc6SawMasterVolume;
                                        sawVolumeChanged = true;

                                        channelData.Add($"{hexp}{OpcodeVrc6SawMasterVolume:x2}+");
                                        channelData.Add($"{hexp}{1 - sawVolume:x2}");
                                    }

                                    var idx = instrumentIndices[note.Instrument];
                                    if (kernel == FamiToneKernel.FamiStudio && idx >= ExtendedInstrumentStart)
                                    {
                                        channelData.Add($"{hexp}{OpcodeExtendedInstrument:x2}+");
                                        channelData.Add($"{hexp}{idx:x2}");
                                    }
                                    else
                                    {
                                        channelData.Add($"{hexp}{(byte)(0x80 | (idx << 1)):x2}+");
                                    }

                                    instrument = note.Instrument;
                                }
                            }

                            int numNotes = 0;

                            if (kernel != FamiToneKernel.FamiStudio)
                            {
                                // Note -> Empty -> Note special encoding.
                                if (time < patternLength - 2)
                                {
                                    pattern.Notes.TryGetValue(time + 1, out var nextNote1);
                                    pattern.Notes.TryGetValue(time + 2, out var nextNote2);

                                    var valid1 = (nextNote1 != null && nextNote1.IsValid) || (isSpeedChannel && FindEffectParam(song, p, time + 1, Note.EffectSpeed) >= 0);
                                    var valid2 = (nextNote2 != null && nextNote2.IsValid) || (isSpeedChannel && FindEffectParam(song, p, time + 2, Note.EffectSpeed) >= 0);

                                    if (!valid1 && valid2)
                                    {
                                        it.Next();
                                        numNotes = 1;
                                    }
                                }
                            }

                            var emittedSlideNote = false;

                            if (note.IsSlideNote)
                            {
                                var noteTableNtsc = NesApu.GetNoteTableForChannelType(channel.Type, false, song.Project.ExpansionNumN163Channels, song.Project.Tuning);
                                var noteTablePal = NesApu.GetNoteTableForChannelType(channel.Type, true, song.Project.ExpansionNumN163Channels, song.Project.Tuning);

                                var found = true;
                                found &= channel.ComputeSlideNoteParams(note, location, currentSpeed, noteTableNtsc, false, true, out _, out int stepSizeNtsc, out _);
                                found &= channel.ComputeSlideNoteParams(note, location, currentSpeed, noteTablePal, true, true, out _, out int stepSizePal, out _);

                                if (machine == MachineType.NTSC)
                                    stepSizePal = stepSizeNtsc;
                                else if (machine == MachineType.PAL)
                                    stepSizeNtsc = stepSizePal;

                                if (found)
                                {
                                    // Take the (signed) maximum of both notes so that we are garantee to reach our note.
                                    var stepSize = Math.Max(Math.Abs(stepSizeNtsc), Math.Abs(stepSizePal)) * Math.Sign(stepSizeNtsc);
                                    channelData.Add($"{hexp}{OpcodeSlide:x2}+");
                                    channelData.Add($"{hexp}{(byte)stepSize:x2}");
                                    channelData.Add($"{hexp}{EncodeNoteValue(c, note.Value):x2}");
                                    channelData.Add($"{hexp}{EncodeNoteValue(c, note.SlideNoteTarget):x2}*");
                                    usesSlideNotes = true;
                                    emittedSlideNote = true;

                                    if (channel.Type == ChannelType.Noise)
                                        usesNoiseSlideNotes = true;
                                }
                            }

                            if (!emittedSlideNote)
                            {
                                if (note.IsRelease)
                                {
                                    var opcode = OpcodeReleaseNote;

                                    // This is a bit overkill but simplifies the asm code. If we every run
                                    // out of opcodes, we could easily go back to a single release opcode.
                                    if (channel.IsVrc7Channel)
                                        opcode = OpcodeVrc7ReleaseNote;
                                    else if (channel.IsFdsChannel)
                                        opcode = OpcodeFdsReleaseNote;
                                    else if (channel.IsN163Channel)
                                        opcode = OpcodeN163ReleaseNote;
                                    else if (channel.IsEPSMFmChannel)
                                        opcode = OpcodeEpsmReleaseNote;

                                    channelData.Add($"{hexp}{opcode:x2}+*");
                                    usesReleaseNotes = true;
                                }
                                else
                                {
                                    var noteValue = (int)note.Value;

                                    if (channel.IsDpcmChannel && note.IsMusical)
                                    {
                                        noteValue = kernel == FamiToneKernel.FamiStudio ? SingleByteNoteMin + 1 : 1;

                                        if (note.Instrument != null)
                                        {
                                            var mapping = note.Instrument.GetDPCMMapping(note.Value);
                                            if (mapping != null)
                                                sampleMappingIndices.TryGetValue(mapping, out noteValue);
                                        }
                                    }

                                    var requiresExtendedNote = kernel == FamiToneKernel.FamiStudio && note.IsMusical && (noteValue <= SingleByteNoteMin || noteValue > SingleByteNoteMax);

                                    // We encode very common notes [C1 - G7] with a single bytes and emit a special
                                    // "extended note" opcode when it falls outside of that range.
                                    if (requiresExtendedNote)
                                    {
                                        channelData.Add($"{hexp}{OpcodeExtendedNote:x2}+");
                                        channelData.Add($"{hexp}{EncodeNoteValue(c, noteValue, false):x2}*");
                                    }
                                    else
                                    {
                                        channelData.Add($"{hexp}{EncodeNoteValue(c, noteValue, true, numNotes):x2}+*");
                                    }
                                }
                            }
                        }
                        else
                        {
                            int numEmptyNotes = 0;

                            while (!it.Done)
                            {
                                time = it.CurrentTime;
                                note = it.CurrentNote;

                                if (note == null)
                                    note = emptyNote;

                                if (OtherVrc7ChannelUsesCustomPatch(song, channel, instrument, p, time))
                                {
                                    instrument = null;
                                }
                                
                                // TODO: Change this, this is a shit show.
                                if (numEmptyNotes >= maxRepeatCount || 
                                    note.IsValid           ||
                                    note.HasVolume         || 
                                    note.HasVibrato        ||
                                    note.HasFinePitch      ||
                                    note.HasDutyCycle      ||
                                    note.HasFdsModSpeed    || 
                                    note.HasFdsModDepth    ||
                                    note.HasEnvelopePeriod || 
                                    note.HasNoteDelay      ||
                                    note.HasCutDelay       ||
                                    note.HasDeltaCounter   ||
                                    note.HasPhaseReset     ||
                                    (isSpeedChannel && FindEffectParam(song, p, time, Note.EffectSpeed) >= 0))
                                {
                                    break;
                                }

                                if (Square1HasEnvelopePeriodEffect(song, channel, p, time))
                                {
                                    currentFlags |= OverrideFlags.EnvPeriod;
                                }

                                numEmptyNotes++;
                                it.Next();
                            }

                            channelData.Add($"{hexp}{(byte)(0x81 | (numEmptyNotes << 1)):x2}+*");
                        }
                    }
                }

                if (song.LoopPoint < 0)
                {
                    if (kernel == FamiToneKernel.FamiStudio)
                        channelData.Add($"{hexp}{OpcodeEndSong:x2}+");
                    else
                        channelData.Add($"{llp}song{songIdx}ch{c}loop:");

                    // We still need a stop note after since our 'famistudio_advance_channel' never ends of an opcode.
                    channelData.Add($"{hexp}{EncodeNoteValue(c, Note.NoteStop):x2}*");
                }

                if (song.LoopPoint >= 0 || kernel != FamiToneKernel.FamiStudio)
                {
                    channelData.Add($"{hexp}{(kernel == FamiToneKernel.FamiStudio ? OpcodeLoop : OpcodeLoopFT2):x2}+");
                    channelData.Add($"{llp}song{songIdx}ch{c}loop");
                }
            }

            // Combine any duplicate channels by doing a string comparison of the output.
            // We need to replace any channel specific strings first with a simple replace
            var concattedChannelData = new List<string>();
            var replacePattern = "ch\\d+";
            foreach (var chn in allChannelData)
            {
                var concatted = string.Join("\n", chn);
                var replaced = Regex.Replace(concatted, replacePattern, "ch");
                concattedChannelData.Add(replaced);
            }
            // Compare each channel data with all of the previous channels to see if there's a match.
            // if it matches, add the channel pointer to the front of the matched channel
            // if there is no match, add it to the list of unique channels
            var uniqueChannels = new List<List<string>>
            {
                allChannelData[0]
            };
            for (int i = 1; i < allChannelData.Count; i++)
            {
                var isUnique = true;
                for (int j = 0; j < uniqueChannels.Count; j++)
                {
                    if (concattedChannelData[i] == concattedChannelData[j])
                    {
                        uniqueChannels[j].Insert(0, $"{ll}song{songIdx}ch{i}:");
                        isUnique = false;
                        break;
                    }
                }
                if (isUnique)
                {
                    uniqueChannels.Add(allChannelData[i]);
                }
            }

            var songData = new List<string>();
            foreach (var chn in uniqueChannels)
            {
                foreach (var line in chn)
                {
                    songData.Add(line);
                }
            }
            return songData;
        }

        // minNotesForJump is the minimum of notes to even consider doing a jump back to a reference. 
        int CompressAndOutputSongData(List<string> data, int songIdx, int minNotesForJump, bool writeLines)
        {
            // We add some suffixes to the data to tell us a bit more info about they represent:
            //   - A "+" suffix means its the beginning of an opcode.
            //   - A "*" suffix means its the end of a note.
            bool IsLabelOrRef(string str) => str[0] == ll[0];
            bool IsLabel(string str) => str[str.Length - 1] == ':';
            bool IsRef(string str) => str[0] == ll[0] && str[str.Length - 1] != ':';
            bool IsNote(string str) => str[str.Length - 1] == '*';
            bool IsOpcode(string str) => str[str.Length - 1] == '+' || str[str.Length - 2] == '+';
            string CleanNote(string str) => str.TrimEnd(new[] { '*', '+' });

            // Number of bytes to hash to start searching.
            const int HashNumBytes = 4;
            const bool DisableCompression = false; // For debugging

            var refs = new HashSet<int>();
            var jumpToRefs = new HashSet<int>();
            var patterns = new Dictionary<uint, List<int>>();
            var compressedData = new List<string>();

            if (DisableCompression)
            {
                compressedData = data;
            }
            else
            {
                for (int i = 0; i < data.Count;)
                {
                    var crc = 0u;
                    var compressible = IsOpcode(data[i]); // Cant only compress at the start of an opcode

                    if (compressible)
                    {
                        // Look ahead 4 bytes and compute hash.
                        for (int k = i; k < data.Count && k < i + HashNumBytes; k++)
                        {
                            var b = data[k];

                            if (IsLabelOrRef(b))
                            {
                                compressible = false;
                                break;
                            }

                            crc = CRC32.Compute(b, crc);
                        }

                        // Look at all the patterns matching the hash, take the longest.
                        if (compressible && patterns.TryGetValue(crc, out var matchingPatterns))
                        {
                            var bestPatternIdx = -1;
                            var bestPatternLen = -1;
                            var bestPatternNumNotes = -1;

                            foreach (var idx in matchingPatterns)
                            {
                                var lastNoteIdx = -1;
                                var numNotes = 0;

                                for (int j = idx, k = i; j < compressedData.Count && k < data.Count && numNotes < 250; j++, k++)
                                {
                                    if (compressedData[j] != data[k] || IsLabelOrRef(compressedData[j]))
                                        break;

                                    if (IsNote(compressedData[j]))
                                    {
                                        numNotes++;
                                        lastNoteIdx = j;
                                    }
                                }

                                if (numNotes >= minNotesForJump)
                                {
                                    var matchLen = lastNoteIdx - idx + 1;
                                    if (matchLen > bestPatternLen)
                                    {
                                        bestPatternIdx = idx;
                                        bestPatternLen = matchLen;
                                        bestPatternNumNotes = numNotes;
                                    }
                                }
                            }

                            // Output a jump to a ref if we found a good match.
                            if (bestPatternIdx > 0)
                            {
                                refs.Add(bestPatternIdx);
                                jumpToRefs.Add(compressedData.Count);

                                compressedData.Add($"{hexp}{(kernel == FamiToneKernel.FamiStudio ? OpcodeSetReference : OpcodeSetReferenceFT2):x2}");
                                compressedData.Add($"{hexp}{bestPatternNumNotes:x2}");
                                compressedData.Add($"{llp}song{songIdx}ref{bestPatternIdx}");

                                i += bestPatternLen;

                                // No point of hashing jumps, we will never want to reuse those.
                                continue;
                            }
                        }
                    }

                    compressedData.Add(data[i++]);

                    // Keep hash of compressed data.
                    if (compressedData.Count >= HashNumBytes)
                    {
                        crc = 0u;

                        var startHashIdx = compressedData.Count - HashNumBytes;
                        for (int j = startHashIdx; j < compressedData.Count; j++)
                            crc = CRC32.Compute(compressedData[j], crc);

                        if (!patterns.TryGetValue(crc, out var list))
                        {
                            list = new List<int>();
                            patterns.Add(crc, list);
                        }

                        list.Add(startHashIdx);
                    }
                }
            }

            // Output the assembly code.
            var size = 0;
            string byteString = null;

            for (int i = 0; i < compressedData.Count; i++)
            {
                var b = CleanNote(compressedData[i]);

                var isRef = refs.Contains(i);
                var isLabel = IsLabel(b);
                var isJumpCode = jumpToRefs.Contains(i);
                var isJumpLabel = IsRef(b);

                if (byteString != null && (isJumpLabel || isLabel || isRef || isJumpCode || byteString.Length > 120))
                {
                    if (writeLines)
                        lines.Add(byteString);
                    byteString = null;
                }

                if (isRef)
                {
                    if (writeLines)
                        lines.Add($"{llp}song{songIdx}ref{i}:");
                }

                if (isLabel)
                {
                    if (writeLines)
                        lines.Add(b);
                }
                else if (isJumpLabel)
                {
                    if (writeLines)
                        lines.Add($"\t{dw} {b}");
                    size += 2;
                }
                else
                {
                    if (byteString == null)
                        byteString = $"\t{db} {b}";
                    else
                        byteString += $", {b}";
                    size++;
                }
            }

            if (writeLines && byteString != null)
                lines.Add(byteString);

            return size;
        }

        private int ProcessAndOutputSong(int songIdx)
        {
            var song = project.Songs[songIdx];

            // Take the channel with the most speed effect as the speed channel.
            // This was a really dumb optimization in FT2...
            var speedEffectCount = new int[song.Channels.Length];
            for (int c = 0; c < song.Channels.Length; c++)
            {
                foreach (var pattern in song.Channels[c].Patterns)
                {
                    foreach (var note in pattern.Notes.Values)
                    {
                        if (note.HasSpeed)
                            speedEffectCount[c]++;
                    }
                }
            }

            int speedChannel = 0;
            int maxSpeedEffects = 0;
            for (int c = 0; c < song.Channels.Length; c++)
            {
                if (speedEffectCount[c] > maxSpeedEffects)
                {
                    maxSpeedEffects = speedEffectCount[c];
                    speedChannel = c;
                }
            }

            // Get raw uncompressed song data.
            var songData = GetSongData(song, songIdx, speedChannel);

            Log.LogMessage(LogSeverity.Debug, $"Uncompressed song data size {songData.Count} bytes.");

            // Try compression with various threshold for jump to ref.
            var bestSize = int.MaxValue;
            var bestMinNotesForJump = 0;

            for (int i = 8; i <= 40; i++)
            {
                var size = CompressAndOutputSongData(songData, songIdx, i, false);

                Log.LogMessage(LogSeverity.Debug, $"Compression with a match of {i} notes = {size} bytes.");

                // Equal is intentional here, if we find same size, but with larger matches, 
                // it will likely waste less CPU doing a bunch of jumps.
                if (size <= bestSize)
                {
                    bestSize = size;
                    bestMinNotesForJump = i;
                }
            }

            return CompressAndOutputSongData(songData, songIdx, bestMinNotesForJump, true);
        }
        
        private void SetupFormat(int format, bool separateSongs)
        {
            assemblyFormat = format;

            switch (format)
            {
                case AssemblyFormat.NESASM:
                    db = ".db";
                    dw = ".dw";
                    ll = ".";
                    lo = "LOW";
                    hi = "HIGH";
                    hexp = "$";
                    break;
                case AssemblyFormat.CA65:
                    db = ".byte";
                    dw = ".word";
                    ll = "@";
                    lo =  ".lobyte";
                    hi =  ".hibyte";
                    hexp = "$";
                    break;
                case AssemblyFormat.ASM6:
                    db = "db";
                    dw = "dw";
                    ll = "@";
                    lo = "<";
                    hi = ">";
                    hexp = "$";
                    break;
                case AssemblyFormat.SDAS:
                    db = ".db";
                    dw = ".dw";
                    ll = ".";
                    lo = "<";
                    hi = ">";
                    hexp = "0x";
                    break;
            }

            allowDashesInName = assemblyFormat != AssemblyFormat.ASM6;

            var name = Utils.MakeNiceAsmName(separateSongs ? project.Songs[0].Name : project.Name, allowDashesInName);
            llp = GetLocalLabelPrefix(name);
        }

        private void RemoveUnsupportedFeatures()
        {
            foreach (var song in project.Songs)
            {
                foreach (var channel in song.Channels)
                {
                    foreach (var pattern in channel.Patterns)
                    {
                        foreach (var note in pattern.Notes.Values)
                        {
                            // FamiTone2 supports very few effects.
                            if (kernel == FamiToneKernel.FamiTone2)
                            { 
                                if (note.IsRelease)
                                    note.Value = Note.NoteInvalid;

                                note.HasAttack       = true;
                                note.HasVibrato      = false;
                                note.HasVolume       = false;
                                note.HasVolumeSlide  = false;
                                note.IsSlideNote     = false;
                                note.HasFinePitch    = false;
                                note.HasDutyCycle    = false;
                                note.HasNoteDelay    = false;
                                note.HasCutDelay     = false;
                                note.HasDeltaCounter = false;
                                note.HasPhaseReset   = false;
                                note.Arpeggio        = null;
                            }
                            else
                            {
                                // Note delays for empty notes are useless and will only confuse the exporter.
                                // We also don't support delays on speed effects.
                                if (note.HasNoteDelay && (!note.IsValid && (note.EffectMask & (~(Note.EffectNoteDelayMask | Note.EffectSpeed))) == 0))
                                {
                                    note.HasNoteDelay = false;
                                }

                                if (note.HasVibrato && note.VibratoDepth > 13)
                                {
                                    Log.LogMessage(LogSeverity.Warning, $"Vibrato depths > 13 will not sound correct in the sound engine and will likely be removed in the future. ({channel.NameWithExpansion}, {pattern.Name}).");
                                }
                            }
                        }
                    }
                }
            }

            // Empty arpeggios confuses the exporter.
            for (int i = 0; i < project.Arpeggios.Count; )
            {
                var arp = project.Arpeggios[i];
                if (arp.Envelope.IsEmpty(EnvelopeType.Arpeggio))
                {
                    project.DeleteArpeggio(arp);
                }
                else
                {
                    i++;
                }
            }

            // Remove releases in envelopes + expansion audio for FamiTone2.
            if (kernel == FamiToneKernel.FamiTone2)
            {
                foreach (var instrument in project.Instruments)
                {
                    var env = instrument.Envelopes[EnvelopeType.Volume];
                    if (env.Release >= 0)
                    {
                        env.Length  = env.Release;
                        env.Release = -1;
                    }
                }

                foreach (var sample in project.Samples)
                {
                    sample.Bank = 0;
                }

                project.SetExpansionAudioMask(ExpansionType.NoneMask);
                project.SoundEngineUsesExtendedInstruments = false;
                project.SoundEngineUsesDpcmBankSwitching = false;
                project.SoundEngineUsesExtendedDpcm = false;
            }
        }

        private void SortInstruments()
        {
            var instrumentChanges = new List<(Instrument, int)>();

            // Roughly estimate the number of times an instrument will trigger and instrument change.
            foreach (var song in project.Songs)
            {
                foreach (var channel in song.Channels)
                {
                    // DPCM channel doesn;t actually uses real instruments.
                    if (channel.IsDpcmChannel)
                        continue;

                    var prevInstrument = (Instrument)null;

                    for (var it = channel.GetSparseNoteIterator(song.StartLocation, song.EndLocation, NoteFilter.Musical); !it.Done; it.Next())
                    {
                        var instrument = it.Note.Instrument;
                        if (instrument != null && instrument != prevInstrument)
                        {
                            var idx = instrumentChanges.FindIndex(i => i.Item1 == instrument);

                            if (idx == -1)
                                instrumentChanges.Add((instrument, 1));
                            else
                                instrumentChanges[idx] = (instrument, instrumentChanges[idx].Item2 + 1);

                            prevInstrument = instrument;
                        }
                    }
                }
            }

            instrumentChanges.Sort((i1, i2) => i2.Item2.CompareTo(i1.Item2));

            // Sort by most common to least common.
            for (var i = instrumentChanges.Count - 1; i >= 0; i--)
            {
                var instrument = instrumentChanges[i].Item1;
                project.MoveInstrument(instrument, null);
            }
        }

        private void MoveAllEnvelopePeriodsOnFirstChannel(Project project, int[] channelTypes)
        {
            // Keeping all envelop period effects on channel 1 simplifies the logic quite a bit. This garantee
            // that all effects will always be processed before any new notes.
            foreach (var song in project.Songs)
            {
                var firstChannel = song.GetChannelByType(channelTypes[0]);
                foreach (var channelType in channelTypes.Skip(1))
                {
                    var channel = song.GetChannelByType(channelType);
                    for (var it = channel.GetSparseNoteIterator(song.StartLocation, song.EndLocation, NoteFilter.EffectEnvPeriod); !it.Done; it.Next())
                    {
                        firstChannel.GetOrCreatePattern(it.PatternIndex).GetOrCreateNoteAt(it.NoteIndex).EnvelopePeriod = it.Note.EnvelopePeriod;
                        it.Note.ClearEffectValue(Note.EffectEnvelopePeriod);
                    }
                }
            }
        }

        private void SetupProject(Project originalProject, int[] songIds, int dpcmExportMode)
        {
            // Work on a temporary copy.
            project = originalProject.DeepClone();
            project.Filename = originalProject.Filename;
            project.ConvertToSimpleNotes();

            if (kernel == FamiToneKernel.FamiTone2 && project.UsesFamiStudioTempo)
            {
                project.ConvertToFamiTrackerTempo(false);
            }

            if (dpcmExportMode == DpcmExportMode.Minimum)
            {
                // Full cleanup.
                project.DeleteAllSongsBut(songIds, true);
            }
            else if (dpcmExportMode == DpcmExportMode.All)
            {
                // Leave any samples alones.
                project.DeleteAllSongsBut(songIds, false);
                project.Cleanup(false); 
            }
            else 
            {
                project.DeleteAllSongsBut(songIds, false);

                var usedInstrument = new HashSet<Instrument>();

                // Gather list of used instruments.
                if (dpcmExportMode == DpcmExportMode.MappedToAnyInstrument)
                {
                    usedInstrument.UnionWith(project.Instruments);
                }
                else
                {
                    foreach (var song in project.Songs)
                    {
                        var channel = song.Channels[ChannelType.Dpcm];

                        for (var it = channel.GetSparseNoteIterator(song.StartLocation, song.EndLocation, NoteFilter.Musical); !it.Done; it.Next())
                        {
                            usedInstrument.Add(it.Note.Instrument);
                        }
                    }
                }

                usedInstrument.Remove(null); // Safety.

                // Then get the used samples from those.
                var usedSamples = new HashSet<DPCMSample>();

                foreach (var inst in usedInstrument)
                {
                    if (inst.SamplesMapping != null)
                    {
                        foreach (var mapping in inst.SamplesMapping)
                        {
                            usedSamples.Add(mapping.Value.Sample);
                        }
                    }
                }

                // Deleted every sample that isnt used.
                for (var i = project.Samples.Count - 1; i >= 0; i--)
                {
                    var sample = project.Samples[i];

                    if (!usedSamples.Contains(sample))
                    {
                        project.DeleteSample(sample);
                    }
                }

                // Then perform cleanup, but leaving samples alone.
                project.Cleanup(false);
            }

            RemoveUnsupportedFeatures();

            project.MergeIdenticalInstruments();
            project.RemoveDpcmNotesWithoutMapping();
            project.PermanentlyApplyGrooves();

            if (project.UsesS5BExpansion)
                MoveAllEnvelopePeriodsOnFirstChannel(project, new[] { ChannelType.S5BSquare1, ChannelType.S5BSquare2, ChannelType.S5BSquare3});
            if (project.UsesEPSMExpansion)
                MoveAllEnvelopePeriodsOnFirstChannel(project, new[] { ChannelType.EPSMSquare1, ChannelType.EPSMSquare2, ChannelType.EPSMSquare3 });

            if (project.UsesMultipleExpansionAudios)
            {
                Debug.Assert(kernel == FamiToneKernel.FamiStudio);

                // HACK : We always pretend to have 8 channels to keep the driver code simple.
                if (project.UsesEPSMExpansion)
                    project.SetExpansionAudioMask(ExpansionType.AllMask, 8, false);
                else
                    project.SetExpansionAudioMask(ExpansionType.AllMask & ~(ExpansionType.EPSMMask), 8, false);
            }

            if (kernel == FamiToneKernel.FamiStudio)
            {
                SortInstruments();
            }
        }

        private void OutputIncludeFile(string includeFilename)
        {
            var includeLines = new List<string>();

            for (int songIdx = 0; songIdx < project.Songs.Count; songIdx++)
            {
                var song = project.Songs[songIdx];
                includeLines.Add($"song_{Utils.MakeNiceAsmName(song.Name, allowDashesInName)} = {songIdx}");
            }
            includeLines.Add($"song_max = {project.Songs.Count}");

            // For CA65, also include song names.
            if (assemblyFormat == AssemblyFormat.CA65)
            {
                includeLines.Add("");
                includeLines.Add(".if SONG_STRINGS");
                includeLines.Add("song_strings:");

                foreach (var song in project.Songs)
                {
                    includeLines.Add($".asciiz \"{song.Name}\"");
                }

                includeLines.Add(".endif");
            }

            File.WriteAllLines(includeFilename, includeLines.ToArray());
        }

        private List<string> GetRequiredFlags()
        {
            var flags = new List<string>();

            // Expansion defines
            if (project.UsesVrc6Expansion)
                flags.Add("Project uses VRC6 expansion, you must set FAMISTUDIO_EXP_VRC6 = 1.");
            if (project.UsesVrc7Expansion)
                flags.Add("Project uses VRC7 expansion, you must set FAMISTUDIO_EXP_VRC7 = 1.");
            if (project.UsesMmc5Expansion)
                flags.Add("Project uses MMC5 expansion, you must set FAMISTUDIO_EXP_MMC5 = 1.");
            if (project.UsesS5BExpansion)
                flags.Add("Project uses S5B expansion, you must set FAMISTUDIO_EXP_S5B = 1.");
            if (project.UsesFdsExpansion)
                flags.Add("Project uses FDS expansion, you must set FAMISTUDIO_EXP_FDS = 1.");
            if (project.UsesN163Expansion)
                flags.Add($"Project uses N163 expansion, you must set FAMISTUDIO_EXP_N163 = 1 and FAMISTUDIO_EXP_N163_CHN_CNT = {project.ExpansionNumN163Channels}.");
            if (project.UsesEPSMExpansion)
                flags.Add("Project uses EPSM expansion, you must set FAMISTUDIO_EXP_EPSM = 1.");
                    
            // Feature usage defines.
            if (usesFamiTrackerTempo)
                flags.Add("Project uses FamiTracker tempo, you must set FAMISTUDIO_USE_FAMITRACKER_TEMPO = 1.");
            if (usesDelayedNotesOrCuts)
                flags.Add("Project uses delayed notes or cuts, you must set FAMISTUDIO_USE_FAMITRACKER_DELAYED_NOTES_OR_CUTS = 1.");
            if (usesReleaseNotes)
                flags.Add("Project uses release notes, you must set FAMISTUDIO_USE_RELEASE_NOTES = 1.");
            if (usesVolumeTrack)
                flags.Add("Volume track is used, you must set FAMISTUDIO_USE_VOLUME_TRACK = 1.");
            if (usesVolumeSlide)
                flags.Add("Volume slides are used, you must set FAMISTUDIO_USE_VOLUME_SLIDES = 1.");
            if (usesPitchTrack)
                flags.Add("Fine pitch track is used, you must set FAMISTUDIO_USE_PITCH_TRACK = 1.");
            if (usesSlideNotes)
                flags.Add("Slide notes are used, you must set FAMISTUDIO_USE_SLIDE_NOTES = 1.");
            if (usesNoiseSlideNotes)
                flags.Add("Slide notes are used on the noise channel, you must set FAMISTUDIO_USE_NOISE_SLIDE_NOTES = 1.");
            if (usesVibrato)
                flags.Add("Vibrato effect is used, you must set FAMISTUDIO_USE_VIBRATO = 1.");
            if (usesArpeggio)
                flags.Add("Arpeggios are used, you must set FAMISTUDIO_USE_ARPEGGIO = 1.");
            if (usesDutyCycleEffect)
                flags.Add("Duty Cycle effect is used, you must set FAMISTUDIO_USE_DUTYCYCLE_EFFECT = 1.");
            if (usesDeltaCounter)
                flags.Add("DPCM Delta Counter effect is used, you must set FAMISTUDIO_USE_DELTA_COUNTER = 1.");
            if (usesPhaseReset)
                flags.Add("Phase Reset effect is used, you must set FAMISTUDIO_USE_PHASE_RESET = 1.");
            if (usesFdsAutoMod)
                flags.Add("FDS auto-modulation is used on at least 1 instrument, you must set FAMISTUDIO_USE_FDS_AUTOMOD = 1.");
            if (project.SoundEngineUsesDpcmBankSwitching)
                flags.Add("Project has DPCM bank-switching enabled in the project settings, you must set FAMISTUDIO_USE_DPCM_BANKSWITCHING = 1 and implement bank switching.");
            else if (project.SoundEngineUsesExtendedDpcm)
                flags.Add($"Project has extended DPCM mode enabled in the project settings, you must set FAMISTUDIO_USE_DPCM_EXTENDED_RANGE = 1.");
            if (project.SoundEngineUsesExtendedInstruments)
                flags.Add($"Project has extended instrument mode enabled in the project settings. You must set FAMISTUDIO_USE_INSTRUMENT_EXTENDED_RANGE = 1.");
            if (project.Tuning != 440)
                flags.Add("Project uses non-standard tuning, the note tables were dumped in .bin files. You will need to replace those in the sound engine code to hear the correct tuning.");

            return flags;
        }

        public bool Save(Project originalProject, int[] songIds, int format, int maxDpcmBankSize, bool separateSongs, string filename, string dmcFilename, int dmcExportMode, bool exportUnusedMappings, string includeFilename, int machine)
        {
            this.machine = machine;

            SetupProject(originalProject, songIds, dmcExportMode);
            SetupFormat(format, separateSongs);
            CleanupEnvelopes();
            GatherDPCMMappings(dmcExportMode, exportUnusedMappings);

            var dmcSizes     = OutputDPCMSamples(filename, dmcFilename, maxDpcmBankSize);
            var headerSize   = OutputHeader(separateSongs);
            var instSize     = OutputInstruments();
            var mappingsSize = OutputDPCMMappings();
            var tempoSize    = OutputTempoEnvelopes();

            if (log)
            {
                Log.LogMessage(LogSeverity.Info, $"Header size : {headerSize} bytes.");
                Log.LogMessage(LogSeverity.Info, $"Instruments size : {instSize} bytes.");
                Log.LogMessage(LogSeverity.Info, $"Tempo envelopes size : {tempoSize} bytes.");
            }

            var totalSongsSize = 0;
            for (int i = 0; i < project.Songs.Count; i++)
            {
                var songSize = ProcessAndOutputSong(i);
                totalSongsSize += songSize;

                if (log)
                    Log.LogMessage(LogSeverity.Info, $"Song '{project.Songs[i].Name}' size: {songSize} bytes.");
            }
            
            var usedFlags = kernel == FamiToneKernel.FamiStudio ? GetRequiredFlags() : new List<string>();
            var pattern = @"FAMISTUDIO_\S+\s*=\s*[1-8]";

            List<string> flagComments = [$"; Required flags for {project.Name}:"];
            flagComments.AddRange
            (
                usedFlags.SelectMany(flag => Regex.Matches(flag, pattern).Cast<Match>()).Select(match => $"; {match.Value}")
            );

            lines.InsertRange(1, flagComments);

            File.WriteAllLines(filename, lines);

            if (includeFilename != null)
            {
                if (!project.EnsureSongAssemblyNamesAreUnique())
                {
                    return false;
                }

                OutputIncludeFile(includeFilename);
            }

            if (log)
            {
                Log.LogMessage(LogSeverity.Info, $"Total assembly file size: {headerSize + instSize + tempoSize + totalSongsSize} bytes.");

                if (project.UsesSamples)
                {
                    for (int i = 0; i < dmcSizes.Length; i++)
                    {
                        if (dmcSizes[i] != 0)
                            Log.LogMessage(LogSeverity.Info, $"DMC bank {i} file size: {dmcSizes[i]} bytes.");
                    }
                }

                if (kernel == FamiToneKernel.FamiStudio)
                {
                    foreach (var flagMessage in usedFlags)
                    {
                        Log.LogMessage(LogSeverity.Info, flagMessage);
                    }

                    if (project.Tuning != 440)
                    {
                        NesApu.DumpNoteTableBinSet(Path.GetDirectoryName(filename), project.Tuning, project.ExpansionAudioMask, machine, project.ExpansionNumN163Channels);
                    }
                }
            }

#if DEBUG
            Debug.Assert(GetAsmFileSize(lines) == headerSize + instSize + mappingsSize + tempoSize + totalSongsSize);
#endif

            return true;
        }

#if DEBUG
        private int GetAsmFileSize(List<string> lines)
        {
            int size = 0;
            var dbspc = db + " ";
            var dwspc = dw + " ";

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                int commentIdx = trimmedLine.IndexOf(';');
                if (commentIdx >= 0)
                    trimmedLine = trimmedLine.Substring(0, commentIdx);

                bool isByte = trimmedLine.StartsWith(dbspc);
                bool isWord = trimmedLine.StartsWith(dwspc);

                if (isByte || isWord)
                {
                    var splits = trimmedLine.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    size += splits.Length * (isWord ? 2 : 1);
                }
            }

            return size;
        }
#endif

        // Assumed to be in ASM6 format.
        public static byte[] ParseAsmFile(string filename, int songOffset, int dpcmOffset)
        {
            var labels = new Dictionary<string, int>();
            var labelsToPatch = new List<Tuple<string, int>>();
            var bytes = new List<byte>();

            string[] lines = File.ReadAllLines(filename);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                int commentIdx = trimmedLine.IndexOf(';');
                if (commentIdx >= 0)
                {
                    trimmedLine = trimmedLine.Substring(0, commentIdx);
                }

                bool isByte = trimmedLine.StartsWith("db ");
                bool isWord = trimmedLine.StartsWith("dw ");

                if (isByte || isWord)
                {
                    var splits = trimmedLine.Substring(3).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < splits.Length; i++)
                    {
                        var hex = false;
                        var valStr = splits[i].Trim();
                        var valNum = 0;

                        if (valStr.StartsWith("$"))
                        {
                            hex = true;
                            valStr = valStr.Substring(1).Trim();
                        }

                        if (valStr.StartsWith("<(") || valStr.StartsWith(">("))
                        {
                            // We only use those for vibrato right now.
                            bool lobyte = valStr.StartsWith("<(");
                            valStr = valStr.Substring(2, valStr.Length - 3);

                            if (labels.ContainsKey(valStr))
                                valNum = lobyte ? (labels[valStr] & 0xff) : (labels[valStr] >> 8);
                            else if (lobyte)
                                labelsToPatch.Add(new Tuple<string, int>(valStr, bytes.Count));
                        }
                        else if (labels.ContainsKey(valStr))
                        {
                            valNum = labels[valStr];
                        }
                        else
                        {
                            if (valStr.StartsWith("@"))
                            {
                                labelsToPatch.Add(new Tuple<string, int>(valStr, bytes.Count));
                            }
                            else if (valStr.Contains("FT_DPCM_PTR") || valStr.Contains("FAMISTUDIO_DPCM_PTR"))
                            {
                                valNum = Convert.ToInt32(valStr.Split('+')[0], 16) + ((dpcmOffset & 0x3fff) >> 6);
                            }
                            else
                            {
                                valNum = Convert.ToInt32(valStr, hex ? 16 : 10);
                            }
                        }

                        if (isByte)
                        {
                            bytes.Add((byte)(valNum & 0xff));
                        }
                        else
                        {
                            bytes.Add((byte)((valNum >> 0) & 0xff));
                            bytes.Add((byte)((valNum >> 8) & 0xff));
                        }
                    }
                }
                else if (trimmedLine.EndsWith(":"))
                {
                    labels[trimmedLine.TrimEnd(':')] = bytes.Count + songOffset;
                }
            }

            foreach (var pair in labelsToPatch)
            {
                int val;
                if (pair.Item1.Contains("@samples-"))
                {
                    var splits = pair.Item1.Split('-');
                    val = labels[splits[0]];
                    val -= Convert.ToInt32(splits[1]);
                }
                else
                {
                    val = labels[pair.Item1];
                }

                bytes[pair.Item2 + 0] = ((byte)((val >> 0) & 0xff));
                bytes[pair.Item2 + 1] = ((byte)((val >> 8) & 0xff));
            }

            return bytes.ToArray();
        }

        // HACK: This is pretty stupid. We write the ASM and parse it to get the bytes. Kind of backwards.
        public byte[] GetBytes(Project project, int[] songIds, int songOffset, int dpcmBankSize, int dpcmExportMode, bool dpcmExportUnusedMappings, int dpcmOffset, int machine)
        {
            var tempFolder = Utils.GetTemporaryDirectory();
            var tempAsmFilename = Path.Combine(tempFolder, "nsf.asm");
            var tempDmcFilename = Path.Combine(tempFolder, "nsf.dmc");

            Save(project, songIds, AssemblyFormat.ASM6, dpcmBankSize, false, tempAsmFilename, tempDmcFilename, dpcmExportMode, dpcmExportUnusedMappings, null, machine);

            return ParseAsmFile(tempAsmFilename, songOffset, dpcmOffset);
        }

        private static bool PatchNoteTableInternal(byte[] data, string tblFile, int tableOffset, int tuning, bool pal, int numN163Channels)
        {
            using (var noteTableStream = typeof(FamitoneMusicFile).Assembly.GetManifestResourceStream(tblFile))
            using (StreamReader reader = new StreamReader(noteTableStream))
            {
                var noteTableLines = reader.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in noteTableLines)
                {
                    var pair = line.Split('=', StringSplitOptions.RemoveEmptyEntries);
                    Debug.Assert(pair.Length == 2);

                    var noteTable = (ushort[])null;

                    if (pair[0].StartsWith("famistudio_s5b_note_table"))
                        noteTable = NesApu.GetNoteTableForChannelType(ChannelType.S5BSquare1, pal, numN163Channels, tuning);
                    else if (pair[0].StartsWith("famistudio_n163_note_table"))
                        noteTable = NesApu.GetNoteTableForChannelType(ChannelType.N163Wave1, pal, numN163Channels, tuning);
                    else if (pair[0].StartsWith("famistudio_vrc7_note_table"))
                        noteTable = NesApu.GetNoteTableForChannelType(ChannelType.Vrc7Fm1, pal, numN163Channels, tuning);
                    else if (pair[0].StartsWith("famistudio_fds_note_table"))
                        noteTable = NesApu.GetNoteTableForChannelType(ChannelType.FdsWave, pal, numN163Channels, tuning);
                    else if (pair[0].StartsWith("famistudio_saw_note_table"))
                        noteTable = NesApu.GetNoteTableForChannelType(ChannelType.Vrc6Saw, pal, numN163Channels, tuning);
                    else if (pair[0].StartsWith("famistudio_epsm_note_table"))
                        noteTable = NesApu.GetNoteTableForChannelType(ChannelType.EPSMFm1, pal, numN163Channels, tuning);
                    else if (pair[0].StartsWith("famistudio_epsm_s_note_table"))
                        noteTable = NesApu.GetNoteTableForChannelType(ChannelType.EPSMSquare1, pal, numN163Channels, tuning);
                    else if (pair[0].StartsWith("famistudio_note_table") || pair[0].StartsWith("_FT2NoteTable"))
                        noteTable = NesApu.GetNoteTableForChannelType(ChannelType.Square1, pal, numN163Channels, tuning);

                    if (noteTable == null)
                    {
                        Debug.Assert(false);
                        Log.LogMessage(LogSeverity.Error, $"Unknown note table '{pair[0]}'.");
                        return false;
                    }
                    
                    // Patch data in binary.
                    var msb = pair[0].ToLower().EndsWith("msb");
                    var offset = tableOffset + int.Parse(pair[1]);
                    var shift = msb ? 8 : 0;

                    for (int i = 0; i < noteTable.Length; i++)
                        data[offset + i] = (byte)((noteTable[i] >> shift) & 0xff);
                }
            }

            return true;
        }

        public static bool PatchNoteTable(byte[] data, string tblFile, int tuning, int machine, int numN163Channels)
        {
            var pal = machine == MachineType.PAL || machine == MachineType.Dual;
            if (!PatchNoteTableInternal(data, tblFile, 0, tuning, pal, numN163Channels))
            {
                return false;
            }

            // For dual, we need to patch the 2 note tables that are back to back.
            if (machine == MachineType.Dual)
            {
                if (!PatchNoteTableInternal(data, tblFile, 97, tuning, false, numN163Channels))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public static class AssemblyFormat
    {
        public const int NESASM = 0;
        public const int CA65   = 1;
        public const int ASM6   = 2;
        public const int SDAS   = 3;

        public static readonly string[] Names =
        {
            "NESASM",
            "CA65",
            "ASM6",
            "SDAS"
        };

        public static int GetValueForName(string str)
        {
            return Array.IndexOf(Names, str);
        }
    };

    public static class FamiToneKernel
    {
        public const int FamiTone2  = 0; // Stock FamiTone2
        public const int FamiStudio = 1; // Heavily modified version that supports every FamiStudio feature.

        public static readonly string[] Names =
        {
            "FamiTone2",
            "FamiStudio"
        };

        public static int GetValueForName(string str)
        {
            return Array.IndexOf(Names, str);
        }
    };
    
    public static class DpcmExportMode
    {
        public const int All                    = 0; // Keeps all samples.
        public const int MappedToAnyInstrument  = 1; // Samples mapped to at least 1 instrument in the project (even if not referenced by exported song(s))
        public const int MappedToUsedInstrument = 2; // Samples mapped to at least 1 instrument (only if instrument is referenced by exported song(s))
        public const int Minimum                = 3; // Minimum set needed to play the song, only actualy notes that are needed are kept.
        public const int Count                  = 4;

        public static LocalizedString[] LocalizedNames = new LocalizedString[Count];

        static DpcmExportMode()
        {
            Localization.LocalizeStatic(typeof(DpcmExportMode));
        }
    };
    }
