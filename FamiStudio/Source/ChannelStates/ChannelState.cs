using System;
using System.Diagnostics;
using System.Collections.Generic;

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
        protected bool resetInstrumentOnNextAttack = false;
        protected Envelope[] envelopes = new Envelope[EnvelopeType.Count];
        protected int[] envelopeIdx = new int[EnvelopeType.Count];
        protected int[] envelopeValues = new int[EnvelopeType.Count];
        protected bool noteTriggered = false;
        protected bool noteReleased = false;
        protected bool resetPhase = false;
        protected bool forceInstrumentReload = false;
        protected ushort[] noteTable = null;
        protected bool palPlayback = false;
        protected bool instrumentPlayer = false;
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

        public ChannelState(IPlayerInterface play, int apu, int type, int tuning, bool pal = false, int numN163Channels = 1)
        {
            player = play;
            apuIdx = apu;
            channelType = type;
            palPlayback = pal;
            instrumentPlayer = apuIdx == NesApu.APU_INSTRUMENT; // HACK : Pass a flag for this.
            maximumPeriod = NesApu.GetPitchLimitForChannelType(channelType);
            noteTable = NesApu.GetNoteTableForChannelType(channelType, pal, numN163Channels, tuning);
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

                // Clear any pending release.
                if (newNote.IsMusicalOrStop)
                {
                    releaseCounter = 0;
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
                    else
                        durationCounter = 0;
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

            if (newNote.HasVibrato)
            {
                if (newNote.VibratoDepth != 0 && newNote.VibratoDepth != 0)
                {
                    envelopes[EnvelopeType.Pitch] = Envelope.CreateVibratoEnvelope(newNote.VibratoSpeed, newNote.VibratoDepth);
                    envelopeIdx[EnvelopeType.Pitch] = 0;
                    envelopeValues[EnvelopeType.Pitch] = 0;
                    pitchEnvelopeOverride = true;
                }
                else
                {
                    envelopes[EnvelopeType.Pitch] = null;
                    pitchEnvelopeOverride = false;
                    resetInstrumentOnNextAttack = true;
                }
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
                        {
                            envelopeIdx[j] = envelopes[j].Release;
                            if (j == EnvelopeType.WaveformRepeat)
                                envelopeValues[j] = envelopes[EnvelopeType.WaveformRepeat].Values[envelopeIdx[j]] + 1;
                        }
                    }
                }

                noteReleased = newNote.IsRelease;
                newNote.Value = note.Value;
                note = newNote;
            }
            else if (newNote.IsMusical)
            {
                var instrumentChanged = note.Instrument != newNote.Instrument || forceInstrumentReload;
                var arpeggioChanged   = note.Arpeggio   != newNote.Arpeggio;

                var noteHasAttack = newNote.HasAttack || !Channel.CanDisableAttack(channelType, note.Instrument, newNote.Instrument);

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
                else if (noteHasAttack && arpeggioEnvelopeOverride)
                {
                    envelopeIdx[EnvelopeType.Arpeggio] = 0;
                    envelopeValues[EnvelopeType.Arpeggio] = 0;
                }

                if (noteHasAttack)
                {
                    instrumentChanged |= resetInstrumentOnNextAttack;

                    if (instrumentChanged)
                    {
                        for (var j = 0; j < EnvelopeType.Count; j++)
                        {
                            if ((j != EnvelopeType.Pitch || !pitchEnvelopeOverride) &&
                                (j != EnvelopeType.Arpeggio || !arpeggioEnvelopeOverride))
                            {
                                envelopes[j] = note.Instrument == null ? null : note.Instrument.Envelopes[j];
                            }
                        }
                    }

                    for (var j = 0; j < EnvelopeType.Count; j++)
                    {
                        envelopeIdx[j] = 0;
                    }

                    envelopeValues[EnvelopeType.DutyCycle] = dutyCycle;
                    envelopeValues[EnvelopeType.Pitch] = 0; // In case we use relative envelopes.

                    // See comment un UpdateEnvelope about why we handle these differently.
                    if (envelopes[EnvelopeType.WaveformRepeat] != null)
                        envelopeValues[EnvelopeType.WaveformRepeat] = envelopes[EnvelopeType.WaveformRepeat].Values[0] + 1;

                    noteTriggered = true;
                    resetInstrumentOnNextAttack = false;
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

            if (note.HasPhaseReset)
            {
                resetPhase = true;
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
                    if (envelopes[j] == null ||
                        (!instrumentPlayer && envelopes[j].IsEmpty(j)) ||
                        ( instrumentPlayer && envelopes[j].Length == 0))
                    {
                        if (j != EnvelopeType.DutyCycle)
                            envelopeValues[j] = Envelope.GetEnvelopeDefaultValue(j);
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

                    var reloadValue = false;
                    var canJumpToLoop = true;

                    // We handle the repeat envelopes a bit different since they are not
                    // stored in their final form. Here the envelope stores the number of
                    // frame to repeat this waveform. In the NSF driver it will simply
                    // contain the wave index and be handled like any other envelopes.
                    if (j == EnvelopeType.WaveformRepeat)
                    {
                        Debug.Assert(envelopeValues[j] > 0);

                        if (--envelopeValues[j] == 0)
                        {
                            idx++;
                            reloadValue = true;
                        }
                        else
                        {
                            canJumpToLoop = false;
                        }
                    }
                    else
                    {
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
                    }

                    if (env.Release >= 0 && idx == env.Release && canJumpToLoop)
                        envelopeIdx[j] = env.Loop;
                    else if (idx >= env.Length)
                        envelopeIdx[j] = env.Loop >= 0 && env.Release < 0 ? env.Loop : (env.Relative ? -1 : env.Length - 1);
                    else
                        envelopeIdx[j] = idx;

                    if (reloadValue)
                        envelopeValues[j] = envelopes[j].Values[envelopeIdx[j]];
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

        protected void SkipCycles(int cycles)
        {
            NesApu.SkipCycles(apuIdx, cycles);
        }

        protected void WriteRegister(int reg, int data, int skipCycles = 4, int metadata = 0)
        {
            Debug.Assert(data == data % 256);
            NesApu.WriteRegister(apuIdx, reg, data);
            player.NotifyRegisterWrite(apuIdx, reg, data, metadata);
            
            // Internally, NesSndEmu skips 4 cycles. Here we have the option to add more.
            skipCycles -= 4;
            if (skipCycles > 0)
                NesApu.SkipCycles(apuIdx, skipCycles);
        }

        protected bool IsSeeking
        {
            get { return NesApu.IsSeeking(apuIdx) != 0; }
        }
        
        public virtual int GetEnvelopeFrame(int envIdx)
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

        public virtual void AddRegisterValuesExtraData(NesApu.NesRegisterValues registerValues)
        {
            var instrument = note.Instrument;
            registerValues.InstrumentIds[channelType] = instrument != null ? instrument.Id : 0;
            registerValues.InstrumentColors[channelType] = instrument != null ? instrument.Color : Color.Invisible;
        }

        // TODO : We should not reference settings from here.
        protected int GetPeriod()
        {
            var noteVal = note.Value + envelopeValues[EnvelopeType.Arpeggio];

            var outsideNoteTable = false;

            if (Settings.ClampPeriods)
            {
                noteVal = Utils.Clamp(noteVal, 0, noteTable.Length - 1);
            }
            else if (noteVal >= noteTable.Length || noteVal < 0)
            {
                outsideNoteTable = true;
            }

            var pitch = (note.FinePitch + envelopeValues[EnvelopeType.Pitch]) << pitchShift;
            var slide = slideShift < 0 ? (slidePitch >> -slideShift) : (slidePitch << slideShift); // Remove the fraction part.

            // When clamping is disabled, we return a random period if we are outside
            // the note table. This will simulate the hardware reading random code/data 
            // and should help debug these issues.
            var notePeriod = outsideNoteTable ? (noteVal & 0xff) * 251 + 293 : noteTable[noteVal];
            var finalPeriod = notePeriod + pitch + slide;

            return Settings.ClampPeriods ? Utils.Clamp(finalPeriod, 0, maximumPeriod) : (finalPeriod & maximumPeriod);
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
            noteReleased = false;
            NesApu.SkipCycles(apuIdx, CyclesBetweenChannels);
        }

        protected virtual void ResetPhase()
        {

        }

        public virtual void PostUpdate()
        {
            NesApu.SkipCycles(apuIdx, resetPhase ? 4 : 5); // lsr/bcc
            if (resetPhase)
            {
                ResetPhase();
                resetPhase = false;
            }
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
