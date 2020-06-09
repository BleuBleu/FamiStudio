using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FamiStudio
{
    static class WinUtils
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int RegisterClipboardFormat(string format);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int IsClipboardFormatAvailable(int format);
        [DllImport("user32.dll")]
        private static extern int OpenClipboard(IntPtr hwnd);
        [DllImport("user32.dll", EntryPoint = "GetClipboardData")]
        private static extern IntPtr GetClipboardDataWin32(int wFormat);
        [DllImport("user32.dll", EntryPoint = "GetClipboardFormatNameA")]
        private static extern int GetClipboardFormatName(int wFormat, string lpString, int nMaxCount);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(int wFlags, int dwBytes);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        private static extern int GlobalUnlock(IntPtr hMem);
        [DllImport("kernel32.dll")]
        private static extern int GlobalSize(IntPtr mem);
        [DllImport("user32.dll")]
        private static extern int CloseClipboard();
        [DllImport("user32.dll")]
        private static extern int SetClipboardData(int wFormat, IntPtr hMem);
        [DllImport("user32.dll")]
        private static extern int EmptyClipboard();

        const int CF_TEXT = 1;
        const int GMEM_MOVEABLE = 2;

        private static int clipboardFormat = -1;

        public static void Initialize()
        {
            clipboardFormat = RegisterClipboardFormat("FamiStudio");
        }

        public static void SetClipboardData(byte[] data)
        {
            IntPtr mem = IntPtr.Zero;

            if (data == null)
            { 
                mem = GlobalAlloc(GMEM_MOVEABLE, 0);
            }
            else
            {
                mem = GlobalAlloc(GMEM_MOVEABLE, data == null ? 0 : data.Length);
                var ptr = GlobalLock(mem);
                Marshal.Copy(data, 0, ptr, data.Length);
                GlobalUnlock(mem);
            }

            if (OpenClipboard(IntPtr.Zero) != 0)
            {
                SetClipboardData(clipboardFormat, mem);
                CloseClipboard();
            }
        }

        public static byte[] GetClipboardData(int maxSize)
        {
            byte[] buffer = null;

            if (IsClipboardFormatAvailable(clipboardFormat) != 0)
            {
                if (OpenClipboard(IntPtr.Zero) != 0)
                {
                    var mem = GetClipboardDataWin32(clipboardFormat);
                    if (mem != IntPtr.Zero)
                    {
                        var size = Math.Min(maxSize, GlobalSize(mem));
                        var ptr = GlobalLock(mem);
                        buffer = new byte[size];
                        Marshal.Copy(ptr, buffer, 0, size);
                        GlobalUnlock(mem);
                    }
                    CloseClipboard();
                }
            }

            return buffer;
        }

        public static string GetClipboardString()
        {
            byte[] buffer = null;

            if (IsClipboardFormatAvailable(CF_TEXT) != 0)
            {
                if (OpenClipboard(IntPtr.Zero) != 0)
                {
                    var mem = GetClipboardDataWin32(CF_TEXT);
                    if (mem != IntPtr.Zero)
                    {
                        var size = Math.Min(8192, GlobalSize(mem));
                        var ptr = GlobalLock(mem);
                        buffer = new byte[size];
                        Marshal.Copy(ptr, buffer, 0, size);
                        GlobalUnlock(mem);
                    }
                    CloseClipboard();
                }
            }

            return ASCIIEncoding.ASCII.GetString(buffer);
        }

        public static void ClearClipboardString()
        {
            if (OpenClipboard(IntPtr.Zero) != 0)
            {
                EmptyClipboard();
                CloseClipboard();
            }
        }
    }
}
