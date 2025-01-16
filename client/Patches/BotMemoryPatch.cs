using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib;
using JetBrains.Annotations;

using System.Collections.Generic;

using System.Reflection;

namespace friendlyPMC.Patches
{
    /**
     * This patch is used to prevent followers from adding teammates as an enemy on friendly fire
     */
    internal class BotMemoryDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMemoryClass), "method_8");
        }
        [PatchPostfix]
        private static void PatchPostfix(BotMemoryClass __instance, DamageInfoStruct damageInfo)
        {
            var botOwner_0 = AccessTools.Field(typeof(BotMemoryClass), "botOwner_0").GetValue(__instance) as BotOwner;

            if (damageInfo.Player == null) return;

            bool isfollower = BossPlayers.IsFollower(botOwner_0);
            if (!isfollower) return;

            bool isBossEnemy = BossPlayers.IsPlayerBoss(damageInfo.Player.iPlayer.ProfileId);

            bool isTeamate = false;

            if (botOwner_0.BotFollower.BossToFollow == null) return;

            botOwner_0.BotFollower.BossToFollow.Followers.ForEach(bt =>
            {
                if (bt.ProfileId == damageInfo.Player.iPlayer.ProfileId) isTeamate = true;
            });

            if (!(isBossEnemy || isTeamate)) return;

            var brain = botOwner_0.Brain.BaseBrain as FollowerBrain;
            if (brain == null) return;

            

            if (brain.currentTactic == "Assist")
            {
                var boss = botOwner_0.BotFollower.BossToFollow as pitAIBossPlayer;
                if (boss == null) return;

                if (damageInfo.Damage <= botOwner_0.Settings.FileSettings.Aiming.MIN_DAMAGE_TO_GET_HIT_AFFETS)
                {
                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.FriendlyFire, true);
                    return;
                }

                botOwner_0.BotTalk.TrySay(EPhraseTrigger.Rat, false);

                var follower = BossPlayers.Instance.GetFollower(botOwner_0);
                BossPlayers.RemoveFollower(botOwner_0, boss);
                follower.Dismiss(true);
            } 
            else
            {
                botOwner_0.BotTalk.TrySay(EPhraseTrigger.FriendlyFire, true);
            }
        }
    }
    // this is used for debug purposes that is why it stays disabled
    /*[HarmonyPatch(typeof(BotMemoryClass), "GoalEnemy", MethodType.Setter)]
    public static class GoalEnemyTracePatch
    {
        public static void Postfix(BotMemoryClass __instance, EnemyInfo value)
        {
            var botOwner_0 = AccessTools.Field(typeof(BotMemoryClass), "botOwner_0").GetValue(__instance) as BotOwner;

            if(BossPlayers.IsFollower(botOwner_0) && value != null)
            {
                Modules.Logger.LogTrace($"Follower accquired an enemy because " + value.GroupInfo.Cause + "flags : " + value.HaveSeen + "; " + value.ShallKnowEnemy());
            }
        }
    }*/
}
