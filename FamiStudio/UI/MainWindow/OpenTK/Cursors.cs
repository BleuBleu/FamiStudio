using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
#else
        public static Gdk.Cursor Default;
        public static Gdk.Cursor SizeWE;
        public static Gdk.Cursor SizeNS;
        public static Gdk.Cursor DragCursor;
        public static Gdk.Cursor CopyCursor;
#endif

        public static void Initialize()
        {
#if FAMISTUDIO_LINUX
            Default    = Gdk.Cursor.NewFromName(Gdk.Display.Default, "default");
            SizeWE     = Gdk.Cursor.NewFromName(Gdk.Display.Default, "col-resize");
            SizeNS     = Gdk.Cursor.NewFromName(Gdk.Display.Default, "row-resize");
            DragCursor = Gdk.Cursor.NewFromName(Gdk.Display.Default, "grab");
            CopyCursor = Gdk.Cursor.NewFromName(Gdk.Display.Default, "copy");
#endif
        }
    }
}
