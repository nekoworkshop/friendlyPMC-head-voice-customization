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
}
