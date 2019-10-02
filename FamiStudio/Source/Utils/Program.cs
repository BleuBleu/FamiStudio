using System;
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
            SetProcessDpiAwareness(1 /*Process_System_DPI_Aware*/);
#endif

            Settings.Load();
            Cursors.Initialize();
            RenderTheme.Initialize();
            PlatformDialogs.Initialize();

#if FAMISTUDIO_WINDOWS
            PerformanceCounter.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif

            var famiStudio = new FamiStudio(args.Length > 0 ? args[0] : null);
            famiStudio.Run();

            Settings.Save();
        }
    }
}
