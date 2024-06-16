using Aki.Reflection.Patching;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace friendlyPMC.Patches
{
    internal class BotGroupIsEnemyPatch : ModulePatch
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
            if (BossPlayers.Instance.IsFollowerGroup(__instance.Id) && BossPlayers.Instance.IsBoss(person.ProfileId))
            {
                BotsGroup bossGroup = BossPlayers.Instance.GetBossPlayer(person.ProfileId).bossGroup;
                if (bossGroup != null && __instance.Id == bossGroup.Id)
                {
                    return false;
                }

                return false;
            }

            return true;
        }
    }

    internal class BotGroupCheckAndAddEnemy : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsGroup), "CheckAndAddEnemy");

        }
        [PatchPrefix]
        private static bool PatchPrefix(BotsGroup __instance, IPlayer player, bool ignoreAI = false)
        {
            if (BossPlayers.Instance.IsFollowerGroup(__instance.Id) && BossPlayers.Instance.IsBoss(player.ProfileId))
            {
                BotsGroup bossGroup = BossPlayers.Instance.GetBossPlayer(player.ProfileId).bossGroup;
                if (bossGroup != null && __instance.Id == bossGroup.Id)
                {
                    return false;
                }

                return false;
            }

            return true;
        }
    }
    internal class BotGroupReportAboutEnemyy : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsGroup), "ReportAboutEnemy");

        }
        [PatchPrefix]
        private static bool PatchPrefix(BotsGroup __instance, [NotNull] IPlayer enemy, EEnemyPartVisibleType isVisibleOnlyBySence)
        {
            if (BossPlayers.Instance.IsFollowerGroup(__instance.Id) && enemy != null && BossPlayers.Instance.IsBoss(enemy.ProfileId))
            {
                BotsGroup bossGroup = BossPlayers.Instance.GetBossPlayer(enemy.ProfileId).bossGroup;
                if (bossGroup != null && __instance.Id == bossGroup.Id)
                {
                    return false;
                }

                return false;
            }

            return true;
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
            if (BossPlayers.Instance.IsBoss(player.ProfileId))
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
