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
        
            Instance._currDoor = door;
        }
        public static Door GetCurDoor()
        {
            return Instance._currDoor;
        }

        public static void SetCurLootItem(LootItem item) 
        {
            Instance._lootItem = item;
        }

        public static LootItem GetCurLootItem()
        {
            return Instance._lootItem;
        }

        public static void SetCurCorpse(Corpse corpse)
        {
            Instance._currCorpse = corpse;
        }

        public static Corpse GetCurCorpse()
        {
            return Instance._currCorpse;
        }

        public static void SetTaker(BotOwner bot)
        {
            Instance._taker = bot;

            BotFollowerPlayer follower = BossPlayers.Instance.GetFollower(bot);

            if(follower != null)
            {
                follower.LootingBrain.ActiveItem = Instance._lootItem;
                follower.LootingBrain.LootObjectPosition = Instance._lootItem.transform.position;
                Instance._lootItem = null;
            }
        }
        public static bool IsToTake(BotOwner bot) 
        { 
            return Instance._taker.GetPlayer.ProfileId == bot.GetPlayer.ProfileId;
        }

        public static void ClearTaker()
        {
            Instance._taker = null;
        }
    }
}
