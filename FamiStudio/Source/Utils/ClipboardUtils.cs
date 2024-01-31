using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;

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
            // Save the original song id so we can patch it when pasting between projects.
            foreach (var pat in patterns)
            {
                int songId = pat.Song.Id;
                serializer.Serialize(ref songId);
                break;
            }

            int numUniquePatterns = patterns.Count;
            serializer.Serialize(ref numUniquePatterns);

            foreach (var pat in patterns)
            {
                var patId = pat.Id;
                var patChannel = pat.ChannelType;
                var patName = pat.Name;
                serializer.Serialize(ref patId);
                serializer.Serialize(ref patChannel);
                serializer.Serialize(ref patName);
            }

            foreach (var pat in patterns)
                pat.Serialize(serializer);
        }

        private static int LoadAndMergePatternList(ProjectLoadBuffer serializer, Song song)
        {
            // Remap whatever original song we had to the current one.
            int songId = -1;
            serializer.Serialize(ref songId);
            serializer.RemapId(songId, song.Id);

            int numPatterns = 0;
            serializer.Serialize(ref numPatterns);

            var patternIdNameMap = new List<Tuple<int,int, string>>();
            for (int i = 0; i < numPatterns; i++)
            {
                var patId = 0;
                var patChannel = 0;
                var patName = "";
                serializer.Serialize(ref patId);
                serializer.Serialize(ref patChannel);
                serializer.Serialize(ref patName);
                patternIdNameMap.Add(new Tuple<int, int, string>(patId, patChannel, patName));
            }

            var dummyPattern = new Pattern();

            // Match patterns by name, create missing ones and remap IDs.
            for (int i = 0; i < numPatterns; i++)
            {
                var patId      = patternIdNameMap[i].Item1;
                var patChannel = patternIdNameMap[i].Item2;
                var patName    = patternIdNameMap[i].Item3;

                if (serializer.Project.IsChannelActive(patChannel))
                {
                    var existingPattern = song.GetChannelByType(patChannel).GetPattern(patName);

                    if (existingPattern != null)
                    {
                        serializer.RemapId(patId, existingPattern.Id);
                        dummyPattern.Serialize(serializer); // Skip 
                    }
                    else
                    {
                        var pattern = song.GetChannelByType(patChannel).CreatePattern(patName);
                        serializer.RemapId(patId, pattern.Id);
                        pattern.Serialize(serializer);
                    }
                }
                else
                {
                    serializer.RemapId(patId, -1);
                    dummyPattern.Serialize(serializer); // Skip 
                }
            }

            return numPatterns;
        }

        private static void SaveArpeggioList(ProjectSaveBuffer serializer, ICollection<Arpeggio> arpeggios)
        {
            int numArpeggios = arpeggios.Count;
            serializer.Serialize(ref numArpeggios);

            foreach (var arp in arpeggios)
            {
                var arpId = arp.Id;
                var arpName = arp.Name;
                serializer.Serialize(ref arpId);
                serializer.Serialize(ref arpName);
            }

            foreach (var arp in arpeggios)
                arp.Serialize(serializer);
        }

        private static void SaveInstrumentList(ProjectSaveBuffer serializer, ICollection<Instrument> instruments)
        {
            int numInstruments = instruments.Count;
            serializer.Serialize(ref numInstruments);

            foreach (var inst in instruments)
            {
                var instId = inst.Id;
                var instType = inst.Expansion;
                var instName = inst.Name;
                serializer.Serialize(ref instId);
                serializer.Serialize(ref instType);
                serializer.Serialize(ref instName);
            }

            foreach (var inst in instruments)
                inst.Serialize(serializer);
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
                var sampleName = sample.Name;

                serializer.Serialize(ref sampleId);
                serializer.Serialize(ref sampleName);

                sample.Serialize(serializer);
            }
        }

        private static bool LoadAndMergeSampleList(ProjectLoadBuffer serializer, bool checkOnly = false, bool createMissing = true)
        {
            int numSamples = 0;
            serializer.Serialize(ref numSamples);

            bool needMerge = false;
            var dummySample = new DPCMSample();

            for (int i = 0; i < numSamples; i++)
            {
                int sampleId = 0;
                string sampleName = "";

                serializer.Serialize(ref sampleId);
                serializer.Serialize(ref sampleName);

                var existingSample = serializer.Project.GetSample(sampleName);

                if (existingSample != null)
                {
                    serializer.RemapId(sampleId, existingSample.Id);
                    dummySample.Serialize(serializer); // Skip
                }
                else
                {
                    needMerge = true;

                    if (!checkOnly && createMissing)
                    {
                        var sample = serializer.Project.CreateDPCMSample(sampleName);
                        serializer.RemapId(sampleId, sample.Id);
                        sample.Serialize(serializer);
                    }
                    else
                    {
                        serializer.RemapId(sampleId, -1);
                        dummySample.Serialize(serializer); // Skip
                    }
                }
            }

            serializer.Project.ConditionalSortSamples();

            return needMerge;
        }

        private static bool LoadAndMergeInstrumentList(ProjectLoadBuffer serializer, bool checkOnly = false, bool createMissing = true)
        {
            int numInstruments = 0;
            serializer.Serialize(ref numInstruments);

            var instrumentIdNameMap = new List<Tuple<int, int, string>>();
            for (int i = 0; i < numInstruments; i++)
            {
                var instId = 0;
                var instType = 0;
                var instName = "";
                serializer.Serialize(ref instId);
                serializer.Serialize(ref instType);
                serializer.Serialize(ref instName);
                instrumentIdNameMap.Add(new Tuple<int, int, string>(instId, instType, instName));
            }

            var dummyInstrument = new Instrument();
            var needMerge = false;

            // Match instruments by name, create missing ones and remap IDs.
            for (int i = 0; i < numInstruments; i++)
            {
                var instId   = instrumentIdNameMap[i].Item1;
                var instType = instrumentIdNameMap[i].Item2;
                var instName = instrumentIdNameMap[i].Item3;

                var existingInstrument = serializer.Project.GetInstrument(instName);

                if (existingInstrument != null)
                {
                    if (existingInstrument.Expansion == instType)
                        serializer.RemapId(instId, existingInstrument.Id);
                    else
                        serializer.RemapId(instId, -1); // Incompatible expansion type, skip.

                    dummyInstrument.Serialize(serializer); // Skip
                }
                else
                {
                    needMerge = true;

                    if (!checkOnly && createMissing && (instType == ExpansionType.None || serializer.Project.UsesExpansionAudio(instType)))
                    {
                        var instrument = serializer.Project.CreateInstrument(instType, instName);
                        serializer.RemapId(instId, instrument.Id);
                        instrument.Serialize(serializer);
                    }
                    else
                    {
                        serializer.RemapId(instId, -1);
                        dummyInstrument.Serialize(serializer); // Skip
                    }
                }
            }

            serializer.Project.ConditionalSortInstruments();

            return needMerge;
        }

        private static bool LoadAndMergeArpeggioList(ProjectLoadBuffer serializer, bool checkOnly = false, bool createMissing = true)
        {
            int numArpeggios = 0;
            serializer.Serialize(ref numArpeggios);

            var arpeggioIdNameMap = new List<Tuple<int, string>>();
            for (int i = 0; i < numArpeggios; i++)
            {
                var arpId = 0;
                var arpName = "";
                serializer.Serialize(ref arpId);
                serializer.Serialize(ref arpName);
                arpeggioIdNameMap.Add(new Tuple<int, string>(arpId, arpName));
            }

            var dummyArpeggio = new Arpeggio();
            var needMerge = false;

            // Match arpeggios by name, create missing ones and remap IDs.
            for (int i = 0; i < numArpeggios; i++)
            {
                var arpId   = arpeggioIdNameMap[i].Item1;
                var arpName = arpeggioIdNameMap[i].Item2;

                var existingArpeggio = serializer.Project.GetArpeggio(arpName);

                if (existingArpeggio != null)
                {
                    serializer.RemapId(arpId, existingArpeggio.Id);
                    dummyArpeggio.Serialize(serializer); // Skip
                }
                else
                {
                    needMerge = true;

                    if (!checkOnly && createMissing)
                    {
                        var instrument = serializer.Project.CreateArpeggio(arpName);
                        serializer.RemapId(arpId, instrument.Id);
                        instrument.Serialize(serializer);
                    }
                    else
                    {
                        serializer.RemapId(arpId, -1);
                        dummyArpeggio.Serialize(serializer); // Skip
                    }
                }
            }

            serializer.Project.ConditionalSortArpeggios();

            return needMerge;
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

        public static bool ContainsMissingInstrumentsOrSamples(Project project, bool notes, out bool missingArpeggios, out bool missingSamples)
        {
            var buffer = GetClipboardDataInternal(notes ? MagicNumberClipboardNotes : MagicNumberClipboardPatterns);

            missingArpeggios = false;
            missingSamples = false;
            if (buffer == null)
                return false;

            var serializer = new ProjectLoadBuffer(project, Compression.DecompressBytes(buffer, 4), Project.Version, ProjectBufferFlags.Clipboard);

            missingSamples = LoadAndMergeSampleList(serializer, true);
            missingArpeggios = LoadAndMergeArpeggioList(serializer, true);
            return LoadAndMergeInstrumentList(serializer, true);
        }

        public static Note[] LoadNotes(Project project, bool createMissingInstruments, bool createMissingArpeggios, bool createMissingSamples)
        {
            var buffer = GetClipboardDataInternal(MagicNumberClipboardNotes);

            if (buffer == null)
                return null;

            var serializer = new ProjectLoadBuffer(project, Compression.DecompressBytes(buffer, 4), Project.Version, ProjectBufferFlags.Clipboard);

            LoadAndMergeSampleList(serializer, false, createMissingSamples);
            LoadAndMergeArpeggioList(serializer, false, createMissingArpeggios);
            LoadAndMergeInstrumentList(serializer, false, createMissingInstruments);

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

            if (uniquePatterns.Count == 0)
            {
                Platform.SetClipboardData(null);
                return;
            }

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

        public static Pattern[,] LoadPatterns(Project project, Song song, bool createMissingInstruments, bool createMissingArpeggios, bool createMissingSamples, out Song.PatternCustomSetting[] customSettings)
        {
            var buffer = GetClipboardDataInternal(MagicNumberClipboardPatterns);

            if (buffer == null)
            {
                customSettings = null;
                return null;
            }

            var decompressedBuffer = Compression.DecompressBytes(buffer, 4);
            var serializer = new ProjectLoadBuffer(project, decompressedBuffer, Project.Version, ProjectBufferFlags.Clipboard);

            LoadAndMergeSampleList(serializer, false, createMissingSamples);
            LoadAndMergeArpeggioList(serializer, false, createMissingArpeggios);
            LoadAndMergeInstrumentList(serializer, false, createMissingInstruments);
            LoadAndMergePatternList(serializer, song);

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

            if (hasCustomSettings && tempoMode == project.TempoMode)
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
}
