using System;
using System.Runtime.InteropServices;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    public static partial class Cursors
    {
        private static IntPtr OleLibrary;
        private static IntPtr DragCursorHandle;
        private static IntPtr CopyCursorHandle;

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);
        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursor(IntPtr hInstance, UInt16 lpCursorName);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadImage(IntPtr hinst, IntPtr name, uint type, int sx, int sy, uint load);

        private const int IMAGE_CURSOR = 2;
        private const int OCR_SIZEALL = 32646;
        private const int LR_DEFAULTSIZE = 0x0040;
        private const int LR_SHARED = 0x8000;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct GLFWCursorWindows
        {
            public IntPtr Dummy;
            public IntPtr Cursor; // HCURSOR = 32/64 bit.
        };

        private static unsafe IntPtr CreateGLFWCursorWindows(IntPtr cursor)
        {
            // TODO : Free that memory when quitting.
            var pc = (GLFWCursorWindows*)Marshal.AllocHGlobal(Marshal.SizeOf(typeof(GLFWCursorWindows))).ToPointer();
            pc->Dummy = IntPtr.Zero;
            pc->Cursor = cursor;
            return (IntPtr)pc;
        }

        private static IntPtr LoadWindowsCursor(int type)
        {
            return CreateGLFWCursorWindows(LoadImage(IntPtr.Zero, (IntPtr)type, IMAGE_CURSOR, 0, 0, LR_SHARED | LR_DEFAULTSIZE));
        }

        public static void Initialize(float scaling)
        {
            InitializeDesktop(scaling);

            OleLibrary = LoadLibrary("ole32.dll");
            DragCursorHandle = LoadCursor(OleLibrary, 2);
            CopyCursorHandle = LoadCursor(OleLibrary, 3);

            Move = LoadWindowsCursor(OCR_SIZEALL);
            DragCursor = CreateGLFWCursorWindows(DragCursorHandle);
            CopyCursor = CreateGLFWCursorWindows(CopyCursorHandle);
        }
    }
}
