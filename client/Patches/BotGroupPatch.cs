using SPT.Reflection.Patching;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib; 
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

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

        public static bool PlayerHasKnightQuest(Profile playerProfile)
        {
            if(playerProfile.QuestsData == null) return false;
            foreach (var data in playerProfile.QuestsData)
            {
                if (data.Id == Utils.Props.Quests["Knight"][0])
                {
                    if (data.Status == EFT.Quests.EQuestStatus.Success || data.Status == EFT.Quests.EQuestStatus.Started)
                    {
                        return true;

                    }
                }
            }

            return false;
        }

        private static List<string> _isFriendly = new List<string>();

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

            _isFriendly.Clear();

            // prevent followers from adding teammates as an enemy on creation
            if (person.IsAI && person.AIData.BotOwner != null && BossPlayers.WillBeFollower(person.AIData.BotOwner))
            {
                __result = false;
                _isFriendly.Add(person.ProfileId);
                return false;
            }

            var plBoss = BossPlayers.GetBoss(person.ProfileId);
            var isAPlayerGroup = BossPlayers.IsBossGroup(__instance.Id);
            BotsGroup bossGroup = plBoss != null ? plBoss.bossGroup : null;

            bool isInitialCause = (cause == EBotEnemyCause.addBotNoGroup || cause == EBotEnemyCause.AddNewMember || cause == EBotEnemyCause.warn);

            // prevent Rogues from adding the player and his followers as enemies if they are friends with the Goons
            var _members = AccessTools.Field(typeof(BotsGroup), "_members").GetValue(__instance) as List<BotOwner>;
            var groupRole = __instance.InitialBotType;
            var _rougeTypes = Utils.Props.BossFollowersType.ToList();
            _rougeTypes.Add(WildSpawnType.exUsec);

            if(isInitialCause && _members != null)
            {
                var followerOfBoss = BossPlayers.GetFollowers().Find(x => x.GetBot().ProfileId == person.ProfileId);

                if(
                    (plBoss != null && PlayerHasKnightQuest(plBoss.realPlayer.Profile)) ||
                    (followerOfBoss != null && PlayerHasKnightQuest(followerOfBoss.GetBoss().realPlayer.Profile))
                )
                {
                    bool isRogue = false;
                    foreach(var mem in _members)
                    {
                        if(_rougeTypes.Contains(mem.Profile.Info.Settings.Role))
                        {
                            isRogue = true;
                            mem.Settings.FileSettings.Boss.SHALL_WARN = false;
                            mem.Settings.FileSettings.Patrol.MAX_YDIST_TO_START_WARN_REQUEST_TO_REQUESTER = 0f;
                        }
                    }

                    if(isRogue) 
                    {
                        _isFriendly.Add(person.ProfileId);
                        __result = false;
                        return false;
                    }
                }
            }

            // if friendly PMC side is on, prevent groups from adding same side players as enemies
            if (friendlyPMC.friendlyPMCFLAG.Value && isInitialCause)
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
                    _isFriendly.Add(person.ProfileId);

                    foreach(var mem in _members)
                    {
                        mem.Settings.FileSettings.Boss.SHALL_WARN = false;
                        mem.Settings.FileSettings.Patrol.MAX_YDIST_TO_START_WARN_REQUEST_TO_REQUESTER = 0f;
                    }
                    __result = false;
                    return false;
                }
            }

            // from this point if this is not the player boss group, allow adding enemies
            if (!isAPlayerGroup) return true;

            // prevent boss players from being added as enemy to the group
            if (plBoss != null && bossGroup != null && __instance.Id == bossGroup.Id)
            {
                _isFriendly.Add(person.ProfileId);
                __result = false;
                return false;
            }
            // prevent followers group from adding friendly bots as enemies
            if (
                isInitialCause &&
                person.Profile?.Info?.Settings?.Role != null &&
                Utils.Props.friendlyBotTypes.Contains(person.Profile.Info.Settings.Role)
            )
            {
                _isFriendly.Add(person.ProfileId);
                __result = false;
                return false;
            }
            // prevent Rogues from being added as enemies if they are friends with the player
            var bossOfGroup = BossPlayers.GetBossByGroup(__instance.Id);

            var personRole = person.Profile?.Info?.Settings?.Role;

            if (isInitialCause && personRole != null && bossOfGroup != null)
            {
                Player bossPlayer = bossOfGroup.realPlayer;
                var friendly = Utils.Props.BossFollowersType.ToList();
                friendly.Add(WildSpawnType.exUsec);
                if (PlayerHasKnightQuest(bossPlayer.Profile))
                {
                    if (
                        Utils.Props.BossFollowersType.Contains(personRole.Value)
                    )
                    {
                        _isFriendly.Add(person.ProfileId);
                        __result = false;
                        return false;
                    }
                }
            }

            if(isAPlayerGroup && person != null && personRole.HasValue && Utils.Props.BossFollowersType.Contains(personRole.Value))
            {
                Modules.Logger.LogInfo("Added " + personRole.Value + " as an enemy because " + cause);
            }

            return true;
        }
        /**
         * Whoever makes the player or his followers an enemy will become the enemy of the player's boss group (BTR is the exception)
         */
        [PatchPostfix]
        private static void PatchPostfix(BotsGroup __instance, IPlayer person, EBotEnemyCause cause)
        {
            if (person == null || (person.IsAI && person.AIData?.BotOwner?.GetPlayer == null))
            {
                return;
            }

            if(_isFriendly.Contains(person.ProfileId))
            {
                _isFriendly.Remove(person.ProfileId);
                return;
            }

            var _members = AccessTools.Field(typeof(BotsGroup), "_members").GetValue(__instance) as List<BotOwner>;

            var plBoss = BossPlayers.GetBoss(person.ProfileId);
            
            bool isFriend = false;

            if(_members !=null)
            {
                foreach(var mem in _members)
                {
                    // ignore BTR and do not add Rogues as enemies if they are friends with the player or his followers
                    if(
                        mem.Profile.Info.Settings.Role == WildSpawnType.shooterBTR
                    )
                    {
                        isFriend = true;
                        break;
                    }
                }
            }

            if(isFriend) return;
            
            if (plBoss != null && plBoss.bossGroup != null && plBoss.bossGroup.Id != __instance.Id)
            {
                try
                {
                    BotsGroup bossGroup = plBoss.bossGroup;
                    
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
