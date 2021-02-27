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
        public static Gdk.Cursor Default;
        public static Gdk.Cursor SizeWE;
        public static Gdk.Cursor SizeNS;
        public static Gdk.Cursor DragCursor;
        public static Gdk.Cursor CopyCursor;
        public static Gdk.Cursor Eyedrop;

        private static Gdk.Cursor CreateCursorFromResource(string name, int x, int y)
        {
            var pixbuf = Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.{name}.png");
            return new Gdk.Cursor(Gdk.Display.Default, pixbuf, x, y);
        }

        public static void Initialize()
        {
            Default    = Gdk.Cursor.NewFromName(Gdk.Display.Default, "default");
            SizeWE     = Gdk.Cursor.NewFromName(Gdk.Display.Default, "col-resize");
            SizeNS     = Gdk.Cursor.NewFromName(Gdk.Display.Default, "row-resize");
            DragCursor = Gdk.Cursor.NewFromName(Gdk.Display.Default, "grab");
            CopyCursor = Gdk.Cursor.NewFromName(Gdk.Display.Default, "copy");
            Eyedrop    = CreateCursorFromResource("EyedropCursor", 7, 24);
        }
    }
}
