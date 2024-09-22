using SPT.Reflection.Patching;

using EFT;

using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using friendlyPMC.Components;
using EFT.UI;
using UnityEngine;

namespace friendlyPMC.Patches
{
    
    internal class AIDataContructPatch : ModulePatch
    {

        public static Dictionary<string,AIData> playerAIData = new Dictionary<string, AIData>();
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Constructor(typeof(AIData), new Type[] { typeof(BotOwner), typeof(Player) });
        }
        // overwrite AIData to make it use our pitAIBossPlayer
        [PatchPostfix]
        private static void PatchPostfix(AIData __instance, BotOwner owner, Player player)
        {
            if (owner == null && player != null)
            {
                // remove old AIBossPlayer
                try
                {
                    if (__instance.AIBossPlayer != null && __instance.AIBossPlayer.GetType() != typeof(pitAIBossPlayer))
                    {
                        __instance.AIBossPlayer.Dispose();
                        pitAIBossPlayer boss =  BossPlayers.GetBoss(player.ProfileId);
                        // replace AIBossPlayer with ours
                        if (boss != null)
                        {
                            var field = AccessTools.Field(typeof(AIData), "<AIBossPlayer>k__BackingField");
                            field.SetValue(__instance, boss);
                            Components.Logger.LogInfo("Replaced AIBossPlayer in AIData with ours");
                        }
                    }

                } catch (Exception ex)
                {
                    Logger.LogError("Failed to dispose old AIBossPlayer");
                    Logger.LogError(ex);
                }



                if (!playerAIData.ContainsKey(player.ProfileId))
                    playerAIData.Add(player.ProfileId, __instance);
            }

        }
    }
    internal class AIBossPlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(AIBossPlayer), "OfferBot");
        }
        // do not let OfferBot run, we have our own method for adding followers to the player
        // somehow this is not fired in pitAIBossPlayer
        [PatchPrefix]
        private static bool PatchPrefix(AIBossPlayer __instance, BotOwner bot)
        {
            if (__instance.Player() == null || __instance.Player().IsAI) return true;
            return false;
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
 
        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            if(Time.time < reported || Time.time < freq) return;

            freq = Time.time + 0.5f;

            if (BossPlayers.IsPlayerBoss(__instance.ProfileId)) return;

            bool isfollower = false;
            foreach(var f in BossPlayers.GetFollowers())
            {
                if(f.GetBot().ProfileId ==  __instance.ProfileId)
                {
                    isfollower = true;
                    break;
                }
            }

            if (isfollower) return;

            bool reportEnemy = false;
            BossPlayers.GetFollowers().ForEach(follower=>{
                BotOwner bot = follower.GetBot();
                FollowerBrain brain = bot.Brain.BaseBrain as FollowerBrain;
                
                if(brain == null || brain.WasHit || bot.Memory.HaveEnemy || bot.BotsGroup == null) return;
                if(
                    bot.HearingSensor.method_6(__instance.Transform.position,40f,out var distance) &&
                    (bot.EnemiesController.IsEnemy(__instance) || bot.BotsGroup.IsEnemy(__instance))
                )
                {
                    if(distance < 12f)
                    {
                        if(!reportEnemy) bot.BotsGroup.ReportAboutEnemy(__instance, EEnemyPartVisibleType.visible);
                        Utils.Enemy.MakeEnemy(bot, __instance);
                        reported = Time.time + 3f;
                        reportEnemy = true;
                    } 
                    else if(distance < 32f)
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