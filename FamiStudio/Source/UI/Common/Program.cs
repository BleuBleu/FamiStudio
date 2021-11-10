using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using RenderTheme = FamiStudio.ThemeRenderResources;

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

            if (!PlatformUtils.IsVS2015RuntimeInstalled())
            {
                // MATTT : Update this message + link to 2019 runtime!
                if (MessageBox.Show("You seem to be missing the VS 2019 C++ Runtime which is required to run FamiStudio, would you like to visit the FamiStudio website for instruction on how to install it?", "Missing Component", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    PlatformUtils.OpenUrl("https://famistudio.org/doc/install/#windows");
                }

                return;
            }

            if (!XAudio2Stream.TryDetectXAudio2())
            {
                if (MessageBox.Show("You seem to be missing parts of DirectX which is required to run FamiStudio, would you like to visit the FamiStudio website for instruction on how to install it?", "Missing Component", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    PlatformUtils.OpenUrl("https://famistudio.org/doc/install/#windows");
                }

                return;
            }
#endif

            Init.InitializeBaseSystems();

#if FAMISTUDIO_WINDOWS
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#if !DEBUG
            if (Settings.IsPortableMode)
                WinUtils.AssociateExtension(".fms", Assembly.GetExecutingAssembly().Location, "FamiStudio Project", "FamiStudio Project");
#endif
#elif FAMISTUDIO_LINUX
            LinuxUtils.SetProcessName("FamiStudio");
#endif

            var cli = new CommandLineInterface(args);

            if (!cli.Run())
            {
                var famiStudio = new FamiStudio();
                famiStudio.Initialize(args.Length > 0 ? args[0] : null);
                famiStudio.Run();
            }

            Settings.Save();

#if FAMISTUDIO_LINUX
            // We sometimes gets stuck here on Linux, lets abort.
            Environment.Exit(0);
#endif
        }
    }
}
