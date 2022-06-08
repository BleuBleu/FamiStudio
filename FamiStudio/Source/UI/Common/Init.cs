namespace FamiStudio
{
    public class Init
    {
        public static bool InitializeBaseSystems()
        {
            if (!Platform.Initialize())
                return false;

            Settings.Initialize();

            if (Platform.IsMobile)
                DpiScaling.Initialize();
            
            Theme.Initialize();
            NesApu.Initialize();
            return true;
        }
    }
}
