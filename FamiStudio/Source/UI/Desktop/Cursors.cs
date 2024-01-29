using System;
using System.Runtime.InteropServices;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    public static partial class Cursors
    {
        private static void ScaleHotspot(float scale, ref int hx, ref int hy)
        {
            hx = (int)Math.Round(hx * scale);
            hy = (int)Math.Round(hy * scale);
        }

        private static unsafe IntPtr CreateCursorFromResource(int size, int hotx, int hoty, string name)
        {
            var suffix = size >= 64 ? "64" : 
                         size >= 48 ? "48" : "32";

            var bmp = TgaFile.LoadFromResource($"FamiStudio.Resources.Cursors.{name}{suffix}.tga");

            // Hotspot is always specified with respect to 32x32.
            ScaleHotspot(size / 32.0f, ref hotx, ref hoty);

            fixed (int* p = &bmp.Data[0])
            {
                var glfwImg = new GLFWimage();
                glfwImg.width = bmp.Width;
                glfwImg.height = bmp.Height;
                glfwImg.pixels = (IntPtr)p;
                return glfwCreateCursor(new IntPtr(&glfwImg), hotx, hoty);
            }
        }

        private static void InitializeDesktop(float scaling)
        {
            Default      = glfwCreateStandardCursor(GLFW_ARROW_CURSOR);
            SizeWE       = glfwCreateStandardCursor(GLFW_HRESIZE_CURSOR);
            SizeNS       = glfwCreateStandardCursor(GLFW_VRESIZE_CURSOR);
            DragCursor   = glfwCreateStandardCursor(GLFW_HAND_CURSOR);
            CopyCursor   = glfwCreateStandardCursor(GLFW_HAND_CURSOR);
            IBeam        = glfwCreateStandardCursor(GLFW_IBEAM_CURSOR);
            PointingHand = glfwCreateStandardCursor(GLFW_HAND_CURSOR);
            Move         = glfwCreateStandardCursor(GLFW_HAND_CURSOR);

            var size = Platform.GetCursorSize(scaling);
            Eyedrop = CreateCursorFromResource(size, 6, 25, "CursorEyedrop");
            Eraser  = CreateCursorFromResource(size, 9, 22, "CursorEraser");
        }
    }
}
