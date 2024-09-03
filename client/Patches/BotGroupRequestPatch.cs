using SPT.Reflection.Patching;

using EFT;

using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Reflection;


namespace friendlyPMC.Patches
{
    internal class FollowRequestPatch : ModulePatch
    {

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
                bool isAFollower = BossPlayers.IsFollower(posibleExecuter);

                if (isAFollower)
                {
                    // if BOT is already a follower, allow "follow me" request to take place if it is the boss who is requesting it
                    if (posibleExecuter.BotFollower.HaveBoss)
                    {
                        if (posibleExecuter.BotFollower.BossToFollow.IsMe(playerBoss.Player()))
                        {
                            return true;
                        // - this is a follower of someone else
                        } 
                        else
                        {
                            posibleExecuter.BotTalk.TrySay(EPhraseTrigger.Negative);
                            posibleExecuter.Gesture.TryGestus(EGesture.Bad, true);
                            __result = false;
                            return false;
                        }
                    }
                }
                
                if (player.Side == posibleExecuter.Side)
                {
                    int followLimit = friendlyPMC.extraPickups.Value;
                    if (friendlyPMC.squadSpawn.Value) followLimit = followLimit + friendlyPMC.squadSize.Value;

                    // add BOT as follower to the player BOSS if limit was not reached
                    if (BossPlayers.GetFollowersByBoss(player.ProfileId).Count < followLimit)
                    {
                        BossPlayers.AddFollower(posibleExecuter, playerBoss);
                        // - bot signals "OK"
                        posibleExecuter.BotTalk.TrySay(EPhraseTrigger.Roger,false);
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
                if (BossPlayers.IsFollower(posibleExecuter, playerBoss))
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
