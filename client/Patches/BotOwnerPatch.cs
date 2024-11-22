using SPT.Reflection.Patching;
using EFT;

using friendlyPMC.Modules;

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using friendlyPMC.Components;
using Comfort.Common;
using UnityEngine.Profiling;


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
                Logger.LogInfo("Exception on BotOwner UpdateManual PatchPostfix: " + e.Message);
            }
        }
    }
    internal class BotOwnerActivatePatch : ModulePatch
    {

        private static List<Action<BotOwner>> onActivate = new List<Action<BotOwner>>
        {
            // make Goons neutral to the player if we have completed the first quest from the Goons
            new Action<BotOwner>((BotOwner bot) =>
            {

                if(bot.IsRole(WildSpawnType.bossKnight) || bot.IsRole(WildSpawnType.followerBigPipe) || bot.IsRole(WildSpawnType.followerBirdEye))
                {
                    foreach (var item in BossPlayers.Instance.GetBossPlayers()) 
                    {
                        Player player = item.Value.realPlayer;
                        string ProfileId = player.ProfileId;
                        foreach (var data in player.Profile.QuestsData) 
                        {
                            if(data.Id == Utils.Props.Quests["Knight"][0])
                            {

                                if(data.Status == EFT.Quests.EQuestStatus.Success) 
                                {
                                    bot.Memory.IsPeace = true;
                                    bot.Settings.FileSettings.Boss.SHALL_WARN = false;
                                    bool playerFound = false;
                                    foreach(var enemy in bot.EnemiesController.EnemyInfos)
                                    {
                                        if(enemy.Key.ProfileId == ProfileId)
                                        {
                                            playerFound = true;
                                            enemy.Value.IgnoreUntilAggression = true;
                                            bot.BotsGroup.RemoveEnemy(player);
                                            bot.Memory.DeleteInfoAboutEnemy(player);
                                            bot.BotsGroup.AddAlly(player);
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    };
                    
                }
            })
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

            // Fix having followers be enemy of same side just because their roles where under ENEMY_BOT_TYPES
            Dictionary<string, pitAIBossPlayer> playerBosses = BossPlayers.Instance.GetBossPlayers();

            foreach (pitAIBossPlayer boss in playerBosses.Values)
            {
                var followers = BossPlayers.GetFollowersByBoss(boss.Player().ProfileId);

                if (followers.Count > 0)
                {
                    EPlayerSide bossSide = boss.Player().Side;
                    if (bossSide == __instance.Side)
                    {
                        var sett = __instance.Settings.FileSettings;
                        if (
                            (bossSide == EPlayerSide.Bear && !sett.Mind.DEFAULT_BEAR_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack)) ||
                            (bossSide == EPlayerSide.Usec && !sett.Mind.DEFAULT_USEC_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack)) ||
                            (bossSide == EPlayerSide.Savage && !sett.Mind.DEFAULT_SAVAGE_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack))
                        )
                        {
                            foreach (var follower in followers)
                            {
                                var botPlayer = follower.GetBot().GetPlayer;

                                __instance.BotsGroup.RemoveEnemy(botPlayer);
                            }
                        }
                    }
                }
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
