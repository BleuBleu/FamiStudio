using System;
using System.Diagnostics;
using System.Threading;

namespace FamiStudio
{
    public static class DpiScaling
    {
        private static bool initialized; 

        private static float windowScaling = 1;
        private static float fontScaling   = 1;

        // HACK : This is thread-local since the video export (which uses this feature) runs on a
        // different thread on mobile and could interfere with the scaling of the main UI.
        private static ThreadLocal<bool> forceUnitScale = new ThreadLocal<bool>();

        public static bool IsInitialized => initialized;

        public static float Window { get { Debug.Assert(initialized); return forceUnitScale.Value ? 1.0f : windowScaling; } }
        public static float Font   { get { Debug.Assert(initialized); return forceUnitScale.Value ? 1.0f : fontScaling; } }
        
        public static bool ForceUnitScaling { get => forceUnitScale.Value; set => forceUnitScale.Value = value; }

        public static int ScaleCustom(float val, float scale)
        {
            Debug.Assert(initialized);
            return (int)Math.Round(val * scale);
        }

        public static float ScaleCustomFloat(float val, float scale)
        {
            Debug.Assert(initialized);
            return val * scale;
        }

        public static int ScaleForWindow(float val)
        {
            Debug.Assert(initialized);
            return (int)Math.Round(val * Window);
        }

        public static float ScaleForWindowFloat(float val)
        {
            Debug.Assert(initialized);
            return val * Window;
        }

        public static int ScaleForFont(float val)
        {
            Debug.Assert(initialized);
            return (int)Math.Round(val * Font);
        }

        public static float ScaleForFontFloat(float val)
        {
            Debug.Assert(initialized);
            return val * Font;
        }

        public static int[] GetAvailableScalings()
        {
            if (Platform.IsWindows || Platform.IsLinux)
            {
                return new[] { 100, 125, 150, 175, 200, 225, 250 };
            }
            else if (Platform.IsMacOS)
            {
                return new int[0]; // Intentional, we dont allow to manually set the scaling on MacOS.
            }
            else if (Platform.IsMobile)
            {
                if (Platform.IsAndroid)
                {
                    return new[] { 66, 100, 133 };
                }
                else
                {
                    return new[] { 64, 76, 88, 100, 112, 124, 140 };
                }
            }


            Debug.Assert(false);
            return new int[] { };
        }

        private static float RoundScaling(float value)
        {
            if (Platform.IsMacOS)
            {
                return value;
            }
            else
            {
                var scalings = GetAvailableScalings();
                var minDiff  = 100.0f;
                var minIndex = -1;

                for (int i = 0; i < scalings.Length; i++)
                {
                    var diff = Math.Abs(scalings[i] / 100.0f - value);
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        minIndex = i;
                    }
                }
                return scalings[minIndex] / 100.0f;
            }
        }

        public static void Initialize(float scaling = -1.0f)
        {
            if (Platform.IsMobile)
            {
                if (Settings.DpiScaling != 0)
                {
                    windowScaling = Settings.DpiScaling / 100.0f;
                }
                else if (Platform.IsAndroid)
                {
                    var density = Platform.GetPixelDensity();

                    if (density < 360)
                        windowScaling = 0.666f;
                    else if (density >= 480)
                        windowScaling = 1.333f;
                    else
                        windowScaling = 1.0f;
                }
                else 
                {
                    var res = Platform.GetScreenResolution();
                    windowScaling = Math.Min(1.0f, Math.Min(res.Width, res.Height) / 1080.0f);
                }

                if (Platform.IsAndroid)
                {
                    fontScaling   = (float)Math.Round(windowScaling * 3);
                    windowScaling = (float)Math.Round(windowScaling * 6);
                }
                else
                {
                    fontScaling   = windowScaling * 3.0f;
                    windowScaling = windowScaling * 6.0f;
                }
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
