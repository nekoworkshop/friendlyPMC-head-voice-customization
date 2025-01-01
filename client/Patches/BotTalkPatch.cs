using SPT.Reflection.Patching;

using HarmonyLib;
using System;

using System.Reflection;

namespace friendlyPMC.Patches
{
    // patch for preventing bots from talking if silenced command is active
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
    // patch for preventing bots from talking if silenced command is active
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
