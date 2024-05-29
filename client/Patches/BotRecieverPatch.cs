using Aki.Reflection.Patching;
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
    internal class BotRecieverInitPatch : ModulePatch
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

    internal class BotRecieverDisposePatch : ModulePatch
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

            /*BotOwner botOwner = (BotOwner)AccessTools.Field(typeof(BotReceiver), "botOwner_0").GetValue(__instance);
            if (botOwner != null)
            {
                if (info.phrase == EPhraseTrigger.Cooperation)
                {
                    Components.Logger.LogInfo("Cooperate");
                    if (!BossPlayers.Instance.IsFollower(botOwner) && !botOwner.BotFollower.HaveBoss && BossPlayers.Instance.IsBoss(info.PlayerRequester.ProfileId))
                    {
                        botOwner.BotsGroup.RequestsController.TryAskFollowMeRequest(info.PlayerRequester, botOwner);
                        return false;
                    }
                }
            }*/

            return true;
        }
    }
}
