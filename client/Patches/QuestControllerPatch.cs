

using EFT;
using EFT.Quests;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace friendlyPMC.Patches
{
    internal class QuestControllerPatch
    {
        // Prefix method to be executed before the original method_15
        static bool Prefix(GClass3250 __instance, IConditionCounter conditional, EQuestStatus status, Condition condition)
        {
            Modules.Logger.LogInfo("Condition " + condition.id);
            return true; // Continue with the original method_15
        }

        public static void ApplyPatch(Harmony harmonyInstance)
        {
            // Get the MethodInfo for method_15 from the abstract class
            Type abstractClassType = typeof(QuestControllerAbstractClass<>).MakeGenericType(typeof(QuestClass));
            MethodInfo targetMethod = abstractClassType.GetMethod("method_15", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (targetMethod != null)
            {
                var prefixMethod = typeof(QuestControllerPatch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
                harmonyInstance.Patch(targetMethod, prefix: new HarmonyMethod(prefixMethod));
            }
            else
            {
                Modules.Logger.LogError("Failed to find method_15 in QuestControllerAbstractClass.");
            }
        }
    }
}
