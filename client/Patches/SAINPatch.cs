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

        private static Type enemyTalk = null;
        private static Type GroupClass = null;

        private static Type BotHearingClass = null;

        public static void PatchSAINIfInstalled(Harmony harmony)
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


                
                if (enemyTalk == null)
                {
                    enemyTalk = Type.GetType("SAIN.SAINComponent.Classes.Talk.EnemyTalk, SAIN");

                }

                if(GroupClass == null)
                {
                    GroupClass = Type.GetType("SAIN.SAINComponent.Classes.Talk.GroupTalk, SAIN");
                }

                if (BotHearingClass == null)
                {
                    BotHearingClass = Type.GetType("SAIN.Components.BotControllerSpace.Classes.BotHearingClass, SAIN");
                }

                if (squadType != null)
                {
                    // disable this for followers
                    harmony.Patch(AccessTools.Method(squadType, "clearPlayerPlace"), new HarmonyMethod(typeof(SAINPatch).GetMethod(nameof(PatchClearPlayerPlace), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)));
                }

                if(SAINEnableClass !=null)
                {
                    harmony.Patch(AccessTools.Method(SAINEnableClass, "isBotExcluded"), new HarmonyMethod(typeof(SAINPatch), nameof(PatchisBotExcluded)));
                }

                if(enemyTalk != null)
                {
                    harmony.Patch(AccessTools.Method(enemyTalk, "playerTalked"), new HarmonyMethod(typeof(SAINPatch).GetMethod(nameof(PatchPlayerTalked), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)));
                }

                if (GroupClass != null)
                {
                    harmony.Patch(AccessTools.Method(GroupClass, "EnemyConversation"), new HarmonyMethod(typeof(SAINPatch).GetMethod(nameof(PatchEnemyConvesation), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)));
                }

                if (squadType != null && SAINEnableClass != null)
                {
                    Logger.LogInfo("Enabled SAIN PATCH");
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
                Logger.LogError("Failed to run SAIN clearPlayerPlace Patch");
                Logger.LogError(ex);
            }
            return allow;
        }

        [HarmonyPrefix]
        private static bool PatchisBotExcluded(BotOwner botOwner, ref bool __result)
        {
            if (BossPlayers.IsFollower(botOwner))
            {
                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        private static bool PatchPlayerTalked(EPhraseTrigger phrase, ETagStatus mask, Player player)
        {
            if(phrase == (EPhraseTrigger)CustomPhrases.TeamStatus || phrase == (EPhraseTrigger)CustomPhrases.OverThere)
            {
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        private static bool PatchEnemyConvesation(EPhraseTrigger trigger, ETagStatus status, Player player)
        {
            return PatchPlayerTalked(trigger, status, player);
        }
    }
}
