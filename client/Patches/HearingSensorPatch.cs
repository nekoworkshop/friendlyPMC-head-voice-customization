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

    [HarmonyPatch(typeof(Player))]
    [HarmonyPatch("Say")]
    internal class PlayerSayPatch
    {
        private static float reported = 0f;
        private static float freq = 0;

        // Patching Player.Say to prevent enemy from hearing team status and over there
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static bool PatchPrefix(Player __instance, EPhraseTrigger @event, bool demand = false, float delay = 0f, ETagStatus mask = (ETagStatus)0, int probability = 100, bool aggressive = false)
        {
            // only players can say "team status"
            if (@event == (EPhraseTrigger)CustomPhrases.TeamStatus)
            {
                pitAIBossPlayer boss = BossPlayers.GetBoss(__instance.ProfileId);
                if (boss != null)
                {
                    BotEventHandler.GClass599 info = new BotEventHandler.GClass599
                    {
                        phrase = @event,
                        PlayerRequester = __instance
                    };

                    boss.PhraseSaid(info);
                }
                return false;
            }
            // only followers will react to over there
            if (@event == (EPhraseTrigger)CustomPhrases.OverThere)
            {
                if(!__instance.HandsController.IsInInteractionStrictCheck())
                {
                    if (__instance.HandsController is Player.FirearmController)
                    {
                        (__instance.HandsController as Player.FirearmController).CurrentOperation.ShowGesture(EGesture.ThatDirection);

                        foreach (var receiver in Receivers.GetReceivers())
                        {
                            GClass453 data = new GClass453
                            {
                                Gesture = (EGesture)CustomGestures.OverThere,
                                Player = __instance
                            };

                            receiver.Value.GestusShown(data);
                        }


                    } else if (__instance.HandsIsEmpty)
                    {
                        __instance.HandsController.ShowGesture(EGesture.ThatDirection);
                    }
                }
                

                return false;
            }
            return true;
        }
        // Patching Player.Say to make followers turn towards the enemy when they talk
        [HarmonyPostfix]
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
