namespace FamiStudio
{
    public class Init
    {
        public static bool InitializeBaseSystems()
        {
            Settings.Initialize();
            
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
