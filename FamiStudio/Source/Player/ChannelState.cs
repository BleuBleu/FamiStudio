using System;
using System.Diagnostics;

namespace FamiStudio
{
    public abstract class ChannelState
    {
        protected int apuIdx;
        protected int channelType;
        protected Note note;
        protected int[] envelopeIdx = new int[Envelope.Max];
        protected int[] envelopeValues = new int[Envelope.Max];
        protected int duty;
        protected ushort[] noteTable = null;
        protected short maximumPeriod = NesApu.MaximumPeriod11Bit;
        protected int slideStep = 0;
        private   int slidePitch = 0;

        public ChannelState(int apu, int type)
        {
            apuIdx = apu;
            channelType = type;
            noteTable = NesApu.GetNoteTableForChannelType(channelType, false);
            note.Value = Note.NoteStop;
            note.Volume = Note.VolumeMax;
        }

        public void ProcessEffects(Song song, ref int patternIdx, ref int noteIdx, ref int speed, bool allowJump = true)
        {
            var pattern = song.Channels[channelType].PatternInstances[patternIdx];

            if (pattern == null)
                return;

            var tmpNote = pattern.Notes[noteIdx];

            switch (tmpNote.Effect)
            {
                case Note.EffectJump:
                    if (!NesApu.NesApuIsSeeking(apuIdx) && allowJump)
                    {
                        patternIdx = tmpNote.EffectParam;
                        noteIdx = 0;
                    }
                    break;
                case Note.EffectSkip:
                    if (!NesApu.NesApuIsSeeking(apuIdx))
                    {
                        patternIdx++;
                        noteIdx = tmpNote.EffectParam;
                    }
                    break;
                case Note.EffectSpeed:
                    speed = tmpNote.EffectParam;
                    break;
            }
        }

        public void Advance(Song song, int patternIdx, int noteIdx)
        {
            var channel = song.Channels[channelType];
            var pattern = channel.PatternInstances[patternIdx];

            if (pattern == null)
                return;

            var tmpNote = pattern.Notes[noteIdx];
            if (tmpNote.IsValid)
            {
                slideStep = 0;

                if (tmpNote.IsSlideNote)
                {
                    var noteTable = NesApu.GetNoteTableForChannelType(channel.Type, false);

                    if (channel.ComputeSlideNoteParams(patternIdx, noteIdx, noteTable, out slidePitch, out slideStep, out _))
                        tmpNote.Value = (byte)tmpNote.SlideNoteTarget;
                }

                PlayNote(tmpNote);
            }
            else if (tmpNote.HasVolume)
            {
                note.Volume = tmpNote.Volume;
            }
        }

        public void PlayNote(Note newNote)
        {
            if (!newNote.HasVolume)
                newNote.Volume = note.Volume;

            if (newNote.IsRelease)
            {
                if (note.Instrument != null)
                {
                    for (int j = 0; j < Envelope.Max; j++)
                    {
                        if (note.Instrument.Envelopes[j].Release >= 0)
                            envelopeIdx[j] = note.Instrument.Envelopes[j].Release;
                    }
                }
            }
            else
            {
                bool instrumentChanged = note.Instrument != newNote.Instrument;

                note = newNote;

                if (newNote.Instrument != null)
                    duty = newNote.Instrument.DutyCycle;

                if (instrumentChanged || note.HasAttack)
                {
                    for (int j = 0; j < Envelope.Max; j++)
                        envelopeIdx[j] = 0;

                    envelopeValues[Envelope.Pitch] = 0; // In case we use relative envelopes.
                }
            }
        }

        public void UpdateEnvelopes()
        {
            var instrument = note.Instrument;
            if (instrument != null)
            {
                for (int j = 0; j < Envelope.Max; j++)
                {
                    if (instrument.Envelopes[j] == null ||
                        instrument.Envelopes[j].Length <= 0)
                    {
                        if (j == Envelope.Volume)
                            envelopeValues[j] = 15;
                        else
                            envelopeValues[j] = 0;
                        continue;
                    }

                    var env = instrument.Envelopes[j];
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
                        envelopeValues[j] += instrument.Envelopes[j].Values[idx];
                    }
                    else
                    {
                        envelopeValues[j] = instrument.Envelopes[j].Values[idx];
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
        }

        protected int MultiplyVolumes(int v0, int v1)
        {
            return (int)Math.Ceiling((v0 / 15.0f) * (v1 / 15.0f) * 15.0f);
        }

        public abstract void UpdateAPU();
    };
}
