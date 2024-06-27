using Aki.Reflection.Patching;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib;
using JetBrains.Annotations;
using System;
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
            //if a bot makes player an enemy, make the group aware of that
            else if (!isfollower && playerBoss != null)
            {   try
                {
                    if (playerBoss.bossGroup != null)
                    {
                        playerBoss.bossGroup.AddEnemy(botOwner_0, EBotEnemyCause.checkAddTODO);
                    }


                } catch
                {

                }
            }

            return true;
        }
    }
}
