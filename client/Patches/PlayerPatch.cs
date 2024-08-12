using SPT.Reflection.Patching;

using EFT;

using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace friendlyPMC.Patches
{
    
    internal class AIDataContructPatch : ModulePatch
    {

        public static Dictionary<string,AIData> playerAIData = new Dictionary<string, AIData>();
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

                if(!playerAIData.ContainsKey(player.ProfileId))
                    playerAIData.Add(player.ProfileId, __instance);

            }

        }
    }
    internal class AIBossPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AIBossPlayer), "OfferBot");
        }
        // do not let OfferBot run, we have our own method for adding followers to the player
        [PatchPrefix]
        private static bool PatchPrefix(AIBossPlayer __instance, BotOwner bot)
        {
            return false;
        }
    }
}