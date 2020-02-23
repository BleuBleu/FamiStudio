using System;
using System.Linq;
using System.Drawing;
using System.Diagnostics;

namespace FamiStudio
{
    public class Instrument
    {
        // FDS
        public const int ParamFdsWavePreset = 0;
        public const int ParamFdsModulationPreset = 1;
        public const int ParamFdsModulationSpeed = 2;
        public const int ParamFdsModulationDepth = 3;
        public const int ParamFdsModulationDelay = 4;

        // Namco
        public const int ParamNamcoWaveSize = 5;
        public const int ParamNamcoWavePreset = 6;

        // VRC7
        public const int ParamVrc7CarTremolo = 7;
        public const int ParamVrc7CarVibrato = 8;
        public const int ParamVrc7CarSustained = 9;
        public const int ParamVrc7CarWaveRectified = 10;
        public const int ParamVrc7CarKeyScaling = 11;
        public const int ParamVrc7CarFreqMultiplier = 12;
        public const int ParamVrc7CarAttack = 13;
        public const int ParamVrc7CarDecay = 14;
        public const int ParamVrc7CarSustain = 15;
        public const int ParamVrc7CarRelease = 16;
        public const int ParamVrc7ModTremolo = 17;
        public const int ParamVrc7ModVibrato = 18;
        public const int ParamVrc7ModSustained = 19;
        public const int ParamVrc7ModWaveRectified = 20;
        public const int ParamVrc7ModKeyScaling = 21;
        public const int ParamVrc7ModFreqMultiplier = 22;
        public const int ParamVrc7ModAttack = 23;
        public const int ParamVrc7ModDecay = 24;
        public const int ParamVrc7ModSustain = 25;
        public const int ParamVrc7ModRelease = 26;

        public const int ParamMax = 27;

        public static readonly string[] RealTimeParamNames =
        {
            // FDS
            "Wave Preset",
            "Mod Preset",
            "Mod Speed",
            "Mod Depth",

            // Namco
            "Wave Size",
            "Wave Preset",

            // VRC7
            "Carrier Tremolo",
            "Carrier Vibrato",
            "Carrier Sustained",
            "Carrier WaveShape",
            "Carrier KeyScaling",
            "Carrier FreqMultiplier",
            "Carrier Attack",
            "Carrier Decay",
            "Carrier Sustain",
            "Carrier Release",
            "Modulator Tremolo",
            "Modulator Vibrato",
            "Modulator Sustained",
            "Modulator WaveShape",
            "Modulator KeyScaling",
            "Modulator FreqMultiplier",
            "Modulator Attack",
            "Modulator Decay",
            "Modulator Sustain",
            "Modulator Release"
        };

        private int id;
        private string name;
        private int expansion = Project.ExpansionNone;
        private Envelope[] envelopes = new Envelope[Envelope.Max];
        private Color color;

        // FDS
        private byte   fdsWavPreset = Envelope.WavePresetSine;
        private byte   fdsModPreset = Envelope.WavePresetFlat;
        private ushort fdsModRate;
        private byte   fdsModDepth;
        private byte   fdsModDelay;

        // Namco
        private byte   namcoWavePreset = Envelope.WavePresetSine;
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

        static readonly int[] FdsParams   = new[] { ParamFdsWavePreset, ParamFdsModulationPreset, ParamFdsModulationSpeed, ParamFdsModulationDepth, ParamFdsModulationDelay };
        static readonly int[] NamcoParams = new[] { ParamNamcoWaveSize, ParamNamcoWavePreset };
        static readonly int[] Vrc7Params  = new[] { ParamVrc7CarTremolo, ParamVrc7CarVibrato, ParamVrc7CarSustained, ParamVrc7CarWaveRectified, ParamVrc7CarKeyScaling,
                                                    ParamVrc7CarFreqMultiplier, ParamVrc7CarAttack, ParamVrc7CarDecay, ParamVrc7CarSustain, ParamVrc7CarRelease,
                                                    ParamVrc7ModTremolo, ParamVrc7ModVibrato, ParamVrc7ModSustained, ParamVrc7ModWaveRectified, ParamVrc7ModKeyScaling,
                                                    ParamVrc7ModFreqMultiplier, ParamVrc7ModAttack, ParamVrc7ModDecay, ParamVrc7ModSustain, ParamVrc7ModRelease };

        public int[] GetRealTimeParams()
        {
            switch (expansion)
            {
                case Project.ExpansionFds   : return FdsParams;
                case Project.ExpansionNamco : return NamcoParams;
                case Project.ExpansionVrc7  : return Vrc7Params;
            }

            return null;
        }

        public int GetRealTimeParamMinValue(int param)
        {
            return 0;
        }

        public int GetRealTimeParamMaxValue(int param)
        {
            switch (param)
            {
                case ParamFdsWavePreset:
                case ParamFdsModulationPreset:
                    return Envelope.WavePresetMax - 1;
                case ParamFdsModulationSpeed:
                    return 4095;
                case ParamFdsModulationDepth:
                    return 63;
                case ParamFdsModulationDelay:
                    return 255;
                case ParamVrc7CarFreqMultiplier:
                case ParamVrc7CarAttack:
                case ParamVrc7CarDecay:
                case ParamVrc7CarSustain:
                case ParamVrc7CarRelease:
                case ParamVrc7ModFreqMultiplier:
                case ParamVrc7ModAttack:
                case ParamVrc7ModDecay:
                case ParamVrc7ModSustain:
                case ParamVrc7ModRelease:
                    return 16;
            }

            return 0;
        }

        public int GetRealTimeParamValue(int param)
        {
            switch (param)
            {
                case ParamFdsWavePreset       : return fdsWavPreset;
                case ParamFdsModulationPreset : return fdsModPreset;
                case ParamFdsModulationSpeed  : return fdsModRate; 
                case ParamFdsModulationDepth  : return fdsModDepth;
                case ParamFdsModulationDelay  : return fdsModDelay;
            }

            return 0;
        }

        public string GetRealTimeParamString(int param)
        {
            switch (param)
            {
                case ParamFdsWavePreset       : return Envelope.PresetNames[fdsWavPreset];
                case ParamFdsModulationPreset : return Envelope.PresetNames[fdsModPreset];
            }

            return GetRealTimeParamValue(param).ToString();
        }

        public void SetRealTimeParamValue(int param, int val)
        {
            var min = GetRealTimeParamMinValue(param);
            var max = GetRealTimeParamMaxValue(param);

            val = Utils.Clamp(val, min, max);

            switch (param)
            {
                case ParamFdsWavePreset :
                    fdsWavPreset = (byte)val;
                    envelopes[Envelope.FdsWaveform].SetFromPreset(Envelope.FdsWaveform, val);
                    break;
                case ParamFdsModulationPreset :
                    fdsModPreset = (byte)val;
                    envelopes[Envelope.FdsModulation].SetFromPreset(Envelope.FdsModulation, val);
                    break;
                case ParamFdsModulationSpeed : fdsModRate  = (ushort)val; break;
                case ParamFdsModulationDepth : fdsModDepth = (byte)val; break;
                case ParamFdsModulationDelay : fdsModDelay = (byte)val; break;
                default : Debug.Assert(false); return;
            }
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
