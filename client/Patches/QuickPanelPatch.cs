using Aki.Reflection.Patching;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI.Gestures;
using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Reflection;

namespace friendlyPMC.Patches
{
    internal class QuickPanelPatch: ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GesturesQuickPanel), "method_1");
        }
        /** Patch QuickGesturesPanel to disable the "Cooperative" phrase if bot is a follower **/
        [PatchPrefix]
        private static bool PatchPrefix(GesturesQuickPanel __instance)
        {
            Player player = (Player)AccessTools.Field(typeof(GesturesQuickPanel), "player_0").GetValue(__instance);
            if (player != null)
            {
                try
                {
                    // original
                    LootItem lootItem = player.InteractableObject as LootItem;
                    bool flag = lootItem != null && lootItem.ItemOwner.RootItem.GetItemComponent<KeyComponent>() != null;
                    bool flag2 = lootItem != null && lootItem.ItemOwner.RootItem is GClass2737;
                    bool flag3 = lootItem != null && (lootItem.ItemOwner.RootItem is Weapon || lootItem.ItemOwner.RootItem.GetItemComponent<KnifeComponent>() != null);

                    // modification here
                    if (lootItem != null && !flag && !flag2) InteractableObjects.SetCurLootItem(lootItem);
                    else InteractableObjects.SetCurLootItem(null);

                    __instance.method_7(EPhraseTrigger.LootKey, flag);
                    __instance.method_7(EPhraseTrigger.LootMoney, flag2);
                    __instance.method_7(EPhraseTrigger.LootWeapon, flag3);
                    __instance.method_7(EPhraseTrigger.LootGeneric, lootItem != null && !flag && !flag2 && !flag3);

                    // modification here
                    Corpse x = player.InteractableObject as Corpse;
                    if (x != null)
                    {
                        InteractableObjects.SetCurCorpse(x);
                        InteractableObjects.SetCurLootItem(null);
                    }
                    else
                        InteractableObjects.SetCurCorpse(null);

                    __instance.method_7(EPhraseTrigger.LootBody, x != null);
                    __instance.method_7(EPhraseTrigger.CheckHim, x != null);

                    __instance.method_7(EPhraseTrigger.LootContainer, player.InteractableObject as LootableContainer != null);
                }
                catch (Exception e) { Components.Logger.LogInfo("Loot Commands Failed: " + e.Message); }

                Door door = player.InteractableObject as Door;
                try
                {
                    // modification here
                    InteractableObjects.SetCurDoor(door);

                    __instance.method_7(EPhraseTrigger.OpenDoor, door != null);
                } 
                catch (Exception e) { Components.Logger.LogInfo("Open Door Command Failed: " + e.Message); }

                __instance.method_7(EPhraseTrigger.LockedDoor, door != null && (door.DoorState == EDoorState.Locked || door.DoorState == EDoorState.Shut));

                // modification is here
                try
                {
                    if (player.InteractablePlayer != null && player.InteractablePlayer.IsAI && player.InteractablePlayer.HealthController.IsAlive)
                    {
                        if (BossPlayers.Instance.IsFollower(player.InteractablePlayer.AIData.BotOwner) || player.InteractablePlayer.Side != player.Side)
                        {
                            __instance.method_7(EPhraseTrigger.Cooperation, false);

                            return false;
                        }
                        else
                        {
                            __instance.method_7(EPhraseTrigger.Cooperation, true);
                        }
                    } else
                    {
                        __instance.method_7(EPhraseTrigger.Cooperation, false);
                    }
                } catch (Exception e) { Components.Logger.LogInfo("Cooperation Command Failed: " + e.Message); }

                return false;
            }

            return true;
        }
    }
}
