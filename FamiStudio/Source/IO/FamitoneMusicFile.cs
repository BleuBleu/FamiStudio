using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace FamiStudio
{
    public class FamitoneMusicFile
    {
        private Project project;

        private List<string> lines = new List<string>();

        private string db = ".byte";
        private string dw = ".word";
        private string ll = "@";
        private string lo = ".lobyte";
        private string hi = ".hibyte";

        private int machine = MachineType.NTSC;
        private int assemblyFormat = AssemblyFormat.NESASM;
        private Dictionary<byte, string> vibratoEnvelopeNames = new Dictionary<byte, string>();
        private Dictionary<Arpeggio, string> arpeggioEnvelopeNames = new Dictionary<Arpeggio, string>();
        private Dictionary<Instrument, int> instrumentIndices = new Dictionary<Instrument, int>();
        private string noArpeggioEnvelopeName;

        private int kernel = FamiToneKernel.FamiStudio;

        private bool log = true;
        private int maxRepeatCount = MaxRepeatCountFT2;

        private const int MaxRepeatCountFT2FS = 63;
        private const int MaxRepeatCountFT2   = 60;

        // Matches "famistudio_opcode_jmp" in assembly.
        private const byte OpcodeFirst                 = 0x40;
        private const byte OpcodeExtendedNote          = 0x40;
        private const byte OpcodeSetReference          = 0x41;
        private const byte OpcodeLoop                  = 0x42;
        private const byte OpcodeDisableAttack         = 0x43;
        private const byte OpcodeReleaseNote           = 0x44;
        private const byte OpcodeSpeed                 = 0x45; // FamiTracker tempo only
        private const byte OpcodeDelayedNote           = 0x46; // FamiTracker tempo only
        private const byte OpcodeDelayedCut            = 0x47; // FamiTracker tempo only
        private const byte OpcodeSetTempoEnv           = 0x46; // FamiStudio tempo only
        private const byte OpcodeResetTempoEnv         = 0x47; // FamiStudio tempo only
        private const byte OpcodeOverridePitchEnv      = 0x48;
        private const byte OpcodeClearPitchEnvOverride = 0x49;
        private const byte OpcodeOverridArpEnv         = 0x4a;
        private const byte OpcodeClearArpEnvOverride   = 0x4b;
        private const byte OpcodeResetArpEnv           = 0x4c;
        private const byte OpcodeFinePitch             = 0x4d;
        private const byte OpcodeDutyCycle             = 0x4e;
        private const byte OpcodeSlide                 = 0x4f;
        private const byte OpcodeVolumeSlide           = 0x50;
        private const byte OpcodeDeltaCounter          = 0x51;
        private const byte OpcodeVrc6SawMasterVolume   = 0x52; // VRC6 only
        private const byte OpcodeVrc7ReleaseNote       = 0x53; // VRC7 only
        private const byte OpcodeFdsModSpeed           = 0x54; // FDS only
        private const byte OpcodeFdsModDepth           = 0x55; // FDS only
        private const byte OpcodeFdsReleaseNote        = 0x56; // FDS only
        private const byte OpcodeN163ReleaseNote       = 0x57; // N163 only
        private const byte OpcodeEpsmReleaseNote       = 0x58; // EPSM only

        private const byte OpcodeSetReferenceFT2       = 0xff; // FT2
        private const byte OpcodeLoopFT2               = 0xfd; // FT2
        private const byte OpcodeSpeedFT2              = 0xfb; // FT2

        private const byte OpcodeVolumeBits            = 0x70;

        private const int SingleByteNoteMin = 12;
        private const int SingleByteNoteMax = SingleByteNoteMin + (OpcodeFirst - 1);

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

        public FamitoneMusicFile(int kernel, bool outputLog)
        {
            this.log = outputLog;
            this.kernel = kernel;
            this.maxRepeatCount = kernel == FamiToneKernel.FamiStudio ? MaxRepeatCountFT2FS : MaxRepeatCountFT2;
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

        private int OutputHeader(bool separateSongs)
        {
            var size = 5;
            var name = Utils.MakeNiceAsmName(separateSongs ? project.Songs[0].Name : project.Name);

            lines.Add($"; This file for the {(kernel == FamiToneKernel.FamiTone2 ? "FamiTone2 library" : "FamiStudio Sound Engine")} and was generated by FamiStudio");
            lines.Add("");

            if (assemblyFormat == AssemblyFormat.CA65)
            {
                // For CA65 we add a re-export of the symbol prefixed with _ for the C code to see
                // For some reason though, we can only rexport these symbols either before they are defined,
                // or after all of the data is completely written.
                lines.Add(".if FAMISTUDIO_CFG_C_BINDINGS");
                lines.Add($".export _music_data_{name}=music_data_{name}");
                lines.Add(".endif");
                lines.Add("");
            }

            lines.Add($"music_data_{name}:");
            lines.Add($"\t{db} {project.Songs.Count}");
            lines.Add($"\t{dw} {ll}instruments");

            if (project.UsesFdsExpansion || project.UsesN163Expansion || project.UsesVrc7Expansion || project.UsesEPSMExpansion)
            {
                lines.Add($"\t{dw} {ll}instruments_exp");
                size += 2;
            }

            if (!project.GetMinMaxMappedSampleIndex(out var sampleTableOffset, out _))                sampleTableOffset = 1;

            lines.Add($"\t{dw} {ll}samples-{sampleTableOffset * (kernel == FamiToneKernel.FamiTone2 ? 3 : 4)}");

            for (int i = 0; i < project.Songs.Count; i++)
            {
                var song = project.Songs[i];
                var line = $"\t{dw} ";

                for (int chn = 0; chn < song.Channels.Length; ++chn)
                {
                    if (chn > 0)
                        line += ",";
                    line += $"{ll}song{i}ch{chn}";
                }

                if (song.UsesFamiTrackerTempo)
                {
                    int tempoPal  = 256 * song.FamitrackerTempo / (50 * 60 / 24);
                    int tempoNtsc = 256 * song.FamitrackerTempo / (60 * 60 / 24);

                    line += $",{tempoPal},{tempoNtsc} ; {i:x2} : {song.Name}";
                    lines.Add(line);

                    usesFamiTrackerTempo = true;
                }
                else
                {
                    var grooveName = GetGrooveAsmName(song.Groove, song.GroovePaddingMode);
                    line += $" ; {i:x2} : {song.Name}";
                    lines.Add(line);
                    lines.Add($"\t{db} {lo}({ll}tempo_env_{grooveName}), {hi}({ll}tempo_env_{grooveName}), {(project.PalMode ? 2 : 0)}, 0"); 
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

            return size;
        }

        private byte[] ProcessEnvelope(Envelope env, bool allowReleases, bool newPitchEnvelope)
        {
            // HACK : Pass dummy type here, volume envelopes have been taken care of already.
            if (env.IsEmpty(EnvelopeType.Count))
                return null;

            env.Truncate();

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
            byte ptr_loop = 0xff;
            byte rle_cnt = 0;
            byte prev_val = (byte)(env.Values[0] + 1);//prevent rle match
            bool found_release = false;

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

                if (prev_val != val || j == env.Loop || (allowReleases && j == env.Release) || j == env.Length - 1 || (rle_cnt == 127 && newPitchEnvelope))
                {
                    if (rle_cnt != 0)
                    {
                        if (rle_cnt == 1)
                        {
                            data[ptr++] = prev_val;
                        }
                        else
                        {
                            while (rle_cnt > 127)
                            {
                                data[ptr++] = 127;
                                rle_cnt -= 127;
                            }

                            data[ptr++] = rle_cnt;
                        }

                        rle_cnt = 0;
                    }

                    if (j == env.Loop) ptr_loop = ptr;

                    if (j == env.Release && allowReleases)
                    {
                        // A release implies the end of the loop.
                        Debug.Assert(ptr_loop != 0xff && data[ptr_loop] >= 128); // Cant be jumping back to the middle of RLE.
                        found_release = true;
                        data[ptr++] = 0;
                        data[ptr++] = ptr_loop;
                        data[0] = ptr;
                    }

                    data[ptr++] = val;

                    prev_val = val;
                }
                else
                {
                    ++rle_cnt;
                }
            }

            if (ptr_loop == 0xff || found_release)
            {
                ptr_loop = (byte)(ptr - 1);
            }
            else
            {
                Debug.Assert(data[ptr_loop] >= 128); // Cant be jumping back to the middle of RLE.
            }

            data[ptr++] = 0;
            data[ptr++] = ptr_loop;

            Array.Resize(ref data, ptr);

            return data;
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
                        case EnvelopeType.WaveformRepeat:
                            // Handled as special case below since multiple-waveform must be splitted and
                            // repeat envelope must be converted.
                            break;
                        case EnvelopeType.FdsModulation:
                            processed = env.BuildFdsModulationTable().Select(m => (byte)m).ToArray();
                            break;
                        case EnvelopeType.FdsWaveform:
                            processed = env.Values.Take(env.Length).Select(m => (byte)m).ToArray();
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

                // Special case for N163 multiple waveforms.
                if (instrument.IsN163)
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
                var arpeggioEnvelopes = new Dictionary<Arpeggio, uint>();

                foreach (var arpeggio in project.Arpeggios)
                {
                    var processed = ProcessEnvelope(arpeggio.Envelope, false, false);
                    uint crc = CRC32.Compute(processed);
                    arpeggioEnvelopes[arpeggio] = crc;
                    uniqueEnvelopes[crc] = processed;
                }

                foreach (var arpeggio in project.Arpeggios)
                {
                    arpeggioEnvelopeNames[arpeggio] = $"{ll}env{uniqueEnvelopes.IndexOfKey(arpeggioEnvelopes[arpeggio])}";
                }
            }

            var size = 0;

            // Write instruments
            lines.Add($"{ll}instruments:");

            var instrumentCount = 0;

            for (int i = 0; i < project.Instruments.Count; i++)
            {
                var instrument = project.Instruments[i];

                if (!instrument.IsFds  && 
                    !instrument.IsN163 &&
                    !instrument.IsVrc7 &&
                    !instrument.IsEpsm)
                {
                    var volumeEnvIdx   = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.Volume]]);
                    var arpeggioEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.Arpeggio]]);
                    var pitchEnvIdx    = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.Pitch]]);

                    if (kernel == FamiToneKernel.FamiStudio)
                    {
                        var dutyEnvIdx = instrument.IsEnvelopeActive(EnvelopeType.DutyCycle) ? uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.DutyCycle]]) : uniqueEnvelopes.IndexOfKey(defaultEnvCRC);

                        lines.Add($"\t{dw} {ll}env{volumeEnvIdx},{ll}env{arpeggioEnvIdx},{ll}env{dutyEnvIdx},{ll}env{pitchEnvIdx} ; {instrumentCount:x2} : {instrument.Name}");
                    }
                    else
                    {
                        var duty = instrument.IsEnvelopeActive(EnvelopeType.DutyCycle) ? instrument.Envelopes[EnvelopeType.DutyCycle].Values[0] : 0;
                        var dutyShift = instrument.IsRegular ? 6    : 4;
                        var dutyBits  = instrument.IsRegular ? 0x30 : 0;

                        lines.Add($"\t{db} ${(duty << dutyShift) | dutyBits:x2} ; {instrumentCount:x2} : {instrument.Name}");
                        lines.Add($"\t{dw} {ll}env{volumeEnvIdx}, {ll}env{arpeggioEnvIdx}, {ll}env{pitchEnvIdx}");
                        lines.Add($"\t{db} $00");
                    }

                    size += 8;
                    instrumentIndices[instrument] = instrumentCount++;
                }
            }

            if (instrumentCount > 64)
                Log.LogMessage(LogSeverity.Error, $"Number of instrument ({instrumentCount}) exceeds the limit of 64, song will not sound correct.");

            lines.Add("");

            // FDS, N163 and VRC7 instruments are special.
            if (project.UsesFdsExpansion  || 
                project.UsesN163Expansion || 
                project.UsesVrc7Expansion ||
                project.UsesEPSMExpansion)
            {
                lines.Add($"{ll}instruments_exp:");

                var instrumentCountExp = 0;

                for (int i = 0; i < project.Instruments.Count; i++)
                {
                    var instrument = project.Instruments[i];

                    if (instrument.IsFds  || 
                        instrument.IsVrc7 ||
                        instrument.IsN163 ||
                        instrument.IsEpsm)
                    {
                        var volumeEnvIdx   = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.Volume]]);
                        var arpeggioEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.Arpeggio]]);
                        var pitchEnvIdx    = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.Pitch]]);

                        lines.Add($"\t{dw} {ll}env{volumeEnvIdx}, {ll}env{arpeggioEnvIdx}, {ll}env{pitchEnvIdx} ; {instrumentCountExp:x2} : {instrument.Name}");

                        if (instrument.IsFds)
                        {
                            var fdsWavEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.FdsWaveform]]);
                            var fdsModEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.FdsModulation]]);

                            lines.Add($"\t{db} {instrument.FdsMasterVolume}");
                            lines.Add($"\t{dw} {ll}env{fdsWavEnvIdx}, {ll}env{fdsModEnvIdx}, {instrument.FdsModSpeed}");
                            lines.Add($"\t{db} {instrument.FdsModDepth}, {instrument.FdsModDelay}, $00");
                        }
                        else if (instrument.IsN163)
                        {
                            var repeatEnvIdx = uniqueEnvelopes.IndexOfKey(instrumentEnvelopes[instrument.Envelopes[EnvelopeType.WaveformRepeat]]);

                            lines.Add($"\t{dw} {ll}env{repeatEnvIdx}");
                            lines.Add($"\t{db} ${instrument.N163WavePos:x2}, ${instrument.N163WaveSize:x2}");
                            lines.Add($"\t{dw} {ll}{Utils.MakeNiceAsmName(instrument.Name)}_waves");
                            lines.Add($"\t{db} $00, $00, $00, $00");
                        }
                        else if (instrument.IsVrc7)
                        {
                            lines.Add($"\t{db} ${(instrument.Vrc7Patch << 4):x2}, $00");
                            lines.Add($"\t{db} {String.Join(",", instrument.Vrc7PatchRegs.Select(r => $"${r:x2}"))}");
                        }
                        else if (instrument.IsEpsm)
                        {
                            lines.Add($"\t{dw} {ll}instrument_epsm_extra_patch{i}");
                            // we can fit the first 8 bytes of data here to avoid needing to add padding
                            lines.Add($"\t{db} {String.Join(",", instrument.EpsmPatchRegs.Take(8).Select(r => $"${r:x2}"))}");
                        }

                        size += 16;
                        instrumentIndices[instrument] = instrumentCountExp++;
                    }
                }

                if (instrumentCountExp > 32)
                    Log.LogMessage(LogSeverity.Error, $"Number of expansion instrument ({instrumentCountExp}) exceeds the limit of 32, song will not sound correct.");

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
                        lines.Add($"{ll}instrument_epsm_extra_patch{i}:");
                        lines.Add($"\t{db} {String.Join(",", instrument.EpsmPatchRegs.Skip(8).Select(r => $"${r:x2}"))}");
                        size += 23;
                    }
                }
                lines.Add("");
            }

            // Write samples.
            lines.Add($"{ll}samples:");

            if (project.UsesSamples)
            {
                if (project.GetMinMaxMappedSampleIndex(out var minMapping, out var maxMapping))
                {
                    for (int i = minMapping; i <= maxMapping; i++)
                    {
                        var mapping = project.SamplesMapping[i];
                        var sampleOffset = 0;
                        var sampleSize = 0;
                        var sampleInitialDmcValue = NesApu.DACDefaultValue;
                        var samplePitchAndLoop = 0;
                        var sampleName = "";

                        if (mapping != null)
                        {
                            sampleOffset = Math.Max(0, project.GetAddressForSample(mapping.Sample, out _, out _)) >> 6;
                            sampleSize = mapping.Sample.ProcessedData.Length >> 4;
                            sampleName = $"({mapping.Sample.Name})";
                            samplePitchAndLoop = mapping.Pitch | ((mapping.Loop ? 1 : 0) << 6);
                            sampleInitialDmcValue = mapping.OverrideDmcInitialValue ? mapping.DmcInitialValueDiv2 * 2 : mapping.Sample.DmcInitialValueDiv2 * 2;
                        }

                        if (kernel == FamiToneKernel.FamiStudio)
                        {
                            size += 4;
                            lines.Add($"\t{db} ${sampleOffset:x2}+{lo}(FAMISTUDIO_DPCM_PTR),${sampleSize:x2},${samplePitchAndLoop:x2},${sampleInitialDmcValue:x2}\t;{i} {sampleName}");
                        }
                        else
                        {
                            size += 3;
                            lines.Add($"\t{db} ${sampleOffset:x2}+{lo}(FT_DPCM_PTR),${sampleSize:x2},${samplePitchAndLoop:x2}\t;{i} {sampleName}");
                        }
                    }
                }
            }

            lines.Add("");

            // Write envelopes.
            int idx = 0;
            foreach (var kv in uniqueEnvelopes)
            {
                var name = $"{ll}env{idx++}";
                lines.Add($"{name}:");
                lines.Add($"\t{db} {String.Join(",", kv.Value.Select(i => $"${i:x2}"))}");

                if (kv.Key == defaultEnvCRC)
                    defaultEnvName = name;
                if (kv.Key == defaultPitchOrReleaseEnvCRC)
                    defaultPitchEnvName = name;

                size += kv.Value.Length;
            }

            lines.Add("");

            // Write the N163 multiple waveforms.
            if (project.UsesN163Expansion)
            {
                foreach (var instrument in project.Instruments)
                {
                    if (instrument.IsN163)
                    {
                        lines.Add($"{ll}{Utils.MakeNiceAsmName(instrument.Name)}_waves:");

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

            // Write the unique vibrato envelopes.
            if (kernel == FamiToneKernel.FamiStudio)
            {
                // Create all the unique vibrato envelopes.
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
                                    if (note.VibratoDepth == 0 || note.VibratoSpeed == 0)
                                    {
                                        note.RawVibrato = 0;
                                        vibratoEnvelopeNames[0] = defaultPitchEnvName;
                                        continue;
                                    }

                                    var env = Envelope.CreateVibratoEnvelope(note.VibratoSpeed, note.VibratoDepth);
                                    var processed = ProcessEnvelope(env, false, true);
                                    uint crc = CRC32.Compute(processed);
                                    if (!uniqueEnvelopes.ContainsKey(crc))
                                    {
                                        var name = $"{ll}env{idx++}";
                                        lines.Add($"{name}:");
                                        lines.Add($"\t{db} {String.Join(",", processed.Select(i => $"${i:x2}"))}");

                                        uniqueEnvelopes[crc] = processed;
                                        vibratoEnvelopeNames[note.RawVibrato] = name;
                                        size += processed.Length;
                                    }
                                }
                            }
                        }
                    }
                }
            }

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

                    lines.Add($"{ll}tempo_env_{GetGrooveAsmName(groove.Item1, groove.Item2)}:");
                    lines.Add($"\t{db} {String.Join(",", env.Select(i => $"${i:x2}"))}");

                    size += env.Length;
                }

                lines.Add("");
            }

            return size;
        }

        private int OutputSamples(string filename, string dmcFilename)
        {
            var samplesSize = 0;

            if (project.UsesSamples)
            {
                var sampleData = project.GetPackedSampleData();

                // TODO: Once we have a real project name, we will use that.
                var path = Path.GetDirectoryName(filename);
                var projectname = Utils.MakeNiceAsmName(project.Name);

                if (dmcFilename == null)
                    dmcFilename = Path.Combine(path, projectname + ".dmc");

                File.WriteAllBytes(dmcFilename, sampleData);

                samplesSize = sampleData.Length;
            }

            return samplesSize;
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
                if (value != 0 && channel != ChannelType.Noise) value = Math.Max(1, value - 12); 
                return (byte)(((value & 63) << 1) | numNotes);
            }
            else
            {
                // 0 = stop, 1 = C0 ... 96 = B7
                if (value != 0)
                {
                    Debug.Assert(Note.IsMusicalNote(value));

                    if (channel == ChannelType.Dpcm)
                    {
                        value = Utils.Clamp(value - Note.DPCMNoteMin, 1, 63);
                    }
                    else
                    {
                        value = Utils.Clamp(value, 1, 96);

                        if (singleByte)
                        {
                            Debug.Assert(value > SingleByteNoteMin && value <= SingleByteNoteMax);
                            value -= SingleByteNoteMin;
                        }
                    }
                }

                return (byte)(value);
            }
        }

        private List<string> GetSongData(Song song, int songIdx, int speedChannel)
        {
            var songData = new List<string>();
            var emptyPattern = new Pattern(-1, song, 0, "");
            var emptyNote = new Note(Note.NoteInvalid);

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

	            songData.Add($"{ll}song{songIdx}ch{c}:");

                if (isSpeedChannel && project.UsesFamiTrackerTempo)
                {
                    songData.Add($"${(kernel == FamiToneKernel.FamiStudio ? OpcodeSpeed : OpcodeSpeedFT2):x2}+");
                    songData.Add($"${song.FamitrackerSpeed:x2}");
                }

                for (int p = 0; p < song.Length; p++)
                {
                    var pattern = channel.PatternInstances[p] == null ? emptyPattern : channel.PatternInstances[p];

                    if (p == song.LoopPoint)
                    {
                        songData.Add($"{ll}song{songIdx}ch{c}loop:");

                        // Clear stored instrument to force a reset. We might be looping
                        // to a section where the instrument was set from a previous pattern.
                        instrument = null;
                        arpeggio = null;
                        lastVolume = -1;

                        if (sawVolumeChanged)
                            sawVolume = -1;

                        // If this channel potentially uses any arpeggios, clear the override since the last
                        // note may have overridden it. TODO: Actually check if thats the case!
                        if (channel.UsesArpeggios)
                            songData.Add($"${OpcodeClearArpEnvOverride:x2}+");
                    }

                    if (isSpeedChannel && project.UsesFamiStudioTempo)
                    {
                        var groove = song.GetPatternGroove(p);
                        var groovePadMode = song.GetPatternGroovePaddingMode(p);

                        // If the groove changes or we are at the loop point, set the tempo envelope again.
                        if (Utils.CompareArrays(groove, previousGroove) != 0 || groovePadMode != previousGroovePadMode || p == song.LoopPoint)
                        {
                            var grooveName = GetGrooveAsmName(groove, groovePadMode);

                            songData.Add($"${OpcodeSetTempoEnv:x2}+");
                            songData.Add($"{lo}({ll}tempo_env_{grooveName})");
                            songData.Add($"{hi}({ll}tempo_env_{grooveName})");
                            previousGroove = groove;
                            previousGroovePadMode = groovePadMode;
                        }
                        else if (p != 0)
                        {
                            // Otherwise just reset it so that it realigns to the groove.
                            songData.Add($"${OpcodeResetTempoEnv:x2}+");
                        }
                    }

                    var patternLength = song.GetPatternLength(p); 

                    for (var it = pattern.GetDenseNoteIterator(0, patternLength); !it.Done; )
                    {
                        var time = it.CurrentTime;
                        var note = it.CurrentNote;

                        if (note == null)
                            note = emptyNote;

                        // We don't allow delaying speed effect at the moment.
                        if (isSpeedChannel && song.UsesFamiTrackerTempo)
                        {
                            var speed = FindEffectParam(song, p, time, Note.EffectSpeed);
                            if (speed >= 0)
                            {
                                currentSpeed = speed;
                                songData.Add($"${OpcodeSpeed:x2}+");
                                songData.Add($"${(byte)speed:x2}");
                            }
                        }

                        if (OtherVrc7ChannelUsesCustomPatch(song, channel, instrument, p, time))
                        {
                            instrument = null;
                        }

                        it.Next();

                        if (note.HasNoteDelay)
                        {
                            songData.Add($"${OpcodeDelayedNote:x2}+");
                            songData.Add($"${note.NoteDelay - 1:x2}");
                            usesDelayedNotesOrCuts = true;
                        }

                        if (note.HasVolume)
                        {
                            if (note.Volume != lastVolume)
                            {
                                songData.Add($"${(byte)(OpcodeVolumeBits | note.Volume):x2}+");
                                lastVolume = note.Volume;
                            }

                            usesVolumeTrack = true;

                            if (note.HasVolumeSlide)
                            {
                                var location = new NoteLocation(p, time);
                                channel.ComputeVolumeSlideNoteParams(note, location, currentSpeed, false, out var stepSizeNtsc, out var _);
                                channel.ComputeVolumeSlideNoteParams(note, location, currentSpeed, false, out var stepSizePal,  out var _);

                                if (song.Project.UsesAnyExpansionAudio || machine == MachineType.NTSC)
                                    stepSizePal = stepSizeNtsc;
                                else if (machine == MachineType.PAL)
                                    stepSizeNtsc = stepSizePal;

                                var stepSize = Math.Max(Math.Abs(stepSizeNtsc), Math.Abs(stepSizePal)) * Math.Sign(stepSizeNtsc);
                                songData.Add($"${OpcodeVolumeSlide:x2}+");
                                songData.Add($"${(byte)stepSize:x2}");
                                songData.Add($"${note.VolumeSlideTarget << 4:x2}");

                                lastVolume = note.VolumeSlideTarget;
                                usesVolumeSlide = true;
                            }
                        }

                        if (note.HasFinePitch)
                        {
                            songData.Add($"${OpcodeFinePitch:x2}+");
                            songData.Add($"${note.FinePitch:x2}");
                            usesPitchTrack = true;
                        }

                        if (note.HasVibrato)
                        {
                            // TODO: If note has attack, no point in setting the default vibrato envelope, instrument will do it anyway.
                            songData.Add($"${OpcodeOverridePitchEnv:x2}+");
                            songData.Add($"{lo}({vibratoEnvelopeNames[note.RawVibrato]})");
                            songData.Add($"{hi}({vibratoEnvelopeNames[note.RawVibrato]})");

                            // MATTT : Why do we do that right after?
                            if (note.RawVibrato == 0)
                                songData.Add($"${OpcodeClearPitchEnvOverride:x2}+");

                            usesVibrato = true;
                        }

                        if (note.IsMusical)
                        {
                            // Set/clear override when changing arpeggio
                            if (note.Arpeggio != arpeggio)
                            {
                                if (note != null || !note.HasAttack)
                                {
                                    songData.Add($"${OpcodeOverridArpEnv:x2}+");

                                    if (note.Arpeggio == null)
                                    {
                                        songData.Add($"{lo}({noArpeggioEnvelopeName})");
                                        songData.Add($"{hi}({noArpeggioEnvelopeName})");
                                    }
                                    else
                                    {
                                        songData.Add($"{lo}({arpeggioEnvelopeNames[note.Arpeggio]})");
                                        songData.Add($"{hi}({arpeggioEnvelopeNames[note.Arpeggio]})");
                                    }
                                }

                                // MATTT : Shouldnt we only do that when turning off the arp?
                                if (note.Arpeggio == null)
                                    songData.Add($"${OpcodeClearArpEnvOverride:x2}+");

                                arpeggio = note.Arpeggio;
                                usesArpeggio = true;
                            }
                            // If same arpeggio, but note has an attack, reset it.
                            else if (note.HasAttack && arpeggio != null)
                            {
                                songData.Add($"${OpcodeResetArpEnv:x2}+");
                            }
                        }

                        if (note.HasDutyCycle)
                        {
                            songData.Add($"${OpcodeDutyCycle:x2}+");
                            songData.Add($"${note.DutyCycle:x2}");
                            usesDutyCycleEffect = true;
                        }

                        if (note.HasFdsModSpeed)
                        {
                            songData.Add($"${OpcodeFdsModSpeed:x2}+");
                            songData.Add($"${(note.FdsModSpeed >> 0) & 0xff:x2}");
                            songData.Add($"${(note.FdsModSpeed >> 8) & 0xff:x2}");
                        }

                        if (note.HasFdsModDepth)
                        {
                            songData.Add($"${OpcodeFdsModDepth:x2}+");
                            songData.Add($"${note.FdsModDepth:x2}");
                        }

                        if (note.HasCutDelay)
                        {
                            songData.Add($"${OpcodeDelayedCut:x2}+");
                            songData.Add($"${note.CutDelay:x2}");
                            usesDelayedNotesOrCuts = true;
                        }

                        if (note.HasDeltaCounter)
                        {
                            // Use hi-bit to flag if we need to apply it immediately (no samples playing this frame)
                            //or a bit later (when playing the sample, overriding the initial DMC value).
                            songData.Add($"${OpcodeDeltaCounter:x2}+");
                            songData.Add($"${((note.IsMusical ? 0x00 : 0x80) | (note.DeltaCounter)):x2}");
                            usesDeltaCounter = true;
                        }

                        if (note.IsValid)
                        {
                            // Instrument change.
                            if (note.IsMusical && note.Instrument != null)
                            {
                                if (note.Instrument != instrument)
                                {
                                    // Change saw volume if needed.
                                    if (channel.Type == ChannelType.Vrc6Saw && sawVolume != note.Instrument.Vrc6SawMasterVolume)
                                    {
                                        sawVolume = note.Instrument.Vrc6SawMasterVolume;
                                        sawVolumeChanged = true;

                                        songData.Add($"${OpcodeVrc6SawMasterVolume:x2}+");
                                        songData.Add($"${1 - sawVolume:x2}");
                                    }

                                    int idx = instrumentIndices[note.Instrument];
                                    songData.Add($"${(byte)(0x80 | (idx << 1)):x2}+");
                                    instrument = note.Instrument;
                                }
                                else if(!note.HasAttack)
                                {
                                    // TODO: Remove note entirely after a slide that matches the next note with no attack.
                                    songData.Add($"${OpcodeDisableAttack:x2}+");
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
                                var noteTableNtsc = NesApu.GetNoteTableForChannelType(channel.Type, false, song.Project.ExpansionNumN163Channels);
                                var noteTablePal  = NesApu.GetNoteTableForChannelType(channel.Type, true,  song.Project.ExpansionNumN163Channels);

                                var found = true;
                                var location = new NoteLocation(p, time);
                                found &= channel.ComputeSlideNoteParams(note, location, currentSpeed, noteTableNtsc, false, true, out _, out int stepSizeNtsc, out _);
                                found &= channel.ComputeSlideNoteParams(note, location, currentSpeed, noteTablePal,  true,  true, out _, out int stepSizePal,  out _);

                                if (song.Project.UsesAnyExpansionAudio || machine == MachineType.NTSC)
                                    stepSizePal = stepSizeNtsc;
                                else if (machine == MachineType.PAL)
                                    stepSizeNtsc = stepSizePal;

                                if (found)
                                {
                                    // Take the (signed) maximum of both notes so that we are garantee to reach our note.
                                    var stepSize = Math.Max(Math.Abs(stepSizeNtsc), Math.Abs(stepSizePal)) * Math.Sign(stepSizeNtsc);
                                    songData.Add($"${OpcodeSlide:x2}+");
                                    songData.Add($"${(byte)stepSize:x2}");
                                    songData.Add($"${EncodeNoteValue(c, note.Value):x2}");
                                    songData.Add($"${EncodeNoteValue(c, note.SlideNoteTarget):x2}*");
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
                                    switch (channel.Expansion)
                                    {
                                        case ExpansionType.Vrc7: opcode = OpcodeVrc7ReleaseNote; break;
                                        case ExpansionType.Fds:  opcode = OpcodeFdsReleaseNote;  break;
                                        case ExpansionType.N163: opcode = OpcodeN163ReleaseNote; break;
                                        case ExpansionType.EPSM: opcode = OpcodeEpsmReleaseNote; break;
                                    }

                                    songData.Add($"${opcode:x2}+*");
                                    usesReleaseNotes = true;
                                }
                                else
                                {
                                    var requiresExtendedNote = kernel == FamiToneKernel.FamiStudio && note.IsMusical && (note.Value <= SingleByteNoteMin || note.Value > SingleByteNoteMax);

                                    // The valid range of DPCM sample should perfectly match the single-byte note range.
                                    Debug.Assert(kernel != FamiToneKernel.FamiStudio || !channel.IsDpcmChannel || !requiresExtendedNote);

                                    // We encode very common notes [C1 - G7] with a single bytes and emit a special
                                    // "extended note" opcode when it falls outside of that range.
                                    if (requiresExtendedNote)
                                    {
                                        songData.Add($"${OpcodeExtendedNote:x2}+");
                                        songData.Add($"${EncodeNoteValue(c, note.Value, false):x2}*");
                                    }
                                    else
                                    {
                                        songData.Add($"${EncodeNoteValue(c, note.Value, true, numNotes):x2}+*");
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
                                    note.IsValid         ||
                                    note.HasVolume       || 
                                    note.HasVibrato      ||
                                    note.HasFinePitch    ||
                                    note.HasDutyCycle    ||
                                    note.HasFdsModSpeed  || 
                                    note.HasFdsModDepth  ||
                                    note.HasNoteDelay    ||
                                    note.HasCutDelay     ||
                                    note.HasDeltaCounter ||
                                    (isSpeedChannel && FindEffectParam(song, p, time, Note.EffectSpeed) >= 0))
                                {
                                    break;
                                }

                                numEmptyNotes++;
                                it.Next();
                            }

                            songData.Add($"${(byte)(0x81 | (numEmptyNotes << 1)):x2}+*");
                        }
                    }
                }

                if (song.LoopPoint < 0)
                {
                    songData.Add($"{ll}song{songIdx}ch{c}loop:");
                    songData.Add($"${EncodeNoteValue(c, Note.NoteStop):x2}");
                }

                songData.Add($"${(kernel == FamiToneKernel.FamiStudio ? OpcodeLoop : OpcodeLoopFT2):x2}");
                songData.Add($"{ll}song{songIdx}ch{c}loop");
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

                                compressedData.Add($"${(kernel == FamiToneKernel.FamiStudio ? OpcodeSetReference : OpcodeSetReferenceFT2):x2}");
                                compressedData.Add($"${bestPatternNumNotes:x2}");
                                compressedData.Add($"{ll}song{songIdx}ref{bestPatternIdx}");

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
                        lines.Add($"{ll}song{songIdx}ref{i}:");
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

            Debug.Assert(byteString == null);

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

            // Try compression with various threshold for jump to ref.
            var bestSize = int.MaxValue;
            var bestMinNotesForJump = 0;

            for (int i = 8; i <= 40; i++)
            {
                var size = CompressAndOutputSongData(songData, songIdx, i, false);
#if DEBUG
                Log.LogMessage(LogSeverity.Info, $"Compression with a match of {i} notes = {size} bytes.");
#endif

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
        
        private void SetupFormat(int format)
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
                    break;
                case AssemblyFormat.CA65:
                    db = ".byte";
                    dw = ".word";
                    ll = "@";
                    lo =  ".lobyte";
                    hi =  ".hibyte";
                    break;
                case AssemblyFormat.ASM6:
                    db = "db";
                    dw = "dw";
                    ll = "@";
                    lo = "<";
                    hi = ">";
                    break;
            }
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

                project.SetExpansionAudioMask(ExpansionType.NoneMask);
            }
        }

        private void SetupProject(Project originalProject, int[] songIds)
        {
            // Work on a temporary copy.
            project = originalProject.DeepClone();
            project.Filename = originalProject.Filename;
            project.ConvertToSimpleNotes();

            if (kernel == FamiToneKernel.FamiTone2 && project.UsesFamiStudioTempo)
            {
                project.ConvertToFamiTrackerTempo(false);
            }

            // NULL = All songs.
            if (songIds != null)
            {
                for (int i = 0; i < project.Songs.Count; i++)
                {
                    if (!songIds.Contains(project.Songs[i].Id))
                    {
                        project.DeleteSong(project.Songs[i]);
                        i--;
                    }
                }
            }

            RemoveUnsupportedFeatures();
            project.DeleteUnusedInstruments();

            if (project.UsesFamiStudioTempo)
            {
                foreach (var song in project.Songs)
                    song.PermanentlyApplyGrooves();
            }

            if (project.UsesMultipleExpansionAudios)
            {
                Debug.Assert(kernel == FamiToneKernel.FamiStudio);

                if (project.UsesEPSMExpansion)
                    project.SetExpansionAudioMask(ExpansionType.AllMask, 8);
                else
                    project.SetExpansionAudioMask(ExpansionType.AllMask & ~(ExpansionType.EPSMMask), 8);
            }
        }

        private void OutputIncludeFile(string includeFilename)
        {
            var includeLines = new List<string>();

            for (int songIdx = 0; songIdx < project.Songs.Count; songIdx++)
            {
                var song = project.Songs[songIdx];
                includeLines.Add($"song_{Utils.MakeNiceAsmName(song.Name)} = {songIdx}");
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

        public bool Save(Project originalProject, int[] songIds, int format, bool separateSongs, string filename, string dmcFilename, string includeFilename, int machine)
        {
            this.machine = machine;
            SetupProject(originalProject, songIds);
            SetupFormat(format);
            CleanupEnvelopes();

            var dmcSize    = OutputSamples(filename, dmcFilename);
            var headerSize = OutputHeader(separateSongs);
            var instSize   = OutputInstruments();
            var tempoSize  = OutputTempoEnvelopes();

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

            File.WriteAllLines(filename, lines);

            if (includeFilename != null)
            {
                OutputIncludeFile(includeFilename);
            }

            if (log)
            {
                Log.LogMessage(LogSeverity.Info, $"Total assembly file size: {headerSize + instSize + tempoSize + totalSongsSize} bytes.");

                if (project.UsesSamples)
                    Log.LogMessage(LogSeverity.Info, $"Total dmc file size: {dmcSize} bytes.");

                if (kernel == FamiToneKernel.FamiStudio)
                {
                    // Expansion defines
                    if (project.UsesVrc6Expansion)
                        Log.LogMessage(LogSeverity.Info, "Project uses VRC6 expansion, you must set FAMISTUDIO_EXP_VRC6 = 1.");
                    if (project.UsesVrc7Expansion)
                        Log.LogMessage(LogSeverity.Info, "Project uses VRC7 expansion, you must set FAMISTUDIO_EXP_VRC7 = 1.");
                    if (project.UsesMmc5Expansion)
                        Log.LogMessage(LogSeverity.Info, "Project uses MMC5 expansion, you must set FAMISTUDIO_EXP_MMC5 = 1.");
                    if (project.UsesS5BExpansion)
                        Log.LogMessage(LogSeverity.Info, "Project uses S5B expansion, you must set FAMISTUDIO_EXP_S5B = 1.");
                    if (project.UsesFdsExpansion)
                        Log.LogMessage(LogSeverity.Info, "Project uses FDS expansion, you must set FAMISTUDIO_EXP_FDS = 1.");
                    if (project.UsesN163Expansion)
                        Log.LogMessage(LogSeverity.Info, $"Project uses N163 expansion, you must set FAMISTUDIO_EXP_N163 = 1 and FAMISTUDIO_EXP_N163_CHN_CNT = {project.ExpansionNumN163Channels}.");
                    if (project.UsesEPSMExpansion)
                        Log.LogMessage(LogSeverity.Info, "Project uses EPSM expansion, you must set FAMISTUDIO_EXP_EPSM = 1.");
                    
                    // Feature usage defines.
                    if (usesFamiTrackerTempo)
                        Log.LogMessage(LogSeverity.Info, "Project uses FamiTracker tempo, you must set FAMISTUDIO_USE_FAMITRACKER_TEMPO = 1.");
                    if (usesDelayedNotesOrCuts)
                        Log.LogMessage(LogSeverity.Info, "Project uses delayed notes or cuts, you must set FAMISTUDIO_USE_FAMITRACKER_DELAYED_NOTES_OR_CUTS = 1.");
                    if (usesReleaseNotes)
                        Log.LogMessage(LogSeverity.Info, "Project uses release notes, you must set FAMISTUDIO_USE_RELEASE_NOTES = 1.");
                    if (usesVolumeTrack)
                        Log.LogMessage(LogSeverity.Info, "Volume track is used, you must set FAMISTUDIO_USE_VOLUME_TRACK = 1.");
                    if (usesVolumeSlide)
                        Log.LogMessage(LogSeverity.Info, "Volume slides are used, you must set FAMISTUDIO_USE_VOLUME_SLIDES = 1.");
                    if (usesPitchTrack)
                        Log.LogMessage(LogSeverity.Info, "Fine pitch track is used, you must set FAMISTUDIO_USE_PITCH_TRACK = 1.");
                    if (usesSlideNotes)
                        Log.LogMessage(LogSeverity.Info, "Slide notes are used, you must set FAMISTUDIO_USE_SLIDE_NOTES = 1.");
                    if (usesNoiseSlideNotes)
                        Log.LogMessage(LogSeverity.Info, "Slide notes are used on the noise channel, you must set FAMISTUDIO_USE_NOISE_SLIDE_NOTES = 1.");
                    if (usesVibrato)
                        Log.LogMessage(LogSeverity.Info, "Vibrato effect is used, you must set FAMISTUDIO_USE_VIBRATO = 1.");
                    if (usesArpeggio)
                        Log.LogMessage(LogSeverity.Info, "Arpeggios are used, you must set FAMISTUDIO_USE_ARPEGGIO = 1.");
                    if (usesDutyCycleEffect)
                        Log.LogMessage(LogSeverity.Info, "Duty Cycle effect is used, you must set FAMISTUDIO_USE_DUTYCYCLE_EFFECT = 1.");
                    if (usesDeltaCounter)
                        Log.LogMessage(LogSeverity.Info, "DPCM Delta Counter effect is used, you must set FAMISTUDIO_USE_DELTA_COUNTER = 1.");
                }
            }

#if DEBUG
            Debug.Assert(GetAsmFileSize(lines) == headerSize + instSize + tempoSize + totalSongsSize);
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
                if (pair.Item1.Contains("-"))
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
        public byte[] GetBytes(Project project, int[] songIds, int songOffset, int dpcmOffset, int machine)
        {
            var tempFolder = Utils.GetTemporaryDiretory();
            var tempAsmFilename = Path.Combine(tempFolder, "nsf.asm");
            var tempDmcFilename = Path.Combine(tempFolder, "nsf.dmc");

            Save(project, songIds, AssemblyFormat.ASM6, false, tempAsmFilename, tempDmcFilename, null, machine);

            return ParseAsmFile(tempAsmFilename, songOffset, dpcmOffset);
        }
    }

    public static class AssemblyFormat
    {
        public const int NESASM = 0;
        public const int CA65   = 1;
        public const int ASM6   = 2;

        public static readonly string[] Names =
        {
            "NESASM",
            "CA65",
            "ASM6"
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

}
