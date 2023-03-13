using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public unsafe class OpenALStream : IAudioStream
    {
        private static IntPtr device;
        private static IntPtr context;

        private GetBufferDataCallback bufferFill;
        private int freq;
        private bool quit;
        private Task playingTask;
        private bool stereo;

        int source;
        int[] buffers;

        int   immediateSource = -1;
        int[] immediateBuffers;

        public OpenALStream(int rate, bool inStereo, int bufferSize, int numBuffers, GetBufferDataCallback bufferFillCallback)
        {
            if (device == IntPtr.Zero)
            {
                device = ALC.OpenDevice(null);
                context = ALC.CreateContext(device, null);
                ALC.MakeContextCurrent(context);

                var deviceName = ALC.GetString(device, ALC.DeviceSpecifier);
                Console.WriteLine($"Default OpenAL audio device is '{deviceName}'");
            }

            stereo = inStereo;
            // TODO : We need to decouple the number of emulated buffered frames and the 
            // size of the low-level audio buffers.
            freq = rate;
            source = AL.GenSource();
            buffers = AL.GenBuffers(numBuffers);
            bufferFill = bufferFillCallback;
            quit = false;
        }

        public void Dispose()
        {
            StopImmediate();

            AL.DeleteBuffers(buffers);
            AL.DeleteSource(source);
        }

        public bool IsStarted => playingTask != null;

        public void Start()
        {
            quit = false;
            playingTask = Task.Factory.StartNew(PlayAsync, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            if (playingTask != null)
            {
                quit = true;
                playingTask.Wait();
                playingTask = null;
            }

            AL.SourceStop(source);
            AL.Source(source, AL.Buffer, 0);
        }

        private void DebugStream()
        {
#if DEBUG
            AL.GetSource(source, AL.BuffersProcessed, out int numProcessed);
            AL.GetSource(source, AL.BuffersQueued, out int numQueued);
            AL.GetSource(source, AL.SourceState, out int state);
            Debug.WriteLine($"{DateTime.Now.Millisecond} = {state} {numQueued} {numProcessed}");
#endif
        }

        private unsafe void PlayAsync()
        {
            int initBufferIdx = buffers.Length - 1;

            while (!quit)
            {
                int numProcessed = 0;

                if (initBufferIdx < 0)
                {
                    do
                    {
                        AL.GetSource(source, AL.BuffersProcessed, out numProcessed);
                        if (numProcessed != 0) break;
                        if (quit) return;
                        Thread.Sleep(1);
                    }
                    while (true);
                }
                else
                {
                    numProcessed = initBufferIdx + 1;
                }

                for (int i = 0; i < numProcessed && !quit; )
                {
                    var data = bufferFill();
                    if (data != null)
                    {
                        int bufferId = initBufferIdx >= 0 ? buffers[initBufferIdx--] : AL.SourceUnqueueBuffer(source);
                        fixed (short* p = &data[0])
                            AL.BufferData(bufferId, stereo ? AL.Stereo16 : AL.Mono16, new IntPtr(p), data.Length * sizeof(short), freq);
                        AL.SourceQueueBuffer(source, bufferId);
                        i++;
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }

                //DebugStream();

                AL.GetSource(source, AL.SourceState, out int state);
                if (state != AL.Playing)
                {
                    Debug.WriteLine("RESTART!");
                    //DebugStream();
                    AL.SourcePlay(source);
                }
            }
        }

        public unsafe void PlayImmediate(short[] data, int sampleRate, float volume)
        {
            Debug.Assert(Platform.IsInMainThread());

            StopImmediate();

            immediateSource  = AL.GenSource();
            immediateBuffers = AL.GenBuffers(1);

            fixed (short* p = &data[0])
                AL.BufferData(immediateBuffers[0], AL.Mono16, new IntPtr(p), data.Length * sizeof(short), sampleRate);

            AL.Source(immediateSource, AL.Gain, volume);
            AL.SourceQueueBuffer(immediateSource, immediateBuffers[0]);
            AL.SourcePlay(immediateSource);
        } 

        public void StopImmediate()
        {
            Debug.Assert(Platform.IsInMainThread());

            if (immediateSource >= 0)
            {
                AL.SourceStop(immediateSource);
                AL.Source(immediateSource, AL.Buffer, 0);
                AL.DeleteBuffers(immediateBuffers);
                AL.DeleteSource(immediateSource);

                immediateBuffers = null;
                immediateSource = -1; 
            }
        }

        public int ImmediatePlayPosition
        {
            get
            {
                var playPos = -1;

                // TODO : Make sure we support the AL_EXT_OFFSET extension.
                if (immediateSource >= 0)
                {
                    AL.GetSource(immediateSource, AL.SourceState, out int state);
                    if (state == AL.Playing)
                        AL.GetSource(immediateSource, AL.SampleOffset, out playPos);
                }

                return playPos;
            }
        }
    }

    public static class ALC
    {
        private const string OpenAlcDll = "libopenal32"; // NET5TODO

        public const int DeviceSpecifier = 0x1005;

        [DllImport(OpenAlcDll, EntryPoint = "alcOpenDevice", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr OpenDevice([In] string devicename);

        [DllImport(OpenAlcDll, EntryPoint = "alcCloseDevice", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CloseDevice([In] IntPtr device);

        [DllImport(OpenAlcDll, EntryPoint = "alcGetString", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetStringInternal([In] IntPtr device, int param);

        [DllImport(OpenAlcDll, EntryPoint = "alcCreateContext", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern IntPtr CreateContext([In] IntPtr device, [In] int* attrlist);

        [DllImport(OpenAlcDll, EntryPoint = "alcDestroyContext", CallingConvention = CallingConvention.Cdecl)]
        public static extern void DestroyContext(IntPtr context);

        [DllImport(OpenAlcDll, EntryPoint = "alcMakeContextCurrent", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool MakeContextCurrent(IntPtr context);

        public static string GetString(IntPtr device, int param)
        {
            return Marshal.PtrToStringAnsi(GetStringInternal(device, param));
        }
    }

    public unsafe static class AL
    {
        private const string OpenAlDll = "libopenal32"; // NET5TODO

        public const int Buffer           = 0x1009;
        public const int Gain             = 0x100A;
        public const int SourceState      = 0x1010;
        public const int Initial          = 0x1011;
        public const int Playing          = 0x1012;
        public const int Paused           = 0x1013;
        public const int Stopped          = 0x1014;
        public const int BuffersQueued    = 0x1015;
        public const int BuffersProcessed = 0x1016;
        public const int SampleOffset     = 0x1025;
        public const int ByteOffset       = 0x1026;
        public const int SourceType       = 0x1027;
        public const int Mono8            = 0x1100;
        public const int Mono16           = 0x1101;
        public const int Stereo8          = 0x1102;
        public const int Stereo16         = 0x1103;

        [DllImport(OpenAlDll, EntryPoint = "alGenSources", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void GenSources(int n, [Out] int* sources);

        [DllImport(OpenAlDll, EntryPoint = "alDeleteSources", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void DeleteSources(int n, [In] int* sources);

        [DllImport(OpenAlDll, EntryPoint = "alSourcePlayv", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void SourcePlay(int ns, [In] int* sids);

        [DllImport(OpenAlDll, EntryPoint = "alSourceStopv", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void SourceStop(int ns, [In] int* sids);

        [DllImport(OpenAlDll, EntryPoint = "alSourcei", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Source(int sid, int param, int value);

        [DllImport(OpenAlDll, EntryPoint = "alSourcef", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Source(int sid, int param, float value);

        [DllImport(OpenAlDll, EntryPoint = "alSourceQueueBuffers", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void SourceQueueBuffers(int sid, int numEntries, [In] int* bids);

        [DllImport(OpenAlDll, EntryPoint = "alSourceUnqueueBuffers", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void SourceUnqueueBuffers(int sid, int numEntries, [In] int* bids);

        [DllImport(OpenAlDll, EntryPoint = "alBufferData", CallingConvention = CallingConvention.Cdecl)]
        public static extern void BufferData(int bid, int format, IntPtr buffer, int size, int freq);

        [DllImport(OpenAlDll, EntryPoint = "alGetSourcei", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetSource(int sid, int param, [Out] out int value);

        [DllImport(OpenAlDll, EntryPoint = "alGenBuffers", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void GenBuffers(int n, [Out] int* buffers);

        [DllImport(OpenAlDll, EntryPoint = "alDeleteBuffers", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void DeleteBuffers(int n, [In] int* buffers);

        public static void SourcePlay(int id)
        {
            var ids = new [] { id };
            fixed (int* p = &ids[0])
                SourcePlay(1, p);
        }

        public static void SourceStop(int id)
        {
            var ids = new[] { id };
            fixed (int* p = &ids[0])
                SourceStop(1, p);
        }

        public static int GenSource()
        {
            var ids = new int[1];
            fixed (int* p = &ids[0])
                GenSources(1, p);
            return ids[0];
        }

        public static int[] GenBuffers(int n)
        {
            var ids = new int[n];
            fixed (int* p = &ids[0])
                GenBuffers(n, p);
            return ids;
        }

        public static void DeleteSource(int id)
        {
            var ids = new[] { id };
            fixed (int* p = &ids[0])
                DeleteSources(1, p);
        }

        public static void DeleteBuffers(int[] ids)
        {
            fixed (int* p = &ids[0])
                DeleteSources(ids.Length, p);
        }

        public static void SourceQueueBuffer(int sid, int bid)
        {
            var ids = new[] { bid };
            fixed (int* p = &ids[0])
                SourceQueueBuffers(sid, 1, p);
        }

        public static int SourceUnqueueBuffer(int sid)
        {
            var ids = new int[1];
            fixed (int* p = &ids[0])
                SourceUnqueueBuffers(sid, 1, p);
            return ids[0];
        }
    }
}
