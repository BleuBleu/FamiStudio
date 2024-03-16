using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public unsafe class PortAudioStream : IAudioStream
    {
        public enum PaError
        {
            paNoError = 0, paNotInitialized = -10000, paUnanticipatedHostError, paInvalidChannelCount,
            paInvalidSampleRate, paInvalidDevice, paInvalidFlag, paSampleFormatNotSupported,
            paBadIODeviceCombination, paInsufficientMemory, paBufferTooBig, paBufferTooSmall,
            paNullCallback, paBadStreamPtr, paTimedOut, paInternalError,
            paDeviceUnavailable, paIncompatibleHostApiSpecificStreamInfo, paStreamIsStopped, paStreamIsNotStopped,
            paInputOverflowed, paOutputUnderflowed, paHostApiNotFound, paInvalidHostApi,
            paCanNotReadFromACallbackStream, paCanNotWriteToACallbackStream, paCanNotReadFromAnOutputOnlyStream, paCanNotWriteToAnInputOnlyStream,
            paIncompatibleStreamHostApi, paBadBufferPtr
        }

        public enum PaStreamCallbackResult
        {
            Continue = 0,
            Complete = 1,
            Abort = 2
        }

        public enum PaStreamCallbackFlags
        {
            InputUnderflow = 1,
            InputOverflow = 2,
            OutputUnderflow = 4,
            OutputOverflow = 8,
            PrimingOutput = 16
        }

        public enum PaSampleFormat
        {
            Float32 = 1,
            Int32 = 2,
            Int24 = 4,
            Int16 = 8,
            Int8 = 16,
            UInt8 = 32,
            Custom = 0x10000,
            NonInterleaved = ~(0x0FFFFFFF)
        }

        public enum PaHostApiTypeId
        {
            paInDevelopment = 0,
            paDirectSound = 1,
            paMME = 2,
            paASIO = 3,
            paSoundManager = 4,
            paCoreAudio = 5,
            paOSS = 7,
            paALSA = 8,
            paAL = 9,
            paBeOS = 10,
            paWDMKS = 11,
            paJACK = 12,
            paWASAPI = 13,
            paAudioScienceHPI = 14,
            paAudioIO = 15,
            paPulseAudio = 16
        }

        public enum PaWasapiThreadPriority
        {
            eThreadPriorityNone = 0,
            eThreadPriorityAudio,           //!< Default for Shared mode.
            eThreadPriorityCapture,
            eThreadPriorityDistribution,
            eThreadPriorityGames,
            eThreadPriorityPlayback,
            eThreadPriorityProAudio,        //!< Default for Exclusive mode.
            eThreadPriorityWindowManager
        }

        public enum PaWasapiStreamCategory
        {
            eAudioCategoryOther = 0,
            eAudioCategoryCommunications = 3,
            eAudioCategoryAlerts = 4,
            eAudioCategorySoundEffects = 5,
            eAudioCategoryGameEffects = 6,
            eAudioCategoryGameMedia = 7,
            eAudioCategoryGameChat = 8,
            eAudioCategorySpeech = 9,
            eAudioCategoryMovie = 10,
            eAudioCategoryMedia = 11
        }

        public enum PaWasapiStreamOption
        {
            eStreamOptionNone = 0,
            eStreamOptionRaw = 1,
            eStreamOptionMatchFormat = 2
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct PaHostApiInfo
        {
            public int structVersion;
            public PaHostApiTypeId type;
            public IntPtr name;
            public int deviceCount;
            public int defaultInputDevice;
            public int defaultOutputDevice;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct PaStreamParameters
        {
            public int device;
            public int channelCount;
            public uint sampleFormat;
            public double suggestedLatency;
            public IntPtr hostApiSpecificStreamInfo;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct PaDeviceInfo
        {
            public int structVersion;
            public IntPtr name;
            public int hostApi; 
            public int maxInputChannels;
            public int maxOutputChannels;

            public double defaultLowInputLatency;
            public double defaultLowOutputLatency;
            public double defaultHighInputLatency;
            public double defaultHighOutputLatency;

            public double defaultSampleRate;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct PaWasapiStreamInfo
        {
            public uint size;
            public PaHostApiTypeId hostApiType;
            public uint version;
            public uint flags; 
            public uint channelMask;
            public IntPtr hostProcessorOutput;
            public IntPtr hostProcessorInput;
            public PaWasapiThreadPriority threadPriority;
            public PaWasapiStreamCategory streamCategory;
            public PaWasapiStreamOption streamOption;
            public uint passthrough;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct PaWinDirectSoundStreamInfo
        {
            public uint size;
            public PaHostApiTypeId hostApiType; 
            public uint version; 
            public uint flags; 
            public uint framesPerBuffer;
            public uint channelMask;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct PaMacCoreStreamInfo
        {
            public uint size;
            public PaHostApiTypeId hostApiType;
            public uint version;
            public uint flags;
            public int* channelMap;
            public uint channelMapSize; 
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct PaStreamCallbackTimeInfo
        {
            public double inputBufferAdcTime;
            public double currentTime;
            public double outputBufferDacTime;
        }

        public static uint paPrimeOutputBuffersUsingStreamCallback = 8u;
        public static uint paWinWasapiExclusive = 1u;
        public static uint paWinWasapiAutoConvert = 64u;
        public static uint paWinDirectSoundUseLowLevelLatencyParameters = 1u;

#if FAMISTUDIO_WINDOWS
        const string PortAudioLibName = "PortAudio.dll";
#elif FAMISTUDIO_MACOS
        const string PortAudioLibName = "libportaudio.2.dylib";
#else
        const string PortAudioLibName = "libportaudio.so.2.0.0";
#endif

        [DllImport(PortAudioLibName)]
        public static extern PaError Pa_Initialize();

        [DllImport(PortAudioLibName)]
        public static extern PaError Pa_Terminate();

        [DllImport(PortAudioLibName)]
        public static extern int Pa_GetDefaultOutputDevice();

        [DllImport(PortAudioLibName)]
        public static extern PaDeviceInfo* Pa_GetDeviceInfo(int device);

        [DllImport(PortAudioLibName)]
        public static extern PaError Pa_OpenStream(out IntPtr stream, IntPtr inputParameters, ref PaStreamParameters outputParameters, double sampleRate, uint framesPerBuffer, uint streamFlags, PaStreamCallback streamCallback, IntPtr userData);

        [DllImport(PortAudioLibName)]
        public static extern PaError Pa_OpenDefaultStream(out IntPtr stream, int numInputChannels, int numOutputChannels, PaSampleFormat sampleFormat, double sampleRate, uint framesPerBuffer, PaStreamCallback streamCallback, IntPtr userData);

        [DllImport(PortAudioLibName)]
        public static extern PaError Pa_CloseStream(IntPtr stream);

        [DllImport(PortAudioLibName)]
        public static extern PaError Pa_StartStream(IntPtr stream);

        [DllImport(PortAudioLibName)]
        public static extern PaError Pa_StopStream(IntPtr stream);

        [DllImport(PortAudioLibName)]
        public static extern PaError Pa_AbortStream(IntPtr stream);

        [DllImport(PortAudioLibName)]
        public static extern int Pa_GetStreamWriteAvailable(IntPtr stream);

        [DllImport(PortAudioLibName)]
        public static extern PaError Pa_WriteStream(IntPtr stream, IntPtr buffer, uint frames);

        [DllImport(PortAudioLibName)]
        public static extern void Pa_Sleep(int msec);

        [DllImport(PortAudioLibName)]
        public static extern int Pa_GetHostApiCount();

        [DllImport(PortAudioLibName)]
        public static extern int Pa_GetDefaultHostApi();

        [DllImport(PortAudioLibName)]
        public static extern int Pa_HostApiDeviceIndexToDeviceIndex(int hostApi, int hostApiDeviceIndex);

        [DllImport(PortAudioLibName)]
        public static extern PaHostApiInfo* Pa_GetHostApiInfo(int hostApi);

        [DllImport(PortAudioLibName)]
        public static extern int Pa_SetStreamFinishedCallback(IntPtr stream, PaStreamFinishedCallback streamFinishedCallback);

        [DllImport(PortAudioLibName)]
        public static extern int PaMacCore_GetBufferSizeRange(int device, int* minBufferSizeFrames, int* maxBufferSizeFrames);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PaStreamFinishedCallback(IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate PaStreamCallbackResult PaStreamCallback(IntPtr input, IntPtr output, uint frameCount, PaStreamCallbackTimeInfo* timeInfo, PaStreamCallbackFlags statusFlags, IntPtr userData);

        // Wrapping in a class so that assignment is atomic.
        private class ImmediateData
        {
            public int samplesOffset = 0;
            public int sampleRate = 0;
            public short[] samples = null;
        }

        private IntPtr stream = new IntPtr();
        private volatile bool play;
        private GetBufferDataCallback bufferFill;
        private bool stereo;
        private int outputSampleRate;
        private int inputSampleRate;
        private PaStreamCallback streamCallback;
        private PaStreamFinishedCallback streamFinishedCallback;

        private static int deviceIndex = -1;
        private static int deviceSampleRate;
        private static int refCount = 0;

        private int samplesOffset = 0;
        private short[] samples = null;
        private double resampleIndex = 0.0;
        private volatile ImmediateData immediateData = null;

        public bool Stereo => stereo;
        public int InputSampleRate => inputSampleRate;
        public int OutputSampleRate => outputSampleRate;
        public bool IsPlaying => play;
        public static int DeviceSampleRate => deviceSampleRate;

        private PortAudioStream()
        {
        }

        public static PortAudioStream Create(int rate, bool stereo, int bufferSizeMs)
        {
            if (refCount == 0)
            {
                Pa_Initialize();

                for (int i = 0; i < Pa_GetHostApiCount(); i++)
                {
                    var api = Pa_GetHostApiInfo(i);

                    if (Platform.IsWindows && api->type == PaHostApiTypeId.paWASAPI)
                    {
                        deviceIndex = api->defaultOutputDevice;
                    }
                    else if (Platform.IsMacOS && api->type == PaHostApiTypeId.paCoreAudio)
                    {
                        deviceIndex = api->defaultOutputDevice;
                    }

#if DEBUG
                    for (var j = 0; j < api->deviceCount; j++)
                    {
                        var idx = Pa_HostApiDeviceIndexToDeviceIndex(i, j);
                        var dev = Pa_GetDeviceInfo(idx);

                        if (dev->maxOutputChannels > 0)
                            Debug.WriteLine($"Enumerating PortAudio Devices : [{Marshal.PtrToStringAnsi(api->name)}] {Marshal.PtrToStringAnsi(dev->name)} ({idx})");
                    }
#endif
                }

                if (deviceIndex >= 0)
                {
                    var dev = Pa_GetDeviceInfo(deviceIndex);
                    deviceSampleRate = (int)dev->defaultSampleRate;
                    Debug.WriteLine($"Using Device {Marshal.PtrToStringAnsi(dev->name)} ({deviceIndex})");
                }
                else
                {
                    Debug.WriteLine($"No audio device found!");
                }
            }

            if (deviceIndex < 0)
                return null;

            var deviceInfo = Pa_GetDeviceInfo(deviceIndex);
            var portAudioStream = new PortAudioStream();

            portAudioStream.streamCallback = new PaStreamCallback(portAudioStream.StreamCallback);
            portAudioStream.streamFinishedCallback = new PaStreamFinishedCallback(portAudioStream.StreamFinishedCallback);
            portAudioStream.stereo = stereo;
            portAudioStream.inputSampleRate = rate;
            portAudioStream.outputSampleRate = rate;
            portAudioStream.play = false;

            var wasapiStreamInfo = new PaWasapiStreamInfo();
            wasapiStreamInfo.size = (uint)sizeof(PaWasapiStreamInfo);
            wasapiStreamInfo.hostApiType = PaHostApiTypeId.paWASAPI;
            wasapiStreamInfo.version = 1;
            wasapiStreamInfo.flags = paWinWasapiAutoConvert; 
            wasapiStreamInfo.streamCategory = PaWasapiStreamCategory.eAudioCategoryMedia;
            
            var streamParams = new PaStreamParameters();
            streamParams.device = deviceIndex;
            streamParams.channelCount = stereo ? 2 : 1;
            streamParams.sampleFormat = (uint)PaSampleFormat.Int16;
            streamParams.suggestedLatency = bufferSizeMs / 1000.0; 
            streamParams.hostApiSpecificStreamInfo = Platform.IsWindows ? new IntPtr(&wasapiStreamInfo) : IntPtr.Zero;

            // First try with the input sample rate.
            // Some low-level APIs such as WASAPI will refuse and force you to use the device sample rate for lower-latency (at least without paWinWasapiAutoConvert).
            var error = Pa_OpenStream(out portAudioStream.stream, IntPtr.Zero, ref streamParams, portAudioStream.outputSampleRate, 0, paPrimeOutputBuffersUsingStreamCallback, portAudioStream.streamCallback, IntPtr.Zero);

            if (error == PaError.paInvalidSampleRate)
            {
                // Use the device rate, well resample the data ourselves.
                portAudioStream.outputSampleRate = deviceSampleRate;
                error = Pa_OpenStream(out portAudioStream.stream, IntPtr.Zero, ref streamParams, portAudioStream.outputSampleRate, 0, paPrimeOutputBuffersUsingStreamCallback, portAudioStream.streamCallback, IntPtr.Zero);
            }

            if (error != PaError.paNoError)
                return null;

            Pa_SetStreamFinishedCallback(portAudioStream.stream, portAudioStream.streamFinishedCallback);

            // Ideally we would call Pa_StartStream/Pa_StopStream in Start/Stop, but on both MacOS and Windows,
            // there are a couple of issues with that. 
            //
            // - At the end of a non-looping song, returning paComplete should play all the queue buffer and 
            //   stop the stream, but most backends interpret paComplete as paAbort and just stops immediately
            //   so stopping a stream at end of a song is tricky.
            //
            // - When re-starting a stream, PortAudio will often play the last samples of the last play and i found
            //   no way to work around this. 
            //
            // Instead will keep the stream running at all time and output zeroe\s where there is nothing to play.

            Pa_StartStream(portAudioStream.stream);

            refCount++;

            return portAudioStream;
        }

        public void Start(GetBufferDataCallback bufferFillCallback, StreamStartingCallback streamStartCallback)
        {
            Debug.Assert(!play);

            bufferFill = bufferFillCallback;
            resampleIndex = 0.0;
            samples = null;
            samplesOffset = 0;
            streamStartCallback();
            play = true;
        }

        public void Stop()
        {
            StopImmediate();

            // The play flag can only be toggle when we arent inside the callback,
            // otherwise, we may return immediately, then the callback may call
            // bufferFill() after the stream has been stopped.
            lock (this)
            {
                play = false;
            }
        }

        public void Dispose()
        {
            Stop();

            if (stream != IntPtr.Zero)
            {
                Pa_AbortStream(stream);
                Pa_CloseStream(stream);
                stream = IntPtr.Zero;
            }

            if (--refCount == 0)
            {
                Pa_Terminate();
                deviceIndex = -1;
                deviceSampleRate = 0;
            }
        }

        PaStreamCallbackResult StreamCallback(IntPtr input, IntPtr output, uint sampleCount, PaStreamCallbackTimeInfo* timeInfo, PaStreamCallbackFlags statusFlags, IntPtr userData)
        {
            var outPtr = output;

            if (stereo)
	            sampleCount *= 2;

            lock (this)
            {
                if (play)
                {
                    do
                    {
                        if (samplesOffset == 0)
                        {
                            while (true)
                            {
                                var newSamples = bufferFill(out var done);

                                if (done)
                                {
                                    // If we are done, pad the last buffer with zeros.
                                    newSamples = new short[sampleCount - samplesOffset];
                                    play = false;
                                }

                                if (newSamples != null)
                                {
                                    samples = WaveUtils.ResampleStream(samples, newSamples, inputSampleRate, outputSampleRate, stereo, ref resampleIndex);
                                    samples = MixImmediateData(samples, samples == newSamples); // Samples are read-only, need to duplicate if we didn't resample.
                                    break;
                                }

                                Pa_Sleep(0);
                            }
                        }

                        var numSamplesToCopy = (int)Math.Min(sampleCount, samples.Length - samplesOffset);
                        Marshal.Copy(samples, samplesOffset, outPtr, numSamplesToCopy);

                        samplesOffset += numSamplesToCopy;
                        if (samplesOffset == samples.Length)
                            samplesOffset = 0;

                        outPtr = IntPtr.Add(outPtr, numSamplesToCopy * sizeof(short));
                        sampleCount = (uint)(sampleCount - numSamplesToCopy);
                    }
                    while (sampleCount != 0);
                }
                else
                {
                    Platform.ZeroMemory(output, (int)sampleCount * sizeof(short));
                }
            }

            return PaStreamCallbackResult.Continue;
        }

        short[] MixImmediateData(short[] samples, bool duplicate)
        {
            // Mix in immediate data if any, storing in variable since main thread can change it anytime.
            var immData = immediateData;
            if (immData != null && immData.samplesOffset < immData.samples.Length)
            {
                var channelCount = stereo ? 2 : 1;
                var sampleCount = Math.Min(samples.Length, (immData.samples.Length - immData.samplesOffset) * channelCount);
                var outputSamples = duplicate ? (short[])samples.Clone() : samples;

                for (int i = 0, j = immData.samplesOffset; i < sampleCount; i++, j += (i % channelCount) == 0 ? 1 : 0)
                    outputSamples[i] = (short)Math.Clamp(samples[i] + immData.samples[j], short.MinValue, short.MaxValue);

                immData.samplesOffset += sampleCount / channelCount;
                return outputSamples;
            }

            return samples;
        }

        void StreamFinishedCallback(IntPtr userData)
        {
            Debug.WriteLine("*** StreamFinishedCallback");
            play = false;
        }

        public unsafe void PlayImmediate(short[] samples, int sampleRate, float volume, int channel = 0)
        {
            Debug.Assert(Platform.IsInMainThread());

            StopImmediate();

            // Cant find volume adjustment in port audio.
            short vol = (short)(volume * 32768);

            var adjustedSamples = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
                adjustedSamples[i] = (short)((samples[i] * vol) >> 15);

            var data = new ImmediateData();
            data.sampleRate = sampleRate;
            data.samplesOffset = 0;
            data.samples = WaveUtils.ResampleBuffer(adjustedSamples, sampleRate, outputSampleRate, false);
            immediateData = data;
        }

        public void StopImmediate()
        {
            Debug.Assert(Platform.IsInMainThread());
            immediateData = null;
        }

        public int ImmediatePlayPosition
        {
            get
            {
                var data = immediateData;
                return data != null && data.samplesOffset < data.samples.Length ? data.samplesOffset * data.sampleRate / outputSampleRate : -1;
            }
        }
    }
}
