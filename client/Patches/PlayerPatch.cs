using SPT.Reflection.Patching;

using EFT;

using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Reflection;

namespace friendlyPMC.Patches
{
    internal class AIDataDisposePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AIData), "Dispose");
        }
        // overwrite AIData Dispose
        // replicate the method but keep note of AIBossPlayer being null
        [PatchPrefix]
        private static bool PatchPrefix(AIData __instance)
        {
            var _movementContext = AccessTools.Field(typeof(AIData), "_movementContext").GetValue(__instance) as MovementContext;
            _movementContext.OnTiltChanged -= __instance.method_2;
            _movementContext.OnMotionApplied -= __instance.method_0;
            
            __instance.AskRequests.Dispose();
            try
            {
                // only players will have this null
                if (__instance.AIBossPlayer != null && __instance.AIBossPlayer.Followers != null)
                {
                    BotOwner[] array = __instance.AIBossPlayer.Followers.ToArray();
                    for (int i = 0; i < array.Length; i++)
                    {
                        if(array[i].BotFollower != null && array[i].BotFollower.PatrolDataFollower != null) array[i].BotFollower.Dispose();
                    }
                    __instance.AIBossPlayer.Followers.Clear();
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
        // overwrite AIData to prevent AIBossPlayer from being set 
        [PatchPostfix]
        private static void PatchPostfix(AIData __instance, BotOwner owner, Player player)
        {
            if (owner == null && player.IsYourPlayer)
            {
                // remove old AIBossPlayer
                try
                {
                    if (__instance.AIBossPlayer != null)
                    {
                        __instance.AIBossPlayer.Dispose();
                    }
                } catch (Exception ex)
                {
                    Logger.LogInfo("Failed to dispose old AIBossPlayer: " + ex.Message);
                }

                var field = AccessTools.Field(typeof(AIData), "<AIBossPlayer>k__BackingField");
                field.SetValue(__instance, null);
                Components.Logger.LogInfo("Set AIData AIBossPlayer NULL for " + player.Profile.Nickname);

            }

        }
    }
    internal class AIDataBossPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(AIData), "AIBossPlayer");
        }
        // overwrite AIData to have our AIBossPlayer returned when needed
        [PatchPrefix]
        private static bool PatchPrefix(AIData __instance, ref AIBossPlayer __result)
        {
            if(BossPlayers.Instance != null && BossPlayers.IsPlayerBoss(__instance.Player.ProfileId))
            {
                __result = BossPlayers.Instance.GetBossPlayer(__instance.Player.ProfileId);
                return false;
            }

            return true;
        }
    }
}