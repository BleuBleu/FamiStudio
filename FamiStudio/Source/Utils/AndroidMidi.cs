using System;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public class Midi
    {
        public delegate void MidiNoteDelegate(int note, bool on);
        public static event MidiNoteDelegate NotePlayed;

        public static void Initialize()
        {
        }

        public static void Shutdown()
        {
        }

        public static int InputCount => 0;

        public static unsafe string GetDeviceName(int idx)
        {
            return null;
        }

        public static bool Open(int id)
        {
            return false;
        }

        public static void Close()
        {
        }
    }
}
