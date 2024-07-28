using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;

using SPT.Common.Http;

using HarmonyLib;
using Comfort.Common;

using UnityEngine;

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Newtonsoft.Json;
using LootingBots.Patch.Components;

using friendlyPMC.Components;
using EFT.UI;


namespace friendlyPMC.Modules
{
    internal class InteractableObjects
    {
        public static InteractableObjects Instance;

        private Corpse _currCorpse;

        private Door _currDoor;

        private LootItem _lootItem;

        private bool IsDisposed = false;

        private Dictionary<string, Dictionary<string,Item>> _lootedItems;
        private Dictionary<string, Dictionary<string, object>> _followersWithLoot;

        public InteractableObjects() { 
            if(Instance == null)
            {
                Instance = this;

                _lootedItems = new Dictionary<string, Dictionary<string,Item>>();
                _followersWithLoot = new Dictionary<string, Dictionary<string, object>>();
            }

        }
        /** Send any items given to the followers back to the player **/
        public void SendStoreItems()
        {

            List<Item> items = new List<Item>();

            List<string> keys = _lootedItems.Keys.ToList();
            // loop through all looted items inside _lootedItems and add them to the list
            foreach (var key in keys)
            {
                if(_lootedItems.TryGetValue(key, out Dictionary<string, Item> stack))
                {
                    List<string> stackKeys = stack.Keys.ToList();
                    foreach (var stackKey in stackKeys)
                    {
                        if (stack.TryGetValue(stackKey, out Item item))
                        {
                            if(item != null && item is MedsClass == false)
                                items.Add(item);
                        }
                    }
                }
            }

            var flatItems = Singleton<ItemFactory>.Instance.TreeToFlatItems(items);

            var converterClass = typeof(AbstractGame).Assembly.GetTypes()
                .First(t => t.GetField("Converters", BindingFlags.Static | BindingFlags.Public) != null);

            var _defaultJsonConverters = Traverse.Create(converterClass).Field<JsonConverter[]>("Converters").Value;

            var info = Instance._followersWithLoot.Values.Random();

            if (flatItems != null && flatItems.Any()) 
                RequestHandler.PutJson("/singleplayer/returnitems", new
                {
                    items = flatItems,
                    member = info
                }.ToJson(_defaultJsonConverters));
        }

        public void Destroy()
        {
            if(IsDisposed) return;
            
            
            try{
                SendStoreItems();
            } catch(Exception e) {
                Components.Logger.LogInfo($"Error sending store items: {e}");
            }

            foreach(var stack in _lootedItems)
            {
                stack.Value.Clear();
            }
            _lootedItems.Clear();

            _currCorpse = null;
            _currDoor = null;
            
            _lootItem = null;
            _lootedItems = null;

            IsDisposed = true;
            Instance = null;
        }

        public static void Dispose()
        {
            if(Instance != null)
            {
                Instance.Destroy();
                Instance = null;
            }
        }

        public static void SetCurDoor(Door door) {

            if (Instance != null)
                Instance._currDoor = door;
        }
        public static Door GetCurDoor()
        {
            if (Instance == null) return null;
            return Instance._currDoor;
        }

        public static void SetCurLootItem(LootItem item) 
        {
            if (Instance != null)
                Instance._lootItem = item;
        }

        public static LootItem GetCurLootItem()
        {
            if (Instance == null) return null;
            return Instance._lootItem;
        }

        public static void SetCurCorpse(Corpse corpse)
        {
            if (Instance != null)
                Instance._currCorpse = corpse;
        }

        public static Corpse GetCurCorpse()
        {
            if (Instance == null) return null;
            return Instance._currCorpse;
        }

        public static void SetTaker(BotOwner bot)
        {
            if (Instance == null) return;


            BotFollowerPlayer follower = BossPlayers.Instance.GetFollower(bot);

            if(follower != null && follower.LootingBrain != null)
            {
                var GetDestination = AccessTools.Method(typeof(LootFinder), "GetDestination");
                if (Instance._currCorpse != null)
                {
                    var pl = Instance._currCorpse.gameObject.GetComponentInParent<Player>();
                    if (pl != null)
                    {
                        Vector3 center = pl.Transform.position;
                        center.y = center.y - 0.4f;
                        var destination = (Vector3)GetDestination.Invoke(follower.LootFinder, new object[] { center });

                        follower.LootingBrain.Destination = destination;
                        follower.LootingBrain.DistanceToLoot = bot.Mover.ComputePathLengthToPoint(destination);

                        follower.LootingBrain.ActiveCorpse = pl;
                        Vector3 lookPos = pl.Transform.position;
                        lookPos.y = lookPos.y + 0.5f;
                        lookPos.Normalize();
                        follower.LootingBrain.LootObjectPosition = lookPos;
                        
                        Instance._currCorpse = null;
                    }
                }
                else if (Instance._lootItem != null)
                {
                    Collider collider = Instance._lootItem.GetComponentInChildren<Collider>();
                    if (collider != null)
                    {
                        Vector3 center = collider.bounds.center;
                        center.y = collider.bounds.center.y - collider.bounds.extents.y - 0.4f;
                        var destination = (Vector3)GetDestination.Invoke(follower.LootFinder, new object[] { center });

                        follower.LootingBrain.Destination = destination;
                        follower.LootingBrain.DistanceToLoot = bot.Mover.ComputePathLengthToPoint(destination);
                    }
                    else
                    {
                        follower.LootingBrain.Destination = Instance._lootItem.transform.position;
                        follower.LootingBrain.DistanceToLoot = bot.Mover.ComputePathLengthToPoint(Instance._lootItem.transform.position);
                    }

                    follower.LootingBrain.ActiveItem = Instance._lootItem;
                    follower.LootingBrain.LootObjectPosition = Instance._lootItem.transform.position;

                    Instance._lootItem = null;
                }
            }
        }

        public static bool IsTaker(BotOwner bot)
        {
            var _follower = BossPlayers.Instance.GetFollower(bot);

            return _follower != null && _follower.LootingBrain != null && _follower.TransactionController != null &&
                (_follower.LootingBrain.ActiveItem != null || _follower.LootingBrain.ActiveCorpse != null);
        }

        public static void RemoveTaker(BotOwner bot)
        {
            if (Instance == null) return;


            BotFollowerPlayer follower = BossPlayers.Instance.GetFollower(bot);

            if (follower != null && follower.LootingBrain != null)
            {
                follower.LootingBrain.StopAllCoroutines();
                follower.LootingBrain.DisableTransactions();
                follower.LootingBrain.UpdateGridStats();

                follower.LootingBrain.ActiveItem = null;
                follower.LootingBrain.ActiveCorpse = null;
            }

            if (bot.BotRequestController.CurRequest != null && bot.BotRequestController.CurRequest.BotRequestType == (BotRequestType)CustomBotRequestType.TakeLoot)
                bot.BotRequestController.CurRequest.Complete();
        }

        public static void StoreItem(BotOwner bot, Item item)
        {
            if(!Instance._lootedItems.ContainsKey(bot.ProfileId)) {
                Instance._lootedItems.Add(bot.ProfileId, new Dictionary<string, Item>());
                Instance._followersWithLoot.Add(bot.ProfileId, new Dictionary<string, object> {
                    { "_id" , bot.ProfileId  },
                    { "aid" , bot.Profile.AccountId },
                    { 
                        "Info" , new Dictionary<string, object>{
                            { "Level", bot.Profile.Info.Level },
                            { "MemberCategory", bot.Profile.Info.MemberCategory },
                            { "Nickname",  bot.Profile.Info.Nickname },
                            { "Side",  bot.Profile.Info.Side },
                        } 
                    },
                });
            }

            var list = Instance._lootedItems[bot.ProfileId];

            if (!list.ContainsKey(item.Id))
            {
                list.Add(item.Id, item.CloneItem());
            }
        }

        public static void RemoveStoredItem(string bot, string itemId)
        {
            if (Instance._lootedItems.ContainsKey(bot))
            {
                var list = Instance._lootedItems[bot];
                if(list.ContainsKey(itemId))
                {
                    list.Remove(itemId);
                }
            }
        }

        public static Dictionary<string, Item> GetStoredItems(string bot)
        {
            if (Instance._lootedItems.ContainsKey(bot))
            {
                return Instance._lootedItems[bot];
            }

            return null;
        }

        public static void ClearStoredItems(string bot)
        {
            if(Instance._lootedItems.ContainsKey(bot))
            {
                Instance._lootedItems.Remove(bot);
                Instance._followersWithLoot.Remove(bot);
            }
        }

    }
}
