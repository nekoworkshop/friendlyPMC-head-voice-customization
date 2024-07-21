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
    internal class SAINPatch
    {
        public static bool IsSAINInstalled()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "SAIN");
        }

        private static Type classType = null;

        private static Type squadType = null;

        public static void PatchSAINIfInstalled()
        {
            if (IsSAINInstalled())
            {
                if(classType == null)
                    classType = Type.GetType("SAIN.SAINComponent.Classes.EnemyClasses.EnemyChooserClass, SAIN");

                if(squadType == null)
                {
                    squadType = Type.GetType("SAIN.BotController.Classes.Squad, SAIN");
                }


                Harmony harmony = new Harmony("xyz.pit.companion.sain");

                Components.Logger.LogInfo("Enable SAIN PATCH");

                if (classType != null)
                {
                    harmony.Patch(AccessTools.Method(classType, "assignActiveEnemy"), new HarmonyMethod(typeof(SAINPatch).GetMethod(nameof(PatchAssignActiveEnemy), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)));
                }

                if(squadType != null)
                {
                    harmony.Patch(AccessTools.Method(squadType, "clearPlayerPlace"), new HarmonyMethod(typeof(SAINPatch).GetMethod(nameof(PatchClearPlayerPlace), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)));
                }
            }
        }

        private static bool PatchAssignActiveEnemy(object __instance)
        {

            PropertyInfo botOwnerProperty = classType.GetProperty("BotOwner");
            BotOwner botObject = botOwnerProperty.GetValue(__instance) as BotOwner;
            // disable this for followers
            if (BossPlayers.Instance.IsFollower(botObject)) return false;
            return true;
        }

        private static bool PatchClearPlayerPlace(object __instance, IPlayer player)
        {

            PropertyInfo membersProperty = squadType.GetProperty("Members", BindingFlags.Public | BindingFlags.NonPublic);

            var Members = membersProperty.GetValue(__instance) as Dictionary<string, object>;

            bool allow = true;
            try
            {
                Components.Logger.LogInfo("clearPlayerPlace #1");
                if (Members == null)
                {
                    Components.Logger.LogInfo("Members is NULL");
                }
                foreach (var bot in Members.Values)
                {
                    if (bot != null)
                    {
                        Components.Logger.LogInfo("clearPlayerPlace #2");
                        Type botType = bot.GetType();
                        PropertyInfo botOwnerProperty = botType.GetProperty("BotOwner");
                        Components.Logger.LogInfo("clearPlayerPlace #3");
                        if (botOwnerProperty != null)
                        {
                            Components.Logger.LogInfo("clearPlayerPlace #4");
                            BotOwner botOwner = botOwnerProperty.GetValue(bot) as BotOwner;

                            if (botOwner == null)
                            {
                                Components.Logger.LogInfo("botOwner is null");
                                return true;
                            }
                            Components.Logger.LogInfo("clearPlayerPlace #5");

                            if (botOwner != null && BossPlayers.Instance.IsFollower(botOwner))
                            {
                                // this should not run for the boss player group
                                allow = false;
                                break;
                            }
                        }
                    }
                }
            } catch(Exception ex)
            {
                Components.Logger.LogInfo("Failed to run clearPlayerPlace :" + ex.Message);
            }
            return allow;
        }
    }
}
