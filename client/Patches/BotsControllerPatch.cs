using Aki.Reflection.Patching;
using EFT;
using friendlyPMC.Actions;
using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

    internal class LocalGameCleanupPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LocalGame), "CleanUp");

        }
        // fix errors during cleanup because of NULL players
        [PatchPrefix]
        private static bool PatchPrefix(LocalGame __instance)
        {
            try
            {
                var dictionary_2 = AccessTools.Field(typeof(LocalGame), "dictionary_2").GetValue(__instance) as Dictionary<string, Player>;
                if (dictionary_2 != null)
                {
                    List<string> keysToRemove = new List<string>();

                    // Iterate through the dictionary to find null values
                    foreach (var kvp in dictionary_2)
                    {
                        if (kvp.Value == null)
                        {
                            keysToRemove.Add(kvp.Key);
                        }
                    }

                    // Remove the keys with null values
                    foreach (var key in keysToRemove)
                    {
                        dictionary_2.Remove(key);
                    }
                }
            } catch (Exception ex)
            {
                Components.Logger.LogInfo("Could not properly fix CleanUp :" + ex.Message);
            }

            return true;
        }
    }
}