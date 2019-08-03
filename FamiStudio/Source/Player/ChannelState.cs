namespace FamiStudio
{
    public abstract class ChannelState
    {
        protected int apuIdx;
        protected int channelIdx;
        protected bool seeking = false;
        protected bool newNote = false;
        protected Note note;
        protected int[] envelopeIdx = new int[Envelope.Max];
        protected int[] envelopeValues = new int[Envelope.Max];
        protected int duty;
        protected int[] shadowRegisters = new int[21];

        public ChannelState(int apu, int idx)
        {
            apuIdx = apu;
            channelIdx = idx;
            note.Value = Note.NoteStop;
        }

        public void ProcessEffects(Song song, ref int patternIdx, ref int noteIdx, ref int speed)
        {
            var pattern = song.Channels[channelIdx].PatternInstances[patternIdx];

            if (pattern == null)
                return;

            var tmpNote = pattern.Notes[noteIdx];

            switch (tmpNote.Effect)
            {
                case Note.EffectJump:
                    if (!seeking)
                    {
                        patternIdx = tmpNote.EffectParam;
                        noteIdx = 0;
                    }
                    break;
                case Note.EffectSkip:
                    if (!seeking)
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
            var pattern = song.Channels[channelIdx].PatternInstances[patternIdx];

            if (pattern == null)
                return;

            var tmpNote = pattern.Notes[noteIdx];
            if (tmpNote.IsValid)
            {
                PlayNote(tmpNote);
            }
        }

        public void PlayNote(Note note)
        {
            this.note = note;
            this.newNote = true;

            if (note.Instrument != null)
                duty = note.Instrument.DutyCycle;

            for (int j = 0; j < Envelope.Max; j++)
                envelopeIdx[j] = 0;
        }

        public void UpdateEnvelopes()
        {
            var instrument = note.Instrument;
            if (instrument == null)
                return;

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

                var idx = envelopeIdx[j];
                var env = instrument.Envelopes[j];

                envelopeValues[j] = instrument.Envelopes[j].Values[idx];

                if (++idx >= env.Length)
                    envelopeIdx[j] = env.Loop >= 0 ? env.Loop : env.Length - 1;
                else
                    envelopeIdx[j] = idx;
            }
        }

        public int GetEnvelopeFrame(int envIdx)
        {
            return envelopeIdx[envIdx];
        }

        public void StartSeeking()
        {
            seeking = true;
            for (int i = 0; i < shadowRegisters.Length; i++)
                shadowRegisters[i] = -1;
        }

        public void StopSeeking()
        {
            seeking = false;
            for (int i = 0; i < shadowRegisters.Length; i++)
            {
                if (shadowRegisters[i] >= 0)
                    NesApu.NesApuWriteRegister(apuIdx, 0x4000 + i, shadowRegisters[i]);
            }
        }

        public void ClearNote()
        {
            note.Instrument = null;
        }

        protected void WriteApuRegister(int register, int data)
        {
            if (seeking)
            {
                int idx = register - 0x4000;
                // Not caching DPCM register for now.
                if (idx < shadowRegisters.Length) 
                {
                    shadowRegisters[idx] = data;
                }
            }
            else
            {
                NesApu.NesApuWriteRegister(apuIdx, register, data);
            }
        }

        public abstract void UpdateAPU();
    };
}
