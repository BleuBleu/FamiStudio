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
        bool Stereo { get; }
        bool RecreateOnDeviceChanged { get; }
        int ImmediatePlayPosition { get; }

        void Start(GetBufferDataCallback bufferFillCallback, StreamStartingCallback streamStartCallback);
        void Stop();
        void PlayImmediate(short[] data, int sampleRate, float volume, int channel = 0);
    }
}
