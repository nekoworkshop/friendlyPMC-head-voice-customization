using BepInEx.Bootstrap;
using EFT;
using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Patches
{
    internal class QuestingPatch
    {

        private static Type questingBrainType = null;
        public static bool isQuestingInstalled()
        {
            return Chainloader.PluginInfos.ContainsKey("com.DanW.QuestingBots");
        }

        public static void PatchQuestingIfInstalled(Harmony harmony)
        {
            if (!isQuestingInstalled())
            {
                return;
            }

            if (questingBrainType == null)
            {
                questingBrainType = Type.GetType("SPTQuestingBots.BotLogic.Objective.BotObjectiveManager, SPTQuestingBots");
            }

            if (questingBrainType != null)
            {
                harmony.Patch(AccessTools.Method(questingBrainType, "Update"), new HarmonyMethod(typeof(QuestingPatch).GetMethod(nameof(PatchQuestingUpdate), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)));
            }
        }


        [HarmonyPrefix]
        private static bool PatchQuestingUpdate(object __instance, BotOwner ___botOwner)
        {
            if(BossPlayers.IsFollower(___botOwner))
            {
                return false;
            }
            return true;
        }
    }
}
