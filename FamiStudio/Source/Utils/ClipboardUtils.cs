using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;

namespace FamiStudio
{
    public static class ClipboardUtils
    {
        const uint MagicNumberClipboardNotes    = 0x214E4346; // FCN!
        const uint MagicNumberClipboardEnvelope = 0x21454346; // FCE!
        const uint MagicNumberClipboardPatterns = 0x21504346; // FCP!

        readonly static char[] Seperators = new[] { ' ', '\t', '\r', '\n', ',', ';', '\0' };

        private static byte[] GetClipboardDataInternal(uint magic, int maxSize = int.MaxValue)
        {
            var buffer = Platform.GetClipboardData(maxSize);

            if (buffer == null || BitConverter.ToUInt32(buffer, 0) != magic)
                return null;

            return buffer;
        }

        public static bool ContainsNotes    => GetClipboardDataInternal(MagicNumberClipboardNotes,    4) != null;
        public static bool ContainsEnvelope => GetClipboardDataInternal(MagicNumberClipboardEnvelope, 4) != null || GetStringEnvelopeData() != null;
        public static bool ContainsPatterns => GetClipboardDataInternal(MagicNumberClipboardPatterns, 4) != null;

        public static sbyte[] GetStringEnvelopeData()
        {
            var str = Platform.GetClipboardString();

            if (!string.IsNullOrEmpty(str))
            {
                var values = new List<sbyte>();
                var splits = str.Split(Seperators, StringSplitOptions.RemoveEmptyEntries);
                foreach (var split in splits)
                {
                    if (int.TryParse(split, out var i))
                        values.Add((sbyte)Utils.Clamp(i, sbyte.MinValue, sbyte.MaxValue));
                    else
                        return null;
                }

                return values.Count > 0 ? values.ToArray() : null;
            }

            return null;
        }

        private static void SavePatternList(ProjectSaveBuffer serializer, ICollection<Pattern> patterns)
        {
            int numUniquePatterns = patterns.Count;
            serializer.Serialize(ref numUniquePatterns);

            if (numUniquePatterns > 0)
            {
                // Save the original song id so we can patch it when pasting between projects.
                foreach (var pat in patterns)
                {
                    int songId = pat.Song.Id;
                    serializer.Serialize(ref songId);
                    break;
                }

                foreach (var pat in patterns)
                {
                    var patId = pat.Id;
                    var patChannel = pat.ChannelType;
                    var patCrc = pat.ComputeCRC();
                    var patName = pat.Name;
                    serializer.Serialize(ref patId);
                    serializer.Serialize(ref patChannel);
                    serializer.Serialize(ref patCrc);
                    serializer.Serialize(ref patName);
                }

                foreach (var pat in patterns)
                {
                    pat.Serialize(serializer);
                }
            }
        }

        private static ClipboardContentFlags LoadAndMergePatternList(ProjectLoadBuffer serializer, Song song, bool checkOnly, ClipboardImportFlags importFlags = ClipboardImportFlags.CreateMissing)
        {
            var numPatterns = 0;
            serializer.Serialize(ref numPatterns);

            var flags = ClipboardContentFlags.None;

            if (numPatterns > 0)
            {
                // Remap whatever original song we had to the current one.
                var songId = -1;
                serializer.Serialize(ref songId);
                serializer.RemapId(songId, song.Id);

                var patternList = new List<(int, int, uint, string)>();
                for (int i = 0; i < numPatterns; i++)
                {
                    var patId = 0;
                    var patChannel = 0;
                    var patCrc = 0u;
                    var patName = "";
                    serializer.Serialize(ref patId);
                    serializer.Serialize(ref patChannel);
                    serializer.Serialize(ref patCrc);
                    serializer.Serialize(ref patName);
                    patternList.Add((patId, patChannel, patCrc, patName));
                }

                var dummyPattern = new Pattern();

                // Match patterns by name, create missing ones and remap IDs.
                for (int i = 0; i < numPatterns; i++)
                {
                    var patId      = patternList[i].Item1;
                    var patChannel = patternList[i].Item2;
                    var patCrc     = patternList[i].Item3;
                    var patName    = patternList[i].Item4;

                    if (serializer.Project.IsChannelActive(patChannel))
                    {
                        var existingPattern = song.GetChannelByType(patChannel).GetPattern(patName);
                        var crcMatch = existingPattern == null || patCrc == existingPattern.ComputeCRC();

                        if (existingPattern != null && (checkOnly || importFlags.HasFlag(ClipboardImportFlags.MatchByName) || crcMatch))
                        {
                            if (checkOnly && !crcMatch)
                            {
                                flags |= ClipboardContentFlags.NameConflict;
                            }

                            serializer.RemapId(patId, existingPattern.Id);
                            dummyPattern.Serialize(serializer); // Skip 
                        }
                        else
                        {
                            if (!checkOnly)
                            {
                                var chan = song.GetChannelByType(patChannel);

                                if (existingPattern != null)
                                {
                                    patName = chan.GenerateUniquePatternNameSmart(patName);
                                }

                                var pattern = chan.CreatePattern(patName);
                                serializer.RemapId(patId, pattern.Id);
                                pattern.Serialize(serializer);
                                pattern.Name = patName;
                            }
                            else
                            {
                                serializer.RemapId(patId, -1);
                                dummyPattern.Serialize(serializer); // Skip
                            }
                        }
                    }
                    else
                    {
                        serializer.RemapId(patId, -1);
                        dummyPattern.Serialize(serializer); // Skip 
                    }
                }
            }

            return flags;
        }

        private static void SaveArpeggioList(ProjectSaveBuffer serializer, ICollection<Arpeggio> arpeggios)
        {
            int numArpeggios = arpeggios.Count;
            serializer.Serialize(ref numArpeggios);

            foreach (var arp in arpeggios)
            {
                var arpId = arp.Id;
                var arpCrc = arp.ComputeCRC(); 
                var arpName = arp.Name;
                serializer.Serialize(ref arpId);
                serializer.Serialize(ref arpCrc);
                serializer.Serialize(ref arpName);
            }

            foreach (var arp in arpeggios)
            {
                arp.Serialize(serializer);
            }
        }

        private static void SaveInstrumentList(ProjectSaveBuffer serializer, ICollection<Instrument> instruments)
        {
            int numInstruments = instruments.Count;
            serializer.Serialize(ref numInstruments);

            foreach (var inst in instruments)
            {
                var instId = inst.Id;
                var instType = inst.Expansion;
                var instCrc = inst.ComputeCRC();
                var instName = inst.Name;
                serializer.Serialize(ref instId);
                serializer.Serialize(ref instType);
                serializer.Serialize(ref instCrc);
                serializer.Serialize(ref instName);
            }

            foreach (var inst in instruments)
            {
                inst.Serialize(serializer);
            }
        }

        private static void SaveSampleList(ProjectSaveBuffer serializer, HashSet<Instrument> instruments)
        {
            var samples = new HashSet<DPCMSample>();

            foreach (var inst in instruments)
            {
                if (inst.HasAnyMappedSamples)
                {
                    foreach (var kv in inst.SamplesMapping)
                    {
                        samples.Add(kv.Value.Sample);
                    }
                }
            }

            int numSamples = samples.Count;
            serializer.Serialize(ref numSamples);

            foreach (var sample in samples)
            {
                var sampleId = sample.Id;
                var sampleCrc = sample.ComputeCRC();
                var sampleName = sample.Name;

                serializer.Serialize(ref sampleId);
                serializer.Serialize(ref sampleCrc);
                serializer.Serialize(ref sampleName);
            }

            foreach (var sample in samples)
            {
                sample.Serialize(serializer);
            }
        }

        private static ClipboardContentFlags LoadAndMergeSampleList(ProjectLoadBuffer serializer, bool checkOnly = false, ClipboardImportFlags importFlags = ClipboardImportFlags.CreateMissing)
        {
            var numSamples = 0;
            serializer.Serialize(ref numSamples);

            var flags = ClipboardContentFlags.None;
            var dummySample = new DPCMSample();

            var sampleList = new List<(int, uint, string)>();
            for (int i = 0; i < numSamples; i++)
            {
                var sampleId = 0;
                var sampleCrc = 0u;
                var sampleName = "";

                serializer.Serialize(ref sampleId);
                serializer.Serialize(ref sampleCrc);
                serializer.Serialize(ref sampleName);

                sampleList.Add((sampleId, sampleCrc, sampleName));
            }

            for (int i = 0; i < numSamples; i++)
            {
                var sampleId   = sampleList[i].Item1;
                var sampleCrc  = sampleList[i].Item2;
                var sampleName = sampleList[i].Item3;

                var existingSample = serializer.Project.GetSample(sampleName);
                var crcMatch = existingSample == null || existingSample.ComputeCRC() == sampleCrc;

                if (existingSample != null && (checkOnly || importFlags.HasFlag(ClipboardImportFlags.MatchByName) || crcMatch))
                {
                    if (checkOnly && !crcMatch)
                    {
                        flags |= ClipboardContentFlags.NameConflict;
                    }

                    serializer.RemapId(sampleId, existingSample.Id);
                    dummySample.Serialize(serializer); // Skip
                }
                else
                {
                    flags |= ClipboardContentFlags.ContainsMissing;

                    if (!checkOnly && (importFlags.HasFlag(ClipboardImportFlags.CreateMissing) || existingSample != null))
                    {
                        if (existingSample != null)
                        {
                            sampleName = serializer.Project.GenerateUniqueDPCMSampleName(sampleName);
                        }

                        var sample = serializer.Project.CreateDPCMSample(sampleName);
                        serializer.RemapId(sampleId, sample.Id);
                        sample.Serialize(serializer);
                        sample.Name = sampleName;
                    }
                    else
                    {
                        serializer.RemapId(sampleId, -1);
                        dummySample.Serialize(serializer); // Skip
                    }
                }
            }

            serializer.Project.EnsureAllFoldersExist(FolderType.Sample);
            serializer.Project.ConditionalSortSamples();

            return flags;
        }

        private static ClipboardContentFlags LoadAndMergeInstrumentList(ProjectLoadBuffer serializer, bool checkOnly = false, ClipboardImportFlags importFlags = ClipboardImportFlags.CreateMissing)
        {
            int numInstruments = 0;
            serializer.Serialize(ref numInstruments);

            var instrumentList = new List<(int, int, uint, string)>();
            for (int i = 0; i < numInstruments; i++)
            {
                var instId = 0;
                var instType = 0;
                var instCrc = 0u;
                var instName = "";
                serializer.Serialize(ref instId);
                serializer.Serialize(ref instType);
                serializer.Serialize(ref instCrc);
                serializer.Serialize(ref instName);
                instrumentList.Add((instId, instType, instCrc, instName));
            }

            var dummyInstrument = new Instrument();
            var contentFlags = ClipboardContentFlags.None;

            // Match instruments by name, create missing ones and remap IDs.
            for (int i = 0; i < numInstruments; i++)
            {
                var instId   = instrumentList[i].Item1;
                var instType = instrumentList[i].Item2;
                var instCrc  = instrumentList[i].Item3;
                var instName = instrumentList[i].Item4;

                if (instType == ExpansionType.None || serializer.Project.UsesExpansionAudio(instType))
                {
                    var existingInstrument = serializer.Project.GetInstrument(instName);
                    var crcMatch = existingInstrument == null || instCrc == existingInstrument.ComputeCRC();

                    if (existingInstrument != null && (checkOnly || importFlags.HasFlag(ClipboardImportFlags.MatchByName) || crcMatch))
                    {
                        if (checkOnly && !crcMatch)
                        {
                            contentFlags |= ClipboardContentFlags.NameConflict;
                        }

                        if (existingInstrument.Expansion == instType)
                            serializer.RemapId(instId, existingInstrument.Id);
                        else
                            serializer.RemapId(instId, -1); // Incompatible expansion type, skip.

                        dummyInstrument.Serialize(serializer); // Skip
                    }
                    else
                    {
                        contentFlags |= ClipboardContentFlags.ContainsMissing;

                        if (!checkOnly && (importFlags.HasFlag(ClipboardImportFlags.CreateMissing) || existingInstrument != null))
                        {
                            if (existingInstrument != null)
                            {
                                instName = serializer.Project.GenerateUniqueInstrumentName(instName);
                            }

                            var instrument = serializer.Project.CreateInstrument(instType, instName);
                            serializer.RemapId(instId, instrument.Id);
                            instrument.Serialize(serializer);
                            instrument.Name = instName;
                        }
                        else
                        {
                            serializer.RemapId(instId, -1);
                            dummyInstrument.Serialize(serializer); // Skip
                        }
                    }
                }
                else
                {
                    serializer.RemapId(instId, -1);
                    dummyInstrument.Serialize(serializer); // Skip
                }
            }

            serializer.Project.EnsureAllFoldersExist(FolderType.Instrument);
            serializer.Project.ConditionalSortInstruments();

            return contentFlags;
        }

        private static ClipboardContentFlags LoadAndMergeArpeggioList(ProjectLoadBuffer serializer, bool checkOnly = false, ClipboardImportFlags importFlags = ClipboardImportFlags.CreateMissing)
        {
            int numArpeggios = 0;
            serializer.Serialize(ref numArpeggios);

            var arpeggioList = new List<(int, uint, string)>();
            for (int i = 0; i < numArpeggios; i++)
            {
                var arpId = 0;
                var arpCrc = 0u;
                var arpName = "";
                serializer.Serialize(ref arpId);
                serializer.Serialize(ref arpCrc);
                serializer.Serialize(ref arpName);
                arpeggioList.Add((arpId, arpCrc, arpName));
            }

            var dummyArpeggio = new Arpeggio();
            var flags = ClipboardContentFlags.None;

            // Match arpeggios by name, create missing ones and remap IDs.
            for (int i = 0; i < numArpeggios; i++)
            {
                var arpId   = arpeggioList[i].Item1;
                var arpCrc  = arpeggioList[i].Item2;
                var arpName = arpeggioList[i].Item3;

                var existingArpeggio = serializer.Project.GetArpeggio(arpName);
                var crcMatch = existingArpeggio == null || arpCrc == existingArpeggio.ComputeCRC();

                if (existingArpeggio != null && (checkOnly || importFlags.HasFlag(ClipboardImportFlags.MatchByName) || crcMatch))
                {
                    if (checkOnly && !crcMatch)
                    {
                        flags |= ClipboardContentFlags.NameConflict;
                    }

                    serializer.RemapId(arpId, existingArpeggio.Id);
                    dummyArpeggio.Serialize(serializer); // Skip
                }
                else
                {
                    flags |= ClipboardContentFlags.ContainsMissing;

                    if (!checkOnly && (importFlags.HasFlag(ClipboardImportFlags.CreateMissing) || existingArpeggio != null))
                    {
                        if (existingArpeggio != null)
                        {
                            arpName = serializer.Project.GenerateUniqueArpeggioName(arpName);
                        }

                        var arp = serializer.Project.CreateArpeggio(arpName);
                        serializer.RemapId(arpId, arp.Id);
                        arp.Serialize(serializer);
                        arp.Name = arpName;
                    }
                    else
                    {
                        serializer.RemapId(arpId, -1);
                        dummyArpeggio.Serialize(serializer); // Skip
                    }
                }
            }

            serializer.Project.EnsureAllFoldersExist(FolderType.Arpeggio);
            serializer.Project.ConditionalSortArpeggios();

            return flags;
        }

        public static void SaveNotes(Project project, Note[] notes)
        {
            if (notes == null)
            {
                Platform.SetClipboardData(null);
                return;
            }

            var serializer = new ProjectSaveBuffer(null);
            var instruments = new HashSet<Instrument>();
            var arpeggios = new HashSet<Arpeggio>();

            foreach (var note in notes)
            {
                if (note != null)
                {
                    if (note.Instrument != null)
                        instruments.Add(note.Instrument);
                    if (note.Arpeggio != null)
                        arpeggios.Add(note.Arpeggio);
                }
            }

            SaveSampleList(serializer, instruments);
            SaveArpeggioList(serializer, arpeggios);
            SaveInstrumentList(serializer, instruments);

            var numNotes = notes.Length;
            serializer.Serialize(ref numNotes);
            foreach (var note in notes)
            {
                if (note == null)
                    Note.EmptyNote.Serialize(serializer);
                else
                    note.Serialize(serializer);
            }
            
            var buffer = Compression.CompressBytes(serializer.GetBuffer(), CompressionLevel.Fastest);
            var clipboardData = new List<byte>();
            clipboardData.AddRange(BitConverter.GetBytes(MagicNumberClipboardNotes));
            clipboardData.AddRange(buffer);

            Platform.SetClipboardData(clipboardData.ToArray());
        }

        public static bool ContainsMissingSamples(Project project, bool notes)
        {
            return false;
        }

        public static bool GetClipboardContentFlags(Song song, bool notes, out ClipboardContentFlags instFlags, out ClipboardContentFlags arpFlags, out ClipboardContentFlags sampleFlags, out ClipboardContentFlags patternFlags)
        {
            var buffer = GetClipboardDataInternal(notes ? MagicNumberClipboardNotes : MagicNumberClipboardPatterns);

            instFlags    = ClipboardContentFlags.None;
            arpFlags     = ClipboardContentFlags.None;
            sampleFlags  = ClipboardContentFlags.None;
            patternFlags = ClipboardContentFlags.None;

            if (buffer != null)
            {
                var serializer = new ProjectLoadBuffer(song.Project, Compression.DecompressBytes(buffer, 4), Project.Version, ProjectBufferFlags.Clipboard);

                sampleFlags = LoadAndMergeSampleList(serializer, true);
                arpFlags    = LoadAndMergeArpeggioList(serializer, true);
                instFlags   = LoadAndMergeInstrumentList(serializer, true);

                if (!notes)
                {
                    patternFlags = LoadAndMergePatternList(serializer, song, true);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static Note[] LoadNotes(Project project, ClipboardImportFlags instImportFlags, ClipboardImportFlags arpImportFlags, ClipboardImportFlags sampleImportFlags)
        {
            var buffer = GetClipboardDataInternal(MagicNumberClipboardNotes);

            if (buffer == null)
                return null;

            var serializer = new ProjectLoadBuffer(project, Compression.DecompressBytes(buffer, 4), Project.Version, ProjectBufferFlags.Clipboard);

            LoadAndMergeSampleList(serializer, false, sampleImportFlags);
            LoadAndMergeArpeggioList(serializer, false, arpImportFlags);
            LoadAndMergeInstrumentList(serializer, false, instImportFlags);
            
            int numNotes = 0;
            serializer.Serialize(ref numNotes);
            var notes = new Note[numNotes];
            for (int i = 0; i < numNotes; i++)
            {
                var note = new Note();
                note.Serialize(serializer);

                if (!note.IsEmpty)
                    notes[i] = note;
            }

            return notes;
        }

        public static void SaveEnvelopeValues(sbyte[] values)
        {
            var clipboardData = new List<byte>();
            clipboardData.AddRange(BitConverter.GetBytes(MagicNumberClipboardEnvelope));
            clipboardData.AddRange(BitConverter.GetBytes(values.Length));
            for (int i = 0; i < values.Length; i++)
                clipboardData.Add((byte)values[i]);

            Platform.ClearClipboardString();
            Platform.SetClipboardData(clipboardData.ToArray());
        }

        public static sbyte[] LoadEnvelopeValues()
        {
            var values = GetStringEnvelopeData();
            if (values != null)
                return values;

            var buffer = GetClipboardDataInternal(MagicNumberClipboardEnvelope);

            if (buffer == null)
                return null;

            var numValues = BitConverter.ToInt32(buffer, 4);

            values = new sbyte[numValues];
            for (int i = 0; i < numValues; i++)
                values[i] = (sbyte)buffer[8 + i];

            return values;
        }

        public static void SavePatterns(Project project, Pattern[,] patterns, Song.PatternCustomSetting[] customSettings = null)
        {
            if (patterns == null)
            {
                Platform.SetClipboardData(null);
                return;
            }

            var uniqueInstruments = new HashSet<Instrument>();
            var uniqueArpeggios = new HashSet<Arpeggio>();
            var uniquePatterns = new HashSet<Pattern>();
            var numPatterns = patterns.GetLength(0);
            var numChannels = patterns.GetLength(1);

            Debug.Assert(customSettings == null || customSettings.Length == numPatterns);

            for (int i = 0; i < numPatterns; i++)
            {
                for (int j = 0; j < numChannels; j++)
                {
                    var pattern = patterns[i, j];
                    if (pattern != null)
                    {
                        uniquePatterns.Add(pattern);

                        foreach (var n in pattern.Notes.Values)
                        {
                            if (n.Instrument != null)
                                uniqueInstruments.Add(n.Instrument);
                            if (n.Arpeggio != null)
                                uniqueArpeggios.Add(n.Arpeggio);
                        }
                    }
                }
            }

            //if (uniquePatterns.Count == 0)
            //{
            //    Platform.SetClipboardData(null);
            //    return;
            //}

            var serializer = new ProjectSaveBuffer(null);

            SaveSampleList(serializer, uniqueInstruments);
            SaveArpeggioList(serializer, uniqueArpeggios);
            SaveInstrumentList(serializer, uniqueInstruments);
            SavePatternList(serializer, uniquePatterns);

            serializer.Serialize(ref numPatterns);
            serializer.Serialize(ref numChannels);

            for (int i = 0; i < numPatterns; i++)
            {
                for (int j = 0; j < numChannels; j++)
                {
                    var pattern = patterns[i, j];
                    var patId = pattern == null ? -1 : pattern.Id;
                    serializer.Serialize(ref patId);
                }
            }

            var hasCustomSettings = customSettings != null;
            var tempoMode = project.TempoMode;

            serializer.Serialize(ref hasCustomSettings);
            serializer.Serialize(ref tempoMode);

            if (hasCustomSettings)
            {
                for (int i = 0; i < numPatterns; i++)
                {
                    serializer.Serialize(ref customSettings[i].useCustomSettings);
                    serializer.Serialize(ref customSettings[i].patternLength);
                    serializer.Serialize(ref customSettings[i].noteLength);
                    serializer.Serialize(ref customSettings[i].groove);
                    serializer.Serialize(ref customSettings[i].groovePaddingMode);
                    serializer.Serialize(ref customSettings[i].beatLength);
                }
            }

            var buffer = Compression.CompressBytes(serializer.GetBuffer(), CompressionLevel.Fastest);
            var clipboardData = new List<byte>();
            clipboardData.AddRange(BitConverter.GetBytes(MagicNumberClipboardPatterns));
            clipboardData.AddRange(buffer);

            Platform.SetClipboardData(clipboardData.ToArray());
        }

        // MATTT : Need a flag for patterns too!
        public static Pattern[,] LoadPatterns(Song song, ClipboardImportFlags instImportFlags, ClipboardImportFlags arpImportFlags, ClipboardImportFlags sampleImportFlags, ClipboardImportFlags patternImportFlags, out Song.PatternCustomSetting[] customSettings)
        {
            var buffer = GetClipboardDataInternal(MagicNumberClipboardPatterns);

            if (buffer == null)
            {
                customSettings = null;
                return null;
            }

            var decompressedBuffer = Compression.DecompressBytes(buffer, 4);
            var serializer = new ProjectLoadBuffer(song.Project, decompressedBuffer, Project.Version, ProjectBufferFlags.Clipboard);

            LoadAndMergeSampleList(serializer, false, sampleImportFlags);
            LoadAndMergeArpeggioList(serializer, false, arpImportFlags);
            LoadAndMergeInstrumentList(serializer, false, instImportFlags);
            LoadAndMergePatternList(serializer, song, false, patternImportFlags);

            int numPatterns = 0;
            int numChannels = 0;
            serializer.Serialize(ref numPatterns);
            serializer.Serialize(ref numChannels);

            var patterns = new Pattern[numPatterns, numChannels];

            for (int i = 0; i < numPatterns; i++)
            {
                for (int j = 0; j < numChannels; j++)
                {
                    var patId = -1;
                    serializer.Serialize(ref patId, true);
                    patterns[i, j] = patId == -1 ? null : song.GetPattern(patId);
                }
            }

            var tempoMode = TempoType.FamiStudio;
            var hasCustomSettings = false;
            serializer.Serialize(ref hasCustomSettings);
            serializer.Serialize(ref tempoMode);

            if (hasCustomSettings && tempoMode == song.Project.TempoMode)
            {
                customSettings = new Song.PatternCustomSetting[numPatterns];
                for (int i = 0; i < numPatterns; i++)
                {
                    customSettings[i] = new Song.PatternCustomSetting();
                    serializer.Serialize(ref customSettings[i].useCustomSettings);
                    serializer.Serialize(ref customSettings[i].patternLength);
                    serializer.Serialize(ref customSettings[i].noteLength);
                    serializer.Serialize(ref customSettings[i].groove);
                    serializer.Serialize(ref customSettings[i].groovePaddingMode);
                    serializer.Serialize(ref customSettings[i].beatLength);
                }
            }
            else
            {
                customSettings = null;
            }

            return patterns;
        }

        public static void Reset()
        {
            Platform.SetClipboardData(null);
        }
    }

    [Flags]
    public enum ClipboardImportFlags
    {
        None = 0,
        CreateMissing = 1,
        MatchByName = 2
    }

    [Flags]
    public enum ClipboardContentFlags
    {
        None = 0,
        ContainsMissing = 1,
        NameConflict = 2
    }
}
