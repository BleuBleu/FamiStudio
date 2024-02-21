using Android.App;
using Android.Content;
using Android.Media;
using Android.Renderscripts;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class AndroidAudioStream : IAudioStream
    {
        // Wrapping in a class so that assignment is atomic.
        private class ImmediateData
        {
            public int samplesOffset = 0;
            public int sampleRate = 0;
            public short[] samples = null;
        }

        private GetBufferDataCallback bufferFill;
        private volatile bool quit;
        private bool stereo;
        private AudioTrack audioTrack;
        private Task playingTask;
        private int inputSampleRate;
        private int outputSampleRate;
        private short[] lastSamples;
        private double resampleIndex = 0.0;
        private volatile ImmediateData immediateData = null;

        public bool IsPlaying => playingTask != null;
        public bool Stereo => stereo;

        private AndroidAudioStream()
        {
        }

        public static AndroidAudioStream Create(int rate, bool inStereo, int bufferSizeMs)
        {
            // Probably not needed, but i've seen things about effects in the log that
            // worries me. Doesnt hurt.
            AudioManager am = (AudioManager)Application.Context.GetSystemService(Context.AudioService);
            am.UnloadSoundEffects();

            // To allow low-latency, we must output direct in the device sample rate,
            // we'll handle the conversion ourselves.
            var deviceSampleRate = int.Parse(am.GetProperty(AudioManager.PropertyOutputSampleRate), CultureInfo.InvariantCulture); ;
            var minBufferSizeInFrames = int.Parse(am.GetProperty(AudioManager.PropertyOutputFramesPerBuffer), CultureInfo.InvariantCulture);

            var stream = new AndroidAudioStream();

            stream.stereo = inStereo;
            stream.inputSampleRate = rate;
            stream.outputSampleRate = deviceSampleRate;

            var bufferSizeInFrames = Utils.RoundUp(stream.outputSampleRate * bufferSizeMs / 1000, 2); // Keep even
            var bufferSizeInBytes = Math.Max(minBufferSizeInFrames, bufferSizeInFrames) * sizeof(short) * (stream.stereo ? 2 : 1);

            stream.audioTrack = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder().SetContentType(AudioContentType.Music).SetUsage(AudioUsageKind.Media).Build())
                .SetAudioFormat(new AudioFormat.Builder().SetSampleRate(stream.outputSampleRate).SetEncoding(Encoding.Pcm16bit).SetChannelMask(stream.stereo ? ChannelOut.Stereo : ChannelOut.Mono).Build())
                .SetTransferMode(AudioTrackMode.Stream)
                .SetPerformanceMode(AudioTrackPerformanceMode.LowLatency)
                .SetBufferSizeInBytes(bufferSizeInBytes).Build();

            Debug.Assert(stream.audioTrack.PerformanceMode == AudioTrackPerformanceMode.LowLatency);

            return stream;
        }

        public void Start(GetBufferDataCallback bufferFillCallback, StreamStartingCallback streamStartCallback)
        {
            bufferFill = bufferFillCallback;
            lastSamples = null;
            resampleIndex = 0.0;
            quit = false;
            audioTrack.Play();
            playingTask = Task.Factory.StartNew(PlayAsync, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            StopInternal(true);
        }

        private void StopInternal(bool mainThread)
        {
            lock (this)
            {
                if (playingTask != null)
                {
                    // Stop can be called from the task itself for non-looping song so we 
                    // need a mutex and we dont want to wait for the task if we are the task.
                    if (mainThread)
                    {
                        quit = true;
                        playingTask.Wait();
                    }

                    audioTrack.Stop();
                    playingTask = null;
                }
            }
        }

        private unsafe void PlayAsync()
        {
            while (!quit)
            {
                // Write will block if the buffer is full. If only all streams were this easy!
                var newSamples = bufferFill(out var done);
                if (newSamples != null)
                {
                    lastSamples = WaveUtils.ResampleStream(lastSamples, newSamples, inputSampleRate, outputSampleRate, stereo, ref resampleIndex);
                    lastSamples = MixImmediateData(lastSamples, lastSamples == newSamples); // Samples are read-only, need to duplicate if we didn't resample.
                    audioTrack.Write(lastSamples, 0, lastSamples.Length, WriteMode.Blocking);
                }
                else
                {
                    if (done)
                    {
                        StopInternal(false);
                        return;
                    }

                    Debug.WriteLine("Starvation!");
                    System.Threading.Thread.Sleep(4);
                }
            }
        }

        short[] MixImmediateData(short[] samples, bool duplicate)
        {
            // Mix in immediate data if any, storing in variable since main thread can change it anytime.
            var immData = immediateData;
            if (immData != null && immData.samplesOffset < immData.samples.Length)
            {
                var channelCount = stereo ? 2 : 1;
                var sampleCount = Math.Min(samples.Length, (immData.samples.Length - immData.samplesOffset) * channelCount);
                var outputSamples = duplicate ? (short[])samples.Clone() : samples;

                for (int i = 0, j = immData.samplesOffset; i < sampleCount; i++, j += (i % channelCount) == 0 ? 1 : 0)
                    outputSamples[i] = (short)Math.Clamp(samples[i] + immData.samples[j], short.MinValue, short.MaxValue);

                immData.samplesOffset += sampleCount / channelCount;
                return outputSamples;
            }

            return samples;
        }

        public void Dispose()
        {
            Stop();
            StopImmediate();

            audioTrack.Release();
            audioTrack.Dispose();
            audioTrack = null;
        }

        private void StopImmediate()
        {
            immediateData = null;
        }

        public void PlayImmediate(short[] samples, int sampleRate, float volume, int channel = 0)
        {
            Debug.Assert(Platform.IsInMainThread());

            StopImmediate();

            // Cant find volume adjustment in port audio.
            short vol = (short)(volume * 32768);

            var adjustedSamples = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
                adjustedSamples[i] = (short)((samples[i] * vol) >> 15);

            var data = new ImmediateData();
            data.sampleRate = sampleRate;
            data.samplesOffset = 0;
            data.samples = WaveUtils.ResampleBuffer(adjustedSamples, sampleRate, outputSampleRate, false);
            immediateData = data;
        }

        public int ImmediatePlayPosition
        {
            get
            {
                var data = immediateData;
                return data != null && data.samplesOffset < data.samples.Length ? data.samplesOffset * data.sampleRate / outputSampleRate : -1;
            }
        }
    }
}