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

    internal class PlayerSayPatch : ModulePatch
    {
        private static float reported = 0f;
        private static float freq = 0;
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "Say");
        }

        // Patch Player.Say to make followers turn towards the enemy when they talk
        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if (Time.time < reported || Time.time < freq) return;

            freq = Time.time + 0.5f;

            if (BossPlayers.IsPlayerBoss(__instance.ProfileId)) return;

            bool isfollower = false;
            foreach (var f in BossPlayers.GetFollowers())
            {
                if (f.GetBot().ProfileId == __instance.ProfileId)
                {
                    isfollower = true;
                    break;
                }
            }

            if (isfollower) return;

            bool reportEnemy = false;
            BossPlayers.GetFollowers().ForEach(follower => {
                BotOwner bot = follower.GetBot();
                FollowerBrain brain = bot.Brain.BaseBrain as FollowerBrain;

                if (brain == null || brain.WasHit || bot.Memory.HaveEnemy || bot.BotsGroup == null) return;
                if (
                    bot.HearingSensor.method_6(__instance.Transform.position, 40f, out var distance) &&
                    (bot.EnemiesController.IsEnemy(__instance) || bot.BotsGroup.IsEnemy(__instance))
                )
                {
                    if (distance < 12f)
                    {
                        if (!reportEnemy) bot.BotsGroup.ReportAboutEnemy(__instance, EEnemyPartVisibleType.visible);
                        Utils.Enemy.MakeEnemy(bot, __instance);
                        reported = Time.time + 3f;
                        reportEnemy = true;
                    }
                    else if (distance < 32f)
                    {
                        reported = Time.time + 3f;
                        brain.FakeShot(__instance.MainParts[BodyPartType.body].Position);
                        if (!reportEnemy)
                        {
                            bot.BotsGroup.ReportAboutEnemy(__instance, EEnemyPartVisibleType.notVisible);
                            bot.BotTalk.TrySay(EPhraseTrigger.NoisePhrase, true);
                        }
                        reportEnemy = true;
                    }
                }
            });
        }
    }

}
