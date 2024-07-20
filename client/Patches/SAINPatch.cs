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
        private static bool IsSAINInstalled()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "SAIN");
        }

        private static Type classType = null;

        public static void PatchSAINIfInstalled()
        {
            if (IsSAINInstalled())
            {
                if(classType == null)
                    classType = Type.GetType("SAIN.SAINComponent.Classes.EnemyClasses.EnemyChooserClass, SAIN");

                if (classType != null)
                {
                    Harmony harmony = new Harmony("xyz.pit.companion.sain");

                    harmony.Patch(AccessTools.Method(classType, "assignActiveEnemy"), new HarmonyMethod(typeof(SAINPatch).GetMethod(nameof(PatchAssignActiveEnemy), BindingFlags.NonPublic | BindingFlags.Static)));
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
    }
}
