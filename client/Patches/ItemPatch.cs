using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
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

    internal class ItemSpecificationPanelPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemSpecificationPanel), "method_17");
        }

        [PatchPrefix]
        public static bool PatchPrefix(ItemSpecificationPanel __instance, ref KeyValuePair<EModLockedState, ModSlotView.GStruct398> __result, Slot slot)
        {
            string itemName = (slot.ContainedItem != null) ? slot.ContainedItem.Name.Localized(null) : string.Empty;
            string id = (slot.ContainedItem != null) ? slot.ContainedItem.Id : null;

            if (slot.Locked)
            {
                bool isBotEquiptment = false;
                Modules.InteractableObjects.GetStoredEquipment().ExecuteForEach((pid, items) =>
                {
                    foreach (var itemId in items)
                    {
                        if (itemId == id)
                        {
                            isBotEquiptment = true;
                            return;
                        }
                    }
                });

                if (!isBotEquiptment) return true;

                __result = new KeyValuePair<EModLockedState, ModSlotView.GStruct398>(EModLockedState.RaidLock, new ModSlotView.GStruct398
                {
                    Error = "<color=red>" + "Raid lock".Localized(null) + "</color>",
                    ItemName = itemName
                });

                return false;
            }

            return true;
        }
    }
}
