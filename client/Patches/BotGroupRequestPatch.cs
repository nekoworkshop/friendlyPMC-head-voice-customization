using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using friendlyPMC.Components;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace friendlyPMC.Patches
{
    internal class FollowRequestPatch : ModulePatch
    {

        public static int followLimit = 2;
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotGroupRequestController).GetMethod("TryAskFollowMeRequest", BindingFlags.Public | BindingFlags.Instance);

        }
        [PatchPrefix]
        private static bool PatchPrefix(BotGroupRequestController __instance, ref bool __result, IPlayer player, BotOwner posibleExecuter)
        {


            

            pitAIBossPlayer playerBoss = BossPlayer.Instance.GetBossPlayer(player.ProfileId);


            if (playerBoss != null && posibleExecuter != null)
            {
                // if BOT is already a follower, allow "follow me" request to take place
                if (BossPlayer.Instance.IsFollower(posibleExecuter, playerBoss))
                {
                    // GClass505 followMe request 
                    GClass505 gclass = new GClass505(player.AIData.Player);
                    gclass.AddPossibleExecutors(posibleExecuter);
                    Components.Logger.LogInfo("Called TryAskFollowMeRequest");
                    __result = true;
                    return false;

                }

                else if (player.Side == posibleExecuter.Side)
                {
                    // add BOT as follower to the player BOSS if limit was not reached
                    if (playerBoss.Followers.Count < followLimit)
                    {

                        BossPlayer.Instance.AddFollower(posibleExecuter, playerBoss);
                        // bot signals "OK"
                        posibleExecuter.BotTalk.TrySay(EPhraseTrigger.Roger);
                        posibleExecuter.Gesture.TryGestus(EGesture.Good, true);

                    }
                    else
                    {
                        // bot signals "NO"
                        posibleExecuter.BotTalk.TrySay(EPhraseTrigger.Negative);
                        posibleExecuter.Gesture.TryGestus(EGesture.Bad, true);
                    }

                    __result = false;
                    return false;
                } else
                {
                    // bot signals "NO"
                    posibleExecuter.BotTalk.TrySay(EPhraseTrigger.Toxic);
                    posibleExecuter.Gesture.TryGestus(EGesture.FuckYou, true);
                    __result = false;
                    return false;
                }

            }
            else
            {
                Components.Logger.LogInfo($"{player.Profile.Nickname} is not a BOSS, falling back to default");
            }

            return true;
        }
    }
}
