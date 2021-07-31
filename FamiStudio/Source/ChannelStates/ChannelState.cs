using System;
using System.Diagnostics;

namespace FamiStudio
{
    public abstract class ChannelState
    {
        const int CyclesBetweenChannels = 120;

        protected int apuIdx;
        protected int channelType;
        protected int releaseCounter = 0;
        protected int durationCounter = 0;
        protected int delayedNoteCounter = 0;
        protected int delayedCutCounter = 0;
        protected int delayedNoteSlidePitch = 0;
        protected int delayedNoteSlideStep = 0;
        protected int delayedNoteVolumeSlideStep = 0;
        protected Note delayedNote = null;
        protected Note note = new Note(Note.NoteInvalid);
        protected int dutyCycle = 0;
        protected bool pitchEnvelopeOverride = false;
        protected bool arpeggioEnvelopeOverride = false;
        protected Envelope[] envelopes = new Envelope[EnvelopeType.Count];
        protected int[] envelopeIdx = new int[EnvelopeType.Count];
        protected int[] envelopeValues = new int[EnvelopeType.Count];
        protected bool customRelease = false;
        protected bool noteTriggered = false;
        protected bool forceInstrumentReload = false;
        protected ushort[] noteTable = null;
        protected bool palPlayback = false;
        protected int maximumPeriod = NesApu.MaximumPeriod11Bit;
        protected int slideStep = 0;
        protected int slidePitch = 0;
        protected int slideShift = 0;
        protected int pitchShift = 0;
        protected int volume = Note.VolumeMax << 4;
        protected int volumeSlideStep = 0;
        protected int volumeSlideTarget = 0;
        protected byte noteValueBeforeSlide = 0;
        protected IPlayerInterface player;

        public int InnerChannelType => channelType; 

        public ChannelState(IPlayerInterface play, int apu, int type, bool pal, int numN163Channels = 1)
        {
            player = play;
            apuIdx = apu;
            channelType = type;
            palPlayback = pal;
            maximumPeriod = NesApu.GetPitchLimitForChannelType(channelType);
            noteTable = NesApu.GetNoteTableForChannelType(channelType, pal, numN163Channels);
            note.Value = Note.NoteStop;
            note.FinePitch = 0;
            Channel.GetShiftsForType(type, numN163Channels, out pitchShift, out slideShift);
        }

        public void Advance(Song song, NoteLocation location, ref int famitrackerSpeed)
        {
            // When advancing row, if there was a delayed note, play it immediately. That's how FamiTracker does it.
            if (delayedNote != null)
            {
                PlayNote(delayedNote, delayedNoteSlidePitch, delayedNoteSlideStep);
                delayedNote = null;
                delayedNoteCounter = 0;
            }

            var channel = song.GetChannelByType(channelType);
            var pattern = channel.PatternInstances[location.PatternIndex];
            var newNote = (Note)null;

            if (pattern != null)
            {
                pattern.Notes.TryGetValue(location.NoteIndex, out newNote);
            }

            var needClone = true;

            // Generate a release note if the release counter reaches zero.
            if (releaseCounter > 0 && --releaseCounter == 0 && (newNote == null || !newNote.IsMusicalOrStop))
            {
                newNote = newNote == null ? new Note() : newNote.Clone();
                newNote.Value = Note.NoteRelease;
                needClone = false;
            }

            // Generate a stop note if the stop counter reaches zero.
            if (durationCounter > 0 && --durationCounter == 0 && (newNote == null || !newNote.IsMusicalOrStop))
            {
                newNote = newNote == null ? new Note() : newNote.Clone();
                newNote.Value = Note.NoteStop;
                needClone = false;
            }

            if (newNote != null)
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

                if (newNote.IsMusical)
                {
                    if (newNote.IsSlideNote)
                    {
                        channel.ComputeSlideNoteParams(newNote, location, famitrackerSpeed, noteTable, palPlayback, true, out noteSlidePitch, out noteSlideStep, out _);
                    }

                    if (newNote.HasRelease)
                    {
                        var releaseLocation = location.Advance(song, newNote.Release);
                        // Don't process release that go beyond the end of the song.
                        if (releaseLocation.IsInSong(song))
                           releaseCounter = newNote.Release;
                    }

                    var stopLocation = location.Advance(song, newNote.Duration);
                    // Don't stop notes that go beyond the end of the song.
                    if (stopLocation.IsInSong(song))
                        durationCounter = newNote.Duration;
                }

                if (newNote.HasVolumeSlide)
                {
                    channel.ComputeVolumeSlideNoteParams(newNote, location, famitrackerSpeed, palPlayback, out volumeSlideStep, out _);
                }

                // Store note for later if delayed.
                if (newNote.HasNoteDelay)
                {
                    delayedNote = newNote;
                    delayedNoteCounter = newNote.NoteDelay + 1;
                    delayedNoteSlidePitch = noteSlidePitch;
                    delayedNoteSlideStep  = noteSlideStep;
                    delayedNoteVolumeSlideStep = volumeSlideStep;
                    return;
                }

                PlayNote(newNote, noteSlidePitch, noteSlideStep, volumeSlideStep, needClone);
            }
        }

        public void PlayNote(Note newNote, int noteSlidePitch = 0, int noteSlideStep = 0, int noteVolumeSlideStep = 0, bool needClone = true)
        {
            if (needClone)
                newNote = newNote.Clone();

            // Pass on the same effect values if this note doesn't specify them.
            if (!newNote.HasFinePitch      && note.HasFinePitch)       newNote.FinePitch   = note.FinePitch;
            if (!newNote.HasFdsModDepth    && note.HasFdsModDepth)     newNote.FdsModDepth = note.FdsModDepth;
            if (!newNote.HasFdsModSpeed    && note.HasFdsModSpeed)     newNote.FdsModSpeed = note.FdsModSpeed;
            //if (!newNote.HasDeltaCounter   && note.HasDeltaCounter)    newNote.DeltaCounter = note.DeltaCounter; MATTT
            if (newNote.Instrument == null && note.Instrument != null) newNote.Instrument  = note.Instrument;
            if (newNote.Arpeggio   == null && note.Arpeggio   != null && !newNote.IsMusical) newNote.Arpeggio = note.Arpeggio;

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
                    for (int j = 0; j < EnvelopeType.Count; j++)
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
                bool instrumentChanged = note.Instrument != newNote.Instrument || forceInstrumentReload;
                bool arpeggioChanged   = note.Arpeggio   != newNote.Arpeggio;

                note = newNote;

                // Set/clear override when changing arpeggio
                if (arpeggioChanged)
                {
                    if (note.Arpeggio != null)
                    {
                        envelopes[EnvelopeType.Arpeggio] = note.Arpeggio.Envelope;
                        arpeggioEnvelopeOverride = true;
                    }
                    else
                    {
                        envelopes[EnvelopeType.Arpeggio] = null;
                        arpeggioEnvelopeOverride = false;
                    }

                    envelopeIdx[EnvelopeType.Arpeggio] = 0;
                    envelopeValues[EnvelopeType.Arpeggio] = 0;
                }
                // If same arpeggio, but note has an attack, reset it.
                else if (note.HasAttack && arpeggioEnvelopeOverride)
                {
                    envelopeIdx[EnvelopeType.Arpeggio] = 0;
                    envelopeValues[EnvelopeType.Arpeggio] = 0;
                }

                if (instrumentChanged || note.HasAttack)
                {
                    for (int j = 0; j < EnvelopeType.Count; j++)
                    {
                        if ((j != EnvelopeType.Pitch     || !pitchEnvelopeOverride) &&
                            (j != EnvelopeType.Arpeggio  || !arpeggioEnvelopeOverride))
                        {
                            envelopes[j] = note.Instrument == null ? null : note.Instrument.Envelopes[j];
                        }
                        envelopeIdx[j] = 0;
                    }

                    envelopeValues[EnvelopeType.DutyCycle] = dutyCycle;
                    envelopeValues[EnvelopeType.Pitch] = 0; // In case we use relative envelopes.
                    noteTriggered = true;
                }

                if (instrumentChanged)
                {
                    LoadInstrument(note.Instrument);
                    forceInstrumentReload = false;
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
                    envelopes[EnvelopeType.Pitch] = Envelope.CreateVibratoEnvelope(note.VibratoSpeed, note.VibratoDepth);
                    envelopeIdx[EnvelopeType.Pitch] = 0;
                    envelopeValues[EnvelopeType.Pitch] = 0;
                    pitchEnvelopeOverride = true;
                }
                else
                {
                    envelopes[EnvelopeType.Pitch] = null;
                    pitchEnvelopeOverride = false;
                }
            }
            
            // Fine pitch will always we read, so make sure it has a value.
            if (!note.HasFinePitch)
            {
                note.FinePitch = 0;
            }

            if (note.HasDutyCycle)
            {
                dutyCycle = note.DutyCycle;
                envelopeValues[EnvelopeType.DutyCycle] = dutyCycle;
            }

            if (note.HasVolume)
            {
                volume = note.Volume << 4;
                volumeSlideStep = 0;
            }

            if (note.HasVolumeSlide)
            {
                volumeSlideTarget = note.VolumeSlideTarget << 4;
                volumeSlideStep = noteVolumeSlideStep;
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
                    PlayNote(new Note(Note.NoteStop), 0, 0, 0, false);
                }
            }
        }

        public void Update()
        {
            UpdateDelayedNote();
            UpdateEnvelopes();
            UpdateSlide();
            UpdateVolumeSlide();
            UpdateAPU();
        }

        private void UpdateEnvelopes()
        {
            if (note.Instrument != null)
            {
                for (int j = 0; j < EnvelopeType.Count; j++)
                {
                    if (envelopes[j] == null || envelopes[j].IsEmpty(j))
                    {
                        if (j == EnvelopeType.Volume)
                            envelopeValues[j] = 15;
                        else if (j != EnvelopeType.DutyCycle)
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
                        Debug.Assert(j == EnvelopeType.Pitch);
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
                    slideStep = 0;
                }
            }
            else
            {
                slidePitch = 0;
            }
        }

        private void UpdateVolumeSlide()
        {
            if (volumeSlideStep != 0)
            {
                volume += volumeSlideStep;

                if ((volumeSlideStep > 0 && volume >= volumeSlideTarget) ||
                    (volumeSlideStep < 0 && volume <= volumeSlideTarget))
                {
                    volume = volumeSlideTarget;
                    volumeSlideStep = 0;
                }
            }
        }

        protected void WriteRegister(int reg, int data)
        {
            NesApu.WriteRegister(apuIdx, reg, data);
            player.NotifyRegisterWrite(apuIdx, reg, data);
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
            for (int i = 0; i < EnvelopeType.Count; i++)
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

        public virtual void IntrumentLoadedNotify(Instrument instrument)
        {
        }

        public void ForceInstrumentReload()
        {
            forceInstrumentReload = true;
        }

        protected int GetPeriod()
        {
            var noteVal = Utils.Clamp(note.Value + envelopeValues[EnvelopeType.Arpeggio], 0, noteTable.Length - 1);
            var pitch = (note.FinePitch + envelopeValues[EnvelopeType.Pitch]) << pitchShift;
            var slide = slideShift < 0 ? (slidePitch >> -slideShift) : (slidePitch << slideShift); // Remove the fraction part.
            return Utils.Clamp(noteTable[noteVal] + pitch + slide, 0, maximumPeriod);
        }

        protected int GetVolume()
        {
            // TODO : Move the 4 to a constant.
            return MultiplyVolumes(volume >> 4, envelopeValues[EnvelopeType.Volume]);
        }

        protected int GetDuty()
        {
            return envelopeValues[EnvelopeType.DutyCycle];
        }

        public virtual void UpdateAPU()
        {
            noteTriggered = false;
            NesApu.SkipCycles(apuIdx, CyclesBetweenChannels);
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
        public int  CurrentVolume => MultiplyVolumes(volume >> 4, envelopeValues[EnvelopeType.Volume]);
    };
}
