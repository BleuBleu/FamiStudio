using System;
using System.Diagnostics;

namespace FamiStudio
{
    public abstract class ChannelState
    {
        protected int apuIdx;
        protected int channelType;
        protected Note note;
        protected bool pitchEnvelopeOverride = false;
        protected Envelope[] envelopes = new Envelope[Envelope.Max];
        protected int[] envelopeIdx = new int[Envelope.Max];
        protected int[] envelopeValues = new int[Envelope.Max];
        protected int duty;
        protected bool palMode;
        protected ushort[] noteTable = null;
        protected short maximumPeriod = NesApu.MaximumPeriod11Bit;
        protected int slideStep = 0;
        private   int slidePitch = 0;

        public ChannelState(int apu, int type, bool pal)
        {
            apuIdx = apu;
            channelType = type;
            palMode = pal;
            noteTable = NesApu.GetNoteTableForChannelType(channelType, pal);
            note.Value = Note.NoteStop;
            note.Volume = Note.VolumeMax;
        }

        public void ProcessEffects(Song song, int patternIdx, int noteIdx, ref int jumpPatternIdx, ref int jumpNoteIdx, ref int speed, bool allowJump = true)
        {
            var pattern = song.GetChannelByType(channelType).PatternInstances[patternIdx];

            if (pattern == null)
                return;

            var tmpNote = pattern.Notes[noteIdx];

            if (tmpNote.HasJump && !IsSeeking && allowJump)
            {
                jumpPatternIdx = tmpNote.Jump;
                jumpNoteIdx = 0;
            }

            if (tmpNote.HasSkip)
            {
                jumpPatternIdx = patternIdx + 1;
                jumpNoteIdx = tmpNote.Skip;
            }

            if (tmpNote.HasSpeed)
            {
                speed = tmpNote.Speed;
            }

            if (tmpNote.HasVibrato)
            {
                if (tmpNote.VibratoDepth != 0 && tmpNote.VibratoDepth != 0)
                {
                    envelopes[Envelope.Pitch] = Envelope.CreateVibratoEnvelope(tmpNote.VibratoSpeed, tmpNote.VibratoDepth);
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
        }

        public void Advance(Song song, int patternIdx, int noteIdx)
        {
            var channel = song.GetChannelByType(channelType);
            var pattern = channel.PatternInstances[patternIdx];

            if (pattern == null)
                return;

            var newNote = pattern.Notes[noteIdx];
            if (newNote.IsValid)
            {
                slideStep = 0;

                if (newNote.IsSlideNote)
                {
                    var noteTable = NesApu.GetNoteTableForChannelType(channel.Type, palMode);

                    if (channel.ComputeSlideNoteParams(patternIdx, noteIdx, noteTable, out slidePitch, out slideStep, out _))
                        newNote.Value = (byte)newNote.SlideNoteTarget;
                }

                PlayNote(newNote);
            }
            else if (newNote.HasVolume)
            {
                note.Volume = newNote.Volume;
            }
            else if (newNote.HasFinePitch)
            {
                note.Pitch = newNote.Pitch;
            }
        }

        public void PlayNote(Note newNote, bool forceInstrumentChange = false)
        {
            if (!newNote.HasVolume)
                newNote.Volume = note.Volume;
            if (!newNote.HasFinePitch)
                newNote.Pitch = 0;

            if (newNote.IsRelease)
            {
                if (note.Instrument != null)
                {
                    for (int j = 0; j < Envelope.Max; j++)
                    {
                        if (envelopes[j] != null && envelopes[j].Release >= 0)
                            envelopeIdx[j] = envelopes[j].Release;
                    }
                }
            }
            else
            {
                bool instrumentChanged = note.Instrument != newNote.Instrument || forceInstrumentChange;

                note = newNote;

                if (newNote.Instrument != null)
                    duty = newNote.Instrument.DutyCycle;

                if (instrumentChanged || note.HasAttack)
                {
                    for (int j = 0; j < Envelope.Max; j++)
                    {
                        if (j != Envelope.Pitch || !pitchEnvelopeOverride)
                            envelopes[j] = note.Instrument == null ? null : note.Instrument.Envelopes[j];
                        envelopeIdx[j] = 0;
                    }

                    envelopeValues[Envelope.Pitch] = 0; // In case we use relative envelopes.
                }

                if (instrumentChanged)
                {
                    LoadInstrument(note.Instrument);
                }
            }
        }

        public void UpdateEnvelopes()
        {
            if (note.Instrument != null)
            {
                for (int j = 0; j < Envelope.Max; j++)
                {
                    if (envelopes[j] == null ||
                        envelopes[j].Length <= 0)
                    {
                        if (j == Envelope.Volume)
                            envelopeValues[j] = 15;
                        else
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

        protected void WriteRegister(int reg, int data)
        {
            NesApu.WriteRegister(apuIdx, reg, data);
        }

        protected bool IsSeeking
        {
            get { return NesApu.IsSeeking(apuIdx) != 0; }
        }

        public int GetSlidePitch()
        {
            return slidePitch >> 1; // Remove the fraction part.
        }

        public int GetEnvelopeFrame(int envIdx)
        {
            return envelopeIdx[envIdx];
        }

        public void ClearNote()
        {
            note.Instrument = null;
            for (int i = 0; i < Envelope.Max; i++)
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
            return Math.Min(maximumPeriod, noteTable[noteVal] + note.FinePitch + GetSlidePitch() + envelopeValues[Envelope.Pitch]);
        }

        protected int GetVolume()
        {
            return MultiplyVolumes(note.Volume, envelopeValues[Envelope.Volume]);
        }

        public abstract void UpdateAPU();
    };
}
