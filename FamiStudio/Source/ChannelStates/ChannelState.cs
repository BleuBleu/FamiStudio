using System;
using System.Diagnostics;

namespace FamiStudio
{
    public abstract class ChannelState
    {
        protected int apuIdx;
        protected int channelType;
        protected int delayedNoteCounter = 0;
        protected int delayedCutCounter = 0;
        protected int delayedNoteSlidePitch = 0;
        protected int delayedNoteSlideStep = 0;
        protected Note delayedNote = null;
        protected Note note = new Note(Note.NoteInvalid);
        protected int dutyCycle = 0;
        protected bool pitchEnvelopeOverride = false;
        protected bool arpeggioEnvelopeOverride = false;
        protected Envelope[] envelopes = new Envelope[Envelope.Count];
        protected int[] envelopeIdx = new int[Envelope.Count];
        protected int[] envelopeValues = new int[Envelope.Count];
        protected bool customRelease = false;
        protected bool noteTriggered = false;
        protected ushort[] noteTable = null;
        protected bool palPlayback = false;
        protected int maximumPeriod = NesApu.MaximumPeriod11Bit;
        protected int slideStep = 0;
        private   int slidePitch = 0;
        private   int slideShift = 0;
        private   int pitchShift = 0;
        private   byte noteValueBeforeSlide = 0;
        private   IRegisterListener registerListener;

        public ChannelState(int apu, int type, bool pal, int numN163Channels = 1)
        {
            apuIdx = apu;
            channelType = type;
            palPlayback = pal;
            maximumPeriod = NesApu.GetPitchLimitForChannelType(channelType);
            noteTable = NesApu.GetNoteTableForChannelType(channelType, pal, numN163Channels);
            note.Value = Note.NoteStop;
            note.Volume = Note.VolumeMax;
            note.FinePitch = 0;
            Channel.GetShiftsForType(type, numN163Channels, out pitchShift, out slideShift);
        }

        public void Advance(Song song, int patternIdx, int noteIdx, ref int famitrackerSpeed)
        {
            // When advancing row, if there was a delayed note, play it immediately. That's how FamiTracker does it.
            if (delayedNote != null)
            {
                PlayNote(delayedNote, delayedNoteSlidePitch, delayedNoteSlideStep);
                delayedNote = null;
                delayedNoteCounter = 0;
            }

            var channel = song.GetChannelByType(channelType);
            var pattern = channel.PatternInstances[patternIdx];

            if (pattern == null)
                return;

            if (pattern.Notes.TryGetValue(noteIdx, out var newNote))
            { 
                // We dont delay speed effects. This is not what FamiTracker does, but I dont care.
                // There is a special place in hell for people who delay speed effect.
                if (newNote.HasSpeed)
                {
                    famitrackerSpeed = newNote.Speed;
                }

                // Slide params needs to be computed right away since we wont have access to the play position/channel later.
                int noteSlidePitch = 0;
                int noteSlideStep  = 0;

                if (newNote.IsValid && newNote.IsSlideNote)
                {
                    channel.ComputeSlideNoteParams(newNote, patternIdx, noteIdx, famitrackerSpeed, noteTable, palPlayback, out noteSlidePitch, out noteSlideStep);
                }

                // Store note for later if delayed.
                if (newNote.HasNoteDelay)
                {
                    delayedNote = newNote;
                    delayedNoteCounter = newNote.NoteDelay + 1;
                    delayedNoteSlidePitch = noteSlidePitch;
                    delayedNoteSlideStep  = noteSlideStep;
                    return;
                }

                PlayNote(newNote, noteSlidePitch, noteSlideStep);
            }
        }

        public void PlayNote(Note newNote, int noteSlidePitch = 0, int noteSlideStep = 0)
        {
            newNote = newNote.Clone();

            // Pass on the same effect values if this note doesn't specify them.
            if (!newNote.HasVolume         && note.HasVolume)          newNote.Volume      = note.Volume;
            if (!newNote.HasFinePitch      && note.HasFinePitch)       newNote.FinePitch   = note.FinePitch;
            if (!newNote.HasFdsModDepth    && note.HasFdsModDepth)     newNote.FdsModDepth = note.FdsModDepth;
            if (!newNote.HasFdsModSpeed    && note.HasFdsModSpeed)     newNote.FdsModSpeed = note.FdsModSpeed;
            if (newNote.Instrument == null && note.Instrument != null) newNote.Instrument  = note.Instrument;
            if (newNote.Arpeggio   == null && note.Arpeggio   != null && newNote.IsValid && !newNote.IsMusical) newNote.Arpeggio = note.Arpeggio;

            if (newNote.IsValid)
            {
                // Any note that isnt a release cancels the current slide.
                if (!newNote.IsRelease)
                    slideStep = 0;

                if (newNote.IsSlideNote)
                {
                    slidePitch = noteSlidePitch;
                    slideStep  = noteSlideStep;
                    noteValueBeforeSlide = newNote.Value;
                    newNote.Value = newNote.SlideNoteTarget;
                }

                // A new valid note always cancels any delayed cut.
                delayedCutCounter = 0;
            }

            if (newNote.IsStop)
            {
                note = newNote;
            }
            else if (newNote.IsRelease)
            {
                // Jump to release point.
                if (note.Instrument != null)
                {
                    for (int j = 0; j < Envelope.Count; j++)
                    {
                        if (envelopes[j] != null && envelopes[j].Release >= 0)
                            envelopeIdx[j] = envelopes[j].Release;
                    }
                }

                // Channels with custom release code (VRC7) will do their own thing.
                if (!customRelease)
                    newNote.Value = note.Value;

                note = newNote;
            }
            else if (newNote.IsMusical)
            {
                bool instrumentChanged = note.Instrument != newNote.Instrument;
                bool arpeggioChanged   = note.Arpeggio   != newNote.Arpeggio;

                note = newNote;

                // Set/clear override when changing arpeggio
                if (arpeggioChanged)
                {
                    if (note.Arpeggio != null)
                    {
                        envelopes[Envelope.Arpeggio] = note.Arpeggio.Envelope;
                        arpeggioEnvelopeOverride = true;
                    }
                    else
                    {
                        envelopes[Envelope.Arpeggio] = null;
                        arpeggioEnvelopeOverride = false;
                    }

                    envelopeIdx[Envelope.Arpeggio] = 0;
                    envelopeValues[Envelope.Arpeggio] = 0;
                }
                // If same arpeggio, but note has an attack, reset it.
                else if (note.HasAttack && arpeggioEnvelopeOverride)
                {
                    envelopeIdx[Envelope.Arpeggio] = 0;
                    envelopeValues[Envelope.Arpeggio] = 0;
                }

                if (instrumentChanged || note.HasAttack)
                {
                    for (int j = 0; j < Envelope.Count; j++)
                    {
                        if ((j != Envelope.Pitch     || !pitchEnvelopeOverride) &&
                            (j != Envelope.Arpeggio  || !arpeggioEnvelopeOverride))
                        {
                            envelopes[j] = note.Instrument == null ? null : note.Instrument.Envelopes[j];
                        }
                        envelopeIdx[j] = 0;
                    }

                    envelopeValues[Envelope.DutyCycle] = dutyCycle;
                    envelopeValues[Envelope.Pitch] = 0; // In case we use relative envelopes.
                    noteTriggered = true;
                }

                if (instrumentChanged)
                {
                    LoadInstrument(note.Instrument);
                }
            }
            else
            {
                // Empty notes just keep whatever value we had.
                Debug.Assert(!newNote.IsValid);
                newNote.Value = note.Value;
                note = newNote;
            }

            if (note.HasVibrato)
            {
                if (note.VibratoDepth != 0 && note.VibratoDepth != 0)
                {
                    envelopes[Envelope.Pitch] = Envelope.CreateVibratoEnvelope(note.VibratoSpeed, note.VibratoDepth);
                    envelopeIdx[Envelope.Pitch] = 0;
                    envelopeValues[Envelope.Pitch] = 0;
                    pitchEnvelopeOverride = true;
                }
                else
                {
                    envelopes[Envelope.Pitch] = null;
                    pitchEnvelopeOverride = false;
                }
            }
            
            // MATTT: What is this???
            if (!note.HasFinePitch)
            {
                note.FinePitch = 0;
            }

            if (note.HasDutyCycle)
            {
                dutyCycle = note.DutyCycle;
                envelopeValues[Envelope.DutyCycle] = dutyCycle;
            }

            if (note.HasCutDelay)
            {
                delayedCutCounter = note.CutDelay + 1;
            }
        }

        private void UpdateDelayedNote()
        {
            Debug.Assert(delayedCutCounter  >= 0);
            Debug.Assert(delayedNoteCounter >= 0);

            if (delayedNote != null)
            {
                Debug.Assert(delayedNoteCounter > 0);

                if (--delayedNoteCounter == 0)
                {
                    PlayNote(delayedNote, delayedNoteSlidePitch, delayedNoteSlideStep);
                    delayedNote = null;
                }
            }
            else
            {
                Debug.Assert(delayedNoteCounter == 0);
            }

            if (delayedCutCounter > 0)
            {
                if (--delayedCutCounter == 0)
                {
                    PlayNote(new Note(Note.NoteStop));
                }
            }
        }

        public void Update()
        {
            UpdateDelayedNote();
            UpdateEnvelopes();
            UpdateSlide();
            UpdateAPU();
        }

        private void UpdateEnvelopes()
        {
            if (note.Instrument != null)
            {
                for (int j = 0; j < Envelope.Count; j++)
                {
                    if (envelopes[j] == null || envelopes[j].IsEmpty)
                    {
                        if (j == Envelope.Volume)
                            envelopeValues[j] = 15;
                        else if (j != Envelope.DutyCycle)
                            envelopeValues[j] = 0;
                        continue;
                    }

                    var env = envelopes[j];
                    var idx = envelopeIdx[j];

                    // Non-looping, relative envelope end up with -1 when done.
                    if (idx < 0)
                    {
                        Debug.Assert(env.Relative);
                        continue;
                    }

                    if (env.Relative)
                    {
                        Debug.Assert(j == Envelope.Pitch);
                        envelopeValues[j] += envelopes[j].Values[idx];
                    }
                    else
                    {
                        envelopeValues[j] = envelopes[j].Values[idx];
                    }

                    idx++;

                    if (env.Release >= 0 && idx == env.Release)
                        envelopeIdx[j] = env.Loop;
                    else if (idx >= env.Length)
                        envelopeIdx[j] = env.Loop >= 0 && env.Release < 0 ? env.Loop : (env.Relative ? -1 : env.Length - 1);
                    else
                        envelopeIdx[j] = idx;
                }
            }
        }

        private void UpdateSlide()
        {
            if (slideStep != 0)
            {
                slidePitch += slideStep;

                if ((slideStep > 0 && slidePitch > 0) ||
                    (slideStep < 0 && slidePitch < 0))
                {
                    slidePitch = 0;
                }
            }
            else
            {
                slidePitch = 0;
            }
        }

        public void SetRegisterListener(IRegisterListener listener)
        {
            registerListener = listener;
        }

        protected void WriteRegister(int reg, int data)
        {
            NesApu.WriteRegister(apuIdx, reg, data);

            if (registerListener != null)
                registerListener.WriteRegister(apuIdx, reg, data);
        }

        protected bool IsSeeking
        {
            get { return NesApu.IsSeeking(apuIdx) != 0; }
        }
        
        public int GetEnvelopeFrame(int envIdx)
        {
            return envelopeIdx[envIdx];
        }

        public void ClearNote()
        {
            arpeggioEnvelopeOverride = false;
            note.Arpeggio = null;
            note.Instrument = null;
            for (int i = 0; i < Envelope.Count; i++)
                envelopes[i] = null;
        }

        protected int MultiplyVolumes(int v0, int v1)
        {
            var vol = (int)Math.Round((v0 / 15.0f) * (v1 / 15.0f) * 15.0f);
            if (vol == 0 && v0 != 0 && v1 != 0) return 1;
            return vol;
        }

        protected virtual void LoadInstrument(Instrument instrument)
        {
        }

        protected int GetPeriod()
        {
            var noteVal = Utils.Clamp(note.Value + envelopeValues[Envelope.Arpeggio], 0, noteTable.Length - 1);
            var pitch = (note.FinePitch + envelopeValues[Envelope.Pitch]) << pitchShift;
            var slide = slideShift < 0 ? (slidePitch >> -slideShift) : (slidePitch << slideShift); // Remove the fraction part.
            return Utils.Clamp(noteTable[noteVal] + pitch + slide, 0, maximumPeriod);
        }

        protected int GetVolume()
        {
            return MultiplyVolumes(note.Volume, envelopeValues[Envelope.Volume]);
        }

        protected int GetDuty()
        {
            return envelopeValues[Envelope.DutyCycle];
        }

        public virtual void UpdateAPU()
        {
            noteTriggered = false;
        }

        public Note CurrentNote
        {
            get
            {
                var n = note.Clone();
                if (n.IsSlideNote)
                    n.Value = noteValueBeforeSlide;
                return n;
            }
        }
        public int  CurrentVolume => MultiplyVolumes(note.Volume, envelopeValues[Envelope.Volume]);
    };

    public interface IRegisterListener
    {
        void WriteRegister(int apuIndex, int reg, int data);
    }
}
