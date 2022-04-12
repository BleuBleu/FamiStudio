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

        public SongPlayer(bool pal, int sampleRate, bool stereo) : base(NesApu.APU_SONG, pal, sampleRate, stereo, Settings.NumBufferedAudioFrames)
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

            ResetThreadingObjects();

            playerThread = new Thread(PlayerThread);
            playerThread.Start(new SongPlayerStartInfo() { song = song, startNote = frame, pal = pal });
        }

        public void Stop()
        {
            // Keeping a local variable of the thread since the song may
            // end naturally and may set playerThread = null after we have set
            // the stop event.
            var thread = playerThread;
            if (thread != null)
            {
                stopEvent.Set();
                thread.Join();
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

            // Since BeginPlaySong is not inside the main loop and will
            // call EndFrame, we need to subtract one immediately so that
            // the semaphore count is not off by one.
            bufferSemaphore.WaitOne();

            if (BeginPlaySong(startInfo.song, startInfo.pal, startInfo.startNote))
            {
                var waitEvents = new WaitHandle[] { stopEvent, bufferSemaphore };

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
