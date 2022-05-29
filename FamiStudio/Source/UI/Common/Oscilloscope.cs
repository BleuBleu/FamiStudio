using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class Oscilloscope : IOscilloscope
    {
        private const float SampleScale = 1.9f; 
        private const int   NumSamples  = 1024;

        private Task task;
        private ManualResetEvent stopEvent    = new ManualResetEvent(false);
        private AutoResetEvent   samplesEvent = new AutoResetEvent(false);
        private ConcurrentQueue<short[]> sampleQueue;
        private int lastBufferPos = 0;
        private int numVertices = 128;
        private bool stereo;
        private volatile float[,] geometry;
        private volatile bool hasNonZeroData = false;

        private int bufferPos = 0;
        private short[] sampleBuffer = new short[NumSamples * 2];

        public Oscilloscope(int scaling, bool stereo)
        {
            this.numVertices *= Math.Min(2, scaling);
            this.stereo = stereo;
        }

        public void Start()
        {
            task = Task.Factory.StartNew(OscilloscopeThread, TaskCreationOptions.LongRunning);
            sampleQueue = new ConcurrentQueue<short[]>();
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

        public void AddSamples(short[] samples)
        {
            if (task != null)
            {
                sampleQueue.Enqueue(samples);
                samplesEvent.Set();
            }
        }

        public float[,] GetGeometry(out bool outHasNonZeroSample)
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
                    if (sampleQueue.TryDequeue(out var samples))
                    {
                        // Mixdown stereo immediately.
                        if (stereo)
                        {
                            // TODO : Use a static buffer for mixing down instead of allocating.
                            samples = WaveUtils.MixDown(samples);
                        }

                        Debug.Assert(samples.Length <= NumSamples);

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

                        var numSamplesSinceLastRender = (bufferPos + sampleBuffer.Length - lastBufferPos) % sampleBuffer.Length;
                        var updateGeometry = numSamplesSinceLastRender >= NumSamples;

                        if (updateGeometry)
                        {
                            int lookback = 0;
                            int maxLookback = numSamplesSinceLastRender / 2;
                            int centerIdx = (lastBufferPos + numSamplesSinceLastRender / 2) % sampleBuffer.Length;
                            int orig = sampleBuffer[centerIdx];

                            // If sample is negative, go back until positive.
                            if (orig < 0)
                            {
                                while (lookback < maxLookback)
                                {
                                    if (--centerIdx < 0)
                                        centerIdx += sampleBuffer.Length;

                                    if (sampleBuffer[centerIdx] > 0)
                                        break;

                                    lookback++;
                                }

                                orig = sampleBuffer[centerIdx];
                            }

                            // Then look for a zero crossing.
                            if (orig > 0)
                            {
                                while (lookback < maxLookback)
                                {
                                    if (--centerIdx < 0)
                                        centerIdx += sampleBuffer.Length;

                                    if (sampleBuffer[centerIdx] < 0)
                                        break;

                                    lookback++;
                                }
                            }

                            var newHasNonZeroData = false;

                            // Build geometry, 8:1 sample to vertex ratio (4:1 if 2x scaling).
                            var vertices = new float[numVertices, 2];
                            var samplesPerVertex = NumSamples / numVertices; // Assumed to be perfectly divisible.

                            int j = centerIdx - NumSamples / 2;
                            if (j < 0) j += sampleBuffer.Length;
                            for (int i = 0; i < numVertices; i++)
                            {
                                int avg = 0;
                                for (int k = 0; k < samplesPerVertex; k++, j = (j + 1) % sampleBuffer.Length)
                                    avg += sampleBuffer[j];

                                avg /= samplesPerVertex;

                                vertices[i, 0] = i / (float)(numVertices - 1);
                                vertices[i, 1] = Utils.Clamp(avg / 32768.0f * SampleScale, -1.0f, 1.0f);
                                newHasNonZeroData |= Math.Abs(avg) > 1024.0f;
                            }

                            lastBufferPos = bufferPos;
                            geometry = vertices;
                            hasNonZeroData = newHasNonZeroData;
                        }
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
