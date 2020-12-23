using System;
using System.Diagnostics;

namespace FamiStudio
{
    public class Envelope
    {
        public const int Volume = 0;
        public const int Arpeggio = 1;
        public const int Pitch = 2;
        public const int DutyCycle = 3;
        public const int RegularCount = 4;
        public const int FdsWaveform = 4;
        public const int FdsModulation = 5;
        public const int N163Waveform = 6;
        public const int Count = 7;
        public static readonly string[] EnvelopeNames = { "Volume", "Arpeggio", "Pitch", "Duty Cycle", "FDS Waveform", "FDS Modulation Table", "N163 Waveform" };
        public static readonly string[] EnvelopeShortNames = { "Volume", "Arpeggio", "Pitch", "DutyCycle", "FDSWave", "FDSMod", "N163Wave" };

        public const byte WavePresetSine     = 0;
        public const byte WavePresetTriangle = 1;
        public const byte WavePresetSawtooth = 2;
        public const byte WavePresetSquare50 = 3;
        public const byte WavePresetSquare25 = 4;
        public const byte WavePresetFlat     = 5;
        public const byte WavePresetCustom   = 6;
        public const byte WavePresetMax      = 7;
        public static readonly string[] PresetNames = { "Sine", "Triangle", "Sawtooth", "Square 50%", "Square 25%", "Flat", "Custom" };

        sbyte[] values;
        int length;
        int loop = -1;
        int release = -1;
        int maxLength = 256;
        bool relative = false;
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
            if (type == FdsWaveform)
                maxLength = 64;
            else if (type == FdsModulation)
                maxLength = 32;
            else 
                maxLength = 256;

            values = new sbyte[maxLength];
            canResize = type != FdsWaveform && type != FdsModulation && type != N163Waveform;
            canRelease = type == Volume;
            canLoop = type <= DutyCycle;

            if (canResize)
            {
                // Give envelope a default size, more intuitive.
                length = type == DutyCycle ? 1 : 8;
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

        public int Loop
        {
            get { return loop; }
            set
            {
                if (value >= 0 && canLoop)
                {
                    if (release >= 0)
                        loop = Utils.Clamp(value, 0, release - 1);
                    else
                        loop = Math.Min(value, maxLength);

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
                        release = Utils.Clamp(value, loop + 1, maxLength);

                    if (release >= length)
                        release = -1;
                }
                else
                {
                    release = -1;
                }
            }
        }

        public int MaxLength
        {
            get { return maxLength; }
            set
            {
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

        public byte[] BuildN163Waveform()
        {
            var packed = new byte[length / 2];

            for (int i = 0; i < length; i += 2)
                packed[i / 2] = (byte)((byte)(values[i + 1] << 4) | (byte)(values[i + 0]));

            return packed;
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

        public void Truncate()
        {
            if (relative || !canResize || length == 0 || release >= 0)
                return;

            if (loop >= 0)
            {
                // Looping envelope can be optimized if they all have the same value.
                for (int i = 1; i < length; i++)
                {
                    if (values[i] != values[0])
                        return;
                }

                length = 1;
            }
            else
            {
                // Non looping envelope can be optimized by removing all the trailing values that have the same value.
                int i = length - 2;
                for (; i >= 0 && i > release; i--)
                {
                    if (values[i] != values[i + 1])
                        break;
                }

                length = i + 2;
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

            var env = new Envelope(Envelope.Pitch);

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
            GetMinMaxValue(null, type, out var min, out var max);

            switch (preset)
            {
                case WavePresetSine:
                    for (int i = 0; i < length; i++)
                        values[i] = (sbyte)Math.Round(Utils.Lerp(min, max, (float)Math.Sin(i * 2.0f * Math.PI / length) * -0.5f + 0.5f));
                    break;
                case WavePresetTriangle:
                    for (int i = 0; i < length / 2; i++)
                    {
                        values[i] = (sbyte)Math.Round(Utils.Lerp(min, max, i / (float)(length / 2 - 1)));
                        values[length - i - 1] = values[i];
                    }
                    break;
                case WavePresetSawtooth:
                    for (int i = 0; i < length; i++)
                        values[i] = (sbyte)Math.Round(Utils.Lerp(min, max, i / (float)(length - 1)));
                    break;
                case WavePresetSquare50:
                    for (int i = 0; i < length; i++)
                        values[i] = (sbyte)(i >= length / 2 ? max : min);
                    break;
                case WavePresetSquare25:
                    for (int i = 0; i < length; i++)
                        values[i] = (sbyte)(i >= length / 4 ? max : min);
                    break;
                case WavePresetFlat:
                    for (int i = 0; i < length; i++)
                        values[i] = (sbyte)((min + max) / 2);
                    break;
            }
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
                crc = CRC32.Compute(values, crc);
                return crc;
            }
        }

        public bool IsEmpty
        {
            get
            {
                if (!canResize)
                    return false;

                if (length == 0)
                    return true;

                foreach (var val in values)
                {
                    if (val != 0)
                        return false;
                }

                return true;
            }
        }

        public static void GetMinMaxValue(Instrument instrument, int type, out int min, out int max)
        {
            if (type == Volume)
            {
                min = 0;
                max = 15;
            }
            else if (type == DutyCycle)
            {
                min = 0;
                max = instrument.ExpansionType == Project.ExpansionVrc6 ? 7 : 3;
            }
            else if (type == FdsWaveform)
            {
                min = 0;
                max = 63;
            }
            else if (type == N163Waveform)
            {
                min = 0;
                max = 15;
            }
            else
            {
                min = -64;
                max =  63;
            }
        }

        public void SerializeState(ProjectBuffer buffer)
        {
            buffer.Serialize(ref length);
            buffer.Serialize(ref loop);
            if (buffer.Version >= 3)
                buffer.Serialize(ref release);
            if (buffer.Version >= 4)
                buffer.Serialize(ref relative);
            buffer.Serialize(ref values);
        }
    }
}
