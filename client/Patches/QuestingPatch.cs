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
                harmony.Patch(AccessTools.Method(questingBrainType, "Update"), new HarmonyMethod(typeof(QuestingPatch).GetMethod(nameof(PatchQuestingUpdate), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)));

                Modules.Logger.LogInfo("QuestingBots Patched");
            }
        }


        [HarmonyPrefix]
        private static bool PatchQuestingUpdate(object __instance)
        {
            if (__instance == null)
            {
                return true;
            }

            var botOwnerField = AccessTools.Field(__instance.GetType(), "botOwner");

            if (botOwnerField == null)
            {
                return true;
            }

            var botOwner = botOwnerField.GetValue(__instance) as BotOwner;

            if (botOwner == null)
            {
                return true;
            }

            if (BossPlayers.IsFollower(botOwner))
            {
                return false;
            }

            return true;
        }

    }
}
