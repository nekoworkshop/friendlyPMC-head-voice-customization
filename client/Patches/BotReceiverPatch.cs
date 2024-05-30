using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Patches
{
    internal class BotReceiverInitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotReceiver), "Init");
        }

        [PatchPrefix]
        private static bool PatchPrefix(BotReceiver __instance)
        {
            BotOwner botOwner = (BotOwner)AccessTools.Field(typeof(BotReceiver), "botOwner_0").GetValue(__instance);
            if (botOwner != null)
            {

                FollowerReceiver receiver = Receivers.GetReceiver(botOwner.ProfileId);
                if (receiver != null)
                {
                    receiver.Initiate();
                    return false;
                }
            }

            return true;
        }
    }

    internal class BotReceiverDisposePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotReceiver), "Dispose");
        }

        [PatchPrefix]
        private static bool PatchPrefix(BotReceiver __instance)
        {
            BotOwner botOwner = (BotOwner)AccessTools.Field(typeof(BotReceiver), "botOwner_0").GetValue(__instance);
            if(botOwner != null)
            {

                FollowerReceiver receiver = Receivers.GetReceiver(botOwner.ProfileId);
                if (receiver != null)
                {
                    receiver.Destroy();
                    return false;
                }
            }

            return true;
        }
    }

    internal class BotReceiverPhrasePatch : ModulePatch
    {

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotReceiver), "method_0");
        }

        [PatchPrefix]
        private static bool PatchPrefix(BotReceiver __instance, BotEventHandler.GClass599 info)
        {

            BotOwner botOwner = (BotOwner)AccessTools.Field(typeof(BotReceiver), "botOwner_0").GetValue(__instance);
            if (botOwner != null)
            {
                // on cooperation, starting following the boss player
                if (info.phrase == EPhraseTrigger.Cooperation)
                {
                    IPlayer requester = info.PlayerRequester;

                    if (requester != null && (botOwner.GetPlayer.Transform.position - requester.Transform.position).sqrMagnitude < 10f)
                    {

                        if (!BossPlayers.Instance.IsFollower(botOwner) && !botOwner.BotFollower.HaveBoss && BossPlayers.Instance.IsBoss(requester.ProfileId))
                        {
                            // this will switch the BotReceiver to our own, so the rest can be altered there
                            botOwner.BotsGroup.RequestsController.TryAskFollowMeRequest(requester, botOwner);
                            return false;
                        }
                    }

                }
                
            }

            return true;
        }
    }
}
