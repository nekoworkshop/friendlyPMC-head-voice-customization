
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace friendlyPMC.Patches
{
    internal class LookSensorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LookSensor), "AIPeriodicUpdate");
        }
        [PatchPrefix]
        private static bool PatchPrefix(LookSensor __instance)
        {
            try
            {
                __instance.UpdateLook();
            }
            catch (Exception ex)
            {
                BotOwner botOwner = (BotOwner)AccessTools.Field(typeof(LookSensor), "_botOwner").GetValue(__instance);

                Components.Logger.LogError("AIPeriodicUpdate Error for Bot " + botOwner.Profile.Nickname);
                Components.Logger.LogError(ex);
            }
            return false;
        }
    }
}
