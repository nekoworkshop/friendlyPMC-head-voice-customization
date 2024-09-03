
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace friendlyPMC.Patches
{
    [HarmonyPatch(typeof(LookSensor))]
    [HarmonyPatch("GInterface10.AIPeriodicUpdate")]
    internal class LookSensorPatch
    {
        [HarmonyPrefix]
        static bool Prefix(LookSensor __instance)
        {
            BotOwner botOwner = AccessTools.Field(typeof(LookSensor), "_botOwner").GetValue(__instance) as BotOwner;
            // @TODO : figure out out why it triggers error for followers in some circumstances
            try
            {
                // prevent UpdateLook to run when bot is still using the medecine (seems to be the cause of the error?)
                if (botOwner.Medecine.Using) return false;
                __instance.UpdateLook();
            } catch(Exception ex) {
                Components.Logger.LogInfo("AIPeriodicUpdate Error for " + botOwner.Profile.Nickname);
                Components.Logger.LogInfo(ex.StackTrace);
            }

            return false;
        }
    }

    internal class GClass974Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass974), "method_7");
        }

        [PatchPrefix]
        private static bool PatchPrefix(GClass974 __instance, ref float __result, Vector3 listenerPos, BetterSource source)
        {
            // @TODO : figure out out why it triggers error for followers in some circumstances
            try
            {
                float maxDistance = source.MaxDistance;
                float value = Vector3.Distance(source.transform.position, listenerPos);
                __result = Mathf.InverseLerp(0f, maxDistance, value);
            }
            catch (Exception ex)
            {
                Components.Logger.LogInfo("GClass974 Error");
                Components.Logger.LogInfo(ex.StackTrace);
                __result = 0.5f;
            }

            return false;
        }
    }
}
