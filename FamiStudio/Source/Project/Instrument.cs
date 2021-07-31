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
        private int expansion = global::FamiStudio.ExpansionType.None;
        private Envelope[] envelopes = new Envelope[EnvelopeType.Count];
        private Color color;

        // FDS
        private byte fdsMasterVolume = FdsMasterVolumeType.Volume100;
        private byte fdsWavPreset = WavePresetType.Sine;
        private byte fdsModPreset = WavePresetType.Flat;
        private ushort fdsModSpeed;
        private byte fdsModDepth;
        private byte fdsModDelay;

        // N163
        private byte n163WavePreset = WavePresetType.Sine;
        private byte n163WaveSize = 16;
        private byte n163WavePos = 0;

        // VRC6
        private byte vrc6SawMasterVolume = Vrc6SawMasterVolumeType.Half; 

        // VRC7
        private byte vrc7Patch = Vrc7InstrumentPatch.Bell;
        private byte[] vrc7PatchRegs = new byte[8];

        public int Id => id;
        public string Name { get => name; set => name = value; }
        public string NameWithExpansion => Name + (expansion == ExpansionType.None ? "" : $" ({ExpansionType.ShortNames[expansion]})");
        public Color Color { get => color; set => color = value; }
        public int Expansion => expansion;
        public bool IsExpansionInstrument => expansion != ExpansionType.None;
        public Envelope[] Envelopes => envelopes;
        public int NumActiveEnvelopes => envelopes.Count(e => e != null);
        public bool HasReleaseEnvelope => envelopes[EnvelopeType.Volume] != null && envelopes[EnvelopeType.Volume].Release >= 0;
        public byte[] Vrc7PatchRegs => vrc7PatchRegs;

        public bool IsFdsInstrument  => expansion == ExpansionType.Fds;
        public bool IsVrc7Instrument => expansion == ExpansionType.Vrc7;
        public bool IsN163Instrument => expansion == ExpansionType.N163;

        public Instrument()
        {
        }

        public Instrument(int id, int expansion, string name)
        {
            this.id = id;
            this.expansion = expansion;
            this.name = name;
            this.color = ThemeBase.RandomCustomColor();
            for (int i = 0; i < EnvelopeType.Count; i++)
            {
                if (IsEnvelopeActive(i))
                    envelopes[i] = new Envelope(i);
            }

            if (expansion == global::FamiStudio.ExpansionType.Fds)
            {
                UpdateFdsWaveEnvelope();
                UpdateFdsModulationEnvelope();
            }
            else if (expansion == global::FamiStudio.ExpansionType.N163)
            {
                UpdateN163WaveEnvelope();
            }
            else if (expansion == global::FamiStudio.ExpansionType.Vrc7)
            {
                vrc7Patch = Vrc7InstrumentPatch.Bell;
                Array.Copy(Vrc7InstrumentPatch.Infos[Vrc7InstrumentPatch.Bell].data, vrc7PatchRegs, 8);
            }
        }

        public bool IsEnvelopeActive(int envelopeType)
        {
            if (envelopeType == EnvelopeType.Volume ||
                envelopeType == EnvelopeType.Pitch  ||
                envelopeType == EnvelopeType.Arpeggio)
            {
                return true;
            }
            else if (envelopeType == EnvelopeType.DutyCycle)
            {
                return expansion == global::FamiStudio.ExpansionType.None ||
                       expansion == global::FamiStudio.ExpansionType.Vrc6 ||
                       expansion == global::FamiStudio.ExpansionType.Mmc5;
            }
            else if (envelopeType == EnvelopeType.FdsWaveform ||
                     envelopeType == EnvelopeType.FdsModulation)
            {
                return expansion == global::FamiStudio.ExpansionType.Fds;
            }
            else if (envelopeType == EnvelopeType.N163Waveform)
            {
                return expansion == global::FamiStudio.ExpansionType.N163;
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
        
        public byte Vrc6SawMasterVolume
        {
            get { return vrc6SawMasterVolume; }
            set { vrc6SawMasterVolume = (byte)Utils.Clamp(value, 0, 2); }
        }

        public byte Vrc7Patch
        {
            get { return vrc7Patch; }
            set
            {
                vrc7Patch = value;
                if (vrc7Patch != 0)
                    Array.Copy(Vrc7InstrumentPatch.Infos[vrc7Patch].data, vrc7PatchRegs, 8);
            }
        }

        public ushort FdsModSpeed     { get => fdsModSpeed;     set => fdsModSpeed = value; }
        public byte   FdsModDepth     { get => fdsModDepth;     set => fdsModDepth = value; }
        public byte   FdsModDelay     { get => fdsModDelay;     set => fdsModDelay = value; } 
        public byte   FdsMasterVolume { get => fdsMasterVolume; set => fdsMasterVolume = value; }

        public void UpdateFdsWaveEnvelope()
        {
            envelopes[EnvelopeType.FdsWaveform].SetFromPreset(EnvelopeType.FdsWaveform, fdsWavPreset);
        }

        public void UpdateFdsModulationEnvelope()
        {
            envelopes[EnvelopeType.FdsModulation].SetFromPreset(EnvelopeType.FdsModulation, fdsModPreset);
        }

        public void UpdateN163WaveEnvelope()
        {
            envelopes[EnvelopeType.N163Waveform].MaxLength = n163WaveSize;
            envelopes[EnvelopeType.N163Waveform].SetFromPreset(EnvelopeType.N163Waveform, n163WavePreset);
        }

        public static string GetVrc7PatchName(int preset)
        {
            return Vrc7InstrumentPatch.Infos[preset].name;
        }

        public uint ComputeCRC(uint crc = 0)
        {
            var serializer = new ProjectCrcBuffer(crc);
            SerializeState(serializer);
            return serializer.CRC;
        }

        public void ValidateIntegrity(Project project, Dictionary<int, object> idMap)
        {
#if DEBUG
            project.ValidateId(id);

            if (idMap.TryGetValue(id, out var foundObj))
                Debug.Assert(foundObj == this);
            else
                idMap.Add(id, this);

            Debug.Assert(project.GetInstrument(id) == this);

            for (int i = 0; i < EnvelopeType.Count; i++)
            {
                bool envelopeExists = envelopes[i] != null;
                bool envelopeShouldExists = IsEnvelopeActive(i);
                Debug.Assert(envelopeExists == envelopeShouldExists);

                if (envelopeExists)
                    Debug.Assert(envelopes[i].ValuesInValidRange(this, i));
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

                // At version 5 (FamiStudio 2.0.0) we added a ton of expansions.
                if (buffer.Version >= 5)
                {
                    switch (expansion)
                    {
                        case global::FamiStudio.ExpansionType.Fds:
                            buffer.Serialize(ref fdsMasterVolume);
                            buffer.Serialize(ref fdsWavPreset);
                            buffer.Serialize(ref fdsModPreset);
                            buffer.Serialize(ref fdsModSpeed);
                            buffer.Serialize(ref fdsModDepth); 
                            buffer.Serialize(ref fdsModDelay);
                            break;
                        case global::FamiStudio.ExpansionType.N163:
                            buffer.Serialize(ref n163WavePreset);
                            buffer.Serialize(ref n163WaveSize);
                            buffer.Serialize(ref n163WavePos);
                            break;

                        case global::FamiStudio.ExpansionType.Vrc7:
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
                        case global::FamiStudio.ExpansionType.Vrc6:
                            // At version 10 (FamiStudio 3.0.0) we added a master volume to the VRC6 saw.
                            if (buffer.Version >= 10)
                                buffer.Serialize(ref vrc6SawMasterVolume);
                            else
                                vrc6SawMasterVolume = Vrc6SawMasterVolumeType.Full;
                            break;
                    }
                }
            }

            byte envelopeMask = 0;
            if (buffer.IsWriting)
            {
                for (int i = 0; i < EnvelopeType.Count; i++)
                {
                    if (envelopes[i] != null)
                        envelopeMask = (byte)(envelopeMask | (1 << i));
                }
            }
            buffer.Serialize(ref envelopeMask);

            for (int i = 0; i < EnvelopeType.Count; i++)
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
                envelopes[EnvelopeType.DutyCycle] = new Envelope(EnvelopeType.DutyCycle);
                if (dutyCycle != 0)
                {
                    envelopes[EnvelopeType.DutyCycle].Length = 1;
                    envelopes[EnvelopeType.DutyCycle].Values[0] = (sbyte)dutyCycle;
                }
            }

            // At FamiStudio 3.2.0, we realized that we had some FDS envelopes (likely imported from NSF)
            // with bad values.
            if (buffer.Version < 12 && IsFdsInstrument)
            {
                envelopes[EnvelopeType.FdsWaveform].ClampToValidRange(this, EnvelopeType.FdsWaveform);
            }
        }
    }

    public static class Vrc7InstrumentPatch
    {
        public const byte Custom       =  0;
        public const byte Bell         =  1;
        public const byte Guitar       =  2;
        public const byte Piano        =  3;
        public const byte Flute        =  4;
        public const byte Clarinet     =  5;
        public const byte RattlingBell =  6;
        public const byte Trumpet      =  7;
        public const byte ReedOrgan    =  8;
        public const byte SoftBell     =  9;
        public const byte Xylophone    = 10;
        public const byte Vibraphone   = 11;
        public const byte Brass        = 12;
        public const byte BassGuitar   = 13;
        public const byte Synthetizer  = 14;
        public const byte Chorus       = 15;

        public struct Vrc7PatchInfo
        {
            public string name;
            public byte[] data;
        };

        public static readonly Vrc7PatchInfo[] Infos = new[]
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
    }

    public static class FdsMasterVolumeType
    {
        public const int Volume100 = 0;
        public const int Volume66  = 1;
        public const int Volume50  = 2;
        public const int Volume40  = 3;

        public static readonly string[] Names =
        {
            "100%",
            "66%",
            "50%",
            "40%",
        };

        public static int GetValueForName(string str)
        {
            return Array.IndexOf(Names, str);
        }
    }

    public static class Vrc6SawMasterVolumeType
    {
        public const int Full = 0;
        public const int Half = 1;
        public const int Quarter = 2;

        public static readonly string[] Names =
        {
            "Full",
            "Half",
            "Quarter"
        };

        public static int GetValueForName(string str)
        {
            return Array.IndexOf(Names, str);
        }
    }
}
