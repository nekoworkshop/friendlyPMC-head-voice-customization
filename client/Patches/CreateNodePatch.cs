using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using System.Reflection;

namespace friendlyPMC.Patches
{
    internal class CreateNodePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GClass460), "CreateNode");
        }

        [PatchPrefix]
        private static bool PatchPrefix(BotSpawner __instance, BotLogicDecision type, BotOwner bot, ref GClass134 __result)
        {
            /*if (Components.BossPlayer.Instance.IsFollower(bot))
            {
                if (type == BotLogicDecision.doorOpen)
                {
                    Components.Logger.LogInfo("called door opened");
                    return false;
                }
            }*/
            return true;
        }
    }
}
