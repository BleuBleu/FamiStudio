using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    class Cursors
    {
        public static IntPtr Default;
        public static IntPtr SizeWE;
        public static IntPtr SizeNS;
        public static IntPtr DragCursor;
        public static IntPtr CopyCursor;
        public static IntPtr Eyedrop;

        private static IntPtr CreateCursorFromResource(string name, int x, int y)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var suffix = GLTheme.MainWindowScaling > 1 ? "@2x" : "";
            using (var stream = assembly.GetManifestResourceStream($"FamiStudio.Resources.{name}{suffix}.png"))
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                // MATTT: 2x hotspot on retina!!
                return MacUtils.CreateCursorFromImage(buffer, x, y);
            }
        }

        public static void Initialize()
        {
            Default    = MacUtils.GetCursorByName("arrowCursor");
            SizeWE     = MacUtils.GetCursorByName("resizeLeftRightCursor");
            SizeNS     = MacUtils.GetCursorByName("resizeUpDownCursor");
            DragCursor = MacUtils.GetCursorByName("closedHandCursor");
            CopyCursor = MacUtils.GetCursorByName("dragCopyCursor");
            Eyedrop    = CreateCursorFromResource("EyedropCursor", 7, 24);
        }
    }
}
