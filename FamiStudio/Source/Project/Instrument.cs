using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace FamiStudio
{
    public class Instrument
    {
        private int id;
        private string name;
        private int expansion = ExpansionType.None;
        private Envelope[] envelopes = new Envelope[EnvelopeType.Count];
        private Color color;

        // FDS
        private byte fdsMasterVolume = FdsMasterVolumeType.Volume100;
        private byte fdsWavPreset = WavePresetType.Sine;
        private byte fdsModPreset = WavePresetType.Flat;
        private ushort fdsModSpeed;
        private byte fdsModDepth;
        private byte fdsModDelay;
        private byte fdsWaveCount = 1;

        // N163
        private byte n163WavePreset = WavePresetType.Sine;
        private byte n163WaveSize = 16;
        private byte n163WavePos = 0;
        private byte n163WaveCount = 1;

        // VRC6
        private byte vrc6SawMasterVolume = Vrc6SawMasterVolumeType.Half;

        // VRC7
        private byte vrc7Patch = Vrc7InstrumentPatch.Bell;
        private byte[] vrc7PatchRegs = new byte[8];

        // EPSM
        private byte epsmPatch = EpsmInstrumentPatch.Default;
        private byte[] epsmPatchRegs = new byte[31];

        public int Id => id;
        public string Name { get => name; set => name = value; }
        public string NameWithExpansion => Name + (expansion == ExpansionType.None ? "" : $" ({ExpansionType.ShortNames[expansion]})");
        public Color Color { get => color; set => color = value; }
        public int Expansion => expansion;
        public bool IsExpansionInstrument => expansion != ExpansionType.None;
        public Envelope[] Envelopes => envelopes;
        public byte[] Vrc7PatchRegs => vrc7PatchRegs;
        public byte[] EpsmPatchRegs => epsmPatchRegs;
        public bool CanRelease => 
            envelopes[EnvelopeType.Volume]         != null && envelopes[EnvelopeType.Volume].Release         >= 0 ||
            envelopes[EnvelopeType.WaveformRepeat] != null && envelopes[EnvelopeType.WaveformRepeat].Release >= 0 ||
            expansion == ExpansionType.Vrc7 || 
            expansion == ExpansionType.EPSM;

        public bool IsRegularInstrument => expansion == ExpansionType.None;
        public bool IsFdsInstrument     => expansion == ExpansionType.Fds;
        public bool IsVrc6Instrument    => expansion == ExpansionType.Vrc6;
        public bool IsVrc7Instrument    => expansion == ExpansionType.Vrc7;
        public bool IsN163Instrument    => expansion == ExpansionType.N163;
        public bool IsS5BInstrument     => expansion == ExpansionType.S5B;
        public bool IsEpsmInstrument    => expansion == ExpansionType.EPSM;

        public Instrument()
        {
        }

        public Instrument(int id, int expansion, string name)
        {
            this.id = id;
            this.expansion = expansion;
            this.name = name;
            this.color = Theme.RandomCustomColor();
            for (int i = 0; i < EnvelopeType.Count; i++)
            {
                if (IsEnvelopeActive(i))
                    envelopes[i] = new Envelope(i);
            }

            if (expansion == ExpansionType.Fds)
            {
                UpdateFdsWaveEnvelope();
                UpdateFdsModulationEnvelope();
            }
            else if (expansion == ExpansionType.N163)
            {
                UpdateN163WaveEnvelope();
            }
            else if (expansion == ExpansionType.Vrc7)
            {
                vrc7Patch = Vrc7InstrumentPatch.Bell;
                Array.Copy(Vrc7InstrumentPatch.Infos[Vrc7InstrumentPatch.Bell].data, vrc7PatchRegs, 8);
            }
            else if (expansion == ExpansionType.EPSM)
            {
                epsmPatch = EpsmInstrumentPatch.Default;
                Array.Copy(EpsmInstrumentPatch.Infos[EpsmInstrumentPatch.Default].data, epsmPatchRegs, 31);
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
                return expansion == ExpansionType.None ||
                       expansion == ExpansionType.Vrc6 ||
                       expansion == ExpansionType.Mmc5;
            }
            else if (envelopeType == EnvelopeType.FdsWaveform ||
                     envelopeType == EnvelopeType.FdsModulation)
            {
                return expansion == ExpansionType.Fds;
            }
            else if (envelopeType == EnvelopeType.N163Waveform)
            {
                return expansion == ExpansionType.N163;
            }
            else if (envelopeType == EnvelopeType.WaveformRepeat)
            {
                return expansion == ExpansionType.N163 ||
                       expansion == ExpansionType.Fds;
            }

            return false;
        }

        public bool IsEnvelopeVisible(int envelopeType)
        {
            return envelopeType != EnvelopeType.WaveformRepeat;
        }

        public bool IsEnvelopeEmpty(int envelopeType)
        {
            return envelopes[envelopeType].IsEmpty(envelopeType);
        }

        public bool EnvelopeHasRepeat(int envelopeType)
        {
            return envelopeType == EnvelopeType.N163Waveform || envelopeType == EnvelopeType.FdsWaveform;
        }

        public int NumVisibleEnvelopes
        {
            get 
            {
                var count = 0;
                for (int i = 0; i < EnvelopeType.Count; i++)
                {
                    if (envelopes[i] != null && IsEnvelopeVisible(i))
                        count++;
                }
                return count;
            }
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
                ClampN163WaveCount();
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
                ClampN163WaveCount();
            }
        }

        public byte N163WaveCount
        {
            get { return n163WaveCount; }
            set
            {
                n163WaveCount = (byte)Utils.Clamp(value, 1, N163MaxWaveCount);
                UpdateN163WaveEnvelope();
            }
        }

        public int N163MaxWaveCount
        {
            get
            {
                return Math.Min(64, envelopes[EnvelopeType.N163Waveform].Values.Length / n163WaveSize); 
            }
        }

        public byte FdsWaveCount
        {
            get { return fdsWaveCount; }
            set
            {
                fdsWaveCount = (byte)Utils.Clamp(value, 1, FdsMaxWaveCount);
                UpdateFdsWaveEnvelope();
            }
        }

        public int FdsMaxWaveCount
        {
            get
            {
                return Math.Min(64, envelopes[EnvelopeType.FdsWaveform].Values.Length / 64); 
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

        public byte EpsmPatch
        {
            get { return epsmPatch; }
            set
            {
                epsmPatch = value;
                if (epsmPatch != 0)
                    Array.Copy(EpsmInstrumentPatch.Infos[epsmPatch].data, epsmPatchRegs, 31);
            }
        }

        public ushort FdsModSpeed     { get => fdsModSpeed;     set => fdsModSpeed = value; }
        public byte   FdsModDepth     { get => fdsModDepth;     set => fdsModDepth = value; }
        public byte   FdsModDelay     { get => fdsModDelay;     set => fdsModDelay = value; } 
        public byte   FdsMasterVolume { get => fdsMasterVolume; set => fdsMasterVolume = value; }

        public void UpdateFdsWaveEnvelope()
        {
            var wavEnv = envelopes[EnvelopeType.FdsWaveform];
            var repEnv = envelopes[EnvelopeType.WaveformRepeat];

            wavEnv.ChunkLength = 64;
            wavEnv.Length = 64 * fdsWaveCount;
            wavEnv.MaxLength = 1024;
            repEnv.Length = fdsWaveCount;
            repEnv.SetLoopReleaseUnsafe(wavEnv.Loop >= 0 ? wavEnv.Loop / 64 : -1, wavEnv.Release >= 0 ? wavEnv.Release / 64 : -1);
            wavEnv.SetFromPreset(EnvelopeType.FdsWaveform, fdsWavPreset);
        }

        public void UpdateFdsModulationEnvelope()
        {
            envelopes[EnvelopeType.FdsModulation].SetFromPreset(EnvelopeType.FdsModulation, fdsModPreset);
        }

        public void UpdateN163WaveEnvelope()
        {
            var wavEnv = envelopes[EnvelopeType.N163Waveform];
            var repEnv = envelopes[EnvelopeType.WaveformRepeat];

            wavEnv.ChunkLength = n163WaveSize;
            wavEnv.Length = n163WaveSize * n163WaveCount;
			wavEnv.MaxLength = N163MaxWaveCount * n163WaveSize;
            repEnv.Length = n163WaveCount;
            repEnv.SetLoopReleaseUnsafe(wavEnv.Loop >= 0 ? wavEnv.Loop / n163WaveSize : -1, wavEnv.Release >= 0 ? wavEnv.Release / n163WaveSize : -1);
            wavEnv.SetFromPreset(EnvelopeType.N163Waveform, n163WavePreset);
        }

        public void PerformPostLoadActions()
        {
            switch (expansion)
            {
                case ExpansionType.N163:
                    envelopes[EnvelopeType.N163Waveform].SetChunkMaxLengthUnsafe(n163WaveSize, N163MaxWaveCount * n163WaveSize);
                    break;
                case ExpansionType.Fds:
                    envelopes[EnvelopeType.FdsWaveform].SetChunkMaxLengthUnsafe(64, FdsMaxWaveCount * 64);
                    break;
            }
        }

        public void NotifyEnvelopeChanged(int envType)
        {
            switch (envType)
            {
                case EnvelopeType.N163Waveform:
                    n163WaveCount = (byte)(envelopes[EnvelopeType.N163Waveform].Length / envelopes[EnvelopeType.N163Waveform].ChunkLength);
                    UpdateN163WaveEnvelope();
                    break;
                case EnvelopeType.FdsWaveform:
                    fdsWaveCount = (byte)(envelopes[EnvelopeType.FdsWaveform].Length / envelopes[EnvelopeType.FdsWaveform].ChunkLength);
                    UpdateFdsWaveEnvelope();
                    break;
            }
        }

        private void ClampN163WaveCount()
        {
            n163WaveCount = (byte)Utils.Clamp(n163WaveCount, 1, N163MaxWaveCount);
        }

        public static string GetVrc7PatchName(int preset)
        {
            return Vrc7InstrumentPatch.Infos[preset].name;
        }

        public static string GetEpsmPatchName(int preset)
        {
            return EpsmInstrumentPatch.Infos[preset].name;
        }

        // Convert from our "repeat" based representation to the "index" based
        // representation used by our NSF driver and FamiTracker. Will optimize
        // duplicated sub-waveforms.
        public void BuildWaveformsAndWaveIndexEnvelope(out byte[][] waves, out Envelope indexEnvelope, bool encode)
        {
            Debug.Assert(IsFdsInstrument || IsN163Instrument);

            var env = envelopes[IsN163Instrument ? EnvelopeType.N163Waveform : EnvelopeType.FdsWaveform];
            var rep = envelopes[EnvelopeType.WaveformRepeat];

            // Compute CRCs, this will eliminate duplicates.
            var waveCrcs = new Dictionary<uint, int>();
            var waveIndexOldToNew = new int[n163WaveCount];
            var waveCount = 0;

            for (int i = 0; i < n163WaveCount; i++)
            {
                var crc = CRC32.Compute(env.Values, i * n163WaveSize, n163WaveSize);

                if (waveCrcs.TryGetValue(crc, out var existingIndex))
                {
                    waveIndexOldToNew[i] = existingIndex;
                }
                else
                {
                    waveCrcs[crc] = waveCount;
                    waveIndexOldToNew[i] = waveCount;
                    waveCount++;
                }
            }

            var waveIndexNewToOld = new int[waveCount];
            for (int i = n163WaveCount - 1; i >= 0; i--)
                waveIndexNewToOld[waveIndexOldToNew[i]] = i;

            // Create the wave index envelope.
            indexEnvelope = rep.CreateWaveIndexEnvelope();
           
            // Remap the indices in the wave index envelope.
            for (int i = 0; i < indexEnvelope.Values.Length; i++)
                indexEnvelope.Values[i] = (sbyte)waveIndexOldToNew[indexEnvelope.Values[i]];

            // Create the individual waveforms.
            waves = new byte[waveCount][];

            for (int i = 0; i < waveCount; i++)
            {
                var oldIndex = waveIndexNewToOld[i];

                if (IsN163Instrument && encode)
                    waves[i] = env.GetN163Waveform(oldIndex);
                else
                    waves[i] = env.GetChunk(oldIndex);
            }
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
                var env = envelopes[i];
                var rep = envelopes[EnvelopeType.WaveformRepeat];
                var envelopeExists = env != null;
                var envelopeShouldExists = IsEnvelopeActive(i);

                Debug.Assert(envelopeExists == envelopeShouldExists);

                if (envelopeExists)
                {
                    Debug.Assert(env.Length % env.ChunkLength == 0);
                    Debug.Assert(env.ValuesInValidRange(this, i));

                    if (i == EnvelopeType.N163Waveform)
                    {
                        Debug.Assert(env.ChunkLength == n163WaveSize);
                        Debug.Assert(env.Length == n163WaveSize * n163WaveCount);
                        Debug.Assert(env.Length / env.ChunkLength == rep.Length);
                        Debug.Assert(env.Loop < 0 && rep.Loop < 0 || rep.Loop == env.Loop / n163WaveSize);
                        Debug.Assert(env.Release < 0 && rep.Release < 0 || rep.Release == env.Release / n163WaveSize);
                    }

                    if (i == EnvelopeType.FdsWaveform)
                    {
                        Debug.Assert(env.ChunkLength == 64);
                        Debug.Assert(env.Length == 64 * fdsWaveCount);
                        Debug.Assert(env.Length / env.ChunkLength == rep.Length);
                        Debug.Assert(env.Loop < 0 && rep.Loop < 0 || rep.Loop == env.Loop / 64);
                        Debug.Assert(env.Release < 0 && rep.Release < 0 || rep.Release == env.Release / 64);
                    }
                }
            }

            if (IsN163Instrument && n163WavePreset != WavePresetType.Custom)
                Debug.Assert(envelopes[EnvelopeType.N163Waveform].ValidatePreset(EnvelopeType.N163Waveform, n163WavePreset));
            if (IsFdsInstrument && fdsWavPreset != WavePresetType.Custom)
                Debug.Assert(envelopes[EnvelopeType.FdsWaveform].ValidatePreset(EnvelopeType.FdsWaveform, fdsWavPreset));
#endif
        }

        public void ChangeId(int newId)
        {
            id = newId;
        }

        public override string ToString()
        {
            return name;
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
                        case ExpansionType.Fds:
                            buffer.Serialize(ref fdsMasterVolume);
                            buffer.Serialize(ref fdsWavPreset);
                            buffer.Serialize(ref fdsModPreset);
                            buffer.Serialize(ref fdsModSpeed);
                            buffer.Serialize(ref fdsModDepth); 
                            buffer.Serialize(ref fdsModDelay);
                            // At version 14 (FamiStudio 4.0.0), we added multi-waveforms for N163 and FDS.
                            if (buffer.Version >= 14)
                                buffer.Serialize(ref fdsWaveCount);
                            break;
                        case ExpansionType.N163:
                            buffer.Serialize(ref n163WavePreset);
                            buffer.Serialize(ref n163WaveSize);
                            buffer.Serialize(ref n163WavePos);
                            // At version 14 (FamiStudio 4.0.0), we added multi-waveforms for N163 and FDS.
                            if (buffer.Version >= 14)
                                buffer.Serialize(ref n163WaveCount);
                            break;

                        case ExpansionType.Vrc7:
                            buffer.Serialize(ref vrc7Patch);
                            for (int i = 0; i < vrc7PatchRegs.Length; i++)
                                buffer.Serialize(ref vrc7PatchRegs[i]);
                            break;

                        case ExpansionType.EPSM:
                            buffer.Serialize(ref epsmPatch);
                            for (int i = 0; i < epsmPatchRegs.Length; i++)
                                buffer.Serialize(ref epsmPatchRegs[i]);
                            break;
                        case ExpansionType.Vrc6:
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
                    envelopes[i].SerializeState(buffer, i);
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

            // At version 12, FamiStudio 3.2.0, we realized that we had some FDS envelopes (likely imported from NSF)
            // with bad values. Also, some pitches as well.
            if (buffer.Version < 12)
            {
                if (IsFdsInstrument)
                    envelopes[EnvelopeType.FdsWaveform].ClampToValidRange(this, EnvelopeType.FdsWaveform);
                if (IsVrc6Instrument)
                    envelopes[EnvelopeType.Pitch].ClampToValidRange(this, EnvelopeType.Pitch);
            }

            // At version 14 (FamiStudio 4.0.0), we added multi-waveforms for N163 and FDS.
            if (buffer.Version < 14 && (IsN163Instrument || IsFdsInstrument))
            {
                envelopes[EnvelopeType.WaveformRepeat] = new Envelope(EnvelopeType.WaveformRepeat);
                envelopes[EnvelopeType.WaveformRepeat].Length = 1;

                // I found some old files where the preset does not match with 
                // the actual waves. Likely stuff i made with an old development
                // version. Need to check if the preset matches the waveform here :(
                if (IsN163Instrument && n163WavePreset != WavePresetType.Custom && !envelopes[EnvelopeType.N163Waveform].ValidatePreset(EnvelopeType.N163Waveform, n163WavePreset))
                    n163WavePreset = WavePresetType.Custom;
                if (IsFdsInstrument && fdsWavPreset != WavePresetType.Custom && !envelopes[EnvelopeType.FdsWaveform].ValidatePreset(EnvelopeType.FdsWaveform, fdsWavPreset))
                    n163WavePreset = WavePresetType.Custom;
            }

            if (buffer.IsReading)
            {
                PerformPostLoadActions();
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


    public static class EpsmInstrumentPatch
    {
        public const byte Custom = 0;
        public const byte Default = 1;

        public struct EpsmPatchInfo
        {
            public string name;
            public byte[] data;
        };

        public static readonly EpsmPatchInfo[] Infos = new[]
        {
            new EpsmPatchInfo() { name = "Custom",       data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } }, // Custom  
            new EpsmPatchInfo() { name = "Default",      data = new byte[] { 0x04, 0xc0, 0x00, 0x20, 0x1f, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x1f, 0x00, 0x00, 0x07, 0x00, 0x00, 0x20, 0x1f, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x1f, 0x00, 0x00, 0x07, 0x00, 0x00 } }, // Default
                                                                           //0xB0, 0xB4, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80, 0x90, 0x38, 0x48, 0x58, 0x68, 0x78, 0x88, 0x98, 0x34, 0x44, 0x54, 0x64, 0x74, 0x84, 0x94, 0x3c, 0x4c, 0x5c, 0x6c, 0x7c, 0x8c, 0x9c, 0x22 -- Register order

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
