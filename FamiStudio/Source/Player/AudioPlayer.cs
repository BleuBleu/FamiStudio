using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

#if FAMISTUDIO_WINDOWS
using AudioStream = FamiStudio.XAudio2Stream;
#elif FAMISTUDIO_LINUX
using AudioStream = FamiStudio.OpenALStream;
#else
using AudioStream = FamiStudio.PortAudioStream;
#endif

namespace FamiStudio
{
    public class AudioPlayer : BasePlayer
    {
        protected const int DefaultSampleRate = 44100;

        protected AudioStream audioStream;
        protected Thread playerThread;
        protected Semaphore bufferSemaphore;
        protected ManualResetEvent stopEvent = new ManualResetEvent(false);
        protected ConcurrentQueue<short[]> sampleQueue = new ConcurrentQueue<short[]>();
        protected int numBufferedFrames = 3;
        protected IOscilloscope oscilloscope;

        protected AudioPlayer(int apuIndex, bool pal, int sampleRate, int numBuffers) : base(apuIndex, sampleRate)
        {
            int bufferSize = (int)Math.Ceiling(sampleRate / (pal ? NesApu.FpsPAL : NesApu.FpsNTSC)) * sizeof(short);
            numBufferedFrames = numBuffers;
            bufferSemaphore = new Semaphore(numBufferedFrames, numBufferedFrames);
            audioStream = new AudioStream(sampleRate, bufferSize, numBufferedFrames, AudioBufferFillCallback);
        }

        protected short[] AudioBufferFillCallback()
        {
            short[] samples = null;
            if (sampleQueue.TryDequeue(out samples))
            {
                if (oscilloscope != null)
                    oscilloscope.AddSamples(samples);

                // Tell player thread it needs to generate one more frame.
                bufferSemaphore.Release(); 
            }
            //else
            //{
            //    Trace.WriteLine("Audio is starving!");
            //}

            return samples;
        }

        protected void ResetThreadingObjects()
        {
            stopEvent.Reset();
            bufferSemaphore = new Semaphore(numBufferedFrames, numBufferedFrames);
        }

        public override void Shutdown()
        {
            stopEvent.Set();
            if (playerThread != null)
                playerThread.Join();

            audioStream.Dispose();
        }

        public void ConnectOscilloscope(IOscilloscope osc)
        {
            oscilloscope = osc;
        }

        protected override unsafe short[] EndFrame()
        {
            sampleQueue.Enqueue(base.EndFrame());

            // Wait until we have queued the maximum number of buffered frames to start
            // the audio thread, otherwise, we risk starving on the first frame.
            if (!audioStream.IsStarted && sampleQueue.Count == numBufferedFrames)
            {
                // Semaphore should be zero by now.
                Debug.Assert(bufferSemaphore.WaitOne(0) == false);
                audioStream.Start();
            }

            return null;
        }
    };
}
