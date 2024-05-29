using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
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
            return AccessTools.Method(typeof(BotGroupRequestController),"TryAskFollowMeRequest");

        }
        [PatchPrefix]
        private static bool PatchPrefix(BotGroupRequestController __instance, ref bool __result, IPlayer player, BotOwner posibleExecuter)
        {

            pitAIBossPlayer playerBoss = BossPlayers.Instance.GetBossPlayer(player.ProfileId);



            if (playerBoss != null && posibleExecuter != null)
            {
                // if BOT is already a follower, allow "follow me" request to take place
                if (BossPlayers.Instance.IsFollower(posibleExecuter, playerBoss))
                {
                    Components.Logger.LogInfo("Called TryAskFollowMeRequest");
                    try
                    {
                        return true;


                    } catch (Exception e)
                    {
                        Logger.LogInfo("Error : " + e.ToString());
                    }
                    

                    __result = false;
                    return false;

                }

                else if (player.Side == posibleExecuter.Side)
                {
                    // add BOT as follower to the player BOSS if limit was not reached
                    if (playerBoss.Followers.Count < followLimit)
                    {

                        BossPlayers.Instance.AddFollower(posibleExecuter, playerBoss);
                        // bot signals "OK"
                        posibleExecuter.BotTalk.TrySay(EPhraseTrigger.Roger);
                        posibleExecuter.Gesture.TryGestus(EGesture.Good, true);

                    }
                    else if(!BossPlayers.Instance.IsFollower(posibleExecuter))
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
            // allow default to take place
            return true;
        }
    }

    internal class HoldRequestPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotGroupRequestController), "TryActivateWait");

        }
        [PatchPrefix]
        private static bool PatchPrefix(BotGroupRequestController __instance, IPlayer player, BotOwner posibleExecuter)
        {

            pitAIBossPlayer playerBoss = BossPlayers.Instance.GetBossPlayer(player.ProfileId);


            if (playerBoss != null && posibleExecuter != null)
            {
                // boss can only send hold requests to it's followers
                if (BossPlayers.Instance.IsFollower(posibleExecuter, playerBoss))
                {

                    return true;
                }

                // bot signals "NO"
                posibleExecuter.BotTalk.TrySay(EPhraseTrigger.Negative);
                posibleExecuter.Gesture.TryGestus(EGesture.Bad, true);

                return false;
            }
            // allow default to take place
            return true;
        }
    }
}
