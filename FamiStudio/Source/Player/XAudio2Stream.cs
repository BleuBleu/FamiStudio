using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class XAudio2Stream : IDisposable
    {
        private XAudio2 xaudio2;
        private MasteringVoice masteringVoice;
        private WaveFormat waveFormat;
        private SourceVoice sourceVoice;
        private AudioBuffer[] audioBuffersRing;
        private DataPointer[] memBuffers;
        private Semaphore bufferSemaphore;
        private ManualResetEvent quitEvent;
        private BufferFillEventHandler bufferFill;
        private Task playingTask;

        public delegate void BufferFillEventHandler(IntPtr data, ref int size);

        public XAudio2Stream(int rate, int bits, int channels, int bufferSize, int numBuffers, BufferFillEventHandler bufferFillCallback)
        {
            xaudio2 = new XAudio2();
            masteringVoice = new MasteringVoice(xaudio2);
            waveFormat = new WaveFormat(rate, bits, channels);
            audioBuffersRing = new AudioBuffer[numBuffers];
            memBuffers = new DataPointer[audioBuffersRing.Length];

            for (int i = 0; i < audioBuffersRing.Length; i++)
            {
                audioBuffersRing[i] = new AudioBuffer();
                memBuffers[i].Size = bufferSize;
                memBuffers[i].Pointer = Utilities.AllocateMemory(memBuffers[i].Size);
            }

            bufferFill = bufferFillCallback;
            bufferSemaphore = new Semaphore(numBuffers, numBuffers);
            quitEvent = new ManualResetEvent(false);
        }

        private void SourceVoice_BufferEnd(IntPtr obj)
        {
            bufferSemaphore.Release();
        }

        public bool IsStarted => sourceVoice != null;

        public void Start()
        {
            Debug.Assert(sourceVoice == null);

            sourceVoice = new SourceVoice(xaudio2, waveFormat);
            sourceVoice.BufferEnd += SourceVoice_BufferEnd;
            sourceVoice.Start();

            quitEvent.Reset();

            try
            {
                while (true) bufferSemaphore.Release();
            }
            catch (SemaphoreFullException)
            {
            }

            playingTask = Task.Factory.StartNew(PlayAsync, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            if (playingTask != null)
            {
                quitEvent.Set();
                playingTask.Wait();
                playingTask = null;
            }

            if (sourceVoice != null)
            {
                sourceVoice.Stop();
                sourceVoice.FlushSourceBuffers();
                sourceVoice.DestroyVoice();
                sourceVoice.BufferEnd -= SourceVoice_BufferEnd;
                sourceVoice.Dispose();
                sourceVoice = null;
            }
        }

        public void Dispose()
        {
            Stop();

            for (int i = 0; i < audioBuffersRing.Length; i++)
            {
                Utilities.FreeMemory(memBuffers[i].Pointer);
                memBuffers[i].Pointer = IntPtr.Zero;
            }

            masteringVoice.Dispose();
            xaudio2.Dispose();
        }

        private void PlayAsync()
        {
            int nextBuffer = 0;
            var waitEvents = new WaitHandle[] { quitEvent, bufferSemaphore };

            while (true)
            {
                int idx = WaitHandle.WaitAny(waitEvents);

                if (idx == 0)
                {
                    break;
                }

                int size = memBuffers[nextBuffer].Size;
                bufferFill(memBuffers[nextBuffer].Pointer, ref size);
                Debug.Assert(size <= memBuffers[nextBuffer].Size);

                audioBuffersRing[nextBuffer].AudioDataPointer = memBuffers[nextBuffer].Pointer;
                audioBuffersRing[nextBuffer].AudioBytes = size;

                sourceVoice.SubmitSourceBuffer(audioBuffersRing[nextBuffer], null);

                nextBuffer = ++nextBuffer % audioBuffersRing.Length;
            }
        }
    }
}
