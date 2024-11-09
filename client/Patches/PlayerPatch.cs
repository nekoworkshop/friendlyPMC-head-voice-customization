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

            if ((EPhraseTrigger)actionId == (EPhraseTrigger)CustomPhrases.TeamStatus)
            {
                pitAIBossPlayer boss = BossPlayers.GetBoss(__instance.Player.ProfileId);
                if (boss != null)
                {
                    BotEventHandler.GClass599 info = new BotEventHandler.GClass599
                    {
                        phrase = (EPhraseTrigger)CustomPhrases.TeamStatus,
                        PlayerRequester = __instance.Player
                    };

                    boss.PhraseSaid(info);
                }
                return false;

            } 
            else if ((EPhraseTrigger)actionId == (EPhraseTrigger)CustomPhrases.OverThere)
            {
                pitAIBossPlayer boss = BossPlayers.GetBoss(__instance.Player.ProfileId);
                if (boss != null)
                {
                    InteractableObjects.CheckSeenEnemies(boss.Player());
                }

                if (!__instance.Player.HandsController.IsInInteractionStrictCheck())
                {
                    if (__instance.Player.HandsController is Player.FirearmController)
                    {
                        (__instance.Player.HandsController as Player.FirearmController).CurrentOperation.ShowGesture(EGesture.ThatDirection);

                        foreach (var receiver in Receivers.GetReceivers())
                        {
                            GClass453 data = new GClass453
                            {
                                Gesture = (EGesture)CustomGestures.OverThere,
                                Player = __instance.Player
                            };

                            receiver.Value.GestusShown(data);
                        }

                    }
                    else if (__instance.Player.HandsIsEmpty)
                    {
                        __instance.Player.HandsController.ShowGesture(EGesture.ThatDirection);
                    }
                }


                return false;
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

        [PatchPrefix]
        private static void PatchPrefix(Player __instance, IPlayer aggressor, DamageInfo damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
        {
            Player alivePlayerByProfileID = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(aggressor.ProfileId);
			if (alivePlayerByProfileID == null || aggressor == null || aggressor.Profile == null || aggressor.Profile.Info == null || aggressor.Profile.Info.Settings == null)
			{
				return;
			}
            
            if (!Singleton<AbstractGame>.Instantiated) return;
            if (GamePlayerOwner.MyPlayer.HealthController == null || !GamePlayerOwner.MyPlayer.HealthController.IsAlive)
            {
                return;
            }
            string ProfileId = GamePlayerOwner.MyPlayer.ProfileId;
            Player player = GamePlayerOwner.MyPlayer;

            if (BossPlayers.Instance == null || !BossPlayers.IsPlayerBoss(ProfileId))
            {
                return;
            }
            
            bool knightKiller = false;
            bool pipeKiller = false;
            bool birdEyeKiller = false;
            if(aggressor.Profile.Info.Settings.Role == WildSpawnType.bossKnight)
            {
                knightKiller = true;
            }

            if(!knightKiller) return;

            List<string> list = new List<string>();
            Item weapon2 = damageInfo.Weapon;
            
            list.Add("Any");

            if(__instance.Side == EPlayerSide.Usec)
            {
                list.Add("Usec");
                list.Add("AnyPmc");
            } 
            else if(__instance.Side == EPlayerSide.Bear)
            {
                list.Add("Bear");
                list.Add("AnyPmc");
            } 
            else if(__instance.Side == EPlayerSide.Savage)
            {
                list.Add("Savage");
                list.Add("Bot");
            }

    
            string locationId = player.Location;
            float distance = Vector3.Distance(aggressor.Position, __instance.Position);

            if(knightKiller) Utils.Utils.FlagSet("knightKiller",true);
            else if(pipeKiller) Utils.Utils.FlagSet("pipeKiller",true);
            else if(birdEyeKiller) Utils.Utils.FlagSet("birdEyeKiller",true);

            list.ForEach(target=>{
                player.AbstractQuestControllerClass.CheckKillConditionCounter(target,__instance.ProfileId,new List<string>{},weapon2,bodyPart,locationId,distance,__instance.Profile.Info.Settings.Role.ToStringNoBox<WildSpawnType>(),__instance.CurrentHour,__instance.HealthController.BodyPartEffects,__instance.HealthController.BodyPartEffects,__instance.TriggerZones,new string[]{});
            });

            Utils.Utils.FlagSet("knightKiller",false);
            Utils.Utils.FlagSet("pipeKiller",false);
            Utils.Utils.FlagSet("birdEyeKiller",false);
        }
    }
}