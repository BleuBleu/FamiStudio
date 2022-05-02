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

        public delegate short[] GetBufferDataCallback();

        private GetBufferDataCallback bufferFill;
        private bool quit;
        private AudioTrack audioTrack;
        private AudioTrack audioTrackImmediate;
        private Task playingTask;

        public AndroidAudioStream(int rate, bool stereo, int bufferSizeInBytes, int numBuffers, GetBufferDataCallback bufferFillCallback)
        {
            // Probably not needed, but i've seen things about effects in the log that
            // worries me. Doesnt hurt.
            AudioManager am = (AudioManager)Application.Context.GetSystemService(Context.AudioService);
            am.UnloadSoundEffects();

            bufferFill = bufferFillCallback;

            audioTrack = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder().SetContentType(AudioContentType.Music).SetUsage(AudioUsageKind.Media).Build())
                .SetAudioFormat(new AudioFormat.Builder().SetSampleRate(rate).SetEncoding(Encoding.Pcm16bit).SetChannelMask(stereo ? ChannelOut.Stereo : ChannelOut.Mono).Build())
                .SetTransferMode(AudioTrackMode.Stream)
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
                    Debug.WriteLine("Starvation!");
                    System.Threading.Thread.Sleep(4);
                }
            }
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
            if (audioTrackImmediate != null)
            {
                audioTrackImmediate.Stop();
                audioTrackImmediate.Release();
                audioTrackImmediate.Dispose();
                audioTrackImmediate = null;
            }
        }

        public void PlayImmediate(short[] data, int sampleRate, float volume)
        {
            StopImmediate();

            audioTrackImmediate = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder().SetContentType(AudioContentType.Music).SetUsage(AudioUsageKind.Media).Build())
                .SetAudioFormat(new AudioFormat.Builder().SetSampleRate(sampleRate).SetEncoding(Encoding.Pcm16bit).SetChannelMask(ChannelOut.Mono).Build())
                .SetTransferMode(AudioTrackMode.Static)
                .SetBufferSizeInBytes(data.Length * sizeof(short)).Build();

            // Volume adjustment
            short vol = (short)(volume * 32768);

            var immediateStreamData = new short[data.Length];
            for (int i = 0; i < data.Length; i++)
                immediateStreamData[i] = (short)((data[i] * vol) >> 15);

            audioTrackImmediate.Write(immediateStreamData, 0, immediateStreamData.Length);
            audioTrackImmediate.Play();
        }

        public int ImmediatePlayPosition
        {
            get
            {
                return audioTrackImmediate != null && audioTrackImmediate.PlayState == PlayState.Playing && audioTrackImmediate.PlaybackHeadPosition < audioTrackImmediate.BufferSizeInFrames ? audioTrackImmediate.PlaybackHeadPosition : 0;
            }
        }
    }
}