using Aki.Common.Http;
using Aki.Reflection.Patching;
using EFT;
using friendlyPMC.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static RootMotion.FinalIK.IKSolver;
using UnityEngine.AI;
using UnityEngine;

using Comfort.Common;
using HarmonyLib;

namespace friendlyPMC.Patches
{
    internal class FollowRequestPatch : ModulePatch
    {
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


                // if BOT is already a follower, on "follow me" we make the bot come closer to the BOSS
                if (posibleExecuter.BotFollower.HaveBoss && playerBoss.Followers.Find(it => it == posibleExecuter))
                {
;                   if (BossPlayer.Instance.IsFollower(posibleExecuter, playerBoss))
                    {
                        __result = true;
                    } else
                    {
                        // bot signals "NO"
                        posibleExecuter.BotTalk.TrySay(EPhraseTrigger.Negative);
                        posibleExecuter.Gesture.TryGestus(EGesture.Bad, true);
                        __result = false;
                    }
                    return false;

                }
                
                else if (player.Side == posibleExecuter.Side)
                {
                    // add BOT as follower to the player BOSS if limit was not reached
                    if (playerBoss.Followers.Count < 2)
                    {

                        BotFollowerPlayer _follower = BossPlayer.Instance.AddFollower(posibleExecuter, playerBoss);
                        // bot signals "OK"
                        _follower.GetBot().BotTalk.TrySay(EPhraseTrigger.Roger);
                        _follower.GetBot().Gesture.TryGestus(EGesture.Good, true);

                    }
                    else
                    {
                        // bot signals "NO"
                        posibleExecuter.BotTalk.TrySay(EPhraseTrigger.Negative);
                        posibleExecuter.Gesture.TryGestus(EGesture.Bad, true);

                        Components.Logger.LogInfo($"Cannot add {posibleExecuter.Profile.Nickname} as follower to player {player.Profile.Nickname}, limit reached");
                    }

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

    internal class HoldRequestPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            //
            return typeof(BotGroupRequestController).GetMethod("TryAskHoldRequest", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static bool PatchPostfix(BotGroupRequestController __instance, ref bool __result, IPlayer player, BotOwner posibleExecuter)

        {

            pitAIBossPlayer playerBoss = BossPlayer.Instance.GetBossPlayer(player.ProfileId);

            if (playerBoss != null && posibleExecuter != null)
            {
                // if BOT is already a follower, on "follow me" we make the bot come closer to the BOSS
                if (posibleExecuter.BotFollower.HaveBoss && playerBoss.Followers.Find(it => it == posibleExecuter))
                {
                    if (BossPlayer.Instance.IsFollower(posibleExecuter, playerBoss))
                    {
                        Components.Logger.LogInfo("Bosss said 'hold position'");
                        __result = true;
                    }
                    else
                    {
                        // bot signals "NO"
                        posibleExecuter.BotTalk.TrySay(EPhraseTrigger.Negative);
                        posibleExecuter.Gesture.TryGestus(EGesture.Bad, true);
                        __result = false;
                    }
                    return false;

                }
            }

            return true;
        }
    }

    internal class ActivateGoToCheckRequestPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            //
            return typeof(BotGroupRequestController).GetMethod("TryActivateGoToCheckRequest", BindingFlags.Public | BindingFlags.Instance);
        }
        [PatchPostfix]
        private static void PatchPostfix()
        {
            Components.Logger.LogInfo("Called TryActivateGoToCheckRequest");
        }
    }

    internal class ActivateGoToPointRequestPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            //
            return typeof(BotGroupRequestController).GetMethod("TryActivateGoToPointRequest", BindingFlags.Public | BindingFlags.Instance);
        }
        [PatchPrefix]
        private static bool PatchPrefix(BotGroupRequestController __instance, ref bool __result, IPlayer requester, Vector3 point, Action completeCallback = null, Action disposeCallback = null)

        {
            Components.Logger.LogInfo("TryActivateGoToPointRequest is : " + requester.Profile.Nickname);

            return true;
           
        }
    }

    internal class ActivateSuppressionRequest : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotGroupRequestController), "TryActivateSuppressionRequest");
        }

        [PatchPrefix]
        private static bool PatchPrefix(BotGroupRequestController __instance, ref bool __result, IPlayer requester, BotOwner posibleExecuter)

        {
            Components.Logger.LogInfo("TryActivateSuppressionRequest is : " + requester.Profile.Nickname);

            return true;

        }
    }
}
