using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;

using UnityEngine;

using Cysharp.Threading.Tasks;
using System;

using EFT.InventoryLogic;
using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using EFT.Interactive;
using Comfort.Common;
using friendlyPMC.Requests;

namespace friendlyPMC.Actions
{
    public class FollowerTakeLoot : BaseNodeAbstractClass
    {
        private BotFollowerPlayer _follower;

        private LootItem _lootItem = null;

        private bool bool_1 = false;

        private bool bool_2 = false;

        public FollowerTakeLoot(BotOwner bot) : base(bot)
        {
        }

        public override void Update()
        {
            botOwner_0.DoorOpener.Update();


            if (bool_1)
            {
                return;
            }


            if (_follower == null)
            {
                _follower = BossPlayers.Instance.GetFollower(botOwner_0);
            }

            if (_lootItem == null)
            {
                _lootItem = AccessTools.Field(typeof(BotItemTaker), "_itemToTake").GetValue(botOwner_0.ItemTaker) as LootItem;
            }


            if (_lootItem == null)
            {
                ClearLoot();
                return;
            }

            if (bool_2)
            {
                if (botOwner_0.GoToSomePointData.IsCome() && !bool_1)
                {
                    bool_1 = true;
                    PickupLoot().Forget();
                    return;
                }

                return;
            }

            botOwner_0.GoToSomePointData.SetPoint(InteractableObjects.GetLootPosition());
            botOwner_0.GoToSomePointData.UpdateToGo(false);
            botOwner_0.Steering.LookToMovingDirection();

            bool_2 = true;
        }


        private async UniTask PickupLoot()
        {
            Item item = _lootItem.Item;

            if (item == null)
            {
                ClearLoot();
                return;
            }

            Vector3 pos = _lootItem.transform.position;
            botOwner_0.Steering.LookToPoint(pos);

            await Task.Delay(2000);

            if (botOwner_0.IsDead || botOwner_0.BotState != EBotState.Active || InteractableObjects.Instance == null || !InteractableObjects.IsTaker(botOwner_0))
            {
                ClearLoot();
                return;
            }

            try
            {
                InventoryControllerClass inventoryControllerClass = botOwner_0.GetPlayer.InventoryControllerClass;
                // order for general loot
                List<EquipmentSlot> possibleSlots = new List<EquipmentSlot> {
                    EquipmentSlot.Backpack,
                    EquipmentSlot.TacticalVest,
                    EquipmentSlot.ArmorVest,
                    EquipmentSlot.Pockets
                };
                // order for special items, like grenades
                List<EquipmentSlot> equipSlots = new List<EquipmentSlot> {
                    EquipmentSlot.Pockets,
                    EquipmentSlot.TacticalVest,
                    EquipmentSlot.ArmorVest,
                    EquipmentSlot.Backpack,
                };

                List<object> equipTypes = new List<object>
                {
                    typeof(GrenadeClass)
                };

                foreach (var item1 in equipTypes)
                {
                    Type type = (Type)item1;

                    if (type.IsInstanceOfType(item))
                    {
                        possibleSlots = equipSlots;
                        break;
                    }
                }

                // find an available grid in the equipment slots to which the key can be transferred
                ItemAddress locationForItem = FindLocationForItem(item, possibleSlots, inventoryControllerClass);
                //  - no space left
                if (locationForItem == null)
                {
                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.Negative, true);
                    ClearLoot();
                    return;
                }

                // initialize the transation to transfer the key to the bot
                var moveResult = InteractionsHandlerClass.Move(item, locationForItem, inventoryControllerClass, true);

                // - failed to make the transaction
                if (!moveResult.Succeeded)
                {
                    ClearLoot();
                    return;
                }


                // execute transaction
                IResult result = await inventoryControllerClass.TryRunNetworkTransaction(moveResult, null);
                if (botOwner_0.IsDead || botOwner_0.BotState != EBotState.Active)
                {
                    ClearLoot();
                    return;
                }
                if (result.Succeed && _follower.IsSquadMate)
                {
                    InteractableObjects.StoreItem(botOwner_0, item);
                }

                await Task.Delay(1000);
                ClearLoot();

                // back to hold position
                var req = botOwner_0.BotRequestController.CurRequest as FollowerTakeLootRequest;
                if (req != null && req.FromWait && !botOwner_0.Memory.HaveEnemy)
                {
                    IPlayer requester = req != null ? req.Requester : null;

                    Player playerRequester = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(requester.ProfileId);

                    if (botOwner_0.BotRequestController.TryStopCurrent(playerRequester, false))
                    {
                        FollowerHold holdit = new FollowerHold(playerRequester);

                        if (botOwner_0.BotsGroup.RequestsController.TryAddRequest(holdit))
                        {
                            holdit.AddPossibleExecutors(botOwner_0);
                            holdit.SetGroup(botOwner_0.BotsGroup.RequestsController);
                            botOwner_0.Gesture.TryGestus(EGesture.Good, true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Modules.Logger.LogError("Failed to pickup Loot");
                Modules.Logger.LogError(e);
                ClearLoot();
            }
        }

        private ItemAddress FindLocationForItem(Item item, IEnumerable<EquipmentSlot> possibleSlots, InventoryControllerClass botInventoryController)
        {
            foreach (EquipmentSlot slot in possibleSlots)
            {
                SearchableItemClass equipmentSlot = botInventoryController.Inventory.Equipment.GetSlot(slot).ContainedItem as SearchableItemClass;
                foreach (StashGridClass grid in (equipmentSlot?.Grids ?? (new StashGridClass[0])))
                {
                    LocationInGrid locationInGrid = grid.FindFreeSpace(item);
                    if (locationInGrid != null)
                    {

                        return new ItemAddressClass(grid, locationInGrid);
                    }
                }
            }

            return null;
        }

        private void ClearLoot()
        {
            AccessTools.Field(typeof(BotItemTaker), "_itemToTake").SetValue(botOwner_0.ItemTaker, null);
            InteractableObjects.RemoveTaker(botOwner_0);
            InteractableObjects.ClearCurLootItem();
            BotRequest currRequest = botOwner_0.BotRequestController.CurRequest;
            if (currRequest != null && currRequest.BotRequestType == (BotRequestType)CustomBotRequestType.TakeLoot)
            {
                currRequest.Complete();
            }

            bool_1 = false;
            bool_2 = false;
            _lootItem = null;
        }
    }
}
