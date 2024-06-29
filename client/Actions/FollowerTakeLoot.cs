using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;

using UnityEngine;
using UnityEngine.AI;

using Cysharp.Threading.Tasks;
using System;

using Aki.SinglePlayer.Utils.Insurance;
using System.Collections;
using System.Reflection;
using EFT.InventoryLogic;
using System.Collections.Generic;

namespace friendlyPMC.Actions
{
    internal class FollowerTakeLoot : BaseNodeClass
    {
        private BotFollowerPlayer _follower;

        private bool bool_0 = false;

        private bool bool_1 = false;    

        private bool takingLoot = false;
        private float lootTimer = 0f;

        public FollowerTakeLoot(BotOwner bot) : base(bot)
        {
        }

        public void EnableTransactions()
        {
            _follower.LootingBrain.EnableTransactions();
            _follower.LootingBrain.UpdateGridStats();
        }

        public void DisableTransactions()
        {
            _follower.LootingBrain.DisableTransactions();
            _follower.LootingBrain.UpdateGridStats();
        }

        public override void Update()
        {
            if(_follower == null)
            {
                _follower = BossPlayers.Instance.GetFollower(botOwner_0);
            }

            if(takingLoot && lootTimer < Time.time)
            {
                if (botOwner_0.IsDead || botOwner_0.BotState != EBotState.Active || _follower.LootingBrain == null || _follower.LootingBrain.IsBotLooting) return;

                DisableTransactions();
                _follower.LootingBrain.StopAllCoroutines();
                _follower.LootingBrain.ActiveItem = null;
                _follower.LootingBrain.ActiveCorpse = null;
                bool_0 = false;
                bool_1 = false;
                takingLoot = false;
                return;
            }

            if (!bool_0)
            {

                Vector3 dest = Vector3.zero;
                bool destset = false;
                
                if(_follower.LootingBrain.ActiveCorpse != null || _follower.LootingBrain.ActiveItem != null)
                {
                    destset = true;
                }

                if (_follower.LootingBrain.ActiveItem != null) dest = _follower.LootingBrain.ActiveItem.transform.position;
                else if (_follower.LootingBrain.ActiveCorpse != null) dest = _follower.LootingBrain.ActiveCorpse.gameObject.transform.position;

                if(!destset)
                {
                    _follower.LootingBrain.ActiveItem = null;
                    _follower.LootingBrain.ActiveCorpse = null;
                    return;
                }

                // try get closer to the loot item/body
                NavMeshPathStatus pathStatus = botOwner_0.GoToPoint(dest, true, -1f, false, false, true, false);

                if (pathStatus != NavMeshPathStatus.PathComplete)
                {
                    _follower.LootingBrain.ActiveItem = null;
                    _follower.LootingBrain.ActiveCorpse = null;
                    return;
                }

                _follower.LootingBrain.Destination = dest;
                _follower.LootingBrain.LootObjectPosition = dest;
                botOwner_0.SetPose(1f);
                botOwner_0.SetTargetMoveSpeed(1f);
                botOwner_0.Steering.LookToMovingDirection();

                bool_0 = true;
            }

            
            if (!botOwner_0.Mover.IsComeTo(0.5f, false))
            {
                return;
            }

            if (!bool_1) {
                botOwner_0.StopMove();
                botOwner_0.SetPose(0f);

                // start looting the body
                if (_follower.LootingBrain.ActiveCorpse != null)
                {
                    Vector3 position = _follower.LootingBrain.LootObjectPosition;
                    position.y += 0.5f;
                    position.Normalize();

                    botOwner_0.Steering.LookToPoint(position);

                    takingLoot = true;
                    lootTimer = Time.time + 10f;

                    var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(10), false);
                    Timer.OnTimer += () =>
                    {
                        if (botOwner_0.IsDead || botOwner_0.BotState != EBotState.Active || _follower.LootingBrain == null || _follower.LootingBrain.IsBotLooting) return;
                        try
                        {
                            DisableTransactions();
                            _follower.LootingBrain.StopAllCoroutines();
                            _follower.LootingBrain.ActiveItem = null;
                            _follower.LootingBrain.ActiveCorpse = null;
                            bool_0 = false;
                            bool_1 = false;
                            takingLoot = false;
                        }
                        catch { }
                    };


                    EnableTransactions();
                    _follower.LootingBrain.StartCoroutine(_follower.LootingBrain.LootCorpse());
                    if (_follower.IsSquadMate) _follower.LootingBrain.StartCoroutine(MonitorLootingCoroutine());
                }
                // pick up the given item
                else if (_follower.LootingBrain.ActiveItem != null && _follower.LootingBrain.ActiveItem.Item != null)
                {
                    PickUpItem().Forget();
                }

                bool_1 = true;
            }
        }

        private async UniTask PickUpItem()
        {


            EnableTransactions();


            bool result = await _follower.TransactionController.TryPickupItem(_follower.LootingBrain.ActiveItem.Item);

            if(!result)
            {
                DisableTransactions();
                _follower.LootingBrain.StopAllCoroutines();
                _follower.LootingBrain.ActiveItem = null;
                _follower.LootingBrain.ActiveCorpse = null;
            } else if(_follower.IsSquadMate)
            {
                InteractableObjects.StoreItem(botOwner_0.ProfileId,_follower.LootingBrain.ActiveItem.Item);
            }

            DisableTransactions();

            bool_0 = false;
            bool_1 = false;
        }

        private IEnumerator MonitorLootingCoroutine()
        {
            while (botOwner_0 != null && botOwner_0.BotState == EBotState.Active && botOwner_0.HealthController.IsAlive && _follower != null && _follower.LootingBrain.IsBotLooting)
            {
                yield return null; // Wait for the next frame
            }

            // Perform the task after looting is complete
            OnLootingComplete();
        }
        /** Check if the bot still has the items given by the player in his inventory **/
        private void OnLootingComplete()
        {

            if(botOwner_0.BotState != EBotState.Active || !botOwner_0.HealthController.IsAlive) return;

            Type botOwnerType =  botOwner_0.GetPlayer.GetType();
                FieldInfo botInventory = botOwnerType.BaseType.GetField(
                    "_inventoryController",
                    BindingFlags.NonPublic
                        | BindingFlags.Static
                        | BindingFlags.Public
                        | BindingFlags.Instance
                );
            InventoryControllerClass _botInventoryController = (InventoryControllerClass)
                    botInventory.GetValue(botOwner_0.GetPlayer);
                    
            SearchableItemClass tacVest = (SearchableItemClass)
                _botInventoryController.Inventory.Equipment
                    .GetSlot(EquipmentSlot.TacticalVest)
                    .ContainedItem;

            SearchableItemClass backpack = (SearchableItemClass)
                _botInventoryController.Inventory.Equipment
                    .GetSlot(EquipmentSlot.Backpack)
                    .ContainedItem;

            SearchableItemClass pockets = (SearchableItemClass)
                _botInventoryController.Inventory.Equipment
                    .GetSlot(EquipmentSlot.Pockets)
                    .ContainedItem;

            var storedItems = InteractableObjects.GetStoredItems(botOwner_0.ProfileId);

            List<string> toRemove = new List<string>();

            if(storedItems!= null)
            {
                foreach(var stored in storedItems)
                {
                    bool found = false;
                    if(tacVest.Grids.Length > 0) {
                        foreach(var item in tacVest.GetAllItems())
                        {
                            if(item.Id == stored.Key)
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    if(!found && backpack.Grids.Length > 0) {
                        foreach(var item in backpack.GetAllItems())
                        {
                            if(item.Id == stored.Key)
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    if(!found && pockets.Grids.Length > 0) {
                        foreach(var item in pockets.GetAllItems())
                        {
                            if(item.Id == stored.Key)
                            {
                                found = true;
                                break;
                            }
                        }
                    }

                    if(!found) {
                        toRemove.Add(stored.Key);
                    }
                }
            }

            foreach(var item in toRemove)
            {
                InteractableObjects.RemoveStoredItem(botOwner_0.ProfileId,item);
            }
            
        }
    }
}
