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
            var suffix       = GLTheme.MainWindowScaling > 1 ? "@2x" : "";
            var hotSpotScale = GLTheme.MainWindowScaling > 1 ? 2 : 1;

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"FamiStudio.Resources.{name}{suffix}.png"))
            {
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);

                return MacUtils.CreateCursorFromImage(buffer, x * hotSpotScale, y * hotSpotScale);
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
