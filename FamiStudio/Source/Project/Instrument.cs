using System;
using System.Linq;
using System.Drawing;

namespace FamiStudio
{
    public class Instrument
    {
        private int id;
        private string name;
        private int expansion = Project.ExpansionNone;
        private Envelope[] envelopes = new Envelope[Envelope.Max];
        private Color color;

        // FDS
        private byte   fdsWavPreset;
        private byte   fdsModPreset;
        private ushort fdsModRate;
        private byte   fdsModDepth;
        private byte   fdsModDelay;

        // Namco
        private byte   namcoWavePreset;
        private byte   namcoWaveSize;

        // VRC7
        struct Vrc7Unit
        {
            public byte tremolo;
            public byte vibrato;
            public byte sustained;
            public byte waveShape;
            public byte keyScaling;
            public byte freqMultiplier;
            public byte attack;
            public byte decay;
            public byte sustain;
            public byte release;
        };

        private Vrc7Unit vrc7CarrierParams;
        private Vrc7Unit vrc7ModulatorParams;

        public int Id => id;
        public string Name { get => name; set => name = value; }
        public Color Color { get => color; set => color = value; }
        public int ExpansionType => expansion; 
        public bool IsExpansionInstrument => expansion != Project.ExpansionNone;
        public Envelope[] Envelopes => envelopes;
        public int DutyCycleRange => expansion == Project.ExpansionNone ? 4 : 8;
        public int NumActiveEnvelopes => envelopes.Count(e => e != null);
        public bool HasReleaseEnvelope => envelopes[Envelope.Volume] != null && envelopes[Envelope.Volume].Release >= 0;

        public Instrument()
        {
            // For serialization.
        }

        public Instrument(int id, int expansion, string name)
        {
            this.id = id;
            this.expansion = expansion;
            this.name = name;
            this.color = ThemeBase.RandomCustomColor();
            for (int i = 0; i < Envelope.Max; i++)
            {
                if (IsEnvelopeActive(i))
                    envelopes[i] = new Envelope(i);
            }
        }

        public bool IsEnvelopeActive(int envelopeType)
        {
            if (envelopeType == Envelope.Volume ||
                envelopeType == Envelope.Pitch  ||
                envelopeType == Envelope.Arpeggio)
            {
                return expansion != Project.ExpansionVrc7;
            }
            else if (envelopeType == Envelope.DutyCycle)
            {
                return expansion == Project.ExpansionNone ||
                       expansion == Project.ExpansionVrc6;
            }
            else if (envelopeType == Envelope.FdsWaveform ||
                     envelopeType == Envelope.FdsModulation)
            {
                return expansion == Project.ExpansionFds;
            }
            else if (envelopeType == Envelope.NamcoWaveform)
            {
                return expansion == Project.ExpansionNamco;
            }
            else if (envelopeType == Envelope.DutyCycle)
            {
                return expansion == Project.ExpansionNone || 
                       expansion == Project.ExpansionVrc6;
            }

            return false;
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref id, true);
            buffer.Serialize(ref name);
            buffer.Serialize(ref color);

            // At version 5 (FamiStudio 1.5.0) we added duty cycle envelopes.
            var dutyCycle = 0;
            if (buffer.Version < 5)
                buffer.Serialize(ref dutyCycle);

            // At version 4 (FamiStudio 1.4.0) we added basic expansion audio (VRC6).
            if (buffer.Version >= 4)
                buffer.Serialize(ref expansion);

            byte envelopeMask = 0;
            if (buffer.IsWriting)
            {
                for (int i = 0; i < Envelope.Max; i++)
                {
                    if (envelopes[i] != null)
                        envelopeMask = (byte)(envelopeMask | (1 << i));
                }
            }
            buffer.Serialize(ref envelopeMask);

            for (int i = 0; i < Envelope.Max; i++)
            {
                if ((envelopeMask & (1 << i)) != 0)
                {
                    if (buffer.IsReading)
                        envelopes[i] = new Envelope(i);
                    envelopes[i].SerializeState(buffer);
                }
                else
                {
                    envelopes[i] = null;
                }
            }

            if (buffer.Version < 5 && dutyCycle != 0)
            {
                envelopes[Envelope.DutyCycle] = new Envelope(Envelope.DutyCycle);
                envelopes[Envelope.DutyCycle].Length = 1;
                envelopes[Envelope.DutyCycle].Values[0] = (sbyte)dutyCycle;
            }
        }
    }
}
