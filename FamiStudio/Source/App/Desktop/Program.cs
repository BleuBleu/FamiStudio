using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static FamiStudio.Init;
using static GLFWDotNet.GLFW;

namespace FamiStudio
{
    static class Program
    {
        static void WaitForDebugger()
        {
        #if DEBUG && WAIT_FOR_DEBUGGER
            for (int i = 10; i >= 0 && !Debugger.IsAttached; i--)
            {
                Debug.WriteLine("Waiting {i} seconds for debugger to attach ...");
                System.Threading.Thread.Sleep(1000);
            }
        #endif
        }

        [STAThread]
        static void Main(string[] args)
        {
            WaitForDebugger();

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
