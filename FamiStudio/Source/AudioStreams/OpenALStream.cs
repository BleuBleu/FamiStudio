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
        private StreamStartingCallback streamStarting;
        private int freq;
        private bool quit;
        private Task playingTask;
        private bool stereo;
        private int bufferSizeBytes;
        private int source;
        private int[] buffers;
        private short[] samples;
        private int samplesOffset;
        private static int refCount = 0;

        private int[]   immediateSource  = new [] { -1, -1 };
        private int[][] immediateBuffers = new int[2][];

        public bool Stereo => stereo;

        private OpenALStream()
        {
        }

        public static OpenALStream Create(int rate, bool stereo, int bufferSizeMs)
        {
            refCount++;
            
            if (device == IntPtr.Zero)
            {
                device = ALC.OpenDevice(null);
                if (device == IntPtr.Zero) return null;
                context = ALC.CreateContext(device, null);
                if (context == IntPtr.Zero) return null;
                ALC.MakeContextCurrent(context);

                var deviceName = ALC.GetString(device, ALC.DeviceSpecifier);
                Console.WriteLine($"Default OpenAL audio device is '{deviceName}'");
            }

            var numBuffers = Math.Max(4, bufferSizeMs / 25);
            var stream = new OpenALStream();

            stream.stereo = stereo;
            stream.freq = rate;
            stream.source = AL.GenSource();
            stream.buffers = AL.GenBuffers(numBuffers);
            stream.bufferSizeBytes = Utils.RoundUp(rate * bufferSizeMs / 1000 * sizeof(short) * (stereo ? 2 : 1) / numBuffers, sizeof(short) * 2);
            stream.quit = false;

            return stream;
        }

        public void Dispose()
        {
            StopImmediate(0);
            StopImmediate(1);

            AL.DeleteBuffers(buffers);
            AL.DeleteSource(source);

            if (--refCount == 0)
            {
                if (context != IntPtr.Zero)
                {
                    ALC.DestroyContext(context);
                    context = IntPtr.Zero;
                }
                if (device != IntPtr.Zero)
                {
                    ALC.CloseDevice(device);
                    device = IntPtr.Zero;
                }
            }
        }

        public bool IsPlaying => playingTask != null;
        public bool RecreateOnDeviceChanged => false;

        public void Start(GetBufferDataCallback bufferFillCallback, StreamStartingCallback streamStartCallback)
        {
            quit = false;
            bufferFill = bufferFillCallback;
            streamStarting = streamStartCallback;
            samples = null;
            samplesOffset = 0;
            playingTask = Task.Factory.StartNew(PlayAsync, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            StopInternal(true);
        }

        private void StopInternal(bool mainThread)
        {
            Debug.Assert(Platform.IsInMainThread() == mainThread);

            var acquired = true;
            
            if (mainThread)
                Monitor.Enter(this);
            else 
                Monitor.TryEnter(this, ref acquired);

            if (acquired)
            {
                if (playingTask != null)
                {
                    // Stop can be called from the task itself for non-looping song so we 
                    // need a mutex and we dont want to wait for the task if we are the task.
                    if (mainThread)
                    {
                        quit = true;
                        playingTask.Wait();
                    }

                    AL.SourceStop(source);
                    AL.Source(source, AL.Buffer, 0);

                    playingTask = null;
                    bufferFill = null;
                    streamStarting = null;
                    samples = null;
                    samplesOffset = 0;
                }

                Monitor.Exit(this);
            }
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
            var streamStarted = false;
            var streamDone = false;
            var bufferSampleCount = bufferSizeBytes / sizeof(short);
            var bufferSamples = new short[bufferSampleCount];

            while (!quit)
            {
                var numBuffersToFill = 0;

                if (!streamDone)
                {
                    if (streamStarted)
                    {
                        do
                        {
                            AL.GetSource(source, AL.BuffersProcessed, out numBuffersToFill);
                            if (numBuffersToFill != 0) break;
                            if (quit) return;
                            Thread.Sleep(1);
                        }
                        while (true);
                    }
                    else
                    {
                        numBuffersToFill = buffers.Length;
                    }
            
                    for (var i = 0; i < numBuffersToFill && !quit && !streamDone; )
                    {
                        var bufferId = streamStarted ? AL.SourceUnqueueBuffer(source) : buffers[i];
                        var bufferSamplesOffset = 0;

                        do
                        {
                            if (samplesOffset == 0)
                            {
                                while (true)
                                {
                                    var newSamples = bufferFill(out streamDone);
                                    
                                    // If we are done, pad the last buffer with zeroes so the stream can start
                                    // for super-short (ex: 1-frame) non-looping songs.
                                    if (streamDone)
                                    {
                                        newSamples = new short[bufferSampleCount - samplesOffset];
                                    }

                                    if (newSamples != null)
                                    {
                                        samples = newSamples;
                                        break;
                                    }
                                    
                                    if (quit)
                                    {
                                        return;
                                    }
                                    
                                    Thread.Sleep(1);
                                }
                            }

                            var numSamplesToCopy = samples.Length - samplesOffset;
                            numSamplesToCopy = Math.Min(bufferSampleCount - bufferSamplesOffset, numSamplesToCopy);
                            Array.Copy(samples, samplesOffset, bufferSamples, bufferSamplesOffset, numSamplesToCopy);
                            
                            samplesOffset += numSamplesToCopy;
                            if (samplesOffset == samples.Length)
                                samplesOffset = 0;

                            bufferSamplesOffset += numSamplesToCopy;
                        }
                        while (bufferSamplesOffset < bufferSampleCount);

                        fixed (short* p = &bufferSamples[0])
                            AL.BufferData(bufferId, stereo ? AL.Stereo16 : AL.Mono16, new IntPtr(p), bufferSamples.Length * sizeof(short), freq);

                        AL.SourceQueueBuffer(source, bufferId);
                        i++;
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }

                //DebugStream();

                AL.GetSource(source, AL.SourceState, out int state);
                if (state != AL.Playing)
                {
                    Debug.WriteLine("RESTART!");

                    if (!streamStarted)
                    {
                        streamStarting();
                        streamStarted = true;
                    }
                    else if (streamDone)
                    {
                        // If the stream is done, it will stop by itself if we stop queueing buffers but we still
                        // need to be able to differenciate between starvation and the true end of the stream.
                        AL.GetSource(source, AL.BuffersProcessed, out var numProcessed);
                        AL.GetSource(source, AL.BuffersQueued, out var numQueued);

                        if (numQueued == numProcessed)
                        {
                            StopInternal(false);
                            return;
                        }
                    }
                    
                    AL.SourcePlay(source);
                }
            }
        }

        public unsafe void PlayImmediate(short[] data, int sampleRate, float volume, int channel = 0)
        {
            Debug.Assert(Platform.IsInMainThread());
            Debug.Assert(channel == 0 || channel == 1);

            StopImmediate(channel);

            immediateSource[channel]  = AL.GenSource();
            immediateBuffers[channel] = AL.GenBuffers(1);

            fixed (short* p = &data[0])
                AL.BufferData(immediateBuffers[channel][0], AL.Mono16, new IntPtr(p), data.Length * sizeof(short), sampleRate);

            AL.Source(immediateSource[channel], AL.Gain, volume);
            AL.SourceQueueBuffer(immediateSource[channel], immediateBuffers[channel][0]);
            AL.SourcePlay(immediateSource[channel]);
        } 

        public void StopImmediate(int channel)
        {
            Debug.Assert(Platform.IsInMainThread());

            if (immediateSource[channel] >= 0)
            {
                AL.SourceStop(immediateSource[channel]);
                AL.Source(immediateSource[channel], AL.Buffer, 0);
                AL.DeleteBuffers(immediateBuffers[channel]);
                AL.DeleteSource(immediateSource[channel]);

                immediateBuffers[channel] = null;
                immediateSource[channel] = -1; 
            }
        }

        public int ImmediatePlayPosition
        {
            get
            {
                var playPos = -1;

                // TODO : Make sure we support the AL_EXT_OFFSET extension.
                if (immediateSource[0] >= 0)
                {
                    AL.GetSource(immediateSource[0], AL.SourceState, out int state);
                    if (state == AL.Playing)
                        AL.GetSource(immediateSource[0], AL.SampleOffset, out playPos);
                }

                return playPos;
            }
        }
    }

    public static class ALC
    {
        private const string OpenAlcDll = "libopenal32";

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
        private const string OpenAlDll = "libopenal32";

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
