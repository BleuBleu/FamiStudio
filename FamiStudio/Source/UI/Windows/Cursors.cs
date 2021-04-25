using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FamiStudio
{
    public static class Cursors
    {
        public static Cursor Default    = System.Windows.Forms.Cursors.Default;
        public static Cursor SizeWE     = System.Windows.Forms.Cursors.SizeWE;
        public static Cursor SizeNS     = System.Windows.Forms.Cursors.SizeNS;
        public static Cursor Move       = System.Windows.Forms.Cursors.SizeAll;
        public static Cursor DragCursor = System.Windows.Forms.Cursors.Default;
        public static Cursor CopyCursor = System.Windows.Forms.Cursors.Default;
        public static Cursor Eyedrop    = System.Windows.Forms.Cursors.Default;

        private static IntPtr OleLibrary;
        private static IntPtr DragCursorHandle;
        private static IntPtr CopyCursorHandle;

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);
        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursor(IntPtr hInstance, UInt16 lpCursorName);

        public static void Initialize()
        {
            OleLibrary = LoadLibrary("ole32.dll");
            DragCursorHandle = LoadCursor(OleLibrary, 2);
            CopyCursorHandle = LoadCursor(OleLibrary, 3);
            DragCursor = new Cursor(DragCursorHandle);
            CopyCursor = new Cursor(CopyCursorHandle);
            Eyedrop = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("FamiStudio.Resources.Eyedrop.cur"));
        }
    }
}
