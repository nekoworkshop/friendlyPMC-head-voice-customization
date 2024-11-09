using EFT;
using HarmonyLib;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace friendlyPMC.Patches
{
    /** Patch notification handler to help with making Knight part of our group **/

    [HarmonyPatch(typeof(GClass3188<RaidSettings>))]
    [HarmonyPatch("SendInvite")]
    internal class SendInvitePatch
    {
        [HarmonyPrefix]
        public static bool PatchPrefix(MatchmakerPlayerControllerClass __instance, string accountId, bool inLobby, Action callback)
        {
            List<string> playerIds = new List<string> { };
            if(__instance.GroupPlayers == null) return true;

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
}
