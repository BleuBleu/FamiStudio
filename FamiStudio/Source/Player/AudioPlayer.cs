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
#if FAMISTUDIO_LINUX
        protected const int DefaultNumAudioBuffers = 4; // ALSA seems to like to have one extra buffer.
#else
        protected const int DefaultNumAudioBuffers = 3;
#endif
        protected const int DefaultSampleRate = 44100;

        protected AudioStream audioStream;
        protected Thread playerThread;
        protected AutoResetEvent frameEvent = new AutoResetEvent(true);
        protected ManualResetEvent stopEvent = new ManualResetEvent(false);
        protected ConcurrentQueue<short[]> sampleQueue = new ConcurrentQueue<short[]>();
        protected int numBufferedFrames = DefaultNumAudioBuffers;

        protected AudioPlayer(int apuIndex, bool pal, int sampleRate = DefaultSampleRate, int numBuffers = DefaultNumAudioBuffers) : base(apuIndex, sampleRate)
        {
            int bufferSize = (int)Math.Ceiling(sampleRate / (pal ? NesApu.FpsPAL : NesApu.FpsNTSC)) * sizeof(short);
            numBufferedFrames = numBuffers;
            audioStream = new AudioStream(sampleRate, bufferSize, numBufferedFrames, AudioBufferFillCallback);
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
                if (sampleQueue.Count == numBufferedFrames)
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
