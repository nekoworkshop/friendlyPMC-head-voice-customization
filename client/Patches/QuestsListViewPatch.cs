using EFT.UI;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using SPT.Common.Http;
using SPT.Common.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace friendlyPMC.Patches
{

    internal class QuestData
    {
        public string ItemLocation { get; set; }
        public string ItemId { get; set; }
        public string ItemContainer { get; set; }
        public bool? Disabled { get; set; }
        public string DisabledKey { get; set; }

        public bool? IsVisible { get; set; }
    }
    /** 
     * This patch will remove the quest from the quest list of a trader if it was disabled in the backend
     */
    internal class QuestsListViewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestsListView), "Show");
        }
        [PatchPrefix]
        private static void PatchPrefix(QuestsListView __instance, ISession backendSession, IInventoryController controller, AbstractQuestControllerClass questController, TraderClass trader, QuestView questView)
        {
            try
            {
                string json = RequestHandler.GetJson("/singleplayer/pitprogress");

                if (json == null || json == "")
                {
                    return;
                }
               
                var jObject = JObject.Parse(json);

                if (jObject.ContainsKey("err") && jObject.ContainsKey("errmsg"))
                {
                    return;
                }

                var Progress = Json.Deserialize<Dictionary<string, QuestData>>(json);

                foreach (var q in Progress)
                {
                    var it = Progress[q.Key];
                    if (it.Disabled.HasValue && it.Disabled.Value)
                    {
                        QuestClass item = null;
                        foreach (var questItem in questController.Quests)
                        {
                            if (questItem.Template.Id == q.Key)
                            {
                                item = questItem;
                                break;
                            }
                        }
                        if(item != null)
                        {
                            questController.Quests.Remove(item);
                            item.IsVisible = false;
                        }
                    } else if(it.IsVisible.HasValue)
                    {
                        QuestClass item = null;
                        foreach (var questItem in questController.Quests)
                        {
                            if (questItem.Template.Id == q.Key)
                            {
                                item = questItem;
                                break;
                            }
                        }
                        if (item != null)
                        {
                            item.IsVisible = it.IsVisible.Value;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Modules.Logger.LogError(e);
            }
        }
    }
}
