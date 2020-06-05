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

        int expansionAudio = Project.ExpansionNone;
        int numExpansionChannels = 0;
        int[] envelopeFrames = new int[Envelope.Count];
        ConcurrentQueue<PlayerNote> noteQueue = new ConcurrentQueue<PlayerNote>();
        bool IsRunning => playerThread != null;

        public InstrumentPlayer() : base(NesApu.APU_INSTRUMENT)
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
            channelStates = CreateChannelStates(project, apuIndex, numExpansionChannels, pal);
            palMode = pal;

            stopEvent.Reset();
            frameEvent.Set();
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
            var lastNoteWasRelease = false;
            var lastReleaseTime = DateTime.Now;

            var activeChannel = -1;
            var waitEvents = new WaitHandle[] { stopEvent, frameEvent };

            NesApu.InitAndReset(apuIndex, sampleRate, palMode, expansionAudio, numExpansionChannels, dmcCallback);
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
                    channelStates[activeChannel].UpdateEnvelopes();
                    channelStates[activeChannel].UpdateAPU();

                    for (int i = 0; i < Envelope.Count; i++)
                        envelopeFrames[i] = channelStates[activeChannel].GetEnvelopeFrame(i);
                }
                else
                {
                    for (int i = 0; i < Envelope.Count; i++)
                        envelopeFrames[i] = 0;
                    foreach (var channel in channelStates)
                        channel.ClearNote();
                }

                EndFrame();
            }

            audioStream.Stop();
            while (sampleQueue.TryDequeue(out _)) ;
        }
    }
}
