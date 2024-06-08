using Aki.Reflection.Patching;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Patches
{
    internal class GetClosePointsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {

            return AccessTools.Method(typeof(CoverPointMaster), nameof(CoverPointMaster.GetClosePoints));
        }

        [PatchPrefix]
        public static bool Prefix(CoverPointMaster __instance, Vector3 pos, BotOwner bot, float dist, ref List<CustomNavigationPoint> __result)
        {
            
            return true;
        }
    }

    internal class GetFreeClosePointPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {

            return AccessTools.Method(typeof(CoverPointMaster), nameof(CoverPointMaster.GetFreeClosePoint));
        }

        [PatchPrefix]
        public static bool Prefix(CoverPointMaster __instance, Vector3 pos, BotCoversData bot, float minSDistToEnemy, ref CustomNavigationPoint __result)
        {
            
            return true;
        }
    }
}
