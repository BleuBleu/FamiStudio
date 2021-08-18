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

        public static int ScaleForMainWindow(float val)
        {
            return (int)(val * mainWindowScaling);
        }

        public static int ScaleForDialog(float val)
        {
            return (int)(val * dialogScaling);
        }

        public static void Initialize()
        {
            dialogScaling = (float)Gdk.Display.Default.DefaultScreen.Resolution / 96.0f;

            if (Settings.DpiScaling != 0)
                mainWindowScaling = Utils.Clamp(Settings.DpiScaling / 100.0f, 1, 2);
            else
                mainWindowScaling = Utils.Clamp((int)(dialogScaling * 2.0f) / 2.0f, 1.0f, 2.0f); // Round to 1/2 (so only 100%, 150% and 200%) are supported.

            fontScaling = mainWindowScaling;
        }
    }
}
