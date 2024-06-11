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
    
    internal class BotTalkPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(BotTalk), "_canSay");
        }

        [PatchPrefix]
        private static bool PatchPrefix(BotTalk __instance, ref bool __result)
        {
            __result = __instance.IsSilenced;

            return true;
        }
    }
}
