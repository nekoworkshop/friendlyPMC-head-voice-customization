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
        private static bool PatchPrefix(BotsGroup __instance, IPlayer person, EBotEnemyCause cause)
        {
            if (person == null || (person.IsAI && person.AIData?.BotOwner?.GetPlayer == null))
            {
                return true;
            }

            var plBoss = BossPlayers.GetBoss(person.ProfileId);
            var isgroup = BossPlayers.IsBossGroup(__instance.Id);

            if (isgroup && plBoss != null)
            {
                // prevent enemies from being added on spawn
                if (
                    cause == EBotEnemyCause.initCauseEnemy ||
                    cause == EBotEnemyCause.initial ||
                    cause == EBotEnemyCause.AddEnemyToAllGroupsInBotZone ||
                    cause == EBotEnemyCause.AddEnemyToAllGroups
                ) return false;

                // prevent boss player from being added as enemy to the group
                BotsGroup bossGroup = plBoss.bossGroup;
                if (bossGroup != null && __instance.Id == bossGroup.Id)
                {
                    return false;
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
                    if (bossGroup != null)
                    {
                        var _members = AccessTools.Field(typeof(BotsGroup), "_members").GetValue(__instance) as List<BotOwner>;
                        if (_members != null)
                        {
                            foreach (var item in _members)
                            {
                                bossGroup.AddEnemy(item, EBotEnemyCause.checkAddTODO);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Components.Logger.LogInfo("Failed to make a group an enemy: " + ex.Message);
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

            if (BossPlayers.Instance != null && BossPlayers.Instance.IsBoss(player.ProfileId))
            {
                BotsGroup bossGroup = BossPlayers.Instance.GetBossPlayer(player.ProfileId).bossGroup;
                if (bossGroup != null && __instance.Id == bossGroup.Id)
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
        }
    }
}
