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
    internal class BotMemoryAddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMemoryClass), "AddEnemy");
        }
        [PatchPrefix]
        private static bool PatchPrefix(BotMemoryClass __instance, [NotNull] IPlayer enemy, BotSettingsClass groupInfo, bool onActivation)
        {
            if (enemy == null || (enemy.IsAI && enemy.AIData?.BotOwner?.GetPlayer == null))
                return true;

            // prevent followers from adding teammates as an enemy on creation
            if (enemy.IsAI && enemy.AIData.BotOwner != null && BossPlayers.WillBeFollower(enemy.AIData.BotOwner))
            {
                return false;
            }

            var botOwner_0 = AccessTools.Field(typeof(BotMemoryClass), "botOwner_0").GetValue(__instance) as BotOwner;

            if (botOwner_0 == null) return true;


            bool isfollower = BossPlayers.IsFollower(botOwner_0);
            bool isBossEnemy = BossPlayers.IsPlayerBoss(enemy.ProfileId);

            pitAIBossPlayer playerBoss = null;
            if (isBossEnemy) playerBoss = BossPlayers.Instance.GetBossPlayer(enemy.ProfileId);

            // prevent same side from being added on creation just because they have a different role
            if (
                enemy.Side == botOwner_0.Side && groupInfo.Cause == EBotEnemyCause.AddNewMember &&
                (
                    (enemy.Side == EPlayerSide.Bear && !botOwner_0.Settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack)) ||
                    (enemy.Side == EPlayerSide.Usec && !botOwner_0.Settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack)) ||
                    (enemy.Side == EPlayerSide.Savage && !botOwner_0.Settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack))
                )
            )
            {
                return false;
            }

            if (isfollower && (enemy.Profile.Info.Settings.Role == WildSpawnType.shooterBTR || enemy.Profile.Info.Settings.Role == WildSpawnType.peacefullZryachiyEvent)) return false;

            // prevent followers from adding teammates as an enemy
            if (isfollower && botOwner_0.BotFollower.HaveBoss)
            {
                bool isTeammate = false;

                foreach (var item in botOwner_0.BotFollower.BossToFollow.Followers)
                {
                    if (item.ProfileId == enemy.ProfileId)
                    {
                        isTeammate = true;
                        break;
                    }
                }
                
                if (isTeammate) return false;
            }
            // prevent followers from adding boss player as an enemy
            else if (playerBoss != null && BossPlayers.IsFollower(botOwner_0, playerBoss))
            {
                return false;
            }

            return true;
        }

        [PatchPostfix]
        private static void PatchPostFix(BotMemoryClass __instance, [NotNull] IPlayer enemy, BotSettingsClass groupInfo, bool onActivation)
        {
            // whoever makes the boss player an enemy, becomes the enemy of the group
            if (enemy != null)
            {
                var botOwner_0 = AccessTools.Field(typeof(BotMemoryClass), "botOwner_0").GetValue(__instance) as BotOwner;

                if (botOwner_0.IsRole(WildSpawnType.shooterBTR) || botOwner_0.IsRole(WildSpawnType.peacefullZryachiyEvent)) return;

                if (botOwner_0.EnemiesController.EnemyInfos.ContainsKey(enemy))
                {
                    var boss = BossPlayers.GetBoss(enemy.ProfileId);
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
    }

    internal class BotMemoryDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMemoryClass), "method_7");
        }
        [PatchPostfix]
        private static void PatchPostfix(BotMemoryClass __instance, DamageInfo damageInfo)
        {
            var botOwner_0 = AccessTools.Field(typeof(BotMemoryClass), "botOwner_0").GetValue(__instance) as BotOwner;
            var botsGroupField = AccessTools.Field(typeof(BotMemoryClass), "botsGroup_0");

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

            if (brain.currentTactic == "Assist")
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
            }
        }
    }
}
