using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
namespace friendlyPMC.Patches
{
    internal class AIDataDisposePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AIData), "Dispose");
        }
        [PatchPrefix]
        // overwrite AIData Dispose to handle disposing AIBossPlayer
        private static bool PatchPrefix(AIData __instance)
        {
            var _movementContext = AccessTools.Field(typeof(AIData), "_movementContext").GetValue(__instance) as MovementContext;
            _movementContext.OnTiltChanged -= __instance.method_2;
            _movementContext.OnMotionApplied -= __instance.method_0;
            
            __instance.AskRequests.Dispose();
            try
            {
                if (__instance.AIBossPlayer != null)
                {
                    __instance.AIBossPlayer.Dispose();
                }
            } catch { }

            return false;
        }
    }

    internal class AIDataContructPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Constructor(typeof(AIData));
        }
        [PatchPostfix]
        // overwrite AIData Dispose to handle disposing AIBossPlayer
        private static void PatchPostfix(AIData __instance, BotOwner owner, Player player)
        {
            var field = AccessTools.Field(typeof(AIData), "AIBossPlayer");
            field.SetValue(__instance, null);
        }
    }
}