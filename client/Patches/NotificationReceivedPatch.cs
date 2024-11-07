using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

using System.Reflection;

namespace friendlyPMC.Patches
{
    /** Patch notification handler to help with making Knight part of our group **/
    internal class NotificationReceivedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TarkovApplication), "method_20");
        }
        [PatchPostfix]
        private static void PatchPostfix(TarkovApplication __instance, NotificationAbstractClass notification)
        {
            try
            {
                if (notification is GClass2006)
                {
                    GClass2006 groupAccepted = (GClass2006)notification;

                    if (groupAccepted.Info.Nickname == "Knight")
                    {
                        Utils.Utils.FlagSet("spawnKnight", true);
                    }
                }
            }
            catch (System.Exception e)
            {
                Modules.Logger.LogError(e);
            }
        }
    }
}
