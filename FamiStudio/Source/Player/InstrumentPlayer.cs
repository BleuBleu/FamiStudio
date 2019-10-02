using System;
using System.Collections.Concurrent;
using System.Threading;

namespace FamiStudio
{
    public class InstrumentPlayer : PlayerBase
    {
        struct PlayerNote
        {
            public int channel;
            public Note note;
        };

        int[] envelopeFrames = new int[Envelope.Max];
        ConcurrentQueue<PlayerNote> noteQueue = new ConcurrentQueue<PlayerNote>();

        public InstrumentPlayer() : base(NesApu.APU_INSTRUMENT)
        {
        }

        public void PlayNote(int channel, Note note)
        {
            noteQueue.Enqueue(new PlayerNote() { channel = channel, note = note });
        }

        public void ReleaseNote(int channel)
        {
            noteQueue.Enqueue(new PlayerNote() { channel = channel, note = new Note() { Value = Note.NoteRelease } });
        }

        public void StopAllNotes()
        {
            noteQueue.Enqueue(new PlayerNote() { channel = -1 });
        }

        public void StopAllNotesAndWait()
        {
            while (!noteQueue.IsEmpty) noteQueue.TryDequeue(out _);
            noteQueue.Enqueue(new PlayerNote() { channel = -1 });
            while (!noteQueue.IsEmpty) Thread.Sleep(1);
        }

        public override void Initialize()
        {
            base.Initialize();

            playerThread = new Thread(PlayerThread);
            playerThread.Start();
        }

        public int GetEnvelopeFrame(int idx)
        {
            return envelopeFrames[idx]; // TODO: Account for output delay.
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

            var lastNoteWasRelease = false;
            var lastReleaseTime = DateTime.Now;

            var activeChannel = -1;
            var waitEvents = new WaitHandle[] { stopEvent, frameEvent };

            NesApu.Reset(apuIndex);
            for (int i = 0; i < 5; i++)
                NesApu.NesApuEnableChannel(apuIndex, i, 0);

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
                        channels[activeChannel].PlayNote(lastNote.note);

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

                    for (int i = 0; i < 5; i++)
                        NesApu.NesApuEnableChannel(apuIndex, i, i == activeChannel ? 1 : 0);
                }

                if (lastNoteWasRelease &&
                    activeChannel >= 0 &&
                    Settings.InstrumentStopTime >= 0 &&
                    DateTime.Now.Subtract(lastReleaseTime).TotalSeconds >= Settings.InstrumentStopTime)
                {
                    NesApu.NesApuEnableChannel(apuIndex, activeChannel, 0);
                    activeChannel = -1;
                }

                if (activeChannel >= 0)
                {
                    channels[activeChannel].UpdateEnvelopes();
                    channels[activeChannel].UpdateAPU();

                    for (int i = 0; i < Envelope.Max; i++)
                        envelopeFrames[i] = channels[activeChannel].GetEnvelopeFrame(i);
                }
                else
                {
                    for (int i = 0; i < Envelope.Max; i++)
                        envelopeFrames[i] = 0;
                    foreach (var channel in channels)
                        channel.ClearNote();
                }

                EndFrameAndQueueSamples();
            }

            audioStream.Stop();
            while (sampleQueue.TryDequeue(out _)) ;
        }
    }
}
