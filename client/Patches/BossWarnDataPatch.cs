using EFT;
using friendlyPMC.Modules;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace friendlyPMC.Patches
{
    /**
     *  Prevent player from being added to the BotWarnData if he is friendly with the specific bot.
     */
    internal class BossWarnDataPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BossWarnData), "method_8");
        }
        [PatchPostfix]
        private static void PatchPostfix(BossWarnData __instance, Dictionary<int, BotSettingsClass> warnTargets)
        {
            var botOwner_0 = AccessTools.Field(typeof(BossWarnData), "botOwner_0").GetValue(__instance) as BotOwner;

            if(botOwner_0 == null) return;

            if(BossPlayers.IsFollower(botOwner_0)) return;

            var bosses = BossPlayers.GetBosses();
            List<int> ids = new List<int>();
            foreach (var item in warnTargets)
            {
                var key = item.Key;
                foreach (var it in bosses)
                {
                    var pl = it.Value;
                    if (pl.realPlayer.Id == key)
                    {
                        if (Utils.Props.friendlyBotTypes.Contains(botOwner_0.Profile.Info.Settings.Role))
                        {
                            ids.Add(key);
                        } 
                        else if(
                            BotGroupAddEnemyPatch.PlayerHasKnightQuest(pl.realPlayer.Profile) && Utils.Props.BossFollowersType.Contains(botOwner_0.Profile.Info.Settings.Role)
                        )
                        {
                            ids.Add(key);
                        } 
                        else if (botOwner_0.Side == pl.realPlayer.Side && friendlyPMC.friendlyPMCFLAG.Value && !(friendlyPMC.badGuy.Value || Utils.Utils.FlagGet("isBadGuy")))
                        {
                            ids.Add(key);
                        }
                        break;
                    }
                };
            }

            ids.ForEach(id =>
            {
                warnTargets.Remove(id);
            });
        }
    }
}
