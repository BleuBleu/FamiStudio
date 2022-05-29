namespace FamiStudio
{
    public class Init
    {
        public static bool InitializeBaseSystems()
        {
            Settings.Load();
            
            if (!PlatformUtils.Initialize())
                return false;

            if (PlatformUtils.IsMobile)
                DpiScaling.Initialize();
            
            Theme.Initialize();
            NesApu.Initialize();
            return true;
        }
    }
}
