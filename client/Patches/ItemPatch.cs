using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Patches
{
    // Make all followers items unlootable
    internal class UnlootableComponentPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(UnlootableComponent), "IsUnlootableFrom");
        }

        [PatchPrefix]
        public static bool PatchPrefix(UnlootableComponent __instance, ref bool __result, IContainer container)
        {
            bool isBotEquiptment = false;

            Modules.InteractableObjects.GetStoredEquipment().ExecuteForEach((id, items) =>
            {
                foreach (var itemId in items)
                {
                    if(itemId == __instance.Item.Id)
                    {
                        isBotEquiptment = true;
                        return;
                    }
                }
            });

            __result = isBotEquiptment;

            return false;
        }
    }
    // Make all followers items unremovable
    internal class ModRaidModdablePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Mod), "RaidModdable");
        }

        [PatchPrefix]
        public static bool PatchPrefix(Mod __instance, ref bool __result)
        {
            bool isBotEquiptment = false;

            Modules.InteractableObjects.GetStoredEquipment().ExecuteForEach((id, items) =>
            {
                foreach (var itemId in items)
                {
                    if (itemId == __instance.Id)
                    {
                        isBotEquiptment = true;
                        return;
                    }
                }
            });
            
            if(isBotEquiptment)
            {
                __result = false;
                return false;
            }
            // let original run
            return true;
        }
    }
}
