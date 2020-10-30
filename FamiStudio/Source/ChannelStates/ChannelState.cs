using System;
using System.Diagnostics;

namespace FamiStudio
{
    public abstract class ChannelState
    {
        protected int apuIdx;
        protected int channelType;
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
            maximumPeriod = NesApu.GetPitchLimitForChannelType(channelType);
            noteTable = NesApu.GetNoteTableForChannelType(channelType, pal, numN163Channels);
            note.Value = Note.NoteStop;
            note.Volume = Note.VolumeMax;
            note.FinePitch = 0;
            Channel.GetShiftsForType(type, numN163Channels, out pitchShift, out slideShift);
        }

        public void Advance(Song song, int patternIdx, int noteIdx, ref int famitrackerSpeed, int famitrackerBaseTempo)
        {
            var channel = song.GetChannelByType(channelType);
            var pattern = channel.PatternInstances[patternIdx];

            if (pattern == null)
                return;

            if (pattern.Notes.TryGetValue(noteIdx, out var newNote))
            { 
                newNote = newNote.Clone();

                if (newNote.IsValid)
                {
                    if (!newNote.IsRelease)
                        slideStep = 0;

                    if (newNote.IsSlideNote)
                    {
                        if (channel.ComputeSlideNoteParams(newNote, patternIdx, noteIdx, famitrackerSpeed, famitrackerBaseTempo, noteTable, out slidePitch, out slideStep, out _))
                        {
                            noteValueBeforeSlide = newNote.Value;
                            newNote.Value = (byte)newNote.SlideNoteTarget;
                        }
                    }

                    if (!newNote.HasVolume      && note.HasVolume)      { newNote.Volume      = note.Volume;      }
                    if (!newNote.HasFinePitch   && note.HasFinePitch)   { newNote.FinePitch   = note.FinePitch;   }
                    if (!newNote.HasFdsModDepth && note.HasFdsModDepth) { newNote.FdsModDepth = note.FdsModDepth; }
                    if (!newNote.HasFdsModSpeed && note.HasFdsModSpeed) { newNote.FdsModSpeed = note.FdsModSpeed; }
                    if (newNote.Instrument == null && note.Instrument != null) { newNote.Instrument = note.Instrument; }
                    if (newNote.Arpeggio   == null && note.Arpeggio   != null && newNote.IsValid && !newNote.IsMusical) { newNote.Arpeggio   = note.Arpeggio;   }

                    PlayNote(newNote);
                }
                else
                { 
                    if (newNote.HasVolume)      { note.Volume      = newNote.Volume;      }
                    if (newNote.HasFinePitch)   { note.FinePitch   = newNote.FinePitch;   }
                    if (newNote.HasFdsModDepth) { note.FdsModDepth = newNote.FdsModDepth; }
                    if (newNote.HasFdsModSpeed) { note.FdsModSpeed = newNote.FdsModSpeed; }
                    if (newNote.Instrument != null) { note.Instrument = newNote.Instrument; }
                }

                if (newNote.HasSpeed)
                {
                    famitrackerSpeed = newNote.Speed;
                }

                if (newNote.HasVibrato)
                {
                    if (newNote.VibratoDepth != 0 && newNote.VibratoDepth != 0)
                    {
                        envelopes[Envelope.Pitch] = Envelope.CreateVibratoEnvelope(newNote.VibratoSpeed, newNote.VibratoDepth);
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

                if (newNote.HasDutyCycle)
                {
                    dutyCycle = newNote.DutyCycle;
                    envelopeValues[Envelope.DutyCycle] = dutyCycle;
                }
            }
        }

        public void PlayNote(Note newNote)
        {
            if (!newNote.HasFinePitch)
                 newNote.FinePitch = 0;

            if (newNote.IsRelease)
            {
                // Channels with custom release code will do their own thing.
                if (customRelease)
                {
                    note = newNote;
                }
                else
                {
                    if (note.Instrument != null)
                    {
                        for (int j = 0; j < Envelope.Count; j++)
                        {
                            if (envelopes[j] != null && envelopes[j].Release >= 0)
                                envelopeIdx[j] = envelopes[j].Release;
                        }
                    }
                }
            }
            else
            {
                bool instrumentChanged = note.Instrument != newNote.Instrument;
                bool arpeggioChanged   = note.Arpeggio   != newNote.Arpeggio;

                note = newNote;

                if (note.IsMusical)
                {
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
                }

                if (instrumentChanged || note.HasAttack && !note.IsStop)
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
        }

        public void Update()
        {
            UpdateEnvelopes();
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
