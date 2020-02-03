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
        protected int channelMask = 0xffff;
        protected int playPosition = 0;
        protected LoopMode loopMode = LoopMode.Song;

        struct SongPlayerStartInfo
        {
            public Song song;
            public int frame;
            public bool pal;
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

        public void Play(Song song, int frame, bool pal)
        {
            Debug.Assert(playerThread == null);
            Debug.Assert(sampleQueue.Count == 0);

            stopEvent.Reset();
            frameEvent.Set();
            playerThread = new Thread(PlayerThread);
            playerThread.Start(new SongPlayerStartInfo() { song = song, frame = frame, pal = pal });
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
            get { return Math.Max(0, playPosition); }
            set { playPosition = value; }
        }

        public bool IsPlaying
        {
            get { return playerThread != null; }
        }

        unsafe void PlayerThread(object o)
        {
            var startInfo = (SongPlayerStartInfo)o;
            var song = startInfo.song;

            var channels = PlayerBase.CreateChannelStates(song.Project, apuIndex, startInfo.pal);

            var advance = true;
            var tempoCounter = 0;
            var playPattern = 0;
            var playNote = 0;
            var jumpPattern = -1;
            var jumpNote = -1;
            //var numFrames = 0;
            var speed = song.Speed;

            playPosition = startInfo.frame;

            NesApu.InitAndReset(apuIndex, SampleRate, startInfo.pal, GetNesApuExpansionAudio(song.Project), dmcCallback);

            if (startInfo.frame != 0)
            {
                NesApu.StartSeeking(apuIndex);
                #if DEBUG
                    NesApu.seeking = true;
                #endif

                while (playPattern * song.PatternLength + playNote < startInfo.frame)
                {
                    if (!AdvanceSong(song.Length, song.PatternLength, loopMode, ref playPattern, ref playNote, ref jumpPattern, ref jumpNote))
                        break;

                    foreach (var channel in channels)
                    {
                        channel.Advance(song, playPattern, playNote);
                        channel.ProcessEffects(song, playPattern, playNote, ref jumpPattern, ref jumpNote, ref speed);
                        channel.UpdateEnvelopes();
                        channel.UpdateAPU();
                    }
                }

                NesApu.StopSeeking(apuIndex);
#if DEBUG
                NesApu.seeking = false;
#endif

                jumpPattern = -1;
                jumpNote = -1;
            }

            var waitEvents = new WaitHandle[] { stopEvent, frameEvent };

            while (true)
            {
                int idx = WaitHandle.WaitAny(waitEvents);

                if (idx == 0)
                {
                    break;
                }

                if (advance)
                {
                    playPosition = playPattern * song.PatternLength + playNote;

                    foreach (var channel in channels)
                    {
                        channel.Advance(song, playPattern, playNote);
                        channel.ProcessEffects(song, playPattern, playNote, ref jumpPattern, ref jumpNote, ref speed);
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
                for (int i = 0; i < channels.Length; i++) 
                {
                    NesApu.EnableChannel(apuIndex, i, (channelMask & (1 << i)));
                }

                EndFrameAndQueueSamples();

                if (UpdateFamitrackerTempo(speed, song.Tempo, startInfo.pal, ref tempoCounter))
                //if (UpdateFamistudioTempo(6, startInfo.pal, ref tempoCounter, ref numFrames))
                {
                    // Advance to next note.
                    if (!AdvanceSong(song.Length, song.PatternLength, loopMode, ref playPattern, ref playNote, ref jumpPattern, ref jumpNote))
                        break;

                    advance = true;
                }
            }

            audioStream.Stop();
            while (sampleQueue.TryDequeue(out _)) ;
        }
    };
}
