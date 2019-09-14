using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class PortAudioStream
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

        [DllImport("libportaudio.2.dylib")]
        public static extern PaError Pa_Initialize();

        [DllImport("libportaudio.2.dylib")]
        public static extern PaError Pa_Terminate();

        [DllImport("libportaudio.2.dylib")]
        public static extern int Pa_GetDefaultOutputDevice();

        [DllImport("libportaudio.2.dylib")]
        public static extern PaError Pa_OpenDefaultStream(out IntPtr stream, int numInputChannels, int numOutputChannels, PaSampleFormat sampleFormat, double sampleRate, uint framesPerBuffer, PaStreamCallback streamCallback, IntPtr userData);

        [DllImport("libportaudio.2.dylib")]
        public static extern PaError Pa_CloseStream(IntPtr stream);

        [DllImport("libportaudio.2.dylib")]
        public static extern PaError Pa_StartStream(IntPtr stream);

        [DllImport("libportaudio.2.dylib")]
        public static extern PaError Pa_StopStream(IntPtr stream);

        [DllImport("libportaudio.2.dylib")]
        public static extern PaError Pa_AbortStream(IntPtr stream);

        [DllImport("libportaudio.2.dylib")]
        public static extern int Pa_GetStreamWriteAvailable(IntPtr stream);

        [DllImport("libportaudio.2.dylib")]
        public static extern PaError Pa_WriteStream(IntPtr stream, IntPtr buffer, uint frames);

        [DllImport("libportaudio.2.dylib")]
        public static extern void Pa_Sleep(int msec);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate PaStreamCallbackResult PaStreamCallback(IntPtr input, IntPtr output, uint frameCount, IntPtr timeInfo, PaStreamCallbackFlags statusFlags, IntPtr userData);

        public delegate short[] GetBufferDataCallback();

        private IntPtr stream = new IntPtr();
        private Task playingTask;
        private bool stop = false;
        private GetBufferDataCallback bufferFill;
        private static int refCount = 0;

        public PortAudioStream(int rate, int channels, int bufferSize, int numBuffers, GetBufferDataCallback bufferFillCallback)
        {
            if (refCount == 0)
            {
                Pa_Initialize();
                refCount++;
            }

            Pa_OpenDefaultStream(out stream, 0, 1, PaSampleFormat.Int16, 44100, 0, null, IntPtr.Zero);
            bufferFill = bufferFillCallback;
        }

        public void Start()
        {
            stop = false;
            Pa_StartStream(stream);
            playingTask = Task.Factory.StartNew(PlayAsync, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            if (playingTask != null)
            {
                //Pa_StopStream(stream);
                Pa_AbortStream(stream); // Sleep slightly faster?
                stop = true;
                playingTask.Wait();
                playingTask = null;
            }
        }

        public bool IsStarted => playingTask != null;

        public void Dispose()
        {
            Stop();

            if (stream != IntPtr.Zero)
            {
                var err = Pa_CloseStream(stream);
                stream = IntPtr.Zero;
            }

            refCount--;
            if (refCount == 0)
            {
                Pa_Terminate();
            }
        }

        private unsafe void PlayAsync()
        {
            short[] samples = null;

            while (!stop)
            {
                if (samples == null)
                {
                    samples = bufferFill();
                }

                if (samples != null)
                {
                    int avail = Pa_GetStreamWriteAvailable(stream);

                    if (avail >= samples.Length)
                    {
                        fixed (short* p = &samples[0])
                            Pa_WriteStream(stream, new IntPtr(p), (uint)samples.Length);
                        samples = null;
                    }
                }

                Pa_Sleep(4);
            }
        }
    }
}
