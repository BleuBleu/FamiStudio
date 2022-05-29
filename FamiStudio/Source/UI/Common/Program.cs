using System;
using System.Runtime.InteropServices;
using static FamiStudio.Init;

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
            // MATTT : I think GLFW calls this internally : Check!
            try
            {
                // This is only supported in Windows 8.1+.
                SetProcessDpiAwareness(1 /*Process_System_DPI_Aware*/);
            }
            catch { }

#endif

            InitializeBaseSystems();

            // MATTT : This will go away, i think?
            //System.Windows.Forms.Application.EnableVisualStyles(); // MATTT : THIS SEEM TO HAVE AN IMPACT!!!
            //Application.SetCompatibleTextRenderingDefault(false);

            var cli = new CommandLineInterface(args);
            if (!cli.Run())
            {
                var fs = new FamiStudio();
                if (!fs.Run(args))
                {
                    Environment.Exit(-1);
                }
            }

            // We sometimes gets stuck here on Linux (likely a thread that we dont control still running), lets abort.
            if (PlatformUtils.IsLinux)
            {
                Environment.Exit(0);
            }
        }
    }
}
