using Android.App;
using Android.Content;
using Android.Media;
using System;
using System.Threading.Tasks;

using Debug = System.Diagnostics.Debug;

namespace FamiStudio
{
    public class AndroidAudioStream : IDisposable
    {
        public bool IsStarted => playingTask != null;
        public int  ImmediatePlayPosition => 0;

        public delegate short[] GetBufferDataCallback();

        private GetBufferDataCallback bufferFill;
        private bool quit;
        private AudioTrack audioTrack;
        private Task playingTask;

        public AndroidAudioStream(int rate, int bufferSizeInBytes, int numBuffers, GetBufferDataCallback bufferFillCallback)
        {
            // Probably not needed, but i've seen things about effects in the log that
            // worries me. Doesnt hurt.
            AudioManager am = (AudioManager)Application.Context.GetSystemService(Context.AudioService);
            am.UnloadSoundEffects();

            bufferFill = bufferFillCallback;

            audioTrack = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder().SetContentType(AudioContentType.Music).SetUsage(AudioUsageKind.Media).Build())
                .SetAudioFormat(new AudioFormat.Builder().SetSampleRate(rate).SetEncoding(Android.Media.Encoding.Pcm16bit).SetChannelMask(ChannelOut.Mono).Build())
                .SetPerformanceMode(AudioTrackPerformanceMode.LowLatency)
                .SetBufferSizeInBytes(bufferSizeInBytes).Build();

            Debug.Assert(audioTrack.PerformanceMode == AudioTrackPerformanceMode.LowLatency);
        }

        public void Start()
        {
            quit = false;
            audioTrack.Play();
            playingTask = Task.Factory.StartNew(PlayAsync, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            if (playingTask != null)
            {
                quit = true;
                playingTask.Wait();
                playingTask = null;
            }
            audioTrack.Stop();
        }

        private unsafe void PlayAsync()
        {
            while (!quit)
            {
                // Write will block if the buffer is full. If only all streams were this easy!
                var samples = bufferFill();
                if (samples != null)
                {
                    audioTrack.Write(samples, 0, samples.Length, WriteMode.Blocking);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Starvation!");
                    System.Threading.Thread.Sleep(4);
                }
            }
        }

        public void Dispose()
        {
            Stop();
            audioTrack.Release();
            audioTrack.Dispose();
            audioTrack = null;
        }

        // DROIDTODO
        public unsafe void PlayImmediate(short[] data, int sampleRate, float volume)
        {
        }
    }
}