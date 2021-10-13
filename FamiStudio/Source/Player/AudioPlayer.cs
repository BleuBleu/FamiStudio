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
        protected const float MetronomeFirstBeatPitch  = 1.375f;
        protected const float MetronomeFirstBeatVolume = 1.5f;

        public struct SamplePair
        {
            public short[] samples;
            public int   metronomePosition;
            public float metronomePitch;
            public float metronomeVolume;
        };

        protected AudioStream audioStream;
        protected Thread playerThread;
        protected Semaphore bufferSemaphore;
        protected ManualResetEvent stopEvent = new ManualResetEvent(false);
        protected ConcurrentQueue<SamplePair> sampleQueue = new ConcurrentQueue<SamplePair>();
        protected short[] metronomeSound;
        protected int metronomePlayPosition = -1;
        protected int metronomeBeat = -1;
        protected int numBufferedFrames = 3;
        protected IOscilloscope oscilloscope;

        protected AudioPlayer(int apuIndex, bool pal, int sampleRate, int numBuffers) : base(apuIndex, sampleRate)
        {
            int bufferSize = (int)Math.Ceiling(sampleRate / (pal ? NesApu.FpsPAL : NesApu.FpsNTSC)) * sizeof(short)*2;
            numBufferedFrames = numBuffers;
            bufferSemaphore = new Semaphore(numBufferedFrames, numBufferedFrames);
            audioStream = new AudioStream(sampleRate, bufferSize, numBufferedFrames, AudioBufferFillCallback);
        }

        protected short[] MixSamples(short[] emulation, short[] metronome, int metronomeIndex, float pitch, float volume)
        {
            if (metronome != null)
            {
                var newSamples = new short[emulation.Length];
                var metronomeIdx = metronomeIndex;

                var i = 0;
                var j = (float)metronomeIndex;

                for (; i < newSamples.Length && (int)j < metronome.Length; i++, j += pitch)
                    newSamples[i] = (short)Utils.Clamp((int)(emulation[i] + metronome[(int)j] * volume), short.MinValue, short.MaxValue);

                if (i != newSamples.Length)
                    Array.Copy(emulation, i, newSamples, i, newSamples.Length - i);

                return newSamples;
            }
            else
            {
                return emulation;
            }
        }

        protected short[] AudioBufferFillCallback()
        {
            SamplePair pair;
            if (sampleQueue.TryDequeue(out pair))
            {
                if (oscilloscope != null)
                    oscilloscope.AddSamples(pair.samples);

                // Tell player thread it needs to generate one more frame.
                bufferSemaphore.Release(); 

                // Mix in metronome if needed.
                if (pair.metronomePosition >= 0)
                    pair.samples = MixSamples(pair.samples, metronomeSound, pair.metronomePosition, pair.metronomePitch, pair.metronomeVolume);
                return pair.samples;
            }
            else
            {
                // Trace.WriteLine("Audio is starving!");
                return null;
            }
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

        public void SetMetronomeSound(short[] sound)
        {
            metronomeSound = sound;
        }

        protected override unsafe short[] EndFrame()
        {
            // Update metronome if there is a beat.
            var metronome = metronomeSound;

            if (beat && beatIndex >= 0 && metronome != null)
                metronomePlayPosition = 0;

            SamplePair pair = new SamplePair();

            pair.samples = base.EndFrame();
            pair.metronomePosition = metronomePlayPosition;
            pair.metronomePitch  = beatIndex == 0 ? MetronomeFirstBeatPitch  : 1.0f;
            pair.metronomeVolume = beatIndex == 0 ? MetronomeFirstBeatVolume : 1.0f;

            if (metronomePlayPosition >= 0)
            {
                metronomePlayPosition += (int)(pair.samples.Length * pair.metronomePitch);
                if (metronome == null || metronomePlayPosition >= metronome.Length)
                    metronomePlayPosition = -1;
            }

            sampleQueue.Enqueue(pair);

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

        public void PlayRawPcmSample(short[] data, int sampleRate, float volume)
        {
            audioStream.PlayImmediate(data, sampleRate, volume);
        }

        public int RawPcmSamplePlayPosition => audioStream.ImmediatePlayPosition;
    };
}
