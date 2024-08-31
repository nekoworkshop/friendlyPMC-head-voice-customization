using SPT.Reflection.Patching;
using EFT.UI.Gestures;
using HarmonyLib;

using System;
using System.Collections.Generic;

using System.Reflection;
using EFT;

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
                    GesturesMenu.Class2965 @class = new GesturesMenu.Class2965();
                    @class.gesturesMenu_0 = __instance;
                    @class.isSituational = false;
                    GestureBaseItem gestureBaseItem = item.CreateNewPhrase(EPhraseTrigger.OnRepeatedContact, @class.isSituational);
                    gestureBaseItem.OnPointerClicked.Subscribe(new Action<GestureBaseItem.GStruct399>(@class.method_0));
                    list_1.Add(gestureBaseItem);
                }

                else if(item.gameObject.name == "TEAM STATUS")
                {
                    GesturesMenu.Class2965 @class = new GesturesMenu.Class2965();
                    @class.gesturesMenu_0 = __instance;
                    @class.isSituational = false;
                    GestureBaseItem gestureBaseItem = item.CreateNewPhrase((EPhraseTrigger)CustomPhrases.TeamStatus, @class.isSituational);
                    gestureBaseItem.OnPointerClicked.Subscribe(new Action<GestureBaseItem.GStruct399>(@class.method_0));
                    list_1.Add(gestureBaseItem);
                }
            });
        }
    }

    internal class GestureMenuAvailablePhrasesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GesturesMenu), "Init");
        }
        [PatchPostfix]
        private static void PatchPostfix(GesturesMenu __instance)
        {
            var hashSet_1 = (HashSet<EPhraseTrigger>)AccessTools.Field(typeof(GesturesMenu), "hashSet_1").GetValue(__instance);
            hashSet_1.Add((EPhraseTrigger)CustomPhrases.TeamStatus);
        }
    }

    internal class PhraseSpeakerClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PhraseSpeakerClass), "Init");
        }
        [PatchPostfix]
        private static void PatchPostfix(PhraseSpeakerClass __instance, EPlayerSide side, int id, string playerVoice, bool registerInSpeakerManager)
        {

            if (side == EPlayerSide.Bear || side == EPlayerSide.Usec)
            {
                foreach (var item in __instance.PhrasesBanks)
                {
                    if (item.Value.Clips.Length <= 0) continue;

                    if(item.Key == EPhraseTrigger.GetBack)
                    {
                        item.Value.Clips = new TaggedClip[] {
                            item.Value.Clips[Math.Min(4,item.Value.Clips.Length -1)]
                        };
                    } 
                    else if (item.Key == EPhraseTrigger.HoldPosition)
                    {
                        item.Value.Clips = new TaggedClip[] {
                            item.Value.Clips[0]
                        };
                    }
                    else if (item.Key == EPhraseTrigger.CoverMe)
                    {
                        item.Value.Clips = new TaggedClip[] {   
                            item.Value.Clips[0]
                        };
                    }
                }
            }
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
                } else if (trigger == (EPhraseTrigger)CustomPhrases.TeamStatus)
                {
                    __result = "Status Report";
                    return false;
                }
            }

            return true;
        }
    }
}