using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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
        bool lastNoteWasRelease = false;
        DateTime lastReleaseTime = DateTime.Now;
        int playingNote = Note.NoteInvalid;
        int[] envelopeFrames = new int[EnvelopeType.Count];
        ConcurrentQueue<PlayerNote> noteQueue = new ConcurrentQueue<PlayerNote>();

        public bool IsPlayingAnyNotes => activeChannel >= 0;
        public int  PlayingNote => playingNote;

        public InstrumentPlayer(IAudioStream stream, bool pal, int sampleRate, bool stereo, int numFrames) : base(stream, NesApu.APU_INSTRUMENT, pal, sampleRate, stereo, numFrames)
        {
        }

        public void PlayNote(int channel, Note note)
        {
            if (IsPlaying)
            {
                noteQueue.Enqueue(new PlayerNote() { channel = channel, note = note });
            }
        }

        public void ReleaseNote(int channel)
        {
            if (IsPlaying)
            {
                noteQueue.Enqueue(new PlayerNote() { channel = channel, note = new Note() { Value = Note.NoteRelease } });
            }
        }

        public void StopAllNotes()
        {
            if (IsPlaying)
            {
                noteQueue.Clear(); 
                noteQueue.Enqueue(new PlayerNote() { channel = -1 });
            }
        }

        public void Start(Project project, bool pal)
        {
            if (audioStream == null)
                return;

            palPlayback = pal;
            channelStates = CreateChannelStates(project, apuIndex, project.Tuning, palPlayback, project.ExpansionNumN163Channels);
            activeChannel = -1;
            lastNoteWasRelease = false;
            lastReleaseTime = DateTime.Now;

            InitAndResetApu(project);

            for (int i = 0; i < channelStates.Length; i++)
                EnableChannelType(channelStates[i].InnerChannelType, false);

            if (UsesEmulationThread)
            {
                Debug.Assert(emulationThread == null);
                Debug.Assert(emulationQueue.Count == 0);

                ResetThreadingObjects();

                emulationThread = new Thread(EmulationThread);
                emulationThread.Start();
            }

            audioStream.Stop(); // Extra safety
            audioStream.Start(AudioBufferFillCallback, AudioStreamStartingCallback);
        }

        public void Stop(bool stopNotes = true)
        {
            if (IsPlaying)
            {
                if (stopNotes)
                    StopAllNotes();

                if (UsesEmulationThread)
                {
                    stopEvent.Set();
                    emulationThread.Join();
                    emulationThread = null;
                }
            }

            audioStream?.Stop();
            emulationQueue?.Clear();
            channelStates = null;
        }

        public int GetEnvelopeFrame(int idx)
        {
            return envelopeFrames[idx]; // TODO: Account for output delay.
        }

        protected override FrameAudioData GetFrameAudioData()
        {
            var data = base.GetFrameAudioData();

            // Read back trigger for oscilloscope.
            if (activeChannel >= 0)
                data.triggerSample = GetOscilloscopeTrigger(channelStates[activeChannel].InnerChannelType);

            return data;
        }

        protected override bool EmulateFrame()
        {
            BeginFrame();

            var now = DateTime.Now;

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

                    // HACK : If we played a DPCM sample before, the DAC value
                    // may not be the default and this will affect the volume
                    // of the triangle/noise channel. Reset it to the default.
                    if (activeChannel == ChannelType.Triangle ||
                        activeChannel == ChannelType.Noise)
                    {
                        var dacNote = new Note();
                        dacNote.DeltaCounter = NesApu.DACDefaultValue;
                        channelStates[ChannelType.Dpcm].PlayNote(dacNote);
                    }

                    channelStates[activeChannel].PlayNote(lastNote.note);

                    if (lastNote.note.IsRelease)
                    {
                        lastNoteWasRelease = true;
                        lastReleaseTime = now;
                    }
                    else
                    {
                        lastNoteWasRelease = false;
                    }
                }

                for (int i = 0; i < channelStates.Length; i++)
                    EnableChannelType(channelStates[i].InnerChannelType, i == activeChannel);
            }

            if (lastNoteWasRelease &&
                activeChannel >= 0 &&
                now.Subtract(lastReleaseTime).TotalSeconds >= Math.Max(0.01f, Settings.InstrumentStopTime))
            {
                EnableChannelType(channelStates[activeChannel].InnerChannelType, false);
                activeChannel = -1;
            }

            if (activeChannel >= 0)
            {
                var channel = channelStates[activeChannel];
                channel.Update();

                for (int i = 0; i < EnvelopeType.Count; i++)
                    envelopeFrames[i] = channel.GetEnvelopeFrame(i);

                playingNote = channel.CurrentNote != null && channel.CurrentNote.IsMusical ? channel.CurrentNote.Value : Note.NoteInvalid;
            }
            else
            {
                for (int i = 0; i < EnvelopeType.Count; i++)
                    envelopeFrames[i] = 0;
                foreach (var channel in channelStates)
                    channel.ClearNote();

                playingNote = Note.NoteInvalid;
            }

            EndFrame();
            
            return true;
        }

        void EmulationThread(object o)
        {
            var waitEvents = new WaitHandle[] { stopEvent, emulationSemaphore };

            while (true)
            {
                if (WaitHandle.WaitAny(waitEvents) == 0)
                    break;

                EmulateFrame();
            }
        }
    }
}
