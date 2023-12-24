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

        public SongPlayer(IAudioStream stream, bool pal, int sampleRate, bool stereo, int numFrames) : base(stream, NesApu.APU_SONG, pal, sampleRate, stereo, numFrames)
        {
            loopMode = LoopMode.LoopPoint;
        }

        public override void Shutdown()
        {
            Stop();
            base.Shutdown();
        }

        public void Start(Song song, int frame, bool pal)
        {
            if (audioStream == null)
                return;

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
            audioStream.Start(AudioBufferFillCallback, AudioStreamStartingCallback);
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

            audioStream?.Stop(true);

            if (UsesEmulationThread)
            {
                // When stopping, reset the play position to the first frame in the queue,
                // this prevent the cursor from jumping ahead when playing/stopping quickly
                playPosition = PlayPosition;

                while (emulationQueue.Count > 0)
                    emulationQueue.TryDequeue(out _);
            }
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
            var advanced = PlaySongFrame();

            // When reaching the end of a non looping song, push a null to signal to stop the stream.
            if (!advanced && UsesEmulationThread)
                emulationQueue.Enqueue(null);

            return advanced;
        }

        void EmulationThread(object o)
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
                    if (WaitHandle.WaitAny(waitEvents) == 0)
                        break;

                    if (!EmulateFrame())
                        break;
                }
            }

            emulationThread = null;
        }
    };
}
