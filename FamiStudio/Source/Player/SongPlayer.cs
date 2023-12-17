using System;
using System.Diagnostics;
using System.Threading;

namespace FamiStudio
{
    public class SongPlayer : AudioPlayer
    {
        protected struct SongPlayerStartInfo
        {
            public Song song;
            public int startNote;
            public bool pal;
        };

        public SongPlayer(bool pal, int sampleRate, bool stereo, int bufferSizeMs, int numFrames) : base(NesApu.APU_SONG, pal, sampleRate, stereo, bufferSizeMs, numFrames)
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
            shouldStopStream = false;

            if (UsesEmulationThread)
            {
                Debug.Assert(emulationThread == null);
                Debug.Assert(emulationQueue.Count == 0);

                ResetThreadingObjects();

                emulationThread = new Thread(EmulationThread);
                emulationThread.Start(new SongPlayerStartInfo() { song = song, startNote = frame, pal = pal });
            }
            else
            {
                BeginPlaySong(song, pal, frame);
            }

            audioStream.Stop(true); // Extra safety
            audioStream.Start();
        }

        public void Stop()
        {
            if (UsesEmulationThread)
            {
                // Keeping a local variable of the thread since the song may
                // end naturally and may set playerThread = null after we have set
                // the stop event.
                var thread = emulationThread;
                if (thread != null)
                {
                    stopEvent.Set();
                    thread.Join();
                    Debug.Assert(emulationThread == null);
                }
            }

            audioStream.Stop(true);
        }

        public bool StopIfReachedSongEnd()
        {
            if (shouldStopStream)
            {
                audioStream.Stop(false);
                shouldStopStream = false;
                return true;
            }

            return false;
        }

        public bool IsPlaying
        {
            get { return UsesEmulationThread ? emulationThread != null : audioStream.IsPlaying; }
        }

#if false // Enable to debug oscilloscope triggers for a specific channel of a song.
        protected override FrameAudioData GetFrameAudioData()
        {
            var data = base.GetFrameAudioData();
            data.triggerSample = NesApu.GetChannelTrigger(apuIndex, ExpansionType.Vrc7, 0);
            return data;
        }
#endif

        protected override bool EmulateFrame()
        {
            return PlaySongFrame();
        }

        unsafe void EmulationThread(object o)
        {
            var startInfo = (SongPlayerStartInfo)o;

            // Since BeginPlaySong is not inside the main loop and will
            // call EndFrame, we need to subtract one immediately so that
            // the semaphore count is not off by one.
            emulationSemaphore.WaitOne();

            if (BeginPlaySong(startInfo.song, startInfo.pal, startInfo.startNote))
            {
                var waitEvents = new WaitHandle[] { stopEvent, emulationSemaphore };

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
                        while (audioStream.IsPlaying && emulationQueue.Count != 0)
                            Thread.Sleep(1);
                        break;
                    }
                }
            }

            shouldStopStream = true;
            while (emulationQueue.TryDequeue(out _));
            emulationThread = null;
        }
    };
}
