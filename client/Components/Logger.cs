using BepInEx.Logging;
using System;
using UnityEngine;

namespace friendlyPMC.Components
{
    internal class Logger
    {
        public static ManualLogSource Instance;
        public Logger()
        {
            Instance = BepInEx.Logging.Logger.CreateLogSource("friendlyPMC");
        }

        public static void LogInfo(string message)
        {
            #if DEBUG
            Instance.LogInfo($"[{Time.time}] " + message);
            #endif
        }

        public static void LogError(string message)
        {
            Instance.LogError(message);
        }
        public static void LogError(Exception error)
        {
            Instance.LogError(error);
        }
    }
}
