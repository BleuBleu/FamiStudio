using System;
using System.Windows.Forms;

namespace FamiStudio
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Theme.Initialize();
            PerformanceCounter.Initialize();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FamiStudioForm(args.Length > 0 ? args[0] : null));
        }
    }
}
