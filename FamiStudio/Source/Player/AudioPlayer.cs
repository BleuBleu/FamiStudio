using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace FamiStudio
{
    public class AudioPlayer : BasePlayer
    {
        protected const float MetronomeFirstBeatPitch  = 1.375f;
        protected const float MetronomeFirstBeatVolume = 1.5f;

        public class FrameAudioData
        {
            public short[] samples;
            public int   triggerSample = NesApu.TRIGGER_NONE;
            public int   metronomePosition;
            public float metronomePitch  = 1.0f;
            public float metronomeVolume = 1.0f;
        };

        protected IAudioStream audioStream;
        protected Thread playerThread;
        protected Semaphore bufferSemaphore;
        protected ManualResetEvent stopEvent = new ManualResetEvent(false);
        protected ConcurrentQueue<FrameAudioData> sampleQueue = new ConcurrentQueue<FrameAudioData>();
        protected short[] metronomeSound;
        protected int metronomePlayPosition = -1;
        protected int metronomeBeat = -1;
        protected int numBufferedFrames = 3;
        protected IOscilloscope oscilloscope;

        public bool IsOscilloscopeConnected => oscilloscope != null;

        protected AudioPlayer(int apuIndex, bool pal, int sampleRate, bool stereo, int numBuffers) : base(apuIndex, stereo, sampleRate)
        {
            int bufferSize = (int)Math.Ceiling(sampleRate / (pal ? NesApu.FpsPAL : NesApu.FpsNTSC)) * sizeof(short)*2;
            numBufferedFrames = numBuffers;
            bufferSemaphore = new Semaphore(numBufferedFrames, numBufferedFrames);
            audioStream = Platform.CreateAudioStream(sampleRate, stereo, bufferSize, numBufferedFrames, AudioBufferFillCallback);
            registerValues.SetPalMode(pal);
        }

        protected short[] MixSamples(short[] emulation, short[] metronome, int metronomeIndex, float pitch, float volume)
        {
            if (metronome != null)
            {
                var newSamples = new short[emulation.Length];

                var i = 0;
                var j = (float)metronomeIndex;

                if (stereo)
                {
                    Debug.Assert((newSamples.Length & 1) == 0);
                    for (; i < newSamples.Length && (int)j < metronome.Length; i += 2, j += pitch)
                    {
                        newSamples[i + 0] = (short)Utils.Clamp((int)(emulation[i + 0] + metronome[(int)j] * volume), short.MinValue, short.MaxValue);
                        newSamples[i + 1] = (short)Utils.Clamp((int)(emulation[i + 1] + metronome[(int)j] * volume), short.MinValue, short.MaxValue);
                    }
                }
                else
                {
                    for (; i < newSamples.Length && (int)j < metronome.Length; i++, j += pitch)
                        newSamples[i] = (short)Utils.Clamp((int)(emulation[i] + metronome[(int)j] * volume), short.MinValue, short.MaxValue);
                }

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
            FrameAudioData data;
            if (sampleQueue.TryDequeue(out data))
            {
                if (oscilloscope != null)
                    oscilloscope.AddSamples(data.samples, data.triggerSample);

                // Tell player thread it needs to generate one more frame.
                bufferSemaphore.Release(); 

                // Mix in metronome if needed.
                if (data.metronomePosition >= 0)
                    data.samples = MixSamples(data.samples, metronomeSound, data.metronomePosition, data.metronomePitch, data.metronomeVolume);
                return data.samples;
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

        protected virtual FrameAudioData GetFrameAudioData()
        {  
            // Update metronome if there is a beat.
            var metronome = metronomeSound;

            if (beat && beatIndex >= 0 && metronome != null)
                metronomePlayPosition = 0;

            FrameAudioData data = new FrameAudioData();

            data.samples = base.EndFrame();
            data.metronomePosition = metronomePlayPosition;

            if (beatIndex == 0)
            {
                data.metronomePitch  = 1.0f;
                data.metronomeVolume = 1.0f;
            }

            if (metronomePlayPosition >= 0)
            {
                metronomePlayPosition += (int)(data.samples.Length * data.metronomePitch);
                if (metronome == null || metronomePlayPosition >= metronome.Length)
                    metronomePlayPosition = -1;
            }

            return data;
        }

        protected override unsafe short[] EndFrame()
        {
            var data = GetFrameAudioData();

            sampleQueue.Enqueue(data);

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
