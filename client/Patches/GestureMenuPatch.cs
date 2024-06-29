using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.UI.Gestures;
using friendlyPMC.Modules;
using friendlyPMC.Utils;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.EventSystems;


namespace friendlyPMC.Patches
{

    internal class GestureMenuPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GesturesMenu), "InitPhraseGroups");
        }
        [PatchPostfix]
        private static void PatchPostfix(GesturesMenu __instance)
        {
            var list_0 = (List<GesturesAudioItem>)AccessTools.Field(typeof(GesturesMenu), "list_0").GetValue(__instance);
            var list_1 = (List<GestureBaseItem>)AccessTools.Field(typeof(GesturesMenu), "list_1").GetValue(__instance);

            list_0.ForEach(item =>
            {
                if(item.gameObject.name == "ENEMY")
                {
                    GesturesMenu.Class2921 @class = new GesturesMenu.Class2921();
                    @class.gesturesMenu_0 = __instance;
                    @class.isSituational = false;
                    GestureBaseItem gestureBaseItem = item.CreateNewPhrase(EPhraseTrigger.OnRepeatedContact, @class.isSituational);
                    gestureBaseItem.OnPointerClicked.Subscribe(new Action<GestureBaseItem.GStruct400>(@class.method_0));
                    list_1.Add(gestureBaseItem);
                }
            });
        }
    }

    internal class EPhraseTriggerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Enum), "ToString", new Type[] { });
        }
        [PatchPrefix]
        private static bool PatchPrefix(Enum __instance, ref string __result)
        {
            if (__instance is EPhraseTrigger trigger)
            {
                if (trigger == EPhraseTrigger.OnRepeatedContact)
                {
                    __result = "Contact";
                    return false;
                } else if (trigger == EPhraseTrigger.PhraseNone)
                {
                    __result = "Check In";
                    return false;
                }
            }

            return true;
        }
    }
}
