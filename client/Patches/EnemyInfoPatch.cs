using SPT.Reflection.Patching;
using EFT;
using friendlyPMC.Modules;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace friendlyPMC.Patches
{
    internal class EnemyInfoIsPointInVisibleSectorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), "IsPointInVisibleSector");
        }

        [PatchPrefix]
        private static bool PatchPrefix(EnemyInfo __instance, ref bool __result, Vector3 position)
        {
            if (__instance.Owner.LookSensor.IsFullSectorView) return true;

            BotOwner Owner = __instance.Owner;
            Vector3 v = position - Owner.Position;

            // followers have better vision angle in order to counter the disabling of their CalcGoal action
            if (BossPlayers.Instance.IsFollower(Owner))
            {
                return true;
                /* float cos = -0.939693f;

                __result = GClass759.IsAngLessNormalized(Owner.LookDirection, GClass759.NormalizeFastSelf(v), cos);

                return false; */
            }

            return true;

        }
    }
}
