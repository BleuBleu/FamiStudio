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
        private string folderName;
        private Project project;
        private Dictionary<int, DPCMSampleMapping> samplesMapping;

        // FDS
        private byte    fdsMasterVolume = FdsMasterVolumeType.Volume100;
        private byte    fdsWavPreset = WavePresetType.Sine;
        private byte    fdsModPreset = WavePresetType.Flat;
        private ushort  fdsModSpeed;
        private byte    fdsModDepth;
        private byte    fdsModDelay;
        private bool    fdsAutoMod;
        private byte    fdsAutoModDenom = 1;
        private byte    fdsAutoModNumer = 1;
        private int     fdsResampleWavPeriod = 128;
        private int     fdsResampleWavOffset = 0;
        private bool    fdsResampleWavNormalize = true;
        private short[] fdsResampleWavData;

        // N163
        private byte    n163WavPreset = WavePresetType.Sine;
        private byte    n163WavSize = 16;
        private byte    n163WavPos = 0;
        private byte    n163WavCount = 1;
        private int     n163ResampleWavPeriod = 128;
        private int     n163ResampleWavOffset = 0;
        private bool    n163ResampleWavNormalize = true;
        private short[] n163ResampleWavData;

        // S5B
        private bool   s5bEnvAutoPitch = true;
        private sbyte  s5bEnvAutoPitchOctave = 3;
        private byte   s5bEnvShape = 0; // 0 = off, 1...8 maps to 0x8 to 0xf.
        private ushort s5bEnvPeriod = 1000; // Only when auto-pitch is off

        // VRC6
        private byte vrc6SawMasterVolume = Vrc6SawMasterVolumeType.Half;

        // VRC7
        private byte   vrc7Patch = Vrc7InstrumentPatch.Bell;
        private byte[] vrc7PatchRegs = new byte[8];

        // EPSM
        private byte   epsmPatch = EpsmInstrumentPatch.Default;
        private byte[] epsmPatchRegs = new byte[31];
        private bool   epsmSquareEnvAutoPitch = true;
        private sbyte  epsmSquareEnvAutoPitchOctave = 3;
        private byte   epsmSquareEnvShape = 0; // 0 = off, 1...8 maps to 0x8 to 0xf.
        private ushort epsmSquareEnvPeriod = 4000; // Only when auto-pitch is off

        // For N163/FDS wav presets.
        public int Id => id;
        public string Name { get => name; set => name = value; }
        public string NameWithExpansion => Name + (expansion == ExpansionType.None ? "" : $" ({ExpansionType.InternalNames[expansion]})");
        public Color Color { get => color; set => color = value; }
        public int Expansion => expansion;
        public bool IsExpansionInstrument => expansion != ExpansionType.None;
        public Envelope[] Envelopes => envelopes;
        public Dictionary<int, DPCMSampleMapping> SamplesMapping => samplesMapping;
        public byte[] Vrc7PatchRegs => vrc7PatchRegs;
        public byte[] EpsmPatchRegs => epsmPatchRegs;
        public string FolderName { get => folderName; set => folderName = value; }
        public Folder Folder => string.IsNullOrEmpty(folderName) ? null : project.GetFolder(FolderType.Instrument, folderName);

        public bool IsRegular => expansion == ExpansionType.None;
        public bool IsFds     => expansion == ExpansionType.Fds;
        public bool IsVrc6    => expansion == ExpansionType.Vrc6;
        public bool IsVrc7    => expansion == ExpansionType.Vrc7;
        public bool IsN163    => expansion == ExpansionType.N163;
        public bool IsS5B     => expansion == ExpansionType.S5B;
        public bool IsEpsm    => expansion == ExpansionType.EPSM;

        public Envelope VolumeEnvelope          => envelopes[EnvelopeType.Volume];
        public Envelope PitchEnvelope           => envelopes[EnvelopeType.Pitch];
        public Envelope ArpeggioEnvelope        => envelopes[EnvelopeType.Arpeggio];
        public Envelope N163WaveformEnvelope    => envelopes[EnvelopeType.N163Waveform];
        public Envelope FdsWaveformEnvelope     => envelopes[EnvelopeType.FdsWaveform];
        public Envelope FdsModulationEnvelope   => envelopes[EnvelopeType.FdsModulation];
        public Envelope WaveformRepeatEnvelope  => envelopes[EnvelopeType.WaveformRepeat];
        public Envelope YMMixerSettingsEnvelope => envelopes[EnvelopeType.S5BMixer];
        public Envelope YMNoiseFreqEnvelope     => envelopes[EnvelopeType.S5BNoiseFreq];

        public const int MaxResampleWavSamples = 12000;

        public Instrument()
        {
        }

        public Instrument(Project project, int id, int expansion, string name)
        {
            this.project = project;
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
            else if (expansion == ExpansionType.None)
            {
                samplesMapping = new Dictionary<int, DPCMSampleMapping>();
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
                return expansion == ExpansionType.N163;
            }
            else if (envelopeType == EnvelopeType.S5BNoiseFreq ||
                     envelopeType == EnvelopeType.S5BMixer)
            {
                return expansion == ExpansionType.S5B ||
                       expansion == ExpansionType.EPSM;
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

        public static bool EnvelopeHasRepeat(int envelopeType)
        {
            return envelopeType == EnvelopeType.N163Waveform;
        }

        public bool CanRelease(Channel channel)
        { 
            return 
                VolumeEnvelope != null && VolumeEnvelope.Release >= 0 || 
                WaveformRepeatEnvelope != null && WaveformRepeatEnvelope.Release >= 0 ||
                expansion == ExpansionType.Vrc7 && channel.IsVrc7Channel ||
                expansion == ExpansionType.EPSM && channel.IsEPSMFmChannel;
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

        public int FdsMaxWaveCount
        {
            get
            {
                return Math.Min(64, FdsWaveformEnvelope.Values.Length / 64);
            }
        }

        public int FdsResampleWavePeriod
        {
            get { return fdsResampleWavPeriod; }
            set { fdsResampleWavPeriod = Math.Max(1, value); ResampleWaveform(); }
        }

        public int FdsResampleWaveOffset
        {
            get { return fdsResampleWavOffset; }
            set { fdsResampleWavOffset = value; ResampleWaveform(); }
        }

        public bool FdsResampleWavNormalize
        {
            get { return fdsResampleWavNormalize; }
            set { fdsResampleWavNormalize = value; ResampleWaveform(); }
        }

        public short[] FdsResampleWaveData => fdsResampleWavData;

        public byte N163WavePreset
        {
            get { return n163WavPreset; }
            set
            {
                n163WavPreset = value;
                UpdateN163WaveEnvelope();
            }
        }

        public byte N163WaveSize
        {
            get { return n163WavSize; }
            set
            {
                Debug.Assert((value & 0x03) == 0);
                n163WavSize = (byte)Utils.Clamp(value      & 0xfc, 4, N163MaxWaveSize);
                n163WavPos  = (byte)Utils.Clamp(n163WavPos & 0xfc, 0, N163MaxWavePos);
                ClampN163WaveCount();
                UpdateN163WaveEnvelope();
            }
        }

        public byte N163WavePos
        {
            get { return n163WavPos; }
            set
            {
                Debug.Assert((value & 0x01) == 0);
                n163WavPos  = (byte)Utils.Clamp(value       & 0xfe, 0, N163MaxWavePos);
                n163WavSize = (byte)Utils.Clamp(n163WavSize & 0xfc, 4, N163MaxWaveSize);
                ClampN163WaveCount();
                UpdateN163WaveEnvelope();
            }
        }

        public byte N163WaveCount
        {
            get { return n163WavCount; }
            set
            {
                n163WavCount = (byte)Utils.Clamp(value, 1, N163MaxWaveCount);
                UpdateN163WaveEnvelope();
            }
        }

        public int N163MaxWaveSize  => (project.N163WaveRAMSize * 2 - n163WavPos) & 0xfc; // Needs to be multiple of 4.
        public int N163MaxWavePos   => (project.N163WaveRAMSize * 2 - 4);
        public int N163MaxWaveCount => Math.Min(64, N163WaveformEnvelope.Values.Length / n163WavSize); 

        public int N163ResampleWavePeriod
        {
            get { return n163ResampleWavPeriod; }
            set { n163ResampleWavPeriod = Math.Max(1, value); ResampleWaveform(); }
        }

        public int N163ResampleWaveOffset
        {
            get { return n163ResampleWavOffset; }
            set { n163ResampleWavOffset = value; ResampleWaveform();  }
        }

        public bool N163ResampleWavNormalize
        {
            get { return n163ResampleWavNormalize; }
            set { n163ResampleWavNormalize = value; ResampleWaveform(); }
        }

        public short[] N163ResampleWaveData => n163ResampleWavData;

        public bool   S5BEnvAutoPitch        { get => s5bEnvAutoPitch;              set => s5bEnvAutoPitch = value; }
        public sbyte  S5BEnvAutoPitchOctave  { get => s5bEnvAutoPitchOctave;        set => s5bEnvAutoPitchOctave = (sbyte)Utils.Clamp(value, -8, 8); }
        public byte   S5BEnvelopeShape       { get => s5bEnvShape;                  set => s5bEnvShape = (byte)Utils.Clamp(value, 0, 8); }
        public ushort S5BEnvelopePitch       { get => s5bEnvPeriod;                 set => s5bEnvPeriod = value; }
        
        public bool   EPSMSquareEnvAutoPitch       { get => epsmSquareEnvAutoPitch;       set => epsmSquareEnvAutoPitch = value; }
        public sbyte  EPSMSquareEnvAutoPitchOctave { get => epsmSquareEnvAutoPitchOctave; set => epsmSquareEnvAutoPitchOctave = (sbyte)Utils.Clamp(value, -8, 8); }
        public byte   EPSMSquareEnvelopeShape      { get => epsmSquareEnvShape;           set => epsmSquareEnvShape = (byte)Utils.Clamp(value, 0, 8); }
        public ushort EPSMSquareEnvelopePitch      { get => epsmSquareEnvPeriod;          set => epsmSquareEnvPeriod = value; }

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
        public bool   FdsAutoMod      { get => fdsAutoMod;      set => fdsAutoMod = value; }
        public byte   FdsAutoModDenom { get => fdsAutoModDenom; set => fdsAutoModDenom = (byte)Utils.Clamp(value, 1, 32); }
        public byte   FdsAutoModNumer { get => fdsAutoModNumer; set => fdsAutoModNumer = (byte)Utils.Clamp(value, 1, 32); }

        public void UpdateFdsModulationEnvelope()
        {
            FdsModulationEnvelope.SetFromPreset(EnvelopeType.FdsModulation, fdsModPreset);
        }

        public void UpdateFdsWaveEnvelope()
        {
            if (fdsWavPreset == WavePresetType.Resample)
                ResampleWaveform();
            else
                FdsWaveformEnvelope.SetFromPreset(EnvelopeType.FdsWaveform, fdsWavPreset);
        }

        public void SetFdsResampleWaveData(short[] wav)
        {
            Debug.Assert(wav.Length <= MaxResampleWavSamples);
            fdsResampleWavData = wav;
            fdsWavPreset = WavePresetType.Resample;
            UpdateFdsWaveEnvelope();
        }

        public void DeleteFdsResampleWavData()
        {
            fdsResampleWavData = null;
            fdsResampleWavOffset = 0;
            fdsResampleWavPeriod = 128;
            if (fdsWavPreset == WavePresetType.Resample)
                fdsWavPreset = WavePresetType.Custom;
        }

        public void UpdateN163WaveEnvelope()
        {
            var wavEnv = N163WaveformEnvelope;
            var repEnv = WaveformRepeatEnvelope;

            wavEnv.MaxLength = N163MaxWaveCount * n163WavSize;
            wavEnv.ChunkLength = n163WavSize;
            wavEnv.Length = n163WavSize * n163WavCount;
            repEnv.Length = n163WavCount;
            repEnv.SetLoopReleaseUnsafe(wavEnv.Loop >= 0 ? wavEnv.Loop / n163WavSize : -1, wavEnv.Release >= 0 ? wavEnv.Release / n163WavSize : -1);

            if (n163WavPreset == WavePresetType.Resample)
                ResampleWaveform();
            else
                wavEnv.SetFromPreset(EnvelopeType.N163Waveform, n163WavPreset);
        }

        public void SetN163ResampleWaveData(short[] wav)
        {
            Debug.Assert(wav.Length <= MaxResampleWavSamples);
            n163ResampleWavData = wav;
            n163WavPreset = WavePresetType.Resample;
            UpdateN163WaveEnvelope();
        }

        public void DeleteN163ResampleWavData()
        {
            n163ResampleWavData = null;
            n163ResampleWavOffset = 0;
            n163ResampleWavPeriod = 128;
            if (n163WavPreset == WavePresetType.Resample)
                n163WavPreset = WavePresetType.Custom;
        }

        private void ResampleWaveform()
        {
            Debug.Assert(IsN163 || IsFds);

            var wavData = IsN163 ? n163ResampleWavData : fdsResampleWavData;

            if (wavData != null)
            {
                var isN163 = IsN163;

                var waveCount     = isN163 ? n163WavCount : 1;
                var waveSize      = isN163 ? n163WavSize  : 64;
                var waveNormalize = isN163 ? n163ResampleWavNormalize : fdsResampleWavNormalize;
                var wavePeriod    = isN163 ? n163ResampleWavPeriod    : fdsResampleWavPeriod;
                var waveOffset    = Utils.Clamp(isN163 ? n163ResampleWavOffset : fdsResampleWavOffset, 0, wavData.Length - 1);

                var wavFiltered = wavData.Clone() as short[];
                if (waveNormalize)
                    WaveUtils.Normalize(wavFiltered);

                var cutoff = waveSize / (float)wavePeriod;
                WaveUtils.LowPassFilter(ref wavFiltered, cutoff, cutoff * 0.8f, 64);

                var resampling = new short[waveCount * waveSize];
                WaveUtils.Resample(wavFiltered, waveOffset, waveOffset + waveCount * wavePeriod, resampling);

                var envType = isN163 ? EnvelopeType.N163Waveform : EnvelopeType.FdsWaveform;
                Envelope.GetMinMaxValueForType(this, envType, out var minValue, out var maxValue);

                var env = envelopes[envType];
                for (int i = 0; i < resampling.Length; i++)
                    env.Values[i] = (sbyte)MathF.Round(Utils.Lerp(minValue, maxValue, (resampling[i] + 32768.0f) / 65535.0f));

                if (isN163)
                    n163ResampleWavOffset = waveOffset;
                else
                    fdsResampleWavOffset = waveOffset;
            }
        }

        public void PerformPostLoadActions()
        {
            switch (expansion)
            {
                case ExpansionType.N163:
                    N163WaveformEnvelope.SetChunkMaxLengthUnsafe(n163WavSize, N163MaxWaveCount * n163WavSize);
                    UpdateN163WaveEnvelope(); // Safety
                    break;
            }
        }

        public void NotifyEnvelopeChanged(int envType, bool invalidatePreset)
        {
            switch (envType)
            {
                case EnvelopeType.N163Waveform:
                    n163WavCount = (byte)(N163WaveformEnvelope.Length / N163WaveformEnvelope.ChunkLength);
                    if (invalidatePreset)
                        n163WavPreset = WavePresetType.Custom;
                    UpdateN163WaveEnvelope();
                    break;
                case EnvelopeType.FdsWaveform:
                    if (invalidatePreset)
                        fdsWavPreset = WavePresetType.Custom;
                    UpdateFdsWaveEnvelope();
                    break;
                case EnvelopeType.FdsModulation:
                    if (invalidatePreset)
                        fdsModPreset = WavePresetType.Custom;
                    break;
            }
        }

        public void NotifyN163RAMSizeChanged()
        {
            if (IsN163)
                N163WavePos = N163WavePos;
        }

        private void ClampN163WaveCount()
        {
            n163WavCount = (byte)Utils.Clamp(n163WavCount, 1, N163MaxWaveCount);
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
            Debug.Assert(IsFds || IsN163);

            var sourceWaveCount = IsN163 ? N163WaveCount : 1;
            var waveSize        = IsN163 ? N163WaveSize  : 64;

            var env = IsN163 ? N163WaveformEnvelope : FdsWaveformEnvelope;
            var rep = WaveformRepeatEnvelope;

            // Compute CRCs, this will eliminate duplicates.
            var waveCrcs = new Dictionary<uint, int>();
            var waveIndexOldToNew = new int[sourceWaveCount];
            var waveCount = 0;

            for (int i = 0; i < sourceWaveCount; i++)
            {
                var crc = CRC32.Compute(env.Values, i * waveSize, waveSize);

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
            for (int i = sourceWaveCount - 1; i >= 0; i--)
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

                if (IsN163 && encode)
                    waves[i] = env.GetN163Waveform(oldIndex);
                else
                    waves[i] = env.GetChunk(oldIndex);
            }
        }

        public uint ComputeCRC(uint crc = 0)
        {
            var serializer = new ProjectCrcBuffer(crc);
            Serialize(serializer);
            return serializer.CRC;
        }

        public static bool AllEnvelopesAreIdentical(Instrument i1, Instrument i2, int envelopeMask = -1)
        {
            if (i1 == null || i2 == null)
            {
                return false;
            }
            
            if (i1 != i2)
            {
                Debug.Assert(i1.expansion == i2.expansion);

                for (var i = 0; i < EnvelopeType.Count; i++)
                {
                    var mask = 1 << i;

                    if ((envelopeMask & mask) != 0)
                    {
                        var e1 = i1.envelopes[i];
                        var e2 = i2.envelopes[i];

                        Debug.Assert((e1 == null) == (e2 == null));

                        if (e1 != null && e2 != null)
                        {
                            if (!Envelope.AreIdentical(i, e1, e2))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        public DPCMSampleMapping MapDPCMSample(int note, DPCMSample sample, int pitch = 15, bool loop = false)
        {
            Debug.Assert(Note.IsMusicalNote(note));

            if (sample != null)
            {
                var mapping = new DPCMSampleMapping();
                mapping.Sample = sample;
                mapping.Pitch = pitch;
                mapping.Loop = loop;
                samplesMapping.Add(note, mapping);
                return mapping;
            }

            return null;
        }

        public void UnmapDPCMSample(int note)
        {
            samplesMapping.Remove(note);
        }

        public DPCMSampleMapping GetDPCMMapping(int note)
        {
            if (samplesMapping != null)
            { 
                samplesMapping.TryGetValue(note, out var mapping);
                return mapping;
            }
            else
            {
                return null;
            }
        }

        public int FindDPCMSampleMapping(DPCMSample sample, int pitch, bool loop)
        {
            foreach (var kv in samplesMapping)
            {
                if (kv.Value.Sample == sample &&
                    kv.Value.Pitch  == pitch &&
                    kv.Value.Loop   == loop)
                {
                    return kv.Key;
                }
            }

            return -1;
        }

        public bool GetMinMaxMappedSampleIndex(out int minIdx, out int maxIdx)
        {
            minIdx = 999;
            maxIdx = -1;

            foreach (var kv in samplesMapping)
            {
                minIdx = Math.Min(kv.Key, minIdx);
                maxIdx = Math.Max(kv.Key, maxIdx);
            }

            return maxIdx != -1;
        }

        public int GetTotalMappedSampleSize(List<DPCMSample> visitedSamples = null)
        {
            var size = 0;

            if (samplesMapping != null)
            {
                if (visitedSamples == null)
                {
                    visitedSamples = new List<DPCMSample>();
                }

                foreach (var kv in samplesMapping)
                {
                    if (kv.Value.Sample != null && !visitedSamples.Contains(kv.Value.Sample))
                    {
                        visitedSamples.Add(kv.Value.Sample);
                        size += Utils.AlignSampleOffset(kv.Value.Sample.ProcessedData.Length);
                    }
                }
            }

            return size;
        }

        public bool HasAnyMappedSamples
        {
            get
            {
                return samplesMapping != null && samplesMapping.Count > 0;
            }
        }

        public void ReplaceSampleInAllMappings(DPCMSample oldSample, DPCMSample newSample)
        {
            foreach (var kv in samplesMapping)
            {
                if (kv.Value.Sample == oldSample)
                {
                    kv.Value.Sample = newSample;
                }
            }
            
            if (newSample == null)
            {
                ClearMappingsWithNullSamples();
            }
        }

        private void ClearMappingsWithNullSamples()
        {
            var toRemove = (List<int>)null;

            foreach (var kv in samplesMapping)
            {
                if (kv.Value.Sample == null)
                {
                    if (toRemove == null)
                        toRemove = new List<int>();
                    toRemove.Add(kv.Key);
                }
            }

            if (toRemove != null)
            {
                foreach (var k in toRemove)
                {
                    samplesMapping.Remove(k);
                }
            }
        }

        public void DeleteAllMappings()
        {
            samplesMapping.Clear();
        }

        public void ValidateIntegrity(Project project, Dictionary<int, object> idMap)
        {
#if DEBUG
            project.ValidateId(id);

            Debug.Assert(project == this.project);

            if (idMap.TryGetValue(id, out var foundObj))
                Debug.Assert(foundObj == this);
            else
                idMap.Add(id, this);

            Debug.Assert(!string.IsNullOrEmpty(name.Trim()));
            Debug.Assert(project.GetInstrument(id) == this);
            Debug.Assert(string.IsNullOrEmpty(folderName) || project.FolderExists(FolderType.Instrument, folderName));

            for (int i = 0; i < EnvelopeType.Count; i++)
            {
                var env = envelopes[i];
                var envelopeExists = env != null;
                var envelopeShouldExists = IsEnvelopeActive(i);

                Debug.Assert(envelopeExists == envelopeShouldExists);

                if (envelopeExists)
                {
                    Debug.Assert(env.Length % env.ChunkLength == 0);
                    Debug.Assert(env.ValuesInValidRange(this, i));

                    if (i == EnvelopeType.N163Waveform)
                    {
                        var rep = WaveformRepeatEnvelope;

                        Debug.Assert(env.ChunkLength == n163WavSize);
                        Debug.Assert(env.Length == n163WavSize * n163WavCount);
                        Debug.Assert(env.Length / env.ChunkLength == rep.Length);
                        Debug.Assert(env.Loop < 0 && rep.Loop < 0 || rep.Loop == env.Loop / n163WavSize);
                        Debug.Assert(env.Release < 0 && rep.Release < 0 || rep.Release == env.Release / n163WavSize);
                    }

                    if (i == EnvelopeType.FdsWaveform)
                    {
                        Debug.Assert(env.Length == 64);
                        Debug.Assert(env.MaxLength == 64);
                    }
                }
            }

            if (IsN163 && n163WavPreset != WavePresetType.Custom)
                Debug.Assert(N163WaveformEnvelope.ValidatePreset(EnvelopeType.N163Waveform, n163WavPreset));
            if (IsFds && fdsWavPreset != WavePresetType.Custom)
                Debug.Assert(FdsWaveformEnvelope.ValidatePreset(EnvelopeType.FdsWaveform, fdsWavPreset));
            if (IsFds && fdsModPreset != WavePresetType.Custom)
                Debug.Assert(FdsModulationEnvelope.ValidatePreset(EnvelopeType.FdsModulation, fdsModPreset));

            Debug.Assert(
                expansion != ExpansionType.None && samplesMapping == null ||
                expansion == ExpansionType.None && samplesMapping != null);

            if (samplesMapping != null)
            {
                foreach (var kv in samplesMapping)
                {
                    Debug.Assert(kv.Value != null); // Null sample is ok-ish, null mapping isnt.
                    Debug.Assert(kv.Value.Sample == null || project.Samples.Contains(kv.Value.Sample));
                    kv.Value.ValidateIntegrity(project, idMap);
                }
            }
#endif
        }

        public void SetProject(Project p)
        {
            project = p;
        }

        public void ChangeId(int newId)
        {
            id = newId;
        }

        public override string ToString()
        {
            return name;
        }

        public void Serialize(ProjectBuffer buffer)
        {
            if (buffer.IsReading)
                project = buffer.Project;

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
                            // At version 16 (FamiStudio 4.2.0), we added FDS auto-modulation
                            if (buffer.Version >= 16)
                            {
                                buffer.Serialize(ref fdsAutoMod);
                                buffer.Serialize(ref fdsAutoModDenom);
                                buffer.Serialize(ref fdsAutoModNumer);
                            }
                            // At version 14 (FamiStudio 4.0.0), we added wav resampling for FDS.
                            if (buffer.Version >= 14)
                            {
                                buffer.Serialize(ref fdsResampleWavPeriod);
                                buffer.Serialize(ref fdsResampleWavOffset);
                                buffer.Serialize(ref fdsResampleWavNormalize);
                                buffer.Serialize(ref fdsResampleWavData);
                            }
                            break;
                        case ExpansionType.N163:
                            buffer.Serialize(ref n163WavPreset);
                            buffer.Serialize(ref n163WavSize);
                            buffer.Serialize(ref n163WavPos);
                            // At version 14 (FamiStudio 4.0.0), we added multi-waveforms for N163 and wav resampling.
                            if (buffer.Version >= 14)
                            {
                                buffer.Serialize(ref n163WavCount);
                                buffer.Serialize(ref n163ResampleWavPeriod);
                                buffer.Serialize(ref n163ResampleWavOffset);
                                buffer.Serialize(ref n163ResampleWavNormalize);
                                buffer.Serialize(ref n163ResampleWavData);
                            }
                            break;

                        case ExpansionType.S5B:
                            // At version 16 (FamiStudio 4.2.0), we added basic support for S5B envelope.
                            if (buffer.Version >= 16)
                            {
                                buffer.Serialize(ref s5bEnvAutoPitch);
                                buffer.Serialize(ref s5bEnvAutoPitchOctave);
                                buffer.Serialize(ref s5bEnvShape);
                                buffer.Serialize(ref s5bEnvPeriod);
                            }
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
                            // At version 16 (FamiStudio 4.2.0), we added basic support for S5B envelope.
                            if (buffer.Version >= 16)
                            {
                                buffer.Serialize(ref epsmSquareEnvAutoPitch);
                                buffer.Serialize(ref epsmSquareEnvAutoPitchOctave);
                                buffer.Serialize(ref epsmSquareEnvShape);
                                buffer.Serialize(ref epsmSquareEnvPeriod);
                            }
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

            ushort envelopeMask = 0;
            if (buffer.IsWriting)
            {
                for (int i = 0; i < EnvelopeType.Count; i++)
                {
                    if (envelopes[i] != null)
                        envelopeMask = (ushort)(envelopeMask | (1 << i));
                }
            }

            // At version 15 (FamiStudio 4.1.0) we expanded the envelope mask to 16bits.
            if (buffer.Version < 15)
            {
                byte envelopeMaskByte = 0;
                buffer.Serialize(ref envelopeMaskByte);
                envelopeMask = envelopeMaskByte;
            }
            else
            {
                buffer.Serialize(ref envelopeMask);
            }

            for (int i = 0; i < EnvelopeType.Count; i++)
            {
                if ((envelopeMask & (1 << i)) != 0)
                {
                    if (buffer.IsReading)
                        envelopes[i] = new Envelope(i);
                    envelopes[i].Serialize(buffer, i);
                }
                else
                {
                    envelopes[i] = null;
                }
            }

            // At version 5 (FamiStudio 2.0.0) we added duty cycle envelopes.
            if (buffer.Version < 5)
            {
                envelopes[EnvelopeType.DutyCycle] = new Envelope(EnvelopeType.DutyCycle);
                if (dutyCycle != 0)
                {
                    envelopes[EnvelopeType.DutyCycle].Length = 1;
                    envelopes[EnvelopeType.DutyCycle].Values[0] = (sbyte)dutyCycle;
                }
            }

            // At version 16 (FamiStudio 4.2.0) we added little folders in the project explorer.
            if (buffer.Version >= 16)
            {
                buffer.Serialize(ref folderName);
            }

            // At version 12, FamiStudio 3.2.0, we realized that we had some FDS envelopes (likely imported from NSF)
            // with bad values. Also, some pitches as well.
            if (buffer.Version < 12)
            {
                if (IsFds)
                    FdsWaveformEnvelope.ClampToValidRange(this, EnvelopeType.FdsWaveform);
                if (IsVrc6)
                    PitchEnvelope.ClampToValidRange(this, EnvelopeType.Pitch);
            }

            // At version 14 (FamiStudio 4.0.0), we added multi-waveforms for N163
            if (buffer.Version < 14 && IsN163)
            {
                envelopes[EnvelopeType.WaveformRepeat] = new Envelope(EnvelopeType.WaveformRepeat);
                envelopes[EnvelopeType.WaveformRepeat].Length = 1;
            }

            if (IsRegular)
            {
                if (buffer.IsReading)
                    samplesMapping = new Dictionary<int, DPCMSampleMapping>();
                else if (!buffer.IsForUndoRedo)
                    ClearMappingsWithNullSamples();

                // At version 15 (FamiStudio 4.1.0) we moved DPCM samples mapping to instruments, a-la Famitracker.
                if (buffer.Version >= 15)
                {
                    var mappingCount = samplesMapping.Count;
                    buffer.Serialize(ref mappingCount);

                    // Ugly, we should look into adding support for dictionaries in the ProjectBuffer.
                    if (buffer.IsReading)
                    {
                        var mappingNotes = new int[mappingCount];
                        for (var i = 0; i < mappingCount; i++)
                        {
                            buffer.Serialize(ref mappingNotes[i]);
                        }

                        for (var i = 0; i < mappingCount; i++)
                        {
                            var mapping = new DPCMSampleMapping();
                            mapping.Serialize(buffer);
                            samplesMapping.Add(mappingNotes[i], mapping);
                        }
                    }
                    else
                    {
                        foreach (var kv in samplesMapping)
                        {
                            var note = kv.Key;
                            buffer.Serialize(ref note);
                        }

                        foreach (var kv in samplesMapping)
                        {
                            kv.Value.Serialize(buffer);
                        }
                    }
                }
            }

            if (buffer.Version < 15 && (IsS5B || IsEpsm))
            {
                envelopes[EnvelopeType.S5BMixer] = new Envelope(EnvelopeType.S5BMixer);
                envelopes[EnvelopeType.S5BNoiseFreq] = new Envelope(EnvelopeType.S5BNoiseFreq);
            }

            if (buffer.IsReading)
            {
                PerformPostLoadActions();
            }

            // Revert back presets to "customs" if they no longer match what the code generates.
            // This is in case we change the code that generates the preset.
            if (buffer.IsReading && !buffer.IsForUndoRedo)
            { 
                if (IsN163 && n163WavPreset != WavePresetType.Custom && !N163WaveformEnvelope.ValidatePreset(EnvelopeType.N163Waveform, n163WavPreset))
                    n163WavPreset = WavePresetType.Custom;
                if (IsFds && fdsWavPreset != WavePresetType.Custom && !FdsWaveformEnvelope.ValidatePreset(EnvelopeType.FdsWaveform, fdsWavPreset))
                    fdsWavPreset = WavePresetType.Custom;
                if (IsFds && fdsModPreset != WavePresetType.Custom && !FdsModulationEnvelope.ValidatePreset(EnvelopeType.FdsModulation, fdsModPreset))
                    fdsModPreset = WavePresetType.Custom;
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

    public static class InstrumentConverter
    {
        private delegate void ConvertDelegate(Instrument src, Instrument dst);
        private static Dictionary<Tuple<int, int>, ConvertDelegate> ConvertMap = new Dictionary<Tuple<int, int>, ConvertDelegate>();

        static InstrumentConverter()
        {
            // There are specialized conversion functions, well be adding more as we go.
            ConvertMap.Add(new Tuple<int, int>(ExpansionType.Fds,  ExpansionType.N163), ConvertFdsToN163);
            ConvertMap.Add(new Tuple<int, int>(ExpansionType.N163, ExpansionType.Fds),  ConvertN163ToFds);
            ConvertMap.Add(new Tuple<int, int>(ExpansionType.S5B,  ExpansionType.EPSM), ConvertS5BToEPSM);
            ConvertMap.Add(new Tuple<int, int>(ExpansionType.EPSM, ExpansionType.S5B),  ConvertEPSMToS5B);
        }

        private static void ConvertEnvelopeValues(Instrument srcInst, Instrument dstInst, int srcType, int dstType, Envelope srcEnv, Envelope dstEnv, int srcLen, int dstLen)
        {
            Envelope.GetMinMaxValueForType(srcInst, srcType, out var srcMin, out var srcMax);
            Envelope.GetMinMaxValueForType(dstInst, dstType, out var dstMin, out var dstMax);

            var srcRange = srcMax - srcMin + 1;
            var dstRange = dstMax - dstMin + 1;

            for (int di = 0; di < dstLen; di++)
            {
                var si = Utils.Clamp((int)Math.Floor(di * (srcLen / (float)dstLen)), 0, srcLen - 1);
                var sv = srcEnv.Values[si];
                var sr = (sv - srcMin) / (float)srcRange;
                var dv = Utils.Clamp((int)Math.Floor(dstMin + sr * dstRange), dstMin, dstMax);

                dstEnv.Values[di] = (sbyte)dv;
            }
        }

        private static void ConvertEnvelopeGeneric(Instrument srcInst, Instrument dstInst, int srcType, int dstType, Envelope srcEnv, Envelope dstEnv)
        {
            Debug.Assert(srcEnv.CanResize   == dstEnv.CanResize);
            Debug.Assert(srcEnv.CanLoop     == dstEnv.CanLoop);
            Debug.Assert(srcEnv.CanRelease  == dstEnv.CanRelease);
            Debug.Assert(srcEnv.ChunkLength == 1); // Anything with chunks should have its own specialized version.
            Debug.Assert(dstEnv.ChunkLength == 1);

            if (dstEnv.CanResize)
                dstEnv.Length = srcEnv.Length;
            if (dstEnv.CanLoop)
                dstEnv.Loop = srcEnv.Loop;
            if (dstEnv.CanRelease)
                dstEnv.Release = srcEnv.Release;

            ConvertEnvelopeValues(srcInst, dstInst, srcType, dstType, srcEnv, dstEnv, srcEnv.Length, dstEnv.Length);
        }

        private static void ConvertGeneric(Instrument srcInst, Instrument dstInst)
        {
            for (var e = 0; e < EnvelopeType.Count; e++)
            {
                var srcEnv = srcInst.Envelopes[e];
                var dstEnv = dstInst.Envelopes[e];

                if (e == EnvelopeType.Pitch)
                    dstEnv.Relative = srcEnv.Relative;

                if (srcEnv != null && dstEnv != null)
                {
                    ConvertEnvelopeGeneric(srcInst, dstInst, e, e, srcEnv, dstEnv);
                }
            }
        }

        private static void ConvertFdsToN163(Instrument src, Instrument dst)
        {
            dst.N163WaveSize = (byte)Envelope.GetEnvelopeMaxLength(EnvelopeType.FdsWaveform);
            dst.N163WavePreset = WavePresetType.Custom;

            ConvertGeneric(src, dst);
            ConvertEnvelopeValues(
                src,
                dst,
                EnvelopeType.FdsWaveform,
                EnvelopeType.N163Waveform,
                src.FdsWaveformEnvelope,
                dst.N163WaveformEnvelope,
                src.FdsWaveformEnvelope.Length,
                dst.N163WaveformEnvelope.Length);
        }

        private static void ConvertN163ToFds(Instrument src, Instrument dst)
        {
            dst.FdsWavePreset = WavePresetType.Custom;
            ConvertGeneric(src, dst);
            ConvertEnvelopeValues(
                src,
                dst,
                EnvelopeType.N163Waveform,
                EnvelopeType.FdsWaveform,
                src.N163WaveformEnvelope,
                dst.FdsWaveformEnvelope,
                src.N163WaveformEnvelope.ChunkLength, // Only first wave.
                dst.FdsWaveformEnvelope.Length);
        }

        private static void ConvertS5BToEPSM(Instrument src, Instrument dst)
        {
            dst.EPSMSquareEnvAutoPitch       = src.S5BEnvAutoPitch;
            dst.EPSMSquareEnvAutoPitchOctave = src.S5BEnvAutoPitchOctave;
            dst.EPSMSquareEnvelopeShape      = src.S5BEnvelopeShape;
            dst.EPSMSquareEnvelopePitch      = src.S5BEnvelopePitch;
            ConvertGeneric(src, dst);
        }

        private static void ConvertEPSMToS5B(Instrument src, Instrument dst)
        {
            dst.S5BEnvAutoPitch       = src.EPSMSquareEnvAutoPitch;
            dst.S5BEnvAutoPitchOctave = src.EPSMSquareEnvAutoPitchOctave;
            dst.S5BEnvelopeShape      = src.EPSMSquareEnvelopeShape;
            dst.S5BEnvelopePitch      = src.EPSMSquareEnvelopePitch;
            ConvertGeneric(src, dst);
        }

        public static void Convert(Instrument src, Instrument dst)
        {
            Debug.Assert(src.Expansion != dst.Expansion);

            if (!ConvertMap.TryGetValue(new Tuple<int, int>(src.Expansion, dst.Expansion), out var convertFunction))
            {
                convertFunction = ConvertGeneric;
            }

            convertFunction(src, dst);
        }
    }
}
