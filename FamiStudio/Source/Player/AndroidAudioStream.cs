using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

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

        public AndroidAudioStream(int rate, int bufferSize, int numBuffers, GetBufferDataCallback bufferFillCallback)
        {
            bufferFill = bufferFillCallback;

            audioTrack = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder().SetContentType(AudioContentType.Music).Build())
                .SetAudioFormat(new AudioFormat.Builder().SetSampleRate(rate).SetEncoding(Android.Media.Encoding.Pcm16bit).SetChannelMask(ChannelOut.Mono).Build())
                .SetBufferSizeInBytes(bufferSize).Build();
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
                    System.Threading.Thread.Sleep(1);
                }
            }
        }

        public void Dispose()
        {
            Stop();
            audioTrack.Dispose();
            audioTrack = null;
        }

        // DROIDTODO
        public unsafe void PlayImmediate(short[] data, int sampleRate, float volume)
        {
        }
    }
}