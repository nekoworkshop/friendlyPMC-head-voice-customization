using Aki.Reflection.Patching;
using EFT;
using friendlyPMC.Components;
using System.Reflection;

namespace friendlyPMC.Patches
{
    internal class PlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnDead", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            BossPlayer.Instance.RemoveBossPlayer(__instance.ProfileId);
        }
    }

    internal class SessionEndPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnGameSessionEnd", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            BossPlayer.Instance.RemoveBossPlayer(__instance.ProfileId);
        }
    }


}

