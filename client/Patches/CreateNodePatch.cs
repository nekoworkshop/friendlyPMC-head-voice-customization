using Aki.Reflection.Patching;
using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
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

            if(type == BotLogicDecision.doorOpen && BossPlayers.Instance.IsFollower(bot))
            {
                __result = new FollowerDoorOpener(bot, bot.BotRequestController.CurRequest as GClass509);
                bot.BotRequestController.CurRequest.Complete();
                return false;
            }
            
            return true;
        }
    }
}
