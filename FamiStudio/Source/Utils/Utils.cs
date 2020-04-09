using System;
using System.Linq;

namespace FamiStudio
{
    static class Utils
    {
        public static int Clamp(int val, int min, int max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        public static float Clamp(float val, float min, float max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }

        public static float Lerp(float v0, float v1, float alpha)
        {
            return v0 * (1.0f - alpha) + v1 * alpha;
        }

        public static int SignedCeil(float x)
        {
            return (x > 0) ? (int)Math.Ceiling(x) : (int)Math.Floor(x);
        }

        public static int SignedFloor(float x)
        {
            return (x < 0) ? (int)Math.Ceiling(x) : (int)Math.Floor(x);
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            T t = a;
            a = b;
            b = t;
        }

        public static int NextPowerOfTwo(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;

            return v;
        }

        public static int NumberOfSetBits(int i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        public static string MakeNiceAsmName(string name)
        {
            string niceName = "";
            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c))
                    niceName += char.ToLower(c);
                else if (char.IsWhiteSpace(c) && niceName.Last() != '_')
                    niceName += '_';
                else if (c == '_' || c == '-')
                    niceName += c;
            }
            return niceName;
        }

        public static string[] GetPalSkipFrameString(int noteLength, int skipPattern, out bool[] bools)
        {
            var strings = new string[noteLength];
            bools = new bool[noteLength];
            for (int i = 0; i < noteLength; i++)
            {
                strings[i] = i.ToString();
                bools[i] = (skipPattern & (1 << i)) != 0;
            }

            return strings;
        }

        public static int GetPalSkipFrameBits(bool[] bools)
        {
            var palSkipPattern = 0;
            for (int i = 0; i < bools.Length; i++)
            {
                if (bools[i])
                    palSkipPattern |= (1 << i);
            }
            return palSkipPattern;
        }
    }
}
