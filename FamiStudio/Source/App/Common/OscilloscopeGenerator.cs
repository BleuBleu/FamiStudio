using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class OscilloscopeGenerator : IOscilloscope
    {
        private const float SampleScale = 1.9f; 

        private class SamplesAndTrigger
        {
            public short[] samples;
            public int trigger;
        };

        private Task task;
        private ManualResetEvent stopEvent    = new ManualResetEvent(false);
        private AutoResetEvent   samplesEvent = new AutoResetEvent(false);
        private ConcurrentQueue<SamplesAndTrigger> sampleQueue;
        private OscilloscopeTrigger triggerFunction;
        private int lastTrigger = -1;
        private int lastSampleCount = 0;
        private int holdFrameCount = 0;
        private bool stereo;
        private volatile float[] geometry;
        private volatile bool hasNonZeroData = false;

        private Dictionary<int, short[]> mixDownBuffers = new Dictionary<int, short[]>();

        private int bufferPos = 0;
        private short[] sampleBuffer = new short[16384];

        public OscilloscopeGenerator(bool stereo)
        {
            this.stereo = stereo;
            this.triggerFunction = new PeakSpeedTrigger(sampleBuffer, true);
        }

        public void Start()
        {
            task = Task.Factory.StartNew(OscilloscopeThread, TaskCreationOptions.LongRunning);
            sampleQueue = new ConcurrentQueue<SamplesAndTrigger>();
        }

        public void Stop()
        {
            if (task != null)
            {
                stopEvent.Set();
                task.Wait();
                task = null;
                sampleQueue = null;
            }
        }

        public void AddSamples(short[] samples, int trigger = -1)
        {
            if (task != null)
            {
                sampleQueue.Enqueue(new SamplesAndTrigger() { samples = samples, trigger = trigger });
                samplesEvent.Set();
            }
        }

        public float[] GetGeometry(out bool outHasNonZeroSample)
        {
            outHasNonZeroSample = hasNonZeroData;
            return geometry;
        }

        public bool HasNonZeroSample => hasNonZeroData;

        private void OscilloscopeThread()
        {
            var waitEvents = new WaitHandle[] { stopEvent, samplesEvent };

            while (true)
            {
                int idx = WaitHandle.WaitAny(waitEvents);

                if (idx == 0)
                    break;

                do
                {
                    // On desktop, the main window stops ticking whenever a dialog is present.
                    // Oscilloscope updates will not be rendered during this time, so we don't
                    // need to process it. Mobile handles this differently.
                    if (Platform.IsDesktop && FamiStudioWindow.Instance.IsAsyncDialogInProgress) 
                        break;
                    
                    if (sampleQueue.TryDequeue(out var pair))
                    {
                        var samples = pair.samples;

                        // Mixdown stereo immediately.
                        if (stereo)
                        {
                            if (!mixDownBuffers.TryGetValue(samples.Length, out var mixDownBuffer))
                            {
                                mixDownBuffer = new short[samples.Length / 2];
                                mixDownBuffers.Add(samples.Length, mixDownBuffer);
                            }

                            WaveUtils.MixDown(samples, mixDownBuffer);
                            samples = mixDownBuffer;
                        }

                        var startBufferPos = bufferPos;

                        // Append to circular buffer.
                        if (bufferPos + samples.Length < sampleBuffer.Length)
                        {
                            Array.Copy(samples, 0, sampleBuffer, bufferPos, samples.Length);
                            bufferPos += samples.Length;
                        }
                        else
                        {
                            int batchSize1 = sampleBuffer.Length - bufferPos;
                            int batchSize2 = samples.Length - batchSize1;

                            Array.Copy(samples, 0, sampleBuffer, bufferPos, batchSize1);
                            Array.Copy(samples, batchSize1, sampleBuffer, 0, batchSize2);

                            bufferPos = batchSize2;
                        }

                        var newTrigger = pair.trigger;

                        // TRIGGER_NONE (-2) means the emulation isnt able to provide a trigger, 
                        // we must fallback on analysing the waveform to detect one.
                        if (newTrigger == NesApu.TRIGGER_NONE)
                        {
                            newTrigger = triggerFunction.Detect(startBufferPos, samples.Length);

                            // Ugly fallback.
                            if (newTrigger < 0)
                                newTrigger = startBufferPos;

                            holdFrameCount = 0;
                        }
                        else if (newTrigger >= 0)
                        {
                            newTrigger = (startBufferPos + newTrigger) % sampleBuffer.Length;
                            holdFrameCount = 0;
                        }
                        else
                        {
                            // We can also get TRIGGER_HOLD (-1) here, which mean we do nothing and 
                            // hope for a new trigger "soon". This will happen on very low freqency
                            // notes where the period is longer than 1 frame.
                            holdFrameCount++;
                        }

                        // If we hit this, it means that the emulation code told us a trigger
                        // was eventually coming, but is evidently not. The longest periods we
                        // have at the moment are very low EPSM notes with periods about 8 frames.
                        if (holdFrameCount >= 10)
                            Debug.WriteLine($"WARNING, oscilloscope triggers on hold for {holdFrameCount} frames. Check emulation code.");

                        if (lastTrigger >= 0)
                        {
                            var newHasNonZeroData = false;
                            var vertices = new float[lastSampleCount * 2];

                            var j = lastTrigger - lastSampleCount / 2; 
                            if (j < 0) j += sampleBuffer.Length;

                            for (int i = 0; i < lastSampleCount; i++, j = (j + 1) % sampleBuffer.Length)
                            {
                                var samp = (int)sampleBuffer[j];

                                vertices[i * 2 + 0] = i / (float)(lastSampleCount - 1);
                                vertices[i * 2 + 1] = Utils.Clamp(samp / 32768.0f * SampleScale, -1.0f, 1.0f);

                                newHasNonZeroData |= Math.Abs(samp) > 1024;
                            }

                            // Not exactly atomic... But OK.
                            geometry = vertices;
                            hasNonZeroData = newHasNonZeroData;
                        }

                        lastTrigger = newTrigger;
                        lastSampleCount = samples.Length;
                    }
                    else
                    {
                        break;
                    }
                }
                while (!sampleQueue.IsEmpty);
            }
        }
    }
}
