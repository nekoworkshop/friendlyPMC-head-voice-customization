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
            try
            {
                // Access the private field 'botOwner_0' from BotEnemiesController
                var botOwner_0 = AccessTools.Field(typeof(BotEnemiesController), "botOwner_0").GetValue(__instance) as BotOwner;

                if (botOwner_0 == null)
                {
                    return;
                }

                // Get the boss player
                pitAIBossPlayer boss = BossPlayers.Instance.GetBossPlayer(enemy.ProfileId);

                if (boss == null)
                {
                    return;
                }

                // Check if botOwner_0 is a follower and if the sides match
                if (BossPlayers.Instance.IsFollower(botOwner_0) && boss.Player().Side == botOwner_0.Side)
                {
                    try
                    {
                        // Attempt to remove the enemy
                        __instance.Remove(enemy);
                    }
                    catch (Exception _ex)
                    {
                        Components.Logger.LogInfo("Removing boss player enemy failed: " + _ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Components.Logger.LogInfo("Exception in PatchPostfix of SetInfo: " + ex.Message);
            }
        }

    }
}
