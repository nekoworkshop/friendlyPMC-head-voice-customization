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
        public InteractableObjects() { 
            if(Instance == null)
            {
                Instance = this;
            }
        }

        public void Destroy()
        {
            _currCorpse = null;
            _currDoor = null;
            _lootItem = null;
            _taker = null;
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
            Instance._currCorpse = corpse;
        }

        public static Corpse GetCurCorpse()
        {
            return Instance._currCorpse;
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

        public static void SetTaker(BotOwner bot, LootItem item)
        {
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
            return Instance._taker.GetPlayer.ProfileId == bot.GetPlayer.ProfileId;
        }

        public static void ClearTaker()
        {
            Instance._taker = null;
        }
    }
}
