using EFT;
using EFT.UI.Matchmaker;
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
    internal class RaidStartPatch : ModulePatch
    {
        public static bool HasFika()
        {
            return Type.GetType("Fika.Core.Coop.GameMode.CoopGame, Fika.Core") != null;
        }
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Class301), "SendRaidSettings");
        }
        [PatchPostfix]
        private static void PatchPostfix(Class301 __instance, RaidSettings settings)
        {
            bool badGuy = friendlyPMC.badGuy.Value;
            
            if(MainMenuControllerPatch.GroupPlayers != null)
            {
                foreach (var player in MainMenuControllerPatch.GroupPlayers)
                {
                    if(player.Id == "bossKnight")
                    {
                        Utils.Utils.FlagSet("spawnKnight", true);
                    }
                    else if(player.Id == "followerBigPipe")
                    {
                        Utils.Utils.FlagSet("spawnBigPipe", true);
                    }
                    else if(player.Id == "followerBirdEye")
                    {
                        Utils.Utils.FlagSet("spawnBirdEye", true);
                    }
                }
            }

            
            if (Utils.Utils.FlagGet("spawnKnight") || Utils.Utils.FlagGet("spawnBigPipe") || Utils.Utils.FlagGet("spawnBirdEye")) 
            {
                badGuy = true;
                Utils.Utils.FlagSet("isBadGuy",true);
            }

            Profile profile = __instance.GetProfileBySide(ESideType.Pmc);

            // patch raid settings to determine if user can spawn with a boss do to questing
            List<string> questCompanions = new List<string>();
            profile.QuestsData.ForEach(quest=>{

                foreach (var item in Utils.Props.Quests)
                {
                    foreach (var item1 in item.Value)
                    {
                        if(item1 == quest.Id)
                        {
                            if(quest.Status == EFT.Quests.EQuestStatus.Started)
                            {
                                bool isGoonQuest = true;

                                if(Utils.Props.QuestsLocations.TryGetValue(quest.Id, out List<string> locations))
                                {
                                    isGoonQuest = false;
                                    if(locations.Contains(settings.LocationId.ToLower()))
                                    {
                                        isGoonQuest = true;
                                    }
                                }
 
                                if (!friendlyPMC.squadSpawn.Value && isGoonQuest)
                                {
                                    Utils.Utils.FlagSet("questGoons", true);
                                    // - when doing Goons quests, we are always bad guys
                                    Utils.Utils.FlagSet("isBadGuy",true);
                                    badGuy = true;
                                    
                                    if(item.Key == "Knight")
                                    {
                                        if(!questCompanions.Contains("bossKnight"))
                                        {
                                            questCompanions.Add("bossKnight");
                                        }
                                    }
                                    else if(item.Key == "BigPipe")
                                    {
                                        if (!questCompanions.Contains("followerBigPipe"))
                                        {
                                            questCompanions.Add("followerBigPipe");
                                        }
                                    }
                                    else if (item.Key == "BirdEye")
                                    {
                                        if (!questCompanions.Contains("followerBirdEye"))
                                        {
                                            questCompanions.Add("followerBirdEye");
                                        }
                                    }

                                    break;
                                }
                            }   
                        }
                    }
                }
            });

            if (questCompanions.Count > 0)
            {
                Utils.Utils.FlagSet("spawnKnight", false);
                Utils.Utils.FlagSet("spawnBigPipe", false);
                Utils.Utils.FlagSet("spawnBirdEye", false);
                questCompanions.ForEach(companion =>
                {
                    if (companion == "bossKnight")
                    {
                        Utils.Utils.FlagSet("spawnKnight", true);
                    }
                    else if (companion == "followerBigPipe")
                    {
                        Utils.Utils.FlagSet("spawnBigPipe", true);
                    }
                    else if (companion == "followerBirdEye")
                    {
                        Utils.Utils.FlagSet("spawnBirdEye", true);
                    }

                });
            }

            // patch raid settings to that we can change the settings without restarting the game
            var converterClass = typeof(AbstractGame).Assembly.GetTypes()
                .First(t => t.GetField("Converters", BindingFlags.Static | BindingFlags.Public) != null);

            var _defaultJsonConverters = Traverse.Create(converterClass).Field<JsonConverter[]>("Converters").Value;

            RequestHandler.PutJson("/client/raid/pitconfig", new
            {
                Config = new Dictionary<string, object>
                {
                    { "sameSideHostile", friendlyPMC.sameSideHostile.Value },
                    { "badGuy", badGuy },
                    { "pmcArmbands", friendlyPMC.pmcArmbands.Value },
                    { "englishBear", friendlyPMC.englishBear.Value },
                    { "location", settings.LocationId }
                }

            }.ToJson(_defaultJsonConverters));
        }
    }

    /** Patch having a raid group to prevent the game from going switching to online matching when starting a game **/
    internal class MainMenuControllerPatch : ModulePatch
    {
        private static List<GClass1323> RemovedPlayers = new List<GClass1323>();

        public static GClass3771<GClass1323> GroupPlayers { get; private set; }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MainMenuController), "method_44");
        }

        // ensure group is empty before moving to the next screen
        [PatchPrefix]
        private static void PatchPrefix(MainMenuController __instance)
        {
            MatchmakerPlayerControllerClass matchmakerPlayerControllerClass = AccessTools.Field(typeof(MainMenuController), "matchmakerPlayerControllerClass").GetValue(__instance) as MatchmakerPlayerControllerClass;

            RemovedPlayers.Clear();

            GClass1323 currentPlayer = matchmakerPlayerControllerClass.CurrentPlayer;
            foreach (var player in matchmakerPlayerControllerClass.GroupPlayers)
            {
                if (player != currentPlayer)
                {
                    RemovedPlayers.Add(player);
                }
            }

            RemovedPlayers.ForEach(player => matchmakerPlayerControllerClass.GroupPlayers.Remove(player));

            RaidSettings raidSettings_0 = AccessTools.Field(typeof(MainMenuController), "raidSettings_0").GetValue(__instance) as RaidSettings;
            if(!RaidStartPatch.HasFika()) raidSettings_0.RaidMode = ERaidMode.Local;
        }

        // add removed players back to the group
        [PatchPostfix]
        private static void PatchPostfix(MainMenuController __instance)
        {
            MatchmakerPlayerControllerClass matchmakerPlayerControllerClass = AccessTools.Field(typeof(MainMenuController), "matchmakerPlayerControllerClass").GetValue(__instance) as MatchmakerPlayerControllerClass;

            // Add back all players that were removed in the prefix
            foreach (var player in RemovedPlayers)
            {
                matchmakerPlayerControllerClass.GroupPlayers.Add(player);
            }

            // Clear the removed players list after restoring
            RemovedPlayers.Clear();
            GroupPlayers = matchmakerPlayerControllerClass.GroupPlayers;
        }
    }
    /** Patch having a raid group to prevent the game from going switching to online matching when pressing "Ready" in the raid settings screen **/
    internal class MainMenuController74Patch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MainMenuController), "method_75");
        }

        // ensure RaidMode is local
        [PatchPrefix]
        private static void PatchPrefix(MainMenuController __instance)
        {
            RaidSettings raidSettings_0 = AccessTools.Field(typeof(MainMenuController), "raidSettings_0").GetValue(__instance) as RaidSettings;
            if(!RaidStartPatch.HasFika())raidSettings_0.RaidMode = ERaidMode.Local;
        }
    }

}
