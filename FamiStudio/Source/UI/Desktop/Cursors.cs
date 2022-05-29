using System;
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

        public static void Initialize()
        {
            Default = glfwCreateStandardCursor(GLFW_ARROW_CURSOR);
            SizeWE = glfwCreateStandardCursor(GLFW_HRESIZE_CURSOR);
            SizeNS = glfwCreateStandardCursor(GLFW_VRESIZE_CURSOR);
            Move = glfwCreateStandardCursor(GLFW_HAND_CURSOR);
            DragCursor = glfwCreateStandardCursor(GLFW_HAND_CURSOR);
            CopyCursor = glfwCreateStandardCursor(GLFW_HAND_CURSOR);
            Eyedrop = glfwCreateStandardCursor(GLFW_ARROW_CURSOR); // MATTT
            IBeam = glfwCreateStandardCursor(GLFW_IBEAM_CURSOR);
            Hand = glfwCreateStandardCursor(GLFW_HAND_CURSOR);

            // MATT
            //Eyedrop = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("FamiStudio.Resources.Eyedrop.cur"));
        }
    }
}
