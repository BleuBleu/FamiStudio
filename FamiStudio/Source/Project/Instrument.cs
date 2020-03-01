using System;
using System.Linq;
using System.Drawing;
using System.Diagnostics;

namespace FamiStudio
{
    public class Instrument
    {
        private int id;
        private string name;
        private int expansion = Project.ExpansionNone;
        private Envelope[] envelopes = new Envelope[Envelope.Count];
        private Color color;

        // FDS
        private byte fdsWavPreset = Envelope.WavePresetSine;
        private byte fdsModPreset = Envelope.WavePresetFlat;
        private ushort fdsModRate;
        private byte fdsModDepth;
        //private byte fdsModDelay;

        // N163
        private byte n163WavePreset = Envelope.WavePresetSine;
        private byte n163WaveSize = 32;
        private byte n163WavePos = 0;

        // VRC7
        private byte vrc7Patch = 1;
        private byte[] vrc7PatchRegs = new byte[8];

        public int Id => id;
        public string Name { get => name; set => name = value; }
        public Color Color { get => color; set => color = value; }
        public int ExpansionType => expansion;
        public bool IsExpansionInstrument => expansion != Project.ExpansionNone;
        public Envelope[] Envelopes => envelopes;
        public int DutyCycleRange => expansion == Project.ExpansionNone ? 4 : 8;
        public int NumActiveEnvelopes => envelopes.Count(e => e != null);
        public bool HasReleaseEnvelope => envelopes[Envelope.Volume] != null && envelopes[Envelope.Volume].Release >= 0;
        public byte[] Vrc7PatchRegs => vrc7PatchRegs;

        public Instrument()
        {
            // For serialization.
            Debug.Assert(RealTimeParamsInfo.Length == ParamMax);
        }

        public Instrument(int id, int expansion, string name)
        {
            Debug.Assert(RealTimeParamsInfo.Length == ParamMax);

            this.id = id;
            this.expansion = expansion;
            this.name = name;
            this.color = ThemeBase.RandomCustomColor();
            for (int i = 0; i < Envelope.Count; i++)
            {
                if (IsEnvelopeActive(i))
                    envelopes[i] = new Envelope(i);
            }

            if (expansion == Project.ExpansionFds)
            {
                envelopes[Envelope.FdsWaveform].SetFromPreset(Envelope.FdsWaveform, fdsWavPreset);
                envelopes[Envelope.FdsModulation].SetFromPreset(Envelope.FdsModulation, fdsModPreset);
            }
            else if (expansion == Project.ExpansionN163)
            {
                envelopes[Envelope.N163Waveform].SetFromPreset(Envelope.N163Waveform, n163WavePreset);
            }
            else if (expansion == Project.ExpansionVrc7)
            {
                vrc7Patch = 1;
                Array.Copy(Vrc7Patches[1].data, vrc7PatchRegs, 8);
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
                       expansion == Project.ExpansionVrc6 ||
                       expansion == Project.ExpansionMmc5;
            }
            else if (envelopeType == Envelope.FdsWaveform ||
                     envelopeType == Envelope.FdsModulation)
            {
                return expansion == Project.ExpansionFds;
            }
            else if (envelopeType == Envelope.N163Waveform)
            {
                return expansion == Project.ExpansionN163;
            }
            else if (envelopeType == Envelope.DutyCycle)
            {
                return expansion == Project.ExpansionNone ||
                       expansion == Project.ExpansionVrc6;
            }

            return false;
        }

        public byte N163WavePreset
        {
            get { return n163WavePreset; }
            set
            {
                n163WavePreset = value;
                UpdateN163WaveEnvelope();
            }
        }

        public byte N163WaveSize
        {
            get { return n163WaveSize; }
            set
            {
                n163WaveSize = (byte)Utils.NextPowerOfTwo(value);
                n163WavePos  = (byte)(n163WavePos & ~(n163WaveSize - 1));
                UpdateN163WaveEnvelope();
            }
        }

        public byte N163WavePos
        {
            get { return n163WavePos; }
            set { n163WavePos = (byte)value; }
        }

        public byte Vrc7Patch
        {
            get { return vrc7Patch; }
            set
            {
                vrc7Patch = value;
                if (vrc7Patch != 0)
                    Array.Copy(Vrc7Patches[vrc7Patch].data, vrc7PatchRegs, 8);
            }
        }

        public ushort FdsModRate  { get => fdsModRate;  set => fdsModRate  = value; }
        public byte   FdsModDepth { get => fdsModDepth; set => fdsModDepth = value; }
        //public byte FdsModDelay => fdsModDelay;

        private void UpdateN163WaveEnvelope()
        {
            envelopes[Envelope.N163Waveform].MaxLength = n163WaveSize;
            envelopes[Envelope.N163Waveform].SetFromPreset(Envelope.N163Waveform, n163WavePreset);
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
            {
                buffer.Serialize(ref expansion);

                // At version 5 (FamiStudio 1.5.0) we added duty cycle envelopes.
                if (buffer.Version >= 5)
                {
                    switch (expansion)
                    {
                        case Project.ExpansionFds:
                            buffer.Serialize(ref fdsWavPreset);
                            buffer.Serialize(ref fdsModPreset);
                            buffer.Serialize(ref fdsModRate);
                            buffer.Serialize(ref fdsModDepth); 
                            //buffer.Serialize(ref fdsModDelay);
                            break;
                        case Project.ExpansionN163:
                            buffer.Serialize(ref n163WavePreset);
                            buffer.Serialize(ref n163WaveSize);
                            buffer.Serialize(ref n163WavePos);
                            break;
                        case Project.ExpansionVrc7:
                            buffer.Serialize(ref vrc7Patch);
                            buffer.Serialize(ref vrc7PatchRegs[0]);
                            buffer.Serialize(ref vrc7PatchRegs[1]);
                            buffer.Serialize(ref vrc7PatchRegs[2]);
                            buffer.Serialize(ref vrc7PatchRegs[3]);
                            buffer.Serialize(ref vrc7PatchRegs[4]);
                            buffer.Serialize(ref vrc7PatchRegs[5]);
                            buffer.Serialize(ref vrc7PatchRegs[6]);
                            buffer.Serialize(ref vrc7PatchRegs[7]);
                            break;
                    }
                }
            }

            byte envelopeMask = 0;
            if (buffer.IsWriting)
            {
                for (int i = 0; i < Envelope.Count; i++)
                {
                    if (envelopes[i] != null)
                        envelopeMask = (byte)(envelopeMask | (1 << i));
                }
            }
            buffer.Serialize(ref envelopeMask);

            for (int i = 0; i < Envelope.Count; i++)
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

        //
        // TODO: Move all this to a different class. Just a way to expose parameters and generate a nice UI.
        //

        struct RealTimeParamInfo
        {
            public string name;
            public int min;
            public int max;
            public bool list;
        };

        // FDS
        public const int ParamFdsWavePreset = 0;
        public const int ParamFdsModulationPreset = 1;
        public const int ParamFdsModulationSpeed = 2;
        public const int ParamFdsModulationDepth = 3;
        //public const int ParamFdsModulationDelay = 4;

        // N163
        public const int ParamN163WavePreset = 5;
        public const int ParamN163WaveSize = 6;
        public const int ParamN163WavePos = 7;

        // VRC7
        public const int ParamVrc7Patch = 8;
        public const int ParamVrc7CarTremolo = 9;
        public const int ParamVrc7CarVibrato = 10;
        public const int ParamVrc7CarSustained = 11;
        public const int ParamVrc7CarWaveRectified = 12;
        public const int ParamVrc7CarKeyScaling = 13;
        public const int ParamVrc7CarKeyScalingLevel = 14;
        public const int ParamVrc7CarFreqMultiplier = 15;
        public const int ParamVrc7CarAttack = 16;
        public const int ParamVrc7CarDecay = 17;
        public const int ParamVrc7CarSustain = 18;
        public const int ParamVrc7CarRelease = 19;
        public const int ParamVrc7ModTremolo = 20;
        public const int ParamVrc7ModVibrato = 21;
        public const int ParamVrc7ModSustained = 22;
        public const int ParamVrc7ModWaveRectified = 23;
        public const int ParamVrc7ModKeyScaling = 24;
        public const int ParamVrc7ModKeyScalingLevel = 25;
        public const int ParamVrc7ModFreqMultiplier = 26;
        public const int ParamVrc7ModAttack = 27;
        public const int ParamVrc7ModDecay = 28;
        public const int ParamVrc7ModSustain = 29;
        public const int ParamVrc7ModRelease = 30;
        public const int ParamVrc7ModLevel = 31;
        public const int ParamVrc7Feedback = 32;

        public const int ParamMax = 33;

        static readonly RealTimeParamInfo[] RealTimeParamsInfo =
        {
            // FDS
            new RealTimeParamInfo() { name = "Wave Preset",                max = Envelope.WavePresetMax - 1, list = true },
            new RealTimeParamInfo() { name = "Mod Preset",                 max = Envelope.WavePresetMax - 1, list = true },
            new RealTimeParamInfo() { name = "Mod Speed",                  max = 4095 },
            new RealTimeParamInfo() { name = "Mod Depth",                  max = 63 },
            new RealTimeParamInfo() { name = "Mod Delay",                  max = 255 },

            // N163
            new RealTimeParamInfo() { name = "Wave Preset",                max = Envelope.WavePresetMax - 1, list = true },
            new RealTimeParamInfo() { name = "Wave Size",                  min = 4, max = 32, list = true },
            new RealTimeParamInfo() { name = "Wave Position",              max = 124, list = true },

            // VRC7
            new RealTimeParamInfo() { name = "Patch",                      max = 15, list = true },
            new RealTimeParamInfo() { name = "Carrier Tremolo",            max = 1 },
            new RealTimeParamInfo() { name = "Carrier Vibrato",            max = 1 },
            new RealTimeParamInfo() { name = "Carrier Sustained",          max = 1 },
            new RealTimeParamInfo() { name = "Carrier Wave Rectified",     max = 1 },
            new RealTimeParamInfo() { name = "Carrier KeyScaling",         max = 1 },
            new RealTimeParamInfo() { name = "Carrier KeyScaling Level",   max = 3 },
            new RealTimeParamInfo() { name = "Carrier FreqMultiplier",     max = 15 },
            new RealTimeParamInfo() { name = "Carrier Attack",             max = 15 },
            new RealTimeParamInfo() { name = "Carrier Decay",              max = 15 },
            new RealTimeParamInfo() { name = "Carrier Sustain",            max = 15 },
            new RealTimeParamInfo() { name = "Carrier Release",            max = 15 },
            new RealTimeParamInfo() { name = "Modulator Tremolo",          max = 1 },
            new RealTimeParamInfo() { name = "Modulator Vibrato",          max = 1 },
            new RealTimeParamInfo() { name = "Modulator Sustained",        max = 1 },
            new RealTimeParamInfo() { name = "Modulator Wave Rectified",   max = 1 },
            new RealTimeParamInfo() { name = "Modulator KeyScaling",       max = 1 },
            new RealTimeParamInfo() { name = "Modulator KeyScaling Level", max = 3 },
            new RealTimeParamInfo() { name = "Modulator FreqMultiplier",   max = 15 },
            new RealTimeParamInfo() { name = "Modulator Attack",           max = 15 },
            new RealTimeParamInfo() { name = "Modulator Decay",            max = 15 },
            new RealTimeParamInfo() { name = "Modulator Sustain",          max = 15 },
            new RealTimeParamInfo() { name = "Modulator Release",          max = 15 },
            new RealTimeParamInfo() { name = "Modulator Level",            max = 63 },
            new RealTimeParamInfo() { name = "Feedback",                   max = 7 }
        };

        static readonly int[] FdsParams = new[]
        {
            ParamFdsWavePreset, ParamFdsModulationPreset, ParamFdsModulationSpeed, ParamFdsModulationDepth //, ParamFdsModulationDelay
        };

        static readonly int[] N163Params = new[]
        {
            ParamN163WavePreset, ParamN163WaveSize, ParamN163WavePos
        };

        static readonly int[] Vrc7Params = new[]
        {
            ParamVrc7Patch,
            ParamVrc7CarTremolo, ParamVrc7CarVibrato, ParamVrc7CarSustained, ParamVrc7CarWaveRectified, ParamVrc7CarKeyScaling,
            ParamVrc7CarKeyScalingLevel, ParamVrc7CarFreqMultiplier, ParamVrc7CarAttack, ParamVrc7CarDecay, ParamVrc7CarSustain, ParamVrc7CarRelease,
            ParamVrc7ModTremolo, ParamVrc7ModVibrato, ParamVrc7ModSustained, ParamVrc7ModWaveRectified, ParamVrc7ModKeyScaling,
            ParamVrc7ModKeyScalingLevel, ParamVrc7ModFreqMultiplier, ParamVrc7ModAttack, ParamVrc7ModDecay, ParamVrc7ModSustain, ParamVrc7ModRelease,
            ParamVrc7ModLevel, ParamVrc7Feedback
        };

        public const int Vrc7PresetCustom = 0;
        public const int Vrc7PresetBell = 1;
        public const int Vrc7PresetGuitar = 2;
        public const int Vrc7PresetPiano = 3;
        public const int Vrc7PresetFlute = 4;
        public const int Vrc7PresetClarinet = 5;
        public const int Vrc7PresetRattlingBell = 6;
        public const int Vrc7PresetTrumpet = 7;
        public const int Vrc7PresetReedOrgan = 8;
        public const int Vrc7PresetSoftBell = 9;
        public const int Vrc7PresetXylophone = 10;
        public const int Vrc7PresetVibraphone = 11;
        public const int Vrc7PresetBrass = 12;
        public const int Vrc7PresetBassGuitar = 13;
        public const int Vrc7PresetSynthetizer = 14;
        public const int Vrc7PresetChorus = 15;

        struct Vrc7PatchInfo
        {
            public string name;
            public byte[] data;
        };

        static readonly Vrc7PatchInfo[] Vrc7Patches = new[]
        {
            new Vrc7PatchInfo() { name = "Custom",       data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } }, // Custom      
            new Vrc7PatchInfo() { name = "Bell",         data = new byte[] { 0x03, 0x21, 0x05, 0x06, 0xe8, 0x81, 0x42, 0x27 } }, // Bell        
            new Vrc7PatchInfo() { name = "Guitar",       data = new byte[] { 0x13, 0x41, 0x14, 0x0d, 0xd8, 0xf6, 0x23, 0x12 } }, // Guitar      
            new Vrc7PatchInfo() { name = "Piano",        data = new byte[] { 0x11, 0x11, 0x08, 0x08, 0xfa, 0xb2, 0x20, 0x12 } }, // Piano       
            new Vrc7PatchInfo() { name = "Flute",        data = new byte[] { 0x31, 0x61, 0x0c, 0x07, 0xa8, 0x64, 0x61, 0x27 } }, // Flute       
            new Vrc7PatchInfo() { name = "Clarinet",     data = new byte[] { 0x32, 0x21, 0x1e, 0x06, 0xe1, 0x76, 0x01, 0x28 } }, // Clarinet    
            new Vrc7PatchInfo() { name = "RattlingBell", data = new byte[] { 0x02, 0x01, 0x06, 0x00, 0xa3, 0xe2, 0xf4, 0xf4 } }, // RattlingBell
            new Vrc7PatchInfo() { name = "Trumpet",      data = new byte[] { 0x21, 0x61, 0x1d, 0x07, 0x82, 0x81, 0x11, 0x07 } }, // Trumpet     
            new Vrc7PatchInfo() { name = "ReedOrgan",    data = new byte[] { 0x23, 0x21, 0x22, 0x17, 0xa2, 0x72, 0x01, 0x17 } }, // ReedOrgan   
            new Vrc7PatchInfo() { name = "SoftBell",     data = new byte[] { 0x35, 0x11, 0x25, 0x00, 0x40, 0x73, 0x72, 0x01 } }, // SoftBell    
            new Vrc7PatchInfo() { name = "Xylophone",    data = new byte[] { 0xb5, 0x01, 0x0f, 0x0F, 0xa8, 0xa5, 0x51, 0x02 } }, // Xylophone   
            new Vrc7PatchInfo() { name = "Vibraphone",   data = new byte[] { 0x17, 0xc1, 0x24, 0x07, 0xf8, 0xf8, 0x22, 0x12 } }, // Vibraphone  
            new Vrc7PatchInfo() { name = "Brass",        data = new byte[] { 0x71, 0x23, 0x11, 0x06, 0x65, 0x74, 0x18, 0x16 } }, // Brass       
            new Vrc7PatchInfo() { name = "BassGuitar",   data = new byte[] { 0x01, 0x02, 0xd3, 0x05, 0xc9, 0x95, 0x03, 0x02 } }, // BassGuitar  
            new Vrc7PatchInfo() { name = "Synthetizer",  data = new byte[] { 0x61, 0x63, 0x0c, 0x00, 0x94, 0xC0, 0x33, 0xf6 } }, // Synthetizer 
            new Vrc7PatchInfo() { name = "Chorus",       data = new byte[] { 0x21, 0x72, 0x0d, 0x00, 0xc1, 0xd5, 0x56, 0x06 } }  // Chorus      
        };
        
        public static string GetVrc7PatchName(int idx)
        {
            return Vrc7Patches[idx].name;
        }

        public static string GetRealTimeParamName(int param)
        {
            return RealTimeParamsInfo[param].name;
        }

        public static bool IsRealTimeParamList(int param)
        {
            return RealTimeParamsInfo[param].list;
        }

        public int[] GetRealTimeParams()
        {
            switch (expansion)
            {
                case Project.ExpansionFds   : return FdsParams;
                case Project.ExpansionN163  : return N163Params;
                case Project.ExpansionVrc7  : return Vrc7Params;
            }

            return null;
        }

        public static int GetRealTimeParamMinValue(int param)
        {
            return RealTimeParamsInfo[param].min;
        }

        public static int GetRealTimeParamMaxValue(int param)
        {
            return RealTimeParamsInfo[param].max;
        }

        public int GetRealTimeParamValue(int param)
        {
            switch (param)
            {
                // FDS
                case ParamFdsWavePreset          : return fdsWavPreset;
                case ParamFdsModulationPreset    : return fdsModPreset;
                case ParamFdsModulationSpeed     : return fdsModRate; 
                case ParamFdsModulationDepth     : return fdsModDepth;
                //case ParamFdsModulationDelay     : return fdsModDelay;
                                                 
                // N163                         
                case ParamN163WavePreset         : return n163WavePreset;
                case ParamN163WaveSize           : return n163WaveSize;
                case ParamN163WavePos            : return n163WavePos;
                                                 
                // VRC7                          
                case ParamVrc7Patch              : return vrc7Patch;
                case ParamVrc7CarTremolo         : return (vrc7PatchRegs[1] & 0x80) >> 7; 
                case ParamVrc7CarVibrato         : return (vrc7PatchRegs[1] & 0x40) >> 6;
                case ParamVrc7CarSustained       : return (vrc7PatchRegs[1] & 0x20) >> 5;
                case ParamVrc7CarKeyScaling      : return (vrc7PatchRegs[1] & 0x10) >> 4;
                case ParamVrc7CarFreqMultiplier  : return (vrc7PatchRegs[1] & 0x0f) >> 0;
                case ParamVrc7CarKeyScalingLevel : return (vrc7PatchRegs[3] & 0xc0) >> 6;
                case ParamVrc7CarWaveRectified   : return (vrc7PatchRegs[3] & 0x10) >> 4;
                case ParamVrc7CarAttack          : return (vrc7PatchRegs[5] & 0xf0) >> 4;
                case ParamVrc7CarDecay           : return (vrc7PatchRegs[5] & 0x0f) >> 0;
                case ParamVrc7CarSustain         : return (vrc7PatchRegs[7] & 0xf0) >> 4;
                case ParamVrc7CarRelease         : return (vrc7PatchRegs[7] & 0x0f) >> 0;
                case ParamVrc7ModTremolo         : return (vrc7PatchRegs[0] & 0x80) >> 7;
                case ParamVrc7ModVibrato         : return (vrc7PatchRegs[0] & 0x40) >> 6;
                case ParamVrc7ModSustained       : return (vrc7PatchRegs[0] & 0x20) >> 5;
                case ParamVrc7ModKeyScaling      : return (vrc7PatchRegs[0] & 0x10) >> 4; 
                case ParamVrc7ModFreqMultiplier  : return (vrc7PatchRegs[0] & 0x0f) >> 0;
                case ParamVrc7ModKeyScalingLevel : return (vrc7PatchRegs[2] & 0xc0) >> 6;
                case ParamVrc7ModWaveRectified   : return (vrc7PatchRegs[3] & 0x08) >> 3;
                case ParamVrc7ModAttack          : return (vrc7PatchRegs[4] & 0xf0) >> 4;
                case ParamVrc7ModDecay           : return (vrc7PatchRegs[4] & 0x0f) >> 0;
                case ParamVrc7ModSustain         : return (vrc7PatchRegs[6] & 0xf0) >> 4;
                case ParamVrc7ModRelease         : return (vrc7PatchRegs[6] & 0x0f) >> 0;
                case ParamVrc7ModLevel           : return (vrc7PatchRegs[2] & 0x3f) >> 0;
                case ParamVrc7Feedback           : return (vrc7PatchRegs[3] & 0x07) >> 0;
            }

            return 0;
        }

        public void SetRealTimeParamValue(int param, int val)
        {
            var min = GetRealTimeParamMinValue(param);
            var max = GetRealTimeParamMaxValue(param);

            val = Utils.Clamp(val, min, max);

            // Changing any value makes us go to custom.
            if (param >= ParamVrc7CarTremolo && param <= ParamVrc7ModRelease)
                vrc7Patch = 0;

            switch (param)
            {
                // FDS
                case ParamFdsWavePreset:
                    fdsWavPreset = (byte)val; // MATTT : Create property.
                    envelopes[Envelope.FdsWaveform].SetFromPreset(Envelope.FdsWaveform, val);
                    break;
                case ParamFdsModulationPreset:
                    fdsModPreset = (byte)val; // MATTT : Create property.
                    envelopes[Envelope.FdsModulation].SetFromPreset(Envelope.FdsModulation, val);
                    break;
                case ParamFdsModulationSpeed: fdsModRate  = (ushort)val; break;
                case ParamFdsModulationDepth: fdsModDepth = (byte)val; break;
                //case ParamFdsModulationDelay: fdsModDelay = (byte)val; break;

                // N163
                case ParamN163WavePreset: N163WavePreset = (byte)val; break;
                case ParamN163WavePos:    N163WavePos    = (byte)(val & ~(n163WaveSize - 1)); break;
                case ParamN163WaveSize:   N163WaveSize   = (byte)val; break;

                // VRC7
                case ParamVrc7Patch:
                    Vrc7Patch = (byte)val;
                    break;

                case ParamVrc7CarTremolo:         vrc7PatchRegs[1] = (byte)((vrc7PatchRegs[1] & (~0x80)) | ((val << 7) & 0x80)); break;
                case ParamVrc7CarVibrato:         vrc7PatchRegs[1] = (byte)((vrc7PatchRegs[1] & (~0x40)) | ((val << 6) & 0x40)); break;
                case ParamVrc7CarSustained:       vrc7PatchRegs[1] = (byte)((vrc7PatchRegs[1] & (~0x20)) | ((val << 5) & 0x20)); break;
                case ParamVrc7CarKeyScaling:      vrc7PatchRegs[1] = (byte)((vrc7PatchRegs[1] & (~0x10)) | ((val << 4) & 0x10)); break;
                case ParamVrc7CarFreqMultiplier:  vrc7PatchRegs[1] = (byte)((vrc7PatchRegs[1] & (~0x0f)) | ((val << 0) & 0x0f)); break;
                case ParamVrc7CarKeyScalingLevel: vrc7PatchRegs[3] = (byte)((vrc7PatchRegs[3] & (~0xc0)) | ((val << 6) & 0xc0)); break;
                case ParamVrc7CarWaveRectified:   vrc7PatchRegs[3] = (byte)((vrc7PatchRegs[3] & (~0x10)) | ((val << 4) & 0x10)); break;
                case ParamVrc7CarAttack:          vrc7PatchRegs[5] = (byte)((vrc7PatchRegs[5] & (~0xf0)) | ((val << 4) & 0xf0)); break;
                case ParamVrc7CarDecay:           vrc7PatchRegs[5] = (byte)((vrc7PatchRegs[5] & (~0x0f)) | ((val << 0) & 0x0f)); break;
                case ParamVrc7CarSustain:         vrc7PatchRegs[7] = (byte)((vrc7PatchRegs[7] & (~0xf0)) | ((val << 4) & 0xf0)); break;
                case ParamVrc7CarRelease:         vrc7PatchRegs[7] = (byte)((vrc7PatchRegs[7] & (~0x0f)) | ((val << 0) & 0x0f)); break;
                case ParamVrc7ModTremolo:         vrc7PatchRegs[0] = (byte)((vrc7PatchRegs[0] & (~0x80)) | ((val << 7) & 0x80)); break;
                case ParamVrc7ModVibrato:         vrc7PatchRegs[0] = (byte)((vrc7PatchRegs[0] & (~0x40)) | ((val << 6) & 0x40)); break;
                case ParamVrc7ModSustained:       vrc7PatchRegs[0] = (byte)((vrc7PatchRegs[0] & (~0x20)) | ((val << 5) & 0x20)); break;
                case ParamVrc7ModKeyScaling:      vrc7PatchRegs[0] = (byte)((vrc7PatchRegs[0] & (~0x10)) | ((val << 4) & 0x10)); break;
                case ParamVrc7ModFreqMultiplier:  vrc7PatchRegs[0] = (byte)((vrc7PatchRegs[0] & (~0x0f)) | ((val << 0) & 0x0f)); break;
                case ParamVrc7ModKeyScalingLevel: vrc7PatchRegs[2] = (byte)((vrc7PatchRegs[2] & (~0xc0)) | ((val << 6) & 0xc0)); break;
                case ParamVrc7ModWaveRectified:   vrc7PatchRegs[3] = (byte)((vrc7PatchRegs[3] & (~0x08)) | ((val << 3) & 0x08)); break;
                case ParamVrc7ModAttack:          vrc7PatchRegs[4] = (byte)((vrc7PatchRegs[4] & (~0xf0)) | ((val << 4) & 0xf0)); break;
                case ParamVrc7ModDecay:           vrc7PatchRegs[4] = (byte)((vrc7PatchRegs[4] & (~0x0f)) | ((val << 0) & 0x0f)); break;
                case ParamVrc7ModSustain:         vrc7PatchRegs[6] = (byte)((vrc7PatchRegs[6] & (~0xf0)) | ((val << 4) & 0xf0)); break;
                case ParamVrc7ModRelease:         vrc7PatchRegs[6] = (byte)((vrc7PatchRegs[6] & (~0x0f)) | ((val << 0) & 0x0f)); break;
                case ParamVrc7ModLevel:           vrc7PatchRegs[2] = (byte)((vrc7PatchRegs[2] & (~0x3f)) | ((val << 0) & 0x3f)); break;
                case ParamVrc7Feedback:           vrc7PatchRegs[3] = (byte)((vrc7PatchRegs[3] & (~0x07)) | ((val << 0) & 0x07)); break;

                default : Debug.Assert(false); return;
            }
        }

        public string GetRealTimeParamString(int param)
        {
            switch (param)
            {
                // FDS
                case ParamFdsWavePreset:       return Envelope.PresetNames[fdsWavPreset];
                case ParamFdsModulationPreset: return Envelope.PresetNames[fdsModPreset];

                // N163
                case ParamN163WavePreset:     return Envelope.PresetNames[n163WavePreset];

                // VRC7
                case ParamVrc7Patch:           return Vrc7Patches[vrc7Patch].name;
            }

            return GetRealTimeParamValue(param).ToString();
        }

        public int GetRealTimeParamPrevValue(int param, int val)
        {
            switch (param)
            {
                case ParamN163WaveSize:
                    return val <= 4 ? 4 : val /= 2;
                case ParamN163WavePos:
                    return val == 0 ? 0 : val - n163WaveSize;
                default:
                    return Utils.Clamp(val - 1, GetRealTimeParamMinValue(param), GetRealTimeParamMaxValue(param));
            }
        }

        public int GetRealTimeParamNextValue(int param, int val)
        {
            switch (param)
            {
                case ParamN163WaveSize:
                    return val >= 32 ? 32 : val *= 2;
                case ParamN163WavePos:
                    return val >= 128 - n163WaveSize ? 128 - n163WaveSize : val + n163WaveSize;
                default:
                    return Utils.Clamp(val + 1, GetRealTimeParamMinValue(param), GetRealTimeParamMaxValue(param));
            }
        }
    }
}
