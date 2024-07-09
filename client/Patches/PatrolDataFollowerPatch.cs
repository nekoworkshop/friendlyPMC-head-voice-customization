using Aki.Reflection.Patching;
using EFT;
using friendlyPMC.Actions;
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
    internal class PatrolDataFollowerPatch: ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PatrolDataFollower), "ManualUpdate");
        }

        [PatchPrefix]
        private static bool PatchPrefix(PatrolDataFollower __instance)
        {

            var botOwner_0 = AccessTools.Field(typeof(PatrolDataFollower), "botOwner_0").GetValue(__instance) as BotOwner;

            bool allowDefault = true;

            if(botOwner_0 != null) foreach(var item in FollowerPatrolInstances.GetPatrols())
            {
                if(item.botOwner.ProfileId == botOwner_0.ProfileId)
                {
                    item.Update();
                    allowDefault = false;
                    break;
                }
            }
            return allowDefault;
        }
    }
}
