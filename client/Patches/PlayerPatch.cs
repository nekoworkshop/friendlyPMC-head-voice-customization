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
            } catch(Exception ex) 
            {
                Components.Logger.LogInfo("Error Dispose AIBossPlayer: " + ex.Message);
            }

            return false;
        }
    }

    internal class AIDataContructPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Constructor(typeof(AIData), new Type[] { typeof(BotOwner), typeof(Player) });
        }
        [PatchPostfix]
        // overwrite AIData Dispose to handle disposing AIBossPlayer
        private static void PatchPostfix(AIData __instance, BotOwner owner, Player player)
        {
            if(owner == null && !player.IsAI)
            {
                var field = AccessTools.Field(typeof(AIData), "<AIBossPlayer>k__BackingField");
                if (BossPlayers.Instance != null)
                {
                    pitAIBossPlayer playerBoss = BossPlayers.Instance.GetBossPlayer(player.ProfileId);
                    
                    field.SetValue(__instance, playerBoss);
                    Components.Logger.LogInfo("AIData AIBossPlayer replaced");
                } else
                {
                    field.SetValue(__instance, null);
                    Components.Logger.LogInfo("AIData AIBossPlayer nullify");
                }
                
            }
            
        }
    }
}