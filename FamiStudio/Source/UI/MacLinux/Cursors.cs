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
#if FAMISTUDIO_MACOS
        public static IntPtr Default    = MacUtils.SelRegisterName("arrowCursor");
        public static IntPtr SizeWE     = MacUtils.SelRegisterName("resizeLeftRightCursor");
        public static IntPtr SizeNS     = MacUtils.SelRegisterName("resizeUpDownCursor");
        public static IntPtr DragCursor = MacUtils.SelRegisterName("closedHandCursor");
        public static IntPtr CopyCursor = MacUtils.SelRegisterName("dragCopyCursor");
        public static IntPtr Eyedrop    = MacUtils.SelRegisterName("arrowCursor");
#else
        public static Gdk.Cursor Default;
        public static Gdk.Cursor SizeWE;
        public static Gdk.Cursor SizeNS;
        public static Gdk.Cursor DragCursor;
        public static Gdk.Cursor CopyCursor;
        public static Gdk.Cursor Eyedrop;
#endif

#if FAMISTUDIO_MACOS
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
#else
        private static Gdk.Cursor CreateCursorFromResource(string name, int x, int y)
        {
            var pixbuf = Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.{name}.png");
            return new Gdk.Cursor(Gdk.Display.Default, pixbuf, x, y);
        }
#endif

        public static void Initialize()
        {
#if FAMISTUDIO_LINUX
            Default    = Gdk.Cursor.NewFromName(Gdk.Display.Default, "default");
            SizeWE     = Gdk.Cursor.NewFromName(Gdk.Display.Default, "col-resize");
            SizeNS     = Gdk.Cursor.NewFromName(Gdk.Display.Default, "row-resize");
            DragCursor = Gdk.Cursor.NewFromName(Gdk.Display.Default, "grab");
            CopyCursor = Gdk.Cursor.NewFromName(Gdk.Display.Default, "copy");
            Eyedrop    = CreateCursorFromResource("EyedropCursor", 7, 24);
#endif
        }
    }
}
