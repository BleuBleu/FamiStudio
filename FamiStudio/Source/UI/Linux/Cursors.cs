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
        public static Gdk.Cursor Move      = null;

        private static Gdk.Cursor CreateCursorFromResource(string name, int x, int y)
        {
            var pixbuf = Gdk.Pixbuf.LoadFromResource($"FamiStudio.Resources.{name}.png");
            return new Gdk.Cursor(Gdk.Display.Default, pixbuf, x, y);
        }

        public static void Initialize()
        {
            Default = Gdk.Cursor.NewFromName(Gdk.Display.Default, "default");
            DragCursor = Gdk.Cursor.NewFromName(Gdk.Display.Default, "grab");
            CopyCursor = Gdk.Cursor.NewFromName(Gdk.Display.Default, "copy");
            Move = Gdk.Cursor.NewFromName(Gdk.Display.Default, "move");
            SizeWE = new Gdk.Cursor(Gdk.CursorType.SbHDoubleArrow);
            SizeNS = new Gdk.Cursor(Gdk.CursorType.SbVDoubleArrow);
            Eyedrop = CreateCursorFromResource("EyedropCursor", 7, 24);
        }
    }
}
