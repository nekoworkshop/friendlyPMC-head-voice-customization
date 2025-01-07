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

            if (MainMenuControllerPatch.GroupPlayers != null)
            {
                foreach (var player in MainMenuControllerPatch.GroupPlayers)
                {
                    if (player.Id == "677c4e0cc7a538c4210d4d47")
                    {
                        Utils.Utils.FlagSet("spawnKnight", true);
                    }
                    else if (player.Id == "677c4e0cc7a538c4210d4d48")
                    {
                        Utils.Utils.FlagSet("spawnBigPipe", true);
                    }
                    else if (player.Id == "677c4e0cc7a538c4210d4d49")
                    {
                        Utils.Utils.FlagSet("spawnBirdEye", true);
                    }
                }
            }



            if (Utils.Utils.FlagGet("spawnKnight") || Utils.Utils.FlagGet("spawnBigPipe") || Utils.Utils.FlagGet("spawnBirdEye"))
            {
                badGuy = true;
                Utils.Utils.FlagSet("isBadGuy", true);
            }

            Profile profile = __instance.GetProfileBySide(ESideType.Pmc);

            // patch raid settings to determine if user can spawn with a boss do to questing
            List<string> questCompanions = new List<string>();
            profile.QuestsData.ForEach(quest =>
            {

                foreach (var item in Utils.Props.Quests)
                {
                    foreach (var item1 in item.Value)
                    {
                        if (item1 == quest.Id)
                        {
                            if (quest.Status == EFT.Quests.EQuestStatus.Started)
                            {
                                bool isGoonQuest = true;

                                if (Utils.Props.QuestsLocations.TryGetValue(quest.Id, out List<string> locations))
                                {
                                    isGoonQuest = false;
                                    if (locations.Contains(settings.LocationId.ToLower()))
                                    {
                                        isGoonQuest = true;
                                    }
                                }

                                if (!friendlyPMC.squadSpawn.Value && isGoonQuest)
                                {
                                    Utils.Utils.FlagSet("questGoons", true);
                                    // - when doing Goons quests, we are always bad guys
                                    Utils.Utils.FlagSet("isBadGuy", true);
                                    badGuy = true;

                                    if (item.Key == "Knight")
                                    {
                                        if (!questCompanions.Contains("bossKnight"))
                                        {
                                            questCompanions.Add("bossKnight");
                                        }
                                    }
                                    else if (item.Key == "BigPipe")
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
                    { "friendlyPMC", friendlyPMC.friendlyPMCFLAG.Value },
                    { "badGuy", badGuy },
                    { "pmcArmbands", friendlyPMC.pmcArmbands.Value },
                    { "englishBear", friendlyPMC.englishBear.Value },
                    { "location", settings.LocationId }
                }

            }.ToJson(_defaultJsonConverters));
        }
    }
    /**
     * Ensure the game does not see player having a group which would switch the game mode to Online
     */
    internal class MainMenuControllerPatch : ModulePatch
    {
        public static GClass3771<GClass1323> GroupPlayers { get; set; }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MainMenuController), "method_46");
        }

        [PatchPrefix]
        private static void PatchPrefix(MainMenuController __instance)
        {
            MatchmakerPlayerControllerClass matchmakerPlayerControllerClass = AccessTools.Field(typeof(MainMenuController), "matchmakerPlayerControllerClass").GetValue(__instance) as MatchmakerPlayerControllerClass;

            GroupPlayers = new GClass3771<GClass1323>();
            foreach (var item in matchmakerPlayerControllerClass.GroupPlayers)
            {
                if (item != matchmakerPlayerControllerClass.CurrentPlayer)
                    GroupPlayers.Add(item);
            }

            foreach (var item in GroupPlayers)
            {
                matchmakerPlayerControllerClass.GroupPlayers.Remove(item);
            }

            RaidSettings raidSettings_0 = AccessTools.Field(typeof(MainMenuController), "raidSettings_0").GetValue(__instance) as RaidSettings;
            if (!RaidStartPatch.HasFika()) raidSettings_0.RaidMode = ERaidMode.Local;
        }

        [PatchPostfix]
        private static void PatchPostfix(MainMenuController __instance)
        {
            MatchmakerPlayerControllerClass matchmakerPlayerControllerClass = AccessTools.Field(typeof(MainMenuController), "matchmakerPlayerControllerClass").GetValue(__instance) as MatchmakerPlayerControllerClass;

            foreach (var item in GroupPlayers)
            {
                matchmakerPlayerControllerClass.GroupPlayers.Add(item);
            }
        }
    }
    /**
     * Ensure the game is set to Local mode even when player is part of a group
     */
    internal class TarkovApplicationPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TarkovApplication), "method_35");
        }
        [PatchPrefix]
        private static void PatchPrefix(TarkovApplication __instance)
        {
            RaidSettings _raidSettings = AccessTools.Field(typeof(TarkovApplication), "_raidSettings").GetValue(__instance) as RaidSettings;
            _raidSettings.RaidMode = ERaidMode.Local;
        }
    }
}
