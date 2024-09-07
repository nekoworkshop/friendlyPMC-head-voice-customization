using EFT;
using HarmonyLib;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Patches
{
    internal class RaidStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Class266), "SendRaidSettings");
        }
        // do not let OfferBot run, we have our own method for adding followers to the player
        // somehow this is not fired in pitAIBossPlayer
        [PatchPostfix]
        private static void PatchPostfix(Class266 __instance, RaidSettings settings)
        {
            var converterClass = typeof(AbstractGame).Assembly.GetTypes()
                .First(t => t.GetField("Converters", BindingFlags.Static | BindingFlags.Public) != null);

            var _defaultJsonConverters = Traverse.Create(converterClass).Field<JsonConverter[]>("Converters").Value;

            RequestHandler.PutJson("/client/raid/pitconfig", new
            {
                Config = new Dictionary<string, bool>
                {
                    { "sameSideHostile", friendlyPMC.sameSideHostile.Value },
                    { "pmcArmbands", friendlyPMC.pmcArmbands.Value },
                    { "englishBear", friendlyPMC.englishBear.Value }
                }
            }.ToJson(_defaultJsonConverters));
        }
    }
}
