using Aki.Reflection.Patching;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Patches
{
    internal class BotEnemiesControllerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotEnemiesController), "SetInfo");

        }
        // how does boss player randomly get added as Enemy?? - fix it
        [PatchPostfix]
        private static void PatchPostfix(BotEnemiesController __instance, IPlayer enemy, EnemyInfo info)
        {
            var botOwner_0 = AccessTools.Field(typeof(BotEnemiesController), "botOwner_0").GetValue(__instance) as BotOwner;
            pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(enemy.ProfileId);
            if (botOwner_0 != null && boss != null)
            {
                if(BossPlayers.Instance.IsFollower(botOwner_0) && boss.Player().Side == botOwner_0.Side)
                {
                    try
                    {
                        __instance.Remove(enemy);

                    } catch (Exception _ex) { Components.Logger.LogInfo("Cannot remove player enemy: " + _ex.Message); }
                }
            }
        }
    }
}
