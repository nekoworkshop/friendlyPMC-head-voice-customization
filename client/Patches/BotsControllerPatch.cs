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
    internal class BotsControllerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), "AddActivePLayer");

        }
        [PatchPrefix]
        private static bool PatchPrefix(BotsController __instance, Player player)
        {
            new BossPlayers();
            new InteractableObjects();
            new Receivers();
            new FollowerPatrolInstances();
            Components.Logger.LogInfo("Raid Started");
            return true;
        }
    }

    internal class LocalGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LocalGame), "Stop");

        }
        [PatchPrefix]
        private static bool PatchPrefix(LocalGame __instance, string profileId, ExitStatus exitStatus, string exitName, float delay = 0f)
        {
            BossPlayers.Dispose();
            InteractableObjects.Dispose();
            Receivers.Dispose();
            FollowerPatrolInstances.Dispose();

            Components.Logger.LogInfo("Raid Ended");

            return true;
        }
    }
}