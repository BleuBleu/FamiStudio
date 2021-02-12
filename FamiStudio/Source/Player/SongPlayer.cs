using System;
using System.Diagnostics;
using System.Threading;

namespace FamiStudio
{
    public class SongPlayer : AudioPlayer
    {
        struct SongPlayerStartInfo
        {
            public Song song;
            public int startNote;
            public bool pal;
        };

        public SongPlayer(bool pal) : base(NesApu.APU_SONG, pal, DefaultSampleRate, Settings.NumBufferedAudioFrames)
        {
            loopMode = LoopMode.LoopPoint;
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
                Debug.Assert(playerThread == null);
            }
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
                    var idx = WaitHandle.WaitAny(waitEvents);

                    if (idx == 0)
                        break;

                    var reachedEnd = !PlaySongFrame();

                    // When we reach the end of the song, we will wait for the stream to finished 
                    // consuming the queued samples. This happens when songs don't have loop points,
                    // (like SFX) and avoids cutting the last couple frames of audio.
                    if (reachedEnd)
                    {
                        if (audioStream.IsStarted)
                        {
                            while (sampleQueue.Count != 0)
                                Thread.Sleep(1);
                        }
                        break;
                    }
                }
            }

            audioStream.Stop();
            while (sampleQueue.TryDequeue(out _));

            playerThread = null;
        }
    };
}
