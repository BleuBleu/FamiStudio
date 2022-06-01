using System;
using System.Runtime.InteropServices;
using static FamiStudio.Init;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    static class Program
    {
        // MATTT : Shutdown too!
        static bool InitializeGLFW()
        {
            if (glfwInit() == 0)
            {
                // MATTT : We will need a "low level" message box if we ever roll out our own.
                Platform.MessageBox("Error initializing GLFW.", "Error", MessageBoxButtons.OK);
                return false;
            }

            return true;
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (!InitializeGLFW() ||
                !InitializeBaseSystems())
            {
                Environment.Exit(-1);
            }

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
            if (Platform.IsLinux)
            {
                Environment.Exit(0);
            }
        }
    }
}
