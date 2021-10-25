namespace FamiStudio
{
    public class Init
    {
        public static void InitializeBaseSystems()
        {
            Settings.Load();
            PlatformUtils.Initialize();
            DpiScaling.Initialize();
            Theme.Initialize();
            NesApu.Initialize();
#if FAMISTUDIO_WINDOWS
            WinUtils.Initialize();
            PerformanceCounter.Initialize();
#endif
        }
    }
}
