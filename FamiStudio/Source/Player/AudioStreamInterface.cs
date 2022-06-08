using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public delegate short[] GetBufferDataCallback();

    public interface IAudioStream : IDisposable
    {
        bool IsStarted { get; }
        int ImmediatePlayPosition { get; }

        void Start();
        void Stop();
        void PlayImmediate(short[] data, int sampleRate, float volume);
    }
}
