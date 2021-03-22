using System;
using System.Collections.Concurrent;
using System.Threading;

namespace FamiStudio
{
    public class InstrumentPlayer : AudioPlayer
    {
        struct PlayerNote
        {
            public int channel;
            public Note note;
        };

        int activeChannel = -1;
        int expansionAudio = ExpansionType.None;
        int numExpansionChannels = 0;
        int[] envelopeFrames = new int[EnvelopeType.Count];
        ConcurrentQueue<PlayerNote> noteQueue = new ConcurrentQueue<PlayerNote>();
        bool IsRunning => playerThread != null;

        public InstrumentPlayer(bool pal) : base(NesApu.APU_INSTRUMENT, pal, DefaultSampleRate, Settings.NumBufferedAudioFrames)
        {
        }

        public void PlayNote(int channel, Note note)
        {
            if (IsRunning)
            {
                noteQueue.Enqueue(new PlayerNote() { channel = channel, note = note });
            }
        }

        public void ReleaseNote(int channel)
        {
            if (IsRunning)
            {
                noteQueue.Enqueue(new PlayerNote() { channel = channel, note = new Note() { Value = Note.NoteRelease } });
            }
        }

        public void StopAllNotes()
        {
            if (IsRunning)
            {
                noteQueue.Enqueue(new PlayerNote() { channel = -1 });
            }
        }

        public void StopAllNotesAndWait()
        {
            if (IsRunning)
            {
                while (!noteQueue.IsEmpty) noteQueue.TryDequeue(out _);
                noteQueue.Enqueue(new PlayerNote() { channel = -1 });
                while (!noteQueue.IsEmpty) Thread.Sleep(1);
            }
        }
        
        public void Start(Project project, bool pal)
        {
            expansionAudio = project.ExpansionAudio;
            numExpansionChannels = project.ExpansionNumChannels;
            palPlayback = pal;
            channelStates = CreateChannelStates(this, project, apuIndex, numExpansionChannels, palPlayback);
            
            ResetThreadingObjects();

            playerThread = new Thread(PlayerThread);
            playerThread.Start();
        }

        public void Stop(bool stopNotes = true)
        {
            if (IsRunning)
            {
                if (stopNotes)
                    StopAllNotesAndWait();

                stopEvent.Set();
                playerThread.Join();
                playerThread = null;
                channelStates = null;
            }
        }

        public int GetEnvelopeFrame(int idx)
        {
            return envelopeFrames[idx]; // TODO: Account for output delay.
        }

        unsafe void PlayerThread(object o)
        {
            activeChannel = -1;

            var lastNoteWasRelease = false;
            var lastReleaseTime = DateTime.Now;

            var waitEvents = new WaitHandle[] { stopEvent, bufferSemaphore };

            NesApu.InitAndReset(apuIndex, sampleRate, palPlayback, expansionAudio, numExpansionChannels, dmcCallback);
            for (int i = 0; i < channelStates.Length; i++)
                NesApu.EnableChannel(apuIndex, i, 0);

            while (true)
            {
                int idx = WaitHandle.WaitAny(waitEvents);

                if (idx == 0)
                {
                    break;
                }

                if (!noteQueue.IsEmpty)
                {
                    PlayerNote lastNote = new PlayerNote();
                    while (noteQueue.TryDequeue(out PlayerNote note))
                    {
                        lastNote = note;
                    }

                    activeChannel = lastNote.channel;
                    if (activeChannel >= 0)
                    {
                        if (lastNote.note.IsMusical)
                            channelStates[activeChannel].ForceInstrumentReload();

                        channelStates[activeChannel].PlayNote(lastNote.note);

                        if (lastNote.note.IsRelease)
                        {
                            lastNoteWasRelease = true;
                            lastReleaseTime = DateTime.Now;
                        }
                        else
                        {
                            lastNoteWasRelease = false;
                        }
                    }

                    for (int i = 0; i < channelStates.Length; i++)
                        NesApu.EnableChannel(apuIndex, i, i == activeChannel ? 1 : 0);
                }

                if (lastNoteWasRelease &&
                    activeChannel >= 0 &&
                    Settings.InstrumentStopTime >= 0 &&
                    DateTime.Now.Subtract(lastReleaseTime).TotalSeconds >= Settings.InstrumentStopTime)
                {
                    NesApu.EnableChannel(apuIndex, activeChannel, 0);
                    activeChannel = -1;
                }

                if (activeChannel >= 0)
                {
                    channelStates[activeChannel].Update();

                    for (int i = 0; i < EnvelopeType.Count; i++)
                        envelopeFrames[i] = channelStates[activeChannel].GetEnvelopeFrame(i);
                }
                else
                {
                    for (int i = 0; i < EnvelopeType.Count; i++)
                        envelopeFrames[i] = 0;
                    foreach (var channel in channelStates)
                        channel.ClearNote();
                }

                EndFrame();
            }

            audioStream.Stop();
            while (sampleQueue.TryDequeue(out _)) ;
        }

        public bool IsPlaying => activeChannel >= 0;

        public void PlayRawPcmSample(short[] data, int sampleRate, float volume)
        {
            audioStream.PlayImmediate(data, sampleRate, volume);
        }

        public void StopRawPcmSample()
        {
            audioStream.StopImmediate();
        }

        public int RawPcmSamplePlayPosition => audioStream.ImmediatePlayPosition;
    }
}
