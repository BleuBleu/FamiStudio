using System;
using System.Runtime.InteropServices;

namespace FamiStudio
{
    public static class Gif
    {
        private const string GifDecDll = Platform.DllStaticLib ? "__Internal" : Platform.DllPrefix + "GifDec" + Platform.DllExtension;

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "GifOpen")]
        public static extern IntPtr Open(IntPtr data, int swap);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "GifGetWidth")]
        public static extern int GetWidth(IntPtr gif);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "GifGetHeight")]
        public static extern int GetHeight(IntPtr gif);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "GifAdvanceFrame")]
        public static extern int AdvanceFrame(IntPtr gif);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "GifRenderFrame")]
        public static extern void RenderFrame(IntPtr gif, IntPtr buffer, int stride, int channels);
        
        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "GifGetFrameDelay")]
        public static extern int GetFrameDelay(IntPtr gif);

        [DllImport(GifDecDll, CallingConvention = CallingConvention.StdCall, EntryPoint = "GifClose")]
        public static extern void Close(IntPtr gif);
    }
}
