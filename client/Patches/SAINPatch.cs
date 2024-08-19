using Diz.Skinning;
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

        private static Type squadType = null;
        private static Type SAINEnableClass = null;

        public static void PatchSAINIfInstalled()
        {
            if (IsSAINInstalled())
            {

                if(squadType == null)
                {
                    squadType = Type.GetType("SAIN.BotController.Classes.Squad, SAIN");
                }

                if (SAINEnableClass == null)
                {
                    SAINEnableClass = Type.GetType("SAIN.SAINEnableClass, SAIN");
                }


                Harmony harmony = new Harmony("xyz.pit.companion.sain");

                Components.Logger.LogInfo("Enable SAIN PATCH");

                if (squadType != null)
                {
                    // disable this for followers
                    harmony.Patch(AccessTools.Method(squadType, "clearPlayerPlace"), new HarmonyMethod(typeof(SAINPatch).GetMethod(nameof(PatchClearPlayerPlace), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)));
                }

                if(SAINEnableClass !=null)
                {
                    harmony.Patch(AccessTools.Method(SAINEnableClass, "IsSAINDisabledForBot"), new HarmonyMethod(typeof(SAINPatch), nameof(PatchIsSAINDisabledForBot)));
                }
            }
        }
        [HarmonyPrefix]
        private static bool PatchClearPlayerPlace(object __instance, IPlayer player)
        {

            PropertyInfo membersProperty = squadType.GetProperty("Members");

            if (membersProperty == null)
            {
                return true;
            }

            var Members = membersProperty.GetValue(__instance);

            if (Members == null)
            {
                return true;
            }
            
            bool allow = true;

            if (BossPlayers.Instance == null) return allow;

            try
            {
                // Get the type of the dictionary
                Type dictionaryType = Members.GetType();
                // Get the enumerator method
                MethodInfo getEnumeratorMethod = dictionaryType.GetMethod("GetEnumerator");
                // Get the enumerator
                var enumerator = getEnumeratorMethod.Invoke(Members, null);
                // Get the type of the enumerator
                Type enumeratorType = enumerator.GetType();
                // Get the MoveNext method
                MethodInfo moveNextMethod = enumeratorType.GetMethod("MoveNext");
                // Get the Current property
                PropertyInfo currentProperty = enumeratorType.GetProperty("Current");
            
                while ((bool)moveNextMethod.Invoke(enumerator, null))
                {
                    var current = currentProperty.GetValue(enumerator);

                    // Get the KeyValuePair type
                    Type kvpType = current.GetType();

                    // Get the Value property
                    PropertyInfo valueProperty = kvpType.GetProperty("Value");

                    var bot = valueProperty.GetValue(current);
                    if (bot != null)
                    {

                        Type botType = bot.GetType();
                        PropertyInfo botOwnerProperty = botType.GetProperty("BotOwner");

                        if (botOwnerProperty != null)
                        {

                            BotOwner botOwner = botOwnerProperty.GetValue(bot) as BotOwner;

                            if (botOwner == null)
                            {

                                return true;
                            }

                            if (botOwner != null && BossPlayers.IsFollower(botOwner))
                            {
                                allow = false;
                                break;
                            }
                        }
                    }
                }
            } catch(Exception ex)
            {
                Components.Logger.LogInfo("Failed to run SAIN clearPlayerPlace Patch :" + ex.Message);
            }
            return allow;
        }

        [HarmonyPrefix]
        private static bool PatchIsSAINDisabledForBot(BotOwner botOwner)
        {
            if (BossPlayers.IsFollower(botOwner))
            {
                return false;
            }
            return true;
        }
    }
}
