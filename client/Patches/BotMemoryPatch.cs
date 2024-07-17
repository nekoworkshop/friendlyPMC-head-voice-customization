using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib;
using JetBrains.Annotations;

using System;
using System.Collections.Generic;

using System.Reflection;
using System.Threading.Tasks;

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

            var botOwner_0 = AccessTools.Field(typeof(BotMemoryClass), "botOwner_0").GetValue(__instance) as BotOwner;

            if(botOwner_0 == null) return true;

            bool isfollower = BossPlayers.Instance.IsFollower(botOwner_0);
            bool isBossEnemy = BossPlayers.Instance.IsBoss(enemy.ProfileId);

            pitAIBossPlayer playerBoss = null;
            if(isBossEnemy) playerBoss = BossPlayers.Instance.GetBossPlayer(enemy.ProfileId);

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
            else if (playerBoss != null && BossPlayers.Instance.IsFollower(botOwner_0, playerBoss))
            {
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(BotMemoryClass), "GoalEnemy", MethodType.Setter)]
    public static class GoalEnemyTracePatch
    {
        private static List<string> enemies = new List<string>();
        public static void Prefix(BotMemoryClass __instance, EnemyInfo value)
        {
            // if a follower makes someone an enemy, the rest of the group should know
            if (value != null)
            {
                try
                {
                    var botOwner_0 = AccessTools.Field(typeof(BotMemoryClass), "botOwner_0").GetValue(__instance) as BotOwner;
                    var enemyInfo_0 = AccessTools.Field(typeof(BotMemoryClass), "enemyInfo_0").GetValue(__instance) as EnemyInfo;

                    if (enemyInfo_0 == value) return;

                    Task.Run(() =>
                    {
                        // ensure all other members know about the enemy
                        if (BossPlayers.Instance.IsFollower(botOwner_0) && botOwner_0.BotFollower.HaveBoss)
                        {

                            if (enemies.Contains(value.ProfileId)) return;

                            enemies.Add(value.ProfileId);
                            // add enemy to group
                            botOwner_0.BotsGroup.AddEnemy(value.Person, EBotEnemyCause.checkAddTODO);

                            BotSettingsClass botsett = new BotSettingsClass(Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(value.ProfileId), botOwner_0.BotsGroup, EBotEnemyCause.checkAddTODO);

                            botOwner_0.BotFollower.BossToFollow.Followers.ForEach(item =>
                            {
                                if (item != null && item.ProfileId != botOwner_0.ProfileId && !item.Memory.HaveEnemy)
                                {
                                    item.Memory.AddEnemy(value.Person, botsett, false);

                                    if (!item.Memory.HaveEnemy)
                                    {
                                        EnemyInfo info;
                                        item.EnemiesController.EnemyInfos.TryGetValue(value.Person, out info);
                                        if (info != null)
                                        {
                                            info.PriorityIndex = 0;
                                            item.Memory.GoalEnemy = info;
                                            info.SetVisible(true);
                                        }
                                    }
                                }
                            });

                            StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(0.1), false).OnTimer += () =>
                            {
                                enemies.Remove(value.ProfileId);
                            };
                        }
                    });
                } catch
                {

                }
            }
        }
    }

}
