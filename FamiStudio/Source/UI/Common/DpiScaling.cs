using System;
using System.Diagnostics;

namespace FamiStudio
{
    public static class DpiScaling
    {
        private static bool initialized; 

        private static float windowScaling = 1;
        private static float fontScaling   = 1;

        public static float Window { get { Debug.Assert(initialized); return windowScaling; } }
        public static float Font   { get { Debug.Assert(initialized); return fontScaling; } }

        public static int ScaleCustom(float val, float scale)
        {
            Debug.Assert(initialized);
            return (int)Math.Round(scale * windowScaling);
        }

        public static float ScaleCustomFloat(float val, float scale)
        {
            Debug.Assert(initialized);
            return scale * windowScaling;
        }

        public static int ScaleForWindow(float val)
        {
            Debug.Assert(initialized);
            return (int)Math.Round(val * windowScaling);
        }

        public static float ScaleForWindowFloat(float val)
        {
            Debug.Assert(initialized);
            return val * windowScaling;
        }

        public static int[] GetAvailableScalings()
        {
            if (Platform.IsWindows || Platform.IsLinux)
                return new[] { 100, 150, 200 };
            else if (Platform.IsAndroid)
                return new[] { 66, 100, 133 };
            else if (Platform.IsMacOS)
                return new int[0]; // Intentional, we dont allow to manually set the scaling on MacOS.

            Debug.Assert(false);
            return new int[] { };
        }

        private static float RoundScaling(float value)
        {
            // Round to 1/2 (so only 100%, 150% and 200%) are supported.
            return Math.Min(2.0f, (int)(value * 2.0f) / 2.0f);
        }

        public static void Initialize(float scaling = -1.0f)
        {
            if (Platform.IsMobile)
            {
                var density = Platform.GetPixelDensity();

                if (Settings.DpiScaling != 0)
                {
                    windowScaling = Settings.DpiScaling / 100.0f;
                }
                else
                {
                    if (density < 360)
                        windowScaling = 0.666f;
                    else if (density >= 480)
                        windowScaling = 1.333f;
                    else
                        windowScaling = 1.0f;
                }

                fontScaling   = (float)Math.Round(windowScaling * 3);
                windowScaling = (float)Math.Round(windowScaling * 6);
            }
            else
            {
                if (Settings.DpiScaling != 0)
                    windowScaling = RoundScaling(Settings.DpiScaling / 100.0f);
                else
                    windowScaling = RoundScaling(scaling);

                fontScaling = windowScaling;
            }

            initialized = true;
        }
    }
}
