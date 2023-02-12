using System;
using System.Collections.Generic;
using System.IO;
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
            var cli = new CommandLineInterface(args);

            if (!InitializeBaseSystems(cli.HasAnythingToDo))
            {
                Environment.Exit(-1);
            }

            if (!cli.Run())
            {
                var fs = new FamiStudio();
                if (!fs.Run(args))
                {
                    Environment.Exit(-1);
                }
            }

            ShutdownBaseSystems();
        }
    }
}
