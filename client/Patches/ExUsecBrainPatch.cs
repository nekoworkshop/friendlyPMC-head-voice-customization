using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Patches
{
    /**
     * Patch for exUsec brain to not add player as an enemy because he just killed an Usec (Goons dependent)
     */
    internal class ExUsecBrainHitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ExUsecBrainClass), "method_17");

        }
        [PatchPrefix]
        private static bool PatchPrefix(ExUsecBrainClass __instance, DamageInfoStruct damageinfo, Player victim)
        {
            IPlayerOwner player = damageinfo.Player;
            // this is original condition + does player have knight quest
            if (victim.Profile.Side == EPlayerSide.Usec && player.iPlayer.Profile.Side != EPlayerSide.Usec && BotGroupAddEnemy.PlayerHasKnightQuest(player.iPlayer.Profile))
            {
                return false;
            }
            return true;
        }
    }
}
