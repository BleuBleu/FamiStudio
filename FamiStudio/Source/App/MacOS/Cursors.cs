using System;
using System.Runtime.InteropServices;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    public static partial class Cursors
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct GLFWCursorMacOS
        {
            public IntPtr Dummy;
            public IntPtr NSCursor; // NSCursor = 64-bit
        }

        private static unsafe IntPtr LoadMacOSCursor(string name)
        {
            return MacUtils.GetCursorByName(name);
        }

        private static unsafe IntPtr CreateGLFWCursorMacOS(IntPtr nsCursor)
        {
            // TODO : Free that memory when quitting.
            var pc = (GLFWCursorMacOS*)Marshal.AllocHGlobal(Marshal.SizeOf(typeof(GLFWCursorMacOS))).ToPointer();
            pc->Dummy = IntPtr.Zero;
            pc->NSCursor = nsCursor;
            return (IntPtr)pc;
        }

        public static void Initialize(float scaling)
        {
            InitializeDesktop(scaling);

            DragCursor = CreateGLFWCursorMacOS(LoadMacOSCursor("closedHandCursor"));
            CopyCursor = CreateGLFWCursorMacOS(LoadMacOSCursor("dragCopyCursor"));

            var size = Platform.GetCursorSize(scaling);
            Move = CreateCursorFromResource(size, 15, 15, "CursorMove");
        }
    }
}
