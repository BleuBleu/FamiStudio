using System;
using System.Runtime.InteropServices;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    public static partial class Cursors
    {
        private const int XC_fleur = 52;

        [DllImport("libX11")]
        private extern static uint XCreateFontCursor(IntPtr display, uint shape);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct GLFWCursorX11
        {
            public IntPtr Dummy;
            public uint   XCursor; // XID = always 32-bit
        };

        private static unsafe IntPtr CreateGLFWCursorLinux(uint cursor)
        {
            // TODO : Free that memory when quitting.
            var pc = (GLFWCursorX11*)Marshal.AllocHGlobal(Marshal.SizeOf(typeof(GLFWCursorX11))).ToPointer();
            pc->Dummy = IntPtr.Zero;
            pc->XCursor = cursor;
            return (IntPtr)pc;
        }

        private static uint LoadLinuxCursor(uint shape)
        {
            return XCreateFontCursor(glfwGetX11Display(), shape);
        }

        public static void Initialize(float scaling)
        {
            InitializeDesktop(scaling);

            var fleur = glfwCreateStandardCursor(GLFW_RESIZE_ALL_CURSOR);

            DragCursor = fleur;
            CopyCursor = fleur;
            Move = fleur;
        }
    }
}
