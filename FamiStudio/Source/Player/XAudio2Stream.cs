using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class XAudio2Stream
    {
        private XAudio2 xaudio2;
        private MasteringVoice masteringVoice;
        private WaveFormat waveFormat;
        private SourceVoice sourceVoice;
        private AudioBuffer[] audioBuffersRing;
        private DataPointer[] memBuffers;
        private Semaphore bufferSemaphore;
        private ManualResetEvent quitEvent;
        private GetBufferDataCallback bufferFill;
        private Task playingTask;

        public delegate short[] GetBufferDataCallback();

        public XAudio2Stream(int rate, int channels, int bufferSize, int numBuffers, GetBufferDataCallback bufferFillCallback)
        {
            xaudio2 = new XAudio2();
            //xaudio2 = new XAudio2(XAudio2Version.Version27); // To simulate Windows 7 behavior.
            //xaudio2.CriticalError += Xaudio2_CriticalError;
            masteringVoice = new MasteringVoice(xaudio2);
            waveFormat = new WaveFormat(rate, 16, channels);
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

        private void Xaudio2_CriticalError(object sender, ErrorEventArgs e)
        {
            Debug.WriteLine("CRITICAL ERROR!");
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

            masteringVoice.DestroyVoice();
            masteringVoice.Dispose();
            masteringVoice = null;

            xaudio2.Dispose();
            xaudio2 = null;
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

                var size = memBuffers[nextBuffer].Size;
                var data = bufferFill();
                if (data != null)
                {
                    size = data.Length * sizeof(short);
                    Debug.Assert(data.Length * sizeof(short) <= memBuffers[nextBuffer].Size);
                    Marshal.Copy(data, 0, memBuffers[nextBuffer].Pointer, data.Length);
                }

                audioBuffersRing[nextBuffer].AudioDataPointer = memBuffers[nextBuffer].Pointer;
                audioBuffersRing[nextBuffer].AudioBytes = size;

                sourceVoice.SubmitSourceBuffer(audioBuffersRing[nextBuffer], null);

                nextBuffer = ++nextBuffer % audioBuffersRing.Length;
            }
        }
    }
}
