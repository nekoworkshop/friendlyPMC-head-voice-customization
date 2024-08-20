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

using friendlyPMC.Components;
using UnityEngine.AI;


namespace friendlyPMC.Modules
{
    internal class InteractableObjects
    {
        public static InteractableObjects Instance;

        private Door _currDoor;
        private Dictionary<string,Door> _doorsToOpen;

        private LootItem _lootItem;
        private Vector3? _lootPosition;
        private BotFollowerPlayer _botToLoot;

        private bool IsDisposed = false;

        private Dictionary<string, List<string>> _lootedItems;
        private List<Item> _toSendItems;
        private Dictionary<string, Dictionary<string, object>> _followersWithLoot;

        private bool _isBossDead = false;

        public InteractableObjects()
        {
            if (Instance == null)
            {
                Instance = this;

                _lootedItems = new Dictionary<string, List<string>>();
                _toSendItems = new List<Item>();
                _followersWithLoot = new Dictionary<string, Dictionary<string, object>>();
                _doorsToOpen = new Dictionary<string, Door>();

            }

        }

        private bool SendStoreItems()
        {
            GatherItems();

            var flatItems = Singleton<ItemFactory>.Instance.TreeToFlatItems(_toSendItems);

            var converterClass = typeof(AbstractGame).Assembly.GetTypes()
                .First(t => t.GetField("Converters", BindingFlags.Static | BindingFlags.Public) != null);

            var _defaultJsonConverters = Traverse.Create(converterClass).Field<JsonConverter[]>("Converters").Value;

            if (flatItems != null && flatItems.Any())
            {
                var info = Instance._followersWithLoot.Values.Random();

                RequestHandler.PutJson("/singleplayer/returnitems", new
                {
                    items = flatItems,
                    member = info,
                    alive = !_isBossDead
                }.ToJson(_defaultJsonConverters));

                return true;
            }

            return false;
        }

        private void GatherItems()
        {
            var bossPlayers = BossPlayers.Instance.GetBossPlayers();
            _toSendItems.Clear();
            List<string> gathered = new List<string>();

            foreach (var player in bossPlayers)
            {
                foreach (var bot in player.Value.Followers)
                {
                    if (bot.BotState != EBotState.Active || !bot.HealthController.IsAlive)
                    {
                        continue;
                    }

                    InventoryControllerClass _botInventoryController = bot.GetPlayer.InventoryControllerClass;

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

                    var storedItems = GetStoredItems(bot.ProfileId);

                    if (storedItems != null)
                    {
                        foreach (var stored in storedItems)
                        {
                            if (gathered.Contains(stored)) continue;

                            bool found = false;
                            if (tacVest.Grids.Length > 0)
                            {
                                foreach (var item in tacVest.GetAllItems())
                                {
                                    if (item.Id == stored)
                                    {
                                        _toSendItems.Add(item.CloneItem());
                                        gathered.Add(stored);
                                        found = true;
                                        break;
                                    }
                                }
                            }

                            if (!found && backpack.Grids.Length > 0)
                            {
                                foreach (var item in backpack.GetAllItems())
                                {
                                    if (item.Id == stored)
                                    {
                                        _toSendItems.Add(item.CloneItem());
                                        found = true;
                                        gathered.Add(stored);
                                        break;
                                    }
                                }
                            }

                            if (!found && pockets.Grids.Length > 0)
                            {
                                foreach (var item in pockets.GetAllItems())
                                {
                                    if (item.Id == stored)
                                    {
                                        _toSendItems.Add(item.CloneItem());
                                        gathered.Add(stored);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Destroy()
        {
            if (IsDisposed) return;


            try
            {
                if (!SendStoreItems())
                {
                    NpcMessage.NpcSendThankYou();
                } else
                {
                    string id = NpcMessage.GetNpcType("boss");
                    if (id == null) id = NpcMessage.GetNpcType("ally");

                    if(id != null)
                    {
                        NpcMessage.NpcSendThankYou(id);
                    }
                }
            }
            catch (Exception e)
            {
                Components.Logger.LogError("Error sending stored loot");
                Components.Logger.LogError(e);
            }

            foreach (var stack in _lootedItems)
            {
                stack.Value.Clear();
            }

            _lootedItems.Clear();
            _toSendItems.Clear();
            _followersWithLoot.Clear();

            _currDoor = null;
            _doorsToOpen.Clear();

            _lootItem = null;
            _lootedItems = null;

            _isBossDead = false;

            IsDisposed = true;
            Instance = null;
        }

        public static void Dispose()
        {
            if (Instance != null)
            {
                Instance.Destroy();
                Instance = null;
            }
        }

        public static void SetCurDoor(Door door)
        {

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
            {
                Instance._lootItem = item;
            }
        }

        public static LootItem GetCurLootItem()
        {
            if (Instance == null) return null;
            return Instance._lootItem;
        }

        public static Vector3 GetLootPosition()
        {
            return (Vector3)Instance._lootPosition;
        }


        public static bool SetTaker(BotOwner bot)
        {
            if (Instance == null) return false;

            var _follower = BossPlayers.Instance.GetFollower(bot);

            if (_follower == null) return false;

            if (Instance._lootItem != null)
            {
                try
                {
                    var thrownItems = (HashSet<LootItem>)AccessTools.Field(typeof(BotItemTaker), "_thrownItems").GetValue(bot.ItemTaker);
                    thrownItems.Add(Instance._lootItem);
                    AccessTools.Field(typeof(BotItemTaker), "_itemToTake").SetValue(bot.ItemTaker, Instance._lootItem);

                    Collider collider = Instance._lootItem.GetComponentInChildren<Collider>();

                    Vector3 center = collider.bounds.center;
                    center.y = collider.bounds.center.y - collider.bounds.extents.y - 0.4f;

                    NavMeshHit navMeshHit;
                    if (!NavMesh.SamplePosition(center, out navMeshHit, 2f, -1))
                    {
                        return false;
                    }

                    Instance._lootPosition = navMeshHit.position;

                    Instance._botToLoot = _follower;

                    return true;

                }
                catch (Exception ex)
                {
                    Components.Logger.LogError("Could not make bot a Loot Taker");
                    Components.Logger.LogError(ex);
                }
            }

            return false;
        }

        public static bool IsTaker(BotOwner bot)
        {
            var _follower = BossPlayers.Instance.GetFollower(bot);

            return _follower != null && _follower == Instance._botToLoot;
        }

        public static void RemoveTaker(BotOwner bot)
        {
            if (Instance == null) return;


            BotFollowerPlayer follower = BossPlayers.Instance.GetFollower(bot);

            if (follower != null && Instance._botToLoot == follower)
            {
                Instance._botToLoot = null;
            }
        }

        public static bool SetOpener(BotOwner bot, Door door = null)
        {
            if (Instance._currDoor != null)
            {
                if (!Instance._doorsToOpen.ContainsKey(bot.ProfileId))
                {
                    Instance._doorsToOpen.Add(bot.ProfileId,Instance._currDoor);
                } else
                {
                    Instance._doorsToOpen[bot.ProfileId] = door != null ? door : Instance._currDoor;
                }
                return true;
            }
            return false;
        }

        public static bool IsOpener(BotOwner bot)
        {
            return Instance._doorsToOpen.ContainsKey(bot.ProfileId);
        }

        public static void RemoveOpener(BotOwner bot)
        {
            if(Instance == null) return;
            if(Instance._doorsToOpen.ContainsKey(bot.ProfileId)) Instance._doorsToOpen.Remove(bot.ProfileId);
        }

        public static Door GetDoorToOpen(BotOwner bot)
        {
            if( Instance == null) return null;
            if(!Instance._doorsToOpen.ContainsKey(bot.ProfileId)) return null;
            return Instance._doorsToOpen[bot.ProfileId];
        }
   


        public static void ClearCurLootItem()
        {
            if (Instance != null)
            {
                Instance._lootItem = null;
                Instance._lootPosition = null;
            }
        }

        public static void StoreItem(BotOwner bot, Item item)
        {
            if (!Instance._lootedItems.ContainsKey(bot.ProfileId))
            {
                Instance._lootedItems.Add(bot.ProfileId, new List<string>());
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

            if (!list.Contains(item.Id))
            {
                list.Add(item.Id);
            }
        }

        public static void RemoveStoredItem(string bot, string itemId)
        {
            if (Instance._lootedItems.ContainsKey(bot))
            {
                var list = Instance._lootedItems[bot];
                if (list.Contains(itemId))
                {
                    list.Remove(itemId);
                }
            }
        }

        public static List<string> GetStoredItems(string bot)
        {
            if (Instance._lootedItems.ContainsKey(bot))
            {
                return Instance._lootedItems[bot];
            }

            return null;
        }

        public static void ClearStoredItems(string bot)
        {
            if (Instance._lootedItems.ContainsKey(bot))
            {
                Instance._lootedItems.Remove(bot);
                Instance._followersWithLoot.Remove(bot);
            }
        }

        public static void BossIsDead()
        {
            Instance._isBossDead = true;
        }

    }
}
