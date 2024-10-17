
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

using friendlyPMC.Modules;
using System.Collections.Generic;

namespace friendlyPMC.Patches
{
    [HarmonyPatch(typeof(LookSensor))]
    [HarmonyPatch("GInterface10.AIPeriodicUpdate")]
    internal class LookSensorPatch
    {
        private static Dictionary<string,float> _switch = new Dictionary<string, float>();

        [HarmonyPrefix]
        static bool Prefix(LookSensor __instance)
        {
            try
            {
                BotOwner botOwner = AccessTools.Field(typeof(LookSensor), "_botOwner").GetValue(__instance) as BotOwner;
                // we are only interested in followers
                if (!BossPlayers.IsFollower(botOwner))
                {
                    __instance.UpdateLook();
                    return false;
                }

                BifacialTransform _weaponRootTransform = AccessTools.Field(typeof(LookSensor), "_weaponRootTransform").GetValue(__instance) as BifacialTransform;
                
                if(_switch.ContainsKey(botOwner.ProfileId))
                {
                    if(Time.time - _switch[botOwner.ProfileId] < 3f)
                    {
                        __instance.UpdateLook();
                        return false;
                    }
                    // reset to original weapon root
                    try
                    {
                        AccessTools.Field(typeof(LookSensor), "_weaponRootTransform").SetValue(__instance, botOwner.Fireport);
                        _switch.Remove(botOwner.ProfileId);
                    } catch
                    {
                    }
                }

                try
                {
                    // attempt to see if the weapon root is good
                    Vector3 _weaponRootPoint = _weaponRootTransform.position;
                }
                catch
                {
                    // attempt to reset it if there is an issue
                    try
                    {
                        Vector3 checkpoint = botOwner.Fireport.position; // check
                        AccessTools.Field(typeof(LookSensor), "_weaponRootTransform").SetValue(__instance, botOwner.Fireport);
                    }
                    catch
                    {
                        // - else switch to alternative weapon root
                        AccessTools.Field(typeof(LookSensor), "_weaponRootTransform").SetValue(__instance, botOwner.WeaponRoot);
                        _switch[botOwner.ProfileId] = Time.time;
                    }
                }

                __instance.UpdateLook();

            } catch(Exception ex) {
                Modules.Logger.LogInfo("LookSensor AIPeriodicUpdate Error");
                Modules.Logger.LogInfo(ex.StackTrace);
            }

            return false;
        }
   

        public static void FlushSwitches()
        {
            _switch.Clear();
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
                if(source == null || source.transform == null) return false;
                float maxDistance = source.MaxDistance;
                float value = Vector3.Distance(source.transform.position, listenerPos);
                __result = Mathf.InverseLerp(0f, maxDistance, value);
            }
            catch (Exception ex)
            {
                Logger.LogInfo("GClass974 Error");
                Logger.LogInfo(ex.StackTrace);
                __result = 0.5f;
            }

            return false;
        }
    }
}
