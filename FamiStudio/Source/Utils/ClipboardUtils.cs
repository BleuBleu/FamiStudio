using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace FamiStudio
{
    public static class ClipboardUtils
    {
        const uint MagicNumberClipboardNotes    = 0x214E4346; // FCN!
        const uint MagicNumberClipboardEnvelope = 0x21454346; // FCE!
        const uint MagicNumberClipboardPatterns = 0x21504346; // FCP!

#if !FAMISTUDIO_WINDOWS
        static byte[] macClipboardData; // Cant copy between FamiStudio instance on MacOS.
#endif

        private static void SetClipboardData(byte[] data)
        {
#if FAMISTUDIO_WINDOWS
            Clipboard.SetData("FamiStudio", data);
#else
            macClipboardData = data;
#endif
        }

        private static byte[] GetClipboardData(uint magic)
        {
            byte[] buffer = null;
#if FAMISTUDIO_WINDOWS
            try
            {
                buffer = Clipboard.GetData("FamiStudio") as byte[];
            }
            catch
            {
                Debug.WriteLine("Random DisconnectedContext exception. Ignoring.");
            }
#else
            buffer = macClipboardData;
#endif

            if (buffer == null || BitConverter.ToUInt32(buffer, 0) != magic)
                return null;

            return buffer;
        }

        public static bool ConstainsNotes    => GetClipboardData(MagicNumberClipboardNotes) != null;
        public static bool ConstainsEnvelope => GetClipboardData(MagicNumberClipboardEnvelope) != null;
        public static bool ConstainsPatterns => GetClipboardData(MagicNumberClipboardPatterns) != null;

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
                pat.SerializeState(serializer);
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
                var patId = patternIdNameMap[i].Item1;
                var patChannel = patternIdNameMap[i].Item2;
                var patName = patternIdNameMap[i].Item3;

                if (serializer.Project.IsChannelActive(patChannel))
                {
                    var existingPattern = song.GetChannelByType(patChannel).GetPattern(patName);

                    if (existingPattern != null)
                    {
                        serializer.RemapId(patId, existingPattern.Id);
                        dummyPattern.SerializeState(serializer); // Skip 
                    }
                    else
                    {
                        var pattern = song.GetChannelByType(patChannel).CreatePattern(patName);
                        serializer.RemapId(patId, pattern.Id);
                        pattern.SerializeState(serializer);
                    }
                }
                else
                {
                    serializer.RemapId(patId, -1);
                    dummyPattern.SerializeState(serializer); // Skip 
                }
            }

            return numPatterns;
        }

        private static void SaveInstrumentList(ProjectSaveBuffer serializer, ICollection<Instrument> instruments)
        {
            int numInstruments = instruments.Count;
            serializer.Serialize(ref numInstruments);

            foreach (var inst in instruments)
            {
                var instId = inst.Id;
                var instType = inst.ExpansionType;
                var instName = inst.Name;
                serializer.Serialize(ref instId);
                serializer.Serialize(ref instType);
                serializer.Serialize(ref instName);
            }

            foreach (var inst in instruments)
                inst.SerializeState(serializer);
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
                var instId = instrumentIdNameMap[i].Item1;
                var instType = instrumentIdNameMap[i].Item2;
                var instName = instrumentIdNameMap[i].Item3;

                var existingInstrument = serializer.Project.GetInstrument(instName);

                if (existingInstrument != null)
                {
                    if (existingInstrument.ExpansionType == instType)
                        serializer.RemapId(instId, existingInstrument.Id);
                    else
                        serializer.RemapId(instId, -1); // Incompatible expansion type, skip.

                    dummyInstrument.SerializeState(serializer); // Skip
                }
                else
                {
                    needMerge = true;

                    if (!checkOnly)
                    {
                        if (createMissing && (instType == Project.ExpansionNone || instType == serializer.Project.ExpansionAudio))
                        {
                            var instrument = serializer.Project.CreateInstrument(instType, instName);
                            serializer.RemapId(instId, instrument.Id);
                            instrument.SerializeState(serializer);
                        }
                        else
                        {
                            serializer.RemapId(instId, -1);
                            dummyInstrument.SerializeState(serializer); // Skip
                        }
                    }
                }
            }

            return needMerge;
        }

        public static void SaveNotes(Project project, Note[] notes)
        {
            if (notes == null)
            {
                SetClipboardData(null);
                return;
            }

            var instruments = new HashSet<Instrument>();
            foreach (var note in notes)
            {
                if (note.Instrument != null)
                    instruments.Add(note.Instrument);
            }

            var serializer = new ProjectSaveBuffer(null);

            SaveInstrumentList(serializer, instruments);

            int numNotes = notes.Length;
            serializer.Serialize(ref numNotes);
            foreach (var note in notes)
                note.SerializeState(serializer);
            
            var buffer = Compression.CompressBytes(serializer.GetBuffer(), CompressionLevel.Fastest);
            var clipboardData = new List<byte>();
            clipboardData.AddRange(BitConverter.GetBytes(MagicNumberClipboardNotes));
            clipboardData.AddRange(buffer);

            SetClipboardData(clipboardData.ToArray());
        }

        public static bool ContainsMissingInstruments(Project project, bool notes)
        {
            var buffer = GetClipboardData(notes ? MagicNumberClipboardNotes : MagicNumberClipboardPatterns);

            if (buffer == null)
                return false;

            var serializer = new ProjectLoadBuffer(project, Compression.DecompressBytes(buffer, 4), Project.Version);

            return LoadAndMergeInstrumentList(serializer, true);
        }

        public static Note[] LoadNotes(Project project, bool createMissingInstruments)
        {
            var buffer = GetClipboardData(MagicNumberClipboardNotes);

            if (buffer == null)
                return null;

            var serializer = new ProjectLoadBuffer(project, Compression.DecompressBytes(buffer, 4), Project.Version);

            LoadAndMergeInstrumentList(serializer, false, createMissingInstruments);

            int numNotes = 0;
            serializer.Serialize(ref numNotes);
            var notes = new Note[numNotes];
            for (int i = 0; i < numNotes; i++)
                notes[i].SerializeState(serializer);

            project.SortInstruments();

            return notes;
        }

        public static void SaveEnvelopeValues(sbyte[] values)
        {
            var clipboardData = new List<byte>();
            clipboardData.AddRange(BitConverter.GetBytes(MagicNumberClipboardEnvelope));
            clipboardData.AddRange(BitConverter.GetBytes(values.Length));
            for (int i = 0; i < values.Length; i++)
                clipboardData.Add((byte)values[i]);

            SetClipboardData(clipboardData.ToArray());
        }

        public static sbyte[] LoadEnvelopeValues()
        {
            var buffer = GetClipboardData(MagicNumberClipboardEnvelope);

            if (buffer == null)
                return null;

            var numValues = BitConverter.ToInt32(buffer, 4);
            var values = new sbyte[numValues];

            for (int i = 0; i < numValues; i++)
                values[i] = (sbyte)buffer[8 + i];

            return values;
        }

        public static void SavePatterns(Project project, Pattern[,] patterns)
        {
            if (patterns == null)
            {
                SetClipboardData(null);
                return;
            }

            var uniqueInstruments = new HashSet<Instrument>();
            var uniquePatterns = new HashSet<Pattern>();
            var numPatterns = patterns.GetLength(0);
            var numChannels = patterns.GetLength(1);

            for (int i = 0; i < numPatterns; i++)
            {
                for (int j = 0; j < numChannels; j++)
                {
                    var pattern = patterns[i, j];
                    if (pattern != null)
                    {
                        uniquePatterns.Add(pattern);
                        for (int k = 0; k < pattern.Song.PatternLength; k++)
                        {
                            var inst = pattern.Notes[k].Instrument;
                            if (inst != null)
                                uniqueInstruments.Add(inst);
                        }
                    }
                }
            }

            if (uniquePatterns.Count == 0)
            {
                SetClipboardData(null);
                return;
            }

            var serializer = new ProjectSaveBuffer(null);

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

            var buffer = Compression.CompressBytes(serializer.GetBuffer(), CompressionLevel.Fastest);
            var clipboardData = new List<byte>();
            clipboardData.AddRange(BitConverter.GetBytes(MagicNumberClipboardPatterns));
            clipboardData.AddRange(buffer);

            SetClipboardData(clipboardData.ToArray());
        }

        public static Pattern[,] LoadPatterns(Project project, Song song, bool createMissingInstruments)
        {
            var buffer = GetClipboardData(MagicNumberClipboardPatterns);

            if (buffer == null)
                return null;

            var decompressedBuffer = Compression.DecompressBytes(buffer, 4);
            var serializer = new ProjectLoadBuffer(project, decompressedBuffer, Project.Version);

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

            return patterns;
        }

        public static void Reset()
        {
            SetClipboardData(null);
        }
    }
}
