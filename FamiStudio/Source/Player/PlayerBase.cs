using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace FamiStudio
{
    public class PlayerBase
    {
        protected const int SampleRate = 44100;
        protected const int BufferSize = 734 * sizeof(short); // 734 = ceil(SampleRate / FrameRate) = ceil(44100 / 60.0988)
        protected const int NumAudioBuffers = 3;
        protected const int NumGraphicsBuffers = 2; // Assume D2D is triple buffered, probably true.
        protected const int OutputDelay = NumAudioBuffers - NumGraphicsBuffers;

        protected int apuIndex;
        NesApu.DmcReadDelegate dmcCallback;

        protected XAudio2Stream xaudio2Stream;
        protected Thread playerThread;
        protected AutoResetEvent frameEvent = new AutoResetEvent(true);
        protected ManualResetEvent stopEvent = new ManualResetEvent(false);
        protected ConcurrentQueue<short[]> sampleQueue = new ConcurrentQueue<short[]>();

        protected PlayerBase(int apuIndex)
        {
            this.apuIndex = apuIndex;
        }

        protected void AudioBufferFillCallback(IntPtr data, ref int size)
        {
            short[] samples;
            if (sampleQueue.TryDequeue(out samples))
            {
                Debug.Assert(samples.Length * sizeof(short) <= size);
                Marshal.Copy(samples, 0, data, samples.Length);
                size = samples.Length * sizeof(short);
                frameEvent.Set(); // Wake up player thread.
            }
            else
            {
                Trace.WriteLine("Audio is starving!");
            }
        }

        static int DmcReadCallback(IntPtr data, int addr)
        {
            return FamiStudioForm.StaticProject.GetSampleForAddress(addr - 0xc000);
        }

        public virtual void Initialize()
        {
            dmcCallback = new NesApu.DmcReadDelegate(DmcReadCallback);
            NesApu.NesApuInit(apuIndex, SampleRate, dmcCallback);
            xaudio2Stream = new XAudio2Stream(SampleRate, 16, 1, BufferSize, NumAudioBuffers, AudioBufferFillCallback);
        }

        public virtual void Shutdown()
        {
            stopEvent.Set();
            if (playerThread != null)
                playerThread.Join();

            xaudio2Stream.Dispose();
        }

        protected unsafe void EndFrameAndDiscardSamples()
        {
            NesApu.NesApuEndFrame(apuIndex);
            int numTotalSamples = NesApu.NesApuSamplesAvailable(apuIndex);
            NesApu.NesApuRemoveSamples(apuIndex, numTotalSamples);
        }

        protected unsafe void EndFrameAndQueueSamples()
        {
            NesApu.NesApuEndFrame(apuIndex);

            int numTotalSamples = NesApu.NesApuSamplesAvailable(apuIndex);
            short[] samples = new short[numTotalSamples];

            fixed (short* ptr = &samples[0])
            {
                NesApu.NesApuReadSamples(apuIndex, new IntPtr(ptr), numTotalSamples);
            }

            sampleQueue.Enqueue(samples);

            // Wait until we have queued as many frames as XAudio buffers to start
            // the audio thread, otherwise, we risk starving on the first frame.
            if (!xaudio2Stream.IsStarted)
            {
                if (sampleQueue.Count == NumAudioBuffers)
                {
                    xaudio2Stream.Start();
                }
                else
                {
                    frameEvent.Set();
                }
            }
        }
    };
}
