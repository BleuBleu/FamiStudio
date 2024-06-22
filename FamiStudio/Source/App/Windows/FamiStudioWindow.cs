using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    public partial class FamiStudioWindow
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int nIndex, IntPtr newProc);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int nIndex, WndProcDelegate newProc);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr proc, IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

        [DllImport("user32.dll")]
        static extern IntPtr SetTimer(IntPtr hwnd, IntPtr evt, uint elapse, TimerProcDelegate func);

        [DllImport("user32.dll")]
        static extern IntPtr KillTimer(IntPtr hwnd, IntPtr evt);

        private const int GWL_WNDPROC = -4;
        private const int WM_ENTERSIZEMOVE = 0x231;
        private const int WM_EXITSIZEMOVE = 0x232;
        private const int WM_TIMER = 0x113;

        private delegate IntPtr WndProcDelegate(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);
        private delegate void TimerProcDelegate(IntPtr hwnd, int msg, int id, int time);

        private TimerProcDelegate timerProc;
        private WndProcDelegate newWndProc;
        private IntPtr oldWndProc;
        private bool inTimerProc;

        private void SubclassWindow(bool enable)
        {
            if (Platform.IsWindows)
            {
                var hwnd = Handle;

                if (enable)
                {
                    Debug.Assert(newWndProc == null);
                    newWndProc = new WndProcDelegate(WndProc);
                    oldWndProc = GetWindowLongPtr(hwnd, GWL_WNDPROC);
                    SetWindowLongPtr(hwnd, GWL_WNDPROC, newWndProc);
                }
                else
                {
                    Debug.Assert(oldWndProc != IntPtr.Zero);
                    SetWindowLongPtr(hwnd, GWL_WNDPROC, oldWndProc);
                }
            }
        }

        private void TimerProc(IntPtr hwnd, int msg, int id, int time)
        {
            // Prevent recursion.
            if (!inTimerProc)
            {
                inTimerProc = true;
                RunIteration();
                inTimerProc = false;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam)
        {
            if (msg == WM_ENTERSIZEMOVE) // WM_ENTERSIZEMOVE 
            {
                Debug.Assert(timerProc == null);
                timerProc = new TimerProcDelegate(TimerProc);
                SetTimer(hwnd, (IntPtr)1, 4, timerProc);
            }
            else if (msg == WM_EXITSIZEMOVE) // WM_EXITSIZEMOVE
            {
                Debug.Assert(timerProc != null);
                timerProc = null;
                KillTimer(hwnd, (IntPtr)1);
            }

            return CallWindowProc(oldWndProc, hwnd, msg, wparam, lparam);
        }

        [DllImport("DwmApi")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, int[] attrValue, int attrSize);

        protected void EnableWindowsDarkTheme()
        {
            if (Platform.IsWindows)
            {
                IntPtr handle = glfwGetNativeWindow(window);

                // From https://stackoverflow.com/questions/57124243/winforms-dark-title-bar-on-windows-10
                try
                {
                    if (DwmSetWindowAttribute(handle, 19, new[] { 1 }, 4) != 0)
                        DwmSetWindowAttribute(handle, 20, new[] { 1 }, 4);
                }
                catch
                {
                    // Will likely fail on Win7/8.
                }
            }
        }

        private void PlatformWindowInitialize()
        {
            SubclassWindow(true);
            EnableWindowsDarkTheme();
        }

        private void PlatformWindowShudown()
        {
            SubclassWindow(false);
        }

        private void ProcessPlatformEvents()
        {
        }
    }
}
