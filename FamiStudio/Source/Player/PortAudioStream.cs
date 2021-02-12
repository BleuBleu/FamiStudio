﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
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

#if FAMISTUDIO_MACOS
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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate PaStreamCallbackResult PaStreamCallback(IntPtr input, IntPtr output, uint frameCount, IntPtr timeInfo, PaStreamCallbackFlags statusFlags, IntPtr userData);

        public delegate short[] GetBufferDataCallback();

        private IntPtr stream = new IntPtr();
        private bool playing = false;
        private GetBufferDataCallback bufferFill;
        private static int refCount = 0;
        private PaStreamCallback streamCallback;

        private IntPtr  immediateStream = new IntPtr();
        private short[] immediateStreamData = null;
        private int     immediateStreamPosition = -1;
        private PaStreamCallback immediateStreamCallback;

        public PortAudioStream(int rate, int bufferSize, int numBuffers, GetBufferDataCallback bufferFillCallback)
        {
            if (refCount == 0)
            {
                Pa_Initialize();
                refCount++;
            }

            streamCallback = new PaStreamCallback(StreamCallback);
            immediateStreamCallback = new PaStreamCallback(ImmediateStreamCallback);

            Pa_OpenDefaultStream(out stream, 0, 1, PaSampleFormat.Int16, rate, 0, streamCallback, IntPtr.Zero);
            bufferFill = bufferFillCallback;
        }

        public void Start()
        {
            Pa_StartStream(stream);
            playing = true;
        }

        public void Stop()
        {
            if (playing)
            {
                Pa_AbortStream(stream); // Sleep slightly faster?
                playing = false;
            }
        }

        public bool IsStarted => playing;

        public void Dispose()
        {
            Stop();

            if (stream != IntPtr.Zero)
            {
                var err = Pa_CloseStream(stream);
                stream = IntPtr.Zero;
            }

            StopImmediate();

            refCount--;
            if (refCount == 0)
            {
                Pa_Terminate();
            }
        }

        short[] lastSamples = null;
        int     lastSamplesOffset = 0;

        PaStreamCallbackResult StreamCallback(IntPtr input, IntPtr output, uint frameCount, IntPtr timeInfo, PaStreamCallbackFlags statusFlags, IntPtr userData)
        {
            var outPtr = output;

            do
            {
                if (lastSamplesOffset == 0)
                {
                    while (true)
                    {
                        lastSamples = bufferFill();
                        if (lastSamples != null)
                            break;
                        Pa_Sleep(0);
                    }
                }

                int numSamplesToCopy = (int)Math.Min(frameCount, lastSamples.Length - lastSamplesOffset);
                Marshal.Copy(lastSamples, lastSamplesOffset, outPtr, numSamplesToCopy);

                lastSamplesOffset += numSamplesToCopy;
                if (lastSamplesOffset == lastSamples.Length)
                    lastSamplesOffset = 0;

                outPtr = IntPtr.Add(outPtr, numSamplesToCopy * sizeof(short));
                frameCount = (uint)(frameCount - numSamplesToCopy);
            }
            while (frameCount != 0);

            return PaStreamCallbackResult.Continue;
        }

        PaStreamCallbackResult ImmediateStreamCallback(IntPtr input, IntPtr output, uint frameCount, IntPtr timeInfo, PaStreamCallbackFlags statusFlags, IntPtr userData)
        {
            int numSamplesToCopy = (int)Math.Min(frameCount, immediateStreamData.Length - immediateStreamPosition);

            if (numSamplesToCopy <= 0)
                return PaStreamCallbackResult.Abort;

            Marshal.Copy(immediateStreamData, immediateStreamPosition, output, numSamplesToCopy);
            immediateStreamPosition += (int)frameCount;

            if (immediateStreamPosition >= immediateStreamData.Length)
            {
                immediateStreamData = null;
                immediateStreamPosition = -1;

                return PaStreamCallbackResult.Complete;
            }
            else
            {
                return PaStreamCallbackResult.Continue;
            }
        }

        public unsafe void PlayImmediate(short[] data, int sampleRate, float volume)
        {
            StopImmediate();

            Pa_OpenDefaultStream(out immediateStream, 0, 1, PaSampleFormat.Int16, sampleRate, 0, immediateStreamCallback, IntPtr.Zero);

            if (immediateStream != IntPtr.Zero)
            {
                // Cant find volume adjustment in port audio.
                short vol = (short)(volume * 32768);

                immediateStreamData = new short[data.Length];
                for (int i = 0; i < data.Length; i++)
                    immediateStreamData[i] = (short)((data[i] * vol) >> 15);

                immediateStreamPosition = 0;
                Pa_StartStream(immediateStream);
            }
        }

        public void StopImmediate()
        {
            if (immediateStream != IntPtr.Zero)
            {
                Pa_AbortStream(immediateStream);
                //Pa_StopStream(immediateStream);
                Pa_CloseStream(immediateStream);
                immediateStream = IntPtr.Zero;
                immediateStreamData = null;
                immediateStreamPosition = -1;
            }
        }

        public int ImmediatePlayPosition
        {
            get
            {
                return immediateStreamPosition;
            }
        }
    }
}
