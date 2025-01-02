using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.Trading;
using HarmonyLib;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace friendlyPMC.Patches
{
    /** Patch notification handler to help with making a bot friend part of our group **/
    [HarmonyPatch(typeof(GClass3565<RaidSettings>))]
    [HarmonyPatch("SendInvite")]
    internal class SendInvitePatch
    {
        [HarmonyPrefix]
        public static bool PatchPrefix(MatchmakerPlayerControllerClass __instance, string accountId, bool inLobby, Action callback)
        {
            List<string> playerIds = new List<string> { };
            if (__instance.GroupPlayers == null) return true;

            foreach (var friend in __instance.GroupPlayers)
            {
                playerIds.Add(friend.AccountId);
            }

            // send current group info to decide if we accept or decline the invite
            var converterClass = typeof(AbstractGame).Assembly.GetTypes()
                .First(t => t.GetField("Converters", BindingFlags.Static | BindingFlags.Public) != null);

            var _defaultJsonConverters = Traverse.Create(converterClass).Field<JsonConverter[]>("Converters").Value;

            RequestHandler.PutJson("/client/match/group/pitstatus", new
            {
                Players = playerIds,

            }.ToJson(_defaultJsonConverters));

            return true;
        }
    }
    internal class SocialNetworkClassPatch : ModulePatch
    {

        private static SocialNetworkClass socialNetworkClass = null;
        private static IChatInteractions iChatInteractions;
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(SocialNetworkClass), "method_1");
        }

        [PatchPostfix]
        private static void PatchPostfix(SocialNetworkClass __instance, IChatInteractions session, InventoryController inventoryController, string version)
        {
            socialNetworkClass = __instance;
            iChatInteractions = session;
        }

        private static float delay = 0;

        public static void RefreshFriendsList()
        {
            if (socialNetworkClass != null && delay < Time.time )
            {
                delay = Time.time + 2;
                iChatInteractions.GetFriendsList(new Callback<GClass1010>(socialNetworkClass.method_13));
            }
        }
    }
    /** Refresh friends list whenever we complete a quest **/
    internal class QuestClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestClass), "SetStatus");
        }

        [PatchPostfix]
        private static void PatchPostfix(QuestClass __instance)
        {
            if(__instance.QuestStatus == EQuestStatus.Success)
            {
                SocialNetworkClassPatch.RefreshFriendsList();
            }
        }

    }
}
