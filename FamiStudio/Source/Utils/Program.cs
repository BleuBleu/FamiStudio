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

            if (!PlatformUtils.IsVS2015RuntimeInstalled())
            {
                if (MessageBox.Show("You seem to be missing the VS 2015 C++ Runtime which is required to run FamiStudio, would you like to visit the FamiStudio website for instruction on how to install it?", "Missing Component", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Utils.OpenUrl("https://famistudio.org/doc/#windows");
                }

                return;
            }

            if (!XAudio2Stream.TryDetectXAudio2())
            {
                if (MessageBox.Show("You seem to be missing parts of DirectX which is required to run FamiStudio, would you like to visit the FamiStudio website for instruction on how to install it?", "Missing Component", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Utils.OpenUrl("https://famistudio.org/doc/#windows");
                }

                return;
            }
#endif

            Settings.Load();
            RenderTheme.Initialize();
            PlatformUtils.Initialize();
            FamiStudioTempoUtils.Initialize();
            NesApu.InitializeNoteTables();

#if FAMISTUDIO_WINDOWS
            WinUtils.Initialize();
            PerformanceCounter.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif
#if FAMISTUDIO_LINUX
            LinuxUtils.SetProcessName("FamiStudio");
#endif

            var cli = new CommandLineInterface(args);

            if (!cli.Run())
            {
                var famiStudio = new FamiStudio(args.Length > 0 ? args[0] : null);
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
