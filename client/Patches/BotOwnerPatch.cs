using SPT.Reflection.Patching;
using EFT;

using friendlyPMC.Modules;

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using friendlyPMC.Components;


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
                Modules.Logger.LogError("Exception on BotOwner UpdateManual PatchPostfix");
                Modules.Logger.LogError(e);
            }
        }
    }
    internal class BotOwnerActivatePatch : ModulePatch
    {
        private static List<WildSpawnType> allies = new List<WildSpawnType>
        {
            WildSpawnType.bossKnight,
            WildSpawnType.followerBigPipe,
            WildSpawnType.followerBirdEye,
            WildSpawnType.exUsec
        };
        private static List<Action<BotOwner>> onActivate = new List<Action<BotOwner>>
        {
            // make Goons and exUsecs neutral to the player and his followers if we have completed the first quest from the Goons
            new Action<BotOwner>((BotOwner bot) =>
            {
                foreach (var role in allies)
                {
                    if(bot.IsRole(role))
                    {
                        foreach (var item in BossPlayers.Instance.GetBossPlayers()) 
                        {
                            pitAIBossPlayer boss = item.Value;
                            Player player = boss.realPlayer;
                            string ProfileId = player.ProfileId;
                            foreach (var data in player.Profile.QuestsData) 
                            {
                                if(data.Id == Utils.Props.Quests["Knight"][0])
                                {
                                    if(data.Status == EFT.Quests.EQuestStatus.Success || (data.Status == EFT.Quests.EQuestStatus.Started && role == WildSpawnType.exUsec)) 
                                    {
                                        bot.Memory.IsPeace = true;
                                        bot.Settings.FileSettings.Boss.SHALL_WARN = false;
                                        foreach(var enemy in bot.EnemiesController.EnemyInfos)
                                        {
                                            if(enemy.Key.ProfileId == ProfileId)
                                            {
                                                // - make player neutral to bot
                                                enemy.Value.IgnoreUntilAggression = true;
                                                bot.BotsGroup.RemoveEnemy(player);
                                                bot.Memory.DeleteInfoAboutEnemy(player);
                                                bot.BotsGroup.AddAlly(player);
                                                // - make all player followers neutral to bot
                                                boss.Followers.ForEach(follower => {
                                                    follower.Memory.DeleteInfoAboutEnemy(bot);
                                                    foreach(var en in follower.EnemiesController.EnemyInfos)
                                                    {
                                                        if(en.Key.ProfileId == bot.ProfileId)
                                                        {
                                                            en.Value.IgnoreUntilAggression = true;
                                                            follower.Memory.DeleteInfoAboutEnemy(bot.GetPlayer);
                                                            
                                                            if(follower.Settings.GetEnemyBotTypes().Contains(role))
                                                                follower.Settings.GetEnemyBotTypes().Remove(role);

                                                            follower.Settings.GetFriendlyBotTypes().Add(role);
                                                        }
                                                    }
                                                });
                                                
                                                boss.bossGroup.RemoveEnemy(bot);
                                                boss.bossGroup.AddAlly(bot.GetPlayer);


                                                break;
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        };
                        break;
                    }
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
