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

        // NSTC: 734 = ceil(SampleRate / FrameRate) = ceil(44100 / 60.0988).
        // PAL:  882 = ceil(SampleRate / FrameRate) = ceil(44100 / 50.0070).
        protected const int BufferSize = 882 * sizeof(short);
        protected const int NumAudioBuffers = 3;

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
            audioStream = new AudioStream(SampleRate, 1, BufferSize, NumAudioBuffers, AudioBufferFillCallback);
        }
        
        public virtual void Shutdown()
        {
            stopEvent.Set();
            if (playerThread != null)
                playerThread.Join();

            audioStream.Dispose();
        }

        public static bool UpdateFamitrackerTempo(int speed, int tempo, bool pal, ref int tempoCounter)
        {
            // Tempo/speed logic straight from Famitracker.
            var tempoDecrement = (tempo * 24) / speed;
            var tempoRemainder = (tempo * 24) % speed;

            if (tempoCounter <= 0)
            {
                int ticksPerSec = pal ? 50 : 60;
                tempoCounter += (60 * ticksPerSec) - tempoRemainder;
            }
            tempoCounter -= tempoDecrement;
           
            return tempoCounter <= 0;
        }

        public static bool UpdateFamistudioTempo(int speed, bool pal, ref int tempoCounter, ref int numFrames)
        {
            numFrames = (pal && (tempoCounter % speed) == (speed - 2)) ? 2 : 1;
            tempoCounter += numFrames;
            return true;
        }

        public static bool AdvanceSong(int songLength, int patternLength, LoopMode loopMode, ref int playPattern, ref int playNote, ref int jumpPattern, ref int jumpNote)
        {
            if (jumpNote >= 0 || jumpPattern >= 0)
            {
                if (loopMode == LoopMode.Pattern)
                {
                    playNote = 0;
                }
                else
                {
                    playNote = Math.Min(patternLength - 1, jumpNote);
                    playPattern = jumpPattern;
                }

                jumpPattern = -1;
                jumpNote = -1;
            }
            else if (++playNote >= patternLength)
            {
                playNote = 0;
                if (loopMode != LoopMode.Pattern)
                    playPattern++;
            }

            if (playPattern >= songLength)
            {
                if (loopMode == LoopMode.None)
                {
                    return false;
                }
                else if (loopMode == LoopMode.Song)
                {
                    playPattern = 0;
                    playNote = 0;
                }
            }

            return true;
        }

        private static ChannelState CreateChannelState(int apuIdx, int channelType, bool pal)
        {
            switch (channelType)
            {
                case Channel.Square1:
                case Channel.Square2:
                    return new ChannelStateSquare(apuIdx, channelType, pal);
                case Channel.Triangle:
                    return new ChannelStateTriangle(apuIdx, channelType, pal);
                case Channel.Noise:
                    return new ChannelStateNoise(apuIdx, channelType, pal);
                case Channel.Dpcm:
                    return new ChannelStateDpcm(apuIdx, channelType, pal);
                case Channel.Vrc6Square1:
                case Channel.Vrc6Square2:
                    return new ChannelStateVrc6Square(apuIdx, channelType);
                case Channel.Vrc6Saw:
                    return new ChannelStateVrc6Saw(apuIdx, channelType);
                case Channel.Vrc7Fm1:
                case Channel.Vrc7Fm2:
                case Channel.Vrc7Fm3:
                case Channel.Vrc7Fm4:
                case Channel.Vrc7Fm5:
                case Channel.Vrc7Fm6:
                    return new ChannelStateVrc7(apuIdx, channelType);
                case Channel.FdsWave:
                    return new ChannelStateFds(apuIdx, channelType);
                case Channel.Mmc5Square1:
                case Channel.Mmc5Square2:
                    return new ChannelStateMmc5Square(apuIdx, channelType);
                case Channel.NamcoWave1:
                case Channel.NamcoWave2:
                case Channel.NamcoWave3:
                case Channel.NamcoWave4:
                case Channel.NamcoWave5:
                case Channel.NamcoWave6:
                case Channel.NamcoWave7:
                case Channel.NamcoWave8:
                    return new ChannelStateNamco(apuIdx, channelType, pal);
                case Channel.SunsoftSquare1:
                case Channel.SunsoftSquare2:
                case Channel.SunsoftSquare3:
                    return new ChannelStateSunsoftSquare(apuIdx, channelType, pal);
            }

            Debug.Assert(false);
            return null;
        }

        public static ChannelState[] CreateChannelStates(Project project, int apuIdx, bool pal)
        {
            var channelCount = project.GetActiveChannelCount();
            var states = new ChannelState[channelCount];

            int idx = 0;
            for (int i = 0; i < Channel.Count; i++)
            {
                if (project.IsChannelActive(i))
                    states[idx++] = CreateChannelState(apuIdx, i, pal);
            }

            return states;
        }
        
        public static int GetNesApuExpansionAudio(Project project)
        {
            switch (project.ExpansionAudio)
            {
                case Project.ExpansionNone:
                    return NesApu.APU_EXPANSION_NONE;
                case Project.ExpansionVrc6:
                    return NesApu.APU_EXPANSION_VRC6;
                case Project.ExpansionVrc7:
                    return NesApu.APU_EXPANSION_VRC7;
                case Project.ExpansionFds:
                    return NesApu.APU_EXPANSION_FDS;
                case Project.ExpansionMmc5:
                    return NesApu.APU_EXPANSION_MMC5;
                case Project.ExpansionNamco:
                    return NesApu.APU_EXPANSION_NAMCO;
                case Project.ExpansionSunsoft:
                    return NesApu.APU_EXPANSION_SUNSOFT;
            }

            Debug.Assert(false);
            return 0;
        }

        protected unsafe void EndFrameAndQueueSamples()
        {
            NesApu.EndFrame(apuIndex);

            int numTotalSamples = NesApu.SamplesAvailable(apuIndex);
            short[] samples = new short[numTotalSamples];

            fixed (short* ptr = &samples[0])
            {
                NesApu.ReadSamples(apuIndex, new IntPtr(ptr), numTotalSamples);
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
