using System;
using System.Runtime.InteropServices;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    public static partial class Cursors
    {
        private const int XC_fleur = 52;

        // HACK : Ive had some issues on some distros where it fails to resolve the
        // name correctly. Will attempt to use 2 lib names for safety.
        [DllImport("libX11")]
        private extern static uint XCreateFontCursor1(IntPtr display, uint shape);
        [DllImport("libX11.so.6")]
        private extern static uint XCreateFontCursor2(IntPtr display, uint shape);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct GLFWCursorX11
        {
            public IntPtr Dummy;
            public uint   XCursor; // XID = always 32-bit
        };

        private static unsafe IntPtr CreateGLFWCursorLinux(uint cursor)
        {
            if (cursor != 0)
            {
                // TODO : Free that memory when quitting.
                var pc = (GLFWCursorX11*)Marshal.AllocHGlobal(Marshal.SizeOf(typeof(GLFWCursorX11))).ToPointer();
                pc->Dummy = IntPtr.Zero;
                pc->XCursor = cursor;
                return (IntPtr)pc;
            }
            else
            {
                return IntPtr.Zero;
            }
        }

        private static uint LoadLinuxCursor(uint shape)
        {
            var display = glfwGetX11Display();

            try 
            {
                return XCreateFontCursor1(display, shape);
            }
            catch
            {
                try
                {
                    return XCreateFontCursor2(display, shape);
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static void Initialize(float scaling)
        {
            InitializeDesktop(scaling);

            var fleur = CreateGLFWCursorLinux(LoadLinuxCursor(XC_fleur));

            if (fleur != IntPtr.Zero)
            {
                DragCursor = fleur;
                CopyCursor = fleur;
                Move = fleur;
            }
        }
    }
}
