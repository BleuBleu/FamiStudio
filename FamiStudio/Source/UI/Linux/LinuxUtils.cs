using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace FamiStudio
{
    // MATTT: This will likely all go away with GLFW.
    public static class LinuxUtils
    {
        [DllImport("libgdk-x11-2.0.so.0")]
        internal static extern uint gdk_x11_drawable_get_xid(IntPtr handle);

        [DllImport("libgdk-x11-2.0.so.0")]
        internal static extern IntPtr gdk_x11_drawable_get_xdisplay(IntPtr handle);

        [DllImport("libX11")]
        public static extern void XLockDisplay(IntPtr display);

        [DllImport("libX11")]
        public static extern void XUnlockDisplay(IntPtr display);

        [DllImport("libX11", EntryPoint = "XReparentWindow")]
        public extern static int XReparentWindow(IntPtr display, IntPtr window, IntPtr parent, int x, int y);

        public static IntPtr GetWindowDisplay(OpenTK.Platform.IWindowInfo windowInfo)
        {
            var x11WindowInfoType = typeof(OpenTK.NativeWindow).Assembly.GetType("OpenTK.Platform.X11.X11WindowInfo");
            return (IntPtr)x11WindowInfoType.GetField("display", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(windowInfo);
        }
    }
}
