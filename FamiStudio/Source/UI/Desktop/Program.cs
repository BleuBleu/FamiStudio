using System;
using System.Runtime.InteropServices;
using static FamiStudio.Init;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (!InitializeBaseSystems())
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
