using System;
using System.Diagnostics;
using System.Threading;

namespace FamiStudio
{
    public enum LoopMode
    {
        None,
        Song,
        Pattern,
        Max
    };

    public class SongPlayer : PlayerBase
    {
        protected int channelMask = 0x1f;
        protected int playFrame = 0;
        protected LoopMode loopMode = LoopMode.Song;

        struct SongPlayerStartInfo
        {
            public Song song;
            public int frame;
        };

        public SongPlayer() : base(NesApu.APU_SONG)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Shutdown()
        {
            Stop();
            base.Shutdown();
        }

        public void Play(Song song, int frame)
        {
            Debug.Assert(playerThread == null);
            Debug.Assert(sampleQueue.Count == 0);

            // HACK: This is a mess. The frame we pass here has the output delay baked in. Need to refactor.
            playFrame = frame + 1;

            stopEvent.Reset();
            frameEvent.Set();
            playerThread = new Thread(PlayerThread);
            playerThread.Start(new SongPlayerStartInfo() { song = song, frame = frame });
        }

        public void Stop()
        {
            if (playerThread != null)
            {
                stopEvent.Set();
                playerThread.Join();
                playerThread = null;
            }
        }

        public bool CheckIfEnded()
        {
            // This can only happen if we reached the end of the song and we 
            // disabled looping.
            if (playerThread != null && !playerThread.IsAlive)
            {
                Stop();
                return true;
            }

            return false;
        }

        public int ChannelMask
        {
            get { return channelMask; }
            set { channelMask = value; }
        }

        public LoopMode Loop
        {
            get { return loopMode; }
            set { loopMode = value; }
        }

        public int CurrentFrame
        {
            get { return Math.Max(0, playFrame - OutputDelay); }
        }

        public bool IsPlaying
        {
            get { return playerThread != null; }
        }

        unsafe void PlayerThread(object o)
        {
            var channels = new ChannelState[5]
            {
                new SquareChannelState(apuIndex, 0),
                new SquareChannelState(apuIndex, 1),
                new TriangleChannelState(apuIndex, 2),
                new NoiseChannelState(apuIndex, 3),
                new DPCMChannelState(apuIndex, 4) 
            };

            var startInfo = (SongPlayerStartInfo)o;
            var song = startInfo.song;

            bool advance = true;
            int tempoCounter = 0;
            int playPattern = 0;
            int playNote = 0;
            int speed = song.Speed;

            NesApu.Reset(apuIndex);

            if (startInfo.frame != 0)
            {
                foreach (var channel in channels)
                    channel.StartSeeking();

                while (playPattern * song.PatternLength + playNote != startInfo.frame)
                {
                    foreach (var channel in channels)
                    {
                        channel.ProcessEffects(song, ref playPattern, ref playNote, ref speed);
                        channel.Advance(song, playPattern, playNote);
                        channel.UpdateEnvelopes();
                        channel.UpdateAPU();
                    }

                    // Tempo/speed logic.
                    tempoCounter += song.Tempo * 256 / 150; // NTSC

                    if ((tempoCounter >> 8) == speed)
                    {
                        tempoCounter -= (speed << 8);
                        if (++playNote == song.PatternLength)
                        {
                            playNote = 0;
                            if (++playPattern == song.Length)
                            {
                                playPattern = 0;
                            }
                        }
                    }
                }

                foreach (var channel in channels)
                    channel.StopSeeking();
            }

            var waitEvents = new WaitHandle[] { stopEvent, frameEvent };

            while (true)
            {
                int idx = WaitHandle.WaitAny(waitEvents);

                if (idx == 0)
                {
                    break;
                }

                // Advance to next note.
                if (advance)
                {
                    foreach (var channel in channels)
                    {
                        channel.ProcessEffects(song, ref playPattern, ref playNote, ref speed);
                        channel.Advance(song, playPattern, playNote);
                    }

                    advance = false;
                }

                // Update envelopes + APU registers.
                foreach (var channel in channels)
                {
                    channel.UpdateEnvelopes();
                    channel.UpdateAPU();
                }

                // Mute.
                for (int i = 0; i < 5; i++)
                {
                    NesApu.NesApuEnableChannel(apuIndex, i, (channelMask & (1 << i)));
                }

                EndFrameAndQueueSamples();

                // Tempo/speed logic.
                tempoCounter += song.Tempo * 256 / 150; // NTSC

                if ((tempoCounter >> 8) == speed)
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
                                    break;
                                playPattern = 0;
                            }
                        }
                    }

                    playFrame = playPattern * song.PatternLength + playNote;
                    advance = true;
                }
            }

            xaudio2Stream.Stop();
            while (sampleQueue.TryDequeue(out _)) ;
        }
    };
}
