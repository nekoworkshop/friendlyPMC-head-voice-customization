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

            // prevent same side from being added on creation just because they have a different role
            if(
                enemy.Side == botOwner_0.Side && groupInfo.Cause == EBotEnemyCause.AddNewMember &&
                (
                    (enemy.Side == EPlayerSide.Bear && botOwner_0.Settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR != EWarnBehaviour.Attack) ||
                    (enemy.Side == EPlayerSide.Usec && botOwner_0.Settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR != EWarnBehaviour.Attack) ||
                    (enemy.Side == EPlayerSide.Savage && botOwner_0.Settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR != EWarnBehaviour.Attack)
                )
            )
            {
                return false;
            }

            // prevent followers from adding boss player as an enemy
            if (BossPlayers.Instance.IsFollower(botOwner_0) && botOwner_0.BotFollower.HaveBoss)
            {
                bool isTeammate = false;
                foreach (var item in botOwner_0.BotFollower.BossToFollow.Followers)
                {
                    if(item.ProfileId == enemy.ProfileId)
                    {
                        isTeammate = true;
                        break;
                    }   
                }
                if (isTeammate) return false;

                pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(botOwner_0.BotFollower.BossToFollow.Player().ProfileId);

                if (boss != null && boss.Player().ProfileId == enemy.ProfileId)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
