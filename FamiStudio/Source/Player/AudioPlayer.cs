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
        protected AudioStream audioStream;
        protected Thread playerThread;
        protected AutoResetEvent frameEvent = new AutoResetEvent(true);
        protected ManualResetEvent stopEvent = new ManualResetEvent(false);
        protected ConcurrentQueue<short[]> sampleQueue = new ConcurrentQueue<short[]>();

        protected AudioPlayer(int apuIndex, int sampleRate = 44100) : base(apuIndex, sampleRate)
        {
            // Assume we are in PAL mode since it will always have a larger buffer.
            int bufferSize = (int)Math.Ceiling(sampleRate / 50.0070) * sizeof(short);
            audioStream = new AudioStream(sampleRate, bufferSize, NumAudioBuffers, AudioBufferFillCallback);
        }

        protected short[] AudioBufferFillCallback()
        {
            short[] samples = null;
            if (sampleQueue.TryDequeue(out samples))
            {
                frameEvent.Set(); // Wake up player thread.
            }
            //else
            //{
            //    Trace.WriteLine("Audio is starving!");
            //}

            return samples;
        }

        public override void Shutdown()
        {
            stopEvent.Set();
            if (playerThread != null)
                playerThread.Join();

            audioStream.Dispose();
        }

        protected override unsafe short[] EndFrame()
        {
            sampleQueue.Enqueue(base.EndFrame());

            // Wait until we have queued as many frames as XAudio buffers to start
            // the audio thread, otherwise, we risk starving on the first frame.
            if (!audioStream.IsStarted)
            {
                if (sampleQueue.Count == NumAudioBuffers)
                {
                    audioStream.Start();
                }
                else
                {
                    frameEvent.Set();
                }
            }

            return null;
        }
    };
}
