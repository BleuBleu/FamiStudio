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

        public static void SetNotes(SortedDictionary<int, Note> notes)
        {
            var serializer = new ProjectSaveBuffer(null);

            foreach (var kv in notes)
            {
                int time = kv.Key;
                serializer.Serialize(ref time);
                kv.Value.SerializeState(serializer);
            }
            
            var buffer = Compression.CompressBytes(serializer.GetBuffer(), CompressionLevel.Optimal);

            var clipboardData = new List<byte>();
            clipboardData.AddRange(BitConverter.GetBytes(MagicNumberClipboardNotes));
            clipboardData.AddRange(BitConverter.GetBytes(notes.Count));
            clipboardData.AddRange(buffer);

            SetClipboardData(clipboardData.ToArray());
        }

        public static SortedDictionary<int, Note> GetNotes(Project project)
        {
            var buffer = GetClipboardData();

            if (BitConverter.ToUInt32(buffer, 0) != MagicNumberClipboardNotes)
                return null;

            var numNotes = BitConverter.ToInt32(buffer, 4);
            var decompressedBuffer = Compression.DecompressBytes(buffer, 8);
            var serializer = new ProjectLoadBuffer(project, decompressedBuffer, Project.Version);

            var notes = new SortedDictionary<int, Note>();
            for (int i = 0; i < numNotes; i++)
            {
                var time = 0; ;
                serializer.Serialize(ref time);
                var note = new Note();
                note.SerializeState(serializer);
                notes[time] = note;
            }

            return notes;
        }
    }
}
