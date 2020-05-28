using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class Midi
    {
        const string AlsaLibName = "libasound.so";

        const int SND_RAWMIDI_STREAM_OUTPUT = 0;
        const int SND_RAWMIDI_STREAM_INPUT = 1;
        const int SND_RAWMIDI_SYNC = 4;

        internal const int STATUS_NOTE_ON = 0x90;
        internal const int STATUS_NOTE_OFF = 0x80;

        const int ENXIO = 6;

        [DllImport(AlsaLibName)]
        static extern int snd_card_next(ref int rcard);

        [DllImport(AlsaLibName)]
        static extern int snd_ctl_open(ref IntPtr ctlp, string name, int mode);

        [DllImport(AlsaLibName)]
        static extern int snd_ctl_rawmidi_next_device(IntPtr ctl, ref int device);

        [DllImport(AlsaLibName)]
        static extern void snd_rawmidi_info_set_device(IntPtr info, uint val);

        [DllImport(AlsaLibName)]
        static extern void snd_rawmidi_info_set_subdevice(IntPtr info, int val);

        [DllImport(AlsaLibName)]
        static extern void snd_rawmidi_info_set_stream(IntPtr info, int val);

        [DllImport(AlsaLibName)]
        static extern int snd_ctl_rawmidi_info(IntPtr ctl, IntPtr info);

        [DllImport(AlsaLibName)]
        static extern int snd_rawmidi_info_get_subdevices_count(IntPtr info);

        [DllImport(AlsaLibName)]
        static extern int snd_rawmidi_info_malloc(out IntPtr info);

        [DllImport(AlsaLibName)]
        static extern void snd_rawmidi_info_free(IntPtr info);

        [DllImport(AlsaLibName)]
        static extern IntPtr snd_rawmidi_info_get_name(IntPtr info);

        [DllImport(AlsaLibName)]
        static extern string snd_rawmidi_info_get_subdevice_name(IntPtr info);

        [DllImport(AlsaLibName)]
        static extern int snd_rawmidi_open(ref IntPtr inputp, IntPtr outputp, string name, int mode);

        [DllImport(AlsaLibName)]
        static extern int snd_rawmidi_drain(IntPtr rawmidi);

        [DllImport(AlsaLibName)]
        static extern int snd_rawmidi_close(IntPtr rawmidi);

        [DllImport(AlsaLibName)]
        internal static extern long snd_rawmidi_read(IntPtr rawmidi, IntPtr buffer, long size);

        [DllImport(AlsaLibName)] 
        static extern long snd_rawmidi_write(IntPtr rawmidi, IntPtr buffer, long size);

        public delegate void MidiNoteDelegate(int note, bool on);
        public event MidiNoteDelegate NotePlayed;

        private IntPtr midiIn = IntPtr.Zero;
        private Thread midiThread;
        private bool quit = false;

        public unsafe Midi()
        {

        }

        struct AlsaMidiDevice
        {
            public string hwName;
            public string name;
            public int device;
            public int sub;
        };

        private static List<AlsaMidiDevice> GetInputDevices()
        {
            var devices = new List<AlsaMidiDevice>();

            int card = -1;
            if (snd_card_next(ref card) < 0)
                return devices;

            while (card >= 0)
            {
                IntPtr ctl = IntPtr.Zero;
                string name = $"hw:{card}";

                if (snd_ctl_open(ref ctl, name, 0) >= 0)
                {
                    int device = -1;
                    do
                    {
                        if (snd_ctl_rawmidi_next_device(ctl, ref device) >= 0 && device >= 0)
                        {
                            snd_rawmidi_info_malloc(out var info);
                            snd_rawmidi_info_set_device(info, (uint)device);

                            snd_rawmidi_info_set_stream(info, SND_RAWMIDI_STREAM_INPUT);
                            snd_ctl_rawmidi_info(ctl, info);
                            var inSubs = snd_rawmidi_info_get_subdevices_count(info);

                            snd_rawmidi_info_set_stream(info, SND_RAWMIDI_STREAM_OUTPUT);
                            snd_ctl_rawmidi_info(ctl, info);
                            var outSubs = snd_rawmidi_info_get_subdevices_count(info);

                            for (int sub = 0; sub < inSubs + outSubs; sub++)
                            {
                                snd_rawmidi_info_set_subdevice(info, sub);
                                snd_rawmidi_info_set_stream(info, SND_RAWMIDI_STREAM_INPUT);

                                int status = snd_ctl_rawmidi_info(ctl, info);
                                if (status == 0 || status < 0 && status != -ENXIO)
                                {
                                    var deviceName = Marshal.PtrToStringAnsi(snd_rawmidi_info_get_name(info));
                                    devices.Add(new AlsaMidiDevice() { hwName = name, name = deviceName, device = device, sub = sub });
                                }
                            }

                            snd_rawmidi_info_free(info);
                        }
                    }
                    while (device >= 0);
                }

                snd_card_next(ref card);
            }

            return devices;
        }

        public static int InputCount
        {
            get
            {
                return GetInputDevices().Count;
            }
        }


        public static unsafe string GetDeviceName(int idx)
        {
            var devices = GetInputDevices();
            if (idx >= 0 && idx < devices.Count)
                return devices[idx].name;
            return null;
        }

        public bool Open(int idx)
        {
            var devices = GetInputDevices();

            if (idx < 0 || idx >= devices.Count)
                return false;

            if (snd_rawmidi_open(ref midiIn, IntPtr.Zero, devices[idx].hwName, SND_RAWMIDI_SYNC) < 0)
                return false;

            if (midiIn != IntPtr.Zero)
            {
                midiThread = new Thread(MidiThread);
                midiThread.Start();
                return true;
            }

            return false;
        }

        public unsafe bool Close()
        {
            if (midiIn != IntPtr.Zero)
            {
                snd_rawmidi_drain(midiIn);
                snd_rawmidi_close(midiIn);
                midiIn = IntPtr.Zero;
            }

            if (midiThread != null)
            {
                quit = true;

                if (!midiThread.Join(100))
                {
                    Debug.WriteLine("MIDI thread not terminating, killing.");
                    midiThread.Abort();
                }

                midiThread = null;
            }

            return false;
        }

        private unsafe void MidiThread(object obj)
        {
            while (!quit)
            {
                int data = 0;
                if (snd_rawmidi_read(midiIn, new IntPtr(&data), 3) < 0)
                {
                    quit = true;
                    return;
                }

                int status = data & 0xf0;
                int note = (data >> 8) & 0xff;
                int velocity = (data >> 8) & 0xff;

                if (status == STATUS_NOTE_OFF || (status == STATUS_NOTE_ON && velocity == 0))
                {
                    NotePlayed?.Invoke(note, false);
                }
                else if (status == STATUS_NOTE_ON)
                {
                    NotePlayed?.Invoke(note, true);
                }

                Debug.WriteLine($"{data:x8}");
            }
        }
    }
}
