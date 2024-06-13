using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using friendlyPMC.Actions;
using friendlyPMC.Components;
using HarmonyLib;
using LootingBots.Patch.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Modules
{
    internal class InteractableObjects
    {
        public static InteractableObjects Instance;

        private Corpse _currCorpse;

        private Door _currDoor;

        private LootItem _lootItem;

        private bool IsDisposed = false;
        public InteractableObjects() { 
            if(Instance == null)
            {
                Instance = this;
            }
        }

        public void Destroy()
        {
            if(IsDisposed) return;

            _currCorpse = null;
            _currDoor = null;
            _lootItem = null;

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
                        lookPos.y = lookPos.y + 0.4f;
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

    }
}
