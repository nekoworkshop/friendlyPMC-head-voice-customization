using SPT.Reflection.Patching;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib;

using System.Reflection;
using JetBrains.Annotations;

namespace friendlyPMC.Patches
{
    /**
     * Patch to stop followers from acquiring enemies through walls 
     */
    internal class BotMemoryAddEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMemoryClass), "AddEnemy");
        }

        [PatchPrefix]
        private static bool PatchPrefix(BotMemoryClass __instance, [NotNull] IPlayer enemy, BotSettingsClass groupInfo, bool onActivation)
        {
            if (enemy == null) return true;

            var botOwner_0 = AccessTools.Field(typeof(BotMemoryClass), "botOwner_0").GetValue(__instance) as BotOwner;

            // - do not assign enemies to followers if the enemy just spawned
            if (BossPlayers.IsFollower(botOwner_0))
            {
                /*if(enemy != null && (groupInfo.Cause == EBotEnemyCause.addBotAtGroup || groupInfo.Cause == EBotEnemyCause.addBotNoGroup))
                    return false;*/
                Modules.Logger.LogTrace($"Follower {botOwner_0.Profile.Nickname} added enemy {enemy.Profile.Nickname} because of cause {groupInfo.Cause}");
            }

            return true;
        }
    }
    /**
     * Patch to turn "Assist" followers into hostile on friendly fire
     * @notinuse
     */
    internal class BotMemoryDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotMemoryClass), "method_8");
        }
        [PatchPrefix]
        private static void PatchPrefix(BotMemoryClass __instance, DamageInfoStruct damageInfo)
        {
            try
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
            catch (System.Exception e)
            {
                Modules.Logger.LogError(e);
            }
        }
    }
}
