using System;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public partial class TutorialDialog
    {
#if FAMISTUDIO_WINDOWS
        private const string GifDecDll = "GifDec.dll";
#elif FAMISTUDIO_MACOS
        private const string GifDecDll = "GifDec.dylib";
#else
        private const string GifDecDll = "GifDec.so";
#endif

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GifOpen(IntPtr data, int swap);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern int GifGetWidth(IntPtr gif);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern int GifGetHeight(IntPtr gif);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern int GifAdvanceFrame(IntPtr gif, IntPtr buffer, int stride);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern int GifGetFrameDelay(IntPtr gif);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall)]
        private static extern void GifClose(IntPtr gif);

        public static readonly string[] TutorialMessages = new[]
        {
            @"(1/9) Welcome to FamiStudio! Let's take a few seconds to review some of the basic controls to make sure you use the app to its fullest.",
            @"(2/9) To PAN around the piano roll or the sequencer, simply PRESS and HOLD the MIDDLE MOUSE BUTTON and DRAG around to smoothly move the viewport. Yes, that wheel on your mouse is also a button!",
            @"(3/9) To ZOOM in and out in the piano roll or the sequencer, simply rotate the mouse wheel.",
            @"(4/9) If you are on a TRACKPAD or a LAPTOP, simply enable TRACKPAD CONTROLS in the settings.",
            @"(5/9) To ADD things like patterns and notes, simply CLICK with the LEFT MOUSE BUTTON.",
            @"(6/9) To DELETE things like patterns, notes, instruments and songs, simply CLICK on them with the RIGHT MOUSE BUTTON.",
            @"(7/9) SNAPPING is ON by default and is expressed in BEATS. With the default settings, it will snap to 1/4 notes. You can change the snapping precision or disable it completely to create notes of different lengths!",
            @"(8/9) Always keep an eye on the TOOLTIPS! They change constantly as you move the mouse and they will teach you how to use the app! For the complete DOCUMENTATION and over 1 hour of VIDEO TUTORIAL, please click on the big QUESTION MARK!",
            @"(9/9) Join us on DISCORD to meet other FamiStudio users and share your songs with them! Link in the documentation (question mark icon in the toolbar)."
        };

        public static readonly string[] TutorialImages = new[]
        {
            "Tutorial0.gif",
            "Tutorial1.gif",
            "Tutorial2.gif",
            "Tutorial3.gif",
            "Tutorial4.gif",
            "Tutorial5.gif",
            "Tutorial6.gif",
            "Tutorial7.gif",
            "Tutorial8.gif",
        };
    }
}
