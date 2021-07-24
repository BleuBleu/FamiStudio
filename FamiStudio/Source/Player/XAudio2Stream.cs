using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FamiStudio
{
    public class XAudio2Stream
    {
        private XAudio2 xaudio2;
        private MasteringVoice masteringVoice;

        // Regular voice for streaming NES audio data.
        private WaveFormat waveFormat;
        private SourceVoice sourceVoice;
        private AudioBuffer[] audioBuffersRing;
        private DataPointer[] memBuffers;
        private Semaphore bufferSemaphore;
        private ManualResetEvent quitEvent;
        private GetBufferDataCallback bufferFill;
        private Task playingTask;

        // Immediate voice for playing raw PCM data (used by DPCM editor).
        private SourceVoice immediateVoice;
        private AudioBuffer immediateAudioBuffer;
        private bool immediateDonePlaying;

        public delegate short[] GetBufferDataCallback();

        public XAudio2Stream(int rate, int bufferSize, int numBuffers, GetBufferDataCallback bufferFillCallback)
        {
            xaudio2 = new XAudio2();
            //xaudio2 = new XAudio2(XAudio2Version.Version27); // To simulate Windows 7 behavior.
            //xaudio2.CriticalError += Xaudio2_CriticalError;
            masteringVoice = new MasteringVoice(xaudio2);
            waveFormat = new WaveFormat(rate, 16, 1);
            audioBuffersRing = new AudioBuffer[numBuffers];
            memBuffers = new DataPointer[audioBuffersRing.Length];

            for (int i = 0; i < audioBuffersRing.Length; i++)
            {
                audioBuffersRing[i] = new AudioBuffer();
                memBuffers[i].Size = bufferSize;
                memBuffers[i].Pointer = Utilities.AllocateMemory(memBuffers[i].Size);
            }

            bufferFill = bufferFillCallback;
            bufferSemaphore = new Semaphore(numBuffers, numBuffers);
            quitEvent = new ManualResetEvent(false);
        }

        private void Xaudio2_CriticalError(object sender, ErrorEventArgs e)
        {
            Debug.WriteLine("CRITICAL ERROR!");
        }

        private void SourceVoice_BufferEnd(IntPtr obj)
        {
            bufferSemaphore.Release();
        }

        public bool IsStarted => sourceVoice != null;

        public void Start()
        {
            Debug.Assert(sourceVoice == null);

            sourceVoice = new SourceVoice(xaudio2, waveFormat);
            sourceVoice.BufferEnd += SourceVoice_BufferEnd;
            sourceVoice.Start();

            quitEvent.Reset();

            try
            {
                while (true) bufferSemaphore.Release();
            }
            catch (SemaphoreFullException)
            {
            }

            playingTask = Task.Factory.StartNew(PlayAsync, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            if (playingTask != null)
            {
                quitEvent.Set();
                playingTask.Wait();
                playingTask = null;
            }

            if (sourceVoice != null)
            {
                sourceVoice.Stop();
                sourceVoice.FlushSourceBuffers();
                sourceVoice.DestroyVoice();
                sourceVoice.BufferEnd -= SourceVoice_BufferEnd;
                sourceVoice.Dispose();
                sourceVoice = null;
            }
        }

        public void Dispose()
        {
            Stop();
            StopImmediate();

            for (int i = 0; i < audioBuffersRing.Length; i++)
            {
                Utilities.FreeMemory(memBuffers[i].Pointer);
                memBuffers[i].Pointer = IntPtr.Zero;
            }

            masteringVoice.DestroyVoice();
            masteringVoice.Dispose();
            masteringVoice = null;

            xaudio2.Dispose();
            xaudio2 = null;
        }

        private void PlayAsync()
        {
#if DEBUG
            try
            {
#endif
                int nextBuffer = 0;
                var waitEvents = new WaitHandle[] { quitEvent, bufferSemaphore };

                while (true)
                {
                    int idx = WaitHandle.WaitAny(waitEvents);

                    if (idx == 0)
                    {
                        break;
                    }

                    var size = memBuffers[nextBuffer].Size;
                    var data = bufferFill();
                    if (data != null)
                    {
                        size = data.Length * sizeof(short);
                        Debug.Assert(data.Length * sizeof(short) <= memBuffers[nextBuffer].Size);
                        Marshal.Copy(data, 0, memBuffers[nextBuffer].Pointer, data.Length);
                    }

                    audioBuffersRing[nextBuffer].AudioDataPointer = memBuffers[nextBuffer].Pointer;
                    audioBuffersRing[nextBuffer].AudioBytes = size;

                    sourceVoice.SubmitSourceBuffer(audioBuffersRing[nextBuffer], null);

                    nextBuffer = ++nextBuffer % audioBuffersRing.Length;
                }
#if DEBUG
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                if (Debugger.IsAttached)
                    Debugger.Break();
            }
#endif
        }

        public void PlayImmediate(short[] data, int sampleRate, float volume)
        {
            Debug.Assert(Utils.IsInMainThread());

            StopImmediate();

            immediateDonePlaying = false;

            immediateAudioBuffer = new AudioBuffer();
            immediateAudioBuffer.AudioDataPointer = Utilities.AllocateMemory(data.Length * sizeof(short));
            immediateAudioBuffer.AudioBytes = data.Length * sizeof(short);
            Marshal.Copy(data, 0, immediateAudioBuffer.AudioDataPointer, data.Length);

            var waveFormat = new WaveFormat(sampleRate, 16, 1);

            immediateVoice = new SourceVoice(xaudio2, waveFormat);
            immediateVoice.BufferEnd += ImmediateVoice_BufferEnd;
            immediateVoice.SetVolume(volume);
            immediateVoice.SubmitSourceBuffer(immediateAudioBuffer, null);
            immediateVoice.Start();
        }

        public void StopImmediate()
        {
            Debug.Assert(Utils.IsInMainThread());

            if (immediateVoice != null)
            {
                immediateVoice.Stop();
                immediateVoice.FlushSourceBuffers();
                immediateVoice.BufferEnd -= ImmediateVoice_BufferEnd;
                immediateVoice.DestroyVoice();
                immediateVoice.Dispose();
                immediateVoice = null;

                Utilities.FreeMemory(immediateAudioBuffer.AudioDataPointer);
                immediateAudioBuffer = null;
                immediateDonePlaying = true;
            }
        }

        private void ImmediateVoice_BufferEnd(IntPtr obj)
        {
            immediateDonePlaying = true;
        }

        public int ImmediatePlayPosition
        {
            get
            {
                if (immediateVoice != null && !immediateDonePlaying)
                {
                    return (int)immediateVoice.State.SamplesPlayed;
                }
                else
                {
                    return -1;
                }
            }
        }

        public static bool TryDetectXAudio2()
        {
            try
            {
                var xaudio2 = new XAudio2();

                xaudio2.Dispose();
                xaudio2 = null;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
