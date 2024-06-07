using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib;
using System.Reflection;

using UnityEngine;

namespace friendlyPMC.Patches
{

    internal class AIBossPlayerDisposePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AIBossPlayer), "Dispose");
        }

        [PatchPrefix]
        private static bool PatchPrefix(AIBossPlayer __instance)
        {
            if (BossPlayers.Instance.RemoveBossPlayer(__instance.Player().ProfileId)) return false;

            return true;
        }
    }

}

