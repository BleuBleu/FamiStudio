using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public static float Frac(float x)
        {
            return x - (int)x;
        }

        public static int IntegerPow(int x, int y)
        {
            int result = 1;
            for (long i = 0; i < y; i++)
                result *= x;
            return result;
        }

        public static int RoundDownAndClamp(int x, int factor, int min)
        {
            return Math.Max((x & ~(factor- 1)), min);
        }

        public static int RoundUpAndClamp(int x, int factor, int max)
        {
            return Math.Min((x + factor - 1) & ~(factor - 1), max);
        }

        public static int DivideAndRoundUp(int x, int y)
        {
            return (x + y - 1) / y;
        }

        public static int NumDecimalDigits(int n)
        {
            int digits = 1;
            while (n >= 10)
            {
                n /= 10;
                digits++;
            }
            return digits;
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

        static readonly byte[] BitLookups = new byte[]
        {
            0x0, 0x8, 0x4, 0xc, 0x2, 0xa, 0x6, 0xe,
            0x1, 0x9, 0x5, 0xd, 0x3, 0xb, 0x7, 0xf
        };

        public static byte ReverseBits(byte b)
        {
            return (byte)((BitLookups[b & 0xf] << 4) | BitLookups[b >> 4]);
        }

        public static void ReverseBits(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = ReverseBits(bytes[i]);
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

        public static int[] GetFactors(int n, int start)
        {
            var factors = new List<int>();

            for (int i = start; i >= 2; i--)
            {
                if (n % i == 0)
                    factors.Add(i);
            }

            return factors.ToArray();
        }

        public static void DisposeAndNullify<T>(ref T obj) where T : IDisposable
        {
            if (obj != null)
            {
                obj.Dispose();
                obj = default(T);
            }
        }

        public static int[] GetFactors(int n)
        {
            return GetFactors(n, n);
        }

        public static string AddFileSuffix(string filename, string suffix)
        {
            var extension = Path.GetExtension(filename);
            var filenameNoExtension = filename.Substring(0, filename.Length - extension.Length);

            return filenameNoExtension + suffix + extension;
        }

        public static float SmoothStep(float t)
        {
            return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
        }

        public static string GetTemporaryDiretory()
        {
            var tempFolder = Path.Combine(Path.GetTempPath(), "FamiStudio");

            try
            {
                Directory.Delete(tempFolder, true);
            }
            catch { }

            Directory.CreateDirectory(tempFolder);
            return tempFolder;
        }

        public static void OpenUrl(string url)
        {
            try
            {
#if FAMISTUDIO_LINUX
                Process.Start("xdg-open", url);
#elif FAMISTUDIO_MACOS
                Process.Start("open", url);
#else
                Process.Start(url);
#endif
            }
            catch { }
        }
    }
}
