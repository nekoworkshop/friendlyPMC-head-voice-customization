using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using friendlyPMC.Components;
using HarmonyLib;
using System.Reflection;

using UnityEngine;

namespace friendlyPMC.Patches
{
    internal class PlayerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "OnDead");
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
            return AccessTools.Method(typeof(Player), "OnGameSessionEnd");
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance)
        {
            BossPlayer.Instance.RemoveBossPlayer(__instance.ProfileId);
        }
    }

    internal class PlayerSayPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), "Say");
        }

        [PatchPrefix]
        private static bool PatchPrefix(Player __instance, EPhraseTrigger @event, bool demand = false, float delay = 0f, ETagStatus mask = (ETagStatus)0, int probability = 100, bool aggressive = false)
        {
            if (@event == EPhraseTrigger.Cooperation)
            {
                Logger.LogInfo("Let's Cooperate");
                if (Singleton<BotEventHandler>.Instantiated)
                {
                    Singleton<BotEventHandler>.Instance.SayPhrase(__instance, @event);
                }
            }

            return true;
        }
    }

}

