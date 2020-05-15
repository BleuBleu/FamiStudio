using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

#if FAMISTUDIO_WINDOWS
    using RenderTheme = FamiStudio.Direct2DTheme;
#else
    using RenderTheme = FamiStudio.GLTheme;
#endif

namespace FamiStudio
{
    static class Program
    {
#if FAMISTUDIO_WINDOWS
        [DllImport("SHCore.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwareness(int awareness);
#endif

        [STAThread]
        static unsafe void Main(string[] args)
        {
#if FAMISTUDIO_WINDOWS
            try
            {
                // This is only supported in Windows 8.1+.
                SetProcessDpiAwareness(1 /*Process_System_DPI_Aware*/);
            }
            catch { }
#endif

            Settings.Load();
            RenderTheme.Initialize();
            PlatformUtils.Initialize();
            Cursors.Initialize();
            ClipboardUtils.Initialize();
            FamiStudioTempoUtils.Initialize();
            NesApu.InitializeNoteTables();

#if FAMISTUDIO_WINDOWS
            PerformanceCounter.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif

            var famiStudio = new FamiStudio(args.Length > 0 ? args[0] : null);
            famiStudio.Run();

            Settings.Save();

#if FAMISTUDIO_LINUX
            // We sometimes gets stuck here on Linux, lets abort.
            Environment.Exit(0);
#endif
        }
    }
}
