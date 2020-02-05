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

    public class SongPlayer : AudioPlayer
    {
        struct SongPlayerStartInfo
        {
            public Song song;
            public int startNote;
            public bool pal;
        };

        public SongPlayer() : base(NesApu.APU_SONG)
        {
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
            playerThread.Start(new SongPlayerStartInfo() { song = song, startNote = frame, pal = pal });
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

        public bool IsPlaying
        {
            get { return playerThread != null; }
        }

        unsafe void PlayerThread(object o)
        {
            var startInfo = (SongPlayerStartInfo)o;

            if (BeginPlaySong(startInfo.song, startInfo.pal, startInfo.startNote))
            {
                var waitEvents = new WaitHandle[] { stopEvent, frameEvent };

                while (true)
                {
                    int idx = WaitHandle.WaitAny(waitEvents);

                    if (idx == 0 || !PlaySongFrame())
                        break;
                }
            }

            audioStream.Stop();
            while (sampleQueue.TryDequeue(out _));
        }
    };
}
