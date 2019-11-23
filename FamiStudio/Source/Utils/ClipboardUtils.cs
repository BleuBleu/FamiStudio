using System;
using System.Collections.Generic;
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

        private static byte[] GetClipboardData()
        {
#if FAMISTUDIO_WINDOWS
            return Clipboard.GetData("FamiStudio") as byte[];
#else
            return macClipboardData;
#endif
        }

        public static void SetNotes(Note[] notes)
        {
            if (notes == null)
            {
                SetClipboardData(null);
                return;
            }

            var serializer = new ProjectSaveBuffer(null);

            foreach (var note in notes)
                note.SerializeState(serializer);
            
            var buffer = Compression.CompressBytes(serializer.GetBuffer(), CompressionLevel.Fastest);

            var clipboardData = new List<byte>();
            clipboardData.AddRange(BitConverter.GetBytes(MagicNumberClipboardNotes));
            clipboardData.AddRange(BitConverter.GetBytes(notes.Length));
            clipboardData.AddRange(buffer);

            SetClipboardData(clipboardData.ToArray());
        }

        public static Note[] GetNotes(Project project)
        {
            var buffer = GetClipboardData();

            if (buffer == null || BitConverter.ToUInt32(buffer, 0) != MagicNumberClipboardNotes)
                return null;

            var numNotes = BitConverter.ToInt32(buffer, 4);
            var decompressedBuffer = Compression.DecompressBytes(buffer, 8);
            var serializer = new ProjectLoadBuffer(project, decompressedBuffer, Project.Version);

            var notes = new Note[numNotes];

            for (int i = 0; i < numNotes; i++)
                notes[i].SerializeState(serializer);

            return notes;
        }

        public static void SetEnvelopeValues(sbyte[] values)
        {
            var clipboardData = new List<byte>();
            clipboardData.AddRange(BitConverter.GetBytes(MagicNumberClipboardEnvelope));
            clipboardData.AddRange(BitConverter.GetBytes(values.Length));
            for (int i = 0; i < values.Length; i++)
                clipboardData.Add((byte)values[i]);

            SetClipboardData(clipboardData.ToArray());
        }

        public static sbyte[] GetEnvelopeValues()
        {
            var buffer = GetClipboardData();

            if (buffer == null || BitConverter.ToUInt32(buffer, 0) != MagicNumberClipboardEnvelope)
                return null;

            var numValues = BitConverter.ToInt32(buffer, 4);
            var values = new sbyte[numValues];

            for (int i = 0; i < numValues; i++)
                values[i] = (sbyte)buffer[8 + i];

            return values;
        }

        public static void SetPatterns(Pattern[,] patterns)
        {
            if (patterns == null)
            {
                SetClipboardData(null);
                return;
            }

            int numNonNullPatterns = 0;

            for (int i = 0; i < patterns.GetLength(0); i++)
            {
                for (int j = 0; j < patterns.GetLength(1); j++)
                {
                    if (patterns[i, j] != null)
                        numNonNullPatterns++;
                }
            }

            var serializer = new ProjectSaveBuffer(null);

            for (int i = 0; i < patterns.GetLength(0); i++)
            {
                for (int j = 0; j < patterns.GetLength(1); j++)
                {
                    var pattern = patterns[i, j];
                    if (pattern != null)
                    {
                        serializer.Serialize(ref i);
                        serializer.Serialize(ref j);
                        patterns[i, j].SerializeState(serializer);
                    }
                }
            }

            var buffer = Compression.CompressBytes(serializer.GetBuffer(), CompressionLevel.Fastest);

            var clipboardData = new List<byte>();
            clipboardData.AddRange(BitConverter.GetBytes(MagicNumberClipboardPatterns));
            clipboardData.AddRange(BitConverter.GetBytes(numNonNullPatterns));
            clipboardData.AddRange(BitConverter.GetBytes(patterns.GetLength(0)));
            clipboardData.AddRange(BitConverter.GetBytes(patterns.GetLength(1)));
            clipboardData.AddRange(buffer);

            SetClipboardData(clipboardData.ToArray());
        }

        public static Pattern[,] GetPatterns(Project project)
        {
            var buffer = GetClipboardData();

            if (buffer == null || BitConverter.ToUInt32(buffer, 0) != MagicNumberClipboardPatterns)
                return null;

            var numNonNullPatterns = BitConverter.ToInt32(buffer, 4);
            var numPatterns = BitConverter.ToInt32(buffer, 8);
            var numChannels = BitConverter.ToInt32(buffer, 12);
            var decompressedBuffer = Compression.DecompressBytes(buffer, 16);
            var serializer = new ProjectLoadBuffer(project, decompressedBuffer, Project.Version);

            var patterns = new Pattern[numPatterns, numChannels];

            for (int p = 0; p < numNonNullPatterns; p++)
            {
                int i = 0;
                int j = 0;
                serializer.Serialize(ref i);
                serializer.Serialize(ref j);
                patterns[i, j] = new Pattern();
                patterns[i, j].SerializeState(serializer);
            }

            return patterns;
        }
    }
}
