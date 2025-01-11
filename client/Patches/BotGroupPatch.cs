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
            var isAPlayerGroup = BossPlayers.IsBossGroup(__instance.Id);
            BotsGroup bossGroup = plBoss != null ? plBoss.bossGroup : null;


            // if friendly PMC side is on, prevent groups from adding same side players as enemies
            if (friendlyPMC.friendlyPMCFLAG.Value && (cause == EBotEnemyCause.addBotNoGroup || cause == EBotEnemyCause.AddNewMember || cause == EBotEnemyCause.warn))
            {
                // - bad guy flag will exclude the player and his followers from the friendly PMC
                if (friendlyPMC.badGuy.Value || Utils.Utils.FlagGet("isBadGuy"))
                {
                    if (plBoss != null && !isAPlayerGroup) return true;

                    else if (person.IsAI && person.AIData.BotOwner && BossPlayers.IsFollower(person.AIData.BotOwner))
                    {
                        return true;
                    }

                }

                if (
                    person.Profile.Info.Side == __instance.Side &&
                    (
                        !(friendlyPMC.badGuy.Value || Utils.Utils.FlagGet("isBadGuy")) ||
                        !isAPlayerGroup
                    )
                )
                {
                    __result = false;
                    return false;
                }
            }

            // if not a boss group, allow adding enemies
            if (!isAPlayerGroup) return true;

            // prevent boss players from being added as enemy to the group
            if (plBoss != null && ((bossGroup != null && __instance.Id == bossGroup.Id) || __instance.Side == plBoss.realPlayer.Side))
            {
                __result = false;
                return false;
            }
            // prevent followers group from adding friendly bots as enemies
            if (
                (cause == EBotEnemyCause.addBotNoGroup || cause == EBotEnemyCause.AddNewMember || cause == EBotEnemyCause.warn) &&
                person.Profile?.Info?.Settings?.Role != null &&
                Utils.Props.friendlyBotTypes.Contains(person.Profile.Info.Settings.Role)
            )
            {
                __result = false;
                return false;
            }
            // prevent Rogues from being added as enemies if they are friends with the player
            var bossOfGroup = BossPlayers.GetBossByGroup(__instance.Id);

            var personRole = person.Profile?.Info?.Settings?.Role;

            if (bossOfGroup != null && personRole != null)
            {
                Player bossPlayer = bossOfGroup.realPlayer;
                foreach (var data in bossPlayer.Profile.QuestsData)
                {
                    if (data.Id == Utils.Props.Quests["Knight"][0])
                    {
                        if (data.Status == EFT.Quests.EQuestStatus.Success)
                        {
                            if (
                                Utils.Props.BossFollowersType.Contains(personRole.Value) ||
                                personRole == WildSpawnType.exUsec
                            )
                            {
                                __result = false;
                                return false;
                            }

                        }
                        else if (data.Status == EFT.Quests.EQuestStatus.Started && personRole == WildSpawnType.exUsec)
                        {
                            __result = false;
                            return false;
                        }
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
                    Modules.Logger.LogError("Failed to make a group an enemy");
                    Modules.Logger.LogError(ex);
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
            // if bad guy flag is on, player and his followers are enemies to all
            if ((friendlyPMC.badGuy.Value || Utils.Utils.FlagGet("isBadGuy")) && !BossPlayers.IsBossGroup(__instance.Id))
            {
                var boss = BossPlayers.GetBoss(player.ProfileId);
                if(boss !=null)
                {
                    __result = true;
                    return false;
                } 
                else if(BossPlayers.GetFollowers().Find(x => x.GetBot().ProfileId == player.ProfileId) != null)
                {
                    __result = true;
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
            
            foreach (var item in Enemies)
            {
                WildSpawnType? Role = item.Value.Player?.Profile?.Info?.Settings?.Role;
                if(
                    Role.HasValue &&
                    Utils.Props.friendlyBotTypes.Contains(Role.Value)
                )
                {
                    RemoveEnemy(item.Value.Player,item.Value.Cause);
                    break;
                }
            }

            initialBot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = Utils.Props.friendlyBotTypes.ToArray();
            initialBot.Settings.GetEnemyBotTypes().RemoveAll(x => Utils.Props.friendlyBotTypes.Contains(x));
        }
    }
}
