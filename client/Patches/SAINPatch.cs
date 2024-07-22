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

        private static Type classType = null;

        private static Type squadType = null;

        private static Type targetType = null;

        private static Type hearingType = null;

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

                if(targetType == null)
                    targetType = Type.GetType("SAIN.SAINComponent.Classes.CurrentTargetClass, SAIN"); 

                if(hearingType == null)
                    hearingType = Type.GetType("SAIN.SAINComponent.Classes.SAINHearingSensorClass, SAIN");


                Harmony harmony = new Harmony("xyz.pit.companion.sain");

                Components.Logger.LogInfo("Enable SAIN PATCH");

                if (classType != null)
                {
                    // disable this for followers
                    harmony.Patch(AccessTools.Method(classType, "Update"), new HarmonyMethod(typeof(SAINPatch).GetMethod(nameof(PatchAssignActiveEnemy), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)));
                }

                if(squadType != null)
                {
                    // disable this for followers
                    harmony.Patch(AccessTools.Method(squadType, "clearPlayerPlace"), new HarmonyMethod(typeof(SAINPatch).GetMethod(nameof(PatchClearPlayerPlace), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)));

                    harmony.Patch(AccessTools.Method(squadType, "calcGoalForBot"), new HarmonyMethod(typeof(SAINPatch).GetMethod(nameof(PatchCalcGoalForBot), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)));
                }

                if(targetType != null)
                    // disable this for followers
                    harmony.Patch(AccessTools.Method(targetType, "updateGoalTarget"), new HarmonyMethod(typeof(SAINPatch).GetMethod(nameof(PatchUpdateGoalTarget), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)));

                if (hearingType != null)
                    harmony.Patch(AccessTools.Method(hearingType, "CheckCalcGoal"), new HarmonyMethod(typeof(SAINPatch).GetMethod(nameof(PatchCheckCalcGoal), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)));
            }
        }

        private static bool PatchAssignActiveEnemy(object __instance)
        {
            try
            {
                PropertyInfo botOwnerProperty = classType.GetProperty("BotOwner");
                BotOwner botObject = botOwnerProperty.GetValue(__instance) as BotOwner;

                if (BossPlayers.Instance.IsFollower(botObject)) return false;
            }
            catch (Exception ex)
            {
                Components.Logger.LogInfo("Failed to run SAIN EnemyChooserClass Update Patch :" + ex.Message);
            }
            return true;
        }

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
                Components.Logger.LogInfo("Members is NULL");
                return true;
            }
            
            bool allow = true;

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
                Components.Logger.LogInfo("Failed to run SAIN clearPlayerPlace Patch :" + ex.Message);
            }
            return allow;
        }

        private static bool PatchCalcGoalForBot(object __instance, BotOwner botOwner)
        {
            return BossPlayers.Instance.IsFollower(botOwner);
        }

        private static bool PatchUpdateGoalTarget(object __instance)
        {
            try
            {
                PropertyInfo botOwnerProperty = targetType.GetProperty("BotOwner");
                BotOwner botObject = botOwnerProperty.GetValue(__instance) as BotOwner;

                if (BossPlayers.Instance.IsFollower(botObject)) return false;
            }
            catch (Exception ex)
            {
                Components.Logger.LogInfo("Failed to run updateGoalTarget Patch :" + ex.Message);
            }
            return true;
        }
    
        private static bool PatchCheckCalcGoal(object __instance)
        {
            PropertyInfo botOwnerProperty = hearingType.GetProperty("BotOwner");
            BotOwner botObject = botOwnerProperty.GetValue(__instance) as BotOwner;

            if (BossPlayers.Instance.IsFollower(botObject))
            {
                EnemyInfo potentialEnemy = botObject.EnemyChooser.FindDangerEnemy();

                return potentialEnemy != null && Utils.Utils.GetNavDistance(botObject.GetPlayer.Transform.position, potentialEnemy.Person.Position) < 35f;
            }

            return true;
        }
    }
}
