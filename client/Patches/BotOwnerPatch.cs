using Aki.Reflection.Patching;
using EFT;

using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace friendlyPMC.Patches
{

    internal class BotOwnerDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "method_9");

        }

        [PatchPrefix]
        private static bool PatchPrefix(BotOwner __instance, DamageInfo damageInfo, EBodyPart bodyType, float damageReducedByArmor)
        {

            // if BOT is getting hit by a player BOSS of which it is a follower of, do not turn hostile
            if (__instance !=null && __instance.BotFollower != null && __instance.BotFollower.HaveBoss && BossPlayers.Instance.IsFollower(__instance) && damageInfo.Player != null)
            {
                
                AIBossPlayer player = BossPlayers.Instance.GetBossPlayer(damageInfo.Player.iPlayer.ProfileId);

                if(player != null && BossPlayers.Instance.IsFollower(__instance, player))
                {
                    // - yell "friendly fire"
                    __instance.BotTalk.TrySay(EPhraseTrigger.FriendlyFire);

                    __instance.StandBy.GetHit();
                    __instance.BotPersonalStats.GetHit(damageInfo, bodyType);
                    __instance.Memory.GetHit(damageInfo);

                    return false;
                }
            }

            return true;
        }
    }

    internal class BotOwnerIsFolowerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "IsFollower");

        }
        [PatchPrefix]
        private static bool PatchPrefix(BotOwner __instance, ref bool __result)
        {   // skip checking bot's role if we have made this bot a follower of a boss player
            if (BossPlayers.Instance.IsFollower(__instance))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    internal class BotOwnerManualUpdatePatch : ModulePatch {

        public static Dictionary<string, Action<BotOwner>> BotOwnerUpdate = new Dictionary<string, Action<BotOwner>>();
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "UpdateManual");
        }
        [PatchPostfix]
        private static void PatchPostfix(BotOwner __instance)
        {
            try
            {
                if (
                    __instance != null && 
                    __instance.BotState == EBotState.Active &&
                    __instance.GetPlayer != null &&
                    __instance.GetPlayer.HealthController != null &&
                    __instance.ProfileId != null &&
                    __instance.GetPlayer.HealthController.IsAlive
                )
                {
                    Action<BotOwner> OnUpdate;
                    BotOwnerUpdate.TryGetValue(__instance.ProfileId, out OnUpdate);
                    if (OnUpdate != null) OnUpdate(__instance);
                }
            } catch (Exception e)
            {
                Components.Logger.LogInfo("Exception on BotOwner UpdateManual: " + e.Message);
            }
        }
    }

    
}
