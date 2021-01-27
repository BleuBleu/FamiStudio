using System;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;

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
        private byte fdsMasterVolume = 0;
        private byte fdsWavPreset = Envelope.WavePresetSine;
        private byte fdsModPreset = Envelope.WavePresetFlat;
        private ushort fdsModSpeed;
        private byte fdsModDepth;
        private byte fdsModDelay;

        // N163
        private byte n163WavePreset = Envelope.WavePresetSine;
        private byte n163WaveSize = 16;
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

        public struct Vrc7PatchInfo
        {
            public string name;
            public byte[] data;
        };

        public static readonly Vrc7PatchInfo[] Vrc7Patches = new[]
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
            new Vrc7PatchInfo() { name = "Synthesizer",  data = new byte[] { 0x61, 0x63, 0x0c, 0x00, 0x94, 0xC0, 0x33, 0xf6 } }, // Synthesizer 
            new Vrc7PatchInfo() { name = "Chorus",       data = new byte[] { 0x21, 0x72, 0x0d, 0x00, 0xc1, 0xd5, 0x56, 0x06 } }  // Chorus      
        };

        public Instrument()
        {
        }

        public Instrument(int id, int expansion, string name)
        {
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
                UpdateFdsWaveEnvelope();
                UpdateFdsModulationEnvelope();
            }
            else if (expansion == Project.ExpansionN163)
            {
                UpdateN163WaveEnvelope();
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
                return true;
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

            return false;
        }

        public byte FdsWavePreset
        {
            get { return fdsWavPreset; }
            set
            {
                fdsWavPreset = value;
                UpdateFdsWaveEnvelope();
            }
        }

        public byte FdsModPreset
        {
            get { return fdsModPreset; }
            set
            {
                fdsModPreset = value;
                UpdateFdsModulationEnvelope();
            }
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
                Debug.Assert((value & 0x03) == 0);
                n163WaveSize = (byte)Utils.Clamp(value       & 0xfc, 4, 248);
                n163WavePos  = (byte)Utils.Clamp(n163WavePos & 0xfc, 0, 248 - n163WaveSize);
                UpdateN163WaveEnvelope();
            }
        }

        public byte N163WavePos
        {
            get { return n163WavePos; }
            set
            {
                Debug.Assert((value & 0x03) == 0);
                n163WavePos  = (byte)Utils.Clamp(value        & 0xfc, 0, 248);
                n163WaveSize = (byte)Utils.Clamp(n163WaveSize & 0xfc, 4, 248 - n163WavePos);
            }
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

        public ushort FdsModSpeed     { get => fdsModSpeed;     set => fdsModSpeed = value; }
        public byte   FdsModDepth     { get => fdsModDepth;     set => fdsModDepth = value; }
        public byte   FdsModDelay     { get => fdsModDelay;     set => fdsModDelay = value; } 
        public byte   FdsMasterVolume { get => fdsMasterVolume; set => fdsMasterVolume = value; }

        public void UpdateFdsWaveEnvelope()
        {
            envelopes[Envelope.FdsWaveform].SetFromPreset(Envelope.FdsWaveform, fdsWavPreset);
        }

        public void UpdateFdsModulationEnvelope()
        {
            envelopes[Envelope.FdsModulation].SetFromPreset(Envelope.FdsModulation, fdsModPreset);
        }

        public void UpdateN163WaveEnvelope()
        {
            envelopes[Envelope.N163Waveform].MaxLength = n163WaveSize;
            envelopes[Envelope.N163Waveform].SetFromPreset(Envelope.N163Waveform, n163WavePreset);
        }

        public static string GetVrc7PatchName(int idx)
        {
            return Vrc7Patches[idx].name;
        }

        public uint ComputeCRC(uint crc = 0)
        {
            var serializer = new ProjectCrcBuffer(crc);
            SerializeState(serializer);
            return serializer.CRC;
        }

        public void Validate(Project project, Dictionary<int, object> idMap)
        {
#if DEBUG
            project.ValidateId(id);

            if (idMap.TryGetValue(id, out var foundObj))
                Debug.Assert(foundObj == this);
            else
                idMap.Add(id, this);

            Debug.Assert(project.GetInstrument(id) == this);

            for (int i = 0; i < Envelope.Count; i++)
            {
                bool envelopeExists = envelopes[i] != null;
                bool envelopeShouldExists = IsEnvelopeActive(i);
                Debug.Assert(envelopeExists == envelopeShouldExists);
            }
#endif
        }

        public void ChangeId(int newId)
        {
            id = newId;
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref id, true);
            buffer.Serialize(ref name);
            buffer.Serialize(ref color);

            // At version 5 (FamiStudio 2.0.0) we added duty cycle envelopes.
            var dutyCycle = 0;
            if (buffer.Version < 5)
                buffer.Serialize(ref dutyCycle);

            // At version 4 (FamiStudio 1.4.0) we added basic expansion audio (VRC6).
            if (buffer.Version >= 4)
            {
                buffer.Serialize(ref expansion);

                // At version 5 (FamiStudio 2.0.0) we added duty cycle envelopes.
                if (buffer.Version >= 5)
                {
                    switch (expansion)
                    {
                        case Project.ExpansionFds:
                            buffer.Serialize(ref fdsMasterVolume);
                            buffer.Serialize(ref fdsWavPreset);
                            buffer.Serialize(ref fdsModPreset);
                            buffer.Serialize(ref fdsModSpeed);
                            buffer.Serialize(ref fdsModDepth); 
                            buffer.Serialize(ref fdsModDelay);
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

            if (buffer.Version < 5)
            {
                envelopes[Envelope.DutyCycle] = new Envelope(Envelope.DutyCycle);
                if (dutyCycle != 0)
                {
                    envelopes[Envelope.DutyCycle].Length = 1;
                    envelopes[Envelope.DutyCycle].Values[0] = (sbyte)dutyCycle;
                }
            }
        }
    }
}
