using BepInEx.Logging;
using System;
using System.Diagnostics;
using UnityEngine;

namespace friendlyPMC.Modules
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

        public static void LogTrace(string message)
        {
            #if DEBUG
            var stackTrace = new StackTrace();
            Instance.LogDebug($"[{Time.time}] {message}\nStackTrace:\n{stackTrace}");
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
