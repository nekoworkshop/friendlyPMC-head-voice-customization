using Comfort.Common;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace friendlyPMC.Patches
{
    internal class HearingSensorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotHearingSensor), "method_0");
        }

        [PatchPostfix]
        public static void PatchPostfix(BotHearingSensor __instance, BotOwner ____botOwner, IPlayer player, Vector3 position, float power, AISoundType type)
        {
            BotOwner botOwner_0 = ____botOwner;

            if (BossPlayers.IsFollower(botOwner_0) && !botOwner_0.Memory.HaveEnemy)
            {
                if (player != null && !BossPlayers.IsPlayerBoss(player.ProfileId))
                {
                    if (player.IsAI && BossPlayers.IsFollower(player.AIData.BotOwner)) return;

                    Player person = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(player.ProfileId);

                    if (botOwner_0.EnemiesController.IsEnemy(person) || botOwner_0.BotsGroup.IsEnemy(player))
                    {
                        bool shouldReact = __instance.method_6(position,power, out var distance);

                        if(shouldReact && botOwner_0.Brain.BaseBrain is FollowerBrain)
                        {
                            (botOwner_0.Brain.BaseBrain as FollowerBrain).SoundHeard(person,position, distance,type);
                        }
                    }
                }
            }
        }
    }

}
