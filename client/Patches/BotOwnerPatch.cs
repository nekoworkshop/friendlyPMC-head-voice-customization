using SPT.Reflection.Patching;
using EFT;

using friendlyPMC.Modules;

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using friendlyPMC.Components;


namespace friendlyPMC.Patches
{
    /** Skip checking bot's role if we have made this bot a follower of a boss player **/
    internal class BotOwnerIsFolowerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "IsFollower");

        }
        
        [PatchPrefix]
        private static bool PatchPrefix(BotOwner __instance, ref bool __result)
        {   

            if (BossPlayers.IsFollower(__instance))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
    /** Patch on botOwner UpdateManual to allow us to execute custom code **/
    internal class BotOwnerManualUpdatePatch : ModulePatch
    {

        public static Dictionary<string, Action<BotOwner>> BotOwnerUpdate = new Dictionary<string, Action<BotOwner>>();
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "UpdateManual");
        }
       
        [PatchPostfix]
        private static void PatchPostfix(BotOwner __instance)
        {
            try
            {
                if (
                    __instance != null &&
                    __instance.BotState == EBotState.Active &&
                    __instance.GetPlayer != null &&
                    __instance.GetPlayer.HealthController != null &&
                    __instance.ProfileId != null &&
                    __instance.GetPlayer.HealthController.IsAlive
                )
                {
                    Action<BotOwner> OnUpdate;
                    BotOwnerUpdate.TryGetValue(__instance.ProfileId, out OnUpdate);
                    if (OnUpdate != null) OnUpdate(__instance);

                }
            }
            catch (Exception e)
            {
                Modules.Logger.LogError("Exception on BotOwner UpdateManual PatchPostfix");
                Modules.Logger.LogError(e);
            }
        }
    }
    internal class BotOwnerActivatePatch : ModulePatch
    {
        private static List<WildSpawnType> allies = new List<WildSpawnType>
        {
            WildSpawnType.bossKnight,
            WildSpawnType.followerBigPipe,
            WildSpawnType.followerBirdEye,
            WildSpawnType.exUsec
        };
        private static List<Action<BotOwner>> onActivate = new List<Action<BotOwner>>
        {
            
        };
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "method_10");

        }
        
        [PatchPostfix]
        private static void PatchPostfix(BotOwner __instance)
        {
            
            if (BossPlayers.IsFollower(__instance)) return;

            try
            {
                onActivate.ForEach(action => action(__instance));
            }
            catch (Exception e)
            {
                Modules.Logger.LogError(e);
            }
        }

        public static void AddOnActivate(Action<BotOwner> action)
        { 
            if(!onActivate.Contains(action)) onActivate.Add(action);
        }

        public static void RemoveOnActivate(Action<BotOwner> action)
        {
            if(onActivate.Contains(action))onActivate.Remove(action);
        }
    }
}
