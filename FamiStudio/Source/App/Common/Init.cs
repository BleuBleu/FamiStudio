namespace FamiStudio
{
    public class Init
    {
        public static bool InitializeBaseSystems(bool commandLine = false)
        {
            if (!Platform.Initialize(commandLine))
                return false;

            Settings.Initialize();

            if (!commandLine)
            {
                if (Platform.IsMobile)
                    DpiScaling.Initialize();

                Theme.Initialize();
            }

            return true;
        }

        public static void ShutdownBaseSystems()
        {
            Platform.Shutdown();
        }
    }
}
