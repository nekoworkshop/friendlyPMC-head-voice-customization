using Aki.Reflection.Patching;
using EFT;
using friendlyPMC.Modules;
using HarmonyLib;
using JetBrains.Annotations;
using System.Reflection;

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
                return false;
            }

            return true;
        }


    }

}
