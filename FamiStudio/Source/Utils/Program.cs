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
        [STAThread]
        static unsafe void Main(string[] args)
        {
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
        }
    }
}
