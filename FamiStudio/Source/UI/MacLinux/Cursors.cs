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
        public static Gdk.Cursor Default    = null;
        public static Gdk.Cursor SizeWE     = null;
        public static Gdk.Cursor SizeNS     = null;
        public static Gdk.Cursor DragCursor = null;
        public static Gdk.Cursor CopyCursor = null;
        public static Gdk.Cursor Eyedrop    = null;

        private static Gdk.Cursor CreateCursorFromResource(string name, int x, int y)
        {
            var pixbuf = Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.{name}.png");
            return new Gdk.Cursor(Gdk.Display.Default, pixbuf, x, y);
        }

#if FAMISTUDIO_MACOS
        private static unsafe Gdk.Cursor CreateMacOSNamedCursor(string name)
        {
            var nsCursor = MacUtils.GetCursorByName(name);
            var gdkCursor = new Gdk.Cursor(Gdk.CursorType.Cross);

            // HACK : Patch the Gdk internal struct with our NSCursor.
            // struct is :
            //   - 4 byte type
            //   - 4 byte ref count
            //   - 8 bytes NSCursor pointer.
            IntPtr* p = (IntPtr*)gdkCursor.Handle.ToPointer();
            p[1] = nsCursor;

            return gdkCursor;
        }
#endif

        public static void Initialize()
        {
#if FAMISTUDIO_LINUX
            Default = Gdk.Cursor.NewFromName(Gdk.Display.Default, "default");
            DragCursor = Gdk.Cursor.NewFromName(Gdk.Display.Default, "grab");
            CopyCursor = Gdk.Cursor.NewFromName(Gdk.Display.Default, "copy");
#else
            DragCursor = CreateMacOSNamedCursor("closedHandCursor");
            CopyCursor = CreateMacOSNamedCursor("dragCopyCursor");
#endif
            SizeWE = new Gdk.Cursor(Gdk.CursorType.SbHDoubleArrow);
            SizeNS = new Gdk.Cursor(Gdk.CursorType.SbVDoubleArrow);
            Eyedrop = CreateCursorFromResource("EyedropCursor", 7, 24);
        }
    }
}
