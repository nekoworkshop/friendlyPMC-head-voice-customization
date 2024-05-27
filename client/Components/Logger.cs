using BepInEx.Logging;

namespace friendlyPMC.Components
{
    internal class Logger
    {
        public static ManualLogSource Instance;
        public Logger() { 
            Instance = BepInEx.Logging.Logger.CreateLogSource("friendlyPMC");
        }

        public static void LogInfo(string message)
        {
            Instance.LogInfo(message);
        }
    }
}
