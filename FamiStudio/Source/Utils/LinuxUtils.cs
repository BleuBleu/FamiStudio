using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace FamiStudio
{
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

        [DllImport("libc")] 
        private static extern int prctl(int option, byte[] arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);

        public static void SetProcessName(string name)
        {
            try
            {
                var ret = prctl(15 /* PR_SET_NAME */, Encoding.ASCII.GetBytes(name + "\0"), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (ret == 0)
                    return;
            }
            catch 
            {
            }

            Debug.WriteLine("Error setting process name.");
        }
    }
}
