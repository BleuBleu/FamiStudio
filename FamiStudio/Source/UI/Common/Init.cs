namespace FamiStudio
{
    public class Init
    {
        public static void InitializeBaseSystems()
        {
            Settings.Load();
            DpiScaling.Initialize();
            PlatformUtils.Initialize();
            Theme.Initialize();
            NesApu.Initialize();
#if FAMISTUDIO_WINDOWS
            WinUtils.Initialize();
            PerformanceCounter.Initialize();
#endif
        }
    }
}
