using SPT.Reflection.Patching;

using EFT;

using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using friendlyPMC.Components;

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
    // we handle firing TeamStatus and OverThere commands inside PlayPhraseOrGesture so that the enemy does not hear them
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
}