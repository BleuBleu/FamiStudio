using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FamiStudio
{
    public class AndroidAudioStream : IDisposable
    {
        public delegate short[] GetBufferDataCallback();

        public bool IsStarted => false;
        public int  ImmediatePlayPosition => 0;

        // DROIDTODO
        public AndroidAudioStream(int rate, int bufferSize, int numBuffers, GetBufferDataCallback bufferFillCallback)
        {

        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }

        public unsafe void PlayImmediate(short[] data, int sampleRate, float volume)
        {
        }
    }
}