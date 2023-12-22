using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public delegate short[] GetBufferDataCallback(out bool done);
    public delegate void StreamStartingCallback();

    public interface IAudioStream : IDisposable
    {
        bool IsPlaying { get; }
        int ImmediatePlayPosition { get; }

        void Start();
        void Stop(bool abort);
        void PlayImmediate(short[] data, int sampleRate, float volume, int channel = 0);
    }
}
