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
     * Patch for whoever makes the boss player an enemy, becomes the enemy of the group
     */
    internal class BotMemoryAddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMemoryClass), "AddEnemy");
        }

        [PatchPostfix]
        private static void PatchPostFix(BotMemoryClass __instance, [NotNull] IPlayer enemy, BotSettingsClass groupInfo, bool onActivation)
        {
            if( enemy == null ) return;
            
            var botOwner_0 = AccessTools.Field(typeof(BotMemoryClass), "botOwner_0").GetValue(__instance) as BotOwner;

            if (botOwner_0.EnemiesController.EnemyInfos.ContainsKey(enemy))
            {
                pitAIBossPlayer boss = BossPlayers.GetBoss(enemy.ProfileId);
                // whoever makes the boss player an enemy, becomes the enemy of the group
                if (boss != null)
                {
                    if (boss.bossGroup != null)
                        boss.bossGroup.AddEnemy(botOwner_0, EBotEnemyCause.addPlayerToBoss);
                    else
                        boss.AddEnemy(botOwner_0);
                }
                // whoever makes a follower an enemy, becomes the enemy of the group
                else if (BossPlayers.IsFollower(enemy.AIData?.BotOwner) && enemy.AIData.BotOwner.BotFollower.HaveBoss)
                {
                    enemy.AIData.BotOwner.BotsGroup.AddEnemy(botOwner_0, EBotEnemyCause.addPlayerToBoss);
                }
            }
        }
    }
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

            botOwner_0.BotTalk.TrySay(EPhraseTrigger.FriendlyFire, true);

            /*if (brain.currentTactic == "Assist")
            {
                var boss = botOwner_0.BotFollower.BossToFollow as pitAIBossPlayer;
                if (boss == null) return;

                if (damageInfo.Damage <= botOwner_0.Settings.FileSettings.Aiming.MIN_DAMAGE_TO_GET_HIT_AFFETS) return;

                BossPlayers.Instance.GetFollower(botOwner_0).Dismiss(true);
                BossPlayers.RemoveFollower(botOwner_0, boss);

                __instance.DangerData.TargetNull();

                Player enemy = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(damageInfo.Player.iPlayer.ProfileId);
                
                EnemyInfo info = Utils.Enemy.MakeEnemy(botOwner_0, enemy, EBotEnemyCause.followGetHit);
                
                botOwner_0.CalcGoal();
            }*/
        }
    }
    // this is used for debug purposes that is why it stays disabled
    /*[HarmonyPatch(typeof(BotMemoryClass), "GoalEnemy", MethodType.Setter)]
    public static class GoalEnemyTracePatch
    {
        public static void Postfix(BotMemoryClass __instance, EnemyInfo value)
        {
            var botOwner_0 = AccessTools.Field(typeof(BotMemoryClass), "botOwner_0").GetValue(__instance) as BotOwner;

            if(BossPlayers.IsFollower(botOwner_0) && value != null && Utils.Props.friendlyBotTypes.Contains(value.Person.Profile.Info.Settings.Role))
            {
                Modules.Logger.LogTrace($"Follower {botOwner_0.ProfileId} is targeting friendly player {value.Person.Profile.Info.Nickname}");
            }
        }
    }*/
}
