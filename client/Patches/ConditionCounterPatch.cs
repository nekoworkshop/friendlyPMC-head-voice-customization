using Comfort.Common;
using EFT;
using EFT.Quests;
using friendlyPMC.Modules;
using HarmonyLib;
using SPT.Reflection.Patching;

using System.Collections.Generic;

using System.Reflection;

namespace friendlyPMC.Patches
{
    internal class ConditionCounterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ConditionCounterManager), "smethod_0");
        }
        // Patch kill counts in order to prevent our quests from counting if required teammate is missing
        [PatchPrefix]
        private static bool PatchPrefix(ConditionCounterManager __instance, int valueToAdd, TaskConditionCounterClass counter, GStruct404[] checks)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (!Singleton<AbstractGame>.Instantiated) return true;
            if (GamePlayerOwner.MyPlayer.HealthController == null || !GamePlayerOwner.MyPlayer.HealthController.IsAlive)
            {
                return true;
            }
            string ProfileId = GamePlayerOwner.MyPlayer.ProfileId;

            if (BossPlayers.Instance == null || !BossPlayers.IsPlayerBoss(ProfileId))
            {
                return true;
            }

            // Knight quests require Knight as teammate
            List<string> knightKillConditions = new List<string> {
                "friendlypmc-knight-competition-1",
                "friendlypmc-knight-competition-2",
                "friendlypmc-knight-competition-3",
                "friendlypmc-knight-competition-4"
            };

            if (knightKillConditions.Contains(counter.Id))
            {
                var followers = BossPlayers.GetFollowersByBoss(ProfileId);
                if (followers == null || followers.Count == 0)
                {
                    return false;
                }

                bool hasKnight = false;
                foreach (var follower in followers)
                {
                    if (follower.GetBot().IsRole(WildSpawnType.bossKnight))
                    {
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
