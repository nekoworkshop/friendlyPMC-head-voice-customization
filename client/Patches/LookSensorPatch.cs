
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace friendlyPMC.Patches
{
    [HarmonyPatch(typeof(LookSensor))]
    [HarmonyPatch("GInterface10.AIPeriodicUpdate")]
    internal class LookSensorPatch
    {
        [HarmonyPrefix]
        static bool Prefix(LookSensor __instance)
        {
            // Your code to run before the original method
            BotOwner botOwner = AccessTools.Field(typeof(LookSensor), "_botOwner").GetValue(__instance) as BotOwner;
            try
            {
                __instance.UpdateLook();
            } catch(Exception ex) {
                Components.Logger.LogError("AIPeriodicUpdate Error for " + botOwner.Profile.Nickname);
                Components.Logger.LogError(ex);
            }

            return false;
        }
    }
}
