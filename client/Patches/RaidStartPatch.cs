using EFT;
using HarmonyLib;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Reflection.Patching;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace friendlyPMC.Patches
{
    internal class RaidStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Class266), "SendRaidSettings");
        }
        [PatchPostfix]
        private static void PatchPostfix(Class266 __instance, RaidSettings settings)
        {

            bool badGuy = friendlyPMC.badGuy.Value;
            Profile profile = __instance.GetProfileBySide(ESideType.Pmc);

            // patch raid settings to determine if user can spawn with a boss do to questing
            profile.QuestsData.ForEach(quest=>{

                foreach (var item in friendlyPMC.Quests)
                {
                    bool found = false;
                    foreach (var item1 in item.Value)
                    {
                        if(item1 == quest.Id)
                        {
                            if(quest.Status == EFT.Quests.EQuestStatus.Started)
                            {
                                bool isGoonQuest = true;

                                if(friendlyPMC.QuestsLocations.TryGetValue(quest.Id, out List<string> locations))
                                {
                                    isGoonQuest = false;
                                    if(locations.Contains(settings.LocationId.ToLower()))
                                    {
                                        isGoonQuest = true;
                                    }
                                }
                                //Modules.Logger.LogInfo("settings.LocationId" + settings.LocationId);
                                if (!friendlyPMC.squadSpawn.Value && isGoonQuest)
                                {
                                    Utils.Utils.FlagSet("questGoons", true);
                                    // - when doing Goons quests, we are always bad guys
                                    Utils.Utils.FlagSet("isBadGuy",true);
                                    badGuy = true;
                                    
                                    if(item.Key == "Knight")
                                    {
                                        Utils.Utils.FlagSet("spawnKnight", true);  
                                    }

                                    found = true;
                                    break;
                                }
                            }   
                        }
                    }
                    if(found)
                    {
                        break;
                    }
                }
            });

            // patch raid settings to that we can change the settings without restarting the game
            var converterClass = typeof(AbstractGame).Assembly.GetTypes()
                .First(t => t.GetField("Converters", BindingFlags.Static | BindingFlags.Public) != null);

            var _defaultJsonConverters = Traverse.Create(converterClass).Field<JsonConverter[]>("Converters").Value;

            Modules.Logger.LogInfo("settings.LocationId " + settings.LocationId);

            RequestHandler.PutJson("/client/raid/pitconfig", new
            {
                Config = new Dictionary<string, bool>
                {
                    { "sameSideHostile", friendlyPMC.sameSideHostile.Value },
                    { "badGuy", badGuy },
                    { "pmcArmbands", friendlyPMC.pmcArmbands.Value },
                    { "englishBear", friendlyPMC.englishBear.Value }
                }

                
            }.ToJson(_defaultJsonConverters));
        }
    }
}
