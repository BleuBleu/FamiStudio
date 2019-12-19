using System;
using System.Drawing;

namespace FamiStudio
{
    public class Instrument
    {
        // Instrument types.
        public const int Apu   = 0;
        public const int Vrc6  = 1;
        public const int Count = 2;

        private int id;
        private string name;
        private int type = Apu;
        private Envelope[] envelopes = new Envelope[Envelope.Max];
        private Color color;
        private int dutyCycle;

        public int Id => id;
        public string Name { get => name; set => name = value; }
        public int Type { get => type; }
        public Color Color { get => color; set => color = value; }
        public Envelope[] Envelopes => envelopes;
        public int DutyCycle { get => dutyCycle; set => dutyCycle = value; }

        public Instrument()
        {
            // For serialization.
        }

        public Instrument(int id, string name)
        {
            this.id = id;
            this.name = name;
            this.color = ThemeBase.RandomCustomColor();
            for (int i = 0; i < Envelope.Max; i++)
                envelopes[i] = new Envelope();
        }

        public bool HasReleaseEnvelope
        {
            get
            {
                for (int i = 0; i < Envelope.Max; i++)
                    if (envelopes[i].Release >= 0) return true;
                return false;
            }
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref id, true);
            buffer.Serialize(ref name);
            buffer.Serialize(ref color);
            buffer.Serialize(ref dutyCycle);

            byte envelopeMask = 0;
            if (buffer.IsWriting)
            {
                for (int i = 0; i < Envelope.Max; i++)
                {
                    if (envelopes[i] != null) envelopeMask = (byte)(envelopeMask | (1 << i));
                }
            }
            buffer.Serialize(ref envelopeMask);

            for (int i = 0; i < Envelope.Max; i++)
            {
                if ((envelopeMask & (1 << i)) != 0)
                {
                    if (buffer.IsReading)
                        envelopes[i] = new Envelope();
                    envelopes[i].SerializeState(buffer);
                }
                else
                {
                    envelopes[i] = null;
                }
            }
        }
    }
}
