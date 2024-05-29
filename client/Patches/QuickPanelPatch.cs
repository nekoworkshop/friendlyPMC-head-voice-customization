using Aki.Reflection.Patching;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI.Gestures;
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
    internal class QuickPanelPatch: ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GesturesQuickPanel), "method_1");
        }
        /** Patch QuickGesturesPanel to disabel the "Cooperative" phrase is bot is a follower **/
        [PatchPrefix]
        private static bool PatchPrefix(GesturesQuickPanel __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(GesturesQuickPanel), "player_0").GetValue(__instance);
            if (player != null)
            {
                // original
                LootItem lootItem = player.InteractableObject as LootItem;
                bool flag = lootItem != null && lootItem.ItemOwner.RootItem.GetItemComponent<KeyComponent>() != null;
                bool flag2 = lootItem != null && lootItem.ItemOwner.RootItem is GClass2737;
                bool flag3 = lootItem != null && (lootItem.ItemOwner.RootItem is Weapon || lootItem.ItemOwner.RootItem.GetItemComponent<KnifeComponent>() != null);
                __instance.method_7(EPhraseTrigger.LootKey, flag);
                __instance.method_7(EPhraseTrigger.LootMoney, flag2);
                __instance.method_7(EPhraseTrigger.LootWeapon, flag3);
                __instance.method_7(EPhraseTrigger.LootGeneric, lootItem != null && !flag && !flag2 && !flag3);
                Corpse x = player.InteractableObject as Corpse;
                __instance.method_7(EPhraseTrigger.LootBody, x != null);
                __instance.method_7(EPhraseTrigger.CheckHim, x != null);
                __instance.method_7(EPhraseTrigger.LootContainer, player.InteractableObject as LootableContainer != null);
                Door door = player.InteractableObject as Door;
                __instance.method_7(EPhraseTrigger.OpenDoor, door != null);
                __instance.method_7(EPhraseTrigger.LockedDoor, door != null && (door.DoorState == EDoorState.Locked || door.DoorState == EDoorState.Shut));

                // modification is here
                if(player.InteractablePlayer != null && player.InteractablePlayer.IsAI)
                {
                    if(player.InteractablePlayer.AIData.BotOwner.BotFollower.HaveBoss)
                    {
                        __instance.method_7(EPhraseTrigger.Cooperation, false);

                        return false;
                    } else
                    {
                        __instance.method_7(EPhraseTrigger.Cooperation,true);
                    }
                }
                
                return false;
            }

            return true;
        }
    }
}
