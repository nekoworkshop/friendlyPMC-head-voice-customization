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
using UnityEngine;
using System.Diagnostics;

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

            // prevent followers from adding teammates as an enemy
            if (isfollower && botOwner_0.BotFollower.HaveBoss)
            {
                bool isTeammate = false;
                
                if (enemy.Profile.Info.Settings.Role == WildSpawnType.shooterBTR) return false;

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
                
                if (botOwner_0.IsRole(WildSpawnType.shooterBTR)) return;

                if(botOwner_0.EnemiesController.EnemyInfos.ContainsKey(enemy))
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
}
