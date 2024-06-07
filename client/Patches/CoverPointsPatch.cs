using Aki.Reflection.Patching;
using EFT;
using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Patches
{
    internal class GetClosePointsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {

            return AccessTools.Method(typeof(CoverPointMaster), nameof(CoverPointMaster.GetClosePoints));
        }

        [PatchPrefix]
        public static bool Prefix(Vector3 pos, BotOwner bot, float dist, ref List<CustomNavigationPoint> __result)
        {
            if (BossPlayers.Instance.IsFollower(bot))
            {
                __result = BossPlayers.Instance.GetCovers();
                return false;
            }
            return true;
        }
    }
}
