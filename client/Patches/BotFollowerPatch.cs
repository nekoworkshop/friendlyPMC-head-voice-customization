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
    internal class BotFollowerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotFollower), "method_1");
        }

        // match method trying to look of old AIBossPlayer
        [PatchPrefix]
        private static bool PatchPrefix(BotFollower __instance, float? maxDist = null)
        {
            return false;
        }
    }

}
