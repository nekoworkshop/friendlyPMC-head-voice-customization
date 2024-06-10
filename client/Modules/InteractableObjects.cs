using EFT;
using EFT.Interactive;
using friendlyPMC.Actions;
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

        public static void SetCurCorpse(Corpse corpse)
        {
            if(Instance != null)
            Instance._currCorpse = corpse;
        }

        public static Corpse GetCurCorpse()
        {
            if (Instance == null) return null;
            return Instance._currCorpse;
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

        public static void SetTaker(BotOwner bot, LootItem item)
        {
            if (Instance == null) return;

            Instance._taker = bot;
            if (item != null)
            {
                try
                {
                    var thrownItems = (HashSet<LootItem>)AccessTools.Field(typeof(BotItemTaker), "_thrownItems").GetValue(bot.ItemTaker);
                    thrownItems.Add(item);
                    AccessTools.Field(typeof(BotItemTaker), "_itemToTake").SetValue(bot.ItemTaker,item);
                } catch (Exception ex)
                {
                    Components.Logger.LogInfo("SetTaker Error : " + ex.Message);
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
