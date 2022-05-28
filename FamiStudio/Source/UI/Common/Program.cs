using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static GLFWDotNet.GLFW;

using RenderTheme = FamiStudio.ThemeRenderResources;

namespace FamiStudio
{
    static class Program
    {
#if FAMISTUDIO_WINDOWS
        [DllImport("SHCore.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwareness(int awareness);
#endif

        // MATTT : Move somewhere else!
        static IntPtr InitializeGLFW()
        {
            if (glfwInit() == 0)
                return IntPtr.Zero;

            glfwWindowHint(GLFW_CLIENT_API, GLFW_OPENGL_API);
            glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 1);
            glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 2);
            glfwWindowHint(GLFW_MAXIMIZED, 1);

            var window = glfwCreateWindow(640, 480, "FamiStudio", IntPtr.Zero, IntPtr.Zero);
            if (window == IntPtr.Zero)
            {
                glfwTerminate();
                return IntPtr.Zero; 
            }

            glfwMakeContextCurrent(window);

            GL2.Initialize();

            return window;
        }

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

            if (!PlatformUtils.IsVS2019RuntimeInstalled())
            {
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
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
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
                var glfwWindow = InitializeGLFW();
                if (glfwWindow == IntPtr.Zero)
                {
                    MessageBox.Show("Error initializing OpenGL.", "Error", MessageBoxButtons.OK);
                    Environment.Exit(-1);
                }

                var famiStudio = new FamiStudio();
                var form = new FamiStudioForm(famiStudio, glfwWindow);
                famiStudio.Initialize(form, args.Length > 0 ? args[0] : null);
                famiStudio.Run();
            }

#if FAMISTUDIO_LINUX
            // We sometimes gets stuck here on Linux, lets abort.
            Environment.Exit(0);
#endif
        }
    }
}
