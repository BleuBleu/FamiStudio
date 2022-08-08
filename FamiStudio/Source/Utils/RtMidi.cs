using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FamiStudio
{
    public static class Midi
    {
        const string RtMidiLibName = "librtmidi" + Platform.DllExtension;

        internal const int STATUS_NOTE_ON  = 0x90;
        internal const int STATUS_NOTE_OFF = 0x80;

        [DllImport(RtMidiLibName)]
        static extern IntPtr rtmidi_in_create(int api, string clientName, int queueSizeLimit);

        [DllImport(RtMidiLibName)]
        static extern void rtmidi_in_free(IntPtr device);

        [DllImport(RtMidiLibName)]
        static extern int rtmidi_get_port_count(IntPtr device);

        [DllImport(RtMidiLibName)]
        static extern IntPtr rtmidi_get_port_name(IntPtr device, int portNumber);

        [DllImport(RtMidiLibName)]
        static extern void rtmidi_open_port(IntPtr device, int portNumber, string portName);

        [DllImport(RtMidiLibName)]
        static extern void rtmidi_close_port(IntPtr device);

        [DllImport(RtMidiLibName)]
        static extern void rtmidi_in_set_callback(IntPtr device, [MarshalAs(UnmanagedType.FunctionPtr)] RtMidiCCallback callback, IntPtr userData);

        [DllImport(RtMidiLibName)]
        static extern void rtmidi_in_cancel_callback(IntPtr device);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void RtMidiCCallback(double timeStamp, IntPtr message, ulong messageSize, IntPtr userData);

        public delegate void MidiNoteDelegate(int note, bool on);
        public static event MidiNoteDelegate NotePlayed;

        private static IntPtr midiIn = IntPtr.Zero;
        private static RtMidiCCallback callback = null;

        public static void Initialize()
        {
            if (midiIn == IntPtr.Zero)
            {
                try
                {
                    midiIn = rtmidi_in_create(0, "FamiStudio", 0);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error initializing RtMidi. MIDI input will not be available.");
                    Console.WriteLine(e.Message);
                    midiIn = IntPtr.Zero;
                }
            }
        }

        public static void Shutdown()
        {
            if (midiIn != IntPtr.Zero)
            {
                Close();
                rtmidi_in_free(midiIn);
                midiIn = IntPtr.Zero;
            }
        }

        public static int InputCount
        {
            get
            {
                return midiIn == IntPtr.Zero ? 0 : rtmidi_get_port_count(midiIn);
            }
        }


        public static unsafe string GetDeviceName(int idx)
        {
            return Marshal.PtrToStringAnsi(rtmidi_get_port_name(midiIn, idx));
        }

        public static bool Open(int idx)
        {
            callback = new RtMidiCCallback(MidiCallback);
            rtmidi_in_set_callback(midiIn, callback, IntPtr.Zero);
            rtmidi_open_port(midiIn, idx, "FamiStudioMidiIn");

            return true;
        }

        public static void Close()
        {
            if (callback != null)
            {
                Debug.Assert(midiIn != null);
                rtmidi_in_cancel_callback(midiIn);
                rtmidi_close_port(midiIn);
                callback = null;
            }
        }

        private static unsafe void MidiCallback(double timeStamp, IntPtr message, ulong messageSize, IntPtr userData)
        {
            if (messageSize == 3)
            {
                byte* p = (byte*)message.ToPointer();

                int status = p[0] & 0xf0;
                int note = p[1];
                int velocity = p[2];

                if (status == STATUS_NOTE_OFF || (status == STATUS_NOTE_ON && velocity == 0))
                {
                    NotePlayed?.Invoke(note, false);
                }
                else if (status == STATUS_NOTE_ON)
                {
                    NotePlayed?.Invoke(note, true);
                }

                Debug.WriteLine($"{status:x2} {note:x2} {velocity:x2}");
            }
        }
    }
}
