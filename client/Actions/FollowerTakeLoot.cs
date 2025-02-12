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
using Diz.LanguageExtensions;

namespace friendlyPMC.Actions
{
    /**
     * Action for a bot follower to take loot given by the player
     */
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
                InventoryController inventoryControllerClass = botOwner_0.GetPlayer.InventoryController;
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
                    typeof(ThrowWeapItemClass),
                    typeof(MedicalItemClass)
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
                bool wasTransferred = false;
                foreach (EquipmentSlot slot in possibleSlots)
                {
                    if(botOwner_0.ItemTaker.method_10(slot, _lootItem))
                    {
                        wasTransferred = true;
                        break;
                    }
                }
                
                if (botOwner_0.IsDead || botOwner_0.BotState != EBotState.Active)
                {
                    ClearLoot();
                    return;
                }

                if(!wasTransferred)
                {
                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.DontKnow, true);
                }

                if (wasTransferred && _follower.IsSquadMate)
                {
                    InteractableObjects.StoreItem(botOwner_0, item);
                }

                await Task.Delay(1000);
                ClearLoot();
            }
            catch (Exception e)
            {
                Modules.Logger.LogError("Failed to pickup Loot");
                Modules.Logger.LogError(e);
                ClearLoot();
            }
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
