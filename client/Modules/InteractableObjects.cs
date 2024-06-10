using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using friendlyPMC.Actions;
using friendlyPMC.Components;
using HarmonyLib;
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

        private BotOwner _taker;

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
            _taker = null;

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

            Instance._taker = bot;

            BotFollowerPlayer follower = BossPlayers.Instance.GetFollower(bot);

            if(follower != null && follower.LootingBrain != null)
            {
                if (Instance._lootItem != null)
                {
                    follower.LootingBrain.ActiveItem = Instance._lootItem;
                    follower.LootingBrain.LootObjectPosition = Instance._lootItem.transform.position;
                    Instance._lootItem = null;
                }
                else if(Instance._currCorpse != null)
                {
                    follower.LootingBrain.ActiveCorpse = Instance._currCorpse.gameObject.GetComponent<Player>();
                    follower.LootingBrain.LootObjectPosition = Instance._currCorpse.transform.position;
                    Instance._currCorpse = null;
                }
                
            }
        }
        public static bool IsToTake(BotOwner bot) 
        { 
            if (Instance == null) return false;
            return Instance._taker.GetPlayer.ProfileId == bot.GetPlayer.ProfileId;
        }

        public static void ClearTaker()
        {
            if (Instance == null) return;
            Instance._taker = null;
        }
    }
}
