namespace FamiStudio
{
    public class Init
    {
        public static bool InitializeBaseSystems()
        {
            Settings.Load();
            
            if (!Platform.Initialize())
                return false;

            if (Platform.IsMobile)
                DpiScaling.Initialize();
            
            Theme.Initialize();
            NesApu.Initialize();
            return true;
        }
    }
}
