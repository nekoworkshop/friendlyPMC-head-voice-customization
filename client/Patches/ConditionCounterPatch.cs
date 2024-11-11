using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.Quests;
using friendlyPMC.Modules;
using HarmonyLib;
using JetBrains.Annotations;
using SPT.Reflection.Patching;

using System.Collections.Generic;

using System.Reflection;
using UnityEngine;

namespace friendlyPMC.Patches
{
    /** Patch kill counts in order to prevent our quests from counting if required teammate is missing **/
    internal class ConditionCounterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ConditionCounterManager), "smethod_0");
        }

        [PatchPrefix]
        private static bool PatchPrefix(ConditionCounterManager __instance, int valueToAdd, TaskConditionCounterClass counter, GStruct404[] checks)
        {
            if (!Singleton<AbstractGame>.Instantiated) return true;
            if (GamePlayerOwner.MyPlayer.HealthController == null || !GamePlayerOwner.MyPlayer.HealthController.IsAlive)
            {
                return true;
            }

            string ProfileId = GamePlayerOwner.MyPlayer.ProfileId;
            Player player = GamePlayerOwner.MyPlayer;

            if (BossPlayers.Instance == null || !BossPlayers.IsPlayerBoss(ProfileId))
            {
                return true;
            }

            bool hasKnight = false;
            var followers = BossPlayers.GetFollowersByBoss(ProfileId);
            
            // Knight quests that require Knight to kill
            if (Utils.Props.QuestsKillConditions["Knight"].Contains(counter.Id))
            {
                if (followers == null || followers.Count == 0)
                {
                    return false;
                }

                foreach (var follower in followers)
                {
                    BotOwner bot = follower.GetBot();
                    if (follower.GetBot().IsRole(WildSpawnType.bossKnight))
                    {
                        if(Vector3.Distance(bot.GetPlayer.Transform.position, player.Transform.position) <= 80)
                            hasKnight = true;
                        break;
                    }
                }

                if(hasKnight) 
                {
                    return Utils.Utils.FlagGet("knightKiller");
                }

                return false;
            }
            // Knight quests that require the player to kill
            if (Utils.Props.QuestsKillConditions["Player"].Contains(counter.Id))
            {
                return !Utils.Utils.FlagGet("knightKiller") && !Utils.Utils.FlagGet("pipeKiller") && !Utils.Utils.FlagGet("birdEyeKiller");
            }

            // Knight quests that require Knight as teammate - player or knight can kill
            if (Utils.Props.QuestsTeamConditions["Knight"].Contains(counter.Id))
            {
                if (followers == null || followers.Count == 0)
                {
                    return false;
                }

                foreach (var follower in followers)
                {
                    BotOwner bot = follower.GetBot();
                    if (follower.GetBot().IsRole(WildSpawnType.bossKnight))
                    {
                        if(Vector3.Distance(bot.GetPlayer.Transform.position, player.Transform.position) < 80)
                            hasKnight = true;
                        break;
                    }
                }

                return hasKnight;
            }


            return true;
        }
    }
}
