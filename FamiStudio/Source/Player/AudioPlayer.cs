using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FamiStudio
{
    public abstract class AudioPlayer : BasePlayer
    {
        protected const float MetronomeFirstBeatPitch  = 1.375f;
        protected const float MetronomeFirstBeatVolume = 1.5f;

        public class FrameAudioData
        {
            public short[] samples;
            public int   playPosition;
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

        // Only used when number of buffered emulation frame == 0
        protected FrameAudioData lastFrameAudioData;

        // Used when accurate seeking.
        protected volatile bool abortSeek;
        protected Task seekTask;

        protected abstract bool EmulateFrame();

        public bool IsOscilloscopeConnected => oscilloscope != null;
        public bool IsPlaying => (UsesEmulationThread ? (emulationThread != null || emulationQueue.Count > 0) : (audioStream != null && audioStream.IsPlaying)) || IsSeeking;
        public bool IsSeeking => seekTask != null;

        protected bool UsesEmulationThread => numBufferedFrames > 0;

        public override int PlayPosition
        {
            get 
            {
                // Take the oldest frame in the queue as our play position when using emulation thread.
                if (UsesEmulationThread && emulationQueue.TryPeek(out var data) && data != null)
                    return Math.Max(0, data.playPosition);
                else
                    return base.PlayPosition;
            }
        }

        protected AudioPlayer(IAudioStream stream, int apuIndex, bool pal, int sampleRate, bool stereo, int numFrames) : base(apuIndex, pal, stereo, sampleRate) 
        {
            numBufferedFrames = numFrames;
            audioStream = stream;
            
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
                    return null;
                }

                // Tell emulation thread it should start rendering one more frame.
                emulationSemaphore.Release();
            }
            else
            {
                // First time it wont be null since we call BeginPlaySong.
                if (lastFrameAudioData == null)
                    EmulateFrame();
                data = lastFrameAudioData;
                lastFrameAudioData = null;
            }

            // This means we've reached the end of a non-looping song.
            if (data == null)
            {
                done = true;
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

        protected void AudioStreamStartingCallback()
        {
            if (UsesEmulationThread)
            {
                // Stream is about to start, wait for emulation to pre-fill its buffers.
                while (emulationQueue.Count < numBufferedFrames && !reachedEnd)
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

            audioStream?.Stop();
            audioStream = null;
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
            data.playPosition = playPosition;

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
