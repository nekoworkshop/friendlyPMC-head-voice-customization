using SPT.Reflection.Patching;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib; 
using System;
using System.Collections.Generic;
using System.Reflection;

namespace friendlyPMC.Patches
{
    internal class BotGroupUsecEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsGroup), "method_0");

        }
        [PatchPrefix]
        private static bool PatchPrefix(BotsGroup __instance, ref bool __result, IPlayer player)
        {
            // fix Usecs turning hostile because of UsecRaidRemainKills
            if (player.Profile.Info.Side == EPlayerSide.Usec)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    internal class BotGroupAddEnemy : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsGroup), "AddEnemy");

        }
        [PatchPrefix]
        private static bool PatchPrefix(BotsGroup __instance, ref bool __result, IPlayer person, EBotEnemyCause cause)
        {
            if (person == null || (person.IsAI && person.AIData?.BotOwner?.GetPlayer == null))
            {
                return true;
            }
            // prevent followers from adding teammates as an enemy on creation
            if (person.IsAI && person.AIData.BotOwner != null && BossPlayers.WillBeFollower(person.AIData.BotOwner))
            {
                __result = false;
                return false;
            }

            var plBoss = BossPlayers.GetBoss(person.ProfileId);
            var isgroup = BossPlayers.IsBossGroup(__instance.Id);

            if (isgroup && plBoss != null)
            {
                BotsGroup bossGroup = plBoss.bossGroup;
                // prevent boss players from being added as enemy to the group
                if ((bossGroup != null && __instance.Id == bossGroup.Id) || __instance.Side == plBoss.realPlayer.Side)
                {
                    __result = false;
                    return false;
                }
            }

            // prevent same side from being added just because they have a different role
            if (cause == EBotEnemyCause.addBotNoGroup || cause == EBotEnemyCause.AddNewMember || cause == EBotEnemyCause.warn)
            {
                if (__instance.Side == person.Side)
                {
                    var _initialBotMindSettings = AccessTools.Field(typeof(BotsGroup), "_initialBotMindSettings").GetValue(__instance) as BotGlobalsMindSettings;
                    if (
                        (person.Side == EPlayerSide.Bear && !_initialBotMindSettings.DEFAULT_BEAR_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack)) ||
                        (person.Side == EPlayerSide.Usec && !_initialBotMindSettings.DEFAULT_USEC_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack)) ||
                        (person.Side == EPlayerSide.Savage && !_initialBotMindSettings.DEFAULT_SAVAGE_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack))
                    )
                    {
                        __result = false;
                        return false;
                    }
                }
            }

            return true;
        }
        [PatchPostfix]
        private static void PatchPostfix(BotsGroup __instance, IPlayer person, EBotEnemyCause cause)
        {
            if (person == null || (person.IsAI && person.AIData?.BotOwner?.GetPlayer == null))
            {
                return;
            }

            var plBoss = BossPlayers.GetBoss(person.ProfileId);

            // whoever makes the player an enemy is our enemy
            if (plBoss != null && plBoss.bossGroup != null && plBoss.bossGroup.Id != __instance.Id)
            {
                try
                {
                    BotsGroup bossGroup = plBoss.bossGroup;
                    var _members = AccessTools.Field(typeof(BotsGroup), "_members").GetValue(__instance) as List<BotOwner>;
                    if (_members != null)
                    {
                        foreach (var item in _members)
                        {
                            bossGroup.AddEnemy(item, EBotEnemyCause.addPlayerToBoss);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("Failed to make a group an enemy");
                    Logger.LogError(ex);
                }
            }
        }
    }
    internal class BotGroupIsPlayerEnemy : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsGroup), "IsPlayerEnemy");

        }
        [PatchPrefix]
        private static bool PatchPrefix(BotsGroup __instance, ref bool __result, IPlayer player)
        {
            if (BossPlayers.Instance != null && BossPlayers.IsPlayerBoss(player.ProfileId))
            {
                BotsGroup bossGroup = BossPlayers.Instance.GetBossPlayer(player.ProfileId).bossGroup;
                if (bossGroup != null && __instance.Id == bossGroup.Id)
                {
                    __result = false;
                    return false;
                }
            }
            // prevent followers from adding teammates as an enemy on creation
            if (player.IsAI && player.AIData.BotOwner != null && BossPlayers.WillBeFollower(player.AIData.BotOwner))
            {
                __result = false;
                return false;
            }

            var _initialBotMindSettings = AccessTools.Field(typeof(BotsGroup), "_initialBotMindSettings").GetValue(__instance) as BotGlobalsMindSettings;

            // prevent same side from being added just because they have a different role
            if (_initialBotMindSettings != null && player.Side == __instance.Side)
            {
                if (
                    (player.Side == EPlayerSide.Bear && !_initialBotMindSettings.DEFAULT_BEAR_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack)) ||
                    (player.Side == EPlayerSide.Usec && !_initialBotMindSettings.DEFAULT_USEC_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack)) ||
                    (player.Side == EPlayerSide.Savage && !_initialBotMindSettings.DEFAULT_SAVAGE_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack))
                )
                {
                    __result = false;
                    return false;
                }
            }

            return true;
        }
    }
    // this is used only in case of squad spawn
    internal class BotsGroupPlayer : BotsGroup
    {
        public BotsGroupPlayer(BotZone zone, IBotGame botGame, BotOwner initialBot, List<BotOwner> enemies, DeadBodiesController deadBodiesController, List<Player> allPlayers, pitAIBossPlayer player) : base(zone, botGame, initialBot, enemies, deadBodiesController, allPlayers, false)
        {
            RemoveEnemy(player.Player());
            AddAlly(player.realPlayer);
            Side = player.realPlayer.Side;
            // clear BTR as anemy to the group
            foreach (var item in Enemies)
            {
                if(item.Value.Player?.Profile?.Info?.Settings?.Role == WildSpawnType.shooterBTR)
                {
                    RemoveEnemy(item.Value.Player,item.Value.Cause);
                    break;
                }
            }

            initialBot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = new WildSpawnType[] {
                WildSpawnType.shooterBTR
            };
        }
    }
}
