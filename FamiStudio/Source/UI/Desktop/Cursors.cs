using System;
using System.Runtime.InteropServices;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    public static class Cursors
    {
        public static IntPtr Default;
        public static IntPtr SizeWE;
        public static IntPtr SizeNS;
        public static IntPtr Move;
        public static IntPtr DragCursor;
        public static IntPtr CopyCursor;
        public static IntPtr Eyedrop;
        public static IntPtr IBeam;
        public static IntPtr Hand;

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
            public IntPtr Cursor;
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

        private static void ScaleHotspot(float scale, ref int hx, ref int hy)
        {
            hx = (int)Math.Round(hx * scale);
            hy = (int)Math.Round(hy * scale);
        }

        private static unsafe IntPtr CreateCursorFromResource(int size, int hotx, int hoty, string name)
        {
            var scaling = size > 32 ? 2 : 1;
            var suffix = scaling == 2 ? "@2x" : "";
            var img = TgaFile.LoadFromResource($"FamiStudio.Resources.{name}{suffix}.tga");

            ScaleHotspot(scaling, ref hotx, ref hoty);

            //if (scaling == 2 && size < 64)
            //{
            //    ScaleHotspot(size / (float)img.GetLength(0), ref hotx, ref hoty);
            //    img = Utils.ResizeBitmap(img, size, size);
            //}
            //else if (scaling == 1 && size < 32)
            //{
            //    ScaleHotspot(size / (float)img.GetLength(0), ref hotx, ref hoty);
            //    img = Utils.ResizeBitmap(img, size, size);
            //}

            fixed (int* p = &img[0, 0])
            {
                var glfwImg = new GLFWimage();
                glfwImg.width  = img.GetLength(1);
                glfwImg.height = img.GetLength(0);
                glfwImg.pixels = (IntPtr)p;
                return glfwCreateCursor(new IntPtr(&glfwImg), hotx, hoty);
            }
        }

        public static void Initialize()
        {
            Default    = glfwCreateStandardCursor(GLFW_ARROW_CURSOR);
            SizeWE     = glfwCreateStandardCursor(GLFW_HRESIZE_CURSOR);
            SizeNS     = glfwCreateStandardCursor(GLFW_VRESIZE_CURSOR);
            DragCursor = glfwCreateStandardCursor(GLFW_HAND_CURSOR);
            CopyCursor = glfwCreateStandardCursor(GLFW_HAND_CURSOR);
            Eyedrop    = glfwCreateStandardCursor(GLFW_ARROW_CURSOR); // MATTT
            IBeam      = glfwCreateStandardCursor(GLFW_IBEAM_CURSOR);
            Hand       = glfwCreateStandardCursor(GLFW_HAND_CURSOR);
            Move       = glfwCreateStandardCursor(GLFW_HAND_CURSOR);

            // Load some better cursors depending on the platform.
            // MATTT : See if its worth re-splitting the files depending on MacOS/Linux.
            if (Platform.IsWindows)
            {
                OleLibrary = LoadLibrary("ole32.dll");
                DragCursorHandle = LoadCursor(OleLibrary, 2);
                CopyCursorHandle = LoadCursor(OleLibrary, 3);

                Move = LoadWindowsCursor(OCR_SIZEALL);
                DragCursor = CreateGLFWCursorWindows(DragCursorHandle);
                CopyCursor = CreateGLFWCursorWindows(CopyCursorHandle);
            }

            var size = Platform.GetCursorSize();
            Eyedrop = CreateCursorFromResource(size, 6, 25, "EyedropCursor");  
        }
    }
}
