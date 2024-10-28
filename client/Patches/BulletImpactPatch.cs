using friendlyPMC.Components;
using friendlyPMC.Modules;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using Systems.Effects;

namespace friendlyPMC.Patches
{
    public class BulletImpactPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EffectsCommutator), "PlayHitEffect");
        }

        [PatchPostfix]
        public static void PatchPostfix(EffectsCommutator __instance,EftBulletClass info)
        {
            var player = info.Player;

            if (!__instance.IsHitPointAlreadyProcessed(info.HitPoint))
            {
                if(info.Player != null)
                {
                    BossPlayers.GetFollowers().ForEach(follower =>{
                        var brain = follower.GetBot().Brain.BaseBrain as FollowerBrain;
                        if(brain != null) {
                            brain.BulletFelt(info);
                        } 
                    });
                }
                return;
            }
        }
    }
}
