using SPT.Reflection.Patching;

using EFT;

using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using friendlyPMC.Components;
using Comfort.Common;
using EFT.InventoryLogic;
using UnityEngine;
using static EFT.Profile;
using UnityEngine.Profiling;

namespace friendlyPMC.Patches
{
    
    internal class AIDataContructPatch : ModulePatch
    {

        public static Dictionary<string, GClass551> playerAIData = new Dictionary<string, GClass551>();
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Constructor(typeof(GClass551), new Type[] { typeof(BotOwner), typeof(Player) });
        }
        // overwrite AIData to make it use our pitAIBossPlayer
        [PatchPostfix]
        private static void PatchPostfix(GClass551 __instance, BotOwner owner, Player player)
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
                            var field = AccessTools.Field(typeof(GClass551), "<AIBossPlayer>k__BackingField");
                            field.SetValue(__instance, boss);
                            Logger.LogInfo("Replaced AIBossPlayer in AIData with ours");
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
    /** Handle firing TeamStatus and OverThere commands inside PlayPhraseOrGesture so that the enemy does not hear them **/
    internal class GamePlayerOwnerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GamePlayerOwner), "PlayPhraseOrGesture");
        }

        [PatchPrefix]
        private static bool PatchPrefix(GamePlayerOwner __instance, int actionId, bool aggressive)
        {
            pitAIBossPlayer boss = BossPlayers.GetBoss(__instance.Player.ProfileId);

            if ((EPhraseTrigger)actionId == (EPhraseTrigger)CustomPhrases.TeamStatus)
            {

                if (boss != null)
                {
                    BotEventHandler.GClass659 info = new BotEventHandler.GClass659
                    {
                        phrase = (EPhraseTrigger)CustomPhrases.TeamStatus,
                        PlayerRequester = __instance.Player
                    };

                    boss.PhraseSaid(info);

                    foreach (var receiver in Receivers.GetReceivers())
                    {
                        receiver.Value.PhraseSaid(info);
                    }
                }
                return false;

            } 
            else if ((EPhraseTrigger)actionId == (EPhraseTrigger)CustomPhrases.OverThere)
            {

                if (boss != null)
                {
                    InteractableObjects.CheckSeenEnemies(boss.Player());
                }

                if (!__instance.Player.HandsController.IsInInteractionStrictCheck())
                {
                    if (__instance.Player.HandsController is Player.FirearmController)
                    {

                        foreach (var receiver in Receivers.GetReceivers())
                        {
                            GClass501 data = new GClass501
                            {
                                Gesture = (EInteraction)CustomGestures.OverThere,
                                Player = __instance.Player
                            };

                            receiver.Value.GestusShown(data);
                        }

                        (__instance.Player.HandsController as Player.FirearmController).CurrentOperation.ShowGesture(EInteraction.ThereGesture);

                    }
                    else if (__instance.Player.HandsIsEmpty)
                    {
                        
                        foreach (var receiver in Receivers.GetReceivers())
                        {
                            GClass501 data = new GClass501
                            {
                                Gesture = (EInteraction)CustomGestures.OverThere,
                                Player = __instance.Player
                            };

                            receiver.Value.GestusShown(data);
                        }

                        __instance.Player.HandsController.ShowGesture(EInteraction.ThereGesture);
                    }
                }


                return false;
            }
            // fix for 0.15 not triggering the gesture shown event when it comes from the boss
            if (boss != null && actionId <=9)
            {

                foreach (var receiver in Receivers.GetReceivers())
                {
                    receiver.Value.GestusShown(new GClass501
                    {
                        Gesture = (EInteraction)actionId,
                        Player = boss.Player()
                    });
                }
            }

            return true;
        }
    }
    /** Check who killed a bot to see if we count it for a knight kill quest **/
    internal class PlayerKilledPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "OnBeenKilledByAggressor");
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance, IPlayer aggressor, DamageInfoStruct damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            try
            {
                if (aggressor == null || aggressor.Profile == null || aggressor.Profile.Info == null || aggressor.Profile.Info.Settings == null) return;

                Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(aggressor.ProfileId);
                if (alivePlayerByProfileID == null)
                {
                    return;
                }

                if (!Singleton<AbstractGame>.Instantiated) return;
                if (GamePlayerOwner.MyPlayer == null) return;
                if (GamePlayerOwner.MyPlayer.HealthController == null || !GamePlayerOwner.MyPlayer.HealthController.IsAlive)
                {
                    return;
                }

                // penalize Knight standing if player kills any of the goons after they become netural
                if (BossPlayers.IsPlayerBoss(aggressor.ProfileId))
                {
                    if (Utils.Props.BossFollowersType.Contains(__instance.Profile.Info.Settings.Role))
                    {
                        foreach (var data in alivePlayerByProfileID.Profile.QuestsData)
                        {
                            if (Utils.Props.Quests["Knight"][0] == data.Id && data.Status == EFT.Quests.EQuestStatus.Success)
                            {

                                if (alivePlayerByProfileID.Profile.TryGetTraderInfo("67768b19fa281ca31708b187", out var traderInfo))
                                {
                                    double standing = alivePlayerByProfileID.Profile.GetTraderStanding("67768b19fa281ca31708b187");
                                    traderInfo.SetStanding(Math.Min(0.1, standing - 0.02));
                                }

                                break;
                            }
                        }
                    }
                    return;
                }

                // have kills of the Goons count as quest kills when needed
                string ProfileId = GamePlayerOwner.MyPlayer.ProfileId;
                Player player = GamePlayerOwner.MyPlayer;

                if (BossPlayers.Instance == null || !BossPlayers.IsPlayerBoss(ProfileId))
                {
                    return;
                }

                bool knightKiller = false;
                bool pipeKiller = false;
                bool birdEyeKiller = false;
                // - check if the aggressor is Knight
                if (aggressor.Profile.Info.Settings.Role == WildSpawnType.bossKnight)
                {
                    knightKiller = true;
                }
                // - check if the aggressor is BigPipe
                else if (aggressor.Profile.Info.Settings.Role == WildSpawnType.followerBigPipe)
                {
                    pipeKiller = true;
                }
                // - check if the aggressor is BirdEye
                else if (aggressor.Profile.Info.Settings.Role == WildSpawnType.followerBirdEye)
                {
                    birdEyeKiller = true;
                }

                // - partial recreation of the "Test" condition that normally runs for player
                List<string> list = new List<string>();
                Item weapon2 = damageInfo.Weapon;

                list.Add("Any");

                if (__instance.Side == EPlayerSide.Usec)
                {
                    list.Add("Usec");
                    list.Add("AnyPmc");
                }
                else if (__instance.Side == EPlayerSide.Bear)
                {
                    list.Add("Bear");
                    list.Add("AnyPmc");
                }
                else if (__instance.Side == EPlayerSide.Savage)
                {
                    list.Add("Savage");
                    list.Add("Bot");
                }


                string locationId = player.Location;
                float distance = Vector3.Distance(aggressor.Position, __instance.Position);

                Utils.Utils.FlagSet("knightKiller", knightKiller);
                Utils.Utils.FlagSet("pipeKiller", pipeKiller);
                Utils.Utils.FlagSet("birdEyeKiller", birdEyeKiller);

                // - check if the kill is a quest kill
                if (knightKiller || pipeKiller || birdEyeKiller)
                {
                    list.ForEach(target =>
                    {
                        player.AbstractQuestControllerClass.CheckKillConditionCounter(target, __instance.ProfileId, new List<string> { }, weapon2, bodyPart, locationId, distance, __instance.Profile.Info.Settings.Role.ToStringNoBox<WildSpawnType>(), __instance.CurrentHour, __instance.HealthController.BodyPartEffects, __instance.HealthController.BodyPartEffects, __instance.TriggerZones, new string[] { });
                    });
                    if (knightKiller) Utils.Utils.FlagSet("knightKiller", false);
                    if (pipeKiller) Utils.Utils.FlagSet("pipeKiller", false);
                    if (birdEyeKiller) Utils.Utils.FlagSet("birdEyeKiller", false);
                }
            }
            catch (Exception ex)
            {
                Modules.Logger.LogError(ex);
            }
        }
    }
}