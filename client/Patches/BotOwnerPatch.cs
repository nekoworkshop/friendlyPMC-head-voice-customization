using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using friendlyPMC.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Patches
{

    internal class BotOwnerDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner),"method_9");

        }

        [PatchPrefix]
        private static bool PatchPrefix(BotOwner __instance, DamageInfo damageInfo, EBodyPart bodyType, float damageReducedByArmor)
        {

            // if BOT is getting hit by a player BOSS of which it is a follower of, do not turn hostile
            if (damageInfo.Player != null && !damageInfo.Player.IsAI)
            {
                __instance.StandBy.GetHit();

                AIBossPlayer player = BossPlayer.Instance.GetBossPlayer(damageInfo.Player.iPlayer.ProfileId);

                if (player != null && player.Followers.Find(it => it == __instance) && __instance.BotFollower.BossToFollow == player)
                {
                    // - yell "friendly fire"
                    __instance.BotTalk.TrySay(EPhraseTrigger.FriendlyFire);

                    __instance.StandBy.GetHit();

                    Components.Logger.Instance.LogInfo("Friendly Fire!");
                    return false;
                }
            }

            return true;
        }
    }

    public class BotOwnerIsFolowerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "IsFollower");

        }
        [PatchPrefix]
        private static bool PatchPrefix(BotOwner __instance, ref bool __result)
        {   // skip checking bot's role if we have made this bot a follower of a boss player
            if (BossPlayer.Instance.IsFollower(__instance))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }
}
