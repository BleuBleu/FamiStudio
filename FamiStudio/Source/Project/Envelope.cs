using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class Envelope
    {
        sbyte[] values;
        int length;
        int loop = -1;
        int release = -1;
        bool relative = false;

        // These are replicated from instrument.
        int maxLength = 256;
        int chunkLength = 1;

        // These are static, depend on type.
        bool canResize;
        bool canLoop;
        bool canRelease;

        public sbyte[] Values => values;
        public bool CanResize => canResize;
        public bool CanLoop => canLoop;
        public bool CanRelease => canRelease;

        private Envelope()
        {
        }

        public Envelope(int type)
        {
            maxLength = GetEnvelopeMaxLength(type);
            values = new sbyte[maxLength];
            canResize = type != EnvelopeType.FdsModulation && type != EnvelopeType.FdsWaveform;
            canRelease = type == EnvelopeType.Volume || type == EnvelopeType.WaveformRepeat || type == EnvelopeType.N163Waveform;
            canLoop = type <= EnvelopeType.DutyCycle || type == EnvelopeType.WaveformRepeat || type == EnvelopeType.N163Waveform || type == EnvelopeType.YMNoiseFreq || type == EnvelopeType.YMMixerSettings;
            chunkLength = type == EnvelopeType.N163Waveform ? 16 : 1;

            if (canResize)
            {
                ResetToDefault(type);
            }
            else
            {
                length = maxLength;
            }
        }

        public int Length
        {
            get { return length; }
            set
            {
                if (canResize)
                    length = Utils.Clamp(value, 0, maxLength); 
                if (loop >= length)
                    loop = -1;
                if (release >= length)
                    release = -1;
            }
        }

        public int ChunkLength
        {
            get { return chunkLength; }
            set
            {
                var prevChunkLength = chunkLength;
                var prevLoop = loop;
                var prevRelease = release;

                chunkLength = value;

                if (prevLoop >= 0)
                    loop = prevLoop / prevChunkLength * chunkLength;
                if (prevRelease >= 0)
                    release = prevRelease / prevChunkLength * chunkLength;
            }
        }

        public int ChunkCount
        { 
            get
            {
                return length / chunkLength;
            }
        }

        public void ResetToDefault(int type)
        {
            var def = GetEnvelopeDefaultValue(type);

            if (chunkLength > 1)
                length = chunkLength;
            else if (canResize)
                length = type == EnvelopeType.DutyCycle || type == EnvelopeType.Volume ? 1 : 8;

            for (int i = 0; i < values.Length; i++)
                values[i] = def;

            loop = -1;
            release = -1;
        }

        public int Loop
        {
            get { return loop; }
            set
            {
                if (value >= 0 && canLoop)
                {
                    if (release >= 0)
                        loop = Utils.Clamp(value, 0, release - chunkLength);
                    else
                        loop = Math.Min(value, length);

                    if (loop >= length)
                        loop = -1;
                }
                else
                {
                    loop = -1;
                    release = -1;
                }
            }
        }

        public int Release
        {
            get { return release; }
            set
            {
                if (value >= 0 && canRelease)
                {
                    if (loop >= 0)
                        release = Utils.Clamp(value, loop + chunkLength, length);

                    if (release >= length)
                        release = -1;
                }
                else
                {
                    release = -1;
                }
            }
        }

        public void SetChunkMaxLengthUnsafe(int c, int m)
        {
            chunkLength = c;
            maxLength = m;
        }

        public void SetLoopReleaseUnsafe(int l, int r)
        {
            loop = l;
            release = r;
        }

        public int MaxLength
        {
            get { return maxLength; }
            set
            {
                Debug.Assert(value <= values.Length);

                maxLength = value;
                if (!canResize)
                    length = maxLength;
                else if (length > maxLength)
                    length = maxLength;
            }
        }

        static readonly sbyte[] FdsModulationDeltas = new sbyte[] { 0, 1, 2, 4, 0, -4, -2, -1 };

        public void ConvertFdsModulationToAbsolute()
        {
            ConvertFdsModulationToAbsolute(Values);
        }

        public static void ConvertFdsModulationToAbsolute(sbyte[] array)
        {
            // Force starting at zero.
            array[0] = 0;

            for (int i = 1; i < 32; i++)
            {
                if (array[i] == 4)
                    array[i] = 0;
                else
                    array[i] = (sbyte)(array[i - 1] + FdsModulationDeltas[array[i]] * 2);
            }
        }

        public sbyte[] BuildFdsModulationTable()
        {
            // FDS modulation table is encoded on 3 bits, each value is a delta
            // with the exception of 4 which is a reset to zero. Since the 
            // table is written in pair, these deltas are actually double.

            // 0 =  0
            // 1 = +1
            // 2 = +2
            // 3 = +4
            // 4 = reset to 0
            // 5 = -4
            // 6 = -2
            // 7 = -1

            var mod = new sbyte[length];
            var prev = 0;

            // Force starting at zero.
            mod[0] = 4;

            for (int i = 1; i < length; i++)
            {
                int val = values[i];

                if (val == 0)
                {
                    mod[i] = 4; // Reset to zero.
                    prev   = 0;
                }
                else
                {
                    // Find best delta (greedy algorithm, probably not best approximation of curve).
                    int minDiff = 99999;

                    for (int j = 0; j < FdsModulationDeltas.Length; j++)
                    {
                        if (j == 4) continue;

                        // Assume the delta will be applied twice.
                        var newVal = prev + FdsModulationDeltas[j] * 2;
                        var diff = Math.Abs(newVal - val);
                        if (diff < minDiff && newVal >= -64 && newVal <= 63)
                        {
                            minDiff = diff;
                            mod[i] = (sbyte)j;
                        }
                    }

                    prev = prev + FdsModulationDeltas[mod[i]] * 2;
                }
            }

            return mod;
        }

        public byte[] GetN163Waveform(int waveIndex)
        {
            var len = chunkLength;
            var offset = waveIndex * chunkLength;
            var packed = new byte[len / 2];

            for (int i = 0; i < len; i += 2)
                packed[i / 2] = (byte)((byte)(values[offset + i + 1] << 4) | (byte)(values[offset + i + 0]));

            return packed;
        }

        public byte[] GetChunk(int chunkIndex)
        {
            var len = chunkLength;
            var offset = chunkIndex * chunkLength;
            var wav = new byte[len];

            for (int i = 0; i < len; i++)
                wav[i] = (byte)values[offset + i];

            return wav;
        }

        public Envelope CreateWaveIndexEnvelope()
        {
            var sum = 0;
            for (int i = 0; i < length; i++)
                sum += values[i];

            // Create a dummy envelope.
            var env = new Envelope(EnvelopeType.Count);
            Array.Resize(ref env.values, sum);
            env.length = sum;
            env.maxLength = sum;

            var idx = 0;

            for (int i = 0; i < length; i++)
            {
                if (i == loop)
                    env.loop = idx;
                if (i == release)
                    env.release = idx;
                for (int j = 0; j < values[i]; j++)
                    env.values[idx++] = (sbyte)i;
            }

            return env;
        }

        public bool Relative
        {
            get { return relative; }
            set { relative = value; }
        }

        public void ConvertToAbsolute()
        {
            // Pitch are absolute in Famitone.
            sbyte val = 0;
            for (int j = 0; j < length; j++)
            {
                val += values[j];
                val = Math.Min((sbyte)63, val);
                val = Math.Max((sbyte)-64, val);
                values[j] = val;
            }
        }

        public void ConvertToRelative()
        {
            for (int j = length - 1; j > 0; j--)
                values[j] = (sbyte)(values[j] - values[j - 1]);
        }

        public static bool AreIdentical(int type, Envelope e1, Envelope e2)
        {
            if (e1.length    != e2.length   ||
                e1.loop      != e2.loop     ||
                e1.release   != e2.release  ||
                e1.relative  != e2.relative ||
                e1.maxLength != e2.maxLength)
            {
                return false;
            }

            for (var i = 0; i < e1.length; i++)
            {
                if (e1.values[i] != e2.values[i])
                    return false;
            }
            
            // These are implied by the type, so they should always match.
            Debug.Assert(
                e1.maxLength   == e2.maxLength   &&
                e1.chunkLength == e2.chunkLength &&
                e1.canResize   == e2.canResize   &&
                e1.canLoop     == e2.canLoop     &&
                e1.canRelease  == e2.canRelease);

            return true;
        }

        public void Optimize()
        {
            if (length == 0|| chunkLength > 1 || !canResize)
            {
                return;
            }

            if (AllValuesEqual(values[0]))
            {
                loop = -1;
                release = -1;
                Length = 1;
                return;
            }

            // TODO : We can optimize more here. We should do it in 2 step.
            // 1) Shorten loop (if any) if its all the same values.
            // 2) Remove identical trailing values (start at release if any)

            if (loop >= 0)
            {
                // Looping envelope can be optimized if they all have the same value.
                for (int i = 1; i < length; i++)
                {
                    if (values[i] != values[0])
                        return;
                }

                Length = 1;
            }
            else
            {
                // Non looping envelope (or envelope with releases) can be optimized
                // by removing all the trailing values that have the same value.
                int i = length - 2;
                for (; i >= 0 && i > release; i--)
                {
                    if (values[i] != values[i + 1])
                        break;
                }

                Length = i + 2;
            }
        }

        public Envelope ShallowClone()
        {
            var env = new Envelope();
            env.length = length;
            env.loop = loop;
            env.release = release;
            env.relative = relative;
            env.maxLength = maxLength;
            env.chunkLength = chunkLength;
            env.canResize = canResize;
            env.values = values.Clone() as sbyte[];
            return env;
        }

        // Based on FamiTracker, but simplified to use regular envelopes.
        static readonly int[] VibratoSpeedLookup = new[] { 0, 64, 32, 21, 16, 13, 11, 9, 8, 7, 6, 5, 4 };
        static readonly int[] VibratoDepthLookup = new[] { 0x01, 0x03, 0x05, 0x07, 0x09, 0x0d, 0x13, 0x17, 0x1b, 0x21, 0x2b, 0x3b, 0x57, 0x7f, 0xbf, 0xff };

        public static Envelope CreateVibratoEnvelope(int speed, int depth)
        {
            Debug.Assert(
                speed >= 0 && speed <= Note.VibratoSpeedMax && 
                depth >= 0 && depth <= Note.VibratoDepthMax);

            var env = new Envelope(EnvelopeType.Pitch);

            if (speed == 0 || depth == 0)
            {
                env.Length = 127;
                env.Loop = 0;
            }
            else
            {
                env.Length = VibratoSpeedLookup[speed];
                env.Loop = 0;

                for (int i = 0; i < env.Length; i++)
                    env.Values[i] = (sbyte)Math.Round(-Math.Sin(i * 2.0f * Math.PI / env.Length) * (VibratoDepthLookup[depth] / 2));
            }

            return env;
        }

        public void SetFromPreset(int type, int preset)
        {
            GetMinMaxValueForType(null, type, out var min, out var max);

            Debug.Assert(length % chunkLength == 0);

            var localChunkLength = chunkLength == 1 ? length : chunkLength;
            var localChunkCount  = length / localChunkLength;

            for (int j = 0; j < localChunkCount; j++)
            {
                var chunkOffset = j * localChunkLength;

                switch (preset)
                {
                    case WavePresetType.Sine:
                        for (int i = 0; i < localChunkLength; i++)
                            values[chunkOffset + i] = (sbyte)Math.Round(Utils.Lerp(min, max, (float)Math.Sin(i * 2.0f * Math.PI / localChunkLength) * -0.5f + 0.5f));
                        break;
                    case WavePresetType.Triangle:
                        for (int i = 0; i < localChunkLength / 2; i++)
                        {
                            values[chunkOffset + i] = (sbyte)Math.Round(Utils.Lerp(min, max, i / (float)(localChunkLength / 2 - 1)));
                            values[chunkOffset + localChunkLength - i - 1] = values[i];
                        }
                        break;
                    case WavePresetType.Sawtooth:
                        for (int i = 0; i < localChunkLength; i++)
                            values[chunkOffset + i] = (sbyte)Math.Round(Utils.Lerp(min, max, i / (float)(localChunkLength - 1)));
                        break;
                    case WavePresetType.Square50:
                        for (int i = 0; i < localChunkLength; i++)
                            values[chunkOffset + i] = (sbyte)(i >= localChunkLength / 2 ? max : min);
                        break;
                    case WavePresetType.Square25:
                        for (int i = 0; i < localChunkLength; i++)
                            values[chunkOffset + i] = (sbyte)(i >= localChunkLength / 4 ? max : min);
                        break;
                    case WavePresetType.Flat:
                        for (int i = 0; i < localChunkLength; i++)
                            values[chunkOffset + i] = (sbyte)Math.Round((min + max) / 2.0);
                        break;
                    case WavePresetType.PWM:
                        for (int i = 0; i < localChunkLength; i++)
                            values[chunkOffset + i] = (sbyte)(i >= (localChunkLength / 2) + Math.Abs(localChunkLength / 2 - (1 + ((j + 1) * (localChunkLength - 2) / localChunkCount))) ? max : min);
                        break;
                    case WavePresetType.PWM2:
                        for (int i = 0; i < localChunkLength; i++)
                            values[chunkOffset + i] = (sbyte)(i >= (localChunkLength / 2) + (((j + 1) * (localChunkLength - 2) / localChunkCount) / 2) ? max : min);
                        break;
                    case WavePresetType.PWM3:
                        for (int i = 0; i < localChunkLength; i++)
                            values[chunkOffset + i] = (sbyte)(i >= (localChunkLength / 2) + (localChunkLength / 2 - (1 + ((j + 1) * (localChunkLength - 2) / localChunkCount) / 2)) ? max : min);
                        break;
                }
            }
        }

        public bool ValidatePreset(int type, int preset)
        {
            var oldValues = values.Clone() as sbyte[];
            SetFromPreset(type, preset);
            var matches = true;

            for (int i = 0; i < length; i++)
            {
                if (oldValues[i] != values[i])
                {
                    matches = false;
                    break;
                }
            }

            values = oldValues;
            return matches;
        }

        public uint CRC
        {
            get
            {
                uint crc = 0;
                crc = CRC32.Compute(BitConverter.GetBytes(length), crc);
                crc = CRC32.Compute(BitConverter.GetBytes(loop), crc);
                crc = CRC32.Compute(BitConverter.GetBytes(release), crc);
                crc = CRC32.Compute(BitConverter.GetBytes(relative), crc);
                crc = CRC32.Compute(BitConverter.GetBytes(maxLength), crc);
                crc = CRC32.Compute(BitConverter.GetBytes(chunkLength), crc);
                crc = CRC32.Compute(values, crc);
                return crc;
            }
        }

        public static sbyte GetEnvelopeZeroValue(int type)
        {
            switch (type)
            {
                case EnvelopeType.Volume:
                    return (sbyte)15;
                case EnvelopeType.YMMixerSettings:
                    return (sbyte)2;
                default:
                    return (sbyte)0;
            }
        }

        public static int GetEnvelopeMaxLength(int type)
        {
            switch (type)
            {
                case EnvelopeType.FdsWaveform:
                    return 64;
                case EnvelopeType.N163Waveform:
                    return 1024; // Actually includes multiple waveforms.
                case EnvelopeType.FdsModulation:
                    return 32;
                case EnvelopeType.WaveformRepeat:
                    return 64;
                default:
                    return 256;
            }
        }

        public static sbyte GetEnvelopeDefaultValue(int type)
        {
            switch (type)
            {
                case EnvelopeType.Volume:
                    return Note.VolumeMax;
                case EnvelopeType.WaveformRepeat:
                    return 1;
                case EnvelopeType.FdsWaveform:
                    return 32;
                case EnvelopeType.YMMixerSettings:
                    return 2;
                default:
                    return 0;
            }
        }

        public bool IsEmpty(int type)
        {
            if (!canResize)
                return false;

            if (length == 0)
                return true;

            return AllValuesEqual(GetEnvelopeZeroValue(type));
        }

        public bool AllValuesEqual(sbyte val)
        {
            for (int i = 0; i < length; i++)
            {
                if (values[i] != val)
                    return false;
            }

            return true;
        }

        public bool GetMinMaxValue(out int min, out int max)
        {
            min = int.MaxValue;
            max = int.MinValue;

            if (length > 0)
            {
                for (int i = 0; i < length; i++)
                {
                    min = Math.Min(min, values[i]);
                    max = Math.Max(max, values[i]);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public static void GetMinMaxValueForType(Instrument instrument, int type, out int min, out int max)
        {
            if (type == EnvelopeType.Volume || type == EnvelopeType.N163Waveform)
            {
                min = 0;
                max = 15;
            }
            else if (type == EnvelopeType.DutyCycle)
            {
                min = 0;
                max = instrument.IsVrc6 ? 7 : 3;
            }
            else if (type == EnvelopeType.FdsWaveform)
            {
                min = 0;
                max = 63;
            }
            else if (type == EnvelopeType.WaveformRepeat)
            {
                min = 1;
                max = 15; // Arbitrary.
            }
            else if (type == EnvelopeType.YMMixerSettings)
            {
                min = 0;
                max = 2;
            }
            else if (type == EnvelopeType.YMNoiseFreq)
            {
                min = 0;
                max = 31;
            }
            else
            {
                min = -64;
                max =  63;
            }
        }

        public static string GetDisplayValue(Instrument instrument, int type, int value)
        {
            if (type == EnvelopeType.YMMixerSettings)
            {
                switch (value)
                {
                    case 0: return "N+T";
                    case 1: return "N";
                    case 2: return "T";
                }
            }
            else if (type == EnvelopeType.YMNoiseFreq)
            {
                if (value == 0)
                    return "NOP";
            }
            else if (type == EnvelopeType.DutyCycle)
            {
                if (instrument.IsVrc6)
                {
                    switch (value)
                    {
                        case 0: return "6.25%";
                        case 1: return "12.5%";
                        case 2: return "18.75%";
                        case 3: return "25%";
                        case 4: return "31.25%";
                        case 5: return "37.5%";
                        case 6: return "43.75%";
                        case 7: return "50%";
                    }
                }
                else
                {
                    switch (value)
                    {
                        case 0: return "12.5%";
                        case 1: return "25%";
                        case 2: return "50%";
                        case 3: return "-25%";
                    }
                }
            }
            else if (type == EnvelopeType.Arpeggio)
            {
                return value.ToString("+#;-#;0");
            }

            return value.ToString();
        }

        public bool ValuesInValidRange(Instrument instrument, int type)
        {
            GetMinMaxValueForType(instrument, type, out var min, out var max);

            for (int i = 0; i < length; i++)
            {
                if (values[i] < min || values[i] > max)
                    return false;
            }

            return true;
        }

        public void ClampToValidRange(Instrument instrument, int type)
        {
            GetMinMaxValueForType(instrument, type, out var min, out var max);

            for (int i = 0; i < length; i++)
                values[i] = (sbyte)Utils.Clamp(values[i], min, max);
        }

        private void FixBadEnvelopeLength(int type)
        {
            var maxTypeLength = GetEnvelopeMaxLength(type);

            if (values.Length != maxTypeLength)
                Array.Resize(ref values, maxTypeLength);
        }

        public void Serialize(ProjectBuffer buffer, int type)
        {
            buffer.Serialize(ref length);
            buffer.Serialize(ref loop);
            if (buffer.Version >= 3)
                buffer.Serialize(ref release);
            if (buffer.Version >= 4)
                buffer.Serialize(ref relative);
            buffer.Serialize(ref values);

            if (buffer.IsReading && !buffer.IsForUndoRedo)
                FixBadEnvelopeLength(type);
        }
    }

    public static class EnvelopeType
    {
        public const int Volume         = 0;
        public const int Arpeggio       = 1;
        public const int Pitch          = 2;
        public const int DutyCycle      = 3;
        public const int RegularCount   = 4;
        public const int FdsWaveform    = 4;
        public const int FdsModulation  = 5;
        public const int N163Waveform   = 6;
        public const int WaveformRepeat = 7;
        public const int YMMixerSettings= 8;
        public const int YMNoiseFreq    = 9;
        public const int Count          = 10;

        // Use these to display to user
        public static readonly LocalizedString[] LocalizedNames = new LocalizedString[Count];

        // Use these to save in files, etc.
        public static readonly string[] InternalNames =
        {
            "Volume",
            "Arpeggio",
            "Pitch",
            "DutyCycle",
            "FDSWave",
            "FDSMod",
            "N163Wave",
            "Repeat",
            "MixerSettings",
            "NoiseFreq",
        };

        public static readonly string[] Icons = new string[]
        {
            "EnvelopeVolume",
            "EnvelopeArpeggio",
            "EnvelopePitch",
            "EnvelopeDuty",
            "EnvelopeWave",
            "EnvelopeMod",
            "EnvelopeWave",
            "EnvelopeWave", // Never actually displayed
            "EnvelopeMixer",
            "EnvelopeNoise", 
        };

        static EnvelopeType()
        {
            Localization.LocalizeStatic(typeof(EnvelopeType));
        }

        public static int GetValueForInternalName(string str)
        {
            return Array.IndexOf(InternalNames, str);
        }
    }

    public static class WavePresetType
    {
        public const int Sine            = 0;
        public const int Triangle        = 1;
        public const int Sawtooth        = 2;
        public const int Square50        = 3;
        public const int Square25        = 4;
        public const int Flat            = 5;
        public const int Custom          = 6;
        public const int CountNoResample = 7;
        public const int Resample        = 7;
        public const int CountNoPWM      = 8;
        public const int PWM             = 8; 
        public const int PWM2            = 9; 
        public const int PWM3            = 10;
        public const int Count           = 11;

        // Use these to display to user
        public static LocalizedString[] LocalizedNames = new LocalizedString[Count];

        // Use these to save in files, etc.
        public static readonly string[] InternalNames =
        {
            "Sine",
            "Triangle",
            "Sawtooth",
            "Square50%",
            "Square25%",
            "Flat",
            "Custom",
            "Resample",
            "PWM",
            "PWM2",
            "PWM3"
        };

        static WavePresetType()
        {
            Localization.LocalizeStatic(typeof(WavePresetType));
        }

        public static int GetValueForInternalName(string str)
        {
            return Array.IndexOf(InternalNames, str);
        }
    }
}
