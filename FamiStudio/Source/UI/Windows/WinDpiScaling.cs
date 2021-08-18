using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    public static class DpiScaling
    {
        private static float mainWindowScaling = 1;
        private static float dialogScaling     = 1;

        public static float MainWindow => mainWindowScaling;
        public static float Font       => mainWindowScaling;
        public static float Dialog     => dialogScaling;

        public static int ScaleCustom(float val, float scale)
        {
            return (int)(scale * mainWindowScaling);
        }

        public static float ScaleCustomFloat(float val, float scale)
        {
            return scale * mainWindowScaling;
        }

        public static int ScaleForMainWindow(float val)
        {
            return (int)(val * mainWindowScaling);
        }

        public static float ScaleForMainWindowFloat(float val)
        {
            return val * mainWindowScaling;
        }

        public static int ScaleForDialog(float val)
        {
            return (int)(val * dialogScaling);
        }

        public static void Initialize()
        {
            var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);

            // For the main window, we only support 1x or 2x.
            dialogScaling = graphics.DpiX / 96.0f;

            if (Settings.DpiScaling != 0)
                mainWindowScaling = Settings.DpiScaling / 100.0f;
            else
                mainWindowScaling = Math.Min(2.0f, (int)(dialogScaling * 2.0f) / 2.0f); // Round to 1/2 (so only 100%, 150% and 200%) are supported.
        }
    }
}
