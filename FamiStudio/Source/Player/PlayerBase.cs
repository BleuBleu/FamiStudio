using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

#if FAMISTUDIO_WINDOWS
using AudioStream = FamiStudio.XAudio2Stream;
#else
using AudioStream = FamiStudio.PortAudioStream;
#endif

namespace FamiStudio
{
    public class PlayerBase
    {
        protected const int SampleRate = 44100;
        protected const int BufferSize = 734 * sizeof(short); // 734 = ceil(SampleRate / FrameRate) = ceil(44100 / 60.0988)
        protected const int NumAudioBuffers = 3;
        protected const int NumGraphicsBuffers = 2; // Assume D2D is triple buffered, probably true.
        protected const int OutputDelay = 0; // NumAudioBuffers - NumGraphicsBuffers; Disabled for now.

        protected int apuIndex;
        protected NesApu.DmcReadDelegate dmcCallback;

        protected AudioStream audioStream;
        protected Thread playerThread;
        protected AutoResetEvent frameEvent = new AutoResetEvent(true);
        protected ManualResetEvent stopEvent = new ManualResetEvent(false);
        protected ConcurrentQueue<short[]> sampleQueue = new ConcurrentQueue<short[]>();

        protected PlayerBase(int apuIndex)
        {
            this.apuIndex = apuIndex;
        }

        protected short[] AudioBufferFillCallback()
        {
            short[] samples = null;
            if (sampleQueue.TryDequeue(out samples))
            {
                frameEvent.Set(); // Wake up player thread.
            }
            //else
            //{
            //    Trace.WriteLine("Audio is starving!");
            //}

            return samples;
        }

        public virtual void Initialize()
        {
            dmcCallback = new NesApu.DmcReadDelegate(NesApu.DmcReadCallback);
            NesApu.NesApuInit(apuIndex, SampleRate, dmcCallback);
            audioStream = new AudioStream(SampleRate, 1, BufferSize, NumAudioBuffers, AudioBufferFillCallback);
        }

        public virtual void Shutdown()
        {
            stopEvent.Set();
            if (playerThread != null)
                playerThread.Join();

            audioStream.Dispose();
        }

        public static bool AdvanceTempo(Song song, int speed, LoopMode loopMode, ref int tempoCounter, ref int playPattern, ref int playNote, ref int playFrame, ref bool advance)
        {
            // Tempo/speed logic.
            tempoCounter += song.Tempo * 256 / 150; // NTSC

            if ((tempoCounter >> 8) >= speed)
            {
                tempoCounter -= (speed << 8);

                if (++playNote == song.PatternLength)
                {
                    playNote = 0;

                    if (loopMode != LoopMode.Pattern)
                    {
                        if (++playPattern == song.Length)
                        {
                            if (loopMode == LoopMode.None)
                                return false;
                            playPattern = 0;
                        }
                    }
                }

                playFrame = playPattern * song.PatternLength + playNote;
                advance = true;
            }

            return true;
        }

        protected unsafe void EndFrameAndQueueSamples()
        {
            NesApu.NesApuEndFrame(apuIndex);

            int numTotalSamples = NesApu.NesApuSamplesAvailable(apuIndex);
            short[] samples = new short[numTotalSamples];

            fixed (short* ptr = &samples[0])
            {
                NesApu.NesApuReadSamples(apuIndex, new IntPtr(ptr), numTotalSamples);
            }

            sampleQueue.Enqueue(samples);

            // Wait until we have queued as many frames as XAudio buffers to start
            // the audio thread, otherwise, we risk starving on the first frame.
            if (!audioStream.IsStarted)
            {
                if (sampleQueue.Count == NumAudioBuffers)
                {
                    audioStream.Start();
                }
                else
                {
                    frameEvent.Set();
                }
            }
        }
    };
}
