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
        public static IntPtr Default    = IntPtr.Zero;
#endif

        public static void Initialize()
        {
        }
    }
}
