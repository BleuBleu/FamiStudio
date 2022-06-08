using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;

namespace FamiStudio
{
    public class OpenALStream : IAudioStream
    {
        private static AudioContext context;

        private GetBufferDataCallback bufferFill;
        private int freq;
        private bool quit;
        private Task playingTask;
        private bool stereo;

        int source;
        int[] buffers;

        int   immediateSource = -1;
        int[] immediateBuffers;

        public OpenALStream(int rate, bool inStereo, int bufferSize, int numBuffers, GetBufferDataCallback bufferFillCallback)
        {
            if (context == null)
            {
                context = new AudioContext();
                Console.WriteLine($"Default OpenAL audio device is '{AudioContext.DefaultDevice}'");
            }
            stereo = inStereo;
            // TODO : We need to decouple the number of emulated buffered frames and the 
            // size of the low-level audio buffers.
            freq = rate;
            source = AL.GenSource();
            buffers = AL.GenBuffers(numBuffers);
            bufferFill = bufferFillCallback;
            quit = false;
        }

        public void Dispose()
        {
            StopImmediate();

            AL.DeleteBuffers(buffers);
            AL.DeleteSource(source);
        }

        public bool IsStarted => playingTask != null;

        public void Start()
        {
            quit = false;
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

            AL.SourceStop(source);
            AL.Source(source, ALSourcei.Buffer, 0);
        }

        private void DebugStream()
        {
#if DEBUG
            AL.GetSource(source, ALGetSourcei.BuffersProcessed, out int numProcessed);
            AL.GetSource(source, ALGetSourcei.BuffersQueued, out int numQueued);
            AL.GetSource(source, ALGetSourcei.SourceState, out int state);
            Debug.WriteLine($"{DateTime.Now.Millisecond} = {state} {numQueued} {numProcessed}");
#endif
        }

        private unsafe void PlayAsync()
        {
            int initBufferIdx = buffers.Length - 1;

            while (!quit)
            {
                int numProcessed = 0;

                if (initBufferIdx < 0)
                {
                    do
                    {
                        AL.GetSource(source, ALGetSourcei.BuffersProcessed, out numProcessed);
                        if (numProcessed != 0) break;
                        if (quit) return;
                        Thread.Sleep(1);
                    }
                    while (true);
                }
                else
                {
                    numProcessed = initBufferIdx + 1;
                }

                for (int i = 0; i < numProcessed && !quit; )
                {
                    var data = bufferFill();
                    if (data != null)
                    {
                        int bufferId = initBufferIdx >= 0 ? buffers[initBufferIdx--] : AL.SourceUnqueueBuffer(source);
                        fixed (short* p = &data[0])
                            AL.BufferData(bufferId, stereo ? ALFormat.Stereo16 : ALFormat.Mono16, new IntPtr(p), data.Length * sizeof(short), freq);
                        AL.SourceQueueBuffer(source, bufferId);
                        i++;
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }

                //DebugStream();

                AL.GetSource(source, ALGetSourcei.SourceState, out int state);
                if ((ALSourceState)state != ALSourceState.Playing)
                {
                    Debug.WriteLine("RESTART!");
                    //DebugStream();
                    AL.SourcePlay(source);
                }
            }
        }

        public unsafe void PlayImmediate(short[] data, int sampleRate, float volume)
        {
            Debug.Assert(Platform.IsInMainThread());

            StopImmediate();

            immediateSource  = AL.GenSource();
            immediateBuffers = AL.GenBuffers(1);

            fixed (short* p = &data[0])
                AL.BufferData(immediateBuffers[0], ALFormat.Mono16, new IntPtr(p), data.Length * sizeof(short), sampleRate);

            AL.Source(immediateSource, ALSourcef.Gain, volume);
            AL.SourceQueueBuffer(immediateSource, immediateBuffers[0]);
            AL.SourcePlay(immediateSource);
        } 

        public void StopImmediate()
        {
            Debug.Assert(Platform.IsInMainThread());

            if (immediateSource >= 0)
            {
                AL.SourceStop(immediateSource);
                AL.Source(immediateSource, ALSourcei.Buffer, 0);
                AL.DeleteBuffers(immediateBuffers);
                AL.DeleteSource(immediateSource);

                immediateBuffers = null;
                immediateSource = -1;
            }
        }

        public int ImmediatePlayPosition
        {
            get
            {
                var playPos = -1;

                // TODO : Make sure we support the AL_EXT_OFFSET extension.
                if (immediateSource >= 0)
                {
                    AL.GetSource(immediateSource, ALGetSourcei.SourceState, out int state);
                    if ((ALSourceState)state == ALSourceState.Playing)
                        AL.GetSource(immediateSource, ALGetSourcei.SampleOffset, out playPos);
                }

                return playPos;
            }
        }
    }
}
