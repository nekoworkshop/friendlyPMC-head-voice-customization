using Aki.Reflection.Patching;
using EFT;
using friendlyPMC.Actions;
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
            if (!BossPlayers.Instance.IsFollower(bot)) return true;

            if(type == BotLogicDecision.doorOpen)
            {
                __result = new FollowerDoorOpener(bot, bot.BotRequestController.CurRequest as GClass509);
                return false;
            }

            if(type == BotLogicDecision.botTakeItem)
            {
                __result = new FollowerTakeLoot(bot);
                return false;
            }
            
            return true;
        }
    }
}
