using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace FamiStudio
{
    public abstract class AudioPlayer : BasePlayer
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
        protected short[] metronomeSound;
        protected int metronomePlayPosition = -1;
        protected int metronomeBeat = -1;
        protected int numBufferedFrames = 3;
        protected IOscilloscope oscilloscope;

        // These are only used when the number of buffered emulation frame > 0
        protected Thread emulationThread;
        protected Semaphore emulationSemaphore;
        protected ManualResetEvent stopEvent;
        protected ConcurrentQueue<FrameAudioData> emulationQueue;
        protected bool shouldStopStream = false;
        protected bool emulationDone = false;

        // Only used when number of buffered emulation frame == 0
        protected FrameAudioData lastFrameAudioData;

        protected abstract bool EmulateFrame();

        public bool IsOscilloscopeConnected => oscilloscope != null;
        protected bool UsesEmulationThread => numBufferedFrames > 0;

        protected AudioPlayer(int apuIndex, bool pal, int sampleRate, bool stereo, int bufferSizeMs, int numFrames) : base(apuIndex, stereo, sampleRate)
        {
            numBufferedFrames = numFrames;
            audioStream = Platform.CreateAudioStream(sampleRate, stereo, bufferSizeMs, AudioBufferFillCallback, AudioStreamStartingCallback);
            registerValues.SetPalMode(pal);
            
            if (UsesEmulationThread)
            {
                stopEvent = new ManualResetEvent(false);
                emulationSemaphore = new Semaphore(numBufferedFrames, numBufferedFrames);
                emulationQueue = new ConcurrentQueue<FrameAudioData>();
            }
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

        protected short[] AudioBufferFillCallback(out bool done)
        {
            FrameAudioData data;

            done = false;

            if (UsesEmulationThread)
            {
                if (!emulationQueue.TryDequeue(out data))
                {
                    // Trace.WriteLine("Audio is starving!");
                    done = shouldStopStream;
                    return null;
                }

                // Tell emulation thread it should start rendering one more frame.
                emulationSemaphore.Release();
            }
            else
            {
                // First time it wont be null since we call BeginPlaySong.
                if (lastFrameAudioData == null)
                    shouldStopStream |= !EmulateFrame();
                done = shouldStopStream;
                data = lastFrameAudioData;
                lastFrameAudioData = null;

                if (done)
                    return null;
            }

            // If we are driving the toolbar oscilloscope, provide samples.
            if (oscilloscope != null)
                oscilloscope.AddSamples(data.samples, data.triggerSample);

            // Mix in metronome if needed.
            if (data.metronomePosition >= 0)
                data.samples = MixSamples(data.samples, metronomeSound, data.metronomePosition, data.metronomePitch, data.metronomeVolume);

            return data.samples;
        }

        void AudioStreamStartingCallback()
        {
            if (UsesEmulationThread && !shouldStopStream)
            {
                // Stream is about to start, wait for emulation to pre-fill its buffers.
                while (emulationQueue.Count < numBufferedFrames && !emulationDone)
                    Thread.Sleep(1);
            }
        }

        protected void ResetThreadingObjects()
        {
            stopEvent.Reset();
            emulationSemaphore = new Semaphore(numBufferedFrames, numBufferedFrames);
        }

        public override void Shutdown()
        {
            if (UsesEmulationThread)
            {
                stopEvent.Set();
                if (emulationThread != null)
                    emulationThread.Join();
            }

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
                data.metronomePitch  = MetronomeFirstBeatPitch;
                data.metronomeVolume = MetronomeFirstBeatVolume;
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

            if (UsesEmulationThread)
            {
                emulationQueue.Enqueue(data);
            }
            else
            {
                Debug.Assert(lastFrameAudioData == null);
                lastFrameAudioData = data;
            }

            return null;
        }

        public void PlayRawPcmSample(short[] data, int sampleRate, float volume, int channel = 0)
        {
            audioStream.PlayImmediate(data, sampleRate, volume, channel);
        }

        public int RawPcmSamplePlayPosition => audioStream.ImmediatePlayPosition;
    };
}
