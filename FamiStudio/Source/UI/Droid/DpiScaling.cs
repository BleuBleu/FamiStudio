using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;

namespace FamiStudio
{
    public static class DpiScaling
    {
        // DROIDTODO : Autodetect.
        private static float mainWindowScaling = 4;
        private static float fontScaling       = 2;
        private static float dialogScaling = 1;

        public static float MainWindowScaling => mainWindowScaling;
        public static float FontScaling       => fontScaling;
        public static float DialogScaling     => dialogScaling;
        
        public static void Initialize()
        {
        }
    }
}
