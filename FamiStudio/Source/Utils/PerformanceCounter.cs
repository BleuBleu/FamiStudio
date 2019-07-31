using System.Runtime.InteropServices;

namespace FamiStudio
{
    public static class PerformanceCounter
    {
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        static long freq = 1;

        public static void Initialize()
        {
            QueryPerformanceFrequency(out freq);
        }

        public static double TimeSeconds()
        {
            long time;
            QueryPerformanceCounter(out time);
            return time / (double)freq;
        }
    }
}
