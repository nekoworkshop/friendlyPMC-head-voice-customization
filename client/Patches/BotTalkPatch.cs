using Aki.Reflection.Patching;
using EFT;
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
    
    internal class BotTalkTrySayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type[] parameterTypes = new Type[] { typeof(EPhraseTrigger), typeof(ETagStatus?), typeof(bool) };
            return AccessTools.Method(typeof(BotTalk), "TrySay", parameterTypes);
        }

        [PatchPrefix]
        private static bool PatchPrefix(BotTalk __instance, EPhraseTrigger type, ETagStatus? additionaMask, bool withGroupDelay)
        {
            if (__instance.IsSilenced) return false;

            return true;
        }
    }

    internal class BotTalkSayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotTalk), "Say");

        }

        [PatchPrefix]
        private static bool PatchPrefix(BotTalk __instance, EPhraseTrigger type, bool sayImmediately = false, ETagStatus? additionalMask = null)
        {
            if (__instance.IsSilenced) return false;

            return true;
        }
    }
}
